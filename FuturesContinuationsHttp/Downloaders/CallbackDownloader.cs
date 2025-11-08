using System.Net.Sockets;
using FuturesContinuationsHttp.Http;
using FuturesContinuationsHttp.Net;

namespace FuturesContinuationsHttp.Downloaders;

// 1) Pure Begin/End callbacks
public sealed class CallbackDownloader
{
    private readonly Url _url;
    private readonly Action _onDone;
    private readonly Socket _s;
    private readonly byte[] _buf = new byte[8192];
    private readonly HttpParser _parser = new();
    private int _totalBody;

    public CallbackDownloader(Url url, Action onDone)
    {
        _url = url;
        _onDone = onDone;
        _s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Start()
    {
        var ep = SocketTasks.Resolve(_url.Host, _url.Port);
        _s.BeginConnect(ep, OnConnected, null);
    }

    private void OnConnected(IAsyncResult ar)
    {
        try { _s.EndConnect(ar); }
        catch (Exception ex) { Console.WriteLine($"[{_url}] connect error: {ex.Message}"); _onDone(); return; }

        var req = HttpParser.BuildGetRequest(_url.Host, _url.Path);
        _s.BeginSend(req, 0, req.Length, SocketFlags.None, OnSent, null);
    }

    private void OnSent(IAsyncResult ar)
    {
        try { _s.EndSend(ar); }
        catch (Exception ex) { Console.WriteLine($"[{_url}] send error: {ex.Message}"); _onDone(); return; }

        _s.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, OnReceived, null);
    }

    private void OnReceived(IAsyncResult ar)
    {
        int n;
        try { n = _s.EndReceive(ar); }
        catch (Exception ex) { Console.WriteLine($"[{_url}] recv error: {ex.Message}"); _onDone(); return; }

        if (n == 0) { Finish(); return; }

        if (_parser.Feed(_buf, n))
        {
            if (_parser.ContentLength >= 0)
            {
                _totalBody += _parser.BodyBytesAlreadyBuffered;
                _parser.BodyBytesAlreadyBuffered = 0;
                if (_totalBody >= _parser.ContentLength) { Finish(); return; }
            }
        }

        _s.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, OnReceived, null);
    }

    private void Finish()
    {
        try { _s.Shutdown(SocketShutdown.Both); } catch { }
        _s.Close();
        Console.WriteLine($"[{_url}] DONE, bytes(body)={_totalBody}");
        _onDone();
    }
}