using System.Windows;
using System.Windows.Controls;
using logtail.gui.ViewModels;

namespace logtail.gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            SizeChanged += MainWindow_SizeChanged;
            
            _viewModel.LogEntries.CollectionChanged += (s, e) => 
            {
                Dispatcher.BeginInvoke(() => UpdateColumnWidths(), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.Start();
            UpdateColumnWidths();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Stop();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnWidths();
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
            
            var scrollBarWidth = 20; // Estimate for vertical scrollbar
            var remainingWidth = LogListView.ActualWidth - totalFixedWidth - scrollBarWidth;
            
            // Set Message column to fill remaining space (minimum 200px)
            gridView.Columns[3].Width = Math.Max(200, remainingWidth);
        }
    }
}