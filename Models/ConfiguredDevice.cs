using CommunityToolkit.Mvvm.ComponentModel;

namespace GoVeeSmartDashboard.Models;

public partial class ConfiguredDevice : ObservableObject
{
    [ObservableProperty] public partial string Name  { get; set; } = "";
    [ObservableProperty] public partial string Model { get; set; } = "";
    [ObservableProperty] public partial DeviceTransport Transport { get; set; } = DeviceTransport.Rest;
    [ObservableProperty] public partial string DeviceId { get; set; } = "";
    [ObservableProperty] public partial bool Enabled { get; set; } = true;

    public string TransportDisplay => Transport == DeviceTransport.Rest ? "REST" : "Bluetooth LE";
    public string TransportName
    {
        get => Transport == DeviceTransport.Bluetooth ? "Bluetooth" : "REST";
        set
        {
            Transport = value.Equals("Bluetooth", StringComparison.OrdinalIgnoreCase)
                ? DeviceTransport.Bluetooth : DeviceTransport.Rest;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransportDisplay));
        }
    }
}
