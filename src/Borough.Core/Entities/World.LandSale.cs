namespace Borough.Core.Entities;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

public sealed partial class World
{
    private LandCapital[] _landCapital = [];
    private int _landCapitalCount;
    private bool _landSaleSweep;
    private bool _landCapitalReady;
    private int _maximumCapitalHousehold = Rows.NoSlot;
    private bool _maximumCapitalReady;

    private readonly record struct LandCapital(long Available, int Household) : IComparable<LandCapital>
    {
        public int CompareTo(LandCapital other)
        {
            int money = Available.CompareTo(other.Available);
            return money != 0 ? money : Household.CompareTo(other.Household);
        }
    }

    // Capital scratch lasts one Zone Rule sweep. Only a buyer draw needs the sorted index.
    internal void BeginLandSaleSweep()
    { _landSaleSweep = true; _landCapitalReady = false; _maximumCapitalReady = false; }
    internal void EndLandSaleSweep()
    { _landSaleSweep = false; _landCapitalReady = false; _maximumCapitalReady = false; }

    internal int MaximumCapitalHousehold()
    {
        if (_landSaleSweep && _maximumCapitalReady) { return _maximumCapitalHousehold; }
        int richest = Rows.NoSlot;
        if (_landSaleSweep && _landCapitalReady)
        {
            if (_landCapitalCount > 0) { richest = _landCapital[_landCapitalCount - 1].Household; }
        }
        else
        {
            long maximum = long.MinValue;
            for (int row = 0; row < Households.Rows.SlotCount; row++)
            {
                if (!Households.Rows.IsLive(row) || !Bins.Rows.IsValid(Households.Balance[row])) { continue; }
                long available = AvailableCapital(row);
                if (available > maximum) { maximum = available; richest = row; }
            }
        }
        _maximumCapitalHousehold = richest;
        _maximumCapitalReady = _landSaleSweep;
        return richest;
    }

    private void IndexLandCapital()
    {
        if (_landCapital.Length < Households.Rows.SlotCount)
        { Array.Resize(ref _landCapital, Households.Rows.SlotCount); }
        _landCapitalCount = 0;
        for (int row = 0; row < Households.Rows.SlotCount; row++)
        {
            if (Households.Rows.IsLive(row) && Bins.Rows.IsValid(Households.Balance[row]))
            { _landCapital[_landCapitalCount++] = new(AvailableCapital(row), row); }
        }
        Array.Sort(_landCapital, 0, _landCapitalCount);
        _landCapitalReady = true;
    }

    private long AvailableCapital(int household) => Bins.LevelAt(Bins.Rows.Resolve(Households.Balance[household]))
        - (Rules.Lots.BuyerReservePerMember?.Raw ?? 0) * Members.Length(household);

    private void RefreshLandCapital(int household, long previous)
    {
        // Only the maximum's own fall needs a rescan. A rise elsewhere either takes the maximum or leaves it.
        long available = AvailableCapital(household);
        if (_maximumCapitalReady && household == _maximumCapitalHousehold)
        {
            if (available < previous) { _maximumCapitalReady = false; }
        }
        else if (_maximumCapitalReady
            && (_maximumCapitalHousehold == Rows.NoSlot || available > AvailableCapital(_maximumCapitalHousehold)))
        {
            _maximumCapitalHousehold = household;
        }
        if (!_landSaleSweep || !_landCapitalReady) { return; }
        int before = Array.BinarySearch(_landCapital, 0, _landCapitalCount, new LandCapital(previous, household));
        if (before < 0) { _landCapitalReady = false; return; }
        var updated = new LandCapital(AvailableCapital(household), household);
        int after = Array.BinarySearch(_landCapital, 0, _landCapitalCount, updated);
        if (after >= 0) { return; }
        after = ~after;
        if (after > before)
        {
            after--;
            Array.Copy(_landCapital, before + 1, _landCapital, before, after - before);
        }
        else
        {
            Array.Copy(_landCapital, after, _landCapital, after + 1, before - after);
        }
        _landCapital[after] = updated;
    }

    /// <summary>Land value adjusts the Lot's footprint price.</summary>
    public Money LandPrice(int lot) => LandPrice(Lots.East[lot], Lots.North[lot],
        (long)Lots.FootprintWide[lot].Raw * Lots.FootprintDeep[lot].Raw);

    internal Money LandPrice(Tiles east, Tiles north, long footprintTiles)
    {
        long rate = Rules.Lots.DemolitionPricePerTile.Raw;
        if (rate == 0) { return Money.Zero; }
        long factor = (long)Fixed.One + Layers.LandValue(CellGrid.ToCells(east), CellGrid.ToCells(north));
        return factor <= 0 ? Money.Zero : new Money(IntegerMath.MulDivFloor(
            factor * footprintTiles, rate, Fixed.One));
    }

    /// <summary>A built Lot includes the cost of replacing its floor.</summary>
    public Money LotPrice(int lot)
    {
        int building = Lots.BuildingOn(lot);
        return LandPrice(lot) + (building == Rows.NoSlot ? Money.Zero : ConstructionCharge(FloorTilesOf(building)));
    }

    /// <summary>Construction sends this Money outside the city for the specified floor area.</summary>
    public Money ConstructionCharge(int floorTiles) =>
        new((Rules.Lots.ConstructionChargePerFloorTile?.Raw ?? 0) * floorTiles);

    /// <summary>The owner can build, or another eligible Household can buy and build.</summary>
    public bool CanFinanceConstruction(int lot, int floorTiles) => !Rules.Lots.LandSales
        || ConstructionBuyer(lot, Lots.LandownerOn(lot), LandPrice(lot), ConstructionCharge(floorTiles), Tick, draw: false) != Rows.NoSlot;

    internal bool FinanceConstruction(int lot, int floorTiles, Ticks tick)
    {
        if (!Rules.Lots.LandSales) { return true; }
        int seller = Lots.LandownerOn(lot);
        Money land = LandPrice(lot), charge = ConstructionCharge(floorTiles);
        int buyer = ConstructionBuyer(lot, seller, land, charge, tick, draw: true);
        if (buyer == Rows.NoSlot) { return false; }
        PayConstruction(buyer, seller, land, charge, tick);
        Lots.Landowner[lot] = Households.Rows.At(buyer);
        return true;
    }

    internal int ConstructionBuyer(int lot, int seller, Money land, Money charge, Ticks tick, bool draw, int? capitalBuyer = null)
    {
        if (seller != Rows.NoSlot && CanKeepReserve(seller, charge.Raw)) { return seller; }
        long cost = land.Raw + charge.Raw;
        if (!draw)
        {
            int richest = capitalBuyer ?? MaximumCapitalHousehold();
            return richest != Rows.NoSlot && CanKeepReserve(richest, cost) ? richest : Rows.NoSlot;
        }
        // An owner who cannot afford the charge cannot appear in the more expensive sale suffix.
        return DrawBuyer(lot, Rows.NoSlot, cost, tick, PurposeTag.LandBuyer);
    }

    private int DrawBuyer(int lot, int excluded, long cost, Ticks tick, PurposeTag purpose)
    {
        if (!_landSaleSweep || !_landCapitalReady) { IndexLandCapital(); }
        int first = Array.BinarySearch(_landCapital, 0, _landCapitalCount, new LandCapital(cost, -1));
        first = ~first;
        int count = _landCapitalCount - first;
        int skip = excluded == Rows.NoSlot ? -1
            : Array.BinarySearch(_landCapital, 0, _landCapitalCount, new LandCapital(AvailableCapital(excluded), excluded));
        if (skip >= first) { count--; } else { skip = -1; }
        if (count <= 0) { return Rows.NoSlot; }
        int at = first + (int)(Randomness.Draw(Key, Lots.Rows.IdAt(lot), tick, purpose) % (ulong)(uint)count);
        return _landCapital[skip >= 0 && at >= skip ? at + 1 : at].Household;
    }

    /// <summary>A departing Household sells each Lot it holds. A Lot nobody buys passes to the city.</summary>
    /// <remarks>The proceeds reach the seller's balance, which leaves the city with it.</remarks>
    internal void SellOnDeparture(int seller, Ticks tick)
    {
        if (!Rules.Lots.LandSales) { return; }
        bool opened = OpenSales();
        for (int lot = 0; lot < Lots.Rows.SlotCount; lot++)
        {
            if (Lots.Rows.IsLive(lot) && Lots.LandownerOn(lot) == seller) { SellLot(lot, seller, tick); }
        }
        if (opened) { EndLandSaleSweep(); }
    }

    /// <summary>On its cadence, the city offers each built Lot it holds for sale.</summary>
    internal void SellCityLots(Ticks tick)
    {
        if (Rules.Lots.CitySaleIntervalDays is not int interval || tick.Raw % (ulong)Ticks.PerDay != 0UL
            || IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay) % interval != 0)
        { return; }
        bool opened = OpenSales();
        for (int lot = 0; lot < Lots.Rows.SlotCount; lot++)
        {
            if (Lots.Rows.IsLive(lot) && !Lots.IsVacant(lot) && Lots.IsCityHeld(lot)) { SellLot(lot, Rows.NoSlot, tick); }
        }
        if (opened) { EndLandSaleSweep(); }
    }

    private bool OpenSales()
    {
        if (_landSaleSweep) { return false; }
        BeginLandSaleSweep();
        return true;
    }

    /// <summary>Sells a Lot at its price, to a Household housed on it if one can pay, else to any.</summary>
    internal bool SellLot(int lot, int seller, Ticks tick)
    {
        Money price = LotPrice(lot);
        int richest = MaximumCapitalHousehold();
        if (richest == Rows.NoSlot || !CanKeepReserve(richest, price.Raw)) { return false; }
        int buyer = TenantBuyer(lot, seller, price.Raw, tick);
        if (buyer == Rows.NoSlot) { buyer = DrawBuyer(lot, seller, price.Raw, tick, PurposeTag.LotResaleBuyer); }
        if (buyer == Rows.NoSlot) { return false; }
        PayConstruction(buyer, seller, price, Money.Zero, tick);
        Lots.Landowner[lot] = Households.Rows.At(buyer);
        return true;
    }

    private int TenantBuyer(int lot, int seller, long price, Ticks tick)
    {
        int building = Lots.BuildingOn(lot);
        if (building == Rows.NoSlot) { return Rows.NoSlot; }
        int eligible = 0;
        foreach (int tenant in Occupants.Walk(building))
        {
            if (tenant != seller && CanKeepReserve(tenant, price)) { eligible++; }
        }
        if (eligible == 0) { return Rows.NoSlot; }
        int pick = (int)(Randomness.Draw(Key, Lots.Rows.IdAt(lot), tick, PurposeTag.LotTenantBuyer) % (ulong)(uint)eligible);
        foreach (int tenant in Occupants.Walk(building))
        {
            if (tenant != seller && CanKeepReserve(tenant, price) && pick-- == 0) { return tenant; }
        }
        return Rows.NoSlot;
    }

    private bool CanKeepReserve(int household, long cost) =>
        Bins.Rows.TryResolve(Households.Balance[household], out int bin)
        && Bins.LevelAt(bin) - (Rules.Lots.BuyerReservePerMember?.Raw ?? 0) * Members.Length(household) >= cost;

    internal void PayConstruction(int buyer, int seller, Money land, Money charge, Ticks tick)
    {
        long previousBuyer = AvailableCapital(buyer);
        long previousSeller = seller == Rows.NoSlot || seller == buyer ? 0 : AvailableCapital(seller);
        Handle<Bin> balance = Households.Balance[buyer];
        if (buyer != seller && land.Raw > 0)
        {
            Handle<Bin> recipient = seller == Rows.NoSlot
                ? Bins.Rows.At(FindTreasuryBin(MoneyResourceOf(Rules))) : Households.Balance[seller];
            Transfer(balance, recipient, land.Raw, tick);
        }
        if (charge.Raw > 0)
        {
            Withdraw(balance, charge.Raw, tick);
            PayOutside(charge);
        }
        RefreshLandCapital(buyer, previousBuyer);
        if (seller != Rows.NoSlot && seller != buyer) { RefreshLandCapital(seller, previousSeller); }
    }

    internal LocalLayoutCheck AssemblyOwner(LocalLayoutProposal proposal, out int owner)
    {
        owner = Lots.LandownerOn(Lots.Rows.Resolve(proposal.Sources[0].Handle));
        if (!Rules.Lots.LandSales) { return default; }
        foreach (LocalLot source in proposal.Sources)
        {
            if (Lots.LandownerOn(Lots.Rows.Resolve(source.Handle)) != owner)
            { return new(LocalLayoutRefusal.MixedOwnership); }
        }
        return default;
    }
}
