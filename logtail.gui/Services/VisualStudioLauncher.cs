using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

namespace logtail.gui.Services;

public class VisualStudioLauncher
{
    // Matches patterns like:
    // "in C:\Path\File.cs:line 123"
    // "C:\Path\File.cs:line 123"
    // "at Namespace.Class.Method() in C:\Path\File.cs:line 123"
    private static readonly Regex FilePathPattern = new(
        @"(?:in\s+)?([a-zA-Z]:\\[^:]+\.cs):line\s+(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    public static bool TryExtractFileInfo(string text, out string filePath, out int lineNumber)
    {
        filePath = string.Empty;
        lineNumber = 0;

        var match = FilePathPattern.Match(text);
        if (!match.Success)
            return false;

        filePath = match.Groups[1].Value;
        lineNumber = int.Parse(match.Groups[2].Value);

        return File.Exists(filePath);
    }

    public static bool OpenInVisualStudio(string filePath, int lineNumber)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;

        try
        {
            // First try to find a running Visual Studio instance
            var dte = GetRunningVisualStudioInstance();
            
            if (dte != null)
            {
                return OpenInRunningInstance(dte, filePath, lineNumber);
            }

            // If no running instance, fall back to launching VS
            return LaunchNewVisualStudioInstance(filePath, lineNumber);
        }
        catch
        {
            return false;
        }
    }

    private static dynamic? GetRunningVisualStudioInstance()
    {
        try
        {
            if (GetRunningObjectTable(0, out IRunningObjectTable rot) != 0)
                return null;

            rot.EnumRunning(out IEnumMoniker enumMoniker);
            enumMoniker.Reset();

            var moniker = new IMoniker[1];
            while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
            {
                CreateBindCtx(0, out IBindCtx bindCtx);
                moniker[0].GetDisplayName(bindCtx, null, out string displayName);

                // Look for Visual Studio DTE instances
                if (displayName.StartsWith("!VisualStudio.DTE.", StringComparison.OrdinalIgnoreCase))
                {
                    rot.GetObject(moniker[0], out object obj);
                    if (obj != null)
                    {
                        // Return the first running instance
                        Marshal.ReleaseComObject(bindCtx);
                        return obj;
                    }
                    Marshal.ReleaseComObject(bindCtx);
                }
                else
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }

            Marshal.ReleaseComObject(enumMoniker);
            Marshal.ReleaseComObject(rot);
        }
        catch
        {
            // Ignore COM errors
        }

        return null;
    }

    private static bool OpenInRunningInstance(dynamic dte, string filePath, int lineNumber)
    {
        try
        {
            // Make Visual Studio the active window
            dte.MainWindow.Activate();

            // Open the file
            var window = dte.ItemOperations.OpenFile(filePath);
            
            if (window != null)
            {
                // Wait a bit for the document to fully load
                System.Threading.Thread.Sleep(100);
                
                // Ensure the window is activated
                window.Activate();
                
                // Navigate to the specific line
                var textSelection = dte.ActiveDocument.Selection;
                textSelection.GotoLine(lineNumber, true);
                
                return true;
            }
        }
        catch
        {
            // If DTE automation fails, ignore
        }

        return false;
    }

    private static bool LaunchNewVisualStudioInstance(string filePath, int lineNumber)
    {
        try
        {
            // Try to find Visual Studio using multiple methods
            var vsPath = FindVisualStudioPath();
            
            if (string.IsNullOrEmpty(vsPath))
            {
                // Fallback: try to open with default program and let Windows handle it
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                return true;
            }

            // Launch Visual Studio with the file and navigate to the line
            // Using /Edit command to open in existing instance if available
            var arguments = $"/Edit \"{filePath}\" /Command \"Edit.Goto {lineNumber}\"";
            
            Process.Start(new ProcessStartInfo
            {
                FileName = vsPath,
                Arguments = arguments,
                UseShellExecute = false
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindVisualStudioPath()
    {
        // Try common Visual Studio paths (2022, 2025, 2026, etc.)
        var possiblePaths = new[]
        {
            @"C:\Program Files\Microsoft Visual Studio\2026\Community\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2026\Professional\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2026\Enterprise\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2025\Community\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2025\Professional\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2025\Enterprise\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe",
            @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Try to find using vswhere (Visual Studio installer tool)
        try
        {
            var vswherePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
            if (File.Exists(vswherePath))
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = vswherePath,
                    Arguments = "-latest -property productPath",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    
                    if (!string.IsNullOrEmpty(output) && File.Exists(output))
                        return output;
                }
            }
        }
        catch
        {
            // Ignore errors from vswhere
        }

        return null;
    }
}
