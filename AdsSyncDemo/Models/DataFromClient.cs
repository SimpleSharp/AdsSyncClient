using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AdsSyncClientDemo.Models
{
    /// <summary>
    /// Represents the data structure containing values read from the ADS client. The class inherits 
    /// from <see cref="ObservableObject"/> to support property change notifications required by the 
    /// ADS synchronization mechanism. Properties must be used to represent the ADS data; fields are 
    /// not supported and cause a <see cref="FormatException"/> when the AdsSyncClient is initialized.
    /// </summary>
    public partial class DataFromClient : ObservableObject
    {
        #region properties
        /// <summary> Lifebit </summary>
        [ObservableProperty] private bool lifeBit;
        /// <summary> test array </summary>
        [ObservableProperty] private ObservableCollection<byte> testArray = [];
        /// <summary> test string </summary>
        [ObservableProperty] private string testString = string.Empty;
        /// <summary> test double</summary>
        [ObservableProperty] private double testDouble;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public DataFromClient()
        {
            for (int i = 0; i < 10; i++)
            {
                testArray.Add(new());
            }
        }
        #endregion
    }
}
