using System.Collections.Generic;
using LogTail.Core.Models;

namespace logtail.gui.Models;

public class ApplicationSettings
{
    // Window State
    public WindowSettings Window { get; set; } = new();

    // Filter Preferences
    public FilterSettings Filter { get; set; } = new();

    // Application Preferences
    public AppPreferences Preferences { get; set; } = new();
}

public class WindowSettings
{
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 600;
    public double Left { get; set; } = 100;
    public double Top { get; set; } = 100;
    public bool IsMaximized { get; set; } = false;

    // Column widths
    public double TimeColumnWidth { get; set; } = double.NaN;
    public double LevelColumnWidth { get; set; } = double.NaN;
    public double SourceColumnWidth { get; set; } = double.NaN;
    public double MessageColumnWidth { get; set; } = double.NaN;
}

public class FilterSettings
{
    public List<string> SelectedLevels { get; set; } = new();
    public List<string> SelectedSources { get; set; } = new();
    public string? MessageFilter { get; set; }
    public string LogFormatName { get; set; } = "Default";
}

public class AppPreferences
{
    public int RefreshRateSeconds { get; set; } = 2;
    public int TailLines { get; set; } = 100;
    public string? LastOpenedFile { get; set; }
    public MonitoringMode MonitoringMode { get; set; } = MonitoringMode.Auto;
}
