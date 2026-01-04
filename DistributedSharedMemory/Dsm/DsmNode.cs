using DistributedSharedMemory.Network;

namespace DistributedSharedMemory.Dsm;

public sealed class DsmNode
{
    public int NodeId { get; }

    public event Action<string, int>? OnVariableChanged;

    private readonly Dictionary<string, DsmVariable> _vars = new();
    private readonly Dictionary<string, List<int>> _subscribers = new();
    private readonly Dictionary<string, long> _nextExpectedSequence = new();
    private readonly Dictionary<string, SortedDictionary<long, DsmMessage>> _messageBuffer = new();

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
        _nextExpectedSequence[variable] = 0;
        _messageBuffer[variable] = new SortedDictionary<long, DsmMessage>();
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
            ProcessOrderedUpdate(msg);
        }
    }

    private void ProcessOrderedUpdate(DsmMessage msg)
    {
        string variable = msg.Variable;
        long expectedSeq = _nextExpectedSequence[variable];
        
        if (msg.Sequence == expectedSeq)
        {
            var v = _vars[variable];
            v.Value = msg.Value;
            _nextExpectedSequence[variable] = expectedSeq + 1;
            OnVariableChanged?.Invoke(variable, msg.Value);
            
            ProcessBufferedMessages(variable);
        }
        else if (msg.Sequence > expectedSeq)
        {
            _messageBuffer[variable][msg.Sequence] = msg;
        }
    }

    private void ProcessBufferedMessages(string variable)
    {
        var buffer = _messageBuffer[variable];
        long expectedSeq = _nextExpectedSequence[variable];
        
        while (buffer.TryGetValue(expectedSeq, out var msg))
        {
            buffer.Remove(expectedSeq);
            var v = _vars[variable];
            v.Value = msg.Value;
            _nextExpectedSequence[variable] = expectedSeq + 1;
            OnVariableChanged?.Invoke(variable, msg.Value);
            
            expectedSeq++;
        }
    }
}
