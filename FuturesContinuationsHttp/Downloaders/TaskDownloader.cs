using System.Net.Sockets;
using FuturesContinuationsHttp.Http;
using FuturesContinuationsHttp.Net;

namespace FuturesContinuationsHttp.Downloaders;

// 2) Task + ContinueWith (futures & continuations)
public static class TaskDownloader
{
    public static Task DownloadAsync(Url url)
    {
        Console.WriteLine($"(tasks) {url}");
        var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var ep = SocketTasks.Resolve(url.Host, url.Port);
        var buf = new byte[8192];
        var parser = new HttpParser();
        int total = 0;

        return SocketTasks.ConnectAsync(s, ep)
            .ContinueWith(_ => {
                var req = HttpParser.BuildGetRequest(url.Host, url.Path);
                return SocketTasks.SendAsync(s, req, 0, req.Length);
            }).Unwrap()
            .ContinueWith(_ => ReceiveLoop()).Unwrap()
            .ContinueWith(_ =>
            {
                try { s.Shutdown(SocketShutdown.Both); } catch { }
                s.Close();
                Console.WriteLine($"[{url}] DONE, bytes(body)={total}");
            });

        Task ReceiveLoop() // loop as a Task chain
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            void Loop()
            {
                SocketTasks.ReceiveAsync(s, buf, 0, buf.Length).ContinueWith(tr =>
                {
                    int n = tr.Result;
                    if (n == 0) { tcs.SetResult(0); return; }
                    if (parser.Feed(buf, n) && parser.ContentLength >= 0)
                    {
                        total += parser.BodyBytesAlreadyBuffered;
                        parser.BodyBytesAlreadyBuffered = 0;
                        if (total >= parser.ContentLength) { tcs.SetResult(0); return; }
                    }
                    Loop();
                });
            }
            Loop();
            return tcs.Task;
        }
    }
}