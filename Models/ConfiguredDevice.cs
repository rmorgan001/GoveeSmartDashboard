using CommunityToolkit.Mvvm.ComponentModel;

namespace GoVeeSmartDashboard.Models;

public partial class ConfiguredDevice : ObservableObject
{
    [ObservableProperty] private string name = "";
    [ObservableProperty] private string model = "";
    [ObservableProperty] private DeviceTransport transport = DeviceTransport.Rest;
    [ObservableProperty] private string deviceId = "";
    [ObservableProperty] private bool enabled = true;

    public string TransportDisplay => Transport == DeviceTransport.Rest ? "REST" : "Bluetooth LE";
    public string TransportName
    {
        get => Transport == DeviceTransport.Bluetooth ? "Bluetooth" : "REST";
        set
        {
            Transport = value.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase)
                ? DeviceTransport.Bluetooth : DeviceTransport.Rest;
            OnPropertyChanged(nameof(TransportName));
            OnPropertyChanged(nameof(TransportDisplay));
        }
    }
}
