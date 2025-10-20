using BankThreads.Domain;

namespace BankThreads.Concurrency;

public interface ILocker
{
    void LockTwo(Account a, Account b);
    void UnlockTwo(Account a, Account b);
}