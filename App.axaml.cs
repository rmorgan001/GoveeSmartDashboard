using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GoVeeSmartDashboard.Services;
using GoVeeSmartDashboard.ViewModels;
using GoveeSmartDashboard.Views;
using GoVeeSmartDashboard.Views;

namespace GoveeSmartDashboard;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = new SettingsService();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(settings, new GoVeeApiClient(), new GoVeeBleService(), new LoggingService())
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
