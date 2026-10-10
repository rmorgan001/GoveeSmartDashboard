using System.Collections.ObjectModel;
using Avalonia.Threading;
using GoVeeSmartDashboard.Models;

namespace GoVeeSmartDashboard.Services;

/// <summary>Shared UI/file logger with daily noon-to-noon file rollover.</summary>
public sealed class LoggingService : IDisposable
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _defaultDirectory;
    private StreamWriter? _writer;
    private string? _activeFilePath;
    private bool _disposed;
    private bool _logToFile = true;
    private readonly object _sync = new();

    public ObservableCollection<LogEntry> Entries { get; } = new();
    public event EventHandler<string>? FilePathChanged;

    public LoggingService()
    {
        _defaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GoveeSmartDashboard", "Logs");
        Directory.CreateDirectory(_defaultDirectory);
        LogFilePath = Path.Combine(_defaultDirectory, $"GoveeSession{DateTime.Now:yyyyMMdd_HHmmss}.log");
    }

    public string LogFilePath { get; private set; }

    public bool LogToFile
    {
        get { lock (_sync) return _logToFile; }
        set
        {
            lock (_sync) _logToFile = value;
            if (!value) CloseWriterAsync().GetAwaiter().GetResult();
        }
    }

    public void SetLogFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A log file path is required.", nameof(path));

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
        lock (_sync)
        {
            if (string.Equals(LogFilePath, fullPath, StringComparison.OrdinalIgnoreCase)) return;
            LogFilePath = fullPath;
            _activeFilePath = null;
        }
        CloseWriterAsync().GetAwaiter().GetResult();
        FilePathChanged?.Invoke(this, fullPath);
    }

    public async Task LogAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        if (_disposed) return;
        //var entry = new LogEntry(logNumber, now, message);
        //await Dispatcher.UIThread.InvokeAsync(() => Entries.Add(entry));
        if (!LogToFile) return;

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            if (!LogToFile) return;
            var targetPath = GetEffectiveFilePath(entry.Timestamp);
            await EnsureWriterAsync(targetPath, cancellationToken);
            await _writer!.WriteLineAsync(entry.DisplayText.AsMemory(), cancellationToken);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _fileLock.Release(); }
    }

    public void ClearVisibleLog() => Dispatcher.UIThread.Post(Entries.Clear);

    private string GetEffectiveFilePath(DateTime timestamp)
    {
        lock (_sync)
        {
            var directory = Path.GetDirectoryName(LogFilePath) ?? _defaultDirectory;
            var extension = Path.GetExtension(LogFilePath);
            var stem = Path.GetFileNameWithoutExtension(LogFilePath);
            var periodStart = timestamp.TimeOfDay >= TimeSpan.FromHours(12)
                ? timestamp.Date.AddHours(12)
                : timestamp.Date.AddDays(-1).AddHours(12);
            return Path.Combine(directory, $"{stem}_{periodStart:yyyy-MM-dd}{extension}");
        }
    }

    private async Task EnsureWriterAsync(string targetPath, CancellationToken cancellationToken)
    {
        if (string.Equals(_activeFilePath, targetPath, StringComparison.OrdinalIgnoreCase) && _writer is not null)
            return;

        if (_writer is not null)
        {
            await _writer.DisposeAsync();
            _writer = null;
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        _writer = new StreamWriter(new FileStream(targetPath, FileMode.Append, FileAccess.Write,
            FileShare.ReadWrite, 4096, useAsync: true));
        _activeFilePath = targetPath;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task CloseWriterAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (_writer is not null) { await _writer.DisposeAsync(); _writer = null; }
            _activeFilePath = null;
        }
        finally { _fileLock.Release(); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _fileLock.Wait();
        try { _writer?.Dispose(); _writer = null; }
        finally { _fileLock.Release(); _fileLock.Dispose(); }
    }
}
