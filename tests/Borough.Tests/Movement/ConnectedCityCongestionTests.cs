using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// <b>A city whose every Street was laid by <c>CommandKind.Connect</c>, built to put the volume-delay
/// function somewhere it is not flat.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the ratifier <c>adr/0052</c>'s 2026-08-15 amendment asks for, and the amendment is why it
/// exists.</b> <c>[traffic]</c>'s α, β and clamp and <c>[households] car_ownership_percent</c> were
/// recorded with <em>5c task 8's long run</em> as their named ratifier — an owner, a date, a
/// purpose-built Ruleset and refuting readings in both directions. It ran and could not fire: load came
/// out at <b>0.0018 / 0.0048 / 0.0110</b> Vehicles per Segment per Tick at 4,000 / 16,000 / 64,000
/// Citizens, so BPR was only ever evaluated where it is nearly flat. <b>The ratifier named a
/// <em>machine</em> and not a <em>world</em>.</b> This file is the world.
/// </para>
/// <para>
/// <b>Congestion is a property of a network's <em>shape</em>, and a Ruleset can scale a lattice without
/// bending one.</b> <c>adr/0090</c> gives the generator land and the player every road, and
/// <c>CommandKind.Populate</c> sizes the paved extent from the population it serves — so the same
/// number sizes both the demand and the supply and <c>v/c</c> peaks at 0.44 whatever the population.
/// The only thing in this build that produces shape is the player's verb, so the fixture uses it.
/// </para>
/// <para>
/// <b>The shape is a dumbbell: two districts of blocks joined by one Street corridor.</b> Everybody in
/// one district who takes a job in the other has exactly one way across, which is the arrangement a
/// generated lattice structurally cannot produce — every block of it has four faces, so every journey
/// has as many routes as it has turns. One corridor is the smallest thing that is a <em>bottleneck</em>
/// rather than a thin patch.
/// </para>
/// <para>
/// ⚠ <b><c>CommandKind.Populate</c> and <c>CommandKind.Connect</c> are mutually exclusive at world
/// creation, and finding that out is what shaped this fixture.</b>
/// <see cref="RoadGenerator.LayInto"/> <em>throws</em> on a world that already has Segments — it is a
/// world-creation pass and says so — and <see cref="SyntheticCity.PopulateInto"/> calls it
/// unconditionally, before it builds anything. So a Connect-laid city cannot be populated by the
/// populator in either order, and this file grows its own population in <see cref="Populate"/>,
/// mirroring that method's three loops. <b>That is a real gap rather than an inconvenience</b>: there
/// is no door in the build through which a player-shaped network gets a population, because the only
/// populator is welded to the only generator. Filed rather than worked around silently
/// (<c>adr/0073</c>) — the workaround is here, and the finding is in <c>plans/0002</c> §C.
/// </para>
/// <para>
/// <b>The population is created directly rather than through the Input Log, so this fixture is a
/// measurement and not a session.</b> <c>TrafficDump.Step</c> has the same shape for the same reason.
/// What is asserted here is a property of a <em>world</em>; replay equivalence is asserted by
/// <c>ReplayTests</c>, which lays its own Streets by Connect and is the reason this one can.
/// </para>
/// </remarks>
public sealed class ConnectedCityCongestionTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong Seed = 0xC0FFEE_0000_0001UL;

    private static readonly WorldKey Key = WorldKey.FromSeed(Seed);

    /// <summary>
    /// <c>SyntheticCity</c>'s <c>DwellingKind</c>, which is <c>private</c> there.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A second copy of a constant, and the honest note is that it is one.</b> Kind 1 is the only
    /// <c>[[building]]</c> every shipped Ruleset declares and the populator hard-codes it for that
    /// reason; this fixture cannot read it because the field is private, and widening it would move a
    /// decision into the core to suit a caller. <c>plans/0012</c> Cause 1 is the risk; what bounds it is
    /// that a Ruleset with no kind 1 fails <see cref="Populate"/>'s own assertion rather than quietly
    /// building nothing.
    /// </remarks>
    private const byte DwellingKind = 1;

    /// <summary>
    /// Blocks of empty ground between the two districts, spanned by a single row of Street.
    /// </summary>
    /// <remarks>
    /// <b>Long enough that the corridor is the journey rather than a detail of it</b>, and short enough
    /// that crossing it stays inside the Commute Budget — a corridor nobody can afford to cross is a
    /// city with no commuters in it, which measures the Budget instead of the road.
    /// </remarks>
    private const int CorridorBlocks = 8;

    /// <summary>Two Days, which is two whole commute cycles after employment has settled.</summary>
    private const int TickCount = 2 * Ticks.PerDay;

    /// <summary>
    /// <c>congested.toml</c>'s <c>block_tiles</c>, as a literal.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A literal because the commands are built before a <c>World</c> exists</b> — the lattice is
    /// the first thing this fixture does and there is nothing to read a Ruleset off yet.
    /// <see cref="The_assumed_block_size_is_the_shipped_one"/> holds it to the file, which is what keeps
    /// it from being a second copy that drifts (<c>plans/0012</c> Cause 1): if the shipped block size
    /// ever moves, that assertion fails rather than this fixture quietly laying its dumbbell on the
    /// wrong grid.
    /// </remarks>
    private const int BlockTiles = 32;

    /// <summary>
    /// How big each half of the dumbbell is, and how many people live on it.
    /// </summary>
    /// <remarks>
    /// <b>Population scales with the square of the district so density is held fixed</b>, which is what
    /// makes <see cref="The_volume_delay_function_is_refutable_on_a_player_laid_network"/>'s ladder a
    /// statement about the <em>corridor</em> rather than about crowding: every rung has the same people
    /// per block and the same one Segment between them.
    /// </remarks>
    private readonly record struct Plan(int DistrictBlocks)
    {
        /// <summary>The first block column of the far district.</summary>
        public int FarColumn => DistrictBlocks + CorridorBlocks;

        public int Population => 4_000 * DistrictBlocks * DistrictBlocks / 16;
    }

    /// <summary>Sixteen blocks a side. Enough to load a corridor and cheap to run.</summary>
    private static Plan Small => new(4);

    // ---- the two claims --------------------------------------------------------------------------

    /// <summary>
    /// <b>A player-laid bottleneck reaches a load the generated lattice never does.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assertion is a <b>comparison</b> and not a level, for the reason every threshold in this
    /// corpus is one: a peak <c>v/c</c> means nothing alone, because it can be manufactured by lowering
    /// the capacity until it appears — which is exactly what <c>rulesets/congested.toml</c> does, and
    /// says so in its own header. What is structural is that the same Ruleset, the same capacity and
    /// the same population give a flat curve on a generated lattice and a loaded one here. <b>The only
    /// variable is who laid the road.</b>
    /// </para>
    /// <para>
    /// ⚠ <b>Run at the shipped capacity rather than at <c>congested.toml</c>'s demonstration rung.</b>
    /// 400 Vehicles an hour puts <b>1.02</b> Vehicles on a Segment where the shipped 3,600 puts 9.2, so
    /// a reading taken at 400 cannot distinguish <em>the player under-built the road</em> from <em>the
    /// file made the road absurd</em>. The point of a Connect-laid world is that it needs neither.
    /// </para>
    /// <para>
    /// <b>Measured 2026-08-15: 65.1% against 43.4%</b>, on 4,000 Citizens over two Days. The control's
    /// figure is <c>adr/0099</c>'s own 0.44 arriving by a third route, which is worth more than the
    /// comparison — it was measured at 4,000, 16,000 and 64,000 Citizens there and it comes back here
    /// on a differently-built world.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_corridor_carries_a_load_a_generated_lattice_never_reaches()
    {
        Reading corridor = Run(Small, Connected(), connected: true, "connect-laid dumbbell");
        Reading lattice = Run(Small, Connected(), connected: false, "generated lattice");

        Assert.True(corridor.VehicleTicks > 0, "nobody drove at all, so there is nothing to compare.");
        Assert.True(lattice.VehicleTicks > 0, "the control had no traffic, so it is not a control.");

        Assert.True(
            corridor.PeakLoad > lattice.PeakLoad,
            $"the corridor peaked at v/c {Percent(corridor.PeakLoad)} against the generated lattice's "
            + $"{Percent(lattice.PeakLoad)}, so laying the road badly did not load it -- either the "
            + "corridor is not a bottleneck or nobody is crossing it.");
    }

    /// <summary>
    /// <b>The volume-delay function changes what this city does, so its parameters are refutable.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the assertion the ratifier turns on, and it is the one task 8 could not make.</b>
    /// <c>SegmentVolumeTests.A_generated_city_is_never_busy_enough_to_slow_itself_down</c> is its
    /// control: on a generated city the loaded and free-flow runs come out <b>byte-identical</b> at the
    /// shipped capacity, because a peak <c>v/c</c> of 0.44 costs ×1.0054 and the sub-Tick carry absorbs
    /// it. ***A number that cannot change an outcome cannot be refuted by an outcome.*** Here the same
    /// Ruleset priced with and without <c>[traffic]</c> must disagree — and it does.
    /// </para>
    /// <para>
    /// <b>The ladder is printed rather than reduced to one rung, because the onset is the finding.</b>
    /// Measured 2026-08-15 at the shipped 3,600 Vehicles an hour, two Days, density held fixed:
    /// </para>
    /// <code>
    /// district   pop     employed   peak v/c loaded   free-flow   vehicle-Ticks loaded / free
    ///    4x4    4,000      1,185           65.1%         65.1%          6,784 /   6,784
    ///    6x6    9,000      2,507           97.7%         97.7%         19,475 /  19,472
    ///    8x8   16,000      4,470        1,074.3%        130.2%         44,853 /  43,703
    ///  10x10   25,000      7,022        2,767.0%        173.6%        122,725 /  80,978
    /// </code>
    /// <para>
    /// ⚠ <b>The onset is at <c>v/c</c> = 1 and the behaviour past it is a runaway, which no static
    /// reading of BPR predicts.</b> At 8×8 the free-flow world peaks at 130% and the priced one at
    /// <b>1,074%</b> — the function is not merely visible, it is <em>feeding itself</em>: congestion
    /// slows a Vehicle, a slower Vehicle dwells on its Segment longer, longer dwell is higher volume,
    /// and higher volume is more congestion. ***The volume-delay function is a loop and not a
    /// formula***, and the corpus has only ever quoted it as the second. That is what
    /// <c>03 §3.2</c> means by using it *only where it is strong* — and it is the argument for the
    /// Microscopic tier arriving from a direction nobody took it from.
    /// </para>
    /// <para>
    /// ⚠ <b>Both ends are asserted and the bottom rung is not decoration.</b> A fixture that only
    /// showed the function biting would be satisfied by one that always bites, which is a broken
    /// control rather than a loaded city — and the flat rung is what says this world is the shipped
    /// world until the corridor is actually short of road.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_volume_delay_function_is_refutable_on_a_player_laid_network()
    {
        Ruleset priced = Connected();
        Ruleset free = ConnectedWithoutTraffic();

        (long Loaded, long Free, Ratio Peak) At(Plan plan)
        {
            Reading loaded = Run(plan, priced, connected: true, $"{Name(plan)} loaded");
            Reading control = Run(plan, free, connected: true, $"{Name(plan)} free-flow");

            return (loaded.VehicleTicks, control.VehicleTicks, loaded.PeakLoad);
        }

        (long Loaded, long Free, Ratio Peak) flat = At(Small);
        (long Loaded, long Free, Ratio Peak) loadedUp = At(new Plan(8));

        Assert.True(flat.Loaded > 0 && loadedUp.Loaded > 0, "nobody drove, so nothing was measured.");

        // The bottom rung: the shipped city, where the function is real and costs nothing anybody can
        // see. Equality rather than "close", because a sub-Tick delay is absorbed exactly.
        Assert.Equal(flat.Free, flat.Loaded);

        // The top rung, and the whole point of the fixture. Vehicle-Ticks rather than a delay column,
        // because a delay column is what the function COMPUTES and this has to be a reading of what the
        // city DID: a journey priced dearer occupies its Segments for longer.
        Assert.True(
            loadedUp.Loaded > loadedUp.Free,
            $"at {Name(new Plan(8))} the loaded run held {loadedUp.Loaded} Vehicle-Ticks against the "
            + $"free-flow control's {loadedUp.Free}. The two runs are the same city priced with and "
            + "without the volume-delay function, so equal totals mean the function is decorative here "
            + "too and this fixture has not moved the ratifier on.");

        // ⚠ And the load is past the knee rather than merely above the control's. v/c = 1 is where BPR
        // stops being a rounding error, so a fixture that peaked at 0.9 would satisfy the assertion
        // above by a margin the sub-Tick carry could swallow on a different seed.
        Assert.True(
            loadedUp.Peak > Ratio.One,
            $"the corridor peaked at v/c {Percent(loadedUp.Peak)}, which is below capacity -- the "
            + "function is being evaluated on the flat part of its own curve, which is the condition "
            + "that made 5c task 8 unable to ratify these numbers in the first place.");
    }

    /// <summary>The block size this fixture assumes is the block size the Ruleset states.</summary>
    [Fact]
    public void The_assumed_block_size_is_the_shipped_one() =>
        Assert.Equal(BlockTiles, Connected().Roads.BlockTiles);

    // ---- the world -------------------------------------------------------------------------------

    private Reading Run(Plan plan, Ruleset rules, bool connected, string what)
    {
        Simulation simulation = Start(plan, rules, connected);
        World world = simulation.World;
        RoadSegmentTable segments = world.Roads.Segments;

        long vehicleTicks = 0;
        var peak = Ratio.Zero;

        for (int tick = 1; tick <= TickCount; tick++)
        {
            simulation.Step(new TickInput(default, rulesetHash: 0));

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (!segments.Rows.IsLive(slot))
                {
                    continue;
                }

                int forward = segments.VolumeForward[slot];
                int backward = segments.VolumeBackward[slot];

                vehicleTicks += forward + backward;

                // The busier direction, never the sum: capacity is stated per direction, so adding
                // them would price a Segment against half its own road. TrafficDump.Step's rule.
                int busier = forward > backward ? forward : backward;
                Ratio load = segments.LoadOf(slot, busier, segments.FreeFlowOver(slot));

                peak = load > peak ? load : peak;
            }
        }

        _output.WriteLine(
            $"{what,-22} pop {plan.Population,6}  segments {Live(segments),4}  "
            + $"vehicle-Ticks {vehicleTicks,7}  peak v/c {Percent(peak),9}  "
            + $"employed {Employed(world),5}");

        return new Reading(vehicleTicks, peak);
    }

    /// <summary>What one run of <see cref="Run"/> is worth reading off it.</summary>
    private readonly record struct Reading(long VehicleTicks, Ratio PeakLoad);

    /// <summary>
    /// A world under <paramref name="rules"/>, its Streets laid by the player or by the generator.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Which one is a parameter because inferring it was this fixture's first defect.</b> The
    /// branch was originally <c>rules.Roads.Runs</c> — read as <em>a Ruleset stating no <c>[roads]</c>
    /// cannot be Connected into, so it must be the generated case</em> — and both runs of the
    /// comparison went down the Connect path, producing two identical readings and an assertion that
    /// failed with the same number on both sides. ***A control is stated by the caller or it is not a
    /// control***: the two worlds differ in exactly one thing and the thing is not a property of the
    /// Ruleset, so nothing in the Ruleset could ever have selected it.
    /// </remarks>
    private static Simulation Start(Plan plan, Ruleset rules, bool connected)
    {
        InputLogBuilder builder = new(
            Seed, new WorldConfiguration(plan.Population), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        // O(world) correctness guard off: this is a measurement over thousands of Ticks and the guard
        // is 95% of such a run (S0a).
        simulation.VerifyDecideWritesNothing = false;

        if (!connected)
        {
            simulation.Step(new TickInput(
                [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

            return simulation;
        }

        // A Tick apart, because a Zone carves against the faces that are standing.
        // LotSubdivider.SubdivideAt reads the StreetGrid, so zoning a block with no Street on it
        // yields nothing -- and since Relot reads a block's Zone off the Lots that survived on it, a
        // block that never had a Lot has nothing to remember it was zoned by.
        simulation.Step(new TickInput(Streets(plan), rulesetHash: 0));
        simulation.Step(new TickInput(Zoning(plan), rulesetHash: 0));

        Populate(simulation.World, plan);

        return simulation;
    }

    /// <summary>
    /// The dumbbell, as one Connect command per Street Segment.
    /// </summary>
    /// <remarks>
    /// <b>Every edge is named by its lower endpoint and an axis</b>, which is <c>StreetAxis</c>'s whole
    /// shape — there is no South and no West, so a block's four faces are two East edges a row apart
    /// and two North edges a column apart, and a grid of them is laid without ever naming an edge
    /// twice.
    /// </remarks>
    private static Command[] Streets(Plan plan)
    {
        List<Command> commands = [];

        Grid(commands, plan, 0);
        Grid(commands, plan, plan.FarColumn);

        // The corridor: one row of Street across the gap, and the only thing joining the two.
        for (int column = plan.DistrictBlocks; column < plan.FarColumn; column++)
        {
            commands.Add(Lay(column, 0, StreetAxis.East));
        }

        return [.. commands];
    }

    /// <summary>Every face of a district-square block grid whose first column is <paramref name="at"/>.</summary>
    private static void Grid(List<Command> into, Plan plan, int at)
    {
        for (int row = 0; row <= plan.DistrictBlocks; row++)
        {
            for (int column = at; column < at + plan.DistrictBlocks; column++)
            {
                into.Add(Lay(column, row, StreetAxis.East));
            }
        }

        for (int column = at; column <= at + plan.DistrictBlocks; column++)
        {
            for (int row = 0; row < plan.DistrictBlocks; row++)
            {
                into.Add(Lay(column, row, StreetAxis.North));
            }
        }
    }

    /// <summary>One <c>Zone</c> per block of both districts.</summary>
    private static Command[] Zoning(Plan plan)
    {
        List<Command> commands = [];

        foreach (int at in (int[])[0, plan.FarColumn])
        {
            for (int column = at; column < at + plan.DistrictBlocks; column++)
            {
                for (int row = 0; row < plan.DistrictBlocks; row++)
                {
                    commands.Add(new Command(
                        CommandKind.Zone, Middle(column), Middle(row), zone: 1));
                }
            }
        }

        return [.. commands];
    }

    /// <summary>
    /// Buildings, Households and Citizens on whatever Lots the zoning produced.
    /// </summary>
    /// <remarks>
    /// <b><see cref="SyntheticCity.PopulateInto"/>'s three loops with its road pass removed</b>, which
    /// is the one thing that method will not let a caller do. The ratios are its ratios — 360
    /// Households per 1,000 Citizens (S4 task 2), a Building per <c>occupants</c> Households — so this
    /// city differs from a generated one in its <em>road network</em> and in nothing else, which is
    /// what makes the comparison above mean what it says.
    /// </remarks>
    private static void Populate(World world, Plan plan)
    {
        int households = plan.Population * 360 / 1_000;
        int buildings = (households
            / (world.TryDeclaredOccupancy(DwellingKind, out int occupancy) && occupancy > 0
                ? occupancy
                : 1)) + 1;
        int lots = world.Lots.Rows.LiveCount;

        Assert.True(lots > 0, "zoning produced no Lots, so the Streets carried no frontage.");

        // The populator's own clamp, and it is what makes the ladder a statement about the corridor:
        // a district holds the Buildings its land can front, so adding people past that point adds
        // nobody to the road. It binds on every rung here by construction.
        if (lots < buildings)
        {
            buildings = lots;
        }

        for (int i = 0; i < buildings; i++)
        {
            world.CreateBuilding(world.Lots.Rows.At(i), DwellingKind, Ticks.Zero, Key);
        }

        for (int i = 0; i < households; i++)
        {
            world.CreateHousehold(world.Buildings.Rows.At(i % buildings), lifeStage: (byte)(i % 5));
        }

        for (int i = 0; i < plan.Population; i++)
        {
            world.CreateCitizen(
                world.Households.Rows.At(i % households), new Ticks((ulong)i % Ticks.PerDay));
        }
    }

    // ---- Rulesets --------------------------------------------------------------------------------

    /// <summary>
    /// <c>congested.toml</c>'s tables at the <b>shipped</b> Street capacity.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The capacity goes back to 3,600 and that is the point of the fixture.</b>
    /// <c>congested.toml</c> exists because a generated city cannot congest at any authored capacity,
    /// so it cuts the road to 400 Vehicles an hour — a Segment holding <b>1.02</b> Vehicles — and its
    /// own header says that makes it a demonstration rather than a city. A Connect-laid world should
    /// not need that, and a reading taken at 400 could not tell the two causes apart.
    /// </remarks>
    private static Ruleset Connected() => Parse(Shipped());

    /// <summary>The same Ruleset with the volume-delay function taken out — the free-flow control.</summary>
    /// <remarks>
    /// Stripped by <b>text</b> and re-parsed, which is <c>TrafficDump.TryFreeFlow</c>'s method and is
    /// chosen over building a <c>Ruleset</c> in C# for <c>GoldenFixtures.Rules</c>' reason: a Ruleset
    /// assembled in code agrees with the loader by construction, so a control built that way is not the
    /// same file with one table removed.
    /// </remarks>
    private static Ruleset ConnectedWithoutTraffic()
    {
        string toml = Shipped();

        // ⚠ The TABLE, not the first mention of the string, and getting that wrong was this fixture's
        // second defect. `[traffic]` appears in a comment 53 lines above the table it names, so
        // IndexOf cut the file mid-sentence and took `[households]` with it -- and a Ruleset with no
        // `[households]` is one in which NOBODY OWNS A CAR. The control came back with 0 vehicle-Ticks
        // against the treatment's 6,784 and the assertion PASSED, reading as `the volume-delay
        // function does everything` when it meant `the control had no cars in it`.
        //
        // A CONTROL THAT DIFFERS IN TWO VARIABLES MEASURES NEITHER, and the tell was there to be read:
        // a free-flow run is the same journeys priced cheaper, so it can hold FEWER Vehicle-Ticks and
        // it cannot hold NONE. A zero on one side of a ratio is never a small number.
        int at = toml.IndexOf("\n[traffic]\n", StringComparison.Ordinal);

        Assert.True(at >= 0, "congested.toml states no [traffic] table, so there is nothing to strip.");

        string stripped = toml[..at];

        // The guard that defect earns, stated positively: what must SURVIVE the strip. Asserting that
        // `[traffic]` is gone would have passed on the broken version too, because it was.
        Assert.Contains("\n[households]\n", stripped, StringComparison.Ordinal);

        return Parse(stripped);
    }

    private static string Shipped() => File
        .ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "congested.toml"))
        .Replace(
            "street_capacity_per_hour       = 400",
            "street_capacity_per_hour       = 3600",
            StringComparison.Ordinal);

    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    // ---- small change ----------------------------------------------------------------------------

    private static Command Lay(int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * BlockTiles),
        new Tiles(row * BlockTiles),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    /// <summary>A Tile in the middle of a block, which is what the Zone verb is addressed by.</summary>
    private static Tiles Middle(int block) => new((block * BlockTiles) + (BlockTiles / 2));

    private static string Name(Plan plan) => $"{plan.DistrictBlocks}x{plan.DistrictBlocks}";

    private static int Live(RoadSegmentTable segments)
    {
        int total = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            total += segments.Rows.IsLive(slot) ? 1 : 0;
        }

        return total;
    }

    private static int Employed(World world)
    {
        int total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>A <see cref="Ratio"/> as a percentage, for something a person reads.</summary>
    private static string Percent(Ratio value) => $"{value.Raw * 100.0 / 65_536.0:F1}%";
}
