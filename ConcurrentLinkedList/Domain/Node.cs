namespace ConcurrentLinkedList.Domain;

public sealed class Node
{
    private static long _nextId;

    internal readonly Mutex Mtx = new(initiallyOwned: false);

    internal Node(long id, int value)
    {
        Id = id;
        Value = value;
    }

    internal Node(int value) : this(Interlocked.Increment(ref _nextId), value) { }

    public long Id { get; }
    public int  Value { get; set; }

    internal Node Prev = null!;
    internal Node Next = null!;

    public override string ToString() => $"Node(Id={Id}, Value={Value})";
}