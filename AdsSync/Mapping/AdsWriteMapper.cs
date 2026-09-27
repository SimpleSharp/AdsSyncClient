using System.ComponentModel;

namespace AdsSync.Mapping
{
    /// <summary>
    /// Provides an ADS mapper for transferring property values from a .NET data object to the 
    /// corresponding fields of an ADS structure.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="AdsMapperBase"/> with functionality for identifying changed 
    /// or requested properties and retrieving their values together with the corresponding ADS 
    /// variable handles.
    ///
    /// Property names are mapped to their indices in the marshalled data structure. The
    /// <see cref="TryTakeOverPropertyValue(string, out uint?, out object?)"/> method uses transfer 
    /// it to <see cref="AdsMapperBase.DataAsMarshalledClass"/>, and return the associated ADS variable 
    /// handle and marshalled field value.
    ///
    /// Variable handles must be generated before the returned handles can be used for ADS write operations.
    /// </remarks>
    internal class AdsWriteMapper : AdsMapperBase
    {
        #region constructors
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="data"> The object containing the data to be written to the ADS client </param>
        /// <param name="structName"> The name of the ADS structure containing the data to be written </param>
        public AdsWriteMapper(INotifyPropertyChanged data, string structName) : base(data, structName) { }
        #endregion
    }
}