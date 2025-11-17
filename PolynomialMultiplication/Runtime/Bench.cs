using System;
using System.Diagnostics;

namespace PolynomialMultiplication.Runtime;

public static class Bench
{
    public static (T result, TimeSpan elapsed) Time<T>(Func<T> fn)
    {
        var sw = Stopwatch.StartNew();
        var r = fn();
        sw.Stop();
        return (r, sw.Elapsed);
    }
}