# Welcome to LogTail GUI Documentation

## What is LogTail GUI?

LogTail GUI is a modern, feature-rich log file viewer for Windows designed to make log analysis simple and efficient. Built with .NET 10 and WPF, it provides a clean, Visual Studio-inspired interface for monitoring and analyzing log files in real-time.

## Key Features

### Real-Time Monitoring
LogTail GUI automatically monitors your log files and refreshes to display the latest entries, allowing you to track application behavior as it happens. The configurable refresh rate (1-60 seconds) and tail lines (10-10,000 lines) give you full control over what you see.

### Advanced Filtering
Quickly find what matters with powerful filtering options:
- Filter by log level (Verbose, Debug, Info, Warning, Error, Fatal)
- Search by source or custom text
- Color-coded log levels for instant visual recognition

### Visual Studio Integration
One of LogTail GUI's standout features is seamless Visual Studio integration. Simply double-click any log entry containing a file path and stack trace, and LogTail will:
- Connect to your running Visual Studio instance via DTE COM automation
- Open the file and navigate to the exact line number
- Launch Visual Studio if no instance is running

### Smart Log Parsing
LogTail automatically parses structured logs to extract:
- Timestamps
- Log levels
- Sources
- Messages and stack traces

### Productivity Features
- **Recent Files**: Quick access to your frequently viewed logs
- **Smart Clipboard**: Copy entire log entries including multi-line stack traces with a double-click
- **Clean Interface**: Dark theme with automatic column sizing for comfortable viewing

## Who Should Use LogTail GUI?

LogTail GUI is perfect for:
- Developers debugging applications and analyzing logs
- DevOps engineers monitoring application behavior
- Anyone working with log files who needs a modern, efficient viewer

## Getting Started

Explore the documentation to learn how to:
- [[Opening and Monitoring Log Files|Open and monitor log files]]
- [[Filtering Logs|Configure filters and settings]]
- [[Settings and Preferences|Customize settings and preferences]]
- [[Visual Studio Integration|Use Visual Studio integration]]
- Navigate keyboard shortcuts
- And much more!

---

*LogTail GUI - Making log analysis effortless.*