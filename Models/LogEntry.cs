namespace GoVeeSmartDashboard.Models;
public sealed record LogEntry(int LogNumber, DateTime Timestamp, string Message)
{
    public string DisplayText => $"{LogNumber}, [{Timestamp:yyyy-MM-dd HH:mm:ss.fff}], {Message}";
}
