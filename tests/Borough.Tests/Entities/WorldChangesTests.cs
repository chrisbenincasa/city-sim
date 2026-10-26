using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Input;

namespace Borough.Tests.Entities;

public sealed class WorldChangesTests
{
    [Fact]
    public void Replacing_a_building_in_the_same_slot_is_reported_without_moving_the_hash()
    {
        var observed = new World(32) { Changes = new WorldChanges() };
        var plain = new World(32);
        Assert.True(observed.Changes.Full);
        observed.Changes.Clear();
        Handle<Building> first = Build(observed);
        Handle<Building> other = Build(plain);
        int slot = observed.Buildings.Rows.Resolve(first);
        Assert.Equal(new[] { slot }, observed.Changes.Buildings.ToArray());
        Assert.Equal(plain.HashState(), observed.HashState());
        observed.Changes.Clear();
        observed.DestroyBuilding(first, new Ticks(1));
        plain.DestroyBuilding(other, new Ticks(1));
        Handle<Building> replacement = Build(observed);
        Build(plain);
        Assert.Equal(slot, observed.Buildings.Rows.Resolve(replacement));
        Assert.NotEqual(first, replacement);
        Assert.Equal(new[] { slot }, observed.Changes.Buildings.ToArray());
        Assert.Equal(plain.HashState(), observed.HashState());
        observed.Changes.Clear();
        Assert.Empty(observed.Changes.Buildings.ToArray());
    }

    [Fact]
    public void An_invalidated_journal_keeps_recording()
    {
        var world = new World(32) { Changes = new WorldChanges() };
        world.Changes.Clear();
        world.Changes.Invalidate();
        int slot = world.Buildings.Rows.Resolve(Build(world));
        Assert.True(world.Changes.Full);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
    }

    [Fact]
    public void Occupancy_and_abandonment_report_the_building_with_no_population_scan()
    {
        var world = new World(32) { Changes = new WorldChanges() };
        Handle<Building> building = Build(world);
        int slot = world.Buildings.Rows.Resolve(building);
        world.Changes.Clear();
        Handle<Household> household = world.CreateHousehold(building, 0);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
        world.Changes.Clear();
        world.DestroyHousehold(household);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
        world.Changes.Clear();
        world.AbandonBuilding(building, new Ticks(1), AbandonmentCause.Condemned);
        Assert.Equal(new[] { slot }, world.Changes.Buildings.ToArray());
    }

    [Fact]
    public void Letting_and_vacating_a_Unit_report_its_building()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "pictured.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(2_000, loaded.Ruleset!, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.Changes = new WorldChanges();
        world.Changes.Clear();

        (int centre, int tenant) = FirstLetCentreUnit(world);
        world.DestroyBusiness(world.Businesses.Rows.At(tenant));
        Assert.Contains(centre, world.Changes.Buildings.ToArray());

        world.Changes.Clear();
        Assert.True(world.OpenInVacantUnit(centre));
        Assert.Contains(centre, world.Changes.Buildings.ToArray());
    }

    private static (int Centre, int Tenant) FirstLetCentreUnit(World world)
    {
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.IsTradeCentre(building)) continue;
            foreach (int unit in world.BuildingUnits.Walk(building))
            {
                int tenant = world.Units.TenantSlot(unit);
                if (tenant >= 0 && world.Units.Anchor[unit] == 0) return (building, tenant);
            }
        }

        throw new InvalidOperationException("pictured.toml raised no centre with a let Unit.");
    }

    [Fact]
    public void Every_Lot_Segment_and_permission_rectangle_a_Tick_changes_is_reported()
    {
        RulesetLoadResult loaded =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "schooled.toml"));
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0x57_4EE7UL);
        var world = new World(2_000, loaded.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.EndowTreasury(new Money(100_000_000));
        world.Changes = new WorldChanges();

        (Command street, int through) = StreetVerbTests.Through(world);
        int demolished = OtherBuildingLot(world, through);
        Func<Command>[] ticks =
        [
            () => street,
            () => Command.Demolish(world.Lots.East[demolished], world.Lots.North[demolished]),
            () => Command.Service(world.Lots.East[demolished], world.Lots.North[demolished], ServingKind(world.Rules)),
            () => ZoneAnyBlock(world),
            () => DemolishFrontedStreet(world),
        ];
        int lots = 0, segments = 0, rectangles = 0;
        for (int tick = 0; tick < ticks.Length + 120; tick++)
        {
            var lotsBefore = Lots(world);
            var segmentsBefore = Segments(world);
            var rectanglesBefore = Rectangles(world);
            Command[] commands = tick < ticks.Length ? [ticks[tick]()] : [];
            foreach (Command command in commands) Assert.Equal(Refusal.None, simulation.Refuses(command));
            world.Changes.Clear();
            simulation.Step(new TickInput(commands, 0));
            Assert.False(world.Changes.Full);
            lots += Unreported(lotsBefore, Lots(world), world.Changes.Lots, "Lot", tick);
            segments += Unreported(segmentsBefore, Segments(world), world.Changes.Segments, "Segment", tick);
            rectangles += Unreported(rectanglesBefore, Rectangles(world), world.Changes.PermissionRectangles, "Permission rectangle", tick);
        }

        Assert.True(lots > 0 && segments > 0 && rectangles > 0, $"Changed {lots} Lots, {segments} Segments, {rectangles} rectangles.");
    }

    private static int Unreported<T>(T[] before, T[] after, ReadOnlySpan<int> reported, string what, int tick)
    {
        var marked = new HashSet<int>(reported.ToArray());
        int changed = 0;
        for (int slot = 0; slot < Math.Max(before.Length, after.Length); slot++)
        {
            T was = slot < before.Length ? before[slot] : default!;
            T now = slot < after.Length ? after[slot] : default!;
            if (EqualityComparer<T>.Default.Equals(was, now)) continue;
            changed++;
            Assert.True(marked.Contains(slot), $"{what} {slot} changed at Tick {tick} and was not reported.");
        }

        return changed;
    }

    private static (ulong, OrientedRectangle, OrientedRectangle, int, Handle<RoadSegment>, Tiles, byte, ushort, (byte, byte, byte, Tiles, Tiles))[] Lots(World world)
    {
        LotTable lots = world.Lots;
        var rows = new (ulong, OrientedRectangle, OrientedRectangle, int, Handle<RoadSegment>, Tiles, byte, ushort, (byte, byte, byte, Tiles, Tiles))[lots.Rows.SlotCount];
        for (int slot = 0; slot < rows.Length; slot++)
        {
            if (!lots.Rows.IsLive(slot)) continue;
            rows[slot] = (lots.Rows.IdAt(slot), lots.Parcel(slot), lots.Footprint(slot), lots.BuildingOn(slot),
                lots.FrontageSegment[slot], lots.FrontageOffset[slot], lots.Side[slot], lots.Zone[slot],
                (lots.Storeys[slot], lots.Pattern[slot], lots.PodiumStoreys[slot], lots.East[slot], lots.North[slot]));
        }

        return rows;
    }

    private static (ulong, Handle<RoadNode>, Handle<RoadNode>, byte, byte, byte, Tiles, SubTiles, StreetArc)[] Segments(World world)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        var rows = new (ulong, Handle<RoadNode>, Handle<RoadNode>, byte, byte, byte, Tiles, SubTiles, StreetArc)[segments.Rows.SlotCount];
        for (int slot = 0; slot < rows.Length; slot++)
        {
            if (!segments.Rows.IsLive(slot)) continue;
            rows[slot] = (segments.Rows.IdAt(slot), segments.NodeA[slot], segments.NodeB[slot], segments.Kind[slot],
                segments.ModesForward[slot], segments.ModesBackward[slot], segments.LengthTiles[slot],
                segments.Sagitta[slot], segments.Centerline[slot]);
        }

        return rows;
    }

    private static (ulong, int, int, int, int, ulong)[] Rectangles(World world)
    {
        LandPermissionTable paint = world.PermissionRectangles;
        var rows = new (ulong, int, int, int, int, ulong)[paint.Rows.SlotCount];
        for (int slot = 0; slot < rows.Length; slot++)
        {
            if (!paint.Rows.IsLive(slot)) continue;
            rows[slot] = (paint.Rows.IdAt(slot), paint.X[slot], paint.Y[slot], paint.Width[slot], paint.Height[slot], paint.Permission[slot]);
        }

        return rows;
    }

    private static int OtherBuildingLot(World world, int except)
    {
        for (int building = world.Buildings.Rows.SlotCount - 1; building >= 0; building--)
        {
            if (building == except || !world.Buildings.Rows.IsLive(building)) continue;
            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
            if (world.Lots.HasFrontage(lot)) return lot;
        }

        throw new InvalidOperationException("No other Building fronts a Street.");
    }

    private static byte ServingKind(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
            if (rules.Kind((byte)kind).Serves != Need.None) return (byte)kind;

        throw new InvalidOperationException("The Ruleset declares no serving kind.");
    }

    private static Command ZoneAnyBlock(World world)
    {
        ushort permission = world.Rules.ZoneRules.Length > 0 ? world.Rules.ZoneRules[^1].Admits : LotTable.Trade;
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot) || world.Lots.Zone[lot] == permission) continue;
            OrientedRectangle parcel = world.Lots.Parcel(lot);
            return new Command(CommandKind.Zone, new Tiles((int)IntegerMath.RoundDiv(parcel.Center.East, Fixed.One)),
                new Tiles((int)IntegerMath.RoundDiv(parcel.Center.North, Fixed.One)), permission);
        }

        throw new InvalidOperationException("Every Lot already carries the permission.");
    }

    private static Command DemolishFrontedStreet(World world)
    {
        for (int lot = world.Lots.Rows.SlotCount - 1; lot >= 0; lot--)
        {
            if (!world.Lots.Rows.IsLive(lot) || world.Lots.IsVacant(lot)) continue;
            int segment = world.Lots.FrontageOn(lot);
            if (segment < 0 || (RoadKind)world.Roads.Segments.Kind[segment] != RoadKind.Street) continue;
            StreetArc line = world.Roads.Segments.Centerline[segment];
            var middle = line.PointAt(line.Length / 2);
            return Command.Demolish(new Tiles((int)IntegerMath.RoundDiv(middle.East, Fixed.One)),
                new Tiles((int)IntegerMath.RoundDiv(middle.North, Fixed.One)), DemolishTarget.Street);
        }

        throw new InvalidOperationException("No occupied Lot fronts a Street.");
    }

    private static Handle<Building> Build(World world)
    {
        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(1), 1);
        return world.CreateBuilding(lot, 1, Ticks.Zero, WorldKey.FromSeed(1));
    }
}
