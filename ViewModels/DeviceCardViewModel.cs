using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoVeeSmartDashboard.Models;

namespace GoVeeSmartDashboard.ViewModels;

public partial class DeviceCardViewModel(ConfiguredDevice c, Func<bool, Task>? setPower) : ObservableObject
{
    [ObservableProperty] public partial bool Online { get; set; }
    [ObservableProperty] public partial bool? PowerOn { get; set; }
    [ObservableProperty] public partial double? TemperatureC { get; set; }
    [ObservableProperty] public partial double? TemperatureF { get; set; }
    [ObservableProperty] public partial double? Humidity { get; set; }
    [ObservableProperty] public partial int? BatteryPercent { get; set; }
    [ObservableProperty] public partial string Detail { get; set; } = "";
    [ObservableProperty] public partial string DeviceId { get; set; } = "";
    [ObservableProperty] public partial DateTimeOffset? LastSeen { get; set; } = null;

    public string Name { get; } = c.Name;
    public string Model { get; } = c.Model;
    public string Transport { get; } = c.TransportDisplay;
    public bool IsPlug => Model.Equals("H5086", StringComparison.OrdinalIgnoreCase);
    public string OnlineText => Online ? "ONLINE" : "OFFLINE";
    public string StatusText => PowerOn switch
    {
        true => "ON",
        false => "OFF",
        _ => "—"
    };
    public string StatusColor => PowerOn == true ? "#39D353" : "#6E7681";

    public void Apply(DeviceSnapshot s)
    {
        Online=s.Online; PowerOn=s.PowerOn; TemperatureC=s.TemperatureC; TemperatureF=s.TemperatureC * 1.8 + 32; Humidity=s.Humidity;
        BatteryPercent=s.BatteryPercent; Detail=s.Detail; DeviceId=s.DeviceId; LastSeen=s.Timestamp;
        OnPropertyChanged(nameof(OnlineText)); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusColor));
    }

    [RelayCommand] private Task TurnOnAsync() => setPower?.Invoke(true) ?? Task.CompletedTask;
    [RelayCommand] private Task TurnOffAsync() => setPower?.Invoke(false) ?? Task.CompletedTask;
}
