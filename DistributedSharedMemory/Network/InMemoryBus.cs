using DistributedSharedMemory.Dsm;

namespace DistributedSharedMemory.Network;

public static class InMemoryBus
{
    private static readonly Dictionary<int, DsmNode> Nodes = new();

    public static void Register(DsmNode node)
    {
        Nodes[node.NodeId] = node;
    }

    public static void Send(int to, DsmMessage msg)
    {
        Nodes[to].Receive(msg);
    }

    public static void Multicast(IEnumerable<int> targets, DsmMessage msg)
    {
        foreach (var t in targets)
            Send(t, msg);
    }
}
