using AdsSync.Exceptions;
using CommunityToolkit.Mvvm.ComponentModel;
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
    /// The class manages the ADS connection, variable handles, notification handles, synchronization, and the dedicated thread used for thread-safe ADS operations.
    /// Call <see cref="ActivateSync"/> to start the data exchange and <see cref="StopSyncAsync"/> to stop it. The instance must be disposed when it is no longer needed.
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
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region  constructors
        /// <summary>
        /// Constructor (You must use <see cref="TryUpdateAmsAddress"/> for setting an AMS address before starting the sync)
        /// </summary>
        /// <param name="dataToAdsClient"> The data to be sent to the ADS client </param>
        /// <param name="dataFromAdsClient"> The data to be read from the ADS client </param>
        /// <param name="structNameDataToClient"> The variable name in the ADS client to which the data is written </param>
        /// <param name="structNameDataFromClient"> The variable name in the ADS client from which the data is read </param>
        /// <exception cref="FormatException"> Thrown when the specified data format is invalid </exception>
        public AdsSyncClient(INotifyPropertyChanged dataToAdsClient,
                             INotifyPropertyChanged dataFromAdsClient,
                             string structNameDataToClient,
                             string structNameDataFromClient) :
            this(new AdsClient() { Timeout = 500 },
                 AmsAddress.Empty,
                 dataToAdsClient,
                 dataFromAdsClient,
                 structNameDataToClient,
                 structNameDataFromClient)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="amsAddress"> The AMS address of the ADS client </param>
        /// <param name="dataToAdsClient"> The data to be sent to the ADS client </param>
        /// <param name="dataFromAdsClient"> The data to be read from the ADS client </param>
        /// <param name="structNameDataToClient"> The variable name in the ADS client to which the data is written </param>
        /// <param name="structNameDataFromClient"> The variable name in the ADS client from which the data is read </param>
        /// <exception cref="FormatException"> Thrown when the specified data format is invalid </exception>
        public AdsSyncClient(AmsAddress amsAddress,
                             INotifyPropertyChanged dataToAdsClient,
                             INotifyPropertyChanged dataFromAdsClient,
                             string structNameDataToClient,
                             string structNameDataFromClient) :
            this(new AdsClient() { Timeout = 500 },
                 amsAddress,
                 dataToAdsClient,
                 dataFromAdsClient,
                 structNameDataToClient,
                 structNameDataFromClient)
        { }

        /// <summary>
        /// Constructor (intended primarily for unit testing)
        /// </summary>
        /// <param name="adsClient"> The ADS client </param>
        /// <param name="amsAddress"> The AMS address of the ADS client </param>
        /// <param name="dataToAdsClient"> The data to be sent to the ADS client </param>
        /// <param name="dataFromAdsClient"> The data to be read from the ADS client </param>
        /// <param name="structNameDataToClient"> The variable name in the ADS client to which the data is written </param>
        /// <param name="structNameDataFromClient"> The variable name in the ADS client from which the data is read </param>
        /// <exception cref="FormatException"> Thrown when the specified data format is invalid </exception>
        public AdsSyncClient(IAdsConnectAddress adsClient,
                             AmsAddress amsAddress,
                             INotifyPropertyChanged dataToAdsClient,
                             INotifyPropertyChanged dataFromAdsClient,
                             string structNameDataToClient,
                             string structNameDataFromClient)
        {
            adsDataSynchronizer = new AdsDataSynchronizer(adsClient,
                                                          dataToAdsClient,
                                                          dataFromAdsClient,
                                                          structNameDataToClient,
                                                          structNameDataFromClient);
            adsDataSynchronizer.CommunicationError += OnCommunicationError;
            connectionManager = new AdsConnectionManager(adsClient, amsAddress);
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

        #region  public methods and tasks
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Activates the data exchange with the ads client
        /// </summary>
        /// <returns> Returns TRUE if connected. Return FALSE otherwise, but will try to connect cyclically.
        /// Check "AdsSyncClient.ConnectionManager.IsConnected" for the current connection status. Use
        /// "StopSyncAsync" if you want to stop the cyclic communication. </returns>
        public async Task<bool> ActivateSync()
        {
            if (connectionManager.IsConnected)
            {
                return true;
            }
            return await connectionManager.ConnectAsync() && connectionManager.IsConnected;
        }

        /// <summary>
        /// Activates the data exchange with the ads client with a new AMD address
        /// </summary>
        /// <returns> Returns TRUE if connected. Return FALSE otherwise, but will try to connect cyclically.
        /// Check "AdsSyncClient.ConnectionManager.IsConnected" for the current connection status. Use
        /// "StopSyncAsync" if you want to stop the cyclic communication. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when the new Amd Address is null. </exception>
        public async Task<bool> ActivateSync(AmsAddress amsAddress)
        {
            ArgumentNullException.ThrowIfNull(amsAddress);
            if (connectionManager.IsConnected)
            {
                return true;
            }
            return connectionManager.TryUpdateAmsAddress(amsAddress) && await connectionManager.ConnectAsync() && connectionManager.IsConnected;
        }

        /// <summary>
        /// Stops the data exchange with the ads client
        /// </summary>
        /// <returns> Returns TRUE if the connection got stopped. Returns FALSE if a stop 
        /// command was already active or if there was no communication at all. </returns>
        public async Task<bool> StopSyncAsync()
        {
            await connectionManager.DisconnectAsync();
            return !connectionManager.IsConnected;
        }

        /// <summary>
        /// Tries to update the AMS address of the ADS client. This can only be done if the client is not connected.
        /// </summary>
        /// <param name="newAmsAddress"> The new AMS address to update </param>
        /// <returns> Returns TRUE if the AMS address was updated. </returns>
        /// <exception cref="ArgumentNullException"> Thrown when the new Amd Address is null. </exception>
        public bool TryUpdateAmsAddress(AmsAddress newAmsAddress)
        {
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
        /// Fires an event if the synchonizer has a communication error
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
        /// <param name="disposing"> Disposes this istance if TRUE </param>
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
            }
            NoLongerExecuteOnPropertyChanged(nameof(IsConnected), connectionManager, TakeOverIsConnectedState);
            NoLongerExecuteOnPropertyChanged(nameof(StateHardware), connectionManager, TakeOverStateHardware);
            NoLongerExecuteOnPropertyChanged(nameof(StateSoftware), connectionManager, TakeOverStateSoftware);
            disposed = true;
        }

        /// <summary>
        /// Sets IsActive to the connection manager's current IsActive value.
        /// </summary>
        private void TakeOverIsActiveState(object? s, EventArgs? e) => IsActive = connectionManager.IsActive;

        /// <summary>
        /// Sets IsConnected to the connection manager's current IsConnected value.
        /// </summary>
        private void TakeOverIsConnectedState(object? s, EventArgs? e) => IsConnected = connectionManager.IsConnected;

        /// <summary>
        /// Sets StateHardware to the connection manager's current StateHardware value.
        /// </summary>
        private void TakeOverStateHardware(object? s, EventArgs? e) => StateHardware = connectionManager.StateHardware;

        /// <summary>
        /// Sets StateSoftware to the connection manager's current StateSoftware value.
        /// </summary>
        private void TakeOverStateSoftware(object? s, EventArgs? e) => StateSoftware = connectionManager.StateSoftware;

        /// <summary>
        /// Ermöglicht das Ausführen eines Events bei Änderung eines Werts einer Eigenschaft
        /// </summary>
        /// <param name="nameofProperty"> Der Name der Eigenschaft (anzugeben mit "nameof(x)") </param>
        /// <param name="objectInstance"> Das Objekt, dass die Eigenschaft beinhaltet </param>
        /// <param name="handler"> Die Methode, die bei Eigenschaftsänderung ausgeführt werden soll </param>
        private static void ExecuteOnPropertyChanged(string nameofProperty, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(nameofProperty, false)!;
            property.AddValueChanged(objectInstance, handler);
        }

        /// <summary>
        /// Beendet das Ausführen eines Events bei Änderung eines Werts einer Eigenschaft
        /// </summary>
        /// <param name="nameofProperty"> Der Name der Eigenschaft (anzugeben mit "nameof(x)") </param>
        /// <param name="objectInstance"> Das Objekt, dass die Eigenschaft beinhaltet </param>
        /// <param name="handler"> Die Methode, die bei Eigenschaftsänderung ausgeführt werden soll </param>
        private static void NoLongerExecuteOnPropertyChanged(string nameofProperty, object objectInstance, EventHandler handler)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(objectInstance);
            PropertyDescriptor property = properties.Find(nameofProperty, false)!;
            property.RemoveValueChanged(objectInstance, handler);
        }
        #endregion
    }
}