using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using TwinCAT.Ads;

namespace AdsSync.Mapping
{
    /// <summary>
    /// Provides the common functionality for mapping a data object to an ADS structure.
    /// </summary>
    /// <remarks>
    /// The mapper converts the properties of an object implementing <see cref="INotifyPropertyChanged"/> 
    /// into a class suitable for interop marshalling. It determines the marshalled size of each field 
    /// and manages the ADS variable handles required to read and write the individual structure fields.
    ///
    /// The data object must be a class with at least one public instance property. Each public property 
    /// must correspond by position to a private instance field of the same type. Derived classes can 
    /// extend this class to implement application-specific ADS read, write, or notification handling.
    /// </remarks>
    internal abstract class AdsMapperBase : IAdsMapper, IAsyncDisposable
    {
        #region properties
        /// <summary> The object containing the data to be written / read to the ADS client. </summary>
        public INotifyPropertyChanged Data { get; protected set; }
        /// <summary> The data object converted to a class suitable for marshalling </summary>
        public object DataAsMarshalledClass { get; set; }
        /// <summary> The marshalled sizes of the elements of this data structure </summary>
        public uint[] Sizes { get; protected set; }
        /// <summary> The ADS variable handles assigned to this data structure </summary>
        public uint[] VariableHandles { get; protected set; }
        /// <summary> The field metadata of the marshalled data class </summary>
        public FieldInfo[] FieldInfos { get; protected set; }
        /// <summary> The property metadata of the input data object </summary>
        public PropertyInfo[] PropertyInfos { get; protected set; }
        /// <summary> The data types of the variables in the client </summary>
        public Type[] Types{ get; protected set; }
        /// <summary> The unmananged data types of the variables in the client </summary>
        public UnmanagedType[] UnmanagedTypes { get; protected set; }
        /// <summary> The name of the ADS structure containing the data to be written / read </summary>
        public string StructName { get; protected set; }
        #endregion

        #region fields
        /// <summary> The ADS client used to manage variable handles </summary>
        protected IAdsConnectAddress? adsClient;
        /// <summary> Indicates whether the object has been disposed </summary>
        protected bool disposed;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="data"> The object whose data is to be read from the ADS client </param>
        /// <param name="structName"> The name of the ADS structure containing the data to be read </param>
        /// /// <exception cref="ObjectDisposedException"> Thrown when this mapper has already been disposed. </exception>
        /// /// <exception cref="ArgumentException"> Thrown when the structure name is null or empty. </exception>
        protected AdsMapperBase(INotifyPropertyChanged data, string structName)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrWhiteSpace(structName);
            ValidateDataObject(data);
            StructName = structName;
            Data = data;
            DataAsMarshalledClass = Marshalling.PropertyClassToMarshalledFieldClass(data);
            Marshalling.UpdateFieldClassValuesFromProperties(DataAsMarshalledClass, Data);
            FieldInfos = DataAsMarshalledClass.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfos = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Sizes = new uint[FieldInfos.Length];
            Types = new Type[FieldInfos.Length];
            UnmanagedTypes = new UnmanagedType[FieldInfos.Length];
            VariableHandles = new uint[Sizes.Length];
            for (int i1 = 0; i1 < FieldInfos.Length; i1++)
            {
                UnmanagedTypes[i1] = FieldInfos[i1].FieldType.TypeToUnmanagedType();
                if (FieldInfos[i1].FieldType.IsArray)
                {
                    Array array = (Array)FieldInfos[i1].GetValue(DataAsMarshalledClass)!;
                    Type elementType = array.GetType().GetElementType()!;
                    // Create a new class data type containing the array for marshalling purposes.
                    Types[i1] = Marshalling.CreateExpandedArrayType(elementType, array.Length);
                    UnmanagedType elementUnmanagedType = elementType.TypeToUnmanagedType();
                    Sizes[i1] = (uint)(elementUnmanagedType.GetUnmanagedTypeSize() * array.Length);
                }
                else
                {
                    Types[i1] = FieldInfos[i1].FieldType;
                    Sizes[i1] = (uint)UnmanagedTypes[i1].GetUnmanagedTypeSize();
                }
            }
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

        /// <summary>
        /// Creates ADS variable handles for the marshalled data structure.
        /// </summary>
        /// <param name="adsClient"> The ADS client used to create the variable handles </param>
        /// <exception cref="ObjectDisposedException"> Thrown when this object is already disposed. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="adsClient"/> is <see langword="null"/>. </exception>
        public async Task GenerateVariableHandlesAsync(IAdsConnectAddress adsClient)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(adsClient);
            List<Exception> exceptions = [];
            for (int i = 0; i < FieldInfos.Length; i++)
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
                try
                {
                    ResultHandle resultHandle = await adsClient.CreateVariableHandleAsync($"{StructName}.{FieldInfos[i].Name}", cts.Token);
                    if (resultHandle.Succeeded)
                    {
                        VariableHandles[i] = resultHandle.Handle;
                    }
                    else
                    {
                        exceptions.Add(new AdsErrorException("Error while creating a variable handle.", resultHandle.ErrorCode));
                    }
                }
                catch (OperationCanceledException)
                {
                    exceptions.Add(new TimeoutException($"Timeout while creating a variable handle."));
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("One or more ADS variable handles could not be deleted.", exceptions);
            }
            this.adsClient = adsClient;
        }

        /// <summary>
        /// Deletes all ADS variable handles for the marshalled data structure.
        /// </summary>
        /// <exception cref="ObjectDisposedException"> Thrown when this object is already disposed. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="adsClient"/> is <see langword="null"/>. </exception>
        public async Task DeleteAllVariableHandlesAsync()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            ArgumentNullException.ThrowIfNull(adsClient);
            List<Exception> exceptions = [];
            for (int i = 0; i < VariableHandles.Length; i++)
            {
                if (VariableHandles[i] > 0)
                {
                    using CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));
                    try
                    {
                        await adsClient.DeleteVariableHandleAsync(VariableHandles[i], cts.Token);
                        VariableHandles[i] = 0;
                    }
                    catch (OperationCanceledException)
                    {
                        exceptions.Add(new TimeoutException($"Timeout while deleting ADS variable handle {VariableHandles[i]}."));
                    }
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException("One or more ADS variable handles could not be deleted.", exceptions);
            }
        }
        #endregion

        #region private methods
        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        /// <param name="disposing"> Disposes this istance if TRUE </param>
        protected async virtual ValueTask DisposeAsync(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing && adsClient is not null && adsClient.IsConnected)
            {
                await DeleteAllVariableHandlesAsync();
            }
            disposed = true;
        }

        /// <summary>
        /// Validates the structure of the specified ADS data object. The object must be a class that implements 
        /// <see cref="INotifyPropertyChanged"/> and must contain at least one public instance property. The number
        /// of public instance properties must correspond to the number of private instance fields. Corresponding
        /// properties and fields must have identical data types.
        /// </summary>
        /// <param name="data"> The data object whose property and field structure is validated </param>
        /// <exception cref="ArgumentNullException"> Thrown when <paramref name="data"/> is <see langword="null"/>. </exception>
        /// <exception cref="ArgumentException"> Thrown when the runtime type of <paramref name="data"/> is not a class
        /// or does not implement <see cref="INotifyPropertyChanged"/>. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when the data object does not contain any public properties,
        /// contains indexer properties, or has an invalid property/field mapping. </exception>
        private static void ValidateDataObject(INotifyPropertyChanged data)
        {
            ArgumentNullException.ThrowIfNull(data);
            Type dataType = data.GetType();
            if (!dataType.IsClass)
            {
                throw new ArgumentException($"The data type '{dataType.FullName}' must be a class.", nameof(data));
            }
            if (!typeof(INotifyPropertyChanged).IsAssignableFrom(dataType))
            {
                throw new ArgumentException($"The data type '{dataType.FullName}' must implement " + $"{nameof(INotifyPropertyChanged)}.", nameof(data));
            }
            PropertyInfo[] publicProperties = dataType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            FieldInfo[] privateFields = [.. dataType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(field => field.IsPrivate)];
            if (publicProperties.Length == 0)
            {
                throw new InvalidOperationException($"The data type '{dataType.FullName}' must contain " + "at least one public property.");
            }
            if (publicProperties.Any(property => property.GetIndexParameters().Length > 0))
            {
                throw new InvalidOperationException($"The data type '{dataType.FullName}' must not contain indexer properties.");
            }
            if (publicProperties.Length != privateFields.Length)
            {
                throw new InvalidOperationException($"The data type '{dataType.FullName}' contains " + $"{publicProperties.Length} public properties but {privateFields.Length} " +
                                                    "private instance fields. Both counts must be equal.");
            }
            List<string> typeMismatches = [];
            for (int i = 0; i < publicProperties.Length; i++)
            {
                PropertyInfo property = publicProperties[i];
                FieldInfo field = privateFields[i];
                if (property.PropertyType != field.FieldType)
                {
                    typeMismatches.Add($"Position {i}: property type " + $"'{property.PropertyType.FullName}' differs from field type " + $"'{field.FieldType.FullName}'.");
                }
            }
            if (typeMismatches.Count == 0)
            {
                return;
            }
            StringBuilder message = new();
            message.AppendLine($"The data type '{dataType.FullName}' has mismatching property and " + "field types.");
            foreach (string mismatch in typeMismatches)
            {
                message.AppendLine($"  - {mismatch}");
            }
            throw new InvalidOperationException(message.ToString());
        }
        #endregion
    }
}