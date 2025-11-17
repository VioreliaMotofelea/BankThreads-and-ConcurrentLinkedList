using System;
using System.Threading.Tasks;
using System.Threading;

namespace PolynomialMultiplication.Algorithms;

public static class Naive
{
    public static long[] MultiplySeq(long[] a, long[] b)
    {
        var c = new long[a.Length + b.Length - 1];
        for (int i = 0; i < a.Length; i++)
        for (int j = 0; j < b.Length; j++)
            c[i + j] += a[i] * b[j];
        return c;
    }

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
