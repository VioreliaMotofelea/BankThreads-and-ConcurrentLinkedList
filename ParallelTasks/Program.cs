/*
dotnet run --project ParallelTasks -- -m 9 -n 9 -p 9 -t 4 -s rows -verbose
dotnet run --project ParallelTasks -- -m 9 -n 9 -p 9 -t 4 -s cols -verbose
dotnet run --project ParallelTasks -- -m 9 -n 9 -p 9 -t 4 -s kth -verbose

dotnet run --project ParallelTasks -- -m 1000 -n 1000 -p 1000 -t 1  -s rows
dotnet run --project ParallelTasks -- -m 1000 -n 1000 -p 1000 -t 4  -s rows

dotnet run --project ParallelTasks -- -m 1000 -n 1000 -p 1000 -t 8  -s rows
dotnet run --project ParallelTasks -- -m 1000 -n 1000 -p 1000 -t 8  -s cols
dotnet run --project ParallelTasks -- -m 1000 -n 1000 -p 1000 -t 8  -s kth

*/


using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace MatrixThreads;

enum Strategy { Rows, Cols, Kth }

static class Program
{
    // ---------- compute one element (required) ----------
    static int ComputeCell(int[,] A, int[,] B, int i, int j, int tId, bool verbose)
    {
        int n = A.GetLength(1);          // shared inner dimension
        int sum = 0;
        for (int k = 0; k < n; k++)
            sum += A[i, k] * B[k, j];

        if (verbose)
            Console.WriteLine($"cell({i},{j}) by thread {tId}");

        return sum;
    }

    // Thread payload
    sealed class Work
    {
        public required int Tid;
        public required List<(int i, int j)> Cells;
        public required int[,] A;
        public required int[,] B;
        public required int[,] C;
        public required bool Verbose;
    }

    static void Worker(object? obj)
    {
        var w = (Work)obj!;
        foreach (var (i, j) in w.Cells)
            w.C[i, j] = ComputeCell(w.A, w.B, i, j, w.Tid, w.Verbose);
    }

    // ---------- Partitions ----------
    static List<(int i, int j)>[] PartitionRows(int m, int p, int threads)
    {
        // Flatten row-major: (0,0),(0,1)…,(0,p-1),(1,0)…,(m-1,p-1)
        long total = (long)m * p;
        long baseChunk = total / threads;
        long extra = total % threads; // first 'extra' threads get 1 more

        var result = new List<(int, int)>[threads];
        long start = 0;
        for (int t = 0; t < threads; t++)
        {
            long count = baseChunk + (t < extra ? 1 : 0);
            var list = new List<(int, int)>((int)count);
            for (long k = start; k < start + count; k++)
            {
                int i = (int)(k / p);
                int j = (int)(k % p);
                list.Add((i, j));
            }
            result[t] = list;
            start += count;
        }
        return result;
    }

    static List<(int i, int j)>[] PartitionCols(int m, int p, int threads)
    {
        // Flatten column-major: (0,0),(1,0)…,(m-1,0),(0,1)…,(m-1,p-1)
        long total = (long)m * p;
        long baseChunk = total / threads;
        long extra = total % threads;

        var result = new List<(int, int)>[threads];
        long start = 0;
        for (int t = 0; t < threads; t++)
        {
            long count = baseChunk + (t < extra ? 1 : 0);
            var list = new List<(int, int)>((int)count);
            for (long k = start; k < start + count; k++)
            {
                int j = (int)(k / m);
                int i = (int)(k % m);
                list.Add((i, j));
            }
            result[t] = list;
            start += count;
        }
        return result;
    }

    static List<(int i, int j)>[] PartitionKth(int m, int p, int threads)
    {
        // Round-robin over row-major order: each t gets k-th elements
        var result = new List<(int, int)>[threads];
        for (int t = 0; t < threads; t++)
            result[t] = new List<(int, int)>();

        long total = (long)m * p;
        for (long k = 0; k < total; k++)
        {
            int t = (int)(k % threads);
            int i = (int)(k / p);
            int j = (int)(k % p);
            result[t].Add((i, j));
        }
        return result;
    }

    // ---------- Reference single-thread multiply (for correctness) ----------
    static int[,] MultiplyRef(int[,] A, int[,] B)
    {
        int m = A.GetLength(0);
        int n = A.GetLength(1);
        int p = B.GetLength(1);
        var C = new int[m, p];
        for (int i = 0; i < m; i++)
            for (int j = 0; j < p; j++)
            {
                int s = 0;
                for (int k = 0; k < n; k++) s += A[i, k] * B[k, j];
                C[i, j] = s;
            }
        return C;
    }

    static bool Equal(int[,] X, int[,] Y)
    {
        int m = X.GetLength(0), p = X.GetLength(1);
        if (m != Y.GetLength(0) || p != Y.GetLength(1)) return false;
        for (int i = 0; i < m; i++)
            for (int j = 0; j < p; j++)
                if (X[i, j] != Y[i, j]) return false;
        return true;
    }

    // ---------- Main ----------
    static void Main(string[] args)
    {
        int m = Get("-m", 900), n = Get("-n", 900), p = Get("-p", 900);
        int threads = Get("-t", Environment.ProcessorCount);
        string stratStr = GetStr("-s", "rows");
        bool verbose = Has("-verbose");

        Strategy strat = stratStr.ToLower() switch
        {
            "rows" => Strategy.Rows,
            "cols" => Strategy.Cols,
            "kth"  => Strategy.Kth,
            _ => throw new ArgumentException("Use -s rows|cols|kth")
        };

        Console.WriteLine();
        Console.WriteLine($"A: {m}x{n}, B: {n}x{p}");
        Console.WriteLine($"Threads: {threads}, Strategy: {strat}{(verbose ? " (verbose)" : "")}");

        // Deterministic small integers to avoid overflow and ease checking
        var rnd = new Random(123);
        var A = new int[m, n];
        var B = new int[n, p];
        for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                A[i, j] = rnd.Next(1, 8);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < p; j++)
                B[i, j] = rnd.Next(1, 8);

        var C = new int[m, p];

        // Partition work
        List<(int i, int j)>[] parts = strat switch
        {
            Strategy.Rows => PartitionRows(m, p, threads),
            Strategy.Cols => PartitionCols(m, p, threads),
            Strategy.Kth  => PartitionKth(m, p, threads),
            _ => throw new ArgumentOutOfRangeException()
        };

        // Launch threads
        var sw = Stopwatch.StartNew();
        var list = new List<Thread>(threads);
        for (int t = 0; t < threads; t++)
        {
            var w = new Work { Tid = t, Cells = parts[t], A = A, B = B, C = C, Verbose = verbose };
            var th = new Thread(Worker) { IsBackground = true, Name = $"T{t}" };
            th.Start(w);
            list.Add(th);
        }
        foreach (var th in list) th.Join();
        sw.Stop();

        // Verify vs reference
        var swRef = Stopwatch.StartNew();
        var Cref = MultiplyRef(A, B);
        swRef.Stop();

        Console.WriteLine();
        Console.WriteLine($"Parallel time : {sw.Elapsed}");
        Console.WriteLine($"Reference time: {swRef.Elapsed}");
        Console.WriteLine(Equal(C, Cref) ? "Result: OK" : "Result: MISMATCH");
        Console.WriteLine();

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
