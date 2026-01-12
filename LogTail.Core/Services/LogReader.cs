using System.Text;

namespace LogTail.Core.Services;

public sealed class LogReader
{
    public IEnumerable<string> ReadLastLines(string path, int count)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];
        var sb = new StringBuilder();
        int linesFound = 0;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        long position = fs.Length;

        while (position > 0 && linesFound <= count)
        {
            int readSize = (int)Math.Min(bufferSize, position);
            position -= readSize;
            fs.Seek(position, SeekOrigin.Begin);
            fs.Read(buffer, 0, readSize);

            for (int i = readSize - 1; i >= 0; i--)
            {
                char c = (char)buffer[i];
                sb.Insert(0, c);
                if (c == '\n')
                    linesFound++;
            }
        }

        return sb
            .ToString()
            .Split(Environment.NewLine, StringSplitOptions.None)
            .TakeLast(count * 5); // include multiline exceptions
    }
}
