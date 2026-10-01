using AdsSync.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Timers;
using TwinCAT.Ads;

namespace AdsSync
{
    /// <summary>
    /// Manages the ADS connection lifecycle, state monitoring, and reconnection logic.
    /// </summary>
    internal partial class AdsConnectionManager : ObservableObject, IAsyncDisposable
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
        #endregion

        #region fields & events
        /// <summary> Event raised when an exception is thrown in a background task </summary>
        public event EventHandler<ConnectionErrorEventArgs>? ConnectionError;
        /// <summary> Event raised when connected </summary>
        public event EventHandler? Connected;
        /// <summary> Event raised when disconnected </summary>
        public event EventHandler? Disconnected;
        /// <summary> The ADS client </summary>
        private readonly IAdsConnectAddress adsClient;
        /// <summary> The ADS client for checking the hardware state </summary>
        private IAdsConnectAddress? adsClientHardwareState;
        /// <summary> The ADS client for checking the software state </summary>
        private IAdsConnectAddress? adsClientSoftwareState;
        /// <summary> The address of the ADS client </summary>
        private AmsAddress amsAddress;
        /// <summary> Logger for diagnostics </summary>
        private readonly ILogger logger;
        /// <summary> Cancellation token source for terminating the communication </summary>
        private CancellationTokenSource tokenSource = new();
        /// <summary> Timer for checking the ADS client state </summary>
        private System.Timers.Timer timer = new(TimeSpan.FromSeconds(2));
        /// <summary> Indicates whether a reconnection is needed </summary>
        private bool isReconnectNeeded;
        /// <summary> Indicates whether the ADS client was connected </summary>
        private bool wasConnected;
        /// <summary> Prevents the user from starting the <see cref="ConnectAsync"/> multiple times </summary>
        private readonly SemaphoreSlim connectLock = new(1, 1);
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="adsClient"> The ADS client </param>
        public AdsConnectionManager(IAdsConnectAddress adsClient) : this(adsClient, AmsAddress.Empty) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="adsClient"> The ADS client </param>
        /// <param name="amsAddress"> The address of the ADS client </param>
        public AdsConnectionManager(IAdsConnectAddress adsClient,
                                    AmsAddress amsAddress) : this(adsClient, amsAddress, null) { }

        /// <summary>
        /// Constructor with an optional logger factory
        /// </summary>
        /// <param name="adsClient"> The ADS client </param>
        /// <param name="amsAddress"> The address of the ADS client </param>
        /// <param name="loggerFactory"> Optional logger factory for diagnostics. If null, logging is disabled. </param>
        public AdsConnectionManager(IAdsConnectAddress adsClient,
                                    AmsAddress amsAddress,
                                    ILoggerFactory? loggerFactory)
        {
            this.adsClient = adsClient;
            this.amsAddress = amsAddress;
            logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AdsConnectionManager>();
            timer.Elapsed += TimerTick;
            logger.LogDebug("AdsConnectionManager created for NetId '{NetId}', Port {Port}.",
                            amsAddress.NetId, amsAddress.Port);
        }
        #endregion

        #region public methods and tasks
        /// <summary>
        /// Releases all resources used by this instance. It is recommended to use <see cref="DisconnectAsync"/>
        /// before using <see cref="DisposeAsync()"/>.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tries to establish a connection to the ADS client. It also starts a cyclic reconnection if the connection fails.
        /// </summary>
        /// <returns> Returns TRUE if the connection is established. </returns>
        public async Task<bool> ConnectAsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (IsConnected)
            {
                return true;
            }
            try
            {
                tokenSource.Dispose();
                tokenSource = new();
                if (!await connectLock.WaitAsync(TimeSpan.FromSeconds(6), tokenSource.Token))
                {
                    return false;
                }
                try
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
                    logger.LogInformation("Connecting to ADS client at {NetId}:{Port}...",
                                          amsAddress.NetId, amsAddress.Port);
                    await adsClient.ConnectAsync(amsAddress, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    IsConnected = false;
                    wasConnected = false;
                    logger.LogWarning("Connection attempt timed out after 5 seconds.");
                    ConnectionError?.Invoke(this, new ConnectionErrorEventArgs(new TimeoutException(), false));
                    return false;
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    wasConnected = false;
                    logger.LogError(ex, "Connection attempt failed.");
                    ConnectionError?.Invoke(this, new ConnectionErrorEventArgs(ex, false));
                    return false;
                }
                await CheckIfIsConnectedAsync();
                if (IsConnected && !wasConnected)
                {
                    Connected?.Invoke(this, EventArgs.Empty);
                    wasConnected = true;
                    logger.LogInformation("Connected to ADS client.");
                }
                else if (!IsConnected)
                {
                    isReconnectNeeded = true;
                    logger.LogDebug("Initial connection not established, cyclic reconnection enabled.");
                }
                IsActive = true;
                timer.Start();
                return IsConnected;
            }
            finally
            {
                connectLock.Release();
            }
        }

        /// <summary>
        /// Disconnects and cancels all pending operations
        /// </summary>
        /// <returns> Returns TRUE if the client is disconnected. </returns>
        public async Task<bool> DisconnectAsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            logger.LogDebug("Disconnect requested.");
            timer.Stop();
            isReconnectNeeded = false;
            await tokenSource.CancelAsync();
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            await adsClient.DisconnectAsync(cts.Token);
            if (!adsClient.IsConnected && wasConnected)
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
                logger.LogInformation("Disconnected from ADS client.");
            }
            await CheckIfIsConnectedAsync();
            wasConnected = false;
            IsActive = false;
            return !adsClient.IsConnected;
        }

        /// <summary>
        /// Tries to update the AMS address of the ADS client. This can only be done if the client is not connected.
        /// </summary>
        /// <param name="newAmsAddress"> The new AMS address to update </param>
        /// <returns> Returns TRUE if the AMS address was updated. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when the new AMS address is null. </exception>
        public bool TryUpdateAmsAddress(AmsAddress newAmsAddress)
        {
            ArgumentNullException.ThrowIfNull(newAmsAddress);
            if (!IsConnected)
            {
                amsAddress = newAmsAddress;
                logger.LogInformation("AMS address updated to {NetId}:{Port}.",
                                      amsAddress.NetId, amsAddress.Port);
                return true;
            }
            logger.LogWarning("AMS address update rejected because the client is still connected.");
            return false;
        }
        #endregion

        #region private methods and tasks

        /// <summary>
        /// Handles a timer tick and starts an asynchronous check of the ADS client state
        /// </summary>
        private void TimerTick(object? sender, ElapsedEventArgs? e)
        {
            _ = CyclicCheckAdsClientStateAsync();
        }

        /// <summary>
        /// Periodically checks the current state of the ADS client.
        /// </summary>
        private async Task CyclicCheckAdsClientStateAsync()
        {
            try
            {
                timer.Stop();
                await CheckAdsClientStateAsync();
            }
            catch (OperationCanceledException)
            {
                /* Normal exit */
                logger.LogDebug("Cyclic state check cancelled.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during cyclic state check.");
                ConnectionError?.Invoke(this, new ConnectionErrorEventArgs(ex, true));
            }
            finally
            {
                timer.Start();
            }
        }

        /// <summary>
        /// Checks the state of the hard- and software of the ADS client. Can also reconnect if the connection is interrupted.
        /// </summary>
        private async Task CheckAdsClientStateAsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (adsClient.IsConnected && StateHardware == AdsState.Run && StateSoftware == AdsState.Run)
            {
                await CheckIfIsConnectedAsync();
                if (!IsConnected)
                {
                    logger.LogWarning("Connection lost despite hardware/software in RUN state. Triggering reconnect.");
                    isReconnectNeeded = true;
                    await adsClient.DisconnectAsync(CancellationToken.None);
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (isReconnectNeeded)
            {
                logger.LogDebug("Attempting automatic reconnection...");
                _ = await ConnectAsync();
                await CheckIfIsConnectedAsync();
                isReconnectNeeded = !IsConnected;
                if (IsConnected)
                {
                    Connected?.Invoke(this, EventArgs.Empty);
                    logger.LogInformation("Automatic reconnection successful.");
                }
            }
        }

        /// <summary>
        /// Gets the current hardware state of the ADS client
        /// </summary>
        /// <returns> Returns TRUE if the hardware is in RUN mode. </returns>
        private async Task<bool> HardwareOfAdsClientIsRunningAsync()
        {
            adsClientHardwareState ??= new AdsClient() { Timeout = 500 };
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            try
            {
                await adsClientHardwareState.ConnectAsync(amsAddress.NetId, (int)AmsPort.SystemService, cts.Token);
                ResultReadDeviceState state = await adsClientHardwareState.ReadStateAsync(cts.Token);
                StateHardware = state.Succeeded
                    ? state.State.AdsState
                    : AdsState.Invalid;
                logger.LogTrace("Hardware state queried: {State}.", StateHardware);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Hardware state query failed.");
                StateHardware = AdsState.Invalid;
            }
            finally
            {
                if (adsClientHardwareState.IsConnected)
                {
                    await adsClientHardwareState.DisconnectAsync(cts.Token);
                }
            }
            return StateHardware == AdsState.Run;
        }

        /// <summary>
        /// Gets the current software state of the ADS client
        /// </summary>
        /// <returns> Returns TRUE if the software is in RUN mode. </returns>
        private async Task<bool> SoftwareOfAdsClientIsRunningAsync()
        {
            adsClientSoftwareState ??= new AdsClient() { Timeout = 500 };
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            try
            {
                await adsClientSoftwareState.ConnectAsync(amsAddress, cts.Token);
                ResultReadDeviceState state = await adsClientSoftwareState.ReadStateAsync(cts.Token);
                StateSoftware = state.Succeeded
                    ? state.State.AdsState
                    : AdsState.Invalid;
                logger.LogTrace("Software state queried: {State}.", StateSoftware);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Software state query failed.");
                StateSoftware = AdsState.Invalid;
            }
            finally
            {
                if (adsClientSoftwareState.IsConnected)
                {
                    await adsClientSoftwareState.DisconnectAsync(cts.Token);
                }
            }
            return StateSoftware == AdsState.Run;
        }

        /// <summary>
        /// Gets the current "IsConnected" state.
        /// </summary>
        /// <returns> Returns TRUE if the client is connected. </returns>
        private async Task<bool> CheckIfIsConnectedAsync()
        {
            if (!adsClient.IsConnected)
            {
                StateHardware = AdsState.Invalid;
                StateSoftware = AdsState.Invalid;
            }
            else
            {
                Task<bool> taskSoftware = SoftwareOfAdsClientIsRunningAsync();
                Task<bool> taskHardware = HardwareOfAdsClientIsRunningAsync();
                await Task.WhenAll(taskSoftware, taskHardware);
            }
            IsConnected = StateHardware == AdsState.Run && StateSoftware == AdsState.Run && adsClient.IsConnected;
            return IsConnected;
        }

        /// <summary>
        /// Releases all resources used by this instance. It is recommended to use <see cref="DisconnectAsync"/>
        /// before using <see cref="DisposeAsync()"/>.
        /// </summary>
        /// <param name="disposing"> Disposes this instance if TRUE </param>
        protected async ValueTask DisposeAsync(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                logger.LogDebug("Disposing AdsConnectionManager.");
                timer.Dispose();
                await tokenSource!.CancelAsync();
                tokenSource.Dispose();
                connectLock.Dispose();
                IsActive = false;
                if (adsClient is IAdsDisposableConnection adsDisposable && !adsDisposable.IsDisposed)
                {
                    if (adsClient.IsConnected)
                    {
                        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
                        await adsDisposable.DisconnectAsync(cts.Token);
                    }
                    adsDisposable.Dispose();
                }
                if (adsClientHardwareState is IAdsDisposableConnection adsClientHardwareDisposable)
                {
                    adsClientHardwareDisposable.Dispose();
                }
                if (adsClientSoftwareState is IAdsDisposableConnection adsClientSoftwareDisposable)
                {
                    adsClientSoftwareDisposable.Dispose();
                }
            }
            disposed = true;
        }
        #endregion
    }
}