using System.IO;
using System.Text.Json;

namespace logtail.gui.Services;

public class RecentFilesManager
{
    private const int MaxRecentFiles = 3;
    private readonly string _settingsPath;

    public RecentFilesManager()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LogTail"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "recentfiles.json");
    }

    public List<string> GetRecentFiles()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }
        catch
        {
            // If there's any error reading the file, return empty list
        }

        return new List<string>();
    }

    public void AddRecentFile(string filePath)
    {
        var recentFiles = GetRecentFiles();

        // Remove the file if it already exists (we'll add it at the front)
        recentFiles.Remove(filePath);

        // Add to the front
        recentFiles.Insert(0, filePath);

        // Keep only the most recent files
        if (recentFiles.Count > MaxRecentFiles)
        {
            recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
        }

        SaveRecentFiles(recentFiles);
    }

    private void SaveRecentFiles(List<string> recentFiles)
    {
        try
        {
            var json = JsonSerializer.Serialize(recentFiles, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Silently fail if we can't save
        }
    }
}
