using System.Windows.Media;
using LogTail.Core.Models;
using LogTail.Core.Services;

namespace logtail.gui.ViewModels;

public class LogEntryViewModel
{
    public string Text { get; set; } = string.Empty;
    public LogLevel? Level { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string LevelText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public Brush Foreground => Level switch
    {
        LogLevel.Verbose => Brushes.DarkGray,
        LogLevel.Debug => Brushes.DarkGray,
        LogLevel.Info => Brushes.White,
        LogLevel.Warning => Brushes.Yellow,
        LogLevel.Error => Brushes.Red,
        LogLevel.Fatal => Brushes.Magenta,
        _ => Brushes.White
    };

    public static LogEntryViewModel FromText(string line)
    {
        var parser = new LogParser();
        var parsed = parser.ParseLogLine(line);
        
        return new LogEntryViewModel
        {
            Text = line,
            Timestamp = parsed.Timestamp,
            LevelText = parsed.Level,
            Source = parsed.Source,
            Message = parsed.Message,
            Level = ExtractLevel(parsed.Level)
        };
    }

    private static LogLevel? ExtractLevel(string levelText)
    {
        if (string.IsNullOrEmpty(levelText))
            return null;

        return levelText.ToUpperInvariant() switch
        {
            "VERBOSE" => LogLevel.Verbose,
            "DBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Info,
            "WARNING" or "WARN" => LogLevel.Warning,
            "ERROR" or "EROR" => LogLevel.Error,
            "FATAL" => LogLevel.Fatal,
            _ => null
        };
    }
}
