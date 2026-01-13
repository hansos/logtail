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
using logtail.gui.Services;
using logtail.gui.Models;

namespace logtail.gui.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly LogTailService _logTailService;
    private readonly DispatcherTimer _refreshTimer;
    private readonly RecentFilesManager _recentFilesManager;
    private readonly SettingsManager _settingsManager;
    private LogTailOptions _options;
    private string _statusText = "Ready";
    private Brush _statusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#007ACC"));
    private int _logCount;
    private List<string>? _previousOutput;
    private HashSet<string> _availableSources = new();
    private HashSet<string> _selectedSources = new();
    private bool _isBusy;

    public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new();
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
    public ICommand RefreshCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand OpenRecentFileCommand { get; }

    public MainViewModel()
    {
        _logTailService = new LogTailService();
        _recentFilesManager = new RecentFilesManager();
        _settingsManager = new SettingsManager();
        
        // Load settings from disk
        var settings = _settingsManager.LoadSettings();
        
        _options = new LogTailOptions
        {
            TailLines = settings.Preferences.TailLines,
            RefreshRate = TimeSpan.FromSeconds(settings.Preferences.RefreshRateSeconds),
            FilePath = settings.Preferences.LastOpenedFile ?? string.Empty
        };

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

        _refreshTimer = new DispatcherTimer
        {
            Interval = _options.RefreshRate
        };
        _refreshTimer.Tick += RefreshTimer_Tick;

        OpenFileCommand = new RelayCommand(OpenFile);
        RefreshCommand = new RelayCommand(Refresh);
        FilterCommand = new RelayCommand(ShowFilterDialog);
        SettingsCommand = new RelayCommand(ShowSettingsDialog);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        OpenRecentFileCommand = new RelayCommand(OpenRecentFile);

        LoadRecentFiles();
    }

    public void Start()
    {
        // If no file is currently set, try to load the most recent file
        if (string.IsNullOrEmpty(_options.FilePath))
        {
            var recentFiles = _recentFilesManager.GetRecentFiles();
            if (recentFiles.Count > 0 && File.Exists(recentFiles[0]))
            {
                OpenLogFile(recentFiles[0]);
                return;
            }
        }

        if (!string.IsNullOrEmpty(_options.FilePath) && File.Exists(_options.FilePath))
        {
            _refreshTimer.Start();
            Refresh(null);
        }
    }

    public void Stop()
    {
        _refreshTimer.Stop();
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
                LogEntries.Clear();
                var newSources = new HashSet<string>();

                foreach (var line in filtered)
                {
                    var entry = LogEntryViewModel.FromText(line);
                    
                    if (!string.IsNullOrWhiteSpace(entry.Source))
                    {
                        newSources.Add(entry.Source);
                    }

                    // Apply source filter
                    if (_selectedSources.Count == 0 || _selectedSources.Contains(entry.Source))
                    {
                        LogEntries.Add(entry);
                    }
                }

                // Update available sources
                _availableSources = newSources;

                _previousOutput = filtered;
                LogCount = LogEntries.Count;
                
                var filterInfo = GetFilterInfo();
                StatusText = $"Showing {LogCount} log entries from {Path.GetFileName(_options.FilePath)} - Last updated: {DateTime.Now:HH:mm:ss}{filterInfo}";
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

    private string GetFilterInfo()
    {
        var parts = new List<string>();

        if (_options.Levels.Count > 0)
            parts.Add($"Levels: {string.Join(", ", _options.Levels)}");

        if (_selectedSources.Count > 0)
            parts.Add($"Sources: {_selectedSources.Count} selected");

        if (!string.IsNullOrWhiteSpace(_options.Filter))
            parts.Add($"Filter: '{_options.Filter}'");

        return parts.Count > 0 ? $" | {string.Join(" | ", parts)}" : string.Empty;
    }

    private void ShowFilterDialog(object? parameter)
    {
        var filterViewModel = new FilterDialogViewModel();

        // Load current options
        filterViewModel.LoadFromOptions(_options);

        // Update sources
        filterViewModel.UpdateSources(_availableSources, _selectedSources);

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
            Refresh(null);
        }
    }

    private void ShowSettingsDialog(object? parameter)
    {
        var settingsViewModel = new SettingsDialogViewModel();

        // Load current options
        settingsViewModel.LoadFromOptions(_options);

        var dialog = new SettingsDialog(settingsViewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true || dialog.WasApplied)
        {
            // Apply settings
            settingsViewModel.ApplyToOptions(_options);

            // Update timer interval
            _refreshTimer.Stop();
            _refreshTimer.Interval = _options.RefreshRate;

            // Save app preferences
            SaveAppPreferences();

            // Refresh display
            _previousOutput = null; // Force refresh
            Refresh(null);

            _refreshTimer.Start();
        }
    }

    private void ApplySourceFilter()
    {
        // If specific sources are selected, we need to modify the filter
        // For now, we'll handle this in the refresh logic by filtering LogEntries
        // A more complete solution would integrate source filtering into LogTailService
    }

    private void OpenFile(object? parameter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Log files (*.log)|*.log|All files (*.*)|*.*",
            Title = "Select Log File"
        };

        if (dialog.ShowDialog() == true)
        {
            OpenLogFile(dialog.FileName);
        }
    }

    private void OpenRecentFile(object? parameter)
    {
        if (parameter is string filePath && File.Exists(filePath))
        {
            OpenLogFile(filePath);
        }
    }

    private void OpenLogFile(string filePath)
    {
        IsBusy = true;
        try
        {
            FilePath = filePath;
            _availableSources.Clear();
            _selectedSources.Clear();
            _refreshTimer.Start();
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

        // Update filter settings
        settings.Filter.SelectedLevels = _options.Levels.ToList();
        settings.Filter.SelectedSources = _selectedSources.ToList();
        settings.Filter.MessageFilter = _options.Filter;

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
