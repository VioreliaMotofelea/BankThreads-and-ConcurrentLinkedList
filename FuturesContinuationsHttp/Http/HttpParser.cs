using System.Text;

namespace FuturesContinuationsHttp.Http;

// Minimal HTTP/1.1 header parser: collects header lines,
// finds Content-Length, and counts any body bytes already buffered
public sealed class HttpParser
{
    private readonly StringBuilder _headers = new();
    private bool _finished;

    public int ContentLength { get; private set; } = -1;
    public int BodyBytesAlreadyBuffered { get; set; } = 0;

    public bool Feed(byte[] buf, int count)
    {
        if (_finished)
        {
            BodyBytesAlreadyBuffered += count;
            return true;
        }

        var chunk = Encoding.ASCII.GetString(buf, 0, count);
        _headers.Append(chunk);

        var s = _headers.ToString();
        int idx = s.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (idx < 0) return false;

        ParseHeaders(s[..(idx + 2)]); // include trailing \r\n
        // count leftover body bytes after the header separator
        int leftover = s.Length - (idx + 4);
        if (leftover > 0) BodyBytesAlreadyBuffered += leftover;
        _finished = true;
        return true;
    }

    private void ParseHeaders(string headerBlock)
    {
        var lines = headerBlock.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Console.WriteLine("  " + lines[0]);
        foreach (var line in lines)
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var len))
                    ContentLength = len;
            }
        }
        if (ContentLength < 0)
            Console.WriteLine("  (No Content-Length; demo assumes it exists)");
    }

    public static byte[] BuildGetRequest(string host, string path)
        => Encoding.ASCII.GetBytes($"GET {path} HTTP/1.1\r\nHost: {host}\r\nConnection: close\r\n\r\n");
}