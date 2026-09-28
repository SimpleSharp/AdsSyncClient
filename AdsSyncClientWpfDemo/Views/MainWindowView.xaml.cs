using System.Windows;
using AdsSync.Exceptions;
using AdsSyncClientWpfDemo.ViewModels;

namespace AdsSyncClientWpfDemo.Views
{
    public partial class MainWindowView : Window
    {
        #region fields
        /// <summary> Is currently showing a MessageBox </summary>
        private bool isShowingMessageBox;
        #endregion

        #region constructors
        /// <summary>
        /// Constructor of the MainWindow class. Initializes the window, sets up data context, and starts a timer to toggle the Lifebit property in the dataToPlc object every second. 
        /// Also sets up property change notifications for the FirstCounter and SecondCounter properties of the dataFromPlc object to update the corresponding properties in the dataToPlc object.
        /// </summary>
        /// <param name="viewModel"> the viewmodel for this window </param>
        public MainWindowView(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.InvalidNetIdEntered += InvalidNetIdMessageBox;
            viewModel.InvalidPortEntered += InvalidNetIdMessageBox;
            viewModel.SyncClient.ConnectionError += ConnectionErrorMessageBox;
            viewModel.SyncClient.CommunicationError += CommunicationErrorMessageBox;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Opens a messagebox with an error when a connection error has occurred
        /// </summary>
        private void ConnectionErrorMessageBox(object? sender, ConnectionErrorEventArgs? e)
        {
            if (!isShowingMessageBox)
            {
                isShowingMessageBox = true;
                _ = MessageBox.Show(e!.Exception.ToString()!, "AdsSyncClient", MessageBoxButton.OK, MessageBoxImage.Error);
                isShowingMessageBox = false;
            }
        }

        /// <summary>
        /// Opens a messagebox with an error when a communication error has occurred
        /// </summary>
        private void CommunicationErrorMessageBox(object? sender, CommunicationErrorEventArgs? e)
        {
            if (!isShowingMessageBox)
            {
                isShowingMessageBox = true;
                _ = MessageBox.Show(e!.Exception.ToString()!, "AdsSyncClient", MessageBoxButton.OK, MessageBoxImage.Error);
                isShowingMessageBox = false;
            }
        }

        /// <summary>
        /// Opens a messagebox with an error when an invalid net id was entered
        /// </summary>
        private void InvalidNetIdMessageBox(object? sender, EventArgs? e)
        {
            if (!isShowingMessageBox)
            {
                isShowingMessageBox = true;
                _ = MessageBox.Show("Invalid Net ID was entered. Please check your input.", "AdsSyncClient", MessageBoxButton.OK, MessageBoxImage.Error);
                isShowingMessageBox = false;
            }
        }

        /// <summary>
        /// Opens a messagebox with an error when an invalid port was entered
        /// </summary>
        private void InvalidPortMessageBox(object? sender, EventArgs? e)
        {
            if (!isShowingMessageBox)
            {
                isShowingMessageBox = true;
                _ = MessageBox.Show("Invalid Port was entered. Please check your input.", "AdsSyncClient", MessageBoxButton.OK, MessageBoxImage.Error);
                isShowingMessageBox = false;
            }
        }
        #endregion
    }
}