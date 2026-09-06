using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;
namespace Borough.Core.Rules;

public readonly struct KnownClinic;
[Table]
public sealed class KnownClinicTable
{
    public KnownClinicTable(CitizenTable citizens, HouseholdTable households, BuildingTable buildings)
    {
        Rows = new Rows<KnownClinic>("known_clinic", 64, Buffering.OneCopy);
        Building = Rows.SavedHandle("building", buildings.Rows, reference: Reference.Severable);
        Next = Rows.Saved<int>("next");
        Rows.Seal();
    }
    public Rows<KnownClinic> Rows { get; }
    public HandleColumn<Building> Building { get; }
    public Column<int> Next { get; }
}
