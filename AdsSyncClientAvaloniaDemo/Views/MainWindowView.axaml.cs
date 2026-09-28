using AdsSync.Exceptions;
using AdsSyncClientAvaloniaDemo.ViewModels;
using Avalonia.Controls;
using MsBox.Avalonia;
using System;

namespace AdsSyncClientAvaloniaDemo.Views;

public partial class MainWindowView : Window
{
    #region fields
    /// <summary> Is currently showing a MessageBox </summary>
    private bool isShowingMessageBox;
    #endregion

    #region  constructors
    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindowView()
    {
        InitializeComponent();
    }

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
            _ = MessageBoxManager.GetMessageBoxStandard(e!.Exception.ToString(), "AdsSyncClient", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
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
            _ = MessageBoxManager.GetMessageBoxStandard(e!.Exception.ToString(), "AdsSyncClient", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
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
            _ = MessageBoxManager.GetMessageBoxStandard("Invalid Net ID was entered. Please check your input.", "AdsSyncClient", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
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
            _ = MessageBoxManager.GetMessageBoxStandard("Invalid Port was entered. Please check your input.", "AdsSyncClient", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            isShowingMessageBox = false;
        }
    }
    #endregion
}