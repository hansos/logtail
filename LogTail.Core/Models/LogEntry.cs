namespace LogTail.Core.Models;

public sealed class LogEntry
{
    public string Text { get; set; } = string.Empty;
    public LogLevel? Level { get; set; }
    public bool IsHeader { get; set; }
}

public enum LogLevel
{
    Verbose,
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
