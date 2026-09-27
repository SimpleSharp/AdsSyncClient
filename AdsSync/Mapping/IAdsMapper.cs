using System.ComponentModel;
using System.Reflection;
using TwinCAT.Ads;

namespace AdsSync.Mapping
{
    /// <summary>
    /// Base interface for ADS mapping purposes.
    /// </summary>
    internal interface IAdsMapper
    {
        #region properties
        /// <summary> The object containing the data to be written / read to the ADS client. </summary>
        public INotifyPropertyChanged Data { get; }
        /// <summary> The data object converted to a class suitable for marshalling </summary>
        public object DataAsMarshalledClass { get; }
        /// <summary> The marshalled sizes of the elements of this data structure </summary>
        public uint[] Sizes { get; }
        /// <summary> The ADS variable handles assigned to this data structure </summary>
        public uint[] VariableHandles { get; }
        /// <summary> The field metadata of the marshalled data class </summary>
        public FieldInfo[] FieldInfos { get; }
        /// <summary> The property metadata of the input data object </summary>
        public PropertyInfo[] PropertyInfos { get; }
        /// <summary> The data types of the variables in the client </summary>
        public Type[] Types { get; }
        /// <summary> The name of the ADS structure containing the data to be written / read </summary>
        public string StructName { get; }
        #endregion

        #region public methods
        /// <summary>
        /// Creates ADS variable handles for the marshalled data structure.
        /// </summary>
        /// <param name="adsClient"> The ADS client used to create the variable handles </param>
        public Task GenerateVariableHandlesAsync(IAdsConnectAddress adsClient);

        /// <summary>
        /// Deletes all ADS variable handles for the marshalled data structure.
        /// </summary>
        public Task DeleteAllVariableHandlesAsync();
        #endregion
    }
}
