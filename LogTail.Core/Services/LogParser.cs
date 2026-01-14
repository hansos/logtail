using System.Text.RegularExpressions;
using LogTail.Core.Models;

namespace LogTail.Core.Services;

public sealed class LogParser
{
    private Regex _levelRegex;
    private Regex _fullLogRegex;
    private LogFormat _currentFormat;

    public LogParser(LogFormat? format = null)
    {
        _currentFormat = format ?? LogFormat.CreateDefault();
        UpdateRegexPatterns();
    }

    public LogFormat CurrentFormat => _currentFormat;

    public void SetFormat(LogFormat format)
    {
        _currentFormat = format;
        UpdateRegexPatterns();
    }

    private void UpdateRegexPatterns()
    {
        _levelRegex = new Regex(_currentFormat.LevelPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        _fullLogRegex = new Regex(_currentFormat.FullLogPattern, RegexOptions.Compiled);
    }

    public bool IsLogHeader(string line)
    {
        // ISO timestamp at start
        return line.Length > 19 &&
               char.IsDigit(line[0]) &&
               char.IsDigit(line[3]) &&
               _levelRegex.IsMatch(line);
    }

    public LogLevel? ExtractLogLevel(string line)
    {
        var match = _levelRegex.Match(line);
        if (!match.Success) return null;

        var levelText = match.Groups["level"].Value.ToUpperInvariant();
        
        // Check if there's a mapping for this level
        if (_currentFormat.LevelMappings.TryGetValue(levelText, out var mappedLevel))
        {
            return mappedLevel switch
            {
                "Verbose" => LogLevel.Verbose,
                "Debug" => LogLevel.Debug,
                "Info" => LogLevel.Info,
                "Warning" => LogLevel.Warning,
                "Error" => LogLevel.Error,
                "Fatal" => LogLevel.Fatal,
                _ => null
            };
        }

        return null;
    }

    public string GetLevelText(string line)
    {
        var match = _levelRegex.Match(line);
        return match.Success ? match.Groups["level"].Value.ToUpperInvariant() : string.Empty;
    }

    public ParsedLogLine ParseLogLine(string line)
    {
        var match = _fullLogRegex.Match(line);
        
        if (match.Success)
        {
            var timestamp = match.Groups["timestamp"].Success ? match.Groups["timestamp"].Value : string.Empty;
            var level = match.Groups["level"].Success ? match.Groups["level"].Value : string.Empty;
            var source = match.Groups["source"].Success ? match.Groups["source"].Value : string.Empty;
            var message = match.Groups["message"].Success ? match.Groups["message"].Value : string.Empty;

            return new ParsedLogLine
            {
                Timestamp = timestamp,
                Level = level,
                Source = source,
                Message = message
            };
        }

        // Fallback parsing for non-standard format
        var levelMatch = _levelRegex.Match(line);
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
