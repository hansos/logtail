using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using logtail.gui.Models;

namespace logtail.gui.Services;

/// <summary>
/// Manages bookmarks and annotations for log files, storing them in sidecar JSON files
/// </summary>
public class BookmarkAnnotationManager
{
    private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<BookmarkAnnotationManager>();
    private BookmarkAnnotationData? _currentData;
    private string? _currentLogFilePath;

    /// <summary>
    /// Gets the sidecar file path for a log file
    /// </summary>
    private string GetSidecarFilePath(string logFilePath)
    {
        return logFilePath + ".logtail.json";
    }

    /// <summary>
    /// Computes a hash for a log line to use for matching
    /// </summary>
    public static string ComputeLineHash(string lineText)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(lineText));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Loads bookmarks and annotations for a log file
    /// </summary>
    public BookmarkAnnotationData LoadForFile(string logFilePath)
    {
        _currentLogFilePath = logFilePath;
        var sidecarPath = GetSidecarFilePath(logFilePath);

        try
        {
            if (File.Exists(sidecarPath))
            {
                var json = File.ReadAllText(sidecarPath);
                var data = JsonSerializer.Deserialize<BookmarkAnnotationData>(json);
                
                if (data != null)
                {
                    _currentData = data;
                    _logger.Information("Loaded {Count} bookmark/annotation entries from {Path}", 
                        data.Entries.Count, sidecarPath);
                    return _currentData;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error loading bookmarks/annotations from {Path}", sidecarPath);
        }

        // Return empty data if file doesn't exist or there was an error
        _currentData = new BookmarkAnnotationData
        {
            LogFilePath = logFilePath,
            LastModified = File.Exists(logFilePath) ? File.GetLastWriteTimeUtc(logFilePath) : DateTime.UtcNow
        };
        return _currentData;
    }

    /// <summary>
    /// Saves bookmarks and annotations for the current log file
    /// </summary>
    public void SaveCurrent()
    {
        if (_currentData == null || string.IsNullOrEmpty(_currentLogFilePath))
            return;

        var sidecarPath = GetSidecarFilePath(_currentLogFilePath);

        try
        {
            // Update last modified time
            if (File.Exists(_currentLogFilePath))
            {
                _currentData.LastModified = File.GetLastWriteTimeUtc(_currentLogFilePath);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_currentData, options);
            File.WriteAllText(sidecarPath, json);
            
            _logger.Information("Saved {Count} bookmark/annotation entries to {Path}", 
                _currentData.Entries.Count, sidecarPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving bookmarks/annotations to {Path}", sidecarPath);
        }
    }

    /// <summary>
    /// Toggles bookmark for a log line
    /// </summary>
    public bool ToggleBookmark(string lineText, int lineNumber)
    {
        if (_currentData == null)
            return false;

        var lineHash = ComputeLineHash(lineText);
        var existing = _currentData.Entries.FirstOrDefault(e => e.LineHash == lineHash);

        if (existing != null)
        {
            if (existing.IsBookmark)
            {
                // Remove bookmark - if no annotation, remove entry entirely
                if (string.IsNullOrWhiteSpace(existing.Annotation))
                {
                    _currentData.Entries.Remove(existing);
                    return false;
                }
                else
                {
                    existing.IsBookmark = false;
                    existing.ModifiedAt = DateTime.Now;
                    return false;
                }
            }
            else
            {
                // Add bookmark to existing annotation entry
                existing.IsBookmark = true;
                existing.ModifiedAt = DateTime.Now;
                return true;
            }
        }
        else
        {
            // Create new bookmark entry
            _currentData.Entries.Add(new BookmarkAnnotationEntry
            {
                LineHash = lineHash,
                LineText = lineText,
                LineNumber = lineNumber,
                IsBookmark = true,
                Annotation = null
            });
            return true;
        }
    }

    /// <summary>
    /// Sets or removes an annotation for a log line
    /// </summary>
    public void SetAnnotation(string lineText, int lineNumber, string? annotation)
    {
        if (_currentData == null)
            return;

        var lineHash = ComputeLineHash(lineText);
        var existing = _currentData.Entries.FirstOrDefault(e => e.LineHash == lineHash);

        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(annotation))
            {
                // Remove annotation - if no bookmark, remove entry entirely
                if (!existing.IsBookmark)
                {
                    _currentData.Entries.Remove(existing);
                }
                else
                {
                    existing.Annotation = null;
                    existing.ModifiedAt = DateTime.Now;
                }
            }
            else
            {
                existing.Annotation = annotation;
                existing.ModifiedAt = DateTime.Now;
            }
        }
        else if (!string.IsNullOrWhiteSpace(annotation))
        {
            // Create new annotation entry
            _currentData.Entries.Add(new BookmarkAnnotationEntry
            {
                LineHash = lineHash,
                LineText = lineText,
                LineNumber = lineNumber,
                IsBookmark = false,
                Annotation = annotation
            });
        }
    }

    /// <summary>
    /// Gets the bookmark/annotation entry for a log line
    /// </summary>
    public BookmarkAnnotationEntry? GetEntry(string lineText)
    {
        if (_currentData == null)
            return null;

        var lineHash = ComputeLineHash(lineText);
        return _currentData.Entries.FirstOrDefault(e => e.LineHash == lineHash);
    }

    /// <summary>
    /// Checks if a line is bookmarked
    /// </summary>
    public bool IsBookmarked(string lineText)
    {
        var entry = GetEntry(lineText);
        return entry?.IsBookmark == true;
    }

    /// <summary>
    /// Gets the annotation for a line
    /// </summary>
    public string? GetAnnotation(string lineText)
    {
        return GetEntry(lineText)?.Annotation;
    }

    /// <summary>
    /// Gets all bookmarked entries
    /// </summary>
    public List<BookmarkAnnotationEntry> GetAllBookmarks()
    {
        if (_currentData == null)
            return new List<BookmarkAnnotationEntry>();

        return _currentData.Entries.Where(e => e.IsBookmark).OrderBy(e => e.LineNumber).ToList();
    }

    /// <summary>
    /// Clears all bookmarks and annotations
    /// </summary>
    public void Clear()
    {
        if (_currentData != null)
        {
            _currentData.Entries.Clear();
        }
    }

    /// <summary>
    /// Exports bookmarks and annotations to a file
    /// </summary>
    public void ExportToFile(string exportPath)
    {
        if (_currentData == null)
            return;

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_currentData, options);
            File.WriteAllText(exportPath, json);
            
            _logger.Information("Exported bookmarks/annotations to {Path}", exportPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error exporting bookmarks/annotations to {Path}", exportPath);
            throw;
        }
    }

    /// <summary>
    /// Imports bookmarks and annotations from a file
    /// </summary>
    public void ImportFromFile(string importPath)
    {
        try
        {
            var json = File.ReadAllText(importPath);
            var data = JsonSerializer.Deserialize<BookmarkAnnotationData>(json);
            
            if (data != null)
            {
                _currentData = data;
                _logger.Information("Imported {Count} bookmark/annotation entries from {Path}", 
                    data.Entries.Count, importPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error importing bookmarks/annotations from {Path}", importPath);
            throw;
        }
    }
}
