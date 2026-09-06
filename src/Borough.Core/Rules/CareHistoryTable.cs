using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
namespace Borough.Core.Rules;

public readonly struct CareEvent;
[Table]
public sealed class CareHistoryTable
{
    public CareHistoryTable(CitizenTable citizens, HouseholdTable households, BuildingTable buildings)
    {
        Rows = new Rows<CareEvent>("care_history", 64, Buffering.OneCopy);
        Tick = Rows.Saved<Ticks>("tick");
        CitizenId = Rows.Saved<ulong>("citizenid");
        HouseholdId = Rows.Saved<ulong>("householdid");
        BuildingId = Rows.Saved<ulong>("buildingid");
        Event = Rows.Saved<byte>("event");
        Severity = Rows.Saved<int>("severity");
        Value = Rows.Saved<long>("value");
        Rows.Seal();
    }
    public Rows<CareEvent> Rows { get; }
    public Column<Ticks> Tick { get; }
    public Column<ulong> CitizenId { get; }
    public Column<ulong> HouseholdId { get; }
    public Column<ulong> BuildingId { get; }
    public Column<byte> Event { get; }
    public Column<int> Severity { get; }
    public Column<long> Value { get; }
}
