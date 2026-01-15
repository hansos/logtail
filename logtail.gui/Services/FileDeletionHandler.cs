using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using logtail.gui.Models;
using Serilog;

namespace logtail.gui.Services;

/// <summary>
/// Handles file deletion scenarios with automatic recreation detection
/// </summary>
public class FileDeletionHandler
{
    private readonly FileDeletionSettings _settings;
    private CancellationTokenSource? _waitCancellation;
    private int _retryCount = 0;
    private readonly ILogger _logger = Log.ForContext<FileDeletionHandler>();
    
    public event EventHandler<FileDeletionEventArgs>? FileDeleted;
    public event EventHandler<FileRecreatedEventArgs>? FileRecreated;
    public event EventHandler<DeletionTimeoutEventArgs>? TimeoutExceeded;
    public event EventHandler<DeletionStatusEventArgs>? StatusChanged;
    
    public int RetryCount => _retryCount;
    public bool IsWaiting => _waitCancellation != null && !_waitCancellation.IsCancellationRequested;
    
    public FileDeletionHandler(FileDeletionSettings settings)
    {
        _settings = settings;
    }
    
    /// <summary>
    /// Handles a file deletion event
    /// </summary>
    public async Task HandleFileDeletionAsync(FileRotationEventArgs deletionEvent)
    {
        _logger.Information("Handling file deletion for {FilePath}", deletionEvent.OldFile);
        
        // Notify of deletion
        FileDeleted?.Invoke(this, new FileDeletionEventArgs
        {
            FilePath = deletionEvent.OldFile ?? string.Empty,
            Timestamp = deletionEvent.Timestamp
        });
        
        // Update status
        StatusChanged?.Invoke(this, new DeletionStatusEventArgs
        {
            Status = "File deleted",
            IsWarning = true
        });
        
        // Determine action based on settings
        if (_settings.StopMonitoringImmediately)
        {
            _logger.Information("Stopping monitoring immediately as configured");
            StatusChanged?.Invoke(this, new DeletionStatusEventArgs
            {
                Status = "File not found | Monitoring stopped",
                IsError = true
            });
            return;
        }
        
        if (_settings.AutoWaitForRecreation)
        {
            _logger.Information("Auto-waiting for file recreation");
            await WaitForFileRecreationAsync(deletionEvent.OldFile ?? string.Empty);
        }
    }
    
    /// <summary>
    /// Waits for a deleted file to be recreated
    /// </summary>
    private async Task WaitForFileRecreationAsync(string filePath)
    {
        _waitCancellation = new CancellationTokenSource();
        _retryCount = 0;
        
        try
        {
            // Create timeout task if configured
            Task? timeoutTask = null;
            if (_settings.WaitTimeoutSeconds > 0)
            {
                timeoutTask = Task.Delay(
                    TimeSpan.FromSeconds(_settings.WaitTimeoutSeconds),
                    _waitCancellation.Token
                );
            }
            
            var checkTask = CheckForFileRecreationAsync(filePath);
            
            Task completedTask;
            if (timeoutTask != null)
            {
                completedTask = await Task.WhenAny(timeoutTask, checkTask);
            }
            else
            {
                // No timeout, just wait for file
                completedTask = checkTask;
                await completedTask;
            }
            
            if (completedTask == timeoutTask)
            {
                // Timeout exceeded
                _logger.Warning("Timeout exceeded waiting for file recreation after {Seconds} seconds", 
                    _settings.WaitTimeoutSeconds);
                
                TimeoutExceeded?.Invoke(this, new DeletionTimeoutEventArgs
                {
                    FilePath = filePath,
                    ElapsedSeconds = _settings.WaitTimeoutSeconds,
                    RetryAttempts = _retryCount
                });
                
                StatusChanged?.Invoke(this, new DeletionStatusEventArgs
                {
                    Status = "File not found | Monitoring stopped",
                    IsError = true
                });
            }
            else
            {
                // File was recreated
                var newFilePath = await checkTask;
                
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    _logger.Information("File recreated at {FilePath} after {Attempts} attempts", 
                        newFilePath, _retryCount);
                    
                    FileRecreated?.Invoke(this, new FileRecreatedEventArgs
                    {
                        FilePath = newFilePath,
                        ElapsedSeconds = _retryCount * _settings.CheckIntervalSeconds
                    });
                    
                    StatusChanged?.Invoke(this, new DeletionStatusEventArgs
                    {
                        Status = "File recreated - monitoring resumed",
                        IsNormal = true
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Wait for file recreation cancelled");
            StatusChanged?.Invoke(this, new DeletionStatusEventArgs
            {
                Status = "Monitoring cancelled",
                IsNormal = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while waiting for file recreation");
            StatusChanged?.Invoke(this, new DeletionStatusEventArgs
            {
                Status = $"Error: {ex.Message}",
                IsError = true
            });
        }
    }
    
    /// <summary>
    /// Checks periodically for file recreation
    /// </summary>
    private async Task<string?> CheckForFileRecreationAsync(string filePath)
    {
        var maxRetries = _settings.WaitTimeoutSeconds > 0 
            ? _settings.WaitTimeoutSeconds / _settings.CheckIntervalSeconds 
            : int.MaxValue;
        
        while (!_waitCancellation!.Token.IsCancellationRequested)
        {
            _retryCount++;
            
            // Update status with retry count
            var statusMessage = maxRetries < int.MaxValue
                ? $"File deleted | Waiting for file... ({_retryCount}/{maxRetries})"
                : $"File deleted | Waiting for file... ({_retryCount})";
            
            StatusChanged?.Invoke(this, new DeletionStatusEventArgs
            {
                Status = statusMessage,
                IsWarning = true,
                RetryCount = _retryCount
            });
            
            if (File.Exists(filePath))
            {
                // Wait a bit to ensure file is fully created
                await Task.Delay(500, _waitCancellation.Token);
                
                // Verify file still exists and is accessible
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            
            if (_retryCount >= maxRetries)
            {
                _logger.Warning("Max retries ({MaxRetries}) exceeded waiting for file recreation", maxRetries);
                break;
            }
            
            await Task.Delay(
                TimeSpan.FromSeconds(_settings.CheckIntervalSeconds),
                _waitCancellation.Token
            );
        }
        
        return null;
    }
    
    /// <summary>
    /// Cancels the wait for file recreation
    /// </summary>
    public void CancelWait()
    {
        _waitCancellation?.Cancel();
        _retryCount = 0;
    }
    
    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        _waitCancellation?.Cancel();
        _waitCancellation?.Dispose();
    }
}

/// <summary>
/// Event arguments for file deletion
/// </summary>
public class FileDeletionEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event arguments for file recreation
/// </summary>
public class FileRecreatedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public int ElapsedSeconds { get; set; }
}

/// <summary>
/// Event arguments for deletion timeout
/// </summary>
public class DeletionTimeoutEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public int ElapsedSeconds { get; set; }
    public int RetryAttempts { get; set; }
}

/// <summary>
/// Event arguments for deletion status changes
/// </summary>
public class DeletionStatusEventArgs : EventArgs
{
    public string Status { get; set; } = string.Empty;
    public bool IsWarning { get; set; }
    public bool IsError { get; set; }
    public bool IsNormal { get; set; }
    public int RetryCount { get; set; }
}
