using System.Net.Sockets;
using FuturesContinuationsHttp.Http;
using FuturesContinuationsHttp.Net;

namespace FuturesContinuationsHttp.Downloaders;

// 3) async/await (reuses the same SocketTasks/ Task wrappers)
public static class AwaitDownloader
{
    public static async Task DownloadAsync(Url url)
    {
        Console.WriteLine($"(await)  {url}");
        using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await SocketTasks.ConnectAsync(s, SocketTasks.Resolve(url.Host, url.Port));

        var req = HttpParser.BuildGetRequest(url.Host, url.Path);
        await SocketTasks.SendAsync(s, req, 0, req.Length);

        var parser = new HttpParser();
        var buf = new byte[8192];
        int total = 0;

        while (true)
        {
            int n = await SocketTasks.ReceiveAsync(s, buf, 0, buf.Length);
            if (n == 0) break;
            if (parser.Feed(buf, n) && parser.ContentLength >= 0)
            {
                total += parser.BodyBytesAlreadyBuffered;
                parser.BodyBytesAlreadyBuffered = 0;
                if (total >= parser.ContentLength) break;
            }
        }

        try { s.Shutdown(SocketShutdown.Both); } catch { }
        Console.WriteLine($"[{url}] DONE, bytes(body)={total}");
    }
}