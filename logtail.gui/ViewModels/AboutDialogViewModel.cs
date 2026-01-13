using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace logtail.gui.ViewModels;

public class AboutDialogViewModel : INotifyPropertyChanged
{
    private string _version = string.Empty;
    private string _licenseText = string.Empty;

    public string ApplicationName => "LogTail";

    public string Description =>
        "A modern, feature-rich log file viewer for Windows with Visual Studio integration. " +
        "Features real-time monitoring, advanced filtering by log level and source, " +
        "Visual Studio integration for opening files from stack traces, and smart clipboard operations.";

    public string Version
    {
        get => _version;
        set
        {
            _version = value;
            OnPropertyChanged();
        }
    }

    public string LicenseText
    {
        get => _licenseText;
        set
        {
            _licenseText = value;
            OnPropertyChanged();
        }
    }

    public AboutDialogViewModel()
    {
        LoadVersion();
        LoadLicense();
    }

    private void LoadVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            if (version != null)
            {
                Version = $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                Version = "Version 1.0.0";
            }
        }
        catch
        {
            Version = "Version 1.0.0";
        }
    }

    private void LoadLicense()
    {
        try
        {
            // Try to find LICENSE.txt relative to the application
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var licensePath = Path.Combine(appDirectory, "LICENSE.txt");

            // If not found in app directory, try parent directories
            if (!File.Exists(licensePath))
            {
                var parentDir = Directory.GetParent(appDirectory);
                while (parentDir != null && !File.Exists(licensePath))
                {
                    licensePath = Path.Combine(parentDir.FullName, "LICENSE.txt");
                    if (File.Exists(licensePath))
                        break;
                    parentDir = parentDir.Parent;
                }
            }

            if (File.Exists(licensePath))
            {
                LicenseText = File.ReadAllText(licensePath);
            }
            else
            {
                LicenseText = "MIT License\n\n" +
                             "Copyright (c) 2026 Hans Olav Sorteberg.\n\n" +
                             "Permission is hereby granted, free of charge, to any person obtaining a copy\n" +
                             "of this software and associated documentation files (the \"Software\"), to deal\n" +
                             "in the Software without restriction, including without limitation the rights\n" +
                             "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell\n" +
                             "copies of the Software, and to permit persons to whom the Software is\n" +
                             "furnished to do so, subject to the following conditions:\n\n" +
                             "The above copyright notice and this permission notice shall be included in all\n" +
                             "copies or substantial portions of the Software.\n\n" +
                             "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR\n" +
                             "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,\n" +
                             "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE\n" +
                             "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER\n" +
                             "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,\n" +
                             "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE\n" +
                             "SOFTWARE.";
            }
        }
        catch (Exception ex)
        {
            LicenseText = $"Unable to load license file: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
