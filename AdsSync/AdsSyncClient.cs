using AdsSync.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel;
using TwinCAT.Ads;

namespace AdsSync
{

    /// <summary>
    /// Provides synchronized data exchange between .NET objects and an ADS client.
    /// </summary>
    /// <remarks>
    /// This class monitors property changes on the object containing the data to be written to the ADS client and writes updated values to the corresponding ADS
    /// variables. It also reads values from the ADS client when notifications are received and updates the corresponding properties on the object containing the read data.
    /// The class manages the ADS connection, variable handles, notification handles, synchronization, and optionally an in-process ADS router.
    ///
    /// Two operating modes are supported:
    /// <list type="bullet">
    /// <item><b>System router mode (default):</b> Uses the locally installed TwinCAT router (Windows with TwinCAT
    /// XAR runtime installed). The target address is passed directly.</item>
    /// <item><b>In-process router mode:</b> Hosts its own TCP/IP ADS router (Linux, macOS, or Windows without a
    /// TwinCAT installation). The router is started before the first connection attempt and stopped when this
    /// instance is disposed. Requires a <see cref="RouterConfiguration"/> including the reverse route on the
    /// target system to be configured manually.</item>
    /// </list>
    /// Call <see cref="ActivateSync"/> to start the data exchange and <see cref="StopSyncAsync"/> to stop it. The
    /// instance must be disposed when it is no longer needed.
    /// </remarks>
    public partial class AdsSyncClient : ObservableObject, IAsyncDisposable
    {
        #region properties
        /// <summary> Status of the ADS client's hardware </summary>
        [ObservableProperty] private AdsState stateHardware = AdsState.Invalid;
        /// <summary> Status of the ADS client's software </summary>
        [ObservableProperty] private AdsState stateSoftware = AdsState.Invalid;
        /// <summary> Indicates whether the ADS client is connected </summary>
        [ObservableProperty] private bool isConnected;
        /// <summary> Indicates whether the ADS client sync is active </summary>
        [ObservableProperty] private bool isActive;
        /// <summary> Indicates whether this instance hosts an in-process ADS router </summary>
        public bool UsesInProcessRouter => routerHost is not null;
        #endregion

        #region fields & events
        /// <summary> Event raised when an exception is thrown in a background task </summary>
        public event EventHandler<ConnectionErrorEventArgs>? ConnectionError;
        /// <summary> Event raised when an exception is thrown while reading / writing </summary>
        public event EventHandler<CommunicationErrorEventArgs>? CommunicationError;
        /// <summary> Manages the ADS connection lifecycle, state monitoring, and reconnection logic </summary>
        private readonly AdsConnectionManager connectionManager;
        /// <summary> Handles data mapping and synchronization between .NET objects and ADS variables. </summary>
        private readonly AdsDataSynchronizer adsDataSynchronizer;
        /// <summary> Hosts the in-process ADS router if the router mode is used; otherwise null. </summary>
        private readonly RouterHost? routerHost;
        /// <summary> The logger factory used by this instance and all owned components </summary>
        private readonly ILoggerFactory loggerFactory;
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor using the locally installed TwinCAT system router (system router mode).
        /// </summary>
        /// <remarks>
        /// Use this constructor if a TwinCAT XAR runtime with its system router is installed locally (typical
        /// Windows engineering station or IPC). The library creates and manages its own internal <c>AdsClient</c>.
        /// </remarks>
        /// <param name="definition"> The definition of the data exchange between the application and the ADS client </param>
        /// <param name="targetAddress"> The AMS address (NetId and port) of the target system </param>
        /// <param name="loggerFactory"> Optional logger factory used for diagnostics. If null, logging is disabled. </param>
        public AdsSyncClient(AdsSyncDefinition definition,
                             AmsAddress targetAddress,
                             ILoggerFactory? loggerFactory = null)
            : this(new AdsClient() { Timeout = 500 },
                   definition,
                   targetAddress,
                   loggerFactory,
                   routerConfiguration: null)
        { }

        /// <summary>
        /// Constructor hosting an in-process ADS TCP/IP router (in-process router mode).
        /// </summary>
        /// <remarks>
        /// Use this constructor if no TwinCAT XAR runtime is installed locally (Linux, macOS, or Windows without
        /// TwinCAT). The library starts an own TCP/IP ADS router on port 48898 before the first connection attempt
        /// and stops it when this instance is disposed. The target address is taken from
        /// <paramref name="routerConfiguration"/>.
        /// Note that the reverse route on the target system pointing to
        /// <c>routerConfiguration.LocalNetId</c> must be configured manually once.
        /// </remarks>
        /// <param name="definition"> The definition of the data exchange between the application and the ADS client </param>
        /// <param name="routerConfiguration"> The router configuration describing the local and the target system </param>
        /// <param name="loggerFactory"> Optional logger factory used for diagnostics. If null, logging is disabled. </param>
        /// <exception cref="IOException"> Thrown when the ADS router port 48898 is already in use, which usually
        /// means that a TwinCAT system router is running locally. In that case use the constructor without a
        /// <see cref="RouterConfiguration"/> instead. </exception>
        public AdsSyncClient(AdsSyncDefinition definition,
                             RouterConfiguration routerConfiguration,
                             ILoggerFactory? loggerFactory = null)
            : this(new AdsClient() { Timeout = 500 },
                   definition,
                   routerConfiguration.TargetAddress,
                   loggerFactory,
                   routerConfiguration)
        { }

        /// <summary>
        /// Constructor with an externally provided ADS client (intended primarily for unit testing and dependency injection).
        /// </summary>
        /// <remarks>
        /// Use this constructor if you want to provide your own <c>AdsClient</c> instance (system router mode). The
        /// client is not disposed by this class.
        /// </remarks>
        /// <param name="adsClient"> The ADS client used for the communication </param>
        /// <param name="definition"> The definition of the data exchange between the application and the ADS client </param>
        /// <param name="targetAddress"> The AMS address (NetId and port) of the target system </param>
        /// <param name="loggerFactory"> Optional logger factory used for diagnostics. If null, logging is disabled. </param>
        public AdsSyncClient(IAdsConnectAddress adsClient,
                             AdsSyncDefinition definition,
                             AmsAddress targetAddress,
                             ILoggerFactory? loggerFactory = null)
            : this(adsClient,
                   definition,
                   targetAddress,
                   loggerFactory,
                   routerConfiguration: null)
        { }

        /// <summary>
        /// Main constructor wiring up all components according to the selected operating mode.
        /// </summary>
        /// <param name="adsClient"> The ADS client used for the communication </param>
        /// <param name="definition"> The definition of the data exchange between the application and the ADS client </param>
        /// <param name="targetAddress"> The AMS address (NetId and port) of the target system </param>
        /// <param name="loggerFactory"> Optional logger factory used for diagnostics </param>
        /// <param name="routerConfiguration"> The router configuration if the in-process router mode is used; otherwise null </param>
        private AdsSyncClient(IAdsConnectAddress adsClient,
                              AdsSyncDefinition definition,
                              AmsAddress targetAddress,
                              ILoggerFactory? loggerFactory,
                              RouterConfiguration? routerConfiguration)
        {
            this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            routerHost = routerConfiguration is null
                ? null
                : new RouterHost(routerConfiguration, this.loggerFactory);
            adsDataSynchronizer = new AdsDataSynchronizer(adsClient, definition, loggerFactory);
            adsDataSynchronizer.CommunicationError += OnCommunicationError;
            connectionManager = new AdsConnectionManager(adsClient, targetAddress, loggerFactory);
            connectionManager.Connected += InitializeAdsDataSynchronizer;
            connectionManager.Disconnected += StopCommunication;
            connectionManager.ConnectionError += OnConnectionError;
            //Take over the connection state of the connection manager to the properties of this class
            ExecuteOnPropertyChanged(nameof(IsActive), connectionManager, TakeOverIsActiveState);
            ExecuteOnPropertyChanged(nameof(IsConnected), connectionManager, TakeOverIsConnectedState);
            ExecuteOnPropertyChanged(nameof(StateHardware), connectionManager, TakeOverStateHardware);
            ExecuteOnPropertyChanged(nameof(StateSoftware), connectionManager, TakeOverStateSoftware);
        }
        #endregion

        #region public methods and tasks
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Activates the data exchange with the ADS client.
        /// </summary>
        /// <remarks>
        /// In in-process router mode, the ADS router is started first (if not yet running). Afterwards the
        /// behavior is identical for both modes.
        /// </remarks>
        /// <returns> Returns TRUE if connected. Return FALSE otherwise, but will try to connect cyclically.
        /// Check <see cref="IsConnected"/> for the current connection status. Use <see cref="StopSyncAsync"/> if
        /// you want to stop the cyclic communication. </returns>
        public async Task<bool> ActivateSync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (routerHost is not null && !routerHost.IsRunning)
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                await routerHost.StartAsync(cts.Token);
            }
            return await connectionManager.ConnectAsync() && connectionManager.IsConnected;
        }

        /// <summary>
        /// Activates the data exchange with the ADS client using a new target address (system router mode only).
        /// </summary>
        /// <returns> Returns TRUE if connected. Return FALSE otherwise, but will try to connect cyclically.
        /// Check <see cref="IsConnected"/> for the current connection status. Use <see cref="StopSyncAsync"/> if
        /// you want to stop the cyclic communication. </returns>
        /// <exception cref="InvalidOperationException"> Thrown when this instance runs in in-process router mode,
        /// since the target address is fixed by the <see cref="RouterConfiguration"/> in that mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when the new AMS address is null. </exception>
        public async Task<bool> ActivateSync(AmsAddress amsAddress)
        {
            if (UsesInProcessRouter)
            {
                throw new InvalidOperationException("The target address cannot be changed in in-process router mode. " +
                                                    "It is fixed by the RouterConfiguration.");
            }
            ArgumentNullException.ThrowIfNull(amsAddress);
            if (connectionManager.IsConnected)
            {
                return true;
            }
            return connectionManager.TryUpdateAmsAddress(amsAddress) &&
                   await connectionManager.ConnectAsync() &&
                   connectionManager.IsConnected;
        }

        /// <summary>
        /// Stops the data exchange with the ADS client.
        /// </summary>
        /// <returns> Returns TRUE if the connection got stopped. Returns FALSE if a stop
        /// command was already active or if there was no communication at all. </returns>
        public async Task<bool> StopSyncAsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            await connectionManager.DisconnectAsync();
            return !connectionManager.IsConnected;
        }

        /// <summary>
        /// Tries to update the AMS address of the ADS client (system router mode only). This can only be done if the client is not connected.
        /// </summary>
        /// <param name="newAmsAddress"> The new AMS address to update </param>
        /// <returns> Returns TRUE if the AMS address was updated. </returns>
        /// <exception cref="InvalidOperationException"> Thrown when this instance runs in in-process router mode. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when the new AMS address is null. </exception>
        public bool TryUpdateAmsAddress(AmsAddress newAmsAddress)
        {
            if (UsesInProcessRouter)
            {
                throw new InvalidOperationException("The target address cannot be changed in in-process router mode. " +
                                                    "It is fixed by the RouterConfiguration.");
            }
            ArgumentNullException.ThrowIfNull(newAmsAddress);
            return connectionManager.TryUpdateAmsAddress(newAmsAddress);
        }
        #endregion

        #region private methods and tasks
        /// <summary>
        /// Fires an event if the connection manager has a connection error
        /// </summary>
        private void OnConnectionError(object? sender, ConnectionErrorEventArgs? e) => ConnectionError?.Invoke(sender!, e!);

        /// <summary>
        /// Fires an event if the synchronizer has a communication error
        /// </summary>
        private void OnCommunicationError(object? sender, CommunicationErrorEventArgs? e) => CommunicationError?.Invoke(sender!, e!);

        /// <summary>
        /// Initializes the communication when connected
        /// </summary>
        private void InitializeAdsDataSynchronizer(object? sender, EventArgs? e) => _ = adsDataSynchronizer.InitializeCommunicationAsync();

        /// <summary>
        /// Stops the communication when disconnected
        /// </summary>
        private void StopCommunication(object? sender, EventArgs? e) => _ = adsDataSynchronizer.StopCommunicationAsync();

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing"> Disposes this instance if TRUE </param>
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                if (connectionManager.IsConnected)
                {
                    await adsDataSynchronizer.StopCommunicationAsync();
                    await connectionManager.DisconnectAsync();
                }
                await adsDataSynchronizer.DisposeAsync();
                await connectionManager.DisposeAsync();
                // The router must be the last component to go down so that the ADS client
                // and both mappers never talk to a dead router.
                if (routerHost is not null)
                {
                    await routerHost.DisposeAsync();
                }
            }
            NoLongerExecuteOnPropertyChanged(nameof(IsConnected), connectionManager, TakeOverIsConnectedState);
            NoLongerExecuteOnPropertyChanged(nameof(StateHardware), connectionManager, TakeOverStateHardware);
            NoLongerExecuteOnPropertyChanged(nameof(StateSoftware), connectionManager, TakeOverStateSoftware);
            NoLongerExecuteOnPropertyChanged(nameof(IsActive), connectionManager, TakeOverIsActiveState);
            disposed = true;
        }

        /// <summary> Sets IsActive to the connection manager's current IsActive value. </summary>
        private void TakeOverIsActiveState(object? s, EventArgs? e) => IsActive = connectionManager.IsActive;

        /// <summary> Sets IsConnected to the connection manager's current IsConnected value. </summary>
        private void TakeOverIsConnectedState(object? s, EventArgs? e) => IsConnected = connectionManager.IsConnected;

        /// <summary> Sets StateHardware to the connection manager's current StateHardware value. </summary>
        private void TakeOverStateHardware(object? s, EventArgs? e) => StateHardware = connectionManager.StateHardware;

        /// <summary> Sets StateSoftware to the connection manager's current StateSoftware value. </summary>
        private void TakeOverStateSoftware(object? s, EventArgs? e) => StateSoftware = connectionManager.StateSoftware;

        /// <summary>
        /// Enables executing an event when the value of a property changes
        /// </summary>
        private static void ExecuteOnPropertyChanged(string nameofProperty, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(nameofProperty, false)!;
            property.AddValueChanged(objectInstance, handler);
        }

        /// <summary>
        /// Ends executing an event when the value of a property changes
        /// </summary>
        private static void NoLongerExecuteOnPropertyChanged(string nameofProperty, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(nameofProperty, false)!;
            property.RemoveValueChanged(objectInstance, handler);
        }
        #endregion
    }
}