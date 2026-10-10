using CommunityToolkit.Mvvm.ComponentModel;

namespace GoVeeSmartDashboard.Models;

public sealed class AppSettings
{
    public string ApiKey { get; set; } = "";
    public int PollingIntervalSeconds { get; set; } = 15;
    public double LowTempLimit { get; set; } =  60.0;
    public double HighTempLimit { get; set; } = 75.0;
    public bool IsPolling { get; set; } = true;

    public List<ConfiguredDeviceData> Devices { get; set; } =
    [
        new() { Name = "Plug 1", Model = "H5086", Transport = DeviceTransport.Rest },
        new() { Name = "Inside", Model = "H5110", Transport = DeviceTransport.Bluetooth },
        new() { Name = "Lanai", Model = "H5110", Transport = DeviceTransport.Bluetooth }
    ];
}
public sealed class ConfiguredDeviceData
{
    public string Name { get; set; } = "";
    public string Model { get; set; } = "";
    public DeviceTransport Transport { get; set; }
    public string DeviceId { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
