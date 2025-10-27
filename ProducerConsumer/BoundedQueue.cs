using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ProducerConsumer;

public sealed class BoundedQueue<T>
{
    private readonly Queue<T> _q = new();
    private readonly int _capacity;
    private bool _completed;

    private readonly object _lock = new();

    public BoundedQueue(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void Enqueue(T item)
    {
        lock (_lock)
        {
            while (_q.Count >= _capacity)
                Monitor.Wait(_lock);
            _q.Enqueue(item);
            Monitor.PulseAll(_lock);
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_lock)
        {
            while (_q.Count == 0 && !_completed)
                Monitor.Wait(_lock);

            if (_q.Count == 0 && _completed)
            {
                item = default!;
                return false;
            }

            item = _q.Dequeue();
            Monitor.PulseAll(_lock);
            return true;
        }
    }

    public void Complete()
    {
        lock (_lock)
        {
            _completed = true;
            Monitor.PulseAll(_lock);
        }
    }
}