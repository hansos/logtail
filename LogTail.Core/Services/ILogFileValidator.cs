namespace LogTail.Core.Services;

public interface ILogFileValidator
{
    /// <summary>
    /// Validates whether a file is a supported log file format.
    /// </summary>
    /// <param name="filePath">Path to the file to validate</param>
    /// <returns>Validation result indicating if the file is valid</returns>
    Task<ValidationResult> ValidateAsync(string filePath);
}
