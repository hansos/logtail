using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogTail.Core.Models;

namespace logtail.gui.ViewModels;

public class SettingsDialogViewModel : INotifyPropertyChanged
{
    private int _tailLines = 100;
    private int _refreshRateSeconds = 2;

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
        TailLines = options.TailLines;
        RefreshRateSeconds = (int)options.RefreshRate.TotalSeconds;
    }

    public void ApplyToOptions(LogTailOptions options)
    {
        options.TailLines = TailLines;
        options.RefreshRate = TimeSpan.FromSeconds(RefreshRateSeconds);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
