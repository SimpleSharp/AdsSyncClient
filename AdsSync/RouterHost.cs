using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.NetworkInformation;
using TwinCAT.Ads;
using TwinCAT.Ads.Configuration;
using TwinCAT.Ads.TcpRouter;
using TwinCAT.Router;
using TwinCAT.Ads.SystemService;

namespace AdsSync
{
    /// <summary>
    /// Hosts an in-process ADS TCP/IP router including its router and system service servers,
    /// and registers the route to the target system.
    /// </summary>
    /// <remarks>
    /// This class is used by <see cref="AdsSyncClient"/> whenever no local TwinCAT router is
    /// available (e.g. Linux machines or Windows hosts without a TwinCAT installation). It starts
    /// three components:
    /// <list type="bullet">
    /// <item><b>AmsTcpIpRouter</b> – the actual router listening on TCP port 48898.</item>
    /// <item><b>AdsRouterServer</b> – the internal ADS router services (AMS port 1).</item>
    /// <item><b>SystemServiceServer</b> – provides the device states required for connection
    /// monitoring (AMS port 10000).</item>
    /// </list>
    /// Constructing this class throws an <see cref="IOException"/> if port 48898 is already in
    /// use, which usually indicates that a TwinCAT router is already running locally.
    ///
    /// Note: The reverse route on the target system (to <c>PartialLocalNetId</c>) must be configured
    /// manually in the TwinCAT runtime once (Static Routes), since the target must know the way back.
    /// </remarks>
    internal sealed partial class RouterHost : ObservableObject, IAsyncDisposable
    {
        #region constants
        /// <summary> The TCP port used by ADS routers </summary>
        private const int RouterPort = 48898;
        #endregion

        #region properties
        /// <summary> Indicates whether the router is currently running </summary>
        [ObservableProperty] private bool isRunning;
        #endregion

        #region fields
        /// <summary> The router configuration </summary>
        private readonly RouterConfiguration configuration;
        /// <summary> The logger factory used for all hosted components </summary>
        private readonly ILoggerFactory loggerFactory;
        /// <summary> Logger for diagnostics </summary>
        private readonly ILogger logger;
        /// <summary> Serializes start/stop operations </summary>
        private readonly SemaphoreSlim lifecycleLock = new(1, 1);
        /// <summary> The in-process TCP/IP router </summary>
        private AmsTcpIpRouter? router;
        /// <summary> The ADS router server (AMS port 1) </summary>
        private AdsRouterServer? routerServer;
        /// <summary> The system service server (AMS port 10000) </summary>
        private SystemServiceServer? systemService;
        /// <summary> Long-running task representing the router operation </summary>
        private Task? routerTask;
        /// <summary> Long-running task representing the router server operation </summary>
        private Task<AdsErrorCode>? routerServerTask;
        /// <summary> Long-running task representing the system service operation </summary>
        private Task<AdsErrorCode>? systemServiceTask;
        /// <summary> Cancellation source for terminating all hosted components </summary>
        private CancellationTokenSource? tokenSource;
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="configuration"> The router configuration describing the local and the target system </param>
        /// <param name="loggerFactory"> Optional logger factory forwarded to all hosted components. </param>
        /// <exception cref="IOException"> Thrown when the ADS router port 48898 is already in use. This usually
        /// means that a TwinCAT router is running locally, in which case an in-process router is not required. </exception>
        public RouterHost(RouterConfiguration configuration, ILoggerFactory? loggerFactory = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            EnsureRouterPortIsFree();
            this.configuration = configuration;
            this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            logger = this.loggerFactory.CreateLogger<RouterHost>();
            logger.LogInformation("RouterHost created for local NetId '{LocalNetId}', target '{TargetNetId}' at '{RemoteHost}'.",
                                 configuration.LocalNetId, configuration.TargetAddress.NetId, configuration.RemoteHost);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Starts the in-process router, its servers, and registers the route to the target system.
        /// </summary>
        /// <param name="cancellationToken"> Token for cancelling the operation </param>
        /// <exception cref="ObjectDisposedException"> Thrown when this object has been disposed. </exception>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await lifecycleLock.WaitAsync(cancellationToken);
            try
            {
                if (isRunning)
                {
                    return;
                }
                tokenSource = new CancellationTokenSource();
                // 1. The actual TCP/IP router
                router = new AmsTcpIpRouter(configuration.LocalNetId,
                                            RouterPort,
                                            null,
                                            RouterPort,
                                            (IPNetwork?)null,
                                            loggerFactory);
                routerTask = router.StartAsync(tokenSource.Token);
                // 2. The ADS router server (AMS port 1)
                routerServer = new AdsRouterServer(router, loggerFactory);
                routerServerTask = routerServer.ConnectServerAndWaitAsync(tokenSource.Token);
                // 3. The system service server (AMS port 10000)
                systemService = new SystemServiceServer(router, loggerFactory);
                systemServiceTask = systemService.ConnectServerAndWaitAsync(tokenSource.Token);
                // 4. The route to the target system
                // NOTE: Extracts the first four octets from TargetAddress.NetId to build the destination IP for the route
                string targetIp = string.Join(".", configuration.TargetAddress.NetId.ToString().Split('.').Take(4));
                Route route = new("AdsSyncClient", configuration.TargetAddress.NetId,
                                  [IPAddress.Parse(targetIp)]);
                router.AddRoute(route);
                ObserveBackgroundTask(routerTask!, nameof(AmsTcpIpRouter));
                ObserveBackgroundTask(routerServerTask!, nameof(AdsRouterServer));
                ObserveBackgroundTask(systemServiceTask!, nameof(SystemServiceServer));
                IsRunning = true;
                logger.LogInformation("In-process ADS router started (local NetId '{LocalNetId}').",
                                      configuration.LocalNetId);
            }
            finally
            {
                lifecycleLock.Release();
            }
        }

        /// <summary>
        /// Stops the in-process router and all its servers in reverse startup order.
        /// </summary>
        /// <param name="cancellationToken"> Token for cancelling the operation </param>
        /// <exception cref="ObjectDisposedException"> Thrown when this object has been disposed. </exception>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await lifecycleLock.WaitAsync(cancellationToken);
            try
            {
                if (!IsRunning)
                {
                    return;
                }
                IsRunning = false;
                // Reverse shutdown order: servers first, then the router
                systemService?.Dispose();
                routerServer?.Dispose();
                if (router is not null)
                {
                    await tokenSource!.CancelAsync();
                    router.Stop();
                }
                systemService = null;
                routerServer = null;
                router = null;
                tokenSource?.Dispose();
                tokenSource = null;
                logger.LogInformation("In-process ADS router stopped.");
            }
            finally
            {
                lifecycleLock.Release();
            }
        }

        /// <summary> Releases all resources used by this instance. </summary>
        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }
            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
                await StopAsync(cts.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while stopping the in-process ADS router during dispose.");
            }
            finally
            {
                lifecycleLock.Dispose();
                disposed = true;
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Verifies that the ADS router port is not already in use on this machine.
        /// </summary>
        /// <exception cref="IOException"> Thrown when the ADS router port 48898 is already in use. This usually
        /// means that a TwinCAT router is running locally, in which case an in-process router is not required. </exception>
        private static void EnsureRouterPortIsFree()
        {
            IPEndPoint[] activeListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            if (activeListeners.Any(listener => listener.Port == RouterPort))
            {
                throw new IOException($"The ADS router port {RouterPort} is already in use. If TwinCAT is installed " +
                                      $"locally, a system router is already running and no in-process router is " +
                                      $"required. Use an AdsSyncClient constructor without a RouterConfiguration instead.");
            }
        }

        /// <summary>
        /// Logs a faulted background task to prevent unobserved task exceptions.
        /// </summary>
        private void ObserveBackgroundTask(Task task, string componentName)
        {
            _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception is not null)
                {
                    logger.LogError(t.Exception, "{ComponentName} terminated unexpectedly.", componentName);
                }
            }, TaskScheduler.Default);
        }
        #endregion
    }
}
