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
