using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace logtail.gui.Collections;

/// <summary>
/// ObservableCollection that supports bulk operations (AddRange) with suppressed notifications
/// to improve performance when adding many items at once.
/// </summary>
public class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    /// <summary>
    /// Adds a range of items to the collection with a single notification at the end.
    /// This is much more efficient than adding items one by one for large collections.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;

        try
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        finally
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Removes a specified number of items from the beginning of the collection with a single notification.
    /// </summary>
    public void RemoveRange(int count)
    {
        if (count <= 0)
            return;

        if (count > Count)
            count = Count;

        _suppressNotification = true;

        try
        {
            for (int i = 0; i < count; i++)
            {
                RemoveAt(0);
            }
        }
        finally
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnPropertyChanged(e);
        }
    }
}
