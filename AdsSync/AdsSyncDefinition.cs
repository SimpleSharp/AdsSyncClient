using System.ComponentModel;

namespace AdsSync
{
    /// <summary>
    /// Describes the data exchange between a .NET application and an ADS client.
    /// </summary>
    public sealed class AdsSyncDefinition
    {
        /// <summary> Gets the object containing the data to be sent to the ADS client. </summary>
        public INotifyPropertyChanged DataToAdsClient { get; }
        /// <summary> Gets the object receiving the data read from the ADS client. </summary>
        public INotifyPropertyChanged DataFromAdsClient { get; }
        /// <summary> Gets the name of the ADS structure to which the data is written. </summary>
        public string StructNameDataToClient { get; }
        /// <summary> Gets the name of the ADS structure from which the data is read. </summary>
        public string StructNameDataFromClient { get; }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        public AdsSyncDefinition(INotifyPropertyChanged dataToAdsClient,
                                  INotifyPropertyChanged dataFromAdsClient,
                                  string structNameDataToClient,
                                  string structNameDataFromClient)
        {
            ArgumentNullException.ThrowIfNull(dataToAdsClient);
            ArgumentNullException.ThrowIfNull(dataFromAdsClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(structNameDataToClient);
            ArgumentException.ThrowIfNullOrWhiteSpace(structNameDataFromClient);
            DataToAdsClient = dataToAdsClient;
            DataFromAdsClient = dataFromAdsClient;
            StructNameDataToClient = structNameDataToClient;
            StructNameDataFromClient = structNameDataFromClient;
        }
    }

}
