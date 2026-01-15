using System.Text;

namespace LogTail.Core.Services;

public sealed class LogReader
{
    public IEnumerable<string> ReadLastLines(string path, int count)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];
        var sb = new StringBuilder();
        var skippedPartial = new StringBuilder(); // Track skipped partial line
        int linesFound = 0;
        bool startedMidFile = false;
        bool foundFirstNewline = false;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        long position = fs.Length;
        long originalLength = fs.Length;

        while (position > 0 && linesFound <= count)
        {
            int readSize = (int)Math.Min(bufferSize, position);
            position -= readSize;
            fs.Seek(position, SeekOrigin.Begin);
            fs.Read(buffer, 0, readSize);

            // Check if this is the first chunk and we're not reading from the file start
            if (!foundFirstNewline && position > 0)
            {
                startedMidFile = true;
            }

            for (int i = readSize - 1; i >= 0; i--)
            {
                char c = (char)buffer[i];
                
                // Skip characters until we find the first newline when starting mid-file
                // This ensures we don't include a partial/clipped first line
                if (startedMidFile && !foundFirstNewline)
                {
                    if (c == '\n')
                    {
                        foundFirstNewline = true;
                    }
                    else
                    {
                        skippedPartial.Insert(0, c);
                    }
                    // Skip this character - it's part of a potentially clipped line
                    continue;
                }
                
                sb.Insert(0, c);
                if (c == '\n')
                    linesFound++;
            }
        }

        // If we read the entire file (position is 0 now and we started from beginning),
        // include any partial content that might have been skipped
        if (position == 0 && !startedMidFile && skippedPartial.Length > 0)
        {
            sb.Insert(0, skippedPartial.ToString());
        }

        return sb
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.None)
            .Where(line => !string.IsNullOrEmpty(line)) // Remove empty lines from split
            .TakeLast(count * 5); // include multiline exceptions
    }
}
