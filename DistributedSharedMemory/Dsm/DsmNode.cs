using DistributedSharedMemory.Network;

namespace DistributedSharedMemory.Dsm;

public sealed class DsmNode
{
    public int NodeId { get; }

    public event Action<string, int>? OnVariableChanged;

    private readonly Dictionary<string, DsmVariable> _vars = new();
    private readonly Dictionary<string, List<int>> _subscribers = new();

    public DsmNode(int id)
    {
        NodeId = id;
        InMemoryBus.Register(this);
    }

    public void Subscribe(string variable, List<int> subscribers)
    {
        int sequencer = subscribers.Min();
        _vars[variable] = new DsmVariable(sequencer);
        _subscribers[variable] = subscribers;
    }

    public void Write(string variable, int value)
    {
        var v = _vars[variable];
        var msg = new DsmMessage
        {
            Type = MsgType.WriteRequest,
            Variable = variable,
            Value = value,
            SenderId = NodeId
        };
        InMemoryBus.Send(v.SequencerId, msg);
    }

    public bool CompareAndExchange(string variable, int expected, int newValue)
    {
        var v = _vars[variable];
        var msg = new DsmMessage
        {
            Type = MsgType.CasRequest,
            Variable = variable,
            Expected = expected,
            Value = newValue,
            SenderId = NodeId
        };
        InMemoryBus.Send(v.SequencerId, msg);
        return true;
    }

    public void Receive(DsmMessage msg)
    {
        var v = _vars[msg.Variable];

        if (msg.Type == MsgType.WriteRequest || msg.Type == MsgType.CasRequest)
        {
            if (NodeId != v.SequencerId) return;

            if (msg.Type == MsgType.CasRequest && v.Value != msg.Expected)
                return;

            v.Value = msg.Value;

            var update = new DsmMessage
            {
                Type = MsgType.UpdateBroadcast,
                Variable = msg.Variable,
                Value = msg.Value,
                Sequence = v.NextSequence++,
                SenderId = NodeId
            };

            InMemoryBus.Multicast(_subscribers[msg.Variable], update);
        }
        else if (msg.Type == MsgType.UpdateBroadcast)
        {
            v.Value = msg.Value;
            OnVariableChanged?.Invoke(msg.Variable, msg.Value);
        }
    }
}
