namespace DistributedSharedMemory.Dsm;

public sealed class DsmVariable
{
    public int Value;
    public long NextSequence = 0;
    public readonly int SequencerId;

    public DsmVariable(int sequencerId)
    {
        SequencerId = sequencerId;
    }
}
