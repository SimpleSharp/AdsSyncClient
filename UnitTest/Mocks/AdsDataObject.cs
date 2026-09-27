using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace AdsSyncClientDemo.Models
{
    public partial class AdsDataObject : ObservableObject
    {
        #region properties
        /// <summary> test bool </summary>
        [ObservableProperty] private bool testBool;
        /// <summary> test array </summary>
        [ObservableProperty] private ObservableCollection<byte> testArray = [];
        /// <summary> test string </summary>
        [ObservableProperty] private string testString = string.Empty;
        /// <summary> test double</summary>
        [ObservableProperty] private double testdouble = new();
        #endregion

        #region constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public AdsDataObject()
        {
            for (int i1 = 0; i1 < 10; i1++)
            {
                testArray.Add(new());
            }
        }
        #endregion
    }
}
