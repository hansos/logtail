# Opening and Monitoring Log Files

This guide explains how to open, view, and monitor log files using LogTail GUI.

## Opening Log Files

### Automatic Startup

LogTail GUI automatically opens the last file you were working with when you launch the application. This means you can close LogTail at any time and when you reopen it, it will immediately resume monitoring your most recent log file.

### Method 1: File Menu

1. Launch LogTail GUI
2. Click **File > Open Log File** (or press **Ctrl+O**)
3. Browse to your log file location
4. Select your `.log` file (or any text file)
5. Click **Open**

The application will immediately load and display the log file content.

### Method 2: Recent Files

LogTail GUI maintains a list of recently opened files for quick access:

1. Click **File > Recent Files**
2. Select a file from the list
3. The file will open immediately

Recent files are preserved between sessions, making it easy to return to the logs you work with frequently.

### Method 3: Drag and Drop

*(Currently not supported)*

## Understanding the Display

Once a log file is opened, LogTail GUI displays the content in a structured, easy-to-read format:

### Column Layout

- **Timestamp**: When the log entry was created
- **Level**: The severity level (Verbose, Debug, Info, Warning, Error, Fatal)
- **Source**: The component or class that generated the log entry
- **Message**: The log message content, including stack traces

### Color Coding

Log entries are color-coded by level for quick visual identification:
- **Verbose**: Light gray text
- **Debug**: Gray text
- **Info**: White text
- **Warning**: Yellow/amber text
- **Error**: Orange/red text
- **Fatal**: Bright red text

## Real-Time Monitoring

LogTail GUI automatically monitors your log file for changes and updates the display. Depending on your settings, it can monitor in real-time (detecting file changes immediately) or by interval (refreshing at a set time period).

### How Auto-Refresh Works

When you open a log file, LogTail GUI:
1. Loads the last N lines (configurable, default 100)
2. Monitors the file for changes (real-time or interval-based)
3. Automatically refreshes at the configured interval (default 2 seconds) or when file changes are detected
4. Displays new entries as they're written to the log file

### Monitoring Modes

LogTail GUI supports two monitoring modes (configurable in Settings):

#### Tail Mode (Default)
- Displays the last N lines from the log file
- Automatically scrolls to show the newest entries
- Perfect for watching live application behavior

#### Full File Mode
- Loads the entire log file
- Allows scrolling through the complete history
- Best for analyzing historical logs
*(Full file mode Currently not supported)*
## Viewing Large Files

LogTail GUI is designed to handle large log files efficiently:

### Tail Lines Configuration

You can configure how many lines to display:
1. Open **View > Filter**
2. Adjust **Tail Lines** (10-10,000)
3. Click **OK**

**Recommendations:**
- For active debugging: 100-500 lines
- For general monitoring: 500-1,000 lines
- For detailed analysis: 1,000-10,000 lines

### Performance Considerations

- Displaying more lines requires more memory
- Very large tail line values may impact refresh performance
- The application automatically handles file growth

## Auto-Refresh Settings

### Adjusting Refresh Rate

To change how often LogTail updates:
1. Click **View > Filter**
2. Adjust **Refresh Rate** (1-60 seconds)
3. Click **OK**

**Recommendations:**
- For high-volume logs: 1-2 seconds
- For normal usage: 2-5 seconds
- For low-volume logs: 5-10 seconds

### Pausing Auto-Refresh

To temporarily stop auto-refresh while examining logs:
- Close the file (**File > Close**) *(Currently not supported)* , or
- Change the refresh rate to a higher value

## Status Bar Information

The status bar at the bottom of the window displays:
*(Currently not fully supported)*
- **Current file path**: The log file being monitored
- **Entry count**: Number of log entries currently displayed
- **Status**: Current application state (Ready, Refreshing, Error)
- **Visual indicator**: Color-coded background showing application health
  - Blue: Normal operation
  - Yellow: Warning or limited functionality
  - Red: Error state

## File Monitoring Features

### Automatic File Change Detection

LogTail GUI uses advanced file monitoring to detect changes:
- Detects when new content is written to the file
- Handles file rotation (when log files are renamed/rotated) *(Currently not supported)*
- Recovers automatically if the file is temporarily locked 
- Shows a warning if the file is deleted *(Currently not supported)*

### File Deleted Notification

If the monitored log file is deleted:
*(Currently not supported)*
1. LogTail displays a warning in the status bar
2. The current content remains visible
3. You can open a new file or wait for the file to be recreated

## Best Practices

### For Active Development

1. Open your application's log file
2. Set refresh rate to 2 seconds
3. Set tail lines to 500
4. Use filtering to focus on specific log levels or sources

### For Production Monitoring

1. Open the production log file
2. Set refresh rate to 5-10 seconds
3. Filter to show only Warning, Error, and Fatal levels
4. Keep the window visible on a secondary monitor

### For Log Analysis

1. Open the log file
2. Increase tail lines to 5,000-10,000
3. Use text filtering to search for specific errors or patterns
4. Use the Smart Clipboard feature to copy relevant entries

## Troubleshooting

### File Won't Open

- **Check file permissions**: Ensure you have read access to the file
- **Check file lock**: Some applications lock log files exclusively
- **Try a different monitoring mode**: Switch between Tail and Full File modes

### Updates Not Appearing

- **Verify auto-refresh is enabled**: Check the refresh rate setting
- **Check if file is being written**: Ensure your application is actually writing logs
- **Manual refresh**: Press F5 to force an update
- **Check file path**: Ensure the file hasn't been moved or renamed

### Performance Issues

- **Reduce tail lines**: Lower the number of displayed lines
- **Increase refresh rate**: Refresh less frequently
- **Filter aggressively**: Show only the log levels you need
- **Close other applications**: Free up system resources

## Next Steps

Now that you know how to open and monitor log files, learn about:
- [[Filtering Logs]] - Focus on what matters with powerful filtering
- [[Visual Studio Integration]] - Navigate directly to code from stack traces
- [[Settings and Preferences]] - Customize LogTail to your workflow
- [[Keyboard Shortcuts]] - Work more efficiently with shortcuts

---

*Need more help? Check out the other documentation pages or visit the project repository.*
