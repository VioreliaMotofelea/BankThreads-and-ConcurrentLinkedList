// Producer-Consumer: scalar product of two vectors
// vary -cap with 1, 2, 8, 64, 1024 to compare times

//   dotnet run -- -n 5_000_000 -cap 64
//   dotnet run -- -n 5_000_000 -cap 1
//   dotnet run -- -n 5_000_000 -cap 1024

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ProducerConsumer;

class Program
{
    static void Main(string[] args)
    {
        int n = Get(args, "-n", 5_000_000);
        int cap = Get(args, "-cap", 64);

        Console.WriteLine($"Vector length n = {n}");
        Console.WriteLine($"Queue capacity  = {cap}");

        // vectors A = [1,1,1,...,1], B = [1,2,3,...,n] => dot = n*(n+1)/2
        var A = Enumerable.Repeat(1.0, n).ToArray();
        var B = Enumerable.Range(1, n).Select(i => (double)i).ToArray();
        double expected = (double)n * (n + 1) / 2.0;

        var queue = new BoundedQueue<double>(cap);

        double result = 0.0;
        var sw = Stopwatch.StartNew();
        
        var producer = new Thread(() =>
        {
            for (int i = 0; i < n; i++)
            {
                queue.Enqueue(A[i] * B[i]);
            }
            queue.Complete();
        })
        { IsBackground = true, Name = "Producer" };
        
        var consumer = new Thread(() =>
        {
            double sum = 0.0;
            while (queue.TryDequeue(out var val))
                sum += val;
            
            Interlocked.Exchange(ref result, sum);
        })
        { IsBackground = true, Name = "Consumer" };

        producer.Start();
        consumer.Start();
        producer.Join();
        consumer.Join();

        sw.Stop();

        Console.WriteLine();
        Console.WriteLine($"Result  : {result:0}");
        Console.WriteLine($"Expected: {expected:0}");
        Console.WriteLine(result == expected ? "OK" : "MISMATCH");
        Console.WriteLine($"Elapsed : {sw.Elapsed}");
    }

    static int Get(string[] args, string key, int def)
    {
        int i = Array.IndexOf(args, key);
        if (i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out var v))
            return v;
        return def;
    }
}