using LogTail.Core.Models;
using System.Globalization;

namespace LogTail.Core.Services;

public sealed class LogFilter
{
    private readonly LogParser _parser;

    public LogFilter(LogParser parser)
    {
        _parser = parser;
    }

    public IEnumerable<string> ApplyFilters(IEnumerable<string> lines, LogTailOptions options)
    {
        var buffer = new List<string>();
        string? currentHeader = null;
        var currentBlock = new List<string>();
        var orphanedLines = new List<string>(); // Lines before the first header

        foreach (var line in lines)
        {
            if (_parser.IsLogHeader(line))
            {
                FlushBlock();
                currentHeader = line;
                currentBlock.Add(line);
            }
            else
            {
                if (currentHeader == null)
                {
                    // Lines before any header - keep track of them separately
                    orphanedLines.Add(line);
                }
                else
                {
                    currentBlock.Add(line);
                }
            }
        }

        FlushBlock();
        
        // Discard orphaned lines at the start (they're likely partial/clipped entries
        // from reading backwards in the file)
        
        return buffer;

        void FlushBlock()
        {
            if (currentHeader == null) return;

            if (MatchesLevel(currentHeader) &&
                MatchesText(currentBlock, options.Filter) &&
                MatchesDateTimeRange(currentHeader, options))
            {
                buffer.AddRange(currentBlock);
            }

            currentHeader = null;
            currentBlock.Clear();
        }

        bool MatchesLevel(string line)
        {
            if (options.Levels.Count == 0) return true;

            var levelText = _parser.GetLevelText(line);
            if (string.IsNullOrEmpty(levelText)) return false;

            return options.Levels.Contains(levelText);
        }

        static bool MatchesText(IEnumerable<string> block, string? filter)
        {
            if (string.IsNullOrWhiteSpace(filter)) return true;
            return block.Any(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        bool MatchesDateTimeRange(string line, LogTailOptions opts)
        {
            if (!opts.IsDateTimeFilterEnabled) return true;
            if (!opts.FromDateTime.HasValue && !opts.ToDateTime.HasValue) return true;

            var parsed = _parser.ParseLogLine(line);
            if (string.IsNullOrWhiteSpace(parsed.Timestamp)) return false;

            if (TryParseTimestamp(parsed.Timestamp, out DateTime logDateTime))
            {
                if (opts.FromDateTime.HasValue && logDateTime < opts.FromDateTime.Value)
                    return false;

                if (opts.ToDateTime.HasValue)
                {
                    // If To time is midnight (00:00:00), treat it as end of that day
                    var toDateTime = opts.ToDateTime.Value;
                    if (toDateTime.TimeOfDay == TimeSpan.Zero)
                    {
                        toDateTime = toDateTime.Date.AddDays(1).AddTicks(-1); // 23:59:59.9999999
                    }
                    
                    if (logDateTime > toDateTime)
                        return false;
                }

                return true;
            }

            // If we can't parse the timestamp, exclude it when date filter is active
            return false;
        }

        static bool TryParseTimestamp(string timestamp, out DateTime dateTime)
        {
            dateTime = DateTime.MinValue;

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
}
