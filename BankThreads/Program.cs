/*
dotnet run -- -accounts 100 -threads 8 -ops 200000 -per-account
dotnet run -- -accounts 100 -threads 8 -ops 200000 -default-all

dotnet run -- -accounts 200 -threads 8 -ops 300000 -per-account
dotnet run -- -accounts 200 -threads 8 -ops 300000 -default-all

dotnet run -- -accounts 100 -threads 2  -ops 100000 -per-account
dotnet run -- -accounts 100 -threads 16 -ops 100000 -per-account

*/

using System.Xml;
using BankThreads.Concurrency;
using BankThreads.Core;
using BankThreads.Runtime;
using BankThreads.Util;

class Program
{
    static void Main(string[] args)
    {
        var kv = Args.Parse(args);

        int accountCount = int.Parse(kv.GetValueOrDefault("accounts", "100"));
        long initialPerAccount = long.Parse(kv.GetValueOrDefault("initial", "10000"));
        int threads = int.Parse(kv.GetValueOrDefault("threads", "8"));
        int opsPerThread = int.Parse(kv.GetValueOrDefault("ops", "200000"));
        int checkEveryMs = int.Parse(kv.GetValueOrDefault("checkms", "200"));
        bool usePerAccount = kv.ContainsKey("per-account");
        bool useDefaultAll = kv.ContainsKey("default-all");

        ILocker locker = usePerAccount || !useDefaultAll ? new OrderedPerAccountLocker() : new GlobalLocker();

        Console.WriteLine($"Accounts: {accountCount}, Initial/account: {initialPerAccount} cents");
        Console.WriteLine($"Threads: {threads}, Ops/thread: {opsPerThread}");
        Console.WriteLine($"Locking: {(locker is OrderedPerAccountLocker ? "Per-account (ordered)" : "Global mutex")}");
        Console.WriteLine($"Periodic check every {checkEveryMs} ms");
        Console.WriteLine();

        var bank = new Bank(accountCount, initialPerAccount, locker);
        var workers = new Workers(bank, threads, opsPerThread, checkEveryMs);
        var elapsed = workers.Run();

        long finalTotal = bank.TotalBalance();
        Console.WriteLine($"Final total: {finalTotal} (expected {bank.InitialTotal})");
        Console.WriteLine($"Elapsed: {elapsed}");
        Console.WriteLine(finalTotal == bank.InitialTotal ? "Invariant OK" : "Invariant BROKEN");
        Console.WriteLine();
    }
}
