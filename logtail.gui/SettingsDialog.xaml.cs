using System.Windows;
using logtail.gui.ViewModels;

namespace logtail.gui;

public partial class SettingsDialog : Window
{
    public SettingsDialogViewModel ViewModel { get; }
    public bool WasApplied { get; private set; }

    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        
        // Set up commands for keyboard shortcuts
        ViewModel.ApplyCommand = new RelayCommand(_ => Apply_Click(this, new RoutedEventArgs()));
        ViewModel.CancelCommand = new RelayCommand(_ => Cancel_Click(this, new RoutedEventArgs()));
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        WasApplied = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        WasApplied = false;
        Close();
    }
}
