using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TwinCAT;
using TwinCAT.Ads;
using TwinCAT.Ads.TypeSystem;
using TwinCAT.TypeSystem;

namespace UnitTest.Mocks
{
    internal class MockAdsClient : IAdsConnectAddress
    {
        public ChannelProtocol ChannelProtocol => throw new NotImplementedException();

        public ChannelPortType ChannelPortType => throw new NotImplementedException();

        public AmsAddress ClientAddress => throw new NotImplementedException();

        public bool IsLocal => throw new NotImplementedException();

        public AmsAddress Address => throw new NotImplementedException();

        public int Id => throw new NotImplementedException();

        public bool IsConnected { get; set; }

        public int Timeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ISession? Session => throw new NotImplementedException();

        public Encoding DefaultValueEncoding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Encoding SymbolEncoding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ILoggerFactory? LoggerFactory => throw new NotImplementedException();

        public ILogger? Logger => throw new NotImplementedException();

        public IConfiguration? Configuration => throw new NotImplementedException();

        public AccessCapabilities Capabilities { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ConnectionState ConnectionState => throw new NotImplementedException();

        public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;
        public event EventHandler<AdsSumNotificationEventArgs> AdsSumNotification;
        public event EventHandler<AdsNotificationEventArgs>? AdsNotification;
        public event EventHandler<AdsNotificationErrorEventArgs>? AdsNotificationError;
        public event EventHandler<AdsNotificationExEventArgs>? AdsNotificationEx;
        public event EventHandler<AdsStateChangedEventArgs> AdsStateChanged;
        public event EventHandler<AdsSymbolVersionChangedEventArgs> AdsSymbolVersionChanged;

        public uint AddDeviceNotification(uint indexGroup, uint indexOffset, int dataSize, NotificationSettings settings, object? userData)
        {
            throw new NotImplementedException();
        }

        public uint AddDeviceNotification(string symbolPath, int dataSize, NotificationSettings settings, object? userData)
        {
            throw new NotImplementedException();
        }

        public Task<ResultHandle> AddDeviceNotificationAsync(string symbolPath, int dataSize, NotificationSettings settings, object? userData, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultHandle> AddDeviceNotificationAsync(uint indexGroup, uint indexOffset, int dataSize, NotificationSettings settings, object? userData, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public uint AddDeviceNotificationEx(uint indexGroup, uint indexOffset, NotificationSettings settings, object? userData, Type type, int[] args)
        {
            throw new NotImplementedException();
        }

        public uint AddDeviceNotificationEx(string symbolPath, NotificationSettings settings, object? userData, Type type)
        {
            throw new NotImplementedException();
        }

        public uint AddDeviceNotificationEx(string symbolPath, NotificationSettings settings, object? userData, Type type, int[]? args)
        {
            throw new NotImplementedException();
        }

        public uint AddDeviceNotificationEx(uint indexGroup, uint indexOffset, NotificationSettings settings, object? userData, Type type)
        {
            throw new NotImplementedException();
        }

        public Task<ResultHandle> AddDeviceNotificationExAsync(string symbolPath, NotificationSettings settings, object? userData, Type type, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultHandle> AddDeviceNotificationExAsync(uint indexGroup, uint indexOffset, NotificationSettings settings, object? userData, Type type, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void CleanupSymbolTable()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Connect(AmsAddress address)
        {
            throw new NotImplementedException();
        }

        public void Connect(AmsNetId netId, int port)
        {
            throw new NotImplementedException();
        }

        public void Connect(int port)
        {
            throw new NotImplementedException();
        }

        public void Connect(string netId, int port)
        {
            throw new NotImplementedException();
        }

        public bool Connect()
        {
            throw new NotImplementedException();
        }

        public Task ConnectAndWaitAsync(AmsAddress address, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task ConnectAndWaitAsync(CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public async Task ConnectAsync(AmsAddress address, CancellationToken cancel)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            IsConnected = true;
            return;
        }

        public Task ConnectAsync(AmsNetId netId, int port, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task ConnectAsync(int port, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ConnectAsync(CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public uint CreateVariableHandle(string symbolPath)
        {
            throw new NotImplementedException();
        }

        public Task<ResultHandle> CreateVariableHandleAsync(string symbolPath, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void DeleteDeviceNotification(uint notificationHandle)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> DeleteDeviceNotificationAsync(uint notificationHandle, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void DeleteVariableHandle(uint variableHandle)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> DeleteVariableHandleAsync(uint variableHandle, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void DisableSumCommands(bool disable)
        {
            throw new NotImplementedException();
        }

        public bool Disconnect()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DisconnectAsync(CancellationToken cancel)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            IsConnected = false;
            return true;
        }

        public AdsErrorCode InjectError(AdsErrorCode errorCode)
        {
            throw new NotImplementedException();
        }

        public void InjectSymbolVersionChanged()
        {
            throw new NotImplementedException();
        }

        public object? InvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters)
        {
            throw new NotImplementedException();
        }

        public object? InvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, out object[]? outParameters)
        {
            throw new NotImplementedException();
        }

        public object? InvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, AnyTypeSpecifier? retSpecifier)
        {
            throw new NotImplementedException();
        }

        public object? InvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, AnyTypeSpecifier[]? outSpecifiers, AnyTypeSpecifier? retSpecifier, out object[]? outParameters)
        {
            throw new NotImplementedException();
        }

        public Task<ResultRpcMethod> InvokeRpcMethodAsync(string symbolPath, string methodName, object[]? inParameters, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultRpcMethod> InvokeRpcMethodAsync(string symbolPath, string methodName, object[]? inParameters, AnyTypeSpecifier[]? outSpecifiers, AnyTypeSpecifier? retSpecifier, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultRpcMethod> InvokeRpcMethodAsync(IRpcCallableInstance symbol, IRpcMethod method, object[]? inParameters, AnyTypeSpecifier[]? outSpecifiers, AnyTypeSpecifier? retSpecifier, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public int Read(uint variableHandle, Memory<byte> readBuffer)
        {
            throw new NotImplementedException();
        }

        public int Read(uint indexGroup, uint indexOffset, Memory<byte> readBuffer)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(uint variableHandle, Type type)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string instancePath, Type type)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(uint variableHandle)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(string instancePath)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(uint variableHandle, int[]? args)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(string instancePath, int[]? args)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(uint variableHandle, Type type, int[]? args)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(string instancePath, Type type, int[]? args)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(uint indexGroup, uint indexOffset, Type type)
        {
            throw new NotImplementedException();
        }

        public object ReadAny(uint indexGroup, uint indexOffset, Type type, int[]? args)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(uint indexGroup, uint indexOffset)
        {
            throw new NotImplementedException();
        }

        public T ReadAny<T>(uint indexGroup, uint indexOffset, int[]? args)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(uint variableHandle)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(string instancePath)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(uint variableHandle, int[]? args)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(string instancePath, int[]? args)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(uint indexGroup, uint indexOffset)
        {
            throw new NotImplementedException();
        }

        public ResultValue<T> ReadAnyAsResult<T>(uint indexGroup, uint indexOffset, int[]? args)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(uint variableHandle, Type type, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(string instancePath, Type type, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(uint variableHandle, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(string instancePath, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(uint variableHandle, Type type, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(string instancePath, Type type, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(uint variableHandle, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(string instancePath, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(uint indexGroup, uint indexOffset, Type type, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(uint indexGroup, uint indexOffset, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyAsync(uint indexGroup, uint indexOffset, Type type, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadAnyAsync<T>(uint indexGroup, uint indexOffset, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public string ReadAnyString(uint indexGroup, uint indexOffset, int len, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public string ReadAnyString(uint variableHandle, int len, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public string ReadAnyString(string instancePath, int len, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyStringAsync(uint indexGroup, uint indexOffset, int len, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyStringAsync(uint variableHandle, int len, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadAnyStringAsync(string instancePath, int len, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public ResultReadBytes ReadAsResult(uint variableHandle, int readLength)
        {
            throw new NotImplementedException();
        }

        public ResultReadBytes ReadAsResult(uint indexGroup, uint indexOffset, int readLength)
        {
            throw new NotImplementedException();
        }

        public Task<ResultRead> ReadAsync(uint variableHandle, Memory<byte> readBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadBytes> ReadAsync(uint variableHandle, int readLength, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultRead> ReadAsync(uint indexGroup, uint indexOffset, Memory<byte> buffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadBytes> ReadAsync(uint indexGroup, uint indexOffset, int readLength, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public IDataType ReadDataType(string typeName)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<IDataType>> ReadDataTypeAsync(string typeName, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public DeviceInfo ReadDeviceInfo()
        {
            throw new NotImplementedException();
        }

        public Task<ResultDeviceInfo> ReadDeviceInfoAsync(CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public StateInfo ReadState()
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadDeviceState> ReadStateAsync(CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public IAdsSymbol ReadSymbol(string instancePath)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<IAdsSymbol>> ReadSymbolAsync(string instancePath, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        [return: NotNull]
        public object ReadValue(ISymbol symbol)
        {
            throw new NotImplementedException();
        }

        [return: NotNull]
        public T ReadValue<T>(ISymbol symbol) where T : notnull
        {
            throw new NotImplementedException();
        }

        public object ReadValue(string instancePath, Type? type)
        {
            throw new NotImplementedException();
        }

        public object ReadValue(string instancePath)
        {
            throw new NotImplementedException();
        }

        public T ReadValue<T>(string instancePath) where T : notnull
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadValueAsync(ISymbol symbol, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadValueAsync<T>(ISymbol symbol, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAnyValue> ReadValueAsync(string instancePath, Type type, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultValue<T>> ReadValueAsync<T>(string instancePath, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public int ReadWrite(uint variableHandle, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public int ReadWrite(uint indexGroup, uint indexOffset, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public ResultReadWriteBytes ReadWriteAsResult(uint indexGroup, uint indexOffset, int readLength, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadWrite> ReadWriteAsync(uint variableHandle, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadWrite> ReadWriteAsync(uint indexGroup, uint indexOffset, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultReadWriteBytes> ReadWriteAsync(uint indexGroup, uint indexOffset, int readLength, ReadOnlyMemory<byte> writeBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> RegisterAdsStateChangedAsync(EventHandler<AdsStateChangedEventArgs> handler, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode RegisterSymbolVersionChanged(EventHandler<AdsSymbolVersionChangedEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> RegisterSymbolVersionChangedAsync(EventHandler<AdsSymbolVersionChangedEventArgs> handler, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryAddDeviceNotification(string symbolPath, int dataSize, NotificationSettings settings, object? userData, out uint handle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryAddDeviceNotification(uint indexGroup, uint indexOffset, int dataSize, NotificationSettings settings, object? userData, out uint handle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryAddDeviceNotificationEx(string symbolPath, NotificationSettings settings, object? userData, Type type, int[]? args, out uint handle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryAddDeviceNotificationEx(uint indexGroup, uint indexOffset, NotificationSettings settings, object? userData, Type type, int[]? args, out uint handle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryCreateVariableHandle(string symbolPath, out uint variableHandle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryDeleteDeviceNotification(uint notificationHandle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryDeleteVariableHandle(uint variableHandle)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryInvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, out object? retValue)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryInvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, out object[]? outParameters, out object? retValue)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryInvokeRpcMethod(string symbolPath, string methodName, object[]? inParameters, AnyTypeSpecifier[]? outSpecifiers, AnyTypeSpecifier? retSpecifier, out object[]? outParameters, out object? retValue)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryInvokeRpcMethod(IRpcCallableInstance symbol, IRpcMethod method, object[]? inParameters, AnyTypeSpecifier[]? outSpecifiers, AnyTypeSpecifier? retSpecifier, out object[]? outParameters, out object? retValue)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryRead(uint variableHandle, Memory<byte> readBuffer, out int readBytes)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryRead(uint indexGroup, uint indexOffset, Memory<byte> buffer, out int readBytes)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadDataType(string typeName, out IDataType? dataType)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadState(out StateInfo stateInfo)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadSymbol(string instancePath, out IAdsSymbol? symbol)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadValue(ISymbol symbol, out object? value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadValue<T>(ISymbol symbol, [AllowNull] out T? value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadValue(string instancePath, Type? type, out object? value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadValue<T>(string instancePath, [AllowNull] out T? value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadWrite(uint variableHandle, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer, out int readBytes)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryReadWrite(uint indexGroup, uint indexOffset, Memory<byte> readBuffer, ReadOnlyMemory<byte> writeBuffer, out int readBytes)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWrite(uint variableHandle, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWrite(uint indexGroup, uint indexOffset, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteControl(StateInfo stateInfo, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteControl(StateInfo stateInfo)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteValue(ISymbol symbol, object value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteValue<T>(ISymbol symbol, [DisallowNull] T value) where T : notnull
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteValue(string symbolPath, object value)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode TryWriteValue<T>(string symbolPath, T value) where T : notnull
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> UnregisterAdsStateChangedAsync(EventHandler<AdsStateChangedEventArgs> handler, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public AdsErrorCode UnregisterSymbolVersionChanged(EventHandler<AdsSymbolVersionChangedEventArgs> handler)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> UnregisterSymbolVersionChangedAsync(EventHandler<AdsSymbolVersionChangedEventArgs> handler, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void Write(uint variableHandle, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public void Write(uint indexGroup, uint indexOffset, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public void Write(uint indexGroup, uint indexOffset)
        {
            throw new NotImplementedException();
        }

        public void WriteAny(uint variableHandle, object value)
        {
            throw new NotImplementedException();
        }

        public void WriteAny(uint indexGroup, uint indexOffset, object value)
        {
            throw new NotImplementedException();
        }

        public void WriteAny(uint variableHandle, object value, int[]? args)
        {
            throw new NotImplementedException();
        }

        public void WriteAny(uint indexGroup, uint indexOffset, object value, int[]? args)
        {
            throw new NotImplementedException();
        }

        public ResultWrite WriteAnyAsResult(uint variableHandle, object value)
        {
            throw new NotImplementedException();
        }

        public ResultWrite WriteAnyAsResult(uint indexGroup, uint indexOffset, object value)
        {
            throw new NotImplementedException();
        }

        public ResultWrite WriteAnyAsResult(uint variableHandle, object value, int[]? args)
        {
            throw new NotImplementedException();
        }

        public ResultWrite WriteAnyAsResult(uint indexGroup, uint indexOffset, object value, int[]? args)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyAsync(uint variableHandle, object value, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyAsync(uint variableHandle, object value, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyAsync(uint indexGroup, uint indexOffset, object value, int[]? args, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyAsync(uint indexGroup, uint indexOffset, object value, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void WriteAnyString(uint indexGroup, uint indexOffset, string value, int length, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public void WriteAnyString(uint variableHandle, string value, int length, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public void WriteAnyString(string symbolPath, string value, int length, Encoding? encoding)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyStringAsync(uint indexGroup, uint indexOffset, string value, int length, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyStringAsync(uint variableHandle, string value, int length, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAnyStringAsync(string symbolPath, string value, int length, Encoding? encoding, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public ResultWrite WriteAsResult(uint indexGroup, uint indexOffset, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAsync(uint variableHandle, ReadOnlyMemory<byte> writeBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteAsync(uint indexGroup, uint indexOffset, ReadOnlyMemory<byte> writeBuffer, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void WriteControl(StateInfo stateInfo)
        {
            throw new NotImplementedException();
        }

        public void WriteControl(StateInfo stateInfo, ReadOnlyMemory<byte> writeBuffer)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> WriteControlAsync(AdsState state, ushort deviceState, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultAds> WriteControlAsync(AdsState state, ushort deviceState, ReadOnlyMemory<byte> data, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public void WriteValue(ISymbol symbol, object value)
        {
            throw new NotImplementedException();
        }

        public void WriteValue<T>(ISymbol symbol, [DisallowNull] T value) where T : notnull
        {
            throw new NotImplementedException();
        }

        public void WriteValue(string symbolPath, object value)
        {
            throw new NotImplementedException();
        }

        public void WriteValue<T>(string symbolPath, T value) where T : notnull
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteValueAsync(ISymbol symbol, object value, CancellationToken cancel)
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteValueAsync<T>(ISymbol symbol, T value, CancellationToken cancel) where T : notnull
        {
            throw new NotImplementedException();
        }

        public Task<ResultWrite> WriteValueAsync<T>(string symbolPath, T value, CancellationToken cancel) where T : notnull
        {
            throw new NotImplementedException();
        }
    }
}
