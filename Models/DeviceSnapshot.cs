namespace GoVeeSmartDashboard.Models;

public sealed class DeviceSnapshot
{
    public string Name { get; init; } = "";
    public string Model { get; init; } = "";
    public string DeviceId { get; init; } = "";
    public bool Online { get; init; }
    public bool? PowerOn { get; init; }
    public double? TemperatureC { get; init; }
    public double? TemperatureF { get; init; }
    public double? Humidity { get; init; }
    public int? BatteryPercent { get; init; }
    public string Transport { get; init; } = "";
    public string Detail { get; init; } = "";
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
}
