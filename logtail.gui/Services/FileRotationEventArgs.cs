using System;

namespace logtail.gui.Services;

/// <summary>
/// Event arguments for file rotation detection
/// </summary>
public class FileRotationEventArgs : EventArgs
{
    public RotationType RotationType { get; set; }
    public string? OldFile { get; set; }
    public string? NewFile { get; set; }
    public string DetectionMethod { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool RequiresUserAction { get; set; }
}

/// <summary>
/// Type of file rotation detected
/// </summary>
public enum RotationType
{
    Deleted,
    Renamed,
    Replaced,
    Recreated,
    Truncated,
    DeletedOrRenamed
}
