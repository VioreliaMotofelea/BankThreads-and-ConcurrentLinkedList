namespace DistributedSharedMemory.Dsm;

public enum MsgType
{
    WriteRequest,
    CasRequest,
    UpdateBroadcast
}

public sealed class DsmMessage
{
    public MsgType Type;
    public string Variable = "";
    public int Value;
    public int Expected;
    public long Sequence;
    public int SenderId;
}
