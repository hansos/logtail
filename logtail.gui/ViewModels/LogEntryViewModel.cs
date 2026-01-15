using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;
using LogTail.Core.Models;
using LogTail.Core.Services;

namespace logtail.gui.ViewModels;

public class LogEntryViewModel
{
    private static readonly Regex FilePathPattern = new(
        @"(?:in\s+)?([a-zA-Z]:\\[^:]+\.cs):line\s+(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Text { get; set; } = string.Empty;
    public LogLevel? Level { get; set; }
    public string Timestamp { get; set; } = string.Empty;
    public string LevelText { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    
    public string? FilePath { get; private set; }
    public int? LineNumber { get; private set; }
    public bool HasFileReference => !string.IsNullOrEmpty(FilePath) && LineNumber.HasValue;
    
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

    public static LogEntryViewModel FromText(string line, LogParser? parser = null)
    {
        parser ??= new LogParser();
        var parsed = parser.ParseLogLine(line);
        
        var viewModel = new LogEntryViewModel
        {
            Text = line,
            Timestamp = parsed.Timestamp,
            LevelText = parsed.Level,
            Source = parsed.Source,
            Message = parsed.Message,
            Level = parser.ExtractLogLevel(line)
        };

        // Extract file path and line number if present
        var match = FilePathPattern.Match(line);
        if (match.Success)
        {
            viewModel.FilePath = match.Groups[1].Value;
            if (int.TryParse(match.Groups[2].Value, out int lineNum))
            {
                viewModel.LineNumber = lineNum;
            }
        }

        return viewModel;
    }

    public static bool TryParseTimestamp(string timestamp, out DateTime dateTime)
    {
        dateTime = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(timestamp))
            return false;

        // Try common timestamp formats
        string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(timestamp, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                return true;
        }

        // Fallback to general parsing
        return DateTime.TryParse(timestamp, out dateTime);
    }
}


