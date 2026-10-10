using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoVeeSmartDashboard.Models;

namespace GoVeeSmartDashboard.ViewModels;

public partial class DeviceCardViewModel : ObservableObject
{
    private readonly Func<bool, Task>? _setPower;

    [ObservableProperty] private bool online;
    [ObservableProperty] private bool? powerOn;
    [ObservableProperty] private double? temperatureC;
    [ObservableProperty] private double? temperatureF;
    [ObservableProperty] private double? humidity;
    [ObservableProperty] private int? batteryPercent;
    [ObservableProperty] private string detail = "";
    [ObservableProperty] private string deviceId = "";
    [ObservableProperty] private DateTimeOffset? lastSeen;

    public string Name { get; }
    public string Model { get; }
    public string Transport { get; }
    public bool IsPlug => Model.Equals("H5086", StringComparison.OrdinalIgnoreCase);
    public string OnlineText => Online ? "ONLINE" : "OFFLINE";
    public string StatusText => PowerOn == true ? "ON" : PowerOn == false ? "OFF" : "—";
    public string StatusColor => PowerOn == true ? "#39D353" : "#6E7681";

    public DeviceCardViewModel(ConfiguredDevice c, Func<bool, Task>? setPower)
    {
        Name=c.Name; Model=c.Model; Transport=c.TransportDisplay; _setPower=setPower;
    }

    public void Apply(DeviceSnapshot s)
    {
        Online=s.Online; PowerOn=s.PowerOn; TemperatureC=s.TemperatureC; TemperatureF=s.TemperatureC * 1.8 + 32; Humidity=s.Humidity;
        BatteryPercent=s.BatteryPercent; Detail=s.Detail; DeviceId=s.DeviceId; LastSeen=s.Timestamp;
        OnPropertyChanged(nameof(OnlineText)); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusColor));
    }

    [RelayCommand] private Task TurnOnAsync() => _setPower?.Invoke(true) ?? Task.CompletedTask;
    [RelayCommand] private Task TurnOffAsync() => _setPower?.Invoke(false) ?? Task.CompletedTask;
}
