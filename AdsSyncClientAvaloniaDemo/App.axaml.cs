using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AdsSyncClientAvaloniaDemo.ViewModels;
using AdsSyncClientAvaloniaDemo.Views;

namespace AdsSyncClientAvaloniaDemo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindowView(new MainWindowViewModel());
            //desktop.MainWindow = new MainWindow2View();
        }

        base.OnFrameworkInitializationCompleted();
    }
}