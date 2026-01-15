using LogTail.Core.Models;

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
                MatchesText(currentBlock, options.Filter))
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
    }
}
