using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LogTail.Core;
using LogTail.Core.Models;

namespace logtail.gui.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly LogTailService _logTailService;
    private readonly DispatcherTimer _refreshTimer;
    private LogTailOptions _options;
    private string _statusText = "Ready";
    private int _logCount;
    private List<string>? _previousOutput;
    private HashSet<string> _availableSources = new();
    private HashSet<string> _selectedSources = new();

    public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new();

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
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
    public ICommand ExitCommand { get; }

    public MainViewModel()
    {
        _logTailService = new LogTailService();
        _options = new LogTailOptions
        {
            TailLines = 100,
            RefreshRate = TimeSpan.FromSeconds(2)
        };

        _refreshTimer = new DispatcherTimer
        {
            Interval = _options.RefreshRate
        };
        _refreshTimer.Tick += RefreshTimer_Tick;

        OpenFileCommand = new RelayCommand(OpenFile);
        RefreshCommand = new RelayCommand(Refresh);
        FilterCommand = new RelayCommand(ShowFilterDialog);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
    }

    public void Start()
    {
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

            // Update timer interval
            _refreshTimer.Stop();
            _refreshTimer.Interval = _options.RefreshRate;

            // Apply source filter to options
            ApplySourceFilter();

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
            FilePath = dialog.FileName;
            _availableSources.Clear();
            _selectedSources.Clear();
            _refreshTimer.Start();
            Refresh(null);
        }
    }

    public void SetOptions(LogTailOptions options)
    {
        _options = options;
        _refreshTimer.Interval = options.RefreshRate;
        OnPropertyChanged(nameof(FilePath));
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
