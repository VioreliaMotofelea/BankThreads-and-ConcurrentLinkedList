using System.Diagnostics;
using BankThreads.Core;

namespace BankThreads.Runtime;

public sealed class Workers
{
    private readonly Bank _bank;
    private readonly int _threadCount;
    private readonly int _opsPerThread;
    private readonly int _checkEveryMs;
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _checkerThread;

    public Workers(Bank bank, int threadCount, int opsPerThread, int checkEveryMs)
    {
        _bank = bank;
        _threadCount = threadCount;
        _opsPerThread = opsPerThread;
        _checkEveryMs = checkEveryMs;
        _checkerThread = new Thread(PeriodicCheck) { IsBackground = true, Name = "Checker" };
    }

    public TimeSpan Run()
    {
        var sw = Stopwatch.StartNew();
        
        _checkerThread.Start();
        
        var threads = new List<Thread>(_threadCount);
        for (int t = 0; t < _threadCount; t++)
        {
            int seed = Environment.TickCount ^ (t * 7919);
            var thread = new Thread(() => DoWork(seed)) { IsBackground = true, Name = $"W{t}" };
            threads.Add(thread);
            thread.Start();
        }

        foreach (var th in threads) th.Join();

        _cts.Cancel();
        _checkerThread.Join();

        sw.Stop();
        return sw.Elapsed;
    }

    private void DoWork(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < _opsPerThread; i++)
        {
            var (from, to) = _bank.RandomTwoAccounts(rng);
            long max = Math.Max(1, (_bank.InitialTotal / _bank.AccountCount) / 100);
            long cents = 1 + rng.NextInt64(max);
            _bank.Transfer(from, to, cents);
        }
    }

    private void PeriodicCheck()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_checkEveryMs > 0)
                {
                    if (_cts.Token.WaitHandle.WaitOne(_checkEveryMs)) break;
                    long total = _bank.TotalBalance();
                    if (total != _bank.InitialTotal)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[CHECK] Invariant BROKEN: total={total} expected={_bank.InitialTotal}");
                        Console.ResetColor();
                        Environment.ExitCode = 2;
                    }
                }
                else
                {
                    _cts.Token.WaitHandle.WaitOne(10);
                }
            }
        }
        catch { /* */ }
    }
}