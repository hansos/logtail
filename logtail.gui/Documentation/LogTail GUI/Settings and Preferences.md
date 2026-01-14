# Settings and Preferences

LogTail GUI provides customizable settings to tailor the application to your workflow. Settings are automatically saved and restored between sessions, ensuring your preferences are always preserved.

## Opening Settings

To access the Settings dialog:

1. Click **Tools > Settings** (or press **Ctrl+,**)
2. The Settings dialog will appear

Settings are organized into three main sections: Log Format, Display Settings, and Monitoring Mode.

## Log Format Settings

LogTail GUI supports multiple log formats to parse different types of log files correctly.

### Selecting a Log Format

1. Open the Settings dialog
2. In the **Log Format** section, use the dropdown to select a format
3. Click **Apply**

### Available Log Formats

LogTail GUI includes several built-in formats and supports custom formats:

**Built-in Formats:**
- **Default**: Standard structured logging format with timestamp, level, source, and message
- **Serilog**: Optimized for Serilog-formatted logs
- **NLog**: Optimized for NLog-formatted logs
- **Log4Net**: Optimized for Log4Net-formatted logs

### Custom Log Formats

You can define custom log formats to match your specific logging structure:

1. Create or edit the file: `%AppData%\LogTail\logformats.json`
2. Define your custom format using regex patterns
3. Restart LogTail GUI or reload the log file
4. Your custom format will appear in the dropdown

**Note**: See the [LOGFORMATS.md](https://github.com/hansos/logtail/blob/master/LOGFORMATS.md) documentation for detailed information on creating custom formats.

### Log Format Best Practices

- **Choose the right format**: Select the format that matches your logging framework
- **Test custom formats**: Verify that custom formats correctly parse your logs
- **Format naming**: Use descriptive names for custom formats (e.g., "MyApp Production Logs")

## Display Settings

Display settings control how log entries are loaded and displayed.

### Tail Lines

The **Tail Lines** setting determines how many lines to display from the end of the log file.

**Configuration:**
1. Open the Settings dialog
2. In the **Display Settings** section, enter the number of lines
3. Valid range: **10 to 10,000 lines**
4. Default: **100 lines**
5. Click **Apply**

**How it works:**
- LogTail loads the last N lines from the log file
- As new entries arrive, the oldest entries are removed to maintain the limit
- Higher values show more history but use more memory

**Recommendations:**
- **Active debugging**: 100-500 lines
- **General monitoring**: 500-1,000 lines
- **Detailed analysis**: 1,000-5,000 lines
- **Historical review**: 5,000-10,000 lines

**Performance impact:**
- More lines = more memory usage
- More lines = slower refresh times
- Balance between history and performance based on your needs

### Refresh Rate

The **Refresh Rate** setting controls how often LogTail checks for new log entries.

**Configuration:**
1. Open the Settings dialog
2. In the **Display Settings** section, enter the refresh interval in seconds
3. Valid range: **1 to 60 seconds**
4. Default: **2 seconds**
5. Click **Apply**

**How it works:**
- In polling mode, LogTail re-reads the file at this interval
- In real-time mode, this serves as a fallback interval
- Lower values = more frequent updates but higher CPU usage

**Recommendations:**
- **High-volume logs**: 1-2 seconds
- **Normal development**: 2-5 seconds
- **Low-volume logs**: 5-10 seconds
- **Production monitoring**: 3-5 seconds
- **Resource-constrained systems**: 10-30 seconds

**Performance impact:**
- Faster refresh = more CPU usage
- Faster refresh = more disk I/O
- For network files, slower refresh is recommended

## Monitoring Mode

LogTail GUI offers three monitoring modes to handle different file scenarios.

### Auto Mode (Recommended)

**Description:**
Automatically selects the best monitoring approach based on the file location.

**How it works:**
- **Local files**: Uses real-time file system watching (FileSystemWatcher)
- **Network files**: Uses polling with the configured refresh rate
- **Fallback**: Switches to polling if real-time monitoring fails

**Advantages:**
- Best balance of performance and reliability
- Handles both local and network files efficiently
- Automatically adapts to file system capabilities

**Use this mode when:**
- You work with both local and network log files
- You want the best overall experience
- You're not sure which mode to use

### Real-Time Only Mode

**Description:**
Uses Windows FileSystemWatcher to detect file changes immediately.

**How it works:**
- Monitors file system events (file modified, file created, file deleted)
- Updates the display as soon as changes are detected
- No polling interval�updates are event-driven

**Advantages:**
- Instant updates when the log file changes
- Lower CPU usage (no constant polling)
- Most responsive for local files

**Limitations:**
- May not work reliably with network files
- Some file systems don't support change notifications
- Can fail if too many files are being watched

**Use this mode when:**
- You only work with local log files
- You need instant updates
- File system supports change notifications

### Polling Only Mode

**Description:**
Checks for file changes at the configured refresh rate interval.

**How it works:**
- Re-reads the file at regular intervals (e.g., every 2 seconds)
- Compares file content to detect changes
- Updates the display if changes are found

**Advantages:**
- Works with all file types (local and network)
- More reliable for network shares
- Predictable resource usage

**Limitations:**
- Slight delay between file change and display update
- Higher CPU usage than real-time mode
- More disk I/O

**Use this mode when:**
- You work with network log files
- Real-time monitoring is unreliable
- You prefer predictable, interval-based updates

## How Settings are Saved

### Automatic Saving

Settings are automatically saved when you click **Apply**:
- Changes take effect immediately
- Settings are written to disk
- No need to restart the application

### Settings Location

Settings are stored in:
```
%AppData%\LogTail\settings.json
```

This file contains:
- Display preferences (tail lines, refresh rate)
- Filter settings (selected levels, sources, message filter)
- Window state (size, position, maximized state)
- Column widths
- Last opened file
- Monitoring mode
- Log format selection

### Settings Persistence

Settings are preserved:
- **Between sessions**: Settings are restored when you restart LogTail
- **After updates**: Settings survive application updates
- **Per user**: Each Windows user has their own settings

### Resetting Settings

To reset all settings to defaults:

1. Close LogTail GUI
2. Navigate to `%AppData%\LogTail\`
3. Delete the `settings.json` file
4. Restart LogTail GUI

All settings will be reset to their default values.

## Window and Layout Settings

### Window Position and Size

LogTail automatically saves and restores:
- Window width and height
- Window position on screen
- Maximized state

**Behavior:**
- Position/size is saved when you close the application
- Restored when you reopen the application
- If window is off-screen (e.g., monitor disconnected), it resets to default

### Column Widths

The log entry columns automatically adjust, but manual adjustments are saved:
- Timestamp column width
- Level column width
- Source column width
- Message column width

**Behavior:**
- Drag column separators to resize
- Widths are saved automatically
- Restored when you reopen the application

## Recent Files

LogTail maintains a list of recently opened files.

### How It Works

- Recently opened files are saved automatically
- Files appear in **File > Recent Files** menu
- Files are ordered by most recently used
- Non-existent files are automatically removed from the list

### Clearing Recent Files

To clear the recent files list:
1. Delete the `settings.json` file (see "Resetting Settings" above)
2. Or edit the JSON file manually to remove recent files

## Advanced Settings

### Editing Settings Manually

You can manually edit the `settings.json` file while LogTail is closed:

1. Close LogTail GUI
2. Navigate to `%AppData%\LogTail\`
3. Open `settings.json` in a text editor
4. Make your changes
5. Save and restart LogTail

**Example settings.json:**
```json
{
  "Window": {
    "Width": 1200,
    "Height": 800,
    "Left": 100,
    "Top": 100,
    "IsMaximized": false
  },
  "Filter": {
    "SelectedLevels": ["ERROR", "FATAL"],
    "SelectedSources": ["PaymentService"],
    "MessageFilter": "exception",
    "LogFormatName": "Default"
  },
  "Preferences": {
    "RefreshRateSeconds": 2,
    "TailLines": 500,
    "LastOpenedFile": "C:\\Logs\\app.log",
    "MonitoringMode": 0
  }
}
```

**Warning**: Invalid JSON will cause LogTail to reset to default settings.

### MonitoringMode Values

When editing manually:
- `0` = Auto
- `1` = RealTimeOnly
- `2` = PollingOnly

## Common Settings Scenarios

### Scenario 1: Optimized for Development

**Configuration:**
- Tail Lines: **500**
- Refresh Rate: **2 seconds**
- Monitoring Mode: **Auto**
- Log Format: **Default** (or your framework's format)

**Purpose:** Balanced performance for active development with quick updates.

### Scenario 2: Optimized for Production Monitoring

**Configuration:**
- Tail Lines: **1000**
- Refresh Rate: **5 seconds**
- Monitoring Mode: **Auto**
- Log Format: **Default** (or your framework's format)
- Filter: Only show WARNING, ERROR, FATAL levels

**Purpose:** Monitor production logs without overwhelming the system.

### Scenario 3: Network Log Files

**Configuration:**
- Tail Lines: **500**
- Refresh Rate: **10 seconds**
- Monitoring Mode: **Polling Only**
- Log Format: **Default** (or your framework's format)

**Purpose:** Reliable monitoring of network-based log files.

### Scenario 4: Maximum History

**Configuration:**
- Tail Lines: **10,000**
- Refresh Rate: **5 seconds**
- Monitoring Mode: **Auto**
- Log Format: **Default** (or your framework's format)

**Purpose:** Keep maximum log history in memory for detailed analysis.

### Scenario 5: Low Resource Usage

**Configuration:**
- Tail Lines: **100**
- Refresh Rate: **30 seconds**
- Monitoring Mode: **Polling Only**
- Log Format: **Default** (or your framework's format)

**Purpose:** Minimize CPU and memory usage on resource-constrained systems.

## Troubleshooting Settings

### Settings Not Being Saved

**Possible causes:**
- No write permissions to `%AppData%\LogTail\`
- Disk is full
- Antivirus blocking file writes

**Solutions:**
- Check folder permissions
- Free up disk space
- Add LogTail to antivirus exceptions

### Settings Reset on Every Launch

**Possible causes:**
- `settings.json` file is corrupted
- File system permissions issue
- File is set to read-only

**Solutions:**
- Delete `settings.json` and restart
- Check file permissions
- Remove read-only attribute from the file

### Custom Log Format Not Appearing

**Possible causes:**
- `logformats.json` has syntax errors
- File is in wrong location
- Format name conflicts with built-in format

**Solutions:**
- Validate JSON syntax
- Ensure file is in `%AppData%\LogTail\logformats.json`
- Use unique names for custom formats
- Check the LOGFORMATS.md documentation

## Best Practices

### Start with Defaults

- Use default settings initially
- Adjust based on your specific needs
- Monitor performance and adjust accordingly

### Match Settings to File Size

- **Small files (< 1 MB)**: Higher tail lines, faster refresh
- **Large files (> 100 MB)**: Lower tail lines, slower refresh
- **Very large files (> 1 GB)**: Minimal tail lines, polling mode

### Test Settings Changes

- Apply settings changes while monitoring a live log
- Observe CPU and memory usage
- Adjust if performance is impacted

### Save Your Configuration

- Once you find optimal settings, note them down
- Export `settings.json` as a backup
- Share configurations with your team

## Next Steps

Now that you understand settings, explore:

- [[Opening and Monitoring Log Files]] - Learn about log file monitoring
- [[Filtering Logs]] - Master filtering techniques
- [[Visual Studio Integration]] - Navigate from logs to source code
- [[Keyboard Shortcuts]] - Work more efficiently

---

*Fine-tune LogTail GUI to match your workflow for maximum productivity!*
