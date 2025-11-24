using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ParallelHamiltonianSearch;

class Program
{
    static void Main(string[] args)
    {
        int n = Get(args, "-n", 10);
        double p = GetDouble(args, "-p", 0.4);
        int threads = Get(args, "-threads", Environment.ProcessorCount);
        int seed = Get(args, "-seed", 1);

        Console.WriteLine($"Graph: n={n}, p={p}, threads={threads}, seed={seed}");

        var g = Graph.Random(n, p, seed);
        var solver = new HamiltonianSolver(g, start: 0);

        // sequential
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var seq = solver.FindSequential();
        sw.Stop();
        Console.WriteLine($"Sequential: {(seq != null ? "FOUND" : "none")} in {sw.Elapsed}");

        // parallel
        sw.Restart();
        var par = solver.FindParallel(threads);
        sw.Stop();
        Console.WriteLine($"Parallel  : {(par != null ? "FOUND" : "none")} in {sw.Elapsed}");

        if (par != null)
        {
            Console.WriteLine("Cycle (parallel): " + string.Join(" -> ", par));
        }
    }

    static int Get(string[] args, string key, int def)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v)) return v;
        return def;
    }

    static double GetDouble(string[] args, string key, double def)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length && double.TryParse(args[i + 1], out var v)) return v;
        return def;
    }
}

/*
dotnet run --project ParallelHamiltonianSearch -- -n 10 -p 0.8 -threads 4
dotnet run --project ParallelHamiltonianSearch -- -n 12 -p 0.2 -threads 8
dotnet run --project ParallelHamiltonianSearch -- -n 13 -p 0.5 -threads 8

dotnet run --project ParallelHamiltonianSearch -- -n 11 -p 0.5 -threads 1
dotnet run --project ParallelHamiltonianSearch -- -n 11 -p 0.5 -threads 2
dotnet run --project ParallelHamiltonianSearch -- -n 11 -p 0.5 -threads 4
dotnet run --project ParallelHamiltonianSearch -- -n 11 -p 0.5 -threads 8

dotnet run --project ParallelHamiltonianSearch -- -n 10 -p 0.5 -threads 8 -seed 99
dotnet run --project ParallelHamiltonianSearch -- -n 12 -p 0.4 -threads 8 -seed 1

*/
