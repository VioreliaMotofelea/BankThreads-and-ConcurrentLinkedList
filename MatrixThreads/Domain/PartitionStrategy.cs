using System;
using System.Collections.Generic;

namespace MatrixThreads.Domain;

public enum PartitionStrategy { Rows, Cols, Kth }

public static class PartitionStrategyHelper
{
    public static List<(int i, int j)>[] Partition(PartitionStrategy strategy, int m, int p, int threads)
        => strategy switch
        {
            PartitionStrategy.Rows => PartitionRows(m, p, threads),
            PartitionStrategy.Cols => PartitionCols(m, p, threads),
            PartitionStrategy.Kth => PartitionKth(m, p, threads),
            _ => throw new ArgumentOutOfRangeException()
        };

    private static List<(int, int)>[] PartitionRows(int m, int p, int threads)
    {
        long total = (long)m * p;
        long baseChunk = total / threads;
        long extra = total % threads;

        var parts = new List<(int, int)>[threads];
        long start = 0;

        for (int t = 0; t < threads; t++)
        {
            long count = baseChunk + (t < extra ? 1 : 0);
            var list = new List<(int, int)>();
            for (long k = start; k < start + count; k++)
                list.Add(((int)(k / p), (int)(k % p)));

            parts[t] = list;
            start += count;
        }

        return parts;
    }

    private static List<(int, int)>[] PartitionCols(int m, int p, int threads)
    {
        long total = (long)m * p;
        long baseChunk = total / threads;
        long extra = total % threads;

        var parts = new List<(int, int)>[threads];
        long start = 0;

        for (int t = 0; t < threads; t++)
        {
            long count = baseChunk + (t < extra ? 1 : 0);
            var list = new List<(int, int)>();
            for (long k = start; k < start + count; k++)
                list.Add(((int)(k % m), (int)(k / m)));

            parts[t] = list;
            start += count;
        }

        return parts;
    }

    private static List<(int, int)>[] PartitionKth(int m, int p, int threads)
    {
        var parts = new List<(int, int)>[threads];
        for (int t = 0; t < threads; t++) parts[t] = new();

        for (long k = 0; k < (long)m * p; k++)
        {
            int t = (int)(k % threads);
            parts[t].Add(((int)(k / p), (int)(k % p)));
        }

        return parts;
    }
}
