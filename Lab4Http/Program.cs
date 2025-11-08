using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab4Http
{
    enum Mode { Callbacks, Tasks, Await }

    static class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 2 || Array.IndexOf(args, "-mode") < 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run -- -mode callbacks|tasks|await <url1> <url2> ...");
                return 1;
            }

            Mode mode = ParseMode(args);
            var urls = new List<string>();
            for (int i = 0; i < args.Length; i++)
                if (args[i].StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    urls.Add(args[i]);

            if (urls.Count == 0)
            {
                Console.WriteLine("Please pass one or more http:// URLs.");
                return 1;
            }

            Console.WriteLine($"Mode: {mode}, URLs: {urls.Count}");

            try
            {
                switch (mode)
                {
                    case Mode.Callbacks:
                        RunCallbacks(urls);
                        break;
                    case Mode.Tasks:
                        RunTasks(urls).GetAwaiter().GetResult(); // Wait only in Main
                        break;
                    case Mode.Await:
                        RunAwait(urls).GetAwaiter().GetResult(); // Wait only in Main
                        break;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Top-level exception: " + ex);
                return 2;
            }
        }

        static Mode ParseMode(string[] args)
        {
            int i = Array.IndexOf(args, "-mode");
            string m = (i >= 0 && i + 1 < args.Length) ? args[i + 1] : "callbacks";
            return m.ToLower() switch
            {
                "callbacks" => Mode.Callbacks,
                "tasks"     => Mode.Tasks,
                "await"     => Mode.Await,
                _ => Mode.Callbacks
            };
        }

        // URL parsing
        sealed class Url
        {
            public readonly string Host;
            public readonly int Port;
            public readonly string Path;

            public Url(string s)
            {
                // very small parser for http://host[:port]/path
                if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Only http:// URLs are supported");

                var rest = s.Substring("http://".Length);
                int slash = rest.IndexOf('/');
                string hostPort = slash >= 0 ? rest.Substring(0, slash) : rest;
                Path = slash >= 0 ? rest.Substring(slash) : "/";

                int colon = hostPort.IndexOf(':');
                if (colon >= 0)
                {
                    Host = hostPort.Substring(0, colon);
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

        // HTTP mini-parser
        // Reads until \r\n\r\n, then parses headers and returns Content-Length (or -1 if missing).
        // Any extra bytes from the first receive that belong to the body are accounted for.
        sealed class HttpParser
        {
            private readonly StringBuilder _headers = new();
            private bool _finishedHeaders;

            public int ContentLength { get; private set; } = -1;
            public int BodyBytesAlreadyBuffered { get; private set; } = 0;

            public int TakeBufferedBodyBytes()
            {
                int v = BodyBytesAlreadyBuffered;
                BodyBytesAlreadyBuffered = 0;
                return v;
            }


            public bool Feed(byte[] buf, int count)
            {
                if (_finishedHeaders)
                {
                    BodyBytesAlreadyBuffered += count;
                    return true;
                }

                string chunk = Encoding.ASCII.GetString(buf, 0, count);
                _headers.Append(chunk);

                string s = _headers.ToString();
                int idx = s.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                if (idx >= 0)
                {
                    string headerBlock = s.Substring(0, idx + 2); // include trailing \r\n
                    ParseHeaders(headerBlock);

                    // count leftover body bytes after the header separator
                    int bodyStartInThisBuffer = s.Length - (idx + 4);
                    if (bodyStartInThisBuffer > 0)
                        BodyBytesAlreadyBuffered += bodyStartInThisBuffer;

                    _finishedHeaders = true;
                }

                return _finishedHeaders;
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
                    Console.WriteLine("  (No Content-Length header; this demo assumes it exists)");
            }
        }

        // Begin/End wrappers for Tasks
        static Task<int> ConnectAsync(Socket s, EndPoint ep)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            s.BeginConnect(ep, ar =>
            {
                try { s.EndConnect(ar); tcs.SetResult(0); }
                catch (Exception ex) { tcs.SetException(ex); }
            }, null);
            return tcs.Task;
        }

        static Task<int> SendAsync(Socket s, byte[] buf, int off, int count)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            s.BeginSend(buf, off, count, SocketFlags.None, ar =>
            {
                try { tcs.SetResult(s.EndSend(ar)); }
                catch (Exception ex) { tcs.SetException(ex); }
            }, null);
            return tcs.Task;
        }

        static Task<int> ReceiveAsync(Socket s, byte[] buf, int off, int count)
        {
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            s.BeginReceive(buf, off, count, SocketFlags.None, ar =>
            {
                try { tcs.SetResult(s.EndReceive(ar)); }
                catch (Exception ex) { tcs.SetException(ex); }
            }, null);
            return tcs.Task;
        }

        static EndPoint Resolve(string host, int port)
        {
            var entry = Dns.GetHostEntry(host);
            // pick an IPv4 if present
            IPAddress? ip = null;
            foreach (var a in entry.AddressList)
                if (a.AddressFamily == AddressFamily.InterNetwork) { ip = a; break; }
            ip ??= entry.AddressList[0];
            return new IPEndPoint(ip, port);
        }

        static byte[] BuildGetRequest(Url u)
            => Encoding.ASCII.GetBytes($"GET {u.Path} HTTP/1.1\r\nHost: {u.Host}\r\nConnection: close\r\n\r\n");

        // 1) CALLBACKS version (Begin*/End* with state machine)
        sealed class CallbackSession
        {
            private readonly Url _url;
            private readonly EndPoint _ep;
            private readonly Socket _s;
            private readonly byte[] _buf = new byte[8192];
            private readonly HttpParser _parser = new();
            private int _totalBody;
            private readonly Action<CallbackSession> _onDone;

            public CallbackSession(Url url, Action<CallbackSession> onDone)
            {
                _url = url;
                _ep = Resolve(url.Host, url.Port);
                _s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _onDone = onDone;
            }

            public void Start()
            {
                _s.BeginConnect(_ep, OnConnected, null);
            }

            private void OnConnected(IAsyncResult ar)
            {
                try { _s.EndConnect(ar); }
                catch (Exception ex) { Console.WriteLine($"[{_url}] connect error: {ex.Message}"); _onDone(this); return; }

                var req = BuildGetRequest(_url);
                _s.BeginSend(req, 0, req.Length, SocketFlags.None, OnSent, null);
            }

            private void OnSent(IAsyncResult ar)
            {
                try { _s.EndSend(ar); }
                catch (Exception ex) { Console.WriteLine($"[{_url}] send error: {ex.Message}"); _onDone(this); return; }
                _s.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, OnReceived, null);
            }

            private void OnReceived(IAsyncResult ar)
            {
                int n;
                try { n = _s.EndReceive(ar); }
                catch (Exception ex) { Console.WriteLine($"[{_url}] recv error: {ex.Message}"); _onDone(this); return; }

                if (n == 0) { Finish(); return; }

                // Feed buffer to the HTTP parser.
                // Feed() returns true once headers are fully parsed.
                bool headersDone = _parser.Feed(_buf, n);

                if (headersDone && _parser.ContentLength >= 0)
                {
                    // Take (and clear) any body bytes that were already buffered.
                    _totalBody += _parser.TakeBufferedBodyBytes();

                    // If we already reached the Content-Length, we're done.
                    if (_totalBody >= _parser.ContentLength)
                    {
                        Finish();
                        return;
                    }
                }

                // Continue reading
                _s.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, OnReceived, null);
            }

            private void Finish()
            {
                try { _s.Shutdown(SocketShutdown.Both); } catch { }
                try { _s.Close(); } catch { }
                Console.WriteLine($"[{_url}] DONE, bytes(body)={_totalBody}");
                _onDone(this);
            }
        }

        static void RunCallbacks(List<string> urls)
        {
            var cde = new CountdownEvent(urls.Count);
            foreach (var s in urls)
            {
                var u = new Url(s);
                var sess = new CallbackSession(u, _ => cde.Signal());
                Console.WriteLine($"Starting (callbacks): {u}");
                sess.Start();
            }

            // Only wait in Main as requested
            cde.Wait();
            Console.WriteLine("All downloads finished (callbacks).");
        }

        // 2) TASKS + ContinueWith (futures & continuations)
        static Task RunTasks(List<string> urls)
        {
            var tasks = new List<Task>();
            foreach (var s in urls)
                tasks.Add(DownloadWithTasks(new Url(s)));
            return Task.WhenAll(tasks);
        }

        static Task DownloadWithTasks(Url u)
        {
            Console.WriteLine($"Starting (tasks): {u}");
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = Resolve(u.Host, u.Port);
            var parser = new HttpParser();
            var buf = new byte[8192];
            int totalBody = 0;

            return ConnectAsync(s, ep)
            .ContinueWith(_ => SendAsync(s, BuildGetRequest(u), 0, BuildGetRequest(u).Length)).Unwrap()
            .ContinueWith(_ => ReceiveLoop())
            .Unwrap()
            .ContinueWith(_ =>
            {
                try { s.Shutdown(SocketShutdown.Both); } catch { }
                s.Close();
                Console.WriteLine($"[{u}] DONE, bytes(body)={totalBody}");
            });

            Task ReceiveLoop()
            {
                // loop as a Task chain
                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                void Loop()
                {
                    ReceiveAsync(s, buf, 0, buf.Length).ContinueWith((Task<int> tr) =>
                    {
                        int n = tr.Result;
                        if (n == 0)
                        {
                            tcs.SetResult(0);
                            return;
                        }
                        if (!parser.Feed(buf, n))
                        {
                            // still parsing headers
                        }
                        else
                        {
                            if (parser.ContentLength >= 0)
                            {
                                totalBody += parser.TakeBufferedBodyBytes();
                                if (totalBody >= parser.ContentLength) { tcs.SetResult(0); return; }
                            }
                        }
                        if (parser.ContentLength >= 0 && totalBody >= parser.ContentLength)
                        {
                            tcs.SetResult(0);
                            return;
                        }
                        
                        Loop(); // continue the chain
                    });
                }
                Loop();
                return tcs.Task;
            }
        }

        // 3) ASYNC/AWAIT (wrappers reused)
        static async Task RunAwait(List<string> urls)
        {
            var all = new List<Task>();
            foreach (var s in urls)
                all.Add(DownloadAwait(new Url(s)));
            await Task.WhenAll(all);
            Console.WriteLine("All downloads finished (await).");
        }

        static async Task DownloadAwait(Url u)
        {
            Console.WriteLine($"Starting (await): {u}");
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ep = Resolve(u.Host, u.Port);
            await ConnectAsync(s, ep);

            var req = BuildGetRequest(u);
            await SendAsync(s, req, 0, req.Length);

            var parser = new HttpParser();
            var buf = new byte[8192];
            int totalBody = 0;

            while (true)
            {
                int n = await ReceiveAsync(s, buf, 0, buf.Length);
                if (n == 0) break;
                if (!parser.Feed(buf, n))
                {
                    // still headers
                }
                else
                {
                    if (parser.ContentLength >= 0)
                    {
                        totalBody += parser.TakeBufferedBodyBytes();
                        if (totalBody >= parser.ContentLength) break;
                    }
                    if (parser.ContentLength >= 0 && totalBody >= parser.ContentLength)
                        break;
                }
            }

            try { s.Shutdown(SocketShutdown.Both); } catch { }
            Console.WriteLine($"[{u}] DONE, bytes(body)={totalBody}");
        }
    }
}
/*
   dotnet run --project Lab4Http -- -mode callbacks http://example.com/
   dotnet run --project Lab4Http -- -mode tasks     http://www.cnatdcu.ro/documente-utile/
   dotnet run --project Lab4Http -- -mode await     http://example.com/ http://www.cnatdcu.ro/
   
 */