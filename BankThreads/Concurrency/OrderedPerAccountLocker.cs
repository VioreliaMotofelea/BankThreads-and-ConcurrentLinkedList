using BankThreads.Domain;

namespace BankThreads.Concurrency;

public class OrderedPerAccountLocker : ILocker
{
    public void LockTwo(Account a, Account b)
    {
        if (a.Id == b.Id) { a.Mutex.WaitOne(); return; }

        Account first = a.Id < b.Id ? a : b; // strict order
        Account second = a.Id < b.Id ? b : a;

        first.Mutex.WaitOne();
        second.Mutex.WaitOne();
    }

    public void UnlockTwo(Account a, Account b)
    {
        if (a.Id == b.Id) { a.Mutex.ReleaseMutex(); return; }

        Account first = a.Id < b.Id ? a : b;
        Account second = a.Id < b.Id ? b : a;

        second.Mutex.ReleaseMutex();
        first.Mutex.ReleaseMutex();
    }
}