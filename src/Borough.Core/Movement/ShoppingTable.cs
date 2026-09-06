using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core.Movement;

public readonly struct ShoppingOuting;
public readonly struct KnownShop;
public enum ShoppingFailure : byte { None, Empty, Unaffordable, Closed, Unreachable, NoKnownShop, LostCargo }

[Table]
public sealed class ShoppingTable
{
    public ShoppingTable(HouseholdTable households, CitizenTable citizens, BuildingTable buildings, BusinessTable businesses)
    {
        Rows = new Rows<ShoppingOuting>("shopping", 64, Buffering.OneCopy);
        Household = Rows.SavedHandle("household", households.Rows, reference: Reference.Severable);
        Citizen = Rows.SavedHandle("citizen", citizens.Rows, reference: Reference.Severable);
        Place = Rows.SavedHandle("place", buildings.Rows, reference: Reference.Severable);
        Destination = Rows.SavedHandle("destination", buildings.Rows, reference: Reference.Severable);
        Seller = Rows.SavedHandle("seller", businesses.Rows, reference: Reference.Severable);
        Good = Rows.Saved<ResourceId>("good");
        Cargo = Rows.Saved<long>("cargo");
        Wanted = Rows.Saved<long>("wanted");
        Stage = Rows.Saved<byte>("stage");
        Attempts = Rows.Saved<byte>("attempts");
        Severe = Rows.Saved<byte>("severe");
        NextAt = Rows.Saved<Ticks>("next_at");
        UnservedSince = Rows.Saved<Ticks>("unserved_since");
        Failed = Rows.Saved<ushort>("failed_occasions");
        Reason = Rows.Saved<byte>("reason");
        ProviderHead = Rows.Saved<int>("provider_head");
        Cursor = Rows.Saved<int>("cursor");
        Rows.Seal();
    }
    public Rows<ShoppingOuting> Rows { get; }
    public HandleColumn<Household> Household { get; }
    public HandleColumn<Citizen> Citizen { get; }
    public HandleColumn<Building> Place { get; }
    public HandleColumn<Business> Seller { get; }
    public HandleColumn<Building> Destination { get; }
    public Column<ResourceId> Good { get; }
    public Column<long> Cargo { get; }
    public Column<long> Wanted { get; }
    public Column<byte> Stage { get; }
    public Column<byte> Attempts { get; }
    public Column<byte> Severe { get; }
    public Column<Ticks> NextAt { get; }
    public Column<ushort> Failed { get; }
    public Column<Ticks> UnservedSince { get; }
    public Column<byte> Reason { get; }
    public Column<int> ProviderHead { get; }
    public Column<int> Cursor { get; }
}

[Table]
public sealed class KnownShopTable
{
    public KnownShopTable(BusinessTable businesses)
    {
        Rows = new Rows<KnownShop>("known_shop", 64, Buffering.OneCopy);
        Business = Rows.SavedHandle("business", businesses.Rows, reference: Reference.Severable);
        Next = Rows.Saved<int>("next");
        Rows.Seal();
    }
    public Rows<KnownShop> Rows { get; }
    public HandleColumn<Business> Business { get; }
    public Column<int> Next { get; }
}
