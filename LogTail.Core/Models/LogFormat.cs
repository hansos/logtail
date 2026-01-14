namespace LogTail.Core.Models;

public sealed class LogFormat
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FullLogPattern { get; set; } = string.Empty;
    public string LevelPattern { get; set; } = string.Empty;
    public Dictionary<string, string> LevelMappings { get; set; } = new();
    public bool IsBuiltIn { get; set; }

    public static LogFormat CreateDefault()
    {
        return new LogFormat
        {
            Name = "Default",
            Description = "Default LogTail format with timestamp, level, source, and message",
            FullLogPattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+\[(?<level>[^\]]+)\]\s+(?<source>[^\s]+)\s+(?<message>.*)$",
            LevelPattern = @"\[(?<level>VERBOSE|DBUG|INFO|WARNING|ERROR|EROR|FATAL)\]",
            LevelMappings = new Dictionary<string, string>
            {
                { "VERBOSE", "Verbose" },
                { "DBUG", "Debug" },
                { "INFO", "Info" },
                { "WARNING", "Warning" },
                { "ERROR", "Error" },
                { "EROR", "Error" },
                { "FATAL", "Fatal" }
            },
            IsBuiltIn = true
        };
    }

    public static LogFormat CreateSerilog()
    {
        return new LogFormat
        {
            Name = "Serilog",
            Description = "Serilog default format",
            FullLogPattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d+\s+[+-]\d{2}:\d{2})\s+\[(?<level>[^\]]+)\]\s+(?<message>.*)$",
            LevelPattern = @"\[(?<level>VRB|DBG|INF|WRN|ERR|FTL)\]",
            LevelMappings = new Dictionary<string, string>
            {
                { "VRB", "Verbose" },
                { "DBG", "Debug" },
                { "INF", "Info" },
                { "WRN", "Warning" },
                { "ERR", "Error" },
                { "FTL", "Fatal" }
            },
            IsBuiltIn = true
        };
    }

    public static LogFormat CreateNLog()
    {
        return new LogFormat
        {
            Name = "NLog",
            Description = "NLog default format",
            FullLogPattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d+)\s+(?<level>[A-Z]+)\s+(?<source>[^\s]+)\s+(?<message>.*)$",
            LevelPattern = @"(?<level>TRACE|DEBUG|INFO|WARN|ERROR|FATAL)",
            LevelMappings = new Dictionary<string, string>
            {
                { "TRACE", "Verbose" },
                { "DEBUG", "Debug" },
                { "INFO", "Info" },
                { "WARN", "Warning" },
                { "ERROR", "Error" },
                { "FATAL", "Fatal" }
            },
            IsBuiltIn = true
        };
    }

    public static LogFormat CreateLog4Net()
    {
        return new LogFormat
        {
            Name = "log4net",
            Description = "log4net default format",
            FullLogPattern = @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2},\d+)\s+\[(?<level>[^\]]+)\]\s+(?<source>[^\s]+)\s+-\s+(?<message>.*)$",
            LevelPattern = @"\[(?<level>DEBUG|INFO|WARN|ERROR|FATAL)\]",
            LevelMappings = new Dictionary<string, string>
            {
                { "DEBUG", "Debug" },
                { "INFO", "Info" },
                { "WARN", "Warning" },
                { "ERROR", "Error" },
                { "FATAL", "Fatal" }
            },
            IsBuiltIn = true
        };
    }

    public static List<LogFormat> GetBuiltInFormats()
    {
        return new List<LogFormat>
        {
            CreateDefault(),
            CreateSerilog(),
            CreateNLog(),
            CreateLog4Net()
        };
    }
}
