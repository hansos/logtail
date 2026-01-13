using System.Windows;
using logtail.gui.ViewModels;

namespace logtail.gui;

public partial class AboutDialog : Window
{
    public AboutDialog(AboutDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
