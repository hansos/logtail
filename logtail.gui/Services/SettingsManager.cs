using System;
using System.IO;
using System.Text.Json;
using logtail.gui.Models;

namespace logtail.gui.Services;

public class SettingsManager
{
    private readonly string _settingsPath;
    private ApplicationSettings _currentSettings;

    public SettingsManager()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LogTail"
        );
        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");
        _currentSettings = new ApplicationSettings();
    }

    /// <summary>
    /// Loads settings from disk. Returns default settings if file doesn't exist or is corrupted.
    /// </summary>
    public ApplicationSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json);
                
                if (settings != null)
                {
                    _currentSettings = settings;
                    return _currentSettings;
                }
            }
        }
        catch (Exception)
        {
            // If there's any error reading or deserializing the file, fall back to defaults
            // We could log this error in the future
        }

        // Return default settings
        _currentSettings = new ApplicationSettings();
        return _currentSettings;
    }

    /// <summary>
    /// Saves the current settings to disk.
    /// </summary>
    public void SaveSettings(ApplicationSettings settings)
    {
        try
        {
            _currentSettings = settings;
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception)
        {
            // Silently fail if we can't save
            // In a production app, we might want to log this
        }
    }

    /// <summary>
    /// Gets the current settings without loading from disk.
    /// </summary>
    public ApplicationSettings GetCurrentSettings()
    {
        return _currentSettings;
    }

    /// <summary>
    /// Deletes the settings file, reverting to defaults on next load.
    /// </summary>
    public void ResetSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                File.Delete(_settingsPath);
            }
            _currentSettings = new ApplicationSettings();
        }
        catch (Exception)
        {
            // Silently fail
        }
    }
}
