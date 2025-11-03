/*
dotnet run --project MatrixThreads -- -m 9 -n 9 -p 9 -t 4 -s rows -verbose
dotnet run --project MatrixThreads -- -m 9 -n 9 -p 9 -t 4 -s cols -verbose
dotnet run --project MatrixThreads -- -m 9 -n 9 -p 9 -t 4 -s kth  -verbose

dotnet run --project MatrixThreads -- -m 300 -n 300 -p 300 -t 1  -s rows
dotnet run --project MatrixThreads -- -m 600 -n 600 -p 600 -t 4  -s rows

dotnet run --project MatrixThreads -- -m 1000 -n 1000 -p 1000 -t 8 -s rows
dotnet run --project MatrixThreads -- -m 1000 -n 1000 -p 1000 -t 8 -s cols
dotnet run --project MatrixThreads -- -m 1000 -n 1000 -p 1000 -t 8 -s kth

*/


using MatrixThreads.Domain;
using MatrixThreads.Runtime;

class Program
{
    static void Main(string[] args)
    {
        int m = Get("-m", 9), n = Get("-n", 9), p = Get("-p", 9);
        int threads = Get("-t", 4);
        string stratStr = GetStr("-s", "rows");
        bool verbose = Has("-verbose");

        PartitionStrategy strategy = stratStr.ToLower() switch
        {
            "rows" => PartitionStrategy.Rows,
            "cols" => PartitionStrategy.Cols,
            "kth" => PartitionStrategy.Kth,
            _ => throw new ArgumentException("Use -s rows|cols|kth")
        };

        ExperimentRunner.Run(m, n, p, threads, strategy, verbose);

        int Get(string key, int def)
        {
            int i = Array.IndexOf(args, key);
            if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v)) return v;
            return def;
        }

        string GetStr(string key, string def)
        {
            int i = Array.IndexOf(args, key);
            if (i >= 0 && i + 1 < args.Length) return args[i + 1];
            return def;
        }

        bool Has(string key) => Array.IndexOf(args, key) >= 0;
    }
}