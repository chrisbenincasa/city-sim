using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
namespace Borough.Core.Rules;

public readonly struct CivicVisit;
[Table]
public sealed class CivicTable
{
    public CivicTable(CitizenTable citizens, HouseholdTable households, BuildingTable buildings)
    {
        Rows = new Rows<CivicVisit>("civic", 64, Buffering.OneCopy);
        Citizen = Rows.SavedHandle("citizen", citizens.Rows, reference: Reference.Severable);
        Household = Rows.SavedHandle("household", households.Rows, reference: Reference.Severable);
        Place = Rows.SavedHandle("place", buildings.Rows, reference: Reference.Severable);
        Provider = Rows.SavedHandle("provider", buildings.Rows, reference: Reference.Severable);
        Destination = Rows.SavedHandle("destination", buildings.Rows, reference: Reference.Severable);
        Stage = Rows.Saved<byte>("stage");
        NextAt = Rows.Saved<Ticks>("nextat");
        EndsAt = Rows.Saved<Ticks>("endsat");
        RoutineAt = Rows.Saved<Ticks>("routineat");
        RequestedAt = Rows.Saved<Ticks>("requestedat");
        IllSince = Rows.Saved<Ticks>("illsince");
        ProgressAt = Rows.Saved<Ticks>("progressat");
        TreatedUntil = Rows.Saved<Ticks>("treateduntil");
        SchoolDay = Rows.Saved<long>("schoolday");
        MissedTicks = Rows.Saved<long>("missedticks");
        LostWages = Rows.Saved<long>("lostwages");
        LostRemainder = Rows.Saved<long>("lostremainder");
        LastMissedDay = Rows.Saved<long>("lastmissedday");
        Reason = Rows.Saved<byte>("reason");
        LastWaitDay = Rows.Saved<long>("lastwaitday");
        AdmissionWanted = Rows.Saved<byte>("admission_wanted");
        Rows.Seal();
    }
    public Column<byte> AdmissionWanted { get; }
    public Rows<CivicVisit> Rows { get; }
    public HandleColumn<Citizen> Citizen { get; }
    public HandleColumn<Household> Household { get; }
    public HandleColumn<Building> Place { get; }
    public HandleColumn<Building> Provider { get; }
    public HandleColumn<Building> Destination { get; }
    public Column<byte> Stage { get; }
    public Column<Ticks> NextAt { get; }
    public Column<Ticks> EndsAt { get; }
    public Column<Ticks> RoutineAt { get; }
    public Column<Ticks> RequestedAt { get; }
    public Column<Ticks> IllSince { get; }
    public Column<Ticks> ProgressAt { get; }
    public Column<Ticks> TreatedUntil { get; }
    public Column<long> SchoolDay { get; }
    public Column<long> MissedTicks { get; }
    public Column<long> LostWages { get; }
    public Column<long> LostRemainder { get; }
    public Column<long> LastMissedDay { get; }
    public Column<byte> Reason { get; }
    public Column<long> LastWaitDay { get; }
}
