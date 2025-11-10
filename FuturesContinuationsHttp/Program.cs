using System.Threading;
using FuturesContinuationsHttp.Downloaders;
using FuturesContinuationsHttp.Net;

namespace FuturesContinuationsHttp;

enum Mode { Callbacks, Tasks, Await }

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || Array.IndexOf(args, "-mode") < 0)
        {
            Console.WriteLine("Usage: dotnet run -- -mode callbacks|tasks|await <http://url1> <http://url2> ...");
            return 1;
        }

        Mode mode = ParseMode(args);
        var urls = args.Where(a => a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            .Select(u => new Url(u))
            .ToList();

        if (urls.Count == 0) { Console.WriteLine("No http:// URLs provided."); return 1; }

        Console.WriteLine($"Mode: {mode}, URLs: {urls.Count}");

        switch (mode)
        {
            case Mode.Callbacks: // RunCallbacks
                var cde = new CountdownEvent(urls.Count);
                foreach (var u in urls)
                {
                    var d = new CallbackDownloader(u, () => cde.Signal());
                    d.Start();
                }
                cde.Wait();
                break;

            case Mode.Tasks:
                await Task.WhenAll(urls.Select(TaskDownloader.DownloadAsync));
                break;

            case Mode.Await:
                await Task.WhenAll(urls.Select(AwaitDownloader.DownloadAsync));
                break;
        }

        Console.WriteLine("All downloads complete.");
        Console.WriteLine();
        return 0;
    }

    static Mode ParseMode(string[] a)
    {
        int i = Array.IndexOf(a, "-mode");
        var m = (i >= 0 && i + 1 < a.Length) ? a[i + 1].ToLowerInvariant() : "callbacks";
        return m switch
        {
            "callbacks" => Mode.Callbacks,
            "tasks"     => Mode.Tasks,
            "await"     => Mode.Await,
            _ => Mode.Callbacks
        };
    }
}


/*
   dotnet run --project FuturesContinuationsHttp -- -mode callbacks http://info.cern.ch/ http://www.cnatdcu.ro/ http://example.com/
   dotnet run --project FuturesContinuationsHttp -- -mode tasks http://info.cern.ch/ http://www.cnatdcu.ro/ http://example.com/
   dotnet run --project FuturesContinuationsHttp -- -mode await http://info.cern.ch/ http://www.cnatdcu.ro/ http://example.com/
   
 */