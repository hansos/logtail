using System.Text.Json;
using LogTail.Core.Models;

namespace LogTail.Core.Services;

public sealed class LogFormatService
{
    private readonly string _customFormatsPath;
    private List<LogFormat> _allFormats;
    private string? _lastLoadError;

    public LogFormatService(string? customFormatsPath = null)
    {
        _customFormatsPath = customFormatsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LogTail",
            "logformats.json"
        );

        _allFormats = new List<LogFormat>();
        LoadFormats();
    }

    public string CustomFormatsPath => _customFormatsPath;
    public string? LastLoadError => _lastLoadError;

    public List<LogFormat> GetAllFormats()
    {
        return new List<LogFormat>(_allFormats);
    }

    public LogFormat? GetFormatByName(string name)
    {
        return _allFormats.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public LogFormat GetDefaultFormat()
    {
        return _allFormats.FirstOrDefault(f => f.Name == "Default") ?? LogFormat.CreateDefault();
    }

    public void SaveCustomFormat(LogFormat format)
    {
        if (format.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot modify built-in formats");
        }

        // Remove existing format with same name if it exists
        _allFormats.RemoveAll(f => !f.IsBuiltIn && f.Name.Equals(format.Name, StringComparison.OrdinalIgnoreCase));
        
        // Add the new/updated format
        _allFormats.Add(format);

        // Save to disk
        SaveCustomFormatsToFile();
    }

    public void DeleteCustomFormat(string name)
    {
        var format = _allFormats.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        
        if (format == null)
        {
            return;
        }

        if (format.IsBuiltIn)
        {
            throw new InvalidOperationException("Cannot delete built-in formats");
        }

        _allFormats.Remove(format);
        SaveCustomFormatsToFile();
    }

    private void LoadFormats()
    {
        _allFormats.Clear();
        _lastLoadError = null;

        // Load built-in formats
        _allFormats.AddRange(LogFormat.GetBuiltInFormats());

        // Load custom formats from file
        try
        {
            if (File.Exists(_customFormatsPath))
            {
                var json = File.ReadAllText(_customFormatsPath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                List<LogFormat>? customFormats = null;
                
                // Try to deserialize as array first
                try
                {
                    customFormats = JsonSerializer.Deserialize<List<LogFormat>>(json, options);
                }
                catch (JsonException)
                {
                    // If that fails, try to deserialize as single object
                    try
                    {
                        var singleFormat = JsonSerializer.Deserialize<LogFormat>(json, options);
                        if (singleFormat != null)
                        {
                            customFormats = new List<LogFormat> { singleFormat };
                            _lastLoadError = "WARNING: File should be a JSON array. Loaded single format successfully, but please wrap it in [] for proper format.";
                        }
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException($"File must be either a JSON array of formats or a single format object. {ex.Message}");
                    }
                }
                
                if (customFormats != null && customFormats.Count > 0)
                {
                    // Ensure custom formats are marked as not built-in
                    foreach (var format in customFormats)
                    {
                        format.IsBuiltIn = false;
                    }
                    _allFormats.AddRange(customFormats);
                }
            }
        }
        catch (Exception ex)
        {
            // Store the error but continue with built-in formats
            _lastLoadError = $"Error loading custom formats: {ex.Message}";
        }
    }

    private void SaveCustomFormatsToFile()
    {
        try
        {
            var customFormats = _allFormats.Where(f => !f.IsBuiltIn).ToList();
            
            var directory = Path.GetDirectoryName(_customFormatsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(customFormats, options);
            File.WriteAllText(_customFormatsPath, json);
        }
        catch (Exception)
        {
            // Silently fail if we can't save
        }
    }

    public void ReloadFormats()
    {
        LoadFormats();
    }
}
