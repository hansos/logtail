# Filtering Logs

LogTail GUI provides powerful filtering capabilities to help you focus on the log entries that matter most. With filters, you can quickly narrow down thousands of log entries to find specific issues, patterns, or information.

## Opening the Filter Dialog

To access filtering options:

1. Click **View > Filter** (or press **Ctrl+F**)
2. The Filter Settings dialog will appear

The filter settings are organized into three main sections: Log Levels, Message Filter, and Sources.

## Log Level Filtering

Log level filtering allows you to show or hide log entries based on their severity level.

### Available Log Levels

LogTail GUI recognizes the following log levels:

- **VERBOSE**: Detailed diagnostic information
- **DBUG** (Debug): Debug-level messages
- **INFO**: Informational messages
- **WARNING**: Warning messages that indicate potential issues
- **ERROR**: Error messages for failures
- **EROR**: Alternative error format
- **FATAL**: Critical failures that cause application termination

### How to Filter by Log Level

1. Open the Filter Settings dialog
2. In the **Log Levels** section, check or uncheck the log levels you want to display
3. Click **Apply**

**Tips:**
- By default, all log levels are shown
- Uncheck levels you want to hide
- Common filtering scenarios:
  - **Troubleshooting**: Show only WARNING, ERROR, EROR, and FATAL
  - **Active Development**: Show all levels
  - **Production Monitoring**: Show only ERROR, EROR, and FATAL

### Filter Behavior

- If **all levels are checked**: All log entries are displayed
- If **some levels are unchecked**: Only checked levels are shown
- The filter applies immediately when you click **Apply**

## Message Filter (Text Search)

The message filter allows you to search for specific text within log messages.

### Using Message Filter

1. Open the Filter Settings dialog
2. In the **Message Filter** section, enter your search text in the text box
3. Click **Apply**

**Features:**
- **Case-insensitive**: Searches are not case-sensitive ("error" matches "Error" and "ERROR")
- **Partial matching**: Finds text anywhere in the message
- **Multi-line support**: Searches through stack traces and multi-line messages

### Filter Examples

- Search for a specific exception: `NullReferenceException`
- Search for a method name: `ProcessOrder`
- Search for a file name: `UserController.cs`
- Search for an error code: `ERR-1001`
- Search for a user ID: `user_12345`

### Clearing Message Filter

To remove the message filter:
1. Open the Filter Settings dialog
2. Clear the text in the **Message Filter** text box
3. Click **Apply**

## Source Filtering

Source filtering allows you to show log entries from specific components, classes, or modules only.

### What are Sources?

Sources are typically:
- Class names (e.g., `UserService`, `OrderController`)
- Component names (e.g., `Database`, `API`)
- Module names (e.g., `Payment`, `Authentication`)

The source is automatically extracted from the log entry by the log parser.

### Using Source Filtering

1. Open the Filter Settings dialog
2. In the **Sources** section, you'll see a scrollable list of all sources found in the current log file
3. Check or uncheck the sources you want to display
4. Click **Apply**

**Features:**
- **Dynamic list**: The source list updates automatically as new sources appear in the log
- **Scrollable**: For log files with many sources, the list is scrollable
- **Selective filtering**: Choose exactly which sources to display

### Filter Behavior

- If **all sources are checked**: All log entries are displayed (no source filter applied)
- If **some sources are unchecked**: Only log entries from checked sources are shown
- Sources appear in alphabetical order

### Common Source Filtering Scenarios

**Focus on a single component:**
1. Uncheck all sources
2. Check only the source you're interested in (e.g., `PaymentService`)
3. Apply

**Exclude noisy sources:**
1. Uncheck sources that generate too much noise (e.g., `HealthCheck`)
2. Keep all other sources checked
3. Apply

## Combining Filters

You can combine all three filter types for powerful, precise filtering:

### Example 1: Finding Critical Payment Errors

1. **Log Levels**: Check only ERROR, EROR, and FATAL
2. **Message Filter**: Enter `payment`
3. **Sources**: Check only `PaymentService` and `PaymentGateway`
4. Click **Apply**

Result: Shows only error-level messages containing "payment" from payment-related sources.

### Example 2: Debugging a Specific Class

1. **Log Levels**: Check all levels
2. **Message Filter**: Leave empty
3. **Sources**: Check only `UserController`
4. Click **Apply**

Result: Shows all log entries from UserController, regardless of level or content.

### Example 3: Production Monitoring

1. **Log Levels**: Check only WARNING, ERROR, EROR, and FATAL
2. **Message Filter**: Leave empty
3. **Sources**: Uncheck `HealthCheck` and other noisy sources
4. Click **Apply**

Result: Shows all warnings and errors except from noisy sources.

## How Filters Work

### Filter Application

When you apply filters:

1. LogTail GUI re-reads the log file
2. Applies all active filters
3. Updates the display with matching entries only
4. Shows the number of filtered entries in the status bar

### Real-Time Filtering

Filters continue to apply to new log entries as they arrive:
- New entries are automatically checked against active filters
- Only matching entries are displayed
- The filter remains active until you change or clear it

### Performance Considerations

- **Heavy filtering**: Very restrictive filters may result in few or no displayed entries
- **Message filter performance**: Text search is fast, even on large log files
- **Multiple filters**: Combining multiple filters can significantly reduce displayed entries

## Filter Status Indication

The status bar shows information about active filters:

**Example status bar messages:**
- `Levels: 3 selected` - Only 3 log levels are being shown
- `Sources: 2 selected` - Only 2 sources are being shown
- `Filter: 'exception'` - A message filter for "exception" is active

This helps you remember what filters are currently applied.

## Clearing Filters

To remove all filters and show all log entries:

### Option 1: Clear Individual Filters

1. Open the Filter Settings dialog
2. Check all log levels
3. Clear the message filter text
4. Check all sources
5. Click **Apply**

### Option 2: Close and Reopen File

Closing and reopening the file will reset filters to their default state.

## Filter Persistence

Filter settings are **saved automatically** when you apply them:

- Filters are preserved when you close and reopen LogTail GUI
- Filters are saved per-session in the application settings
- When you reopen the same log file, your last filters are restored

**Note**: Specific source selections are remembered between sessions, making it easy to return to your preferred view.

## Best Practices

### Start Broad, Then Narrow

1. Open the log file and view all entries
2. Identify patterns (common sources, log levels, error messages)
3. Apply filters to focus on specific areas
4. Further refine as needed

### Use Message Filter for Quick Searches

- Instead of scrolling through thousands of lines, use the message filter to jump to relevant entries
- Combine with log level filtering to find "all errors containing X"

### Save Time with Source Filtering

- If you're working on a specific feature, filter to show only relevant sources
- Exclude sources you know are working correctly to reduce noise

### Combine with Visual Studio Integration

1. Filter to show only errors
2. Double-click stack traces to open files in Visual Studio
3. Fix the issue
4. Monitor the filtered view to verify the fix

### Reset Filters When Switching Context

- When moving from debugging one issue to another, clear your filters
- This ensures you don't miss new issues because of old filters

## Troubleshooting Filters

### No Entries Displayed

If the log view is empty after applying filters:

- **Check your filters**: You may have filters that are too restrictive
- **Verify log levels**: Ensure at least one log level is checked
- **Check sources**: Ensure at least one source is checked
- **Clear message filter**: Remove text filters to see if entries appear

### Filters Not Working

- **Verify filter was applied**: Check the status bar for filter indicators
- **Refresh manually**: Press F5 to force a refresh
- **Check the log file**: Ensure the log file actually contains entries matching your filter

### Missing Sources in List

- **Wait for sources to appear**: Sources are discovered as log entries are parsed
- **Check log format**: Ensure your log format correctly extracts source information
- **Refresh the filter dialog**: Close and reopen the filter dialog to update the source list

## Keyboard Shortcuts

- **Ctrl+F**: Open Filter Settings dialog
- **F5**: Refresh and reapply filters

## Next Steps

Now that you understand filtering, explore:

- [[Opening and Monitoring Log Files]] - Learn about real-time monitoring
- [[Settings and Preferences]] - Customize display and monitoring preferences
- [[Visual Studio Integration]] - Navigate from filtered errors to source code
- [[Keyboard Shortcuts]] - Work more efficiently

---

*Effective filtering is key to efficient log analysis. Master these techniques to find issues faster!*
