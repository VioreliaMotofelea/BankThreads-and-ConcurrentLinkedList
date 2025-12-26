using DistributedSharedMemory.Dsm;

class Program
{
    static void Main()
    {
        var n0 = new DsmNode(0);
        var n1 = new DsmNode(1);
        var n2 = new DsmNode(2);

        var subs = new List<int> { 0, 1, 2 };

        n0.Subscribe("A", subs);
        n1.Subscribe("A", subs);
        n2.Subscribe("A", subs);

        n0.OnVariableChanged += (v, val) => Console.WriteLine("[P0] " + v + "=" + val);
        n1.OnVariableChanged += (v, val) => Console.WriteLine("[P1] " + v + "=" + val);
        n2.OnVariableChanged += (v, val) => Console.WriteLine("[P2] " + v + "=" + val);

        n1.Write("A", 1);
        n2.Write("A", 2);
        n0.CompareAndExchange("A", 2, 5);

        Console.ReadLine();
    }
}
