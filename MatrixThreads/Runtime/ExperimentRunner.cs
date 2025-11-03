using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using MatrixThreads.Domain;
using MatrixThreads.Concurrency;

namespace MatrixThreads.Runtime;

public static class ExperimentRunner
{
    public static void Run(int m, int n, int p, int threads, PartitionStrategy strategy, bool verbose)
    {
        Console.WriteLine();
        Console.WriteLine($"A: {m}x{n}, B: {n}x{p}");;
        Console.WriteLine($"Threads: {threads}, Strategy: {strategy}\n");

        var A = Matrix.Random(m, n);
        var B = Matrix.Random(n, p);
        var C = new int[m, p];

        var partitions = PartitionStrategyHelper.Partition(strategy, m, p, threads);

        var sw = Stopwatch.StartNew();

        var threadList = new List<Thread>(threads);
        for (int t = 0; t < threads; t++)
        {
            var worker = new Worker(t, A, B, C, partitions[t], verbose);
            var th = new Thread(worker.Run);
            th.Start();
            threadList.Add(th);
        }

        foreach (var th in threadList)
            th.Join();

        sw.Stop();

        Console.WriteLine();
        Console.WriteLine($"Parallel time: {sw.Elapsed}");

        var refTime = Stopwatch.StartNew();
        var Cref = Matrix.MultiplyReference(A, B);
        refTime.Stop();

        Console.WriteLine($"Reference time: {refTime.Elapsed}");
        Console.WriteLine(Matrix.AreEqual(C, Cref) ? "Result OK" : "Result MISMATCH");
        Console.WriteLine();
    }
}