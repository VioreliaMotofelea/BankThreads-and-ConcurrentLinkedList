using BankThreads.Concurrency;
using BankThreads.Domain;

namespace BankThreads.Core;

public sealed class Bank
{
    private readonly Account[] _accounts;
    private readonly ILocker _locker;
    private readonly long _initialTotal;

    public Bank(int accountCount, long initialPerAccount, ILocker locker)
    {
        _accounts = Enumerable.Range(0, accountCount)
            .Select(i => new Account(i, initialPerAccount))
            .ToArray();
        _locker = locker;
        _initialTotal = (long)accountCount * initialPerAccount;
    }

    public int AccountCount => _accounts.Length;
    public long InitialTotal => _initialTotal;

    public (Account From, Account To) RandomTwoAccounts(Random rng)
    {
        int i = rng.Next(_accounts.Length);
        int j;
        do { j = rng.Next(_accounts.Length); } while (j == i);
        return (_accounts[i], _accounts[j]);
    }

    public void Transfer(Account from, Account to, long cents)
    {
        _locker.LockTwo(from, to);
        try
        {
            long amount = Math.Min(cents, from.Balance);
            from.Balance -= amount;
            to.Balance += amount;
        }
        finally
        {
            _locker.UnlockTwo(from, to);
        }
    }

    public long TotalBalance()
    {
        for (int i = 0; i < _accounts.Length; i++)
            _accounts[i].Mutex.WaitOne();

        try
        {
            long total = 0;
            for (int i = 0; i < _accounts.Length; i++)
                total += _accounts[i].Balance;
            return total;
        }
        finally
        {
            for (int i = _accounts.Length - 1; i >= 0; i--)
                _accounts[i].Mutex.ReleaseMutex();
        }
    }
}