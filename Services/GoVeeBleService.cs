using System.Runtime.InteropServices.WindowsRuntime;
using GoVeeSmartDashboard.Models;
using Windows.Devices.Bluetooth.Advertisement;

namespace GoVeeSmartDashboard.Services;

public sealed class GoVeeBleService
{
    private readonly Dictionary<ulong, BleReading> _latest = new();
    private readonly object _gate = new();

    public async Task<IReadOnlyList<BleReading>> ScanAsync(TimeSpan duration, CancellationToken ct)
    {
        BluetoothLEAdvertisementWatcher? watcher = null;
        void Received(BluetoothLEAdvertisementWatcher _, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                var name = args.Advertisement.LocalName ?? "";
                if (!name.Contains("H5110", StringComparison.OrdinalIgnoreCase) &&
                    !name.Contains("GVH5110", StringComparison.OrdinalIgnoreCase)) return;

                foreach (var section in args.Advertisement.ManufacturerData)
                {
                    var bytes = section.Data.ToArray();
                    if (bytes.Length < 6) continue;
                    var decoded = Decode(bytes, 2);
                    if (decoded.error || decoded.temp < -40 || decoded.temp > 100) continue;
                    lock (_gate)
                        _latest[args.BluetoothAddress] = new BleReading
                        {
                            BluetoothAddress = FormatAddress(args.BluetoothAddress),
                            LocalName = name, TemperatureC = decoded.temp,
                            Humidity = decoded.humidity, BatteryPercent = decoded.battery,
                            LastSeen = DateTimeOffset.Now
                        };
                }
            }
            catch { }
        }

        watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        watcher.Received += Received;
        watcher.Start();
        try { await Task.Delay(duration, ct); }
        finally { watcher.Stop(); watcher.Received -= Received; }

        lock (_gate) return _latest.Values.ToList();
    }

    private static (double temp, double humidity, int battery, bool error) Decode(byte[] b, int o)
    {
        if (b.Length < o + 4) return (0, 0, 0, true);
        var raw = (b[o] << 16) | (b[o + 1] << 8) | b[o + 2];
        var negative = (raw & 0x800000) != 0;
        var magnitude = raw & 0x7FFFFF;
        var temp = (int)(magnitude / 1000) / 10.0;
        if (negative) temp = -temp;
        return (temp, (magnitude % 1000) / 10.0, b[o + 3] & 0x7F, (b[o + 3] & 0x80) != 0);
    }

    private static string FormatAddress(ulong a) =>
        string.Join(":", Enumerable.Range(0, 6).Select(i => ((a >> (8 * (5 - i))) & 255).ToString("X2")));

    public sealed class BleReading
    {
        public string BluetoothAddress { get; init; } = "";
        public string LocalName { get; init; } = "";
        public double TemperatureC { get; init; }
        public double Humidity { get; init; }
        public int BatteryPercent { get; init; }
        public DateTimeOffset LastSeen { get; init; }
    }
}
