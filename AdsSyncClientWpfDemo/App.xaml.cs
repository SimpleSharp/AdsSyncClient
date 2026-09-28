using System.Windows;
using AdsSyncClientWpfDemo.ViewModels;
using AdsSyncClientWpfDemo.Views;

namespace AdsSyncClientWpfDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Starts the app with a MainWidow and sets up global exception handlers for unhandled exceptions and unobserved task exceptions.
        /// </summary>
        public void OnStartup(object sender, StartupEventArgs args)
        {
            MainWindowView mainWindow = new(new());
            mainWindow.Show();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// Handles unhandled exceptions
        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(ex.ToString(), "AdsSyncDemo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles unobserved task exceptions by displaying an error message box with the exception details.
        /// </summary>
        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs? e)
        {
            if (e is not null && e.Exception is Exception ex)
            {
                MessageBox.Show(ex.ToString(), "AdsSyncDemo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
