namespace LogTail.Core.Services;

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ValidationFailureReason Reason { get; set; }
}

public enum ValidationFailureReason
{
    None,
    FileNotFound,
    FileIsDirectory,
    FileIsBinary,
    InvalidJsonStructure,
    NoLogPatternsDetected,
    FileAccessDenied,
    UnknownError
}
