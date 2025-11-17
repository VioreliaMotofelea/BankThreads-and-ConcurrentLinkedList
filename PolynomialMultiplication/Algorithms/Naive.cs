using System;
using System.Threading.Tasks;
using System.Threading;

namespace PolynomialMultiplication.Algorithms;

// 1. Naïve O(n²) – sequential
// 2. Naïve O(n²) – parallel (partitioned loop, safe accumulation)

public static class Naive
{
    // O(n^2) sequential
    public static long[] MultiplySeq(long[] a, long[] b)
    {
        var c = new long[a.Length + b.Length - 1];
        for (int i = 0; i < a.Length; i++)
        for (int j = 0; j < b.Length; j++)
            c[i + j] += a[i] * b[j];
        return c;
    }

    // O(n^2) parallel – partition 'i' across threads; different i,j hit same c[k] so we must synchronize.
    // We use Interlocked.Add(ref long, value) which is lock-free and safe for concurrent accumulation.
    public static long[] MultiplyPar(long[] a, long[] b, int maxDegreeOfParallelism)
    {
        var c = new long[a.Length + b.Length - 1];

        Parallel.For(0, a.Length, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, i =>
        {
            long ai = a[i];
            for (int j = 0; j < b.Length; j++)
            {
                long v = ai * b[j];
                Interlocked.Add(ref c[i + j], v);
            }
        });

        return c;
    }
}

/*
 * In the parallel naïve algorithm, multiple i iterations contribute to the same result index c[i+j].
 * We therefore protect each accumulation with Interlocked.Add(ref long, …).
 * This avoids a coarse lock and lets unrelated indices proceed independently.
 */