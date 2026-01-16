using System.Windows;

namespace logtail.gui;

public partial class AnnotationDialog : Window
{
    public string? AnnotationText { get; private set; }

    public AnnotationDialog(string? existingAnnotation = null)
    {
        InitializeComponent();
        
        if (!string.IsNullOrWhiteSpace(existingAnnotation))
        {
            AnnotationTextBox.Text = existingAnnotation;
        }
        
        AnnotationTextBox.Focus();
        AnnotationTextBox.SelectAll();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        AnnotationText = string.IsNullOrWhiteSpace(AnnotationTextBox.Text) ? null : AnnotationTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }
}
