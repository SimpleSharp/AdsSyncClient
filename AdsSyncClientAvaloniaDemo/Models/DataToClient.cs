using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AdsSyncClientAvaloniaDemo.Models
{
    /// <summary>
    /// Represents the data structure containing values written to the ADS client. The class inherits 
    /// from <see cref="ObservableObject"/> to support property change notifications required by the 
    /// ADS synchronization mechanism. Properties must be used to represent the ADS data; fields are 
    /// not supported and cause a <see cref="FormatException"/> when the AdsSyncClient is initialized.
    /// </summary>
    public partial class DataToClient : ObservableObject
    {
        #region properties
        /// <summary> Lifebit </summary>
        [ObservableProperty] private bool lifeBit;
        /// <summary> test array </summary>
        [ObservableProperty] private ObservableCollection<byte> testArray = [];
        /// <summary> test string </summary>
        [ObservableProperty] private string testString = "This a data exchange test with a UTF8-string!";
        /// <summary> test double</summary>
        [ObservableProperty] private double testdouble = 82.8235;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public DataToClient()
        {
            for (int i1 = 0; i1 < 10; i1++)
            {
                testArray.Add(new());
            }
        }
        #endregion
    }
}
