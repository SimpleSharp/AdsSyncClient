using AdsSync;
using AdsSyncClientAvaloniaDemo.Models;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TwinCAT.Ads;
using TwinCAT.Router;

namespace AdsSyncClientAvaloniaDemo.ViewModels
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
        [ObservableProperty] private ObservableCollection<string> logMessages = [];

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
        /// <summary> The TCP port used by ADS routers </summary>
        private const int RouterPort = 48898;
        #endregion

        #region fields & events
        /// <summary> Event raised when an invalid net id was entered </summary>
        public event EventHandler<EventArgs>? InvalidNetIdEntered;
        /// <summary> Event raised when an invalid port was entered </summary>
        public event EventHandler<EventArgs>? InvalidPortEntered;
        private readonly ILoggerFactory loggerFactory;
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
            LogSink sink = new(msg => Dispatcher.UIThread.Post(() => {
                LogMessages.Add(msg);
            }));
            loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddProvider(new UiLoggerProvider(sink))
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss.fff ";
                    });
            });
            AdsSyncDefinition syncDefinition = new(DataToClient, DataFromClient, structNameDataToClient, structNameDataFromClient);
            if (EnsureRouterPortIsFree())
            {
                RouterConfiguration config = new(AmsNetId.Parse("10.44.223.102.1.1"),
                                                 IPAddress.Parse("10.44.223.123"),
                                                 new AmsAddress("10.44.223.123.1.1", 851));
                SyncClient = new(syncDefinition, config, loggerFactory);
            }
            else
            {
                SyncClient = new(syncDefinition, new AmsAddress("192.168.20.34.1.1", 851), loggerFactory);
            }
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
        /// Verifies that the ADS router port is not already in use on this machine.
        /// </summary>
        /// <exception cref="IOException"> Thrown when the ADS router port 48898 is already in use. This usually
        /// means that a TwinCAT router is running locally, in which case an in-process router is not required. </exception>
        private static bool EnsureRouterPortIsFree()
        {
            IPEndPoint[] activeListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return activeListeners.All(listener => listener.Port != RouterPort);
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

            if (SyncClient.UsesInProcessRouter)
            {
                _ = await SyncClient.ActivateSync();
            }
            else
            {
                _ = await SyncClient.ActivateSync(new(NetId, port));
            }
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
