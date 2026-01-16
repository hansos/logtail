# Bookmarks and Annotations Feature

## Overview
The LogTail application now supports bookmarks and annotations to help you mark and annotate important log entries during analysis.

## Features

### Bookmarks
- **Toggle Bookmark**: Mark log entries for quick reference
- **Visual Indicator**: Gold star (?) icon displayed in the bookmark column
- **Keyboard Shortcut**: `Ctrl+B` to toggle bookmark on selected line
- **Navigation**: 
  - `F2` - Jump to next bookmark
  - `Shift+F2` - Jump to previous bookmark
  - Navigation wraps around (goes to first/last bookmark at boundaries)

### Annotations
- **Add Notes**: Add text annotations to log entries
- **Visual Indicator**: Note icon (??) displayed in the annotation column
- **View Annotations**: Hover over the note icon to see the annotation text
- **Edit/Remove**: Right-click and select "Add/Edit Annotation..." or use the context menu
- **Rich Text**: Supports multi-line text annotations

### Context Menu
Right-click on any log entry to access:
- **Toggle Bookmark** - Quick bookmark toggle
- **Add/Edit Annotation...** - Open annotation editor dialog
- Other existing options (Remove Level, Copy, etc.)

### Persistence
- Bookmarks and annotations are automatically saved to a sidecar file alongside your log file
- Sidecar file format: `yourlog.log.logtail.json`
- Data is preserved when closing and reopening the application
- Bookmarks/annotations are matched to log lines using hash-based identification

## Usage Examples

### Basic Workflow
1. Open a log file
2. Find an important log entry
3. Press `Ctrl+B` to bookmark it or right-click and select "Toggle Bookmark"
4. To add a note, right-click and select "Add/Edit Annotation..."
5. Navigate between bookmarks using `F2` (next) and `Shift+F2` (previous)

### Adding Annotations
1. Right-click on a log entry
2. Select "Add/Edit Annotation..."
3. Enter your annotation text in the dialog
4. Click OK to save
5. To remove an annotation, clear the text and click OK

### Viewing Annotations
- Hover your mouse over the ?? icon to see the annotation text as a tooltip
- The tooltip displays the full annotation text

## Data Storage

Bookmarks and annotations are stored in JSON format in a sidecar file:
```
yourlog.log.logtail.json
```

The sidecar file contains:
- Log file path
- Last modified timestamp
- List of bookmarks and annotations with:
  - Line hash (for matching)
  - Line text (for reference)
  - Line number (approximate)
  - Bookmark flag
  - Annotation text
  - Creation and modification timestamps

## Implementation Details

### Line Matching
- Lines are matched using SHA256 hash of the line text
- This ensures bookmarks/annotations persist even as the log file grows
- Line numbers are stored for reference but the hash is used for matching

### Performance
- Bookmark/annotation data is loaded once when opening a file
- Data is applied to log entries during refresh operations
- Minimal performance impact on large log files

## Future Enhancements
Potential future improvements could include:
- Export/import bookmarks and annotations
- Search within annotations
- Bookmark panel showing all bookmarks with preview
- Color-coded bookmarks
- Annotation categories or tags
