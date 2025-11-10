namespace FuturesContinuationsHttp.Net;

// URL parsing http://host[:port]/path parser
public sealed class Url
{
    public string Host { get; }
    public int Port { get; }
    public string Path { get; }

    public Url(string s)
    {
        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only http:// URLs are supported", nameof(s));

        var rest = s["http://".Length..];
        int slash = rest.IndexOf('/');
        var hostPort = slash >= 0 ? rest[..slash] : rest;
        Path = slash >= 0 ? rest[slash..] : "/";

        int colon = hostPort.IndexOf(':');
        if (colon >= 0)
        {
            Host = hostPort[..colon];
            Port = int.Parse(hostPort[(colon + 1)..]);
        }
        else
        {
            Host = hostPort;
            Port = 80;
        }
    }

    public override string ToString() => $"{Host}:{Port}{Path}";
}