namespace GoVeeSmartDashboard.Models;
public sealed record LogEntry(int LogNumber, DateTimeOffset Timestamp, string Message)
{
    public string DisplayText => $"{LogNumber}| {Timestamp:yyyy-MM-dd HH:mm:ss.fff}| {Message}";
}
