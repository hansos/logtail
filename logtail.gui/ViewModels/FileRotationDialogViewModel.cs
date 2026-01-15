using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using logtail.gui.Models;

namespace logtail.gui.ViewModels;

public class FileRotationDialogViewModel : INotifyPropertyChanged
{
    private bool _autoDetect = true;
    private int _checkIntervalSeconds = 5;
    private bool _showNotification = true;
    private bool _logRotationEvents = true;
    private int _retryIntervalSeconds = 2;
    private int _maxRetries = 30;
    
    // Deletion settings
    private bool _showWarning = true;
    private bool _autoWaitForRecreation = true;
    private int _waitTimeoutSeconds = 60;
    private bool _promptUserOnDeletion = false;
    private bool _stopMonitoringImmediately = false;
    private int _deletionCheckIntervalSeconds = 2;
    
    public ICommand? ApplyCommand { get; set; }
    public ICommand? CancelCommand { get; set; }

    #region Rotation Detection Properties

    public bool AutoDetect
    {
        get => _autoDetect;
        set
        {
            _autoDetect = value;
            OnPropertyChanged();
        }
    }

    public int CheckIntervalSeconds
    {
        get => _checkIntervalSeconds;
        set
        {
            _checkIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public bool ShowNotification
    {
        get => _showNotification;
        set
        {
            _showNotification = value;
            OnPropertyChanged();
        }
    }

    public bool LogRotationEvents
    {
        get => _logRotationEvents;
        set
        {
            _logRotationEvents = value;
            OnPropertyChanged();
        }
    }

    public int RetryIntervalSeconds
    {
        get => _retryIntervalSeconds;
        set
        {
            _retryIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public int MaxRetries
    {
        get => _maxRetries;
        set
        {
            _maxRetries = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region File Deletion Properties

    public bool ShowWarning
    {
        get => _showWarning;
        set
        {
            _showWarning = value;
            OnPropertyChanged();
        }
    }

    public bool AutoWaitForRecreation
    {
        get => _autoWaitForRecreation;
        set
        {
            _autoWaitForRecreation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDeletionBehaviorEnabled));
        }
    }

    public int WaitTimeoutSeconds
    {
        get => _waitTimeoutSeconds;
        set
        {
            _waitTimeoutSeconds = value;
            OnPropertyChanged();
        }
    }

    public bool PromptUserOnDeletion
    {
        get => _promptUserOnDeletion;
        set
        {
            _promptUserOnDeletion = value;
            OnPropertyChanged();
        }
    }

    public bool StopMonitoringImmediately
    {
        get => _stopMonitoringImmediately;
        set
        {
            _stopMonitoringImmediately = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDeletionBehaviorEnabled));
            OnPropertyChanged(nameof(IsAutoWaitEnabled));
        }
    }

    public int DeletionCheckIntervalSeconds
    {
        get => _deletionCheckIntervalSeconds;
        set
        {
            _deletionCheckIntervalSeconds = value;
            OnPropertyChanged();
        }
    }

    public bool IsDeletionBehaviorEnabled => !StopMonitoringImmediately && AutoWaitForRecreation;
    public bool IsAutoWaitEnabled => !StopMonitoringImmediately;

    #endregion

    public void LoadFromSettings(FileRotationSettings settings)
    {
        AutoDetect = settings.AutoDetect;
        CheckIntervalSeconds = settings.CheckIntervalSeconds;
        ShowNotification = settings.ShowNotification;
        LogRotationEvents = settings.LogRotationEvents;
        RetryIntervalSeconds = settings.RetryIntervalSeconds;
        MaxRetries = settings.MaxRetries;
        
        ShowWarning = settings.Deletion.ShowWarning;
        AutoWaitForRecreation = settings.Deletion.AutoWaitForRecreation;
        WaitTimeoutSeconds = settings.Deletion.WaitTimeoutSeconds;
        PromptUserOnDeletion = settings.Deletion.PromptUserOnDeletion;
        StopMonitoringImmediately = settings.Deletion.StopMonitoringImmediately;
        DeletionCheckIntervalSeconds = settings.Deletion.CheckIntervalSeconds;
    }

    public void ApplyToSettings(FileRotationSettings settings)
    {
        settings.AutoDetect = AutoDetect;
        settings.CheckIntervalSeconds = CheckIntervalSeconds;
        settings.ShowNotification = ShowNotification;
        settings.LogRotationEvents = LogRotationEvents;
        settings.RetryIntervalSeconds = RetryIntervalSeconds;
        settings.MaxRetries = MaxRetries;
        
        settings.Deletion.ShowWarning = ShowWarning;
        settings.Deletion.AutoWaitForRecreation = AutoWaitForRecreation;
        settings.Deletion.WaitTimeoutSeconds = WaitTimeoutSeconds;
        settings.Deletion.PromptUserOnDeletion = PromptUserOnDeletion;
        settings.Deletion.StopMonitoringImmediately = StopMonitoringImmediately;
        settings.Deletion.CheckIntervalSeconds = DeletionCheckIntervalSeconds;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
