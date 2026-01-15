using System;
using System.IO;
using System.Windows.Threading;
using LogTail.Core.Models;
using Serilog;
using ILogger = Serilog.ILogger;

namespace logtail.gui.Services;

public class FileMonitorService : IDisposable
{
    private readonly DispatcherTimer _debounceTimer;
    private FileSystemWatcher? _watcher;
    private string? _filePath;
    private readonly ILogger _logger = Log.ForContext<FileMonitorService>();
    private bool _isPaused;
    private int _bufferedChangeCount;

    public event EventHandler? FileChanged;
    public event EventHandler? FileDeleted;
    public event EventHandler? BufferedCountChanged;

    public bool IsWatcherActive { get; private set; }
    public MonitoringMode ActiveMode { get; private set; } = MonitoringMode.Auto;
    
    public bool IsPaused
    {
        get => _isPaused;
        private set
        {
            if (_isPaused != value)
            {
                _isPaused = value;
                if (!_isPaused)
                {
                    _bufferedChangeCount = 0;
                    BufferedCountChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
    
    public int BufferedCount => _bufferedChangeCount;

    public FileMonitorService()
    {
        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;
    }

    public void StartMonitoring(string filePath, MonitoringMode mode)
    {
        _filePath = filePath;
        ActiveMode = mode;
        StopMonitoring();
        
        // Reset pause state when starting new monitoring
        _isPaused = false;
        _bufferedChangeCount = 0;

        var usePolling = mode == MonitoringMode.PollingOnly || IsNetworkPath(filePath);
        if (usePolling)
        {
            IsWatcherActive = false;
            _logger.Information("Using polling mode for file {FilePath}", filePath);
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
            {
                IsWatcherActive = false;
                _logger.Warning("Invalid file path provided to StartMonitoring: {FilePath}", filePath);
                return;
            }

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };

            _watcher.Changed += OnWatcherChanged;
            _watcher.Created += OnWatcherChanged;
            _watcher.Renamed += OnWatcherRenamed;
            _watcher.Deleted += OnWatcherDeleted;

            _watcher.EnableRaisingEvents = true;
            IsWatcherActive = true;
            _logger.Information("Started FileSystemWatcher for {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            IsWatcherActive = false;
            _logger.Error(ex, "Failed to start FileSystemWatcher for {FilePath}", filePath);
        }
    }

    public void StopMonitoring()
    {
        _debounceTimer.Stop();

        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnWatcherChanged;
            _watcher.Created -= OnWatcherChanged;
            _watcher.Renamed -= OnWatcherRenamed;
            _watcher.Deleted -= OnWatcherDeleted;
            _watcher.Dispose();
            _watcher = null;
            _logger.Information("Stopped FileSystemWatcher for {FilePath}", _filePath);
        }

        IsWatcherActive = false;
    }

    private void OnWatcherChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsSameFile(e.FullPath))
            return;

        _logger.Debug("File change event received for {FilePath}", e.FullPath);
        
        if (_isPaused)
        {
            _bufferedChangeCount++;
            BufferedCountChanged?.Invoke(this, EventArgs.Empty);
            _logger.Debug("File change buffered while paused. Buffered count: {Count}", _bufferedChangeCount);
            return;
        }
        
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void OnWatcherDeleted(object sender, FileSystemEventArgs e)
    {
        if (!IsSameFile(e.FullPath))
            return;

        _logger.Information("File deleted: {FilePath}", e.FullPath);
        FileDeleted?.Invoke(this, EventArgs.Empty);
    }

    private void OnWatcherRenamed(object sender, RenamedEventArgs e)
    {
        if (!IsSameFile(e.OldFullPath))
            return;

        _logger.Information("File renamed from {OldPath} to {NewPath}", e.OldFullPath, e.FullPath);
        FileDeleted?.Invoke(this, EventArgs.Empty);
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        _logger.Debug("Debounce tick fired for {FilePath}", _filePath);
        FileChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool IsSameFile(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(_filePath), Path.GetFullPath(fullPath), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNetworkPath(string path)
    {
        try
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                if (uri.IsUnc)
                {
                    return true;
                }
            }

            var root = Path.GetPathRoot(path);
            if (!string.IsNullOrEmpty(root))
            {
                var drive = new DriveInfo(root);
                return drive.DriveType == DriveType.Network;
            }
        }
        catch (Exception ex)
        {
            Log.ForContext<FileMonitorService>().Warning(ex, "Failed to determine if path is network: {Path}", path);
            // Ignore errors and treat as local path
        }

        return false;
    }

    public void Dispose()
    {
        StopMonitoring();
        _debounceTimer.Tick -= DebounceTimer_Tick;
    }
    
    public void Pause()
    {
        if (_isPaused)
            return;
            
        IsPaused = true;
        _bufferedChangeCount = 0;
        _logger.Information("Monitoring paused for {FilePath}", _filePath);
    }
    
    public void Resume()
    {
        if (!_isPaused)
            return;
            
        var bufferedCount = _bufferedChangeCount;
        IsPaused = false;
        _logger.Information("Monitoring resumed for {FilePath}. Buffered changes: {Count}", _filePath, bufferedCount);
        
        // Trigger a refresh to catch up with buffered changes
        if (bufferedCount > 0)
        {
            FileChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
