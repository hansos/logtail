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
        private CancellationTokenSource? _statusBarResetCts;

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
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            
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
            // Cancel any pending status bar reset
            _statusBarResetCts?.Cancel();
            _statusBarResetCts?.Dispose();
            
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
            await HandleLogEntryAction();
        }

        private async Task HandleLogEntryAction()
        {
            if (LogListView.SelectedItem is not LogEntryViewModel selectedEntry)
                return;

            // Cancel any previous status bar reset timer
            _statusBarResetCts?.Cancel();
            _statusBarResetCts?.Dispose();
            _statusBarResetCts = new CancellationTokenSource();
            var currentToken = _statusBarResetCts.Token;

            // Try to open file in Visual Studio if the log entry contains a file path
            if (VisualStudioLauncher.TryExtractFileInfo(selectedEntry.Text, out string filePath, out int lineNumber))
            {
                var success = VisualStudioLauncher.OpenInVisualStudio(filePath, lineNumber);
                
                if (success)
                {
                    _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA66"));
                    _viewModel.StatusText = $"Opening {Path.GetFileName(filePath)}:line {lineNumber} in Visual Studio";

                    try
                    {
                        await Task.Delay(2500, currentToken);
                        
                        // Only reset if this token hasn't been cancelled
                        if (!currentToken.IsCancellationRequested)
                        {
                            _viewModel.UpdateStatusBarColor();
                            _viewModel.UpdateStatusText();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Timer was cancelled by a new click - this is expected
                    }
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
                // Change the background color to green
                _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA66"));

                // Write the new text
                Clipboard.SetText(textToCopy);
                _viewModel.StatusText = $"Copied {entriesToCopy.Count} line{(entriesToCopy.Count > 1 ? "s" : "")} to clipboard";

                // Wait for 2.5 seconds and reset color and text
                try
                {
                    await Task.Delay(2500, currentToken);
                    
                    // Only reset if this token hasn't been cancelled
                    if (!currentToken.IsCancellationRequested)
                    {
                        _viewModel.UpdateStatusBarColor();
                        _viewModel.UpdateStatusText();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timer was cancelled by a new click - this is expected
                }
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

        private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+Space for pause/resume regardless of focus
            if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_viewModel.PauseResumeCommand.CanExecute(null))
                {
                    _viewModel.PauseResumeCommand.Execute(null);
                    e.Handled = true;
                }
                return;
            }

            // Handle Ctrl+Enter for log entry actions (open in VS or copy to clipboard)
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (LogListView?.SelectedItem != null)
                {
                    await HandleLogEntryAction();
                    e.Handled = true;
                }
                return;
            }

            if (LogListView == null || LogListView.Items.Count == 0)
                return;

            switch (e.Key)
            {
                case Key.PageUp:
                    ScrollByPage(isUp: true);
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    ScrollByPage(isUp: false);
                    e.Handled = true;
                    break;
                case Key.Home:
                    JumpToFirst();
                    e.Handled = true;
                    break;
                case Key.End:
                    JumpToLast();
                    e.Handled = true;
                    break;
            }
        }

        private void ScrollByPage(bool isUp)
        {
            var scrollViewer = FindScrollViewer(LogListView);
            if (scrollViewer == null)
                return;

            if (isUp)
            {
                scrollViewer.PageUp();
            }
            else
            {
                scrollViewer.PageDown();
            }
        }

        private void JumpToFirst()
        {
            if (LogListView.Items.Count > 0)
            {
                LogListView.ScrollIntoView(LogListView.Items[0]);
                LogListView.SelectedIndex = 0;
            }
        }

        private void JumpToLast()
        {
            if (LogListView.Items.Count > 0)
            {
                var lastIndex = LogListView.Items.Count - 1;
                LogListView.ScrollIntoView(LogListView.Items[lastIndex]);
                LogListView.SelectedIndex = lastIndex;
            }
        }

        private ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;

                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        #region Drag and Drop Event Handlers

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (IsValidFileDrop(e))
            {
                e.Effects = DragDropEffects.Copy;
                
                // Update status bar to show drag feedback
                _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
                _viewModel.StatusText = "Drop file to open";
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (IsValidFileDrop(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // Restore normal status bar
            _viewModel.UpdateStatusBarColor();
            _viewModel.UpdateStatusText();
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // Cancel any previous status bar reset timer
            _statusBarResetCts?.Cancel();
            _statusBarResetCts?.Dispose();
            _statusBarResetCts = new CancellationTokenSource();
            var currentToken = _statusBarResetCts.Token;

            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    
                    if (files != null && files.Length > 0)
                    {
                        var filePath = files[0]; // Open first file if multiple dropped
                        
                        // Validate file exists and is not a directory
                        if (File.Exists(filePath))
                        {
                            // Use the public OpenFileAsync method with validation
                            await _viewModel.OpenFileAsync(filePath, validateFile: true);
                            
                            // Show success message
                            var fileInfo = new FileInfo(filePath);
                            _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00AA66"));
                            _viewModel.StatusText = $"Opened: {fileInfo.Name}";
                            
                            // Reset status bar after 2.5 seconds
                            try
                            {
                                await Task.Delay(2500, currentToken);
                                
                                if (!currentToken.IsCancellationRequested)
                                {
                                    _viewModel.UpdateStatusBarColor();
                                    _viewModel.UpdateStatusText();
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                // Timer was cancelled - this is expected
                            }
                        }
                        else if (Directory.Exists(filePath))
                        {
                            _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438"));
                            _viewModel.StatusText = "Cannot open folders - please drop a file";
                            
                            // Reset status bar after 3 seconds
                            try
                            {
                                await Task.Delay(3000, currentToken);
                                
                                if (!currentToken.IsCancellationRequested)
                                {
                                    _viewModel.UpdateStatusBarColor();
                                    _viewModel.UpdateStatusText();
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                // Timer was cancelled - this is expected
                            }
                        }
                        else
                        {
                            _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438"));
                            _viewModel.StatusText = $"File not found: {Path.GetFileName(filePath)}";
                            
                            // Reset status bar after 3 seconds
                            try
                            {
                                await Task.Delay(3000, currentToken);
                                
                                if (!currentToken.IsCancellationRequested)
                                {
                                    _viewModel.UpdateStatusBarColor();
                                    _viewModel.UpdateStatusText();
                                }
                            }
                            catch (TaskCanceledException)
                            {
                                // Timer was cancelled - this is expected
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _viewModel.StatusBarBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438"));
                _viewModel.StatusText = $"Error opening file: {ex.Message}";
                
                // Reset status bar after 3 seconds
                try
                {
                    await Task.Delay(3000, currentToken);
                    
                    if (!currentToken.IsCancellationRequested)
                    {
                        _viewModel.UpdateStatusBarColor();
                        _viewModel.UpdateStatusText();
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timer was cancelled - this is expected
                }
            }
            finally
            {
                e.Handled = true;
            }
        }

        private bool IsValidFileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return false;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
                return false;

            // Check if the first item is a file (not a directory)
            var firstPath = files[0];
            return File.Exists(firstPath);
        }

        #endregion
    }
}