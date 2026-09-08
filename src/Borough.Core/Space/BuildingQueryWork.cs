namespace Borough.Core.Space;

public sealed class BuildingQueryWork
{
    public long PrefixReads { get; set; }
    public long RebuiltCells { get; set; }
    public long CountCalls { get; set; }
    public long CountCells { get; set; }
    public long CandidateCalls { get; set; }
    public long CandidateCells { get; set; }
    public long CandidateLinks { get; set; }
    public void Reset() { PrefixReads = RebuiltCells = CountCalls = CountCells = CandidateCalls = CandidateCells = CandidateLinks = 0; }
}
