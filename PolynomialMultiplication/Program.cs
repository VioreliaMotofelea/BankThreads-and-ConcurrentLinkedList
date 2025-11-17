using System;
using System.Linq;
using PolynomialMultiplication.Domain;
using PolynomialMultiplication.Algorithms;
using PolynomialMultiplication.Runtime;

class Program
{
    static int Main(string[] args)
    {
        int n = Get("-n", 1 << 12, args);
        int threads = Get("-threads", Environment.ProcessorCount, args);
        int baseCutoff = Get("-base", 64, args); // when to stop recursing and do Naive
        bool show = args.Contains("-show");

        Console.WriteLine();
        Console.WriteLine($"n={n}, threads={threads}, baseCutoff={baseCutoff}");

        var A = Poly.Random(n, seed: 1);
        var B = Poly.Random(n, seed: 2);

        // 1. Naive sequential
        var (c1, t1) = Bench.Time(() => Naive.MultiplySeq(A, B));
        c1 = Poly.Trim(c1);
        Console.WriteLine($"Naive seq     : {t1}");

        // 2. Naive parallel
        var (c2, t2) = Bench.Time(() => Naive.MultiplyPar(A, B, threads));
        c2 = Poly.Trim(c2);
        Console.WriteLine($"Naive parallel: {t2}  (degMax={threads})");

        // 3. Karatsuba sequential
        var (c3, t3) = Bench.Time(() => Karatsuba.MultiplySeq(A, B, baseCutoff));
        Console.WriteLine($"Karatsuba seq : {t3}");

        // 4. Karatsuba parallel
        int maxDepth = Math.Max(0, (int)Math.Floor(Math.Log(threads, 3.0)));
        var (c4, t4) = Bench.Time(() => Karatsuba.MultiplyPar(A, B, maxDepth, baseCutoff));
        Console.WriteLine($"Karatsuba par : {t4}  (maxDepth={maxDepth})");

        bool ok = Poly.Equal(c1, c2) && Poly.Equal(c1, c3) && Poly.Equal(c1, c4);
        Console.WriteLine(ok ? "CHECK: OK" : "CHECK: MISMATCH");
        Console.WriteLine();

        if (show)
        {
            Console.WriteLine("A " + Poly.ToShortString(A));
            Console.WriteLine("B " + Poly.ToShortString(B));
            Console.WriteLine("C " + Poly.ToShortString(c1));
        }

        return ok ? 0 : 2;
    }

    static int Get(string key, int def, string[] args)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v)) return v;
        return def;
    }
}

/*
  dotnet run --project PolynomialMultiplication -- -n 2048 -threads 8
  dotnet run --project PolynomialMultiplication -- -n 4096 -threads 8 -base 64
  dotnet run --project PolynomialMultiplication -- -n 8192 -threads 16 -show
  
  dotnet run --project PolynomialMultiplication -- -n 4096 -threads 8
  dotnet run --project PolynomialMultiplication -- -n 8192 -threads 16 -base 64
  dotnet run --project PolynomialMultiplication -- -n 2048 -threads 1
  
*/
