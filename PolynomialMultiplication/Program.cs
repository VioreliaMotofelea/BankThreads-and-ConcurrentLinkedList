using System;
using System.Linq;
using PolynomialMultiplication.Domain;
using PolynomialMultiplication.Algorithms;
using PolynomialMultiplication.Runtime;

class Program
{
    static int Main(string[] args)
    {
        int n = Get("-n", 1 << 12, args); // polynomial size (coefficient count)
        int threads = Get("-threads", Environment.ProcessorCount, args);
        int baseCutoff = Get("-base", 64, args); // when to stop recursing and do Naive
        bool show = args.Contains("-show");

        Console.WriteLine($"n={n}, threads={threads}, baseCutoff={baseCutoff}");

        var A = Poly.Random(n, seed: 1);
        var B = Poly.Random(n, seed: 2);

        // 1) Naïve sequential
        var (c1, t1) = Bench.Time(() => Naive.MultiplySeq(A, B));
        c1 = Poly.Trim(c1);
        Console.WriteLine($"Naïve seq     : {t1}");

        // 2) Naïve parallel
        var (c2, t2) = Bench.Time(() => Naive.MultiplyPar(A, B, threads));
        c2 = Poly.Trim(c2);
        Console.WriteLine($"Naïve parallel: {t2}  (degMax={threads})");

        // 3) Karatsuba sequential
        var (c3, t3) = Bench.Time(() => Karatsuba.MultiplySeq(A, B, baseCutoff));
        Console.WriteLine($"Karatsuba seq : {t3}");

        // 4) Karatsuba parallel (choose top depth so 3^depth <= threads)
        int maxDepth = Math.Max(0, (int)Math.Floor(Math.Log(threads, 3.0)));
        var (c4, t4) = Bench.Time(() => Karatsuba.MultiplyPar(A, B, maxDepth, baseCutoff));
        Console.WriteLine($"Karatsuba par : {t4}  (maxDepth={maxDepth})");

        // Verify all equal
        bool ok = Poly.Equal(c1, c2) && Poly.Equal(c1, c3) && Poly.Equal(c1, c4);
        Console.WriteLine(ok ? "CHECK: OK ✅" : "CHECK: MISMATCH ❌");

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
  
  
  Algorithms
   
   Naïve O(n²): c[k] = Σ a[i]*b[j] for all i+j=k. Two nested loops.
   
   Karatsuba:
   Split A, B into low/high halves (a0,a1, b0,b1), compute:
   
   z0 = a0*b0, z2 = a1*b1, z1 = (a0+a1)*(b0+b1)
   Combine result as:
   res = z0 + ((z1 - z0 - z2) << m) + (z2 << 2m) where m = n/2.
   Recurse until n <= baseCutoff, then switch to naïve (better cache behavior).
   
   Synchronization
   
   Naïve parallel: concurrent updates to c[i+j] are protected with Interlocked.Add(ref long, …). This is per-cell synchronization; independent indices proceed without contention.
   
   Karatsuba parallel: no shared writes; each recursion builds its own arrays and the parent combines them. Only task joins are needed—no locks/mutexes.
   
   Parallelism strategy (don’t “parallelize everything”)
   
   Karatsuba spawns 3 subproblems per level. Choose maxDepth = floor(log₃(threads)) so at most ~threads subproblems run in parallel. Below that depth, recurse sequentially to avoid oversubscription.
   
   Also keep a base cutoff (baseCutoff, e.g., 64–128) to stop recursion when subproblems are small and run the cache-friendly naïve kernel.
   
   Performance measurements
   
   Vary n (e.g., 2k, 4k, 8k, 16k), threads (1,2,4,8,16), and base cutoff (32,64,128).
   
   Record wall‐clock times for all 4 variants.
   
   You should see:
   
   Karatsuba beats naïve for large n.
   
   Parallel naïve gives moderate speedup (limited by memory bandwidth and Interlocked).
   
   Parallel Karatsuba gives the best scaling when maxDepth matches your core count.
   
   Bonus – big numbers
   
   Replace long with System.Numerics.BigInteger:
   
   For naïve parallel use thread-local accumulators (arrays of BigInteger) and combine at the end (because Interlocked.Add doesn’t support BigInteger).
   
   Karatsuba changes only the arithmetic type; the structure remains identical.
*/
