namespace BankThreads.Domain;

public class Account
{
    public int Id { get; }
    public long Balance;
    public readonly Mutex Mutex = new(initiallyOwned: false);

    public Account(int id, long initial)
    {
        Id = id;
        Balance = initial;
    }
}