using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogTail.Core.Models;

namespace LogTail.Core.Services;

public sealed class LogFileValidator : ILogFileValidator
{
    private const int MaxLinesToSample = 100;
    private const int MaxBytesToRead = 1024 * 100; // 100 KB
    private const double MinPatternMatchThreshold = 0.10; // 10% of lines must match
    
    private readonly LogFormatService _formatService;
    
    // Common patterns for log file detection
    private static readonly Regex TimestampPattern = new(
        @"\d{4}[-/]\d{2}[-/]\d{2}[\sT]\d{2}:\d{2}:\d{2}|\d{2}:\d{2}:\d{2}",
        RegexOptions.Compiled);
    
    private static readonly Regex LogLevelPattern = new(
        @"\b(TRACE|DEBUG|DBUG|DBG|INFO|INF|WARN|WRN|WARNING|ERROR|ERR|EROR|FATAL|FTL|VERBOSE|VRB)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public LogFileValidator()
    {
        _formatService = new LogFormatService();
    }

    public async Task<ValidationResult> ValidateAsync(string filePath)
    {
        try
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File not found.",
                    Reason = ValidationFailureReason.FileNotFound
                };
            }

            // Check if it's a directory
            var attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "The specified path is a directory, not a file.",
                    Reason = ValidationFailureReason.FileIsDirectory
                };
            }

            // Try to read the file
            byte[] buffer;
            try
            {
                using var stream = File.OpenRead(filePath);
                var bytesToRead = (int)Math.Min(stream.Length, MaxBytesToRead);
                buffer = new byte[bytesToRead];
                await stream.ReadAsync(buffer, 0, bytesToRead);
            }
            catch (UnauthorizedAccessException)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Access denied. You do not have permission to read this file.",
                    Reason = ValidationFailureReason.FileAccessDenied
                };
            }

            // Check if file is binary
            if (IsBinaryFile(buffer))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "This appears to be a binary file, not a text log file.",
                    Reason = ValidationFailureReason.FileIsBinary
                };
            }

            // Decode to text
            string content;
            try
            {
                content = Encoding.UTF8.GetString(buffer);
            }
            catch
            {
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Unable to read file as text.",
                    Reason = ValidationFailureReason.FileIsBinary
                };
            }

            // Check if it's JSON
            if (content.TrimStart().StartsWith("{") || content.TrimStart().StartsWith("["))
            {
                return await ValidateJsonLogFileAsync(content);
            }

            // Validate as text log file
            return ValidateTextLogFile(content);
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"An error occurred while validating the file: {ex.Message}",
                Reason = ValidationFailureReason.UnknownError
            };
        }
    }

    private bool IsBinaryFile(byte[] buffer)
    {
        // Check for null bytes or high percentage of non-printable characters
        int nullBytes = 0;
        int nonPrintable = 0;
        
        foreach (var b in buffer)
        {
            if (b == 0)
            {
                nullBytes++;
            }
            else if (b < 9 || (b > 13 && b < 32 && b != 27))
            {
                nonPrintable++;
            }
        }

        // If more than 1% null bytes, it's likely binary
        if (nullBytes > buffer.Length * 0.01)
            return true;

        // If more than 30% non-printable characters, it's likely binary
        if (nonPrintable > buffer.Length * 0.30)
            return true;

        return false;
    }

    private async Task<ValidationResult> ValidateJsonLogFileAsync(string content)
    {
        try
        {
            // Try to parse as JSON
            using var doc = JsonDocument.Parse(content);
            
            // Check if it's an array or object
            var root = doc.RootElement;
            
            // For arrays, check the first few elements
            if (root.ValueKind == JsonValueKind.Array)
            {
                int validElements = 0;
                int totalElements = 0;
                
                foreach (var element in root.EnumerateArray())
                {
                    totalElements++;
                    if (totalElements > MaxLinesToSample)
                        break;
                    
                    // Check if element has common log properties
                    if (HasLogProperties(element))
                    {
                        validElements++;
                    }
                }
                
                if (totalElements > 0 && (double)validElements / totalElements >= MinPatternMatchThreshold)
                {
                    return new ValidationResult { IsValid = true };
                }
            }
            // For objects, check if it has log-like properties
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (HasLogProperties(root))
                {
                    return new ValidationResult { IsValid = true };
                }
            }
            
            // JSON is valid but doesn't look like a log file
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "JSON file does not appear to contain log entries.",
                Reason = ValidationFailureReason.InvalidJsonStructure
            };
        }
        catch (JsonException)
        {
            // Not valid JSON, might still be a text log file with { or [ in it
            return ValidateTextLogFile(content);
        }
    }

    private bool HasLogProperties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        // Common log properties
        var commonProps = new[] { "timestamp", "time", "date", "level", "severity", "message", "msg", "log", "text" };
        
        int matchCount = 0;
        foreach (var prop in element.EnumerateObject())
        {
            var propName = prop.Name.ToLowerInvariant();
            if (commonProps.Contains(propName))
            {
                matchCount++;
            }
        }
        
        // Require at least 2 common properties
        return matchCount >= 2;
    }

    private ValidationResult ValidateTextLogFile(string content)
    {
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var linesToCheck = Math.Min(lines.Length, MaxLinesToSample);
        
        if (linesToCheck == 0)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "File appears to be empty.",
                Reason = ValidationFailureReason.NoLogPatternsDetected
            };
        }

        int matchCount = 0;
        var formats = LogFormat.GetBuiltInFormats();
        
        for (int i = 0; i < linesToCheck; i++)
        {
            var line = lines[i];
            
            // Check if line matches any built-in format
            bool matchesFormat = false;
            foreach (var format in formats)
            {
                try
                {
                    var parser = new LogParser(format);
                    if (parser.IsLogHeader(line))
                    {
                        matchesFormat = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore regex errors
                }
            }
            
            if (matchesFormat)
            {
                matchCount++;
                continue;
            }
            
            // Fallback: check for common log patterns
            if (TimestampPattern.IsMatch(line) && LogLevelPattern.IsMatch(line))
            {
                matchCount++;
            }
        }

        var matchPercentage = (double)matchCount / linesToCheck;
        
        if (matchPercentage >= MinPatternMatchThreshold)
        {
            return new ValidationResult { IsValid = true };
        }

        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = $"File does not appear to contain recognized log patterns. Only {matchPercentage:P0} of sampled lines matched known formats.",
            Reason = ValidationFailureReason.NoLogPatternsDetected
        };
    }
}
