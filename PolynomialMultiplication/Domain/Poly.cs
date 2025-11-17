using System;
using System.Linq;

namespace PolynomialMultiplication.Domain;

public static class Poly
{
    public static long[] Random(int degreePlus1, int seed = 42, int maxAbs = 1000)
    {
        var r = new Random(seed);
        var a = new long[degreePlus1];
        for (int i = 0; i < a.Length; i++)
            a[i] = r.Next(-maxAbs, maxAbs + 1);
        return a;
    }

    public static long[] Trim(long[] c)
    {
        int i = c.Length - 1;
        while (i > 0 && c[i] == 0) i--;
        if (i == c.Length - 1) return c;
        var t = new long[i + 1];
        Array.Copy(c, t, t.Length);
        return t;
    }

    public static bool Equal(long[] a, long[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    public static string ToShortString(long[] a, int max = 6)
        => $"[{string.Join(",", a.Take(max))}{(a.Length > max ? ",..." : "")}] (len={a.Length})";
}