using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Headless;

namespace Borough.Tests.Evidence;

/// <summary>
/// Utilities slice 1 — what a Building's explanation says about the Power it is served.
/// </summary>
/// <remarks>
/// <para>
/// <b>A brownout is a success, so none of the fields a stopped Rule fills can report one.</b> A
/// delivered-Utility term never blocks — the Rule applies <c>floor(count × delivered)</c> times
/// instead — so every assertion here is about <see cref="UtilityEvidence"/> and about the Rules
/// <em>not</em> being blocked beside it.
/// </para>
/// <para>
/// <b>The world is a ridge with one plant and one occupied dwelling</b>, which is
/// <c>PowerDeliveryTests</c>' shape without its greedy works. That omission is what makes the east
/// <b>fully</b> delivered while the road stands: the dwelling's whole Day of demand fits inside one
/// crossing's capacity, so the only thing that can take its Power away is the road itself.
/// </para>
/// </remarks>
public sealed class UtilityEvidenceTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    /// <summary><c>powered.toml</c>'s first <c>[[resource]]</c>, and its delivered Utility.</summary>
    private static readonly ResourceId Power = new(1);

    private const byte Dwelling = 1;
    private const byte Plant = 2;

    /// <summary>The fourth kind, declared only by <see cref="FallbackChain"/>.</summary>
    private const byte Lamp = 4;

    private const int Cells = 8;
    private const int PeakCells = 2;
    private const int Tall = 8;
    private const int Short = 3;

    /// <summary>Households in the one dwelling that draws Power, and so its apply count.</summary>
    private const int Occupants = 2;

    private sealed class City
    {
        public required World World { get; init; }

        public required Simulation Simulation { get; init; }

        /// <summary>The District the occupied dwelling stands in.</summary>
        public required int Burning { get; init; }

        /// <summary>The occupied dwelling's Building slot.</summary>
        public required int Home { get; init; }

        /// <summary>The plant's Building slot.</summary>
        public required int Generator { get; init; }

        /// <summary>The lamp's Building slot, or <c>Rows.NoSlot</c> where none was raised.</summary>
        public int Lit { get; init; } = Rows.NoSlot;
    }

    private static RulesetLoadResult Loaded()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded;
    }

    private static Ruleset Rules() => Loaded().Ruleset!;

    /// <summary>
    /// A fourth kind whose Power draw sits on an <c>on_fail</c> link rather than on the head.
    /// </summary>
    /// <remarks>
    /// <b>The head starts failing</b>, because the lamp's widget Bin begins empty. The link refills
    /// that Bin from the Pool, which is <c>adr/0045</c>'s ladder with a Utility at the bottom of it,
    /// and a reporting terminal closes the chain as the loader requires. A reader stopping at the
    /// head's own inputs calls this Building undrawn.
    /// </remarks>
    private const string FallbackChain = """

        [[building]]
        name = "lamp"
        bins = [
          { resource = "lumens", capacity = 65536 },
          { resource = "widget", capacity = 16 },
        ]

        [[rule]]
        name    = "lamp_oil"
        kind    = "lamp"
        rate    = 32
        apply   = { min = 1, max = 1 }
        on_fail = "lamp_mains"
        inputs  = [ { scope = "local", resource = "widget", amount = 1 } ]
        outputs = [ { scope = "local", resource = "lumens", amount = 1 } ]

        [[rule]]
        name    = "lamp_mains"
        kind    = "lamp"
        rate    = 32
        apply   = { min = 1, max = 1 }
        on_fail = "lamp_dark"
        inputs  = [ { scope = "pool", resource = "power", amount = 4 } ]
        outputs = [ { scope = "local", resource = "widget", amount = 1 } ]

        [[rule]]
        name    = "lamp_dark"
        kind    = "lamp"
        rate    = 32
        apply   = { min = 1, max = 1 }
        inputs  = []
        outputs = []
        reports = "unlit"

        """;

    /// <summary><c>powered.toml</c> with <see cref="FallbackChain"/> spliced in above its tables.</summary>
    private static Ruleset ChainedRules()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        const string tables = "\n[districts]\n";

        Assert.Contains(tables, toml, StringComparison.Ordinal);

        RulesetLoadResult loaded = RulesetLoader.Parse(
            toml.Replace(tables, FallbackChain + tables, StringComparison.Ordinal),
            "powered-chained.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    private static City Build(Ruleset? rules = null, bool lamp = false)
    {
        var world = new World(400, rules ?? Rules(), Key);
        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = true };

        Lay(world);

        int generator = Rows.NoSlot;
        int home = Rows.NoSlot;
        int lit = Rows.NoSlot;

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;
            int standing = 0;

            if (cell == 0)
            {
                generator = Raise(world, cell, standing++, Plant);
            }

            if (cell == Cells - 1)
            {
                home = Raise(world, cell, standing++, Dwelling);

                for (int occupant = 0; occupant < Occupants; occupant++)
                {
                    world.CreateHousehold(world.Buildings.Rows.At(home), lifeStage: 1);
                }

                // It takes a filler's place, so the ridge keeps its shape and its Building count
                // and the watershed falls where every other test here finds it.
                if (lamp)
                {
                    lit = Raise(world, cell, standing++, Lamp);
                }
            }

            // The rest are empty dwellings, raised to shape the watershed. Each one still draws
            // Power through its lighting Rule; its apply count is its occupancy, so it draws none.
            for (int filler = standing; filler < (peak ? Tall : Short); filler++)
            {
                Raise(world, cell, filler, Dwelling);
            }
        }

        world.RebuildParcels();
        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Equal(2, world.Districts.Rows.LiveCount);

        int making = DistrictOf(world, generator);
        int burning = DistrictOf(world, home);

        Assert.NotEqual(Rows.NoSlot, making);
        Assert.NotEqual(making, burning);

        return new City
        {
            World = world,
            Simulation = simulation,
            Burning = burning,
            Home = home,
            Generator = generator,
            Lit = lit,
        };
    }

    /// <summary>The ridge: a chain of Nodes a block apart along <c>north = 0</c>.</summary>
    private static void Lay(World world)
    {
        int block = world.Rules.Roads.BlockTiles;
        Handle<RoadNode> previous = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int step = 1; step <= Cells; step++)
        {
            Handle<RoadNode> next = world.Roads.Nodes.Create(new Tiles(step * block), Tiles.Zero);

            world.Roads.Segments.Create(
                previous, next, new Tiles(block), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        world.Roads.RebuildDerived();
    }

    /// <summary>Tiles of ground under each Building, and the stride that keeps them apart.</summary>
    /// <remarks>
    /// ⚠ <b>The ground is stated because the shell draws no massing for a one-Tile Lot.</b> The
    /// frontage floor is 6 m and a Tile is 4 m, so the default parcel <c>LotTable.Create</c> seals
    /// leaves the ridge invisible — which is the whole of what the driven demonstration looks at.
    /// Occupancy counts occupants and not places, so the wider ground moves no number asserted here.
    /// </remarks>
    private const int GroundTiles = 3;

    private const int StrideTiles = 4;

    private static int Raise(World world, int cell, int index, byte kind)
    {
        int block = world.Rules.Roads.BlockTiles;

        Handle<Lot> lot = world.Lots.Create(
            new Tiles((cell * block) + 1 + (index * StrideTiles)),
            Tiles.Zero,
            zone: 1,
            wide: new Tiles(GroundTiles),
            deep: new Tiles(GroundTiles),
            storeys: kind == Plant ? (byte)1 : (byte)2);

        return world.Buildings.Rows.Resolve(world.CreateBuilding(lot, kind, Ticks.Zero, Key));
    }

    private static int DistrictOf(World world, int buildingSlot)
    {
        int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[buildingSlot]);

        return DistrictAt(world, world.Lots.East[lot], world.Lots.North[lot]);
    }

    private static int DistrictAt(World world, Tiles east, Tiles north)
    {
        Handle<District> district = world.DistrictsInCells.Of(
            world.DistrictCells, CellGrid.ToCells(east), CellGrid.ToCells(north));

        return world.Districts.Rows.TryResolve(district, out int slot) ? slot : Rows.NoSlot;
    }

    /// <summary>The lower Cell index of the one neighboring pair in different Districts.</summary>
    private static int Boundary(World world)
    {
        int block = world.Rules.Roads.BlockTiles;

        for (int cell = 0; cell < Cells - 1; cell++)
        {
            if (DistrictAt(world, new Tiles((cell * block) + 4), Tiles.Zero)
                != DistrictAt(world, new Tiles(((cell + 1) * block) + 4), Tiles.Zero))
            {
                return cell;
            }
        }

        throw new InvalidOperationException("the ridge has no District boundary.");
    }

    private static void StepTo(Simulation simulation, long tick)
    {
        while ((long)simulation.World.Tick.Raw < tick)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// Steps through the settle that closes <paramref name="day"/> and no further, so the fraction
    /// read next governs a Day that has run exactly one Tick.
    /// </summary>
    private static void AfterSettling(Simulation simulation, int day) =>
        StepTo(simulation, (((long)day + 1) * Ticks.PerDay) + 1);

    private static void Issue(Simulation simulation, Command command)
    {
        int refused = simulation.CommandsRefused;

        simulation.Step(new TickInput([command], rulesetHash: 0));

        Assert.Equal(refused, simulation.CommandsRefused);
    }

    /// <summary>The one delivered Utility the Building draws, as its explanation reports it.</summary>
    private static UtilityEvidence Drawn(World world, int buildingSlot)
    {
        BuildingEvidence evidence =
            Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(buildingSlot));

        Assert.Equal(1, evidence.Utilities.Length);
        Assert.Equal(Power, evidence.Utilities.Span[0].Utility);

        return evidence.Utilities.Span[0];
    }

    private static UtilityEvidence Brownout(World world, int buildingSlot) =>
        Core.Evidence.Evidence
            .SupplyOfBuilding(world, world.Buildings.Rows.At(buildingSlot))
            .Brownout;

    /// <summary>
    /// The explanation names Power and its delivered fraction, and loses it with the road that
    /// carries it. <b>The acceptance line of slice 1's readouts.</b>
    /// </summary>
    /// <remarks>
    /// Both edits go through the player's own door, and the recovery is asserted as the fraction the
    /// Building had before the outage rather than merely as something above zero.
    /// </remarks>
    [Fact]
    public void A_cut_off_building_reports_power_below_full_and_reports_it_full_again()
    {
        City city = Build();
        World world = city.World;

        AfterSettling(city.Simulation, day: 0);

        UtilityEvidence lit = Drawn(world, city.Home);

        Assert.Equal(Ratio.One, lit.Delivered);
        Assert.True(lit.Asked > 0, "nothing in the east asked for Power");
        Assert.Equal(lit.Asked, lit.Received);

        // Nothing is short, so there is no Utility to name in the one-line reading.
        Assert.Equal(0, Brownout(world, city.Home).Utility.Raw);

        int block = world.Rules.Roads.BlockTiles;
        int boundary = Boundary(world);
        var west = new Tiles(boundary * block);
        var east = new Tiles((boundary + 1) * block);

        Issue(city.Simulation, Command.Demolish(
            new Tiles(east.Raw - 1), Tiles.Zero, DemolishTarget.Street));

        AfterSettling(city.Simulation, day: 1);

        UtilityEvidence dark = Drawn(world, city.Home);

        Assert.True(dark.Delivered < Ratio.One, "the cut-off dwelling still reports full Power");
        Assert.Equal(Ratio.Zero, dark.Delivered);
        Assert.True(dark.Asked > 0, "the cut-off dwelling stopped asking");
        Assert.Equal(0, dark.Received);

        // The one-line reading names the Utility and the Rules are not blocked, because a
        // delivered Utility never blocks. That is why this field had to exist at all.
        UtilityEvidence named = Brownout(world, city.Home);

        Assert.Equal(Power, named.Utility);
        Assert.Equal(Ratio.Zero, named.Delivered);
        Assert.Equal(0, Core.Evidence.Evidence
            .SupplyOfBuilding(world, world.Buildings.Rows.At(city.Home)).Shortfalls);

        foreach (int instance in world.BuildingRules.Walk(city.Home))
        {
            Assert.Equal(Blocking.Nothing, world.RuleInstances.Blocked[instance]);
        }

        Issue(city.Simulation, Command.Street(west, Tiles.Zero, east, Tiles.Zero, SubTiles.Zero));

        AfterSettling(city.Simulation, day: 2);

        UtilityEvidence restored = Drawn(world, city.Home);

        Assert.Equal(Ratio.One, restored.Delivered);
        Assert.Equal(lit.Asked, restored.Asked);
        Assert.Equal(lit.Received, restored.Received);
        Assert.Equal(0, Brownout(world, city.Home).Utility.Raw);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Utility drawn only by an <c>on_fail</c> link is reported, and loses its road with the rest.
    /// </summary>
    /// <remarks>
    /// <b>A link has no Rule Instance of its own</b> — <c>RuleEngine.Descend</c> evaluates and fires
    /// it against the head's instance — so a reader walking only the head's inputs would call this
    /// Building undrawn while it runs at nothing delivered. That is the failure this covers, in all
    /// three readers.
    /// </remarks>
    [Fact]
    public void A_utility_drawn_only_by_a_fallback_is_reported_and_follows_the_road()
    {
        City city = Build(ChainedRules(), lamp: true);
        World world = city.World;

        Assert.NotEqual(Rows.NoSlot, city.Lit);
        Assert.Equal(city.Burning, DistrictOf(world, city.Lit));

        // The premise, asserted so this stays a regression: the loader instances heads only, and
        // no head the lamp holds draws on a Pool itself. Reading heads alone finds no Utility here.
        foreach (int instance in world.BuildingRules.Walk(city.Lit))
        {
            foreach (Term term in world.Rules.Inputs(world.RuleInstances.Rule[instance]))
            {
                Assert.NotEqual(Scope.Pool, term.Bin.Scope);
            }
        }

        AfterSettling(city.Simulation, day: 0);

        UtilityEvidence lit = Drawn(world, city.Lit);

        Assert.Equal(Ratio.One, lit.Delivered);
        Assert.Equal(0, Brownout(world, city.Lit).Utility.Raw);

        Assert.True(Core.Evidence.Evidence.TryUtilityOfBuilding(
            world, world.Buildings.Rows.At(city.Lit), out UtilityEvidence washed));
        Assert.Equal(Power, washed.Utility);
        Assert.Equal(Ratio.One, washed.Delivered);

        int block = world.Rules.Roads.BlockTiles;
        int boundary = Boundary(world);
        var west = new Tiles(boundary * block);
        var east = new Tiles((boundary + 1) * block);

        Issue(city.Simulation, Command.Demolish(
            new Tiles(east.Raw - 1), Tiles.Zero, DemolishTarget.Street));

        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(Ratio.Zero, Drawn(world, city.Lit).Delivered);
        Assert.Equal(Power, Brownout(world, city.Lit).Utility);

        Assert.True(Core.Evidence.Evidence.TryUtilityOfBuilding(
            world, world.Buildings.Rows.At(city.Lit), out washed));
        Assert.Equal(Ratio.Zero, washed.Delivered);

        Issue(city.Simulation, Command.Street(west, Tiles.Zero, east, Tiles.Zero, SubTiles.Zero));

        AfterSettling(city.Simulation, day: 2);

        Assert.Equal(Ratio.One, Drawn(world, city.Lit).Delivered);
        Assert.Equal(0, Brownout(world, city.Lit).Utility.Raw);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A plant feeds the network and draws nothing, so it reports no Utility at all.
    /// </summary>
    /// <remarks>
    /// <b>The Building wash reads this.</b> A Building that draws no delivered Utility has no
    /// delivered fraction to colour it by and stays neutral, and an output term must not be mistaken
    /// for a draw.
    /// </remarks>
    [Fact]
    public void A_plant_draws_nothing_and_so_reports_nothing()
    {
        City city = Build();

        AfterSettling(city.Simulation, day: 0);

        BuildingEvidence plant = Core.Evidence.Evidence.OfBuilding(
            city.World, city.World.Buildings.Rows.At(city.Generator));

        Assert.Equal(0, plant.Utilities.Length);
        Assert.False(Core.Evidence.Evidence.TryUtilityOfBuilding(
            city.World, city.World.Buildings.Rows.At(city.Generator), out _));

        Assert.True(Core.Evidence.Evidence.TryUtilityOfBuilding(
            city.World, city.World.Buildings.Rows.At(city.Home), out UtilityEvidence drawn));
        Assert.Equal(Power, drawn.Utility);
        Assert.Equal(Ratio.One, drawn.Delivered);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// Writes the ridge as a city save the shell can open, which is the driven demonstration's
    /// world.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An instrument, because it writes a file rather than asserting a behaviour</b> — the
    /// behaviour is the three tests above. It exists so the committed demonstration world is
    /// reproducible from this fixture instead of being a binary nobody can rebuild.
    /// </para>
    /// <para>
    /// ⚠ <b>The city a generator makes cannot stand in for this one.</b> <c>SyntheticCity</c> raises
    /// kind 1 everywhere and nothing else, so a generated <c>powered.toml</c> city has consumers,
    /// no plant and no Day of delivery to watch. The ridge is the world the brownout is legible in.
    /// </para>
    /// <para>
    /// Set <c>BOROUGH_UTILITY_CITY</c> to the path to write; it goes to a temporary file otherwise.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Writes_the_ridge_city_for_the_driven_demonstration()
    {
        City city = Build();

        // Two Days: the first settle has run, so the save opens on a city that is already lit.
        AfterSettling(city.Simulation, day: 1);

        Assert.Equal(Ratio.One, Drawn(city.World, city.Home).Delivered);

        string path = Environment.GetEnvironmentVariable("BOROUGH_UTILITY_CITY")
            ?? Path.Combine(
                Path.GetTempPath(), $"borough-utility-ridge-{Environment.ProcessId}.borough");

        RulesetSourceResult loaded = RulesetSource.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "powered.toml"));

        Assert.NotNull(loaded.Capture);

        CitySave.Write(path, city.World, loaded.Capture!, seed: 7);

        SavedCity reopened = CitySave.Read(path);

        Assert.Equal(city.World.HashState(), reopened.World.HashState());
        Assert.Equal(Ratio.One, Drawn(reopened.World, city.Home).Delivered);

        // The Tiles a drive script has to click, written beside the save rather than copied into
        // one by hand. The watershed decides where the boundary falls, so the script is only
        // reproducible if the fixture states it.
        int block = city.World.Rules.Roads.BlockTiles;
        int boundary = Boundary(city.World);
        int homeLot = city.World.Lots.Rows.Resolve(city.World.Buildings.Lot[city.Home]);
        int plantLot = city.World.Lots.Rows.Resolve(city.World.Buildings.Lot[city.Generator]);

        File.WriteAllText(
            Path.ChangeExtension(path, ".geometry.txt"),
            $"""
             saved tick           {city.World.Tick.Raw}
             districts            {city.World.Districts.Rows.LiveCount}
             buildings            {city.World.Buildings.Rows.LiveCount}
             boundary cells       {boundary} | {boundary + 1}
             crossing demolish    east {((boundary + 1) * block) - 1} north 0
             crossing relay from  east {boundary * block} north 0
             crossing relay to    east {(boundary + 1) * block} north 0
             occupied dwelling    building {city.World.Buildings.Rows.IdAt(city.Home)} at east {city.World.Lots.East[homeLot].Raw} north {city.World.Lots.North[homeLot].Raw}
             plant                building {city.World.Buildings.Rows.IdAt(city.Generator)} at east {city.World.Lots.East[plantLot].Raw} north {city.World.Lots.North[plantLot].Raw}

             """);
    }

    /// <summary>The dump's one row for Power, which is where its numbers are.</summary>
    /// <remarks>The prose beneath the table names percentages too, so a whole-report substring
    /// assertion passes on the footnote.</remarks>
    private static string PowerRow(string printed)
    {
        foreach (string line in printed.Split('\n'))
        {
            if (line.StartsWith("  power", StringComparison.Ordinal))
            {
                return line;
            }
        }

        throw new InvalidOperationException($"the dump printed no `power` row:\n{printed}");
    }

    /// <summary>
    /// The headless dump prints the delivered fraction, and prints nothing for a Building that
    /// draws no Utility.
    /// </summary>
    /// <remarks>
    /// <b>The table is rendered here rather than through <c>--evidence</c></b>, because that mode
    /// refuses a Ruleset with no <c>[[zone_rule]]</c> and nothing may be condemned in
    /// <c>powered.toml</c> — the fixture exists so that one network can be watched in a city that
    /// stays built. What is asserted is the printer against a world that really is short.
    /// </remarks>
    [Fact]
    public void The_headless_dump_prints_what_the_network_delivers()
    {
        City city = Build();
        World world = city.World;
        RulesetNames names = Loaded().Names;

        AfterSettling(city.Simulation, day: 0);

        var lit = new StringWriter();

        EvidenceDump.Utilities(
            lit, names, Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(city.Home)));

        Assert.Contains("100%", PowerRow(lit.ToString()), StringComparison.Ordinal);

        var plant = new StringWriter();

        EvidenceDump.Utilities(
            plant,
            names,
            Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(city.Generator)));

        Assert.Equal(string.Empty, plant.ToString());

        int block = world.Rules.Roads.BlockTiles;
        var east = new Tiles((Boundary(world) + 1) * block);

        Issue(city.Simulation, Command.Demolish(
            new Tiles(east.Raw - 1), Tiles.Zero, DemolishTarget.Street));

        AfterSettling(city.Simulation, day: 1);

        var dark = new StringWriter();

        EvidenceDump.Utilities(
            dark, names, Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(city.Home)));

        Assert.Contains("0%", PowerRow(dark.ToString()), StringComparison.Ordinal);
        Assert.DoesNotContain("100%", PowerRow(dark.ToString()), StringComparison.Ordinal);

        city.Simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A Building standing in no District is served nothing, and the reading says so rather than
    /// reporting an absent network as full delivery.
    /// </summary>
    [Fact]
    public void Premises_in_no_district_report_nothing_delivered()
    {
        City city = Build();

        AfterSettling(city.Simulation, day: 0);

        // Far enough east to be outside every built Cell, so the watershed claims nothing here.
        int outside = Raise(city.World, Cells + 8, 0, Dwelling);

        city.World.RebuildParcels();
        city.World.RebuildDerived();

        Assert.Equal(Rows.NoSlot, DistrictOf(city.World, outside));

        UtilityEvidence drawn = Drawn(city.World, outside);

        Assert.Equal(Ratio.Zero, drawn.Delivered);
        Assert.Equal(0, drawn.Asked);
        Assert.Equal(Power, Brownout(city.World, outside).Utility);
    }
}
