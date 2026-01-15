using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LogTail.Core.Models;

namespace logtail.gui.ViewModels;

public class FilterDialogViewModel : INotifyPropertyChanged
{
    private string _messageFilter = string.Empty;
    private bool _isDateTimeFilterEnabled;
    private DateTime? _fromDateTime;
    private DateTime? _toDateTime;
    private string _fromTimeText = "00:00:00";
    private string _toTimeText = "23:59:59";
    
    public ICommand? ApplyCommand { get; set; }
    public ICommand? CancelCommand { get; set; }

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

    public bool IsDateTimeFilterEnabled
    {
        get => _isDateTimeFilterEnabled;
        set
        {
            _isDateTimeFilterEnabled = value;
            OnPropertyChanged();
        }
    }

    public DateTime? FromDateTime
    {
        get => _fromDateTime;
        set
        {
            _fromDateTime = value;
            OnPropertyChanged();
        }
    }

    public DateTime? ToDateTime
    {
        get => _toDateTime;
        set
        {
            _toDateTime = value;
            OnPropertyChanged();
        }
    }

    public string FromTimeText
    {
        get => _fromTimeText;
        set
        {
            _fromTimeText = value;
            OnPropertyChanged();
        }
    }

    public string ToTimeText
    {
        get => _toTimeText;
        set
        {
            _toTimeText = value;
            OnPropertyChanged();
        }
    }

    public void LoadFromOptions(LogTailOptions options)
    {
        MessageFilter = options.Filter ?? string.Empty;

        // Update log level checkboxes
        foreach (var levelItem in LogLevels)
        {
            levelItem.IsChecked = options.Levels.Count == 0 || 
                                 options.Levels.Contains(levelItem.Level);
        }

        // Load date/time filter settings
        IsDateTimeFilterEnabled = options.IsDateTimeFilterEnabled;
        
        if (options.FromDateTime.HasValue)
        {
            FromDateTime = options.FromDateTime.Value.Date;
            FromTimeText = options.FromDateTime.Value.ToString("HH:mm:ss");
        }
        else
        {
            FromDateTime = null;
            FromTimeText = "00:00:00";
        }
            
        if (options.ToDateTime.HasValue)
        {
            ToDateTime = options.ToDateTime.Value.Date;
            ToTimeText = options.ToDateTime.Value.ToString("HH:mm:ss");
        }
        else
        {
            ToDateTime = null;
            ToTimeText = "23:59:59";
        }
    }

    public void ApplyToOptions(LogTailOptions options)
    {
        options.Filter = string.IsNullOrWhiteSpace(MessageFilter) ? null : MessageFilter;

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

        // Apply date/time filter settings with time components
        options.IsDateTimeFilterEnabled = IsDateTimeFilterEnabled;
        options.FromDateTime = CombineDateAndTime(FromDateTime, FromTimeText);
        options.ToDateTime = CombineDateAndTime(ToDateTime, ToTimeText);
    }

    private static DateTime? CombineDateAndTime(DateTime? date, string timeText)
    {
        if (!date.HasValue)
            return null;

        if (TimeSpan.TryParse(timeText, out TimeSpan time))
        {
            return date.Value.Date.Add(time);
        }

        // Default to start of day if parsing fails
        return date.Value.Date;
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
