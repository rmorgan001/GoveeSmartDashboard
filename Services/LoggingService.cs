using Avalonia.Threading;
using GoVeeSmartDashboard.Models;
using System.Collections.ObjectModel;

namespace GoVeeSmartDashboard.Services;

/// <summary>Shared UI/file logger with daily noon-to-noon file rollover.</summary>
public sealed class LoggingService : IDisposable
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _defaultDirectory;
    private StreamWriter? _writer;
    private string? _activeFilePath;
    private bool _disposed;
    private readonly Lock _sync = new();
    public int LogNumber;

    public ObservableCollection<LogEntry> Entries { get; } = [];
    public event EventHandler<string>? FilePathChanged;

    public LoggingService()
    {
        _defaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GoVeeSmartDashboard", "Logs");
        Directory.CreateDirectory(_defaultDirectory);
        LogFilePath = Path.Combine(_defaultDirectory, $"GoVeeSession{DateTime.Now:yyyy-MM-dd_HH}.log");

        var cutoffDate = DateTime.Now.AddDays(-14);
        if (Directory.Exists(_defaultDirectory))
        {
            var dirInfo = new DirectoryInfo(_defaultDirectory);
    
            foreach (var file in dirInfo.GetFiles())
            {
                // Use LastWriteTime to check when the file was last modified
                if (file.LastWriteTime >= cutoffDate) continue;
                file.Delete();
                Console.WriteLine($"Deleted: {file.Name}");
            }
        }
        else
        {
            LogMessage($"Missing: {_defaultDirectory}");
        }
    }

    public string LogFilePath { get; private set; }

    public bool LogToFile
    {
        get { lock (_sync) return field; }
        set
        {
            lock (_sync) field = value;
            if (!value) CloseWriterAsync().GetAwaiter().GetResult();
        }
    } = true;

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

    public void LogMessage(string message)
    {
        LogNumber++;
        var now = DateTimeOffset.Now;
        var entry = new LogEntry(LogNumber, now, message);
        Entries.Add(entry);
        LogAsync(entry).GetAwaiter().GetResult();
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
            var targetPath = GetEffectiveFilePath();
            await EnsureWriterAsync(targetPath, cancellationToken);
            await _writer!.WriteLineAsync(entry.DisplayText.AsMemory(), cancellationToken);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { _fileLock.Release(); }
    }

    public void ClearVisibleLog() => Dispatcher.UIThread.Post(Entries.Clear);

    private string GetEffectiveFilePath()
    {
        lock (_sync)
        {
            var directory = Path.GetDirectoryName(LogFilePath) ?? _defaultDirectory;
            var extension = Path.GetExtension(LogFilePath);
            var stem = Path.GetFileNameWithoutExtension(LogFilePath);
            return Path.Combine(directory, $"{stem}_{extension}");
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
