using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogTail.Core.Models;

namespace logtail.gui.ViewModels;

public class SettingsDialogViewModel : INotifyPropertyChanged
{
    private int _tailLines = 100;
    private int _refreshRateSeconds = 2;
    private MonitoringMode _monitoringMode = MonitoringMode.Auto;
    private string _selectedLogFormat = "Default";

    public ObservableCollection<string> AvailableLogFormats { get; } = new();

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

    public MonitoringMode MonitoringMode
    {
        get => _monitoringMode;
        set
        {
            _monitoringMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAutoMode));
            OnPropertyChanged(nameof(IsRealTimeMode));
            OnPropertyChanged(nameof(IsPollingMode));
        }
    }

    public string SelectedLogFormat
    {
        get => _selectedLogFormat;
        set
        {
            _selectedLogFormat = value;
            OnPropertyChanged();
        }
    }

    public bool IsAutoMode
    {
        get => MonitoringMode == MonitoringMode.Auto;
        set
        {
            if (value)
            {
                MonitoringMode = MonitoringMode.Auto;
            }
        }
    }

    public bool IsRealTimeMode
    {
        get => MonitoringMode == MonitoringMode.RealTimeOnly;
        set
        {
            if (value)
            {
                MonitoringMode = MonitoringMode.RealTimeOnly;
            }
        }
    }

    public bool IsPollingMode
    {
        get => MonitoringMode == MonitoringMode.PollingOnly;
        set
        {
            if (value)
            {
                MonitoringMode = MonitoringMode.PollingOnly;
            }
        }
    }

    public void LoadFromOptions(LogTailOptions options)
    {
        TailLines = options.TailLines;
        RefreshRateSeconds = (int)options.RefreshRate.TotalSeconds;
        MonitoringMode = options.MonitoringMode;
        SelectedLogFormat = options.LogFormatName;
    }

    public void ApplyToOptions(LogTailOptions options)
    {
        options.TailLines = TailLines;
        options.RefreshRate = TimeSpan.FromSeconds(RefreshRateSeconds);
        options.MonitoringMode = MonitoringMode;
        options.LogFormatName = SelectedLogFormat;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
