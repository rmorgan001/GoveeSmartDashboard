using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoVeeSmartDashboard.Models;
using GoVeeSmartDashboard.Services;
using System.Collections.ObjectModel;
using System.Reflection;

namespace GoVeeSmartDashboard.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly LoggingService _loggingService;
    private readonly GoVeeApiClient _api;
    private readonly GoVeeBleService _ble;
    private CancellationTokenSource? _pollCts;
    private int _logNumber;

    [ObservableProperty] public partial bool IsBusy { get; set; } 
    [ObservableProperty] public partial string StatusMessage { get; set; } = "Starting…";
    [ObservableProperty] public partial  DateTime LastPollTime { get; set; } = DateTime.Now;    
    [ObservableProperty] public partial string MessageToLog { get; set; }  = "Hello from the first tab!";
    [ObservableProperty] public partial string LogFilePath { get; set; }
    [ObservableProperty] public partial bool LogToFile { get; set; }
    [ObservableProperty] public partial string AssemblyVersion { get; set; } = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";

    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];
    public ObservableCollection<LogEntry> Entries => _loggingService.Entries;
    public SettingsViewModel Settings { get; }

    public MainWindowViewModel(SettingsService settings, GoVeeApiClient api, GoVeeBleService ble, LoggingService loggingService)
    {
        _settings=settings; _api=api; _ble=ble;
        Settings=new SettingsViewModel(SaveSettingsAsync);
        _loggingService = loggingService;
        LogFilePath = _loggingService.LogFilePath;
        LogToFile = _loggingService.LogToFile;
        _loggingService.FilePathChanged += (_, path) => Dispatcher.UIThread.Post(() => LogFilePath = path);
        LogMessage($"Application started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _ =InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        LastPollTime = DateTime.Now;
        var s=await _settings.LoadAsync();
        Settings.Load(s);
        await FindAndPollAsync();
        StartPolling();
    }

    partial void OnLogToFileChanged(bool value)
    {
        _loggingService.LogToFile = value;
        StatusMessage = value ? "File logging enabled." : "File logging disabled.";
        LogMessage(StatusMessage);
    }

    [RelayCommand]
    private void ClearLog()
    {
        _loggingService.ClearVisibleLog();
        StatusMessage = "Visible log cleared. Existing log files were not deleted.";
        LogMessage(StatusMessage);
    }

    [RelayCommand]
    private void ApplyLogFilePath()
    {
        try
        {
            _loggingService.SetLogFilePath(LogFilePath);
            StatusMessage = $"Log file base path set to: {LogFilePath}";
            LogMessage(StatusMessage);
        }
        catch (Exception ex) { StatusMessage = $"Invalid log path: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task FindAndPollAsync()
    {
        if (IsBusy)
        {
            StatusMessage=$"Warning: Busy {IsBusy}"; /* log status */
            LogMessage(StatusMessage);
            return;
        }
        if (!Settings.IsPolling)
        {
            StatusMessage=$"Warning: Polling {Settings.IsPolling}"; /* log status */
            LogMessage(StatusMessage);
            return;
        }
        IsBusy=true;
        try
        {
            await SaveSettingsAsync();
            var configs = Settings.Devices.Where(d => d.Enabled).ToList();
            var ble = configs.Where(d => d.Transport == DeviceTransport.Bluetooth).ToList();

            List<GoVeeRestDevice> restDevices = string.IsNullOrWhiteSpace(Settings.ApiKey)
                ? []
                : [.. (await _api.GetDevicesAsync(Settings.ApiKey, CancellationToken.None))];

            GoVeeBleService.BleReading[] readings = ble.Count == 0
                ? []
                : [.. (await _ble.ScanAsync(TimeSpan.FromSeconds(7), CancellationToken.None))];

            var unassignedBle = readings
                .OrderBy(r => r.BluetoothAddress, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var c in configs)
            {
                DeviceSnapshot? snapshot = null;
                if (c.Transport == DeviceTransport.Rest)
                {
                    var match = restDevices.FirstOrDefault(d =>
                        d.Sku.Equals(c.Model, StringComparison.OrdinalIgnoreCase) &&
                        (string.IsNullOrWhiteSpace(c.DeviceId)
                            ? d.DeviceName.Equals(c.Name, StringComparison.OrdinalIgnoreCase)
                            : d.DeviceId.Equals(c.DeviceId, StringComparison.OrdinalIgnoreCase)));
                    if (match is not null)
                    {
                        if (string.IsNullOrWhiteSpace(c.DeviceId)) c.DeviceId = match.DeviceId;
                        snapshot = await _api.GetStateAsync(Settings.ApiKey, c, CancellationToken.None);
                    }
                }
                else
                {
                    var match = !string.IsNullOrWhiteSpace(c.DeviceId)
                        ? readings.FirstOrDefault(r =>
                            r.BluetoothAddress.Equals(c.DeviceId, StringComparison.OrdinalIgnoreCase))
                        : unassignedBle.FirstOrDefault(r =>
                            r.LocalName.Contains(c.Model, StringComparison.OrdinalIgnoreCase));

                    if (match is not null)
                    {
                        if (string.IsNullOrWhiteSpace(c.DeviceId))
                        {
                            c.DeviceId = match.BluetoothAddress;
                            unassignedBle.Remove(match);
                        }

                        snapshot = new DeviceSnapshot
                        {
                            Name = c.Name, Model = c.Model, DeviceId = match.BluetoothAddress, Online = true,
                            TemperatureC = match.TemperatureC, TemperatureF=match.TemperatureC * 1.8 + 32, Humidity = match.Humidity,
                            BatteryPercent = match.BatteryPercent,
                            Transport = "Bluetooth LE", Detail = match.LocalName, Timestamp = match.LastSeen
                        };
                    }
                }

                UpdateCard(c, snapshot);
            }

            await SaveSettingsAsync();
            LastPollTime = DateTime.Now;
            StatusMessage="Poll Completed";
            LogMessage(StatusMessage);

        }
        catch (Exception ex)
        {
            StatusMessage=$"Error: {ex.Message}";
            LogMessage(StatusMessage);
        }
        finally { IsBusy=false; }
    }

    private void UpdateCard(ConfiguredDevice c, DeviceSnapshot? s)
    {
        var card=Devices.FirstOrDefault(x=>x.Name==c.Name);
        if(card is null)
        {
            card=new DeviceCardViewModel(c,
                c.Transport==DeviceTransport.Rest && c.Model.Equals("H5086",StringComparison.OrdinalIgnoreCase)
                    ? on=>SetPowerAsync(c,on) : null);
            Devices.Add(card);
        }
        card.Apply(s ?? new DeviceSnapshot
        {
            Name=c.Name, Model=c.Model, DeviceId=c.DeviceId, Online=false,
            Transport=c.TransportDisplay, Detail="Not discovered"
        });
    }

    private async Task SetPowerAsync(ConfiguredDevice c,bool on)
    {
        try
        {
            await _api.SetPowerAsync(Settings.ApiKey,c,on,CancellationToken.None);
            StatusMessage=$"{c.Name}: {(on ? "turning ON" : "turning OFF")}";
            await FindAndPollAsync();
        }
        catch(Exception ex) { StatusMessage=$"Power command failed: {ex.Message}"; }
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts=new CancellationTokenSource();
        var ct=_pollCts.Token;
        _=Task.Run(async()=>{
            while(!ct.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(Settings.PollingIntervalSeconds,2,300)),ct); }
                catch(OperationCanceledException ex)
                {
                    StatusMessage=$"OperationCanceledException: {ex.Message}";
                    LogMessage(StatusMessage);
                    break;
                }
                await Dispatcher.UIThread.InvokeAsync(async()=>await FindAndPollAsync());
                Dispatcher.UIThread.Invoke(RunTempChecks);
            }
        },ct);
    }

    private Task SaveSettingsAsync() => _settings.SaveAsync(Settings.ToSettings());

    private void RunTempChecks()
    {
        try
        {
            var plug1 = Devices.FirstOrDefault(u => string.Equals(u.Name, "PLUG 1", StringComparison.OrdinalIgnoreCase));
            var inside = Devices.FirstOrDefault(u => string.Equals(u.Name, "INSIDE", StringComparison.OrdinalIgnoreCase));
            var lanai = Devices.FirstOrDefault(u => string.Equals(u.Name, "LANAI", StringComparison.OrdinalIgnoreCase));

        if (plug1 == null || inside == null || lanai == null)
        {
            StatusMessage="Warning: Some sensors are missing ";
            LogMessage(StatusMessage);
            return; /* log any sensors that are null */
        }

        if (plug1 is { Online: false } && inside is { Online: false } && lanai is { Online: false })
        {
            StatusMessage="Warning: Some sensors not online ";
            LogMessage(StatusMessage);
            return; /* log any sensors that are not online */
        }

        if (inside.TemperatureF == null || lanai.TemperatureF == null)
        {
            StatusMessage=$"Warning: {inside.Name} {inside.TemperatureF ?? 0}, {lanai.Name} {lanai.TemperatureF ?? 0} ";
            LogMessage(StatusMessage);
            return; /* log temp data is missing from sensor */
        }

        if (inside.TemperatureF <= Settings.LowTempLimit) { 
            plug1.TurnOffCommand.Execute(null); /* log temp is below inside limit */ 
            StatusMessage=$"Action: {plug1.Name} Off - {inside.Name}: {inside.TemperatureF}";
            LogMessage(StatusMessage);
            return; }

        if (lanai.TemperatureF <= Settings.HighTempLimit)
        {
            plug1.TurnOnCommand.Execute(null); /* log temp is above lanai limit */
            StatusMessage=$"Action: {plug1.Name} On - {lanai.Name}: {lanai.TemperatureF}";
            LogMessage(StatusMessage);
            return;
        }

        if (lanai.TemperatureF > Settings.HighTempLimit)
        {
            plug1.TurnOffCommand.Execute(null); /* log temp is above lanai limit */
            StatusMessage=$"Action: {plug1.Name} Off - {lanai.Name}: {lanai.TemperatureF}";
            LogMessage(StatusMessage);
            return;
        }
        
        StatusMessage="All checks passed no actions ";
        LogMessage(StatusMessage);

        
        }
        catch (Exception ex)
        {
            StatusMessage=$"Error: {ex.Message}";
            LogMessage(StatusMessage);
        }
    }

    private void LogMessage(string message)
    {
        _logNumber++;
        var now = DateTimeOffset.Now;
        var entry = new LogEntry(_logNumber, now, message);
        Entries.Add(entry);
        _loggingService.LogAsync(entry).GetAwaiter().GetResult();
    }

    public void Dispose()=>_pollCts?.Cancel();
}