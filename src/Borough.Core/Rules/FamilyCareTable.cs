using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
namespace Borough.Core.Rules;

public readonly struct FamilyCare;
[Table]
public sealed class FamilyCareTable
{
    public FamilyCareTable(CitizenTable citizens, HouseholdTable households, BuildingTable buildings)
    {
        Rows = new Rows<FamilyCare>("family_care", 64, Buffering.OneCopy);
        Household = Rows.SavedHandle("household", households.Rows, reference: Reference.Severable);
        Habitual = Rows.SavedHandle("habitual", buildings.Rows, reference: Reference.Severable);
        KnownHead = Rows.Saved<int>("knownhead");
        ExcessWaits = Rows.Saved<int>("excesswaits");
        LastExcessDay = Rows.Saved<long>("lastexcessday");
        Rows.Seal();
    }
    public Rows<FamilyCare> Rows { get; }
    public HandleColumn<Household> Household { get; }
    public HandleColumn<Building> Habitual { get; }
    public Column<int> KnownHead { get; }
    public Column<int> ExcessWaits { get; }
    public Column<long> LastExcessDay { get; }
}
