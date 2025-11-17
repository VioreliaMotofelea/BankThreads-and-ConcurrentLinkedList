using System;

namespace PolynomialMultiplication.Algorithms;

// 1. Karatsuba – sequential
// 2. Karatsuba – parallel (parallelize only the top recursion levels, sized to your CPU)

public static class Karatsuba
{
    // Public API – choose sequential or parallel by maxDepth (0 = sequential)
    public static long[] MultiplySeq(long[] a, long[] b, int baseCutoff = 64)
        => TrimTrailing(Rec(a, b, baseCutoff, maxDepth: 0, depth: 0));

    public static long[] MultiplyPar(long[] a, long[] b, int maxDepth, int baseCutoff = 64)
        => TrimTrailing(Rec(a, b, baseCutoff, maxDepth, depth: 0));

    // ---- helpers ----

    static long[] Rec(long[] a, long[] b, int baseCutoff, int maxDepth, int depth)
    {
        int n = Math.Max(a.Length, b.Length);

        if (n <= baseCutoff)
            return Naive.MultiplySeq(a, b);

        // Pad both to same power-of-two length
        int m = NextPow2(n);
        var aPad = Pad(a, m);
        var bPad = Pad(b, m);

        int half = m / 2;

        var a0 = Sub(aPad, 0, half);
        var a1 = Sub(aPad, half, half);
        var b0 = Sub(bPad, 0, half);
        var b1 = Sub(bPad, half, half);

        long[] z0, z1, z2;

        if (depth < maxDepth)
        {
            // Parallelize the 3 Karatsuba multiplications at the top levels only
            var t0 = System.Threading.Tasks.Task.Run(() => Rec(a0, b0, baseCutoff, maxDepth, depth + 1));
            var t2 = System.Threading.Tasks.Task.Run(() => Rec(a1, b1, baseCutoff, maxDepth, depth + 1));

            var sA = Add(a0, a1);
            var sB = Add(b0, b1);
            var t1 = System.Threading.Tasks.Task.Run(() => Rec(sA, sB, baseCutoff, maxDepth, depth + 1));

            System.Threading.Tasks.Task.WaitAll(t0, t1, t2);
            z0 = t0.Result; z1 = t1.Result; z2 = t2.Result;
        }
        else
        {
            z0 = Rec(a0, b0, baseCutoff, maxDepth, depth + 1);
            z2 = Rec(a1, b1, baseCutoff, maxDepth, depth + 1);
            var sA = Add(a0, a1);
            var sB = Add(b0, b1);
            z1 = Rec(sA, sB, baseCutoff, maxDepth, depth + 1);
        }

        // Karatsuba combine:
        // (a0 + a1)(b0 + b1) = z1 = z0 + z2 + middle
        var middle = Subtract(Subtract(z1, z0), z2);

        var res = new long[2 * m];

        AddAt(res, 0, z0);
        AddAt(res, half, middle);
        AddAt(res, 2 * half, z2);

        return res;
    }

    static int NextPow2(int n)
    {
        int p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    static long[] Pad(long[] a, int len)
    {
        if (a.Length == len) return a;
        var t = new long[len];
        Array.Copy(a, t, a.Length);
        return t;
    }

    static long[] Sub(long[] a, int start, int len)
    {
        var t = new long[len];
        Array.Copy(a, start, t, 0, Math.Min(len, a.Length - start));
        return t;
    }

    static long[] Add(long[] a, long[] b)
    {
        int n = Math.Max(a.Length, b.Length);
        var r = new long[n];
        for (int i = 0; i < n; i++)
        {
            long x = i < a.Length ? a[i] : 0;
            long y = i < b.Length ? b[i] : 0;
            r[i] = x + y;
        }
        return r;
    }

    static long[] Subtract(long[] a, long[] b)
    {
        int n = Math.Max(a.Length, b.Length);
        var r = new long[n];
        for (int i = 0; i < n; i++)
        {
            long x = i < a.Length ? a[i] : 0;
            long y = i < b.Length ? b[i] : 0;
            r[i] = x - y;
        }
        return r;
    }

    static void AddAt(long[] dst, int offset, long[] src)
    {
        for (int i = 0; i < src.Length; i++)
            dst[offset + i] += src[i];
    }

    static long[] TrimTrailing(long[] a)
    {
        int i = a.Length - 1;
        while (i > 0 && a[i] == 0) i--;
        if (i == a.Length - 1) return a;
        var t = new long[i + 1];
        Array.Copy(a, t, t.Length);
        return t;
    }
}

/*
 * For Karatsuba, no shared writes happen: each recursive call builds its own local result and the combining step runs in the parent thread.
 * Therefore no locks are needed. Concurrency happens only at the top recursion levels (controlled by maxDepth) to match the number of threads/cores.
   
   How deep to parallelize (tree height):
   At each node you spawn 3 child multiplications: so the number of parallel tasks at depth d is 3^d. If you have P cores/threads to use, pick
   
        maxDepth = floor( log_base_3(P) )

   so you don’t oversubscribe. (This matches the right-side tree in your photo.)
 */