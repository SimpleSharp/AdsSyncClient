using AdsSync.Exceptions;
using AdsSync.Mapping;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TwinCAT;
using TwinCAT.Ads;

namespace AdsSync
{
    /// <summary>
    /// Handles data mapping and synchronization between .NET objects and ADS variables.
    /// </summary>
    internal partial class AdsDataSynchronizer : ObservableObject, IAsyncDisposable
    {
        #region properties
        /// <summary> Communication is initialized </summary>
        [ObservableProperty] private bool isCommunicationInitialized;
        #endregion

        #region fields & events
        /// <summary> Event raised when an exception is thrown while reading / writing </summary>
        public event EventHandler<CommunicationErrorEventArgs>? CommunicationError;
        /// <summary> The ADS client </summary>
        private readonly IAdsConnectAddress adsClient;
        /// <summary> Contains the data to be sent and all related information </summary>
        private readonly AdsWriteMapper adsDataMapperWrite;
        /// <summary> Contains the data to be read and all related information </summary>
        private readonly AdsReadMapper adsDataMapperRead;
        /// <summary> A list containing all collection handlers for disposing purposes </summary>
        private readonly List<(object Collection, NotifyCollectionChangedEventHandler Handler)> collectionHandlers = [];
        /// <summary> Cancellation token source for terminating the communication </summary>
        private readonly CancellationTokenSource tokenSource = new();
        /// <summary> Serializes collection updates to prevent race conditions (Read vs Write) </summary>
        private readonly SemaphoreSlim _collectionUpdateLock = new(1, 1);
        /// <summary> Suppresses Write events during ADS read operations to prevent recursive updates </summary>
        private bool isUpdatingFromAds;
        /// <summary> Indicates whether the object has been disposed </summary>
        private bool disposed;
        #endregion

        #region constants
        /// <summary> Defines the default string length in TwinCAT </summary>
        private const int DefaultTwinCatStringByteLength = 80;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="adsClient"> The ADS client </param>
        /// <param name="definition"> The definition of the data exchange </param>
        public AdsDataSynchronizer(IAdsConnectAddress adsClient, AdsSyncDefinition definition)
        {
            this.adsClient = adsClient;
            adsDataMapperRead = new(definition.DataFromAdsClient, definition.StructNameDataFromClient);
            adsDataMapperWrite = new(definition.DataToAdsClient, definition.StructNameDataToClient);
        }
        #endregion

        #region public methods
        /// <summary>
        /// Initializes the new communication (variable and notification handles, event subscriptions etc.)
        /// </summary>
        public async Task InitializeCommunicationAsync()
        {
            //Returns if its just a reconnect
            if (IsCommunicationInitialized)
            {
                return;
            }
            //create all variable an notification handles
            await adsDataMapperRead.GenerateVariableHandlesAsync(adsClient);
            await adsDataMapperRead.GenerateNotificationHandlesAsync();
            await adsDataMapperWrite.GenerateVariableHandlesAsync(adsClient);
            await InitializeStructureSyncAsync();
            if (adsDataMapperRead.NotificationHandles.Length > 0)
            {
                adsClient.AdsNotification += Read;
            }
            //create all events to start reading and writing
            foreach (PropertyInfo propertyInfo in adsDataMapperWrite.Data.GetType().GetProperties())
            {
                if (propertyInfo.PropertyType.IsObservableCollection())
                {
                    object collection = propertyInfo.GetValue(adsDataMapperWrite.Data)!;
                    var eventInfo = typeof(ObservableCollection<>).MakeGenericType(propertyInfo.PropertyType.GenericTypeArguments[0]).GetEvent("CollectionChanged")!;
                    NotifyCollectionChangedEventHandler handler = (sender, args) =>
                    {
                        if (!isUpdatingFromAds && !tokenSource.Token.IsCancellationRequested)
                        {
                            WriteObservableCollection(propertyInfo.Name);
                        }
                    };
                    eventInfo.AddEventHandler(collection, handler);
                    collectionHandlers.Add((collection, handler));
                }
                else
                {
                    propertyInfo.ExecuteOnPropertyChanged(adsDataMapperWrite.Data, WriteProperty);
                }
            }
            IsCommunicationInitialized = true;
        }

        /// <summary>
        /// Stops the communication (variable and notification handles, event subscriptions etc.)
        /// </summary>
        public async Task StopCommunicationAsync()
        {
            if (!IsCommunicationInitialized)
            {
                return;
            }
            //deletes all variable an notification handles
            if (adsDataMapperRead.NotificationHandles.Length > 0)
            {
                adsClient.AdsNotification -= Read;
            }
            if (adsClient.IsConnected)
            {
                await adsDataMapperRead.DeleteAllVariableHandlesAsync();
                await adsDataMapperRead.DeleteAllNotificationsHandlesAsync();
                await adsDataMapperWrite.DeleteAllVariableHandlesAsync();
            }
            //Unregisters all observable collection events to stop reading and writing
            if (collectionHandlers.Count > 0)
            {
                var handlersToUnregister = collectionHandlers.ToArray();
                foreach (var (collection, handler) in handlersToUnregister)
                {
                    EventInfo? eventInfo = collection.GetType().GetEvent("CollectionChanged");
                    eventInfo?.RemoveEventHandler(collection, handler);
                }
                collectionHandlers.Clear();
            }
            //Unregisters all property events to stop reading and writing
            foreach (PropertyInfo propertyInfo in adsDataMapperWrite.Data.GetType().GetProperties())
            {
                if (!propertyInfo.PropertyType.IsObservableCollection())
                {
                    propertyInfo.NoLongerExecuteOnPropertyChanged(adsDataMapperWrite.Data, WriteProperty);
                }
            }
            IsCommunicationInitialized = false;
        }

        /// <summary>
        /// Releases all resources used by this instance. It is recommended to call the <see cref="StopCommunicationAsync()"/>
        /// task before disposing of this instance.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Thread-safe collection synchronization without breaking bindings; clears and repopulates ObservableCollection using Clear/Add pattern
        /// </summary>
        private void SyncCollectionContents<T>(ObservableCollection<T> target, Array source)
        {
            isUpdatingFromAds = true;
            try
            {
                target.Clear();
                foreach (var item in source.OfType<T>())
                {
                    target.Add(item);
                }
            }
            finally
            {
                isUpdatingFromAds = false;
            }
        }

        /// <summary>
        /// Executes a complete data exchange (read and write) to synchronize the data between the clients
        /// </summary>
        private async Task InitializeStructureSyncAsync()
        {
            //Writes all data into the client
            Marshalling.UpdateFieldClassValuesFromProperties(adsDataMapperWrite.DataAsMarshalledClass, adsDataMapperWrite.Data);
            for (int i1 = 0; i1 < adsDataMapperWrite.VariableHandles.Length; i1++)
            {
                adsClient.WriteAny(adsDataMapperWrite.VariableHandles[i1], adsDataMapperWrite.FieldInfos[i1].GetValue(adsDataMapperWrite.DataAsMarshalledClass)!);
            }
            //Reads all data from the client
            List<Exception> exceptions = [];
            for (int i1 = 0; i1 < adsDataMapperRead.VariableHandles.Length; i1++)
            {
                try
                {
                    object readValue = await ReadValueAsync(adsDataMapperRead.VariableHandles[i1], adsDataMapperRead.Types[i1]);
                    if (adsDataMapperRead.UnmanagedTypes[i1] == UnmanagedType.ByValArray)
                    {
                        //Converts an array to ObservableCollection
                        readValue = ClientArrayToObservableCollection(readValue, adsDataMapperRead.PropertyInfos[i1]);
                    }
                    adsDataMapperRead.PropertyInfos[i1].SetValue(adsDataMapperRead.Data, readValue);

                }
                catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException or ClientNotConnectedException ||
                                           ex is AdsErrorException adsEx && (adsEx.ErrorCode == AdsErrorCode.ClientSyncTimeOut ||
                                                                             adsEx.ErrorCode == AdsErrorCode.DeviceSymbolNotFound ||
                                                                             adsEx.ErrorCode == AdsErrorCode.TargetPortNotFound))
                {
                    //Communication terminated
                }
                catch
                {
                    exceptions.Add(new("Error while reading value of handle: " + adsDataMapperRead.VariableHandles[i1].ToString()));
                }
            }
        }

        /// <summary>
        /// Converts an array from the client to an ObservableCollection
        /// </summary>
        /// <param name="valueFromClient"> The read array from the client </param>
        /// <param name="propertyInfo"> The PropertyInfo of the ObservableCollection </param>
        /// <returns> Returns the ObservableCollection. </returns>
        private IList ClientArrayToObservableCollection(object valueFromClient, PropertyInfo propertyInfo)
        {
            Array sourceArray = (Array)valueFromClient.GetType().GetFields()[0].GetValue(valueFromClient)!;
            IList targetList = (IList)propertyInfo.GetValue(adsDataMapperRead.Data)!;
            if (targetList is ObservableCollection<object> obsCol)
            {
                SyncCollectionContents(obsCol, sourceArray);
            }
            else
            {
                for (int i = 0; i < Math.Min(sourceArray.Length, targetList.Count); i++)
                {
                    targetList[i] = sourceArray.GetValue(i);
                }
                while (targetList.Count > sourceArray.Length)
                {
                    targetList.RemoveAt(targetList.Count - 1);
                }
                for (int i = targetList.Count; i < sourceArray.Length; i++)
                {
                    targetList.Add(sourceArray.GetValue(i));
                }
            }

            return targetList;
        }

        /// <summary>
        /// Releases all resources used by this instance. It is recommended to call the <see cref="StopCommunicationAsync()"/>
        /// task before disposing of this instance.
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
                await tokenSource.CancelAsync();
                tokenSource.Dispose();
                _collectionUpdateLock.Dispose();
                if (adsClient.IsConnected && IsCommunicationInitialized)
                {
                     await StopCommunicationAsync();
                }
                await adsDataMapperRead.DisposeAsync();
                await adsDataMapperWrite.DisposeAsync();
            }
            disposed = true;
        }

        /// <summary>
        /// Writes new data into the ADS client if an ObservableCollection has changed
        /// </summary>
        private void WriteObservableCollection(string? collectionName)
        {
            _ = WriteObservableCollectionAsync(collectionName!);
        }

        /// <summary>
        /// Writes new data into the ADS client if an ObservableCollection has changed
        /// </summary>
        private async Task WriteObservableCollectionAsync(string collectionName)
        {
            try
            {
                await _collectionUpdateLock.WaitAsync(tokenSource.Token);
                try
                {
                    if (await WriteValueAsync(collectionName) is AdsErrorCode errorCode && errorCode != AdsErrorCode.NoError)
                    {
                        throw new AdsErrorException("WriteAnyAsync failed. ", errorCode);
                    }
                }
                finally
                {
                    _collectionUpdateLock.Release();
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException or ClientNotConnectedException ||
                                       ex is AdsErrorException adsEx && (adsEx.ErrorCode == AdsErrorCode.ClientSyncTimeOut ||
                                                                         adsEx.ErrorCode == AdsErrorCode.DeviceSymbolNotFound ||
                                                                         adsEx.ErrorCode == AdsErrorCode.TargetPortNotFound))
            {
                //Communication terminated
            }
            catch (Exception ex)
            {
                CommunicationError?.Invoke(this, new CommunicationErrorEventArgs(ex, collectionName, false, true));
            }
        }

        /// <summary>
        /// Write a value into the ads client
        /// </summary>
        /// <param name="name"> The name of the variable </param>
        /// <returns> Returns TRUE if successfully written. </returns>
        public async Task<AdsErrorCode> WriteValueAsync(string name)
        {
            if (adsClient.IsConnected &&
                IsCommunicationInitialized &&
                !string.IsNullOrEmpty(name) &&
                Array.FindIndex(adsDataMapperWrite.FieldInfos, fi => fi.Name == name) is int index &&
                index >= 0)
            {
                object value = adsDataMapperWrite.Data.GetType().GetProperty(name)!.GetValue(adsDataMapperWrite.Data)!;
                if (value is IList list)
                {
                    //Converts from ObservableCollection to array
                    Array array = (Array)adsDataMapperWrite.DataAsMarshalledClass.GetType().GetField(name)!.GetValue(adsDataMapperWrite.DataAsMarshalledClass)!;
                    for (int i2 = 0; i2 < list.Count; i2++)
                    {
                        array.SetValue(list[i2], i2);
                    }
                    value = array;
                }
                adsDataMapperWrite.FieldInfos[index].SetValue(adsDataMapperWrite.DataAsMarshalledClass, value);
                ResultWrite result = await adsClient.WriteAnyAsync(adsDataMapperWrite.VariableHandles[index], value, tokenSource.Token);
                return result.ErrorCode;
            }
            return AdsErrorCode.NoError;
        }

        /// <summary>
        /// Writes new data into the ADS client if a value of a property has changed
        /// </summary>
        private void WriteProperty(object? sender, EventArgs? e) => _ = WritePropertyAsync(e!);

        /// <summary>
        /// Writes new data into the ADS client if a value of a property has changed
        /// </summary>
        private async Task WritePropertyAsync(EventArgs e)
        {
            try
            {
                if (e is PropertyChangedEventArgs pe &&
                    !string.IsNullOrEmpty(pe.PropertyName) &&
                    await WriteValueAsync(pe.PropertyName) is AdsErrorCode errorCode && errorCode != AdsErrorCode.NoError)
                {
                    throw new AdsErrorException("WriteAnyAsync failed. ", errorCode);
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException or ClientNotConnectedException ||
                                       ex is AdsErrorException adsEx && (adsEx.ErrorCode == AdsErrorCode.ClientSyncTimeOut ||
                                                                         adsEx.ErrorCode == AdsErrorCode.DeviceSymbolNotFound ||
                                                                         adsEx.ErrorCode == AdsErrorCode.TargetPortNotFound))
            {
                //Communication terminated
            }
            catch (Exception ex)
            {
                if (e is PropertyChangedEventArgs pe && !string.IsNullOrEmpty(pe.PropertyName))
                {
                    CommunicationError?.Invoke(this, new CommunicationErrorEventArgs(ex, pe.PropertyName, false, true));
                }
                else
                {
                    CommunicationError?.Invoke(this, new CommunicationErrorEventArgs(ex, "Name not found", false, true));
                }
            }
        }

        /// <summary>
        /// Reads data from the ADS client if a value has changed
        /// </summary>
        private void Read(object? sender, AdsNotificationEventArgs? e)
        {
            if (e is not null && Array.IndexOf(adsDataMapperRead.NotificationHandles, e.Handle) is int i && i >= 0)
            {
                _ = ReadAsync(i);
            }
        }

        /// <summary>
        /// Reads data from the ADS client if a value has changed
        /// </summary>
        private async Task ReadAsync(int index)
        {
            try
            {
                await _collectionUpdateLock.WaitAsync(tokenSource.Token);
                try
                {
                    object readValue = await ReadValueAsync(adsDataMapperRead.VariableHandles[index], adsDataMapperRead.Types[index]);
                    if (adsDataMapperRead.UnmanagedTypes[index] == UnmanagedType.ByValArray)
                    {
                        readValue = ClientArrayToObservableCollection(readValue, adsDataMapperRead.PropertyInfos[index]
                        );
                    }
                    adsDataMapperRead.PropertyInfos[index].SetValue(adsDataMapperRead.Data, readValue);
                }
                finally
                {
                    _collectionUpdateLock.Release();
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException or ObjectDisposedException or ClientNotConnectedException ||
                                       ex is AdsErrorException adsEx && (adsEx.ErrorCode == AdsErrorCode.ClientSyncTimeOut ||
                                                                         adsEx.ErrorCode == AdsErrorCode.DeviceSymbolNotFound ||
                                                                         adsEx.ErrorCode == AdsErrorCode.TargetPortNotFound))
            {
                //Communication terminated
            }
            catch (Exception ex)
            {
                CommunicationError?.Invoke(this, new CommunicationErrorEventArgs(ex, adsDataMapperRead.PropertyInfos[index].Name, true, false));
            }
        }

        /// <summary>
        /// Read a value from the ads client
        /// </summary>
        /// <param name="variableHandle"> The variable handle </param>
        /// <param name="type"> The type of the value </param>
        /// <returns> Return the read value. </returns>
        /// <exception cref="AdsErrorException"> Throws an AdsErrorException when reading failed. </exception>
        private async Task<object> ReadValueAsync(uint variableHandle, Type type)
        {
            ResultAnyValue value;
            if (type == typeof(string))
            {
                value = await adsClient.ReadAnyStringAsync(variableHandle, DefaultTwinCatStringByteLength, Encoding.UTF8, tokenSource.Token);
            }
            else
            {
                value = await adsClient.ReadAnyAsync(variableHandle, type, tokenSource.Token);
            }
            if (value.Succeeded)
            {
                return value.Value!;
            }
            else
            {
                throw new AdsErrorException("Error while reading value. ", value.ErrorCode);
            }
        }
        #endregion
    }
}
