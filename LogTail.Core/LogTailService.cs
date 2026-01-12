using LogTail.Core.Models;
using LogTail.Core.Services;

namespace LogTail.Core;

public sealed class LogTailService
{
    private readonly LogReader _reader;
    private readonly LogParser _parser;
    private readonly LogFilter _filter;

    public LogTailService()
    {
        _reader = new LogReader();
        _parser = new LogParser();
        _filter = new LogFilter(_parser);
    }

    public IEnumerable<string> GetFilteredLogs(LogTailOptions options)
    {
        if (!File.Exists(options.FilePath))
        {
            throw new FileNotFoundException($"File not found: {options.FilePath}", options.FilePath);
        }

        var lines = _reader.ReadLastLines(options.FilePath, options.TailLines);
        return _filter.ApplyFilters(lines, options);
    }

    public LogParser Parser => _parser;
}
