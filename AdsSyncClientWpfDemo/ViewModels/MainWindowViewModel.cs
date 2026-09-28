using AdsSync;
using AdsSyncClientWpfDemo.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TwinCAT.Ads;

namespace AdsSyncClientWpfDemo.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject, IAsyncDisposable
    {
        #region properties
        /// <summary> Read data from the ads client </summary>
        [ObservableProperty] private ObservableCollection<PropertyItem> dataFromClientRows = [];
        /// <summary> Write data to the ads client </summary>
        [ObservableProperty] private ObservableCollection<PropertyItem> dataToClientRows = [];
        /// <summary> ADS client used for communication </summary>
        [ObservableProperty] private AdsSyncClient syncClient;
        /// <summary> The net id of the client (127.0.0.1.1.1 is for local TwinCAT applications) </summary>
        [ObservableProperty] private string netId = "127.0.0.1.1.1";
        /// <summary> The port of the client (851 is the default of plc applications in TwinCAT) </summary>
        [ObservableProperty] private string port = ((uint)AmsPort.PlcRuntime_851).ToString();
        /// <summary> Data to be sent to the client </summary>
        [ObservableProperty] private DataToClient dataToClient = new();
        /// <summary> Data to be read from the client </summary>
        [ObservableProperty] private DataFromClient dataFromClient = new();

        #endregion

        #region commands
        /// <summary> Command for starting the sync with the ads client </summary>
        public RelayCommand StartSyncCommand { get; }
        /// <summary> Command for stopping the sync with the ads client </summary>
        public RelayCommand StopSyncCommand { get; }
        #endregion

        #region constants
        /// <summary> Name of the client structure containing data to be written </summary>
        private const string structNameDataFromClient = "GVL.DataToHmi";
        /// <summary> Name of the client structure containing data to be read </summary>
        private const string structNameDataToClient = "GVL.DataFromHmi";
        #endregion

        #region fields & events
        /// <summary> Event raised when an invalid net id was entered </summary>
        public event EventHandler<EventArgs>? InvalidNetIdEntered;
        /// <summary> Event raised when an invalid port was entered </summary>
        public event EventHandler<EventArgs>? InvalidPortEntered;
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowViewModel()
        {
            StartSyncCommand = new RelayCommand(async () => await StartSyncAsync());
            StopSyncCommand = new RelayCommand(async () => await StopSyncAsync());
            SyncClient = new(DataToClient, DataFromClient, structNameDataToClient, structNameDataFromClient);
            _ = SetNewValuesAsync();

            CreatePropertyRows(DataToClient, DataToClientRows);
            CreatePropertyRows(DataFromClient, DataFromClientRows);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        private static void CreatePropertyRows(object source, ObservableCollection<PropertyItem> target)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
            {
                target.Add(new PropertyItem(source, property));
            }
        }

        /// <summary>
        /// Starts the sync with the ads client
        /// </summary>
        /// <returns></returns>
        private async Task StartSyncAsync()
        {
            if (SyncClient.IsConnected)
            {
                return;
            }
            //CHeck if the port is valid
            bool isPortValid = int.TryParse(Port, out int port) && port < (int)AmsPortRange.PORT_LAST;
            if (!isPortValid)
            {
                InvalidPortEntered?.Invoke(Port, EventArgs.Empty);
                return;
            }
            //CHeck if the net ID is valid
            AmsNetId? netId = AmsNetId.Empty;
            bool isNetIdValid = !string.IsNullOrEmpty(NetId) && AmsNetId.TryParse(NetId, out netId);
            if (!isNetIdValid)
            {
                InvalidNetIdEntered?.Invoke(NetId, EventArgs.Empty);
                return;
            }
            _ = await SyncClient.ActivateSync(new(NetId, port));
        }

        /// <summary>
        /// Starts the sync with the ads client
        /// </summary>
        private async Task StopSyncAsync()
        {
            await SyncClient.StopSyncAsync();
        }

        /// <summary>
        /// Manipulates the data sent to the ads client for testing purposes
        /// </summary>
        private async Task SetNewValuesAsync()
        {
            while (true)
            {
                DataToClient.LifeBit = !DataToClient.LifeBit;
                if (DataToClient.TestString == "Test1")
                {
                    DataToClient.TestString = "Test2";

                }
                else
                {
                    DataToClient.TestString = "Test1";
                }
                if (DataToClient.TestArray[0] == 1)
                {
                    for (int i1 = 0; i1 < 10; i1++)
                    {
                        if (i1 % 2 != 0)
                        {
                            DataToClient.TestArray[i1] = 1;
                        }
                        else
                        {
                            DataToClient.TestArray[i1] = 0;
                        }
                    }
                }
                else
                {
                    for (int i1 = 0; i1 < 10; i1++)
                    {
                        if (i1 % 2 == 0)
                        {
                            DataToClient.TestArray[i1] = 1;
                        }
                        else
                        {
                            DataToClient.TestArray[i1] = 0;
                        }
                    }
                }
                if (DataToClient.Testdouble < 10.0)
                {
                    DataToClient.Testdouble *= 25.0;
                }
                else
                {
                    DataToClient.Testdouble = Math.Round(DataToClient.Testdouble / 1.295, 3);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing"> Disposes this istance if TRUE </param>
        protected async ValueTask DisposeAsync(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                if (SyncClient.IsConnected)
                {
                    await SyncClient.StopSyncAsync();
                }
                await SyncClient.DisposeAsync();
            }
            disposed = true;
        }
        #endregion
    }
}
