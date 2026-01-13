using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using logtail.gui.ViewModels;
using logtail.gui.Services;
using logtail.gui.Models;

namespace logtail.gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            SizeChanged += MainWindow_SizeChanged;
            LocationChanged += MainWindow_LocationChanged;
            StateChanged += MainWindow_StateChanged;
            
            _viewModel.LogEntries.CollectionChanged += (s, e) => 
            {
                Dispatcher.BeginInvoke(() => UpdateColumnWidths(), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Restore window state from settings
            var settings = _viewModel.GetCurrentSettings();
            
            if (settings.Window.Width > 0 && settings.Window.Height > 0)
            {
                Width = settings.Window.Width;
                Height = settings.Window.Height;
            }

            if (settings.Window.Left > 0 && settings.Window.Top > 0)
            {
                Left = settings.Window.Left;
                Top = settings.Window.Top;
            }

            if (settings.Window.IsMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            // Restore column widths if they were saved
            if (LogListView?.View is GridView gridView && gridView.Columns.Count == 4)
            {
                if (!double.IsNaN(settings.Window.TimeColumnWidth) && settings.Window.TimeColumnWidth > 0)
                    gridView.Columns[0].Width = settings.Window.TimeColumnWidth;
                
                if (!double.IsNaN(settings.Window.LevelColumnWidth) && settings.Window.LevelColumnWidth > 0)
                    gridView.Columns[1].Width = settings.Window.LevelColumnWidth;
                
                if (!double.IsNaN(settings.Window.SourceColumnWidth) && settings.Window.SourceColumnWidth > 0)
                    gridView.Columns[2].Width = settings.Window.SourceColumnWidth;
                
                if (!double.IsNaN(settings.Window.MessageColumnWidth) && settings.Window.MessageColumnWidth > 0)
                    gridView.Columns[3].Width = settings.Window.MessageColumnWidth;
            }

            _isInitialized = true;
            _viewModel.Start();
            UpdateColumnWidths();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Stop();
            SaveWindowState();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnWidths();
            
            if (_isInitialized && WindowState == WindowState.Normal)
            {
                SaveWindowState();
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (_isInitialized && WindowState == WindowState.Normal)
            {
                SaveWindowState();
            }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (_isInitialized)
            {
                SaveWindowState();
            }
        }

        private void SaveWindowState()
        {
            var settings = _viewModel.GetCurrentSettings();
            
            settings.Window.IsMaximized = WindowState == WindowState.Maximized;
            
            if (WindowState == WindowState.Normal)
            {
                settings.Window.Width = Width;
                settings.Window.Height = Height;
                settings.Window.Left = Left;
                settings.Window.Top = Top;
            }

            // Save column widths
            if (LogListView?.View is GridView gridView && gridView.Columns.Count == 4)
            {
                settings.Window.TimeColumnWidth = gridView.Columns[0].ActualWidth;
                settings.Window.LevelColumnWidth = gridView.Columns[1].ActualWidth;
                settings.Window.SourceColumnWidth = gridView.Columns[2].ActualWidth;
                settings.Window.MessageColumnWidth = gridView.Columns[3].ActualWidth;
            }

            _viewModel.SaveSettings(settings);
        }

        private async void LogListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LogListView.SelectedItem is not LogEntryViewModel selectedEntry)
                return;

            // Try to open file in Visual Studio if the log entry contains a file path
            if (VisualStudioLauncher.TryExtractFileInfo(selectedEntry.Text, out string filePath, out int lineNumber))
            {
                var success = VisualStudioLauncher.OpenInVisualStudio(filePath, lineNumber);
                
                if (success)
                {
                    var originalStatusText = _viewModel.StatusText;
                    var originalBackground = _viewModel.StatusBarBackground;

                    _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA66"));
                    _viewModel.StatusText = $"Opening {Path.GetFileName(filePath)}:line {lineNumber} in Visual Studio";

                    await Task.Delay(2500);
                    _viewModel.StatusBarBackground = originalBackground;
                    _viewModel.StatusText = originalStatusText;
                    return;
                }
            }

            // Fallback to clipboard copy behavior for entries with timestamps
            // Only process if the clicked item has a Timestamp
            if (string.IsNullOrWhiteSpace(selectedEntry.Timestamp))
                return;

            var entriesToCopy = new List<LogEntryViewModel> { selectedEntry };
            var selectedIndex = LogListView.SelectedIndex;

            // Find all following entries without a Timestamp
            for (int i = selectedIndex + 1; i < _viewModel.LogEntries.Count; i++)
            {
                var nextEntry = _viewModel.LogEntries[i];
                if (!string.IsNullOrWhiteSpace(nextEntry.Timestamp))
                    break;
                
                entriesToCopy.Add(nextEntry);
            }

            // Build the text to copy
            var textToCopy = string.Join(Environment.NewLine, entriesToCopy.Select(entry => entry.Text));

            // Copy to clipboard
            try
            {
                // 1) Copy the existing text to a local variable
                var originalStatusText = _viewModel.StatusText;
                var originalBackground = _viewModel.StatusBarBackground;

                // 2) Change the background color to a green one matching the default blue one
                _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA66"));

                // 3) Write the new text
                Clipboard.SetText(textToCopy);
                _viewModel.StatusText = $"Copied {entriesToCopy.Count} line{(entriesToCopy.Count > 1 ? "s" : "")} to clipboard";

                // 4) Wait for 5 seconds and reset color and text to the original one
                await Task.Delay(2500);
                _viewModel.StatusBarBackground = originalBackground;
                _viewModel.StatusText = originalStatusText;
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Error copying to clipboard: {ex.Message}";
            }
        }

        private void UpdateColumnWidths()
        {
            if (LogListView?.View is not GridView gridView || gridView.Columns.Count != 4)
                return;

            // Auto-size first three columns (Time, Level, Source)
            gridView.Columns[0].Width = double.NaN; // Time
            gridView.Columns[1].Width = double.NaN; // Level
            gridView.Columns[2].Width = double.NaN; // Source

            // Force measure to get actual widths
            LogListView.UpdateLayout();

            // Calculate remaining width for Message column
            var totalFixedWidth = gridView.Columns[0].ActualWidth + 
                                 gridView.Columns[1].ActualWidth + 
                                 gridView.Columns[2].ActualWidth;
            
            var scrollBarWidth = 35; // Estimate for vertical scrollbar
            var remainingWidth = LogListView.ActualWidth - totalFixedWidth - scrollBarWidth;
            
            // Set Message column to fill remaining space (minimum 200px)
            gridView.Columns[3].Width = Math.Max(200, remainingWidth);
        }
    }
}