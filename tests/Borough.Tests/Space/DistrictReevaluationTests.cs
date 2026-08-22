using Borough.Core;
using Borough.Core.Invariants;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 12 task 4: re-evaluation — persistence, hysteresis, damping and the cadence.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the task that earns a District its <c>Saved</c> disposition</b>, and these tests are
/// where that stops being an assertion in an ADR. All three stability mechanisms consult the
/// <em>previous</em> extent: a District keeps its row so that task 5's Pool Bins keep their owner, a
/// Cell stays where it was unless the field is decisive, and a boundary moves by a bounded number of
/// Cells per evaluation. A District rebuilt from a snapshot would have none of that.
/// </para>
/// <para>
/// <b>The fixtures are hand-built and the reason is that no shipped Ruleset can exercise any of it.</b>
/// The Building-density field is flat everywhere, so on <c>twinned.toml</c> the two basins never touch,
/// nothing is ever contested and the band is never read — the same measured fact that makes the
/// threshold unratifiable. ⚠ <b><see cref="Ridge"/> is therefore the only world in this repository in
/// which hysteresis does anything</b>, and it exists to make the mechanism testable rather than to
/// demonstrate a city.
/// </para>
/// <para>
/// ⚠ <b>What these tests do NOT do is ratify a value.</b> They run the band at 1, 50 and 100 and assert
/// that the *shape* is right at each — a tie never moves, a decisive Cell moves below its own margin and
/// not above it. ***That a mechanism behaves monotonically is not evidence that 50 is the number***;
/// <c>plans/0002</c> §D1 names milestone 15 for all three.
/// </para>
/// </remarks>
public sealed class DistrictReevaluationTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private const int Percent = 50;
    private const int Revisit = 2_048;
    private const int Band = 50;
    private const int Migrate = 16;

    /// <summary>Cells at each end of <see cref="Ridge"/> that carry the tall count.</summary>
    private const int PeakCells = 2;

    private static string Body(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    /// <summary>
    /// A priced <c>[[hinterland]]</c>, appended to every Ruleset these tests assemble.
    /// </summary>
    /// <remarks>
    /// <b>Milestone 12 task 6 made this mandatory rather than decorative.</b> A file that states
    /// <c>[districts]</c> and leaves a <c>good</c> unpriced at every Hinterland is refused at load —
    /// a District opens a Pool per Good and the Hinterland's price is the only ceiling on it, so an
    /// unpriced Good is free everywhere for ever (<c>adr/0050</c>, <c>adr/0135</c>). Nothing here is
    /// about prices; this is the fragment that lets the file load at all.
    /// </remarks>
    /// <remarks>
    /// ⚠ <b>The edge is <c>south</c> and the prices are HIGH on purpose.</b> These helpers are handed
    /// <c>twinned.toml</c> as well as <c>minimal.toml</c>, and that file already declares north and
    /// east — a second table for either edge is refused, and a cheaper one would move the ceiling it
    /// authors. ***A test fixture that has to be added to a shipped file must not change what the
    /// shipped file says.***
    /// </remarks>
    private const string PricedHinterland =
        "\n[[hinterland]]\nedge = \"south\"\nemigrant_balance_min = 0\n"
        + "emigrant_balance_max = 0\nprices = [ { resource = \"sundries\", price = 500 }, "
        + "{ resource = \"repairs\", price = 500 } ]\n";

    private static Ruleset Rules(
        string file,
        int percent = Percent,
        int revisit = Revisit,
        int band = Band,
        int migrate = Migrate)
    {
        string stripped = Strip(Body(file));

        RulesetLoadResult result = RulesetLoader.Parse(
            $"{stripped}{PricedHinterland}\n[districts]\n"
            + $"prominence_percent = {percent}\nrevisit_ticks = {revisit}\n"
            + $"hysteresis_percent = {band}\nmigrate_cells = {migrate}\n",
            file);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>A TOML body with any <c>[districts]</c> table removed.</summary>
    private static string Strip(string body)
    {
        List<string> kept = [];
        bool inside = false;

        foreach (string line in body.Split('\n'))
        {
            if (line.TrimStart().StartsWith('['))
            {
                inside = line.TrimStart().StartsWith("[districts]", StringComparison.Ordinal);
            }

            if (!inside)
            {
                kept.Add(line);
            }
        }

        return string.Join('\n', kept);
    }

    private static World Populated(Ruleset rules, int citizens)
    {
        var world = new World(citizens, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>Every live District's monotonic identity, as a set the test can compare across calls.</summary>
    private static List<Handle<District>> Standing(World world)
    {
        List<Handle<District>> standing = [];

        for (int slot = 0; slot < world.Districts.Rows.SlotCount; slot++)
        {
            if (world.Districts.Rows.IsLive(slot))
            {
                standing.Add(world.Districts.Rows.At(slot));
            }
        }

        return standing;
    }

    private static Handle<District> Of(World world, Cells east, Cells north) =>
        world.DistrictsInCells.Of(world.DistrictCells, east, north);

    /// <summary>Writes a Cell's District directly, which is how a test makes the field disagree.</summary>
    private static void Misfile(World world, Cells east, Cells north, Handle<District> district)
    {
        int slot = world.DistrictsInCells.Slot(east, north);

        Assert.NotEqual(DistrictResidency.NotResident, slot);

        world.DistrictCells.District[slot] = district;
    }

    // ---- persistence ------------------------------------------------------------------------------

    /// <summary>
    /// Re-evaluating an unchanged world changes nothing at all — not the rows, not the identities.
    /// </summary>
    /// <remarks>
    /// <b>The identities are the half that matters.</b> Row counts agreeing would be satisfied by a
    /// table cleared and rebuilt to the same shape, which is exactly what task 3 did and exactly what
    /// task 5's Pool Bins cannot survive. Comparing the handles is comparing the monotonic ids the
    /// State Hash folds.
    /// </remarks>
    [Fact]
    public void Re_evaluating_an_unchanged_world_keeps_every_district_row()
    {
        World world = Populated(Rules("twinned.toml"), 4_000);

        List<Handle<District>> before = Standing(world);
        int cells = world.DistrictCells.Rows.LiveCount;

        world.EvaluateDistricts();
        world.EvaluateDistricts();

        Assert.Equal(2, before.Count);
        Assert.Equal(before, Standing(world));
        Assert.Equal(cells, world.DistrictCells.Rows.LiveCount);
    }

    /// <summary>A District whose basin has gone is destroyed, and its neighbour keeps its own row.</summary>
    /// <remarks>
    /// 🔴 <b>At task 5 this becomes a real question and it is deliberately left open here.</b> A
    /// destroyed District will be holding Pool Bins, and destroying those destroys Goods and money,
    /// which <c>adr/0024</c> forbids. Today it holds nothing, so the merge path can be built and
    /// exercised before anything is at stake — which is the order that lets the transfer be designed
    /// rather than discovered.
    /// </remarks>
    [Fact]
    public void A_district_whose_centre_is_demolished_is_destroyed_and_its_neighbour_is_not()
    {
        World world = Populated(Rules("twinned.toml"), 4_000);

        List<Handle<District>> before = Standing(world);

        Assert.Equal(2, before.Count);

        // Everything east of the authored gap. The western lattice is left standing, so exactly one
        // basin survives and exactly one District should.
        int frontier = CellGrid.ToCells(new Tiles(world.Rules.Lattices.Max(l => l.OriginEastTiles))).Raw;
        Handle<District> west = Of(world, new Cells(0), new Cells(0));

        Demolish(world, east => CellGrid.ToCells(east).Raw >= frontier);

        world.EvaluateDistricts();

        Assert.Single(Standing(world));
        Assert.Equal(west, Standing(world)[0]);
        Assert.Contains(west, before);
    }

    /// <summary>Ground built on since the last evaluation joins a District without spending the bound.</summary>
    /// <remarks>
    /// <b>A Cell joining its FIRST District is growth and not migration</b>, so it is exempt from
    /// <c>migrate_cells</c>. Counting it would freeze a growing city's boundaries against a budget its
    /// own construction was spending, which is the opposite of what damping is for — and the fixture
    /// here sets the bound to <b>one</b> so that the exemption is the only thing that could produce the
    /// result.
    /// </remarks>
    [Fact]
    public void New_ground_joins_a_district_without_spending_the_migration_bound()
    {
        World world = Ridge(migrate: 1);

        int before = world.DistrictCells.Rows.LiveCount;

        // Two Cells past the ridge's east end, so they are new ground rather than contested ground.
        Raise(world, cell: PeakCells + Valleys + PeakCells, count: 3);
        Raise(world, cell: PeakCells + Valleys + PeakCells + 1, count: 3);

        world.EvaluateDistricts();

        Assert.Equal(before + 2, world.DistrictCells.Rows.LiveCount);
    }

    // ---- hysteresis -------------------------------------------------------------------------------

    /// <summary>
    /// A Cell both basins reach at the same level never changes District, at any band.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0134</c>'s <em>"never on a tie"</em>, and the tie here is exact rather than
    /// approximate.</b> Once two basins have met at a level, everything either of them gains below that
    /// level is reachable from both at the same level — so the watershed's answer for such a Cell is
    /// the order the scan happened to visit it in. ***A scan order is not a finding and must never be
    /// felt.***
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(Band)]
    [InlineData(100)]
    public void A_contested_cell_never_changes_district(int band)
    {
        World world = Ridge(band: band);

        Handle<District> east = Of(world, Cell(LastCell), new Cells(0));

        Misfile(world, Cell(PeakCells + 1), new Cells(0), east);

        world.EvaluateDistricts();

        Assert.Equal(east, Of(world, Cell(PeakCells + 1), new Cells(0)));
    }

    /// <summary>
    /// A Cell whose own basin reaches it decisively higher than the rival does IS corrected.
    /// </summary>
    /// <remarks>
    /// <b>The other half of the band, and without it the test above is satisfied by a mechanism that
    /// never moves anything.</b> The peak Cell here is reached by its own basin at the tall count and
    /// by the rival only at the valley, so its margin is real; at a band of 1 or 50 that clears, and at
    /// 100 it does not, because 100 is <em>the whole of the Cell's own height</em> and no margin can be
    /// larger than that.
    /// </remarks>
    [Theory]
    [InlineData(1, true)]
    [InlineData(Band, true)]
    [InlineData(100, false)]
    public void A_decisive_cell_is_corrected_and_only_below_its_own_margin(int band, bool moves)
    {
        World world = Ridge(band: band);

        Handle<District> west = Of(world, Cell(0), new Cells(0));
        Handle<District> east = Of(world, Cell(LastCell), new Cells(0));

        Assert.NotEqual(west, east);

        // Cell 1 is the western peak's second Cell -- tall, and NOT the centre, so misfiling it does
        // not disturb the identity rule.
        Misfile(world, Cell(1), new Cells(0), east);

        world.EvaluateDistricts();

        Assert.Equal(moves ? west : east, Of(world, Cell(1), new Cells(0)));
    }

    // ---- damping ----------------------------------------------------------------------------------

    /// <summary>At most <c>migrate_cells</c> Cells change District in one re-evaluation.</summary>
    /// <remarks>
    /// <b>The world is <c>twinned.toml</c> because its two basins never touch</b>, so every Cell in it
    /// is decisive and the band holds nothing back — leaving the bound as the only thing that can
    /// produce a number less than all of them. On a world with a contested boundary the two mechanisms
    /// would be indistinguishable from one another in the result.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void A_boundary_migrates_by_at_most_the_bound_per_evaluation(int migrate)
    {
        World world = Populated(Rules("twinned.toml", migrate: migrate), 4_000);

        List<Handle<District>> standing = Standing(world);
        Handle<District> west = standing[0];

        int misfiled = MisfileAllBut(world, standing[1], west);

        Assert.True(
            misfiled > migrate,
            $"the fixture misfiled {misfiled} Cells against a bound of {migrate}. If the bound is not "
            + "the binding constraint this test passes without it doing anything.");

        // Only the centre is left, by construction -- MisfileAllBut may not move it without breaking
        // the identity rule, which is a different mechanism and not this one.
        Assert.Equal(1, CountUnder(world, standing[1]));

        world.EvaluateDistricts();

        Assert.Equal(1 + migrate, CountUnder(world, standing[1]));
    }

    /// <summary>A boundary that has to move far gets there, one bounded step per evaluation.</summary>
    /// <remarks>
    /// <b>Damping is a delay and not a refusal</b> — <c>adr/0134</c>'s words are <em>"migrates rather
    /// than jumps"</em>. A bound that never converged would be a boundary permanently wrong, which is
    /// worse than one that jumps.
    /// </remarks>
    [Fact]
    public void A_damped_boundary_converges()
    {
        const int Step = 3;

        World world = Populated(Rules("twinned.toml", migrate: Step), 4_000);

        List<Handle<District>> standing = Standing(world);
        int misfiled = MisfileAllBut(world, standing[1], standing[0]);

        int evaluations = 0;
        int settled = CountUnder(world, standing[1]);

        while (evaluations < 100)
        {
            world.EvaluateDistricts();
            evaluations++;

            int now = CountUnder(world, standing[1]);

            if (now == settled)
            {
                break;
            }

            settled = now;
        }

        // One evaluation per bounded step, plus the one that finds nothing left to move.
        Assert.Equal(((misfiled + Step - 1) / Step) + 1, evaluations);
        Assert.Equal(1 + misfiled, settled);
        Assert.Equal(2, Standing(world).Count);
    }

    // ---- the cadence ------------------------------------------------------------------------------

    /// <summary>The cadence fires on its own period and never on Tick 0.</summary>
    /// <remarks>
    /// <b>Tick 0 is excluded because world creation has already evaluated.</b> Re-running on the first
    /// Step would be the same answer computed twice today, and at task 5 it would be a District
    /// destroyed and reopened before anything had used it.
    /// </remarks>
    [Fact]
    public void The_cadence_skips_tick_zero_and_fires_on_its_period()
    {
        DistrictRuleset rules = Rules("twinned.toml", revisit: 64).Districts;

        Assert.False(rules.RevisitsOn(new Ticks(0)));
        Assert.False(rules.RevisitsOn(new Ticks(63)));
        Assert.True(rules.RevisitsOn(new Ticks(64)));
        Assert.False(rules.RevisitsOn(new Ticks(65)));
        Assert.True(rules.RevisitsOn(new Ticks(128)));
    }

    /// <summary>A Ruleset with no <c>[districts]</c> never revisits.</summary>
    [Fact]
    public void A_ruleset_with_no_districts_never_revisits()
    {
        Assert.False(DistrictRuleset.None.RevisitsOn(new Ticks(2_048)));
    }

    /// <summary>Stepping the simulation past the period re-evaluates, and before it does not.</summary>
    /// <remarks>
    /// <b>The mis-filing is what makes the assertion mean something.</b> A world whose Districts are
    /// already correct is one where running the watershed and not running it look identical, so the
    /// test would pass on a Simulation that never called it at all.
    /// </remarks>
    [Fact]
    public void Stepping_past_the_period_re_evaluates_and_stepping_short_of_it_does_not()
    {
        const int Period = 64;

        World world = Populated(Rules("twinned.toml", revisit: Period), 4_000);
        var simulation = new Simulation(world, Key);

        List<Handle<District>> standing = Standing(world);
        (Cells east, Cells north) = SomeCellUnder(world, standing[1]);

        Misfile(world, east, north, standing[0]);

        // Period steps, which are Ticks 0 through Period-1: none of them is a multiple of the period
        // except Tick 0, and Tick 0 is the one the cadence excludes because world creation has already
        // evaluated. The step after this loop is the first that fires.
        for (int tick = 0; tick < Period; tick++)
        {
            simulation.Step(default);
        }

        Assert.Equal(standing[0], Of(world, east, north));

        simulation.Step(default);

        Assert.Equal(standing[1], Of(world, east, north));
    }

    // ---- the refusals -----------------------------------------------------------------------------

    /// <summary>Every key of a stated <c>[districts]</c> is required, and each end is refused.</summary>
    [Theory]
    [InlineData("prominence_percent = 50\nhysteresis_percent = 50\nmigrate_cells = 16", "revisit_ticks")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 64\nmigrate_cells = 16", "hysteresis_percent")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 64\nhysteresis_percent = 50", "migrate_cells")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 0\nhysteresis_percent = 50\nmigrate_cells = 16", "revisit_ticks")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 64\nhysteresis_percent = 0\nmigrate_cells = 16", "hysteresis_percent")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 64\nhysteresis_percent = 101\nmigrate_cells = 16", "hysteresis_percent")]
    [InlineData("prominence_percent = 50\nrevisit_ticks = 64\nhysteresis_percent = 50\nmigrate_cells = 0", "migrate_cells")]
    public void A_districts_table_states_every_key_and_each_ones_ends_are_refused(string table, string key)
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            $"{Strip(Body("minimal.toml"))}\n[districts]\n{table}\n", "test.toml");

        Assert.Null(result.Ruleset);
        Assert.Contains(key, result.Describe(), StringComparison.Ordinal);
    }

    /// <summary><c>twinned.toml</c> states all four keys, with the values this file tests against.</summary>
    [Fact]
    public void The_shipped_ruleset_states_the_four_values_this_file_restates()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Body("twinned.toml"), "twinned.toml");
        DistrictRuleset shipped = result.Ruleset!.Districts;

        Assert.Equal(Percent, shipped.ProminencePercent);
        Assert.Equal(Revisit, shipped.RevisitTicks);
        Assert.Equal(Band, shipped.HysteresisPercent);
        Assert.Equal(Migrate, shipped.MigrateCells);
    }

    // ---- the fixtures -----------------------------------------------------------------------------

    /// <summary>How many Cells sit between the two peaks in <see cref="Ridge"/>.</summary>
    private const int Valleys = 4;

    /// <summary>Buildings in a peak Cell.</summary>
    private const int Tall = 8;

    /// <summary>Buildings in a valley Cell.</summary>
    private const int Short = 3;

    /// <summary>The last Cell of the ridge.</summary>
    private const int LastCell = (PeakCells * 2) + Valleys - 1;

    private static Cells Cell(int index) => new(index);

    /// <summary>
    /// A single Street running east, with a tall Cell at each end and a low ridge between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The only world in this repository where hysteresis does anything</b>, and it is hand-built
    /// for that reason. Every shipped Ruleset produces a field that is flat inside a lattice and zero
    /// outside it, so its basins never touch and nothing is ever contested — measured, and it is what
    /// <c>twinned.toml</c>'s own header records.
    /// </para>
    /// <para>
    /// <b>The numbers are chosen so that both mechanisms are exercised by the same fixture.</b> Each
    /// peak stands <see cref="Tall"/> against a saddle of <see cref="Short"/>, so the lower peak's
    /// prominence clears the threshold and there are two Districts; and the valley Cells are reached by
    /// both basins at <see cref="Short"/> exactly, so they are ties. ⚠ <b>Two Cells per peak, not
    /// one</b>: the taller Cell of each pair becomes the centre, which leaves the other as a Cell that
    /// is decisive AND not a centre — the only kind the band can be tested on without disturbing the
    /// identity rule.
    /// </para>
    /// <para>
    /// ⚠ <b>Lots sit four Tiles in from the block corner and two apart.</b> A Lot on an intersection
    /// fronts nothing (an Address is never a Node), and a Cell whose Buildings all front nothing reports
    /// no road component at all — which merged two islands into one District the first time this file's
    /// sibling was written.
    /// </para>
    /// </remarks>
    private static World Ridge(int band = Band, int migrate = Migrate)
    {
        var world = new World(100, Rules("minimal.toml", band: band, migrate: migrate), Key);

        int block = world.Rules.Roads.BlockTiles;
        int cells = (PeakCells * 2) + Valleys;

        Handle<RoadNode> previous = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int step = 1; step <= cells; step++)
        {
            Handle<RoadNode> next = world.Roads.Nodes.Create(new Tiles(step * block), Tiles.Zero);

            world.Roads.Segments.Create(
                previous, next, new Tiles(block), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        world.Roads.RebuildDerived();

        for (int cell = 0; cell < cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= cells - PeakCells;

            Raise(world, cell, peak ? Tall : Short, rebuild: false);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        return world;
    }

    /// <summary>Puts a number of Buildings into one Cell of the ridge.</summary>
    private static void Raise(World world, int cell, int count, bool rebuild = true)
    {
        int block = world.Rules.Roads.BlockTiles;

        for (int i = 0; i < count; i++)
        {
            Handle<Lot> lot = world.Lots.Create(
                new Tiles((cell * block) + 4 + (i * 2)), Tiles.Zero, zone: 1);

            world.CreateBuilding(lot, kind: 0, Ticks.Zero, Key);
        }

        if (rebuild)
        {
            world.RebuildDerived();
        }
    }

    /// <summary>Demolishes every Building whose Lot's east Tile satisfies a predicate.</summary>
    private static void Demolish(World world, Func<Tiles, bool> doomed)
    {
        List<Handle<Building>> condemned = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot)
                && doomed(world.Lots.East[lot]))
            {
                condemned.Add(world.Buildings.Rows.At(slot));
            }
        }

        foreach (Handle<Building> building in condemned)
        {
            world.DestroyBuilding(building, Ticks.Zero);
        }
    }

    /// <summary>How many Cells a District holds.</summary>
    private static int CountUnder(World world, Handle<District> district)
    {
        int held = 0;

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (world.DistrictCells.Rows.IsLive(slot) && world.DistrictCells.District[slot] == district)
            {
                held++;
            }
        }

        return held;
    }

    /// <summary>Some Cell of a District that is not its centre.</summary>
    private static (Cells East, Cells North) SomeCellUnder(World world, Handle<District> district)
    {
        int seat = world.Districts.Rows.Resolve(district);

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (!world.DistrictCells.Rows.IsLive(slot)
                || world.DistrictCells.District[slot] != district
                || (world.DistrictCells.East[slot] == world.Districts.CentreEast[seat]
                    && world.DistrictCells.North[slot] == world.Districts.CentreNorth[seat]))
            {
                continue;
            }

            return (world.DistrictCells.East[slot], world.DistrictCells.North[slot]);
        }

        throw new InvalidOperationException("the District holds nothing but its centre.");
    }

    /// <summary>
    /// Hands every Cell of a District except its centre to another one, and says how many.
    /// </summary>
    /// <remarks>
    /// <b>The centre is left alone deliberately.</b> Identity travels through the centre Cell, so a
    /// misfiled centre would make the basin open a NEW District — which is the identity rule working
    /// and would hide whatever the test was actually asking about.
    /// </remarks>
    private static int MisfileAllBut(World world, Handle<District> from, Handle<District> into)
    {
        int seat = world.Districts.Rows.Resolve(from);
        int moved = 0;

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (!world.DistrictCells.Rows.IsLive(slot)
                || world.DistrictCells.District[slot] != from
                || (world.DistrictCells.East[slot] == world.Districts.CentreEast[seat]
                    && world.DistrictCells.North[slot] == world.Districts.CentreNorth[seat]))
            {
                continue;
            }

            world.DistrictCells.District[slot] = into;
            moved++;
        }

        return moved;
    }

    // ---- the invariant ----------------------------------------------------------------------------

    /// <summary>A membership row left naming a destroyed District is reported.</summary>
    /// <remarks>
    /// 🔴 <b>The State Hash structurally cannot report this, which is the reason the invariant exists.</b>
    /// A handle column folds the target row's monotonic id, and a handle whose target has been freed
    /// folds as <b>zero</b> — so replay, thread-count and save/reload equivalence would all agree about
    /// a dangling District. ***Two runs reproduce the same wrong answer.***
    /// </remarks>
    [Fact]
    public void A_membership_row_naming_a_destroyed_district_is_reported()
    {
        World world = Populated(Rules("twinned.toml"), 4_000);

        world.Invariants.Collect = true;
        WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation =>
                violation.Invariant == Invariant.ADistrictCellNamesALiveDistrict);

        // Freed behind the membership's back, which is exactly what a reconciliation that released
        // things in the wrong order would leave.
        world.Districts.Rows.Free(Standing(world)[1]);

        WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround(world, world.Invariants);

        Assert.Contains(
            world.Invariants.Collected,
            violation =>
                violation.Invariant == Invariant.ADistrictCellNamesALiveDistrict);
    }

    /// <summary>
    /// A demolition leaves the extent naming unbuilt ground, and that is the cadence rather than a
    /// violation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS TEST USED TO ASSERT THE OPPOSITE, and it was wrong.</b> <c>plans/0003</c> queue item
    /// 16: a three-Day headless run of <c>rulesets/twinned.toml</c> panicked on the end-of-run walk,
    /// because the extent is derived on <c>[districts] revisit_ticks</c> and ***between two evaluations
    /// it describes the city as of the last one.*** Measured: a Cell demolished at Tick 1,152 keeps its
    /// membership until Tick 2,048, and the eviction then clears it. **The mechanism was right and the
    /// sentence describing it was too strong.**
    /// </para>
    /// <para>
    /// ⚠ <b>The tempting repair — evict at the demolition site — was refused, and the reason is
    /// symmetry.</b> A Cell that <em>gains</em> its first Building also waits for the cadence to join a
    /// District, so making removal instant while addition stays cadenced is an asymmetry with nothing
    /// behind it. ***A structure derived on a cadence is stale between evaluations, and that is what a
    /// cadence IS.***
    /// </para>
    /// <para>
    /// <b>The property that was removed still holds where it is true</b> — see
    /// <see cref="Re_evaluating_clears_the_membership_a_demolition_stranded"/>, which is the same
    /// fixture run one evaluation further on.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_demolition_leaves_the_extent_stale_and_that_is_not_a_violation()
    {
        World world = Ridge();

        world.Invariants.Collect = true;
        WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround(world, world.Invariants);

        Assert.Empty(world.Invariants.Collected);

        int block = world.Rules.Roads.BlockTiles;

        Demolish(world, east => east.Raw >= (LastCell * block));

        WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation =>
                violation.Invariant == Invariant.ADistrictCellNamesALiveDistrict);
    }

    /// <summary>The evaluation's post-condition fires when the extent names unbuilt ground.</summary>
    /// <remarks>
    /// <b>Every diagnostic ships with a test that writes the violation and watches it fire</b>, and
    /// this one has to be written by hand: the eviction pass frees anything filed over unbuilt ground
    /// before the check runs, so the violation cannot be reached through
    /// <c>World.EvaluateDistricts</c>. ***A post-condition that is unreachable from outside its own
    /// pass is a post-condition that has to be called directly***, which is why it lives on
    /// <c>WorldInvariants</c> rather than inside <c>DistrictWatershed</c>.
    /// </remarks>
    [Fact]
    public void The_evaluations_post_condition_reports_a_membership_row_over_unbuilt_ground()
    {
        World world = Ridge();

        world.Invariants.Collect = true;
        WorldInvariants.DistrictExtentIsBuiltGround(world, world.Invariants);

        Assert.Empty(world.Invariants.Collected);

        int block = world.Rules.Roads.BlockTiles;

        Demolish(world, east => east.Raw >= (LastCell * block));

        WorldInvariants.DistrictExtentIsBuiltGround(world, world.Invariants);

        Assert.Contains(
            world.Invariants.Collected,
            violation =>
                violation.Invariant == Invariant.ADistrictCellNamesBuiltGroundWhenEvaluated);
    }

    /// <summary>A re-evaluation clears the stale membership a demolition left.</summary>
    /// <remarks>
    /// <b>The claim is about the reconciliation, so it has to be run on both sides of one.</b> A
    /// staleness no mechanism ever repairs would be a leak rather than a cadence, and this is what
    /// tells the two apart. ⚠ <b><c>EvaluateDistricts</c> asserts the post-condition internally</b>, so
    /// this test would throw before reaching its assertion if the eviction were ever dropped.
    /// </remarks>
    [Fact]
    public void Re_evaluating_clears_the_membership_a_demolition_stranded()
    {
        World world = Ridge();

        int block = world.Rules.Roads.BlockTiles;

        Demolish(world, east => east.Raw >= (LastCell * block));

        world.EvaluateDistricts();

        world.Invariants.Collect = true;
        WorldInvariants.DistrictMembershipNamesLiveDistrictsAndBuiltGround(world, world.Invariants);

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation =>
                violation.Invariant == Invariant.ADistrictCellNamesALiveDistrict);

        WorldInvariants.DistrictExtentIsBuiltGround(world, world.Invariants);

        Assert.Empty(world.Invariants.Collected);
    }

    /// <summary>
    /// The only shipped world with Districts in it survives three Days of its own cadence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE REGRESSION TEST FOR <c>plans/0003</c> QUEUE ITEM 16, and the finding behind that item
    /// is that nothing in this repository did this.</b> Every other test in this file builds its world
    /// in code and evaluates once or twice by hand; no golden trace and no long run uses a Ruleset that
    /// states <c>[districts]</c>. ***The only shipped world with Districts in it had never been run for
    /// two Days***, and one headless invocation found what four tasks of tests did not.
    /// </para>
    /// <para>
    /// <b>Three Days rather than two, because two only reaches the first re-evaluation.</b> The defect
    /// appeared between the first and the second — a Building demolished at Tick 1,152 against a
    /// cadence of 2,048 — so a run that stopped at 2,048 was clean and said nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>It asserts no number and that is deliberate.</b> What it is for is that the Tick loop
    /// completes: <c>Simulation.Step</c> runs the end-of-run invariants through
    /// <c>CheckEndOfRun</c> and the evaluation runs its post-condition internally, so both throw rather
    /// than return a value this test would have to compare. ***A test whose assertion is that nothing
    /// threw is the right shape when the thing under test is a panic.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_shipped_district_world_survives_three_days_of_its_own_cadence()
    {
        World world = Populated(Rules("twinned.toml"), 2_000);

        var simulation = new Simulation(world, Key);

        for (int tick = 0; tick < 3 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        world.Invariants.RunEndOfRun(world);

        Assert.Equal((ulong)(3 * Ticks.PerDay), world.Tick.Raw);
    }
}
