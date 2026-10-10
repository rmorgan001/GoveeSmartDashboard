using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GoVeeSmartDashboard.ViewModels;

namespace GoVeeSmartDashboard.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_,_) => (DataContext as MainWindowViewModel)?.Dispose();
    }
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
