using ConcurrentLinkedList.Domain;

var list = new ConcurrentDoublyLinkedList();

var a = list.InsertAfter(list.HeadSentinel, 1);
var b = list.InsertAfter(a, 2);

int threads = 8, opsPerThread = 200000;
var done = new CountdownEvent(threads);

for (int t = 0; t < threads; t++)
{
    new Thread(() =>
    {
        var rng = new Random(Environment.TickCount ^ (t * 7919));
        var cursor = a;

        for (int i = 0; i < opsPerThread; i++)
        {
            int op = rng.Next(4);
            switch (op)
            {
                case 0: cursor = list.MoveNext(cursor); break;
                case 1: cursor = list.MovePrev(cursor); break;
                case 2: cursor = list.InsertAfter(cursor, rng.Next(1000)); break;
                case 3: cursor = list.InsertBefore(cursor, rng.Next(1000)); break;
            }

            if (cursor == list.TailSentinel) cursor = list.MovePrev(cursor);
            if (cursor == list.HeadSentinel) cursor = list.MoveNext(cursor);
        }
        done.Signal();
    }) { IsBackground = true }.Start();
}

done.Wait();
Console.WriteLine($"Done. Count = {list.CountUnsafe()}");


// dotnet run --project ConcurrentLinkedList
