using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FuturesContinuationsHttp.Net;

// Begin/End → Task wrappers (Connect/Send/Receive)
public static class SocketTasks
{
    public static EndPoint Resolve(string host, int port)
    {
        var entry = Dns.GetHostEntry(host);
        // prefer IPv4
        var ip = entry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                 ?? entry.AddressList[0];
        return new IPEndPoint(ip, port);
    }

    public static Task ConnectAsync(Socket s, EndPoint ep)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        s.BeginConnect(ep, ar =>
        {
            try { s.EndConnect(ar); tcs.SetResult(0); }
            catch (Exception ex) { tcs.SetException(ex); }
        }, null);
        return tcs.Task;
    }

    public static Task<int> SendAsync(Socket s, byte[] buf, int off, int count)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        s.BeginSend(buf, off, count, SocketFlags.None, ar =>
        {
            try { tcs.SetResult(s.EndSend(ar)); }
            catch (Exception ex) { tcs.SetException(ex); }
        }, null);
        return tcs.Task;
    }

    public static Task<int> ReceiveAsync(Socket s, byte[] buf, int off, int count)
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        s.BeginReceive(buf, off, count, SocketFlags.None, ar =>
        {
            try { tcs.SetResult(s.EndReceive(ar)); }
            catch (Exception ex) { tcs.SetException(ex); }
        }, null);
        return tcs.Task;
    }
}