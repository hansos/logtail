namespace LogTail.Core.Models;

public sealed class LogTailOptions
{
    public int TailLines { get; set; } = 10;
    public string FilePath { get; set; } = string.Empty;
    public HashSet<string> Levels { get; set; } = new();
    public string? Filter { get; set; }
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(5);
}
