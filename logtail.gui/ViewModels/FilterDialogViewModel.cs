using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogTail.Core.Models;

namespace logtail.gui.ViewModels;

public class FilterDialogViewModel : INotifyPropertyChanged
{
    private string _messageFilter = string.Empty;
    private int _tailLines = 100;
    private int _refreshRateSeconds = 2;

    public ObservableCollection<LogLevelFilterItem> LogLevels { get; } = new()
    {
        new LogLevelFilterItem { Level = "VERBOSE", IsChecked = true },
        new LogLevelFilterItem { Level = "DBUG", IsChecked = true },
        new LogLevelFilterItem { Level = "INFO", IsChecked = true },
        new LogLevelFilterItem { Level = "WARNING", IsChecked = true },
        new LogLevelFilterItem { Level = "ERROR", IsChecked = true },
        new LogLevelFilterItem { Level = "EROR", IsChecked = true },
        new LogLevelFilterItem { Level = "FATAL", IsChecked = true }
    };

    public ObservableCollection<SourceFilterItem> Sources { get; } = new();

    public string MessageFilter
    {
        get => _messageFilter;
        set
        {
            _messageFilter = value;
            OnPropertyChanged();
        }
    }

    public int TailLines
    {
        get => _tailLines;
        set
        {
            _tailLines = value;
            OnPropertyChanged();
        }
    }

    public int RefreshRateSeconds
    {
        get => _refreshRateSeconds;
        set
        {
            _refreshRateSeconds = value;
            OnPropertyChanged();
        }
    }

    public void LoadFromOptions(LogTailOptions options)
    {
        MessageFilter = options.Filter ?? string.Empty;
        TailLines = options.TailLines;
        RefreshRateSeconds = (int)options.RefreshRate.TotalSeconds;

        // Update log level checkboxes
        foreach (var levelItem in LogLevels)
        {
            levelItem.IsChecked = options.Levels.Count == 0 || 
                                 options.Levels.Contains(levelItem.Level);
        }
    }

    public void ApplyToOptions(LogTailOptions options)
    {
        options.Filter = string.IsNullOrWhiteSpace(MessageFilter) ? null : MessageFilter;
        options.TailLines = TailLines;
        options.RefreshRate = TimeSpan.FromSeconds(RefreshRateSeconds);

        // Update log levels
        options.Levels.Clear();
        var checkedLevels = LogLevels.Where(l => l.IsChecked).Select(l => l.Level).ToList();
        
        // If all are checked, clear the filter (show all)
        if (checkedLevels.Count < LogLevels.Count)
        {
            foreach (var level in checkedLevels)
            {
                options.Levels.Add(level);
            }
        }
    }

    public void UpdateSources(IEnumerable<string> sources, HashSet<string>? selectedSources = null)
    {
        var existingSources = Sources.Select(s => s.Source).ToHashSet();
        
        // Add new sources
        foreach (var source in sources.OrderBy(s => s))
        {
            if (!existingSources.Contains(source))
            {
                var isChecked = selectedSources == null || selectedSources.Count == 0 || selectedSources.Contains(source);
                Sources.Add(new SourceFilterItem { Source = source, IsChecked = isChecked });
            }
        }

        // Remove sources that no longer exist
        var currentSources = sources.ToHashSet();
        var toRemove = Sources.Where(s => !currentSources.Contains(s.Source)).ToList();
        foreach (var item in toRemove)
        {
            Sources.Remove(item);
        }
    }

    public HashSet<string> GetSelectedSources()
    {
        var selected = Sources.Where(s => s.IsChecked).Select(s => s.Source).ToHashSet();
        // If all are selected, return empty set (no filter)
        return selected.Count == Sources.Count ? new HashSet<string>() : selected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class LogLevelFilterItem : INotifyPropertyChanged
{
    private bool _isChecked;

    public string Level { get; set; } = string.Empty;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SourceFilterItem : INotifyPropertyChanged
{
    private bool _isChecked;

    public string Source { get; set; } = string.Empty;

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
