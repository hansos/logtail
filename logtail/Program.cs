using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using LogTail.Core;
using LogTail.Core.Models;

internal class Program
{
    private static readonly Regex LevelRegex =
        new(@"\[(?<level>VERBOSE|DBUG|INFO|WARNING|ERROR|EROR|FATAL)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static void Main(string[] args)
    {
        var options = ParseArgs(args);

        Console.WriteLine($"Command line arguments:");
        Console.WriteLine($"  File: {options.FilePath}");
        Console.WriteLine($"  Tail lines: {options.TailLines}");
        Console.WriteLine($"  Refresh rate: {options.RefreshRate.TotalMilliseconds}ms");
        if (options.Levels.Count > 0)
            Console.WriteLine($"  Levels: {string.Join(", ", options.Levels)}");
        if (!string.IsNullOrWhiteSpace(options.Filter))
            Console.WriteLine($"  Filter: {options.Filter}");
        Console.WriteLine();

        var logTailService = new LogTailService();

        try
        {
            // Validate file exists
            var _ = logTailService.GetFilteredLogs(options).ToList();
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return;
        }

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Environment.Exit(0);
        };

        List<string>? previousOutput = null;

        while (true)
        {
            var filtered = logTailService.GetFilteredLogs(options).ToList();

            if (previousOutput == null || !filtered.SequenceEqual(previousOutput))
            {
                Console.Clear();

                foreach (var entry in filtered)
                {
                    WriteColored(entry);
                }

                previousOutput = filtered;
            }

            Thread.Sleep(options.RefreshRate);
        }
    }

    private static void WriteColored(string line)
    {
        var match = LevelRegex.Match(line);
        if (!match.Success)
        {
            Console.ResetColor();
            Console.WriteLine(line);
            return;
        }

        Console.ForegroundColor = match.Groups["level"].Value.ToUpperInvariant() switch
        {
            "VERBOSE" or "DBUG" => ConsoleColor.DarkGray,
            "INFO" => ConsoleColor.White,
            "WARNING" => ConsoleColor.Yellow,
            "ERROR" or "EROR" => ConsoleColor.Red,
            "FATAL" => ConsoleColor.Magenta,
            _ => Console.ForegroundColor
        };

        Console.WriteLine(line);
        Console.ResetColor();
    }

    private static LogTailOptions ParseArgs(string[] args)
    {
        var opt = new LogTailOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--tail":
                    opt.TailLines = int.Parse(args[++i]);
                    break;

                case "--file":
                    opt.FilePath = args[++i];
                    break;

                case "--level":
                    opt.Levels = args[++i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim().ToUpperInvariant())
                        .ToHashSet();
                    break;

                case "--filter":
                    opt.Filter = args[++i];
                    break;

                case "--refreshrate":
                    opt.RefreshRate = ParseDuration(args[++i]);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(opt.FilePath))
            throw new ArgumentException("--file is required");

        return opt;
    }

    private static TimeSpan ParseDuration(string value)
    {
        if (value.EndsWith("ms"))
            return TimeSpan.FromMilliseconds(int.Parse(value[..^2]));
        if (value.EndsWith("s"))
            return TimeSpan.FromSeconds(int.Parse(value[..^1]));
        if (value.EndsWith("m"))
            return TimeSpan.FromMinutes(int.Parse(value[..^1]));

        throw new FormatException($"Invalid duration: {value}");
    }
}
