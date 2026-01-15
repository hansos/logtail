namespace logtail.gui.Models;

/// <summary>
/// Settings for file rotation and deletion handling
/// </summary>
public class FileRotationSettings
{
    /// <summary>
    /// Whether to automatically detect file rotation
    /// </summary>
    public bool AutoDetect { get; set; } = true;
    
    /// <summary>
    /// Interval in seconds to check for rotation
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Whether to show notification when rotation is detected
    /// </summary>
    public bool ShowNotification { get; set; } = true;
    
    /// <summary>
    /// Whether to log rotation events
    /// </summary>
    public bool LogRotationEvents { get; set; } = true;
    
    /// <summary>
    /// Retry interval in seconds if new file is not found
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 2;
    
    /// <summary>
    /// Maximum number of retries (0 = infinite)
    /// </summary>
    public int MaxRetries { get; set; } = 30;
    
    /// <summary>
    /// Settings specific to file deletion handling
    /// </summary>
    public FileDeletionSettings Deletion { get; set; } = new();
}

/// <summary>
/// Settings for handling file deletion scenarios
/// </summary>
public class FileDeletionSettings
{
    /// <summary>
    /// Whether to show warning dialog when file is deleted
    /// </summary>
    public bool ShowWarning { get; set; } = true;
    
    /// <summary>
    /// Whether to automatically wait for file to be recreated
    /// </summary>
    public bool AutoWaitForRecreation { get; set; } = true;
    
    /// <summary>
    /// Timeout in seconds to wait for file recreation (0 = wait indefinitely)
    /// </summary>
    public int WaitTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Whether to prompt user for action on deletion
    /// </summary>
    public bool PromptUserOnDeletion { get; set; } = false;
    
    /// <summary>
    /// Whether to stop monitoring immediately when file is deleted
    /// </summary>
    public bool StopMonitoringImmediately { get; set; } = false;
    
    /// <summary>
    /// Interval in seconds between checks for file recreation
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 2;
}
