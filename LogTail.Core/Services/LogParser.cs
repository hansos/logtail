using System.Text.RegularExpressions;
using LogTail.Core.Models;

namespace LogTail.Core.Services;

public sealed class LogParser
{
    private static readonly Regex LevelRegex =
        new(@"\[(?<level>VERBOSE|DBUG|INFO|WARNING|ERROR|EROR|FATAL)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex FullLogRegex =
        new(@"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+\[(?<level>[^\]]+)\]\s+(?<source>[^\s]+)\s+(?<message>.*)$",
            RegexOptions.Compiled);

    public bool IsLogHeader(string line)
    {
        // ISO timestamp at start
        return line.Length > 19 &&
               char.IsDigit(line[0]) &&
               char.IsDigit(line[3]) &&
               LevelRegex.IsMatch(line);
    }

    public LogLevel? ExtractLogLevel(string line)
    {
        var match = LevelRegex.Match(line);
        if (!match.Success) return null;

        return match.Groups["level"].Value.ToUpperInvariant() switch
        {
            "VERBOSE" => LogLevel.Verbose,
            "DBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Info,
            "WARNING" => LogLevel.Warning,
            "ERROR" or "EROR" => LogLevel.Error,
            "FATAL" => LogLevel.Fatal,
            _ => null
        };
    }

    public string GetLevelText(string line)
    {
        var match = LevelRegex.Match(line);
        return match.Success ? match.Groups["level"].Value.ToUpperInvariant() : string.Empty;
    }

    public ParsedLogLine ParseLogLine(string line)
    {
        var match = FullLogRegex.Match(line);
        
        if (match.Success)
        {
            return new ParsedLogLine
            {
                Timestamp = match.Groups["timestamp"].Value,
                Level = match.Groups["level"].Value,
                Source = match.Groups["source"].Value,
                Message = match.Groups["message"].Value
            };
        }

        // Fallback parsing for non-standard format
        var levelMatch = LevelRegex.Match(line);
        if (levelMatch.Success)
        {
            var levelIndex = levelMatch.Index;
            var levelEndIndex = levelMatch.Index + levelMatch.Length;
            
            var timestamp = line.Length >= levelIndex ? line[..levelIndex].Trim() : string.Empty;
            var afterLevel = line.Length > levelEndIndex ? line[levelEndIndex..].Trim() : string.Empty;
            
            // Try to find source (first word after level)
            var parts = afterLevel.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var source = parts.Length > 0 ? parts[0] : string.Empty;
            var message = parts.Length > 1 ? parts[1] : string.Empty;

            return new ParsedLogLine
            {
                Timestamp = timestamp,
                Level = levelMatch.Groups["level"].Value,
                Source = source,
                Message = message
            };
        }

        // No level found - treat entire line as message
        return new ParsedLogLine
        {
            Timestamp = string.Empty,
            Level = string.Empty,
            Source = string.Empty,
            Message = line
        };
    }
}

public sealed class ParsedLogLine
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
