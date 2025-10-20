using BankThreads.Domain;

namespace BankThreads.Concurrency;

public class GlobalLocker : ILocker
{
    private readonly Mutex _bankMutex = new(initiallyOwned: false);

    public void LockTwo(Account a, Account b) => _bankMutex.WaitOne();
    public void UnlockTwo(Account a, Account b) => _bankMutex.ReleaseMutex();
}