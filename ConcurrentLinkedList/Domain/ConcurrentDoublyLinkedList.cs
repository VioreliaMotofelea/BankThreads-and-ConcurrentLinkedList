using System.Threading;

namespace ConcurrentLinkedList.Domain;

public sealed class ConcurrentDoublyLinkedList
{
    private readonly Node _head;
    private readonly Node _tail;

    public ConcurrentDoublyLinkedList()
    {
        _head = new Node(long.MinValue, int.MinValue);
        _tail = new Node(long.MaxValue, int.MaxValue);
        _head.Next = _tail;
        _tail.Prev = _head;
    }

    public Node HeadSentinel => _head;
    public Node TailSentinel => _tail;

    public Node MoveNext(Node current)
    {
        current.Mtx.WaitOne();
        try { return current.Next; }
        finally { current.Mtx.ReleaseMutex(); }
    }

    public Node MovePrev(Node current)
    {
        current.Mtx.WaitOne();
        try { return current.Prev; }
        finally { current.Mtx.ReleaseMutex(); }
    }

    private static void LockOrdered(Node a, Node b)
    {
        if (a.Id < b.Id) { a.Mtx.WaitOne(); b.Mtx.WaitOne(); }
        else if (a.Id > b.Id) { b.Mtx.WaitOne(); a.Mtx.WaitOne(); }
        else { a.Mtx.WaitOne(); }
    }

    private static void UnlockOrdered(Node a, Node b)
    {
        if (a.Id < b.Id) { b.Mtx.ReleaseMutex(); a.Mtx.ReleaseMutex(); }
        else if (a.Id > b.Id) { a.Mtx.ReleaseMutex(); b.Mtx.ReleaseMutex(); }
        else { a.Mtx.ReleaseMutex(); }
    }

    public Node InsertAfter(Node current, int value)
    {
        while (true)
        {
            Node next = current.Next; 

            LockOrdered(current, next);
            try
            {
                if (current.Next != next || next.Prev != current)
                    continue;

                var n = new Node(value)
                {
                    Prev = current,
                    Next = next
                };

                current.Next = n;
                next.Prev = n;

                return n;
            }
            finally
            {
                UnlockOrdered(current, next);
            }
        }
    }

    public Node InsertBefore(Node current, int value)
    {
        while (true)
        {
            Node prev = current.Prev;

            LockOrdered(prev, current);
            try
            {
                if (prev.Next != current || current.Prev != prev)
                    continue;

                var n = new Node(value)
                {
                    Prev = prev,
                    Next = current
                };

                prev.Next = n;
                current.Prev = n;

                return n;
            }
            finally
            {
                UnlockOrdered(prev, current);
            }
        }
    }

    public int CountUnsafe()
    {
        int c = 0;
        for (var p = _head.Next; p != _tail; p = p.Next) c++;
        return c;
    }
}
