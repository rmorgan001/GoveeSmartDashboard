using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoVeeSmartDashboard.Models;
using System.Collections.ObjectModel;

namespace GoVeeSmartDashboard.ViewModels;

public partial class SettingsViewModel(Func<Task> save) : ObservableObject
{
    [ObservableProperty] private string apiKey = "";
    [ObservableProperty] private int pollingIntervalSeconds = 15;
    [ObservableProperty] private double lowTempLimit = 60.0;
    [ObservableProperty] private double highTempLimit = 75.0;
    [ObservableProperty] private bool isPolling = true;

    public ObservableCollection<ConfiguredDevice> Devices { get; } = [];

    public void Load(AppSettings s)
    {
        ApiKey=s.ApiKey; PollingIntervalSeconds=s.PollingIntervalSeconds; LowTempLimit=s.LowTempLimit; HighTempLimit=s.HighTempLimit; IsPolling=s.IsPolling; Devices.Clear();
        foreach (var d in s.Devices)
            Devices.Add(new ConfiguredDevice { Name=d.Name, Model=d.Model, Transport=d.Transport, DeviceId=d.DeviceId, Enabled=d.Enabled });
    }

    [RelayCommand] private async Task AddDeviceAsync()
    {
        Devices.Add(new ConfiguredDevice { Name="New Device", Model="H5110", Transport=DeviceTransport.Bluetooth });
        await save();
    }
    [RelayCommand] private async Task RemoveDeviceAsync(ConfiguredDevice? d)
    {
        if (d is null) return; Devices.Remove(d); await save();
    }
    [RelayCommand] private Task SaveAsync() => save();

    public AppSettings ToSettings() => new()
    {
        ApiKey=ApiKey.Trim(), PollingIntervalSeconds=Math.Clamp(PollingIntervalSeconds,2,300), LowTempLimit=Math.Round(LowTempLimit, 2), HighTempLimit=Math.Round(HighTempLimit, 2), IsPolling=IsPolling,
        Devices=
        [
            .. Devices.Select(d => new ConfiguredDeviceData
            {
                Name = d.Name, Model = d.Model.Trim(), Transport = d.Transport, DeviceId = d.DeviceId.Trim(),
                Enabled = d.Enabled
            })
        ]
    };
}
