using System;
using System.Collections.Generic;

namespace logtail.gui.Models;

/// <summary>
/// Represents the stored bookmarks and annotations for a log file
/// </summary>
public class BookmarkAnnotationData
{
    /// <summary>
    /// Path to the log file this data is associated with
    /// </summary>
    public string LogFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Last modification time of the log file when bookmarks were saved
    /// </summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>
    /// Collection of bookmarks and annotations
    /// </summary>
    public List<BookmarkAnnotationEntry> Entries { get; set; } = new();
}

/// <summary>
/// Represents a single bookmark or annotation entry
/// </summary>
public class BookmarkAnnotationEntry
{
    /// <summary>
    /// Unique identifier for this entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Hash of the log line for matching
    /// </summary>
    public string LineHash { get; set; } = string.Empty;
    
    /// <summary>
    /// The actual log line text (for display and fallback matching)
    /// </summary>
    public string LineText { get; set; } = string.Empty;
    
    /// <summary>
    /// Approximate line number (for reference, may change as file grows)
    /// </summary>
    public int LineNumber { get; set; }
    
    /// <summary>
    /// Whether this is a bookmark
    /// </summary>
    public bool IsBookmark { get; set; }
    
    /// <summary>
    /// Annotation text (null if no annotation)
    /// </summary>
    public string? Annotation { get; set; }
    
    /// <summary>
    /// When this entry was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// When this entry was last modified
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}
