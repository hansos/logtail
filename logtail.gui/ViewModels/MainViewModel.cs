using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LogTail.Core;
using LogTail.Core.Models;
using LogTail.Core.Services;
using logtail.gui.Services;
using logtail.gui.Models;
using logtail.gui.Collections;

namespace logtail.gui.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly LogTailService _logTailService;
    private readonly LogFormatService _logFormatService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly RecentFilesManager _recentFilesManager;
    private readonly SettingsManager _settingsManager;
    private readonly FileMonitorService _fileMonitorService;
    private readonly ILogFileValidator _fileValidator;
    private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<MainViewModel>();
    private LogTailOptions _options;
    private string _statusText = "Ready";
    private Brush _statusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
    private int _logCount;
    private List<string>? _previousOutput;
    private HashSet<string> _availableSources = new();
    private HashSet<string> _selectedSources = new();
    private bool _isBusy;
    private bool _isMonitoringPaused;
    private int _bufferedEntryCount;
    private string _defaultStatusBarColor = "#007ACC";
    private string _pausedStatusBarColor = "#FFA500";

    public BulkObservableCollection<LogEntryViewModel> LogEntries { get; } = new();
    public ObservableCollection<string> RecentFiles { get; } = new();

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public Brush StatusBarBackground
    {
        get => _statusBarBackground;
        set
        {
            _statusBarBackground = value;
            OnPropertyChanged();
        }
    }

    public int LogCount
    {
        get => _logCount;
        set
        {
            _logCount = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsMonitoringPaused
    {
        get => _isMonitoringPaused;
        set
        {
            _isMonitoringPaused = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PauseResumeButtonText));
            OnPropertyChanged(nameof(PauseResumeButtonIcon));
            UpdateStatusBarColor();
            UpdateStatusText();
        }
    }
    
    public int BufferedEntryCount
    {
        get => _bufferedEntryCount;
        set
        {
            _bufferedEntryCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BufferedCountText));
            UpdateStatusText();
        }
    }
    
    public string PauseResumeButtonText => IsMonitoringPaused ? "Resume" : "Pause";
    
    public string PauseResumeButtonIcon => IsMonitoringPaused ? "?" : "?";
    
    public string BufferedCountText => BufferedEntryCount > 0 ? $" ({BufferedEntryCount} new)" : string.Empty;

    public string FilePath
    {
        get => _options.FilePath;
        set
        {
            _options.FilePath = value;
            OnPropertyChanged();
        }
    }

    public ICommand OpenFileCommand { get; }
    public ICommand CloseFileCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand FileRotationSettingsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand OpenRecentFileCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand PauseResumeCommand { get; }

    public MainViewModel()
    {
        _logTailService = new LogTailService();
        _logFormatService = new LogFormatService();
        _recentFilesManager = new RecentFilesManager();
        _settingsManager = new SettingsManager();
        _fileMonitorService = new FileMonitorService();
        _fileValidator = new LogFileValidator();
        _fileMonitorService.FileChanged += FileMonitorService_FileChanged;
        _fileMonitorService.FileDeleted += FileMonitorService_FileDeleted;
        _fileMonitorService.BufferedCountChanged += FileMonitorService_BufferedCountChanged;
        _fileMonitorService.FileRotationDetected += FileMonitorService_FileRotationDetected;
        _fileMonitorService.FileRecreated += FileMonitorService_FileRecreated;
        _fileMonitorService.DeletionStatusChanged += FileMonitorService_DeletionStatusChanged;
        
        // Load settings from disk
        var settings = _settingsManager.LoadSettings();
        
        _options = new LogTailOptions
        {
            TailLines = settings.Preferences.TailLines,
            RefreshRate = TimeSpan.FromSeconds(settings.Preferences.RefreshRateSeconds),
            FilePath = settings.Preferences.LastOpenedFile ?? string.Empty,
            MonitoringMode = settings.Preferences.MonitoringMode,
            LogFormatName = settings.Filter.LogFormatName
        };

        // Apply the log format to the parser
        var selectedFormat = _logFormatService.GetFormatByName(_options.LogFormatName) ?? _logFormatService.GetDefaultFormat();
        _logTailService.Parser.SetFormat(selectedFormat);

        // Restore filter settings
        if (settings.Filter.SelectedLevels.Count > 0)
        {
            foreach (var level in settings.Filter.SelectedLevels)
            {
                _options.Levels.Add(level);
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Filter.MessageFilter))
        {
            _options.Filter = settings.Filter.MessageFilter;
        }

        if (settings.Filter.SelectedSources.Count > 0)
        {
            _selectedSources = settings.Filter.SelectedSources.ToHashSet();
        }
        
        // Load pause settings
        _pausedStatusBarColor = settings.Preferences.PauseStatusBarColor;
        
        // Apply rotation settings to file monitor
        _fileMonitorService.SetRotationSettings(settings.Preferences.FileRotation);

        _refreshTimer = new DispatcherTimer
        {
            Interval = _options.RefreshRate
        };
        _refreshTimer.Tick += RefreshTimer_Tick;

        OpenFileCommand = new RelayCommand(OpenFile);
        CloseFileCommand = new RelayCommand(CloseFile, CanCloseFile);
        RefreshCommand = new RelayCommand(Refresh);
        FilterCommand = new RelayCommand(ShowFilterDialog);
        SettingsCommand = new RelayCommand(ShowSettingsDialog);
        FileRotationSettingsCommand = new RelayCommand(ShowFileRotationDialog);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        OpenRecentFileCommand = new RelayCommand(OpenRecentFile);
        AboutCommand = new RelayCommand(ShowAboutDialog);
        PauseResumeCommand = new RelayCommand(TogglePauseResume, CanPauseResume);

        LoadRecentFiles();
    }

    public async void Start()
    {
        // If no file is currently set, try to load the most recent file
        if (string.IsNullOrEmpty(_options.FilePath))
        {
            var recentFiles = _recentFilesManager.GetRecentFiles();
            if (recentFiles.Count > 0 && File.Exists(recentFiles[0]))
            {
                // Skip validation for recent files
                await OpenLogFileAsync(recentFiles[0], validateFile: false);
                return;
            }
        }

        if (!string.IsNullOrEmpty(_options.FilePath) && File.Exists(_options.FilePath))
        {
            SetupMonitoringForCurrentFile();
            Refresh(null);
        }
    }

    public void Stop()
    {
        _refreshTimer.Stop();
        _fileMonitorService.StopMonitoring();
    }

    /// <summary>
    /// Opens a log file with validation. This can be called from UI components like drag-and-drop.
    /// </summary>
    /// <param name="filePath">Path to the file to open</param>
    /// <param name="validateFile">Whether to validate the file before opening</param>
    public async Task OpenFileAsync(string filePath, bool validateFile = true)
    {
        await OpenLogFileAsync(filePath, validateFile);
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        Refresh(null);
    }

    private void Refresh(object? parameter)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.FilePath) || !File.Exists(_options.FilePath))
            {
                StatusText = "No file selected or file not found";
                return;
            }

            IsBusy = true;

            var filtered = _logTailService.GetFilteredLogs(_options).ToList();

            if (_previousOutput == null || !filtered.SequenceEqual(_previousOutput))
            {
                // Determine what changed
                var oldLines = _previousOutput ?? new List<string>();
                var newLines = filtered;

                // Find new lines that were added
                var addedLines = newLines.Skip(oldLines.Count).ToList();

                // Check if we need to do a full reload (filters changed, file shrunk, etc)
                bool needsFullReload = _previousOutput == null || 
                                      newLines.Count < oldLines.Count ||
                                      !oldLines.SequenceEqual(newLines.Take(oldLines.Count));

                if (needsFullReload)
                {
                    // Full reload - file changed significantly or filters changed
                    PerformFullReload(filtered);
                }
                else if (addedLines.Count > 0)
                {
                    // Incremental update - only new lines added
                    PerformIncrementalUpdate(addedLines, filtered);
                }

                _previousOutput = filtered;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void PerformFullReload(List<string> filtered)
    {
        var newSources = new HashSet<string>();
        var entriesToAdd = new List<LogEntryViewModel>();

        foreach (var line in filtered)
        {
            var entry = LogEntryViewModel.FromText(line, _logTailService.Parser);
            
            if (!string.IsNullOrWhiteSpace(entry.Source))
            {
                newSources.Add(entry.Source);
            }

            // Apply source filter
            // Include lines without a source (continuation lines like stack traces)
            if (_selectedSources.Count == 0 || 
                string.IsNullOrWhiteSpace(entry.Source) || 
                _selectedSources.Contains(entry.Source))
            {
                entriesToAdd.Add(entry);
            }
        }

        // Update UI in one batch operation
        LogEntries.Clear();
        LogEntries.AddRange(entriesToAdd);

        // Update available sources
        _availableSources = newSources;

        LogCount = LogEntries.Count;
        
        UpdateStatusText();
    }

    private void PerformIncrementalUpdate(List<string> addedLines, List<string> allFiltered)
    {
        var newEntries = new List<LogEntryViewModel>();
        var newSources = new HashSet<string>(_availableSources);

        // Parse only the new lines
        foreach (var line in addedLines)
        {
            var entry = LogEntryViewModel.FromText(line, _logTailService.Parser);
            
            if (!string.IsNullOrWhiteSpace(entry.Source))
            {
                newSources.Add(entry.Source);
            }

            // Apply source filter
            // Include lines without a source (continuation lines like stack traces)
            if (_selectedSources.Count == 0 || 
                string.IsNullOrWhiteSpace(entry.Source) || 
                _selectedSources.Contains(entry.Source))
            {
                newEntries.Add(entry);
            }
        }

        // Calculate how many items to remove from the start to maintain tail limit
        int maxDisplayCount = _options.TailLines * 5; // Same multiplier as LogReader
        int totalAfterAdd = LogEntries.Count + newEntries.Count;
        int toRemove = Math.Max(0, totalAfterAdd - maxDisplayCount);

        // Remove old entries from the beginning if needed (single batch operation)
        if (toRemove > 0)
        {
            LogEntries.RemoveRange(toRemove);
        }

        // Add new entries to the end (single batch operation)
        if (newEntries.Count > 0)
        {
            LogEntries.AddRange(newEntries);
        }

        // Update available sources
        _availableSources = newSources;

        LogCount = LogEntries.Count;
        
        UpdateStatusText();
    }

    private string GetFilterInfo()
    {
        var parts = new List<string>();

        if (_options.Levels.Count > 0)
            parts.Add($"Levels: {string.Join(", ", _options.Levels)}");

        if (_selectedSources.Count > 0)
            parts.Add($"Sources: {_selectedSources.Count} selected");

        if (!string.IsNullOrWhiteSpace(_options.Filter))
            parts.Add($"Filter: '{_options.Filter}'");

        if (_options.IsDateTimeFilterEnabled)
        {
            var dateRangeText = "Date/Time: ";
            if (_options.FromDateTime.HasValue && _options.ToDateTime.HasValue)
                dateRangeText += $"{_options.FromDateTime.Value:yyyy-MM-dd HH:mm:ss} to {_options.ToDateTime.Value:yyyy-MM-dd HH:mm:ss}";
            else if (_options.FromDateTime.HasValue)
                dateRangeText += $"from {_options.FromDateTime.Value:yyyy-MM-dd HH:mm:ss}";
            else if (_options.ToDateTime.HasValue)
                dateRangeText += $"to {_options.ToDateTime.Value:yyyy-MM-dd HH:mm:ss}";
            parts.Add(dateRangeText);
        }

        return parts.Count > 0 ? $" | {string.Join(" | ", parts)}" : string.Empty;
    }

    private void ShowFilterDialog(object? parameter)
    {
        var filterViewModel = new FilterDialogViewModel();

        // Load current options
        filterViewModel.LoadFromOptions(_options);

        // Update sources
        filterViewModel.UpdateSources(_availableSources, _selectedSources);

        // Get first visible log entry's timestamp for default date range
        DateTime? firstLogDate = null;
        var firstEntry = LogEntries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Timestamp));
        if (firstEntry != null && LogEntryViewModel.TryParseTimestamp(firstEntry.Timestamp, out DateTime parsedDate))
        {
            firstLogDate = parsedDate.Date;
        }
        filterViewModel.SetDefaultDateFromFirstLog(firstLogDate);

        var dialog = new FilterDialog(filterViewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true || dialog.WasApplied)
        {
            // Apply filters
            filterViewModel.ApplyToOptions(_options);
            _selectedSources = filterViewModel.GetSelectedSources();

            // Apply source filter to options
            ApplySourceFilter();

            // Save filter settings
            SaveFilterSettings();

            // Refresh display
            _previousOutput = null; // Force refresh
            SetupMonitoringForCurrentFile();
            Refresh(null);
        }
    }

    private void ShowSettingsDialog(object? parameter)
    {
        var settingsViewModel = new SettingsDialogViewModel();

        // Reload formats to pick up any changes to the logformats.json file
        _logFormatService.ReloadFormats();

        // Load available log formats
        var formats = _logFormatService.GetAllFormats();
        settingsViewModel.AvailableLogFormats.Clear();
        foreach (var format in formats)
        {
            settingsViewModel.AvailableLogFormats.Add(format.Name);
        }

        // Load current options
        settingsViewModel.LoadFromOptions(_options);

        var dialog = new SettingsDialog(settingsViewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true || dialog.WasApplied)
        {
            // Check if log format changed
            bool formatChanged = settingsViewModel.SelectedLogFormat != _options.LogFormatName;

            // Apply settings
            settingsViewModel.ApplyToOptions(_options);

            // Update the parser if format changed
            if (formatChanged)
            {
                var selectedFormat = _logFormatService.GetFormatByName(_options.LogFormatName) ?? _logFormatService.GetDefaultFormat();
                _logTailService.Parser.SetFormat(selectedFormat);
            }

            // Update timer interval
            _refreshTimer.Stop();
            _refreshTimer.Interval = _options.RefreshRate;

            // Save app preferences
            SaveAppPreferences();

            // Refresh display
            _previousOutput = null; // Force refresh
            SetupMonitoringForCurrentFile();
            Refresh(null);
        }
    }

    private void ShowAboutDialog(object? parameter)
    {
        var aboutViewModel = new AboutDialogViewModel();
        var dialog = new AboutDialog(aboutViewModel)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }
    
    private void ShowFileRotationDialog(object? parameter)
    {
        var settings = _settingsManager.LoadSettings();
        var viewModel = new FileRotationDialogViewModel();
        
        // Load current rotation settings
        viewModel.LoadFromSettings(settings.Preferences.FileRotation);
        
        var dialog = new FileRotationDialog(viewModel)
        {
            Owner = Application.Current.MainWindow
        };
        
        if (dialog.ShowDialog() == true || dialog.WasApplied)
        {
            // Apply settings
            viewModel.ApplyToSettings(settings.Preferences.FileRotation);
            
            // Update file monitor service with new settings
            _fileMonitorService.SetRotationSettings(settings.Preferences.FileRotation);
            
            // Save settings
            _settingsManager.SaveSettings(settings);
            
            // Optionally restart monitoring to apply changes
            if (!string.IsNullOrEmpty(_options.FilePath) && File.Exists(_options.FilePath))
            {
                SetupMonitoringForCurrentFile();
            }
        }
    }

    private void ApplySourceFilter()
    {
        // If specific sources are selected, we need to modify the filter
        // For now, we'll handle this in the refresh logic by filtering LogEntries
        // A more complete solution would integrate source filtering into LogTailService
    }

    private async void OpenFile(object? parameter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Select Log File"
        };

        if (dialog.ShowDialog() == true)
        {
            await OpenLogFileAsync(dialog.FileName, validateFile: true);
        }
    }

    private async void OpenRecentFile(object? parameter)
    {
        if (parameter is string filePath && File.Exists(filePath))
        {
            // Skip validation for recent files - they were validated when first opened
            await OpenLogFileAsync(filePath, validateFile: false);
        }
    }

    private async Task OpenLogFileAsync(string filePath, bool validateFile)
    {
        IsBusy = true;
        try
        {
            // Validate the file if requested
            if (validateFile)
            {
                var validationResult = await _fileValidator.ValidateAsync(filePath);
                
                if (!validationResult.IsValid)
                {
                    // Ask user if they want to open anyway
                    var result = MessageBox.Show(
                        $"{validationResult.ErrorMessage}\n\nThis file may not be a supported log file format. Are you sure you want to open it?",
                        "File Validation Warning",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }
            
            FilePath = filePath;
            _availableSources.Clear();
            _selectedSources.Clear();
            _previousOutput = null; // Force full reload for new file
            
            // Reset pause state when opening new file
            IsMonitoringPaused = false;
            BufferedEntryCount = 0;
            
            SetupMonitoringForCurrentFile();
            Refresh(null);

            // Add to recent files
            _recentFilesManager.AddRecentFile(filePath);
            LoadRecentFiles();

            // Save the last opened file
            SaveAppPreferences();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanCloseFile(object? parameter)
    {
        return !string.IsNullOrEmpty(_options.FilePath) && LogEntries.Count > 0;
    }

    private void CloseFile(object? parameter)
    {
        // Stop monitoring and timer
        _refreshTimer.Stop();
        _fileMonitorService.StopMonitoring();

        // Clear data
        LogEntries.Clear();
        _previousOutput = null;
        _availableSources.Clear();
        _selectedSources.Clear();

        // Reset filters
        _options.Levels.Clear();
        _options.Filter = string.Empty;
        
        // Reset pause state
        IsMonitoringPaused = false;
        BufferedEntryCount = 0;

        // Clear file path
        FilePath = string.Empty;

        // Update status
        LogCount = 0;
        StatusText = "No file open";
        UpdateStatusBarColor();

        // Save settings
        SaveAppPreferences();

        // Force UI update for command states
        OnPropertyChanged(nameof(FilePath));
        CommandManager.InvalidateRequerySuggested();
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        var recentFiles = _recentFilesManager.GetRecentFiles();
        foreach (var file in recentFiles)
        {
            if (File.Exists(file))
            {
                RecentFiles.Add(file);
            }
        }
    }

    public void SetOptions(LogTailOptions options)
    {
        _options = options;
        _refreshTimer.Interval = options.RefreshRate;
        OnPropertyChanged(nameof(FilePath));
    }

    public ApplicationSettings GetCurrentSettings()
    {
        var settings = _settingsManager.GetCurrentSettings();
        
        // Update preferences from current state
        settings.Preferences.TailLines = _options.TailLines;
        settings.Preferences.RefreshRateSeconds = (int)_options.RefreshRate.TotalSeconds;
        settings.Preferences.LastOpenedFile = string.IsNullOrWhiteSpace(_options.FilePath) ? null : _options.FilePath;
        settings.Preferences.MonitoringMode = _options.MonitoringMode;

        // Update filter settings
        settings.Filter.SelectedLevels = _options.Levels.ToList();
        settings.Filter.SelectedSources = _selectedSources.ToList();
        settings.Filter.MessageFilter = _options.Filter;
        settings.Filter.LogFormatName = _options.LogFormatName;
        settings.Filter.IsDateTimeFilterEnabled = _options.IsDateTimeFilterEnabled;
        settings.Filter.FromDateTime = _options.FromDateTime;
        settings.Filter.ToDateTime = _options.ToDateTime;

        return settings;
    }

    public void SaveSettings(ApplicationSettings settings)
    {
        _settingsManager.SaveSettings(settings);
    }

    private void SaveFilterSettings()
    {
        var settings = GetCurrentSettings();
        _settingsManager.SaveSettings(settings);
    }

    private void SaveAppPreferences()
    {
        var settings = GetCurrentSettings();
        _settingsManager.SaveSettings(settings);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void FileMonitorService_FileChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() => Refresh(null), DispatcherPriority.Background);
    }

    private void FileMonitorService_FileDeleted(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusText = "File deleted or renamed. Monitoring stopped.";
            _refreshTimer.Stop();
            _fileMonitorService.StopMonitoring();
        }, DispatcherPriority.Background);
    }
    
    private void FileMonitorService_BufferedCountChanged(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            BufferedEntryCount = _fileMonitorService.BufferedCount;
        }, DispatcherPriority.Background);
    }
    
    private void FileMonitorService_FileRotationDetected(object? sender, FileRotationEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _logger.Information("File rotation detected: {Type} - {Method}", e.RotationType, e.DetectionMethod);
            
            var settings = _settingsManager.LoadSettings();
            if (settings.Preferences.FileRotation.ShowNotification && settings.Preferences.FileRotation.LogRotationEvents)
            {
                var rotationType = e.RotationType.ToString();
                var oldFile = e.OldFile != null ? Path.GetFileName(e.OldFile) : "N/A";
                var newFile = e.NewFile != null ? Path.GetFileName(e.NewFile) : "N/A";
                
                _logger.Information("File rotation: {Type}, Old: {OldFile}, New: {NewFile}", 
                    rotationType, oldFile, newFile);
            }
        }, DispatcherPriority.Background);
    }
    
    private void FileMonitorService_FileRecreated(object? sender, FileRecreatedEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            _logger.Information("File recreated: {FilePath}", e.FilePath);
            
            // Reopen the file and resume monitoring
            await OpenLogFileAsync(e.FilePath, validateFile: false);
            
            var settings = _settingsManager.LoadSettings();
            if (settings.Preferences.FileRotation.ShowNotification)
            {
                var tempStatus = StatusText;
                StatusText = $"File recreated - monitoring resumed ({e.ElapsedSeconds}s elapsed)";
                
                await System.Threading.Tasks.Task.Delay(3000);
                
                if (StatusText.StartsWith("File recreated"))
                {
                    UpdateStatusText();
                }
            }
        }, DispatcherPriority.Background);
    }
    
    private void FileMonitorService_DeletionStatusChanged(object? sender, DeletionStatusEventArgs e)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusText = e.Status;
            
            if (e.IsWarning)
            {
                StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500")); // Orange
            }
            else if (e.IsError)
            {
                StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")); // Red
            }
            else if (e.IsNormal)
            {
                UpdateStatusBarColor();
            }
        }, DispatcherPriority.Background);
    }
    
    private void TogglePauseResume(object? parameter)
    {
        if (IsMonitoringPaused)
        {
            ResumeMonitoring();
        }
        else
        {
            PauseMonitoring();
        }
    }
    
    private bool CanPauseResume(object? parameter)
    {
        // If paused, always allow resume (as long as a file is open)
        if (IsMonitoringPaused)
            return !string.IsNullOrEmpty(_options.FilePath);
        
        // If not paused, allow pause only if monitoring is active
        return !string.IsNullOrEmpty(_options.FilePath) && 
               (_fileMonitorService.IsWatcherActive || _refreshTimer.IsEnabled);
    }
    
    private void PauseMonitoring()
    {
        IsMonitoringPaused = true;
        _fileMonitorService.Pause();
        _refreshTimer.Stop();
    }
    
    private void ResumeMonitoring()
    {
        var bufferedCount = BufferedEntryCount;
        IsMonitoringPaused = false;
        _fileMonitorService.Resume();
        
        if (!_fileMonitorService.IsWatcherActive)
        {
            _refreshTimer.Start();
        }
        
        // Force a refresh to show buffered changes
        if (bufferedCount > 0)
        {
            Refresh(null);
            
            var settings = _settingsManager.LoadSettings();
            if (settings.Preferences.ShowPauseNotification)
            {
                var tempStatus = StatusText;
                StatusText = $"Resumed - Refreshed with buffered changes";
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Threading.Thread.Sleep(3000);
                    if (StatusText.StartsWith("Resumed -"))
                    {
                        UpdateStatusText();
                    }
                }, DispatcherPriority.Background);
            }
        }
    }
    
    public void UpdateStatusBarColor()
    {
        var color = IsMonitoringPaused ? _pausedStatusBarColor : _defaultStatusBarColor;
        StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
    
    public void UpdateStatusText()
    {
        if (string.IsNullOrEmpty(_options.FilePath))
        {
            StatusText = "No file open";
            return;
        }
        
        var filterInfo = GetFilterInfo();
        var pauseInfo = IsMonitoringPaused ? $" | PAUSED{BufferedCountText}" : string.Empty;
        StatusText = $"Showing {LogCount} log entries from {Path.GetFileName(_options.FilePath)} - Last updated: {DateTime.Now:HH:mm:ss}{filterInfo}{pauseInfo} | Mode: {GetMonitoringModeLabel()}";
    }

    private void SetupMonitoringForCurrentFile()
    {
        _refreshTimer.Stop();
        _fileMonitorService.StopMonitoring();

        if (string.IsNullOrEmpty(_options.FilePath) || !File.Exists(_options.FilePath))
        {
            return;
        }

        _fileMonitorService.StartMonitoring(_options.FilePath, _options.MonitoringMode);

        if (!_fileMonitorService.IsWatcherActive)
        {
            _refreshTimer.Interval = _options.RefreshRate;
            _refreshTimer.Start();
        }
    }

    private string GetMonitoringModeLabel()
    {
        if (_fileMonitorService.IsWatcherActive)
        {
            return "Real-time";
        }

        return _options.MonitoringMode == MonitoringMode.RealTimeOnly ? "Polling (fallback)" : "Polling";
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
