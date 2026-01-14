# Custom Log Formats Feature

## Overview

LogTail now supports custom log formats through configuration, allowing you to parse logs from various logging frameworks and custom applications without code changes.

## Built-in Formats

LogTail comes with the following pre-configured log formats:

### 1. Default
- **Description**: Default LogTail format with timestamp, level, source, and message
- **Pattern**: `YYYY-MM-DD HH:mm:ss.fff [LEVEL] Source Message`
- **Supported Levels**: VERBOSE, DBUG, INFO, WARNING, ERROR, EROR, FATAL

### 2. Serilog
- **Description**: Serilog default format
- **Pattern**: `YYYY-MM-DD HH:mm:ss.fff +00:00 [LEVEL] Message`
- **Supported Levels**: VRB, DBG, INF, WRN, ERR, FTL

### 3. NLog
- **Description**: NLog default format
- **Pattern**: `YYYY-MM-DD HH:mm:ss.fff LEVEL Source Message`
- **Supported Levels**: TRACE, DEBUG, INFO, WARN, ERROR, FATAL

### 4. log4net
- **Description**: log4net default format
- **Pattern**: `YYYY-MM-DD HH:mm:ss,fff [LEVEL] Source - Message`
- **Supported Levels**: DEBUG, INFO, WARN, ERROR, FATAL

## Using Custom Formats

### Selecting a Format

1. Open LogTail
2. Click on the "Settings" button (or press the settings shortcut)
3. In the "Log Format" section at the top-left of the Settings dialog, select your desired format from the dropdown
4. Click "Apply"

The selected format will be persisted and used for all subsequent log file parsing.

### Creating Custom Formats

Custom log formats are stored in `%APPDATA%\LogTail\logformats.json`. You can create custom formats by adding entries to this file.

**Important**: The file must be a JSON array (wrapped in `[` and `]`), even if you only have one format.

#### Format Structure

The file should contain a JSON array of format objects:

```json
[
  {
    "Name": "Your Format Name",
    "Description": "Description of your format",
    "FullLogPattern": "^(?<timestamp>...)\\s+\\[(?<level>...)\\]\\s+(?<source>...)\\s+(?<message>.*)$",
    "LevelPattern": "\\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\\]",
    "LevelMappings": {
      "TRACE": "Verbose",
      "DEBUG": "Debug",
      "INFO": "Info",
      "WARN": "Warning",
      "ERROR": "Error",
      "FATAL": "Fatal"
    },
    "IsBuiltIn": false
  }
]
```

To add multiple formats, add more objects inside the array:

```json
[
  {
    "Name": "Format 1",
    ...
  },
  {
    "Name": "Format 2",
    ...
  }
]
```

#### Pattern Details

- **FullLogPattern**: A .NET regular expression with named capture groups:
  - `(?<timestamp>...)`: Captures the timestamp portion
  - `(?<level>...)`: Captures the log level
  - `(?<source>...)`: Captures the source/logger name (optional)
  - `(?<message>...)`: Captures the message text

- **LevelPattern**: A simpler regex pattern used for quick level detection

- **LevelMappings**: Maps your log levels to LogTail's internal levels (Verbose, Debug, Info, Warning, Error, Fatal)

#### Example: Custom ISO 8601 Format

```json
{
  "Name": "ISO8601",
  "Description": "ISO 8601 timestamp format",
  "FullLogPattern": "^(?<timestamp>\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}\\.\\d{3}Z)\\s+\\[(?<level>[^\\]]+)\\]\\s+(?<source>[^\\s]+)\\s+(?<message>.*)$",
  "LevelPattern": "\\[(?<level>TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\\]",
  "LevelMappings": {
    "TRACE": "Verbose",
    "DEBUG": "Debug",
    "INFO": "Info",
    "WARN": "Warning",
    "ERROR": "Error",
    "FATAL": "Fatal"
  },
  "IsBuiltIn": false
}
```

### Testing Your Format

1. Create or edit `%APPDATA%\LogTail\logformats.json`
2. Add your custom format
3. Restart LogTail (custom formats are loaded at startup)
4. Open Settings dialog and select your custom format
5. Click Apply and open a log file that matches your format

If the format doesn't parse correctly, check:
- Regex patterns are valid .NET regular expressions
- Named capture groups are properly defined
- Level mappings match the levels in your LevelPattern

## Format Persistence

- Selected format is saved in application settings
- Custom formats persist between sessions
- Built-in formats cannot be modified or deleted

## Troubleshooting

### Format Not Appearing
- Ensure `logformats.json` is valid JSON
- Restart LogTail after adding custom formats
- Check file location: `%APPDATA%\LogTail\logformats.json`

### Logs Not Parsing Correctly
- Verify your regex pattern matches your log lines
- Test regex patterns at https://regex101.com/ (select .NET flavor)
- Check that named capture groups are spelled correctly
- Ensure level mappings cover all your log levels

### Color Coding Not Working
- Verify LevelMappings correctly map to internal levels
- Check that your LevelPattern matches log levels in your files

## Technical Details

### Files Modified
- `LogTail.Core\Models\LogFormat.cs` - Log format model
- `LogTail.Core\Services\LogFormatService.cs` - Format management service
- `LogTail.Core\Services\LogParser.cs` - Updated to use configurable formats
- `LogTail.Core\Models\LogTailOptions.cs` - Added LogFormatName property
- `logtail.gui\Models\ApplicationSettings.cs` - Added format persistence
- `logtail.gui\ViewModels\SettingsDialogViewModel.cs` - Format selection UI
- `logtail.gui\SettingsDialog.xaml` - Format dropdown control
- `logtail.gui\ViewModels\MainViewModel.cs` - Format service integration

### Storage Locations
- Application Settings: `%APPDATA%\LogTail\settings.json`
- Custom Formats: `%APPDATA%\LogTail\logformats.json`

## Future Enhancements

Potential future improvements:
- Format editor UI within LogTail
- Format validation and testing tool
- Import/export format configurations
- Sample log line preview in format selection
- Format-specific color schemes
