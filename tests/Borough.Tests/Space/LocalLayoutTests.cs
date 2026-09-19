using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

public sealed class LocalLayoutTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(62002);
    private const ushort Terrace = 1 << (int)BlockPattern.BackToBack;
    private const ushort Courtyard = 1 << (int)BlockPattern.Courtyard;

    [Fact]
    public void Courtyard_assembles_whole_lots_preserving_neighbours_and_ground_permissions()
    {
        var (world, lots) = Fixture();
        var neighbours = new[] { LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lots[0])), LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lots[3])), LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lots[4])) };
        ulong permission = Saved(world.PermissionRectangles.Rows);
        ulong before = All(world);
        LocalLayoutProposal plan = Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        Assert.Equal(before, All(world));
        Assert.Equal(512, plan.FloorTiles);
        Assert.Equal(4, plan.HousingCapacity);
        var simulation = new Simulation(world, Key);
        Assert.True(simulation.CommitLocalLayout(plan, out var building).Accepted);
        Assert.Equal(3, world.Buildings.Rows.LiveCount);
        Assert.Equal(4, world.Lots.Rows.LiveCount);
        Assert.False(world.Lots.Rows.TryResolve(lots[1], out _));
        Assert.False(world.Lots.Rows.TryResolve(lots[2], out _));
        Assert.Equal(permission, Saved(world.PermissionRectangles.Rows));
        int row = world.Lots.Rows.Resolve(world.Buildings.Lot[world.Buildings.Rows.Resolve(building)]);
        Assert.Equal(plan.Site, LocalLot.Read(world.Lots, row).Parcel);
        Assert.Equal(plan.FloorTiles, world.Lots.FloorTiles(row));
        Assert.True(world.Lots.HasFrontage(row));
        foreach (LocalLot neighbour in neighbours) { Assert.Equal(neighbour, LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(neighbour.Handle))); }
        RefusesCommit(simulation, plan, LocalLayoutRefusal.StaleLot);
        ulong saved = world.HashState();
        world.RebuildDerived();
        Assert.Equal(saved, world.HashState());
        Assert.Equal(plan.Site, LocalLot.Read(world.Lots, row).Parcel);
    }

    [Fact]
    public void Terrace_branch_creates_only_one_building_and_repaint_invalidates_the_other()
    {
        var (world, lots) = Fixture();
        var simulation = new Simulation(world, Key);
        var a = Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        var b = Evaluate(world, [lots[2]], BlockPattern.BackToBack);
        Assert.Equal(2, a.HousingCapacity);
        Assert.True(simulation.CommitLocalLayout(a, out _).Accepted);
        Assert.True(world.Lots.IsVacant(world.Lots.Rows.Resolve(lots[2])));
        Assert.False(world.LandPermissions.At(b.Site.X, b.Site.Y).RestrictsForms);
        world.PaintFormPermissions(b.Site, true, Courtyard);
        RefusesCommit(simulation, b, LocalLayoutRefusal.Permission);
        Assert.Equal(3, world.Buildings.Rows.LiveCount);
    }

    [Theory]
    [InlineData(0, 1, BlockPattern.BackToBack, LocalLayoutRefusal.Occupied)]
    [InlineData(3, 4, BlockPattern.BackToBack, LocalLayoutRefusal.Occupied)]
    [InlineData(2, 3, BlockPattern.Courtyard, LocalLayoutRefusal.Permission)]
    [InlineData(1, 3, BlockPattern.BackToBack, LocalLayoutRefusal.Noncontiguous)]
    [InlineData(1, 1, BlockPattern.BackToBack, LocalLayoutRefusal.DuplicateLot)]
    public void Invalid_sites_refuse_without_any_saved_or_derived_mutation(int a, int b, BlockPattern form, LocalLayoutRefusal refusal)
    {
        var (world, lots) = Fixture();
        ulong before = All(world);
        LocalLayoutCheck check = LocalLayout.Evaluate(world, [lots[a], lots[b]], Plan(world, [lots[a], lots[b]], form), out var result);
        Assert.Equal(refusal, check.Refusal);
        Assert.Null(result);
        Assert.Equal(before, All(world));
    }

    [Theory]
    [InlineData(0, LocalLayoutRefusal.Permission)]
    [InlineData(1, LocalLayoutRefusal.Permission)]
    [InlineData(2, LocalLayoutRefusal.Occupied)]
    [InlineData(3, LocalLayoutRefusal.StaleProposal)]
    [InlineData(4, LocalLayoutRefusal.StaleLot)]
    [InlineData(5, LocalLayoutRefusal.StaleProposal)]
    [InlineData(6, LocalLayoutRefusal.Overlap)]
    public void Revalidation_catches_changes_before_retiring_sources(int change, LocalLayoutRefusal refusal)
    {
        var (world, lots) = Fixture();
        var simulation = new Simulation(world, Key);
        var proposal = Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        int row = world.Lots.Rows.Resolve(lots[2]);
        switch (change)
        {
            case 0: world.PaintPermissions(new(proposal.Site.X, proposal.Site.Y, 1, 1), default); break;
            case 1: world.PaintPermissions(new(proposal.Site.X, proposal.Site.Y, 1, 1), new(LotTable.Housing, 2)); break;
            case 2: world.CreateBuilding(lots[2], 1, world.Tick, Key); break;
            case 3: world.Lots.ParcelDeep[row] = new Tiles(15); break;
            case 4:
                world.Lots.Rows.Free(lots[2]);
                world.Lots.Create(new Tiles(31), Tiles.Zero, LotTable.Housing);
                break;
            case 5: world.Roads.BulldozeStreet(0, 0, StreetAxis.East); break;
            case 6:
                var extra = world.Lots.Create(new Tiles(60), Tiles.Zero, LotTable.Housing);
                int extraRow = world.Lots.Rows.Resolve(extra);
                world.Lots.ParcelEast[extraRow] = new Tiles(proposal.Site.X);
                world.Lots.ParcelNorth[extraRow] = new Tiles(proposal.Site.Y);
                break;
        }
        RefusesCommit(simulation, proposal, refusal);
    }

    [Theory]
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Courtyard)]
    public void Save_load_and_continuation_preserve_realised_geometry_and_allocator_identity(BlockPattern form)
    {
        var (world, lots) = Fixture();
        var sources = form == BlockPattern.BackToBack ? new[] { lots[1] } : new[] { lots[1], lots[2] };
        var proposal = Evaluate(world, sources, form);
        Assert.True(new Simulation(world, Key).CommitLocalLayout(proposal, out var built).Accepted);
        var file = new MemorySave();
        SaveFile.Write(world, 62002, file);
        World loaded = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        RefusesCommit(new Simulation(loaded, Key), proposal, LocalLayoutRefusal.StaleProposal);
        foreach (World city in new[] { world, loaded })
        {
            city.DestroyBuilding(built, city.Tick);
            var site = city.Buildings.Rows.TryResolve(built, out _);
            Assert.False(site);
            var next = Evaluate(city, [lots[3]], BlockPattern.BackToBack);
            Assert.True(new Simulation(city, Key).CommitLocalLayout(next, out _).Accepted);
        }
        Assert.Equal(world.HashState(), loaded.HashState());
        var uninterrupted = new Simulation(world, Key) { RouteWorkerCount = 1, VerifyDecideWritesNothing = true };
        var resumed = new Simulation(loaded, Key) { RouteWorkerCount = 2, VerifyDecideWritesNothing = true };
        for (int tick = 0; tick < 32; tick++)
        {
            uninterrupted.Step(default);
            resumed.Step(default);
            Assert.Equal(world.HashState(), loaded.HashState());
        }
        uninterrupted.CheckEndOfRun();
        resumed.CheckEndOfRun();
    }

    [Fact]
    public void Source_order_does_not_change_committed_state()
    {
        var (a, al) = Fixture();
        var (b, bl) = Fixture();
        var pa = Evaluate(a, [al[1], al[2]], BlockPattern.Courtyard);
        var pb = Evaluate(b, [bl[2], bl[1]], BlockPattern.Courtyard);
        Assert.True(new Simulation(a, Key).CommitLocalLayout(pa, out _).Accepted);
        Assert.True(new Simulation(b, Key).CommitLocalLayout(pb, out _).Accepted);
        Assert.Equal(a.HashState(), b.HashState());
    }

    [Fact]
    public void Commit_is_refused_during_decide()
    {
        var (world, lots) = Fixture();
        var simulation = new Simulation(world, Key);
        bool reached = false;
        simulation.PhaseCompleted = phase =>
        {
            if (phase != TickPhase.Decide) { return; }
            reached = true;
            RefusesCommit(simulation, Evaluate(world, [lots[1]], BlockPattern.BackToBack), LocalLayoutRefusal.WrongPhase);
        };
        simulation.Step(default);
        Assert.True(reached);
    }

    [Fact]
    public void Abandoned_shell_is_still_occupied_even_with_a_stale_reverse_index()
    {
        var (world, lots) = Fixture();
        int row = world.Lots.Rows.Resolve(lots[1]);
        var building = world.CreateBuilding(lots[1], 1, world.Tick, Key);
        world.Buildings.AbandonedSince[world.Buildings.Rows.Resolve(building)] = new Ticks(1);
        world.Lots.BuildingSlot[row] = 0;
        ulong before = All(world);
        Assert.Equal(LocalLayoutRefusal.Occupied,
            LocalLayout.Evaluate(world, [lots[1]], Plan(world, [lots[1]], BlockPattern.BackToBack), out _).Refusal);
        Assert.Equal(before, All(world));
    }

    [Fact]
    public void Condemnation_history_remains_severed_after_reuse_and_save_load()
    {
        var (world, lots) = Fixture();
        world.CondemnationTrail.Record(world.Tick, lots[1], 1, ConditionId.None);
        var proposal = Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        Assert.True(new Simulation(world, Key).CommitLocalLayout(proposal, out _).Accepted);
        Assert.Equal(lots[1], world.CondemnationTrail.Lot[1]);
        Assert.False(world.Lots.Rows.TryResolve(world.CondemnationTrail.Lot[1], out _));
        var file = new MemorySave();
        SaveFile.Write(world, 62002, file);
        var loaded = SaveFile.Read(file, world.Rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        Assert.Equal(lots[1], loaded.CondemnationTrail.Lot[1]);
        Assert.False(loaded.Lots.Rows.TryResolve(loaded.CondemnationTrail.Lot[1], out _));
    }

    [Fact]
    public void Exhausted_fitted_state_allocator_refuses_before_capacity_or_identity_changes()
    {
        var (world, lots) = Fixture();
        var proposal = Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        // A valid table with no remaining monotonic ids exercises an otherwise impractical bound.
        Rows rows = world.Bins.Rows;
        var bytes = new MemorySave();
        foreach (Column column in rows.SavedColumns) { bytes.Write(column.StorageBytes(rows.SlotCount)); }
        rows.Restore(rows.SlotCount, rows.LiveCount, rows.FreeHead, ulong.MaxValue, bytes);
        var capacities = world.Tables.ToArray().Select(t => t.Capacity).ToArray();
        RefusesCommit(new Simulation(world, Key), proposal, LocalLayoutRefusal.Storage);
        Assert.Equal(capacities, world.Tables.ToArray().Select(t => t.Capacity));
    }

    [Fact]
    public void Mixed_use_capacity_and_repeated_removal_reclaim_all_fitted_state()
    {
        var (world, lots) = Fixture(mixed: true);
        var simulation = new Simulation(world, Key);
        Handle<Lot>[] sources = [lots[1], lots[2]];
        int[]? slots = null;
        for (int cycle = 0; cycle < 12; cycle++)
        {
            var proposal = Evaluate(world, sources, BlockPattern.Courtyard);
            Assert.Equal(4, proposal.Occupancy);
            Assert.Equal(3, proposal.HousingCapacity);
            Assert.True(simulation.CommitLocalLayout(proposal, out var building).Accepted);
            Assert.Equal(3, world.Businesses.Rows.LiveCount);
            Assert.Equal(3, world.CarParks.Rows.LiveCount);
            Assert.Equal(6, world.RuleInstances.Rows.LiveCount);
            int buildingRow = world.Buildings.Rows.Resolve(building);
            sources = [world.Buildings.Lot[buildingRow]];
            world.DestroyBuilding(building, world.Tick);
            Assert.Equal(2, world.Businesses.Rows.LiveCount);
            Assert.Equal(2, world.CarParks.Rows.LiveCount);
            Assert.Equal(4, world.RuleInstances.Rows.LiveCount);
            var now = world.Tables.ToArray().Select(t => t.SlotCount).ToArray();
            if (slots is not null) { Assert.Equal(slots, now); }
            slots = now;
            simulation.CheckEndOfRun();
        }
    }

    [Theory]
    [InlineData(BlockPattern.BackToBack)]
    [InlineData(BlockPattern.Tower)]
    public void Proposed_floor_area_refuses_overflow(BlockPattern form)
    {
        Assert.False(BuildingPlan.TryFloorTiles(form, 16384, 16384, int.MaxValue, out _));
        Assert.False(BuildingPlan.TryFloorTiles(form, int.MaxValue, 2, 1, out _));
    }

    [Theory]
    [InlineData(BlockFace.South)]
    [InlineData(BlockFace.North)]
    [InlineData(BlockFace.West)]
    [InlineData(BlockFace.East)]
    public void Assembly_preserves_access_on_each_street_face(BlockFace face)
    {
        var (world, lots) = Fixture(face: face);
        var proposal = Evaluate(world, [lots[1], lots[2]], BlockPattern.Courtyard);
        var simulation = new Simulation(world, Key);
        Assert.True(simulation.CommitLocalLayout(proposal, out var building).Accepted);
        int row = world.Lots.Rows.Resolve(world.Buildings.Lot[world.Buildings.Rows.Resolve(building)]);
        Assert.True(world.Lots.HasFrontage(row));
        Assert.Equal(proposal.Site, LocalLot.Read(world.Lots, row).Parcel);
        simulation.CheckEndOfRun();
    }

    [Fact]
    public void Proposal_does_not_survive_a_tick_or_ruleset_reload()
    {
        var (world, lots) = Fixture();
        var simulation = new Simulation(world, Key);
        var proposal = Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        world.Advance();
        RefusesCommit(simulation, proposal, LocalLayoutRefusal.StaleProposal);
        proposal = Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        world.Adopt(RulesetLoader.Parse(Toml, "reload.toml").Ruleset!, 1, world.Tick, Key);
        RefusesCommit(simulation, proposal, LocalLayoutRefusal.StaleProposal);
    }

    [Theory]
    [InlineData(0, LocalLayoutRefusal.InvalidGeometry)]
    [InlineData(1, LocalLayoutRefusal.UnsupportedKind)]
    [InlineData(2, LocalLayoutRefusal.NoFrontage)]
    [InlineData(3, LocalLayoutRefusal.IncompatibleFrontage)]
    [InlineData(4, LocalLayoutRefusal.AddressConflict)]
    [InlineData(5, LocalLayoutRefusal.InvalidGeometry)]
    public void Geometry_kind_and_address_refusals_are_read_only(int change, LocalLayoutRefusal expected)
    {
        var (world, lots) = Fixture();
        Handle<Lot>[] sources = [lots[1], lots[2]];
        var plan = Plan(world, sources, BlockPattern.Courtyard);
        int row = world.Lots.Rows.Resolve(lots[2]);
        switch (change)
        {
            case 0: plan = plan with { Footprint = plan.Footprint with { Width = plan.Footprint.Width + 1 } }; break;
            case 1: plan = plan with { Kind = 255 }; break;
            case 2: world.Lots.North[row] = new Tiles(1); break;
            case 3:
                world.Roads.LayStreet(0, 1, StreetAxis.East);
                world.Lots.North[row] = new Tiles(64);
                world.Lots.Side[row] = (byte)StreetSide.Right;
                break;
            case 4:
                var extra = world.Lots.Create(world.Lots.East[world.Lots.Rows.Resolve(lots[1])], Tiles.Zero, LotTable.Housing);
                int extraRow = world.Lots.Rows.Resolve(extra);
                world.Lots.ParcelEast[extraRow] = new Tiles(100);
                world.Lots.ParcelNorth[extraRow] = new Tiles(100);
                break;
            case 5: plan = plan with { Storeys = 0 }; break;
        }
        ulong before = All(world);
        Assert.Equal(expected, LocalLayout.Evaluate(world, sources, plan, out var proposal).Refusal);
        Assert.Null(proposal);
        Assert.Equal(before, All(world));
    }

    private static void RefusesCommit(Simulation simulation, LocalLayoutProposal proposal, LocalLayoutRefusal refusal)
    {
        ulong before = All(simulation.World);
        Assert.Equal(refusal, simulation.CommitLocalLayout(proposal, out var result).Refusal);
        Assert.True(result.IsNone);
        Assert.Equal(before, All(simulation.World));
    }

    private static ulong Saved(Rows rows) { ulong hash = 0; rows.Fold(ref hash); return hash; }

    private static ulong All(World world)
    {
        ulong hash = world.HashState();
        foreach (Rows rows in world.Tables) { rows.FoldAll(ref hash); }
        return hash;
    }

    internal static LocalLayoutProposal Evaluate(World world, Handle<Lot>[] lots, BlockPattern form)
    {
        var check = LocalLayout.Evaluate(world, lots, Plan(world, lots, form), out var proposal);
        Assert.True(check.Accepted, check.ToString());
        return proposal!;
    }

    private static LocalBuildingPlan Plan(World world, Handle<Lot>[] lots, BlockPattern form)
    {
        var parcels = lots.Select(l => LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(l)).Parcel).ToArray();
        int x = parcels.Min(p => p.X), end = parcels.Max(p => p.X + p.Width);
        int y = parcels.Min(p => p.Y), top = parcels.Max(p => p.Y + p.Height);
        return new(1, form, new(x, y, end - x, top - y), 2);
    }

    internal static (World World, Handle<Lot>[] Lots) Fixture(bool mixed = false, BlockFace face = BlockFace.South)
    {
        var loaded = RulesetLoader.Parse(mixed ? MixedToml : Toml, "local-layout.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        var world = new World(0, loaded.Ruleset!, Key);
        Assert.True(world.Roads.LayStreet(face == BlockFace.East ? 1 : 0, face == BlockFace.North ? 1 : 0,
            face is BlockFace.South or BlockFace.North ? StreetAxis.East : StreetAxis.North));
        var lots = new Handle<Lot>[5];
        int gap = world.Rules.Lots.StreetHalfWidthTiles;
        for (int i = 0; i < lots.Length; i++)
        {
            int x = gap + 12 * i;
            bool horizontal = face is BlockFace.South or BlockFace.North;
            int across = face is BlockFace.North or BlockFace.East ? 64 : 0;
            StreetSide side = face is BlockFace.South or BlockFace.East ? StreetSide.Left : StreetSide.Right;
            lots[i] = world.Lots.Create(new Tiles(horizontal ? x + 6 : across), new Tiles(horizontal ? across : x + 6), LotTable.Housing, side);
            int row = world.Lots.Rows.Resolve(lots[i]);
            int behind = across == 0 ? gap : across - gap - 16;
            world.Lots.ParcelEast[row] = world.Lots.FootprintEast[row] = new Tiles(horizontal ? x : behind);
            world.Lots.ParcelNorth[row] = world.Lots.FootprintNorth[row] = new Tiles(horizontal ? behind : x);
            world.Lots.ParcelWide[row] = world.Lots.FootprintWide[row] = new Tiles(horizontal ? 12 : 16);
            world.Lots.ParcelDeep[row] = world.Lots.FootprintDeep[row] = new Tiles(horizontal ? 16 : 12);
            world.Lots.Storeys[row] = 2;
        }
        world.Frontage.Rebuild(world.Lots, world.Roads.Streets);
        world.CreateBuilding(lots[0], 1, Ticks.Zero, Key);
        world.CreateBuilding(lots[4], 1, Ticks.Zero, Key);
        for (int i = 1; i <= 3; i++)
        {
            world.PaintPermissions(LocalLot.Read(world.Lots, world.Lots.Rows.Resolve(lots[i])).Parcel,
                new(LotTable.Housing, 0, i != 2, i == 1 ? (ushort)(Terrace | Courtyard) : i == 3 ? Terrace : (ushort)0));
        }
        return (world, lots);
    }

    private const string Toml = """
        [[resource]]
        name = "sundries"
        family = "good"
        [[building]]
        name = "dwelling"
        houses = true
        bins = [{ resource = "sundries", capacity = 48 }]
        [roads]
        block_tiles = 64
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000
        [lots]
        lots_per_segment = 5
        setback_tiles = 2
        [capacity]
        floor_tiles_per_occupant = 128
        floor_tiles_per_job = 1
        floor_tiles_per_parking_space = 6
        """;
    private static readonly string MixedToml = Toml.Replace("houses = true", "houses = true\npremises = true\nparked = true\nbusiness = \"shop\"", StringComparison.Ordinal)
        .Replace("bins = [{ resource = \"sundries\", capacity = 48 }]",
            "bins = [{ resource = \"sundries\", capacity = 48 }, { resource = \"stock\", capacity = 96, owner = \"business\" }, { resource = \"money\", owner = \"business\" }]", StringComparison.Ordinal)
        + """

        [[resource]]
        name = "stock"
        family = "good"
        [[resource]]
        name = "money"
        family = "money"
        [[business]]
        name = "shop"
        shift_start_earliest_hour = 6
        shift_start_latest_hour = 10
        [[rule]]
        name = "upkeep"
        kind = "dwelling"
        rate = 16
        apply = { min = 1, max = 1 }
        inputs = [{ scope = "local", resource = "sundries", amount = 1 }]
        outputs = []
        [[rule]]
        name = "sell"
        kind = "dwelling"
        rate = 16
        apply = { min = 1, max = 1 }
        inputs = [{ scope = "local", resource = "stock", amount = 1 }]
        outputs = []
        """;

}
