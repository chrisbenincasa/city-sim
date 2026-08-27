using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 12 task 3: the Districts, found by a persistence-seeded watershed over Building density.
/// </summary>
/// <remarks>
/// <para>
/// <b>What these tests can and cannot establish, stated first because the difference is the whole
/// shape of the file.</b> They establish that the <em>operator</em> is right: that two concentrations
/// separated by empty ground come out as two Districts, that every built Cell lands in exactly one,
/// that no unbuilt Cell lands in any, and that a road-component boundary splits a field that would
/// otherwise merge. ⚠ <b>They establish nothing whatever about the threshold's value.</b> The
/// Building-density field is flat on every shipped Ruleset — <c>plans/0037</c> F8, and
/// <c>twinned.toml</c>'s header measures it — so the two peaks here never meet at a saddle and the
/// threshold is never consulted. Every value the loader admits gives the same answer on this world.
/// ***A test that could not tell 1 from 100 has not ratified 50***, which is why <c>plans/0002</c> §D1
/// names milestone 15.
/// </para>
/// <para>
/// <b>The clip is held by a hand-built world and not by a shipped Ruleset</b>, because no shipped
/// Ruleset has two road components — <c>twinned.toml</c> is joined by a corridor on purpose, so that
/// component labelling cannot pass for a watershed (<c>adr/0134</c>'s rejected candidate). A fixture
/// that laid two islands and then read back which one each Building was on would be comparing a number
/// against itself; <see cref="TwoIslands"/> instead builds a field the watershed would merge — one flat
/// plateau across adjacent Cells — and asserts that it comes apart anyway.
/// </para>
/// </remarks>
public sealed class DistrictWatershedTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    /// <summary>The threshold <c>twinned.toml</c> ships. Restated so a test can say what it read.</summary>
    private const int ShippedPercent = 50;

    /// <summary>The cadence <c>twinned.toml</c> ships — one Day.</summary>
    private const int ShippedRevisit = 2_048;

    /// <summary>The hysteresis band <c>twinned.toml</c> ships.</summary>
    private const int ShippedBand = 50;

    /// <summary>The damping bound <c>twinned.toml</c> ships.</summary>
    private const int ShippedMigrate = 16;

    private static string Body(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static Ruleset Parsed(string body, string file)
    {
        RulesetLoadResult result = RulesetLoader.Parse(body, file);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static Ruleset Shipped(string file) => Parsed(Body(file), file);

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

    /// <summary>
    /// A shipped Ruleset with a <c>[districts]</c> table bolted on, replacing any it already states.
    /// </summary>
    /// <remarks>
    /// <b>The strip is what lets the theories sweep the threshold on the file that ships one.</b>
    /// Appending a second table is refused by the loader, correctly, so the file's own has to come out
    /// first — and it comes out by finding the section rather than by knowing its line, because a file
    /// this test cannot re-read is a file whose header nobody may edit.
    /// </remarks>
    private static Ruleset WithDistricts(
        string file,
        int percent,
        int revisit = ShippedRevisit,
        int band = ShippedBand,
        int migrate = ShippedMigrate) =>
        Parsed(
            $"{Without(Body(file), "[districts]")}{PricedHinterland}\n[districts]\n"
            + $"prominence_percent = {percent}\nrevisit_ticks = {revisit}\n"
            + $"hysteresis_percent = {band}\nmigrate_cells = {migrate}\n",
            file);

    /// <summary>A TOML body with one table section removed, header comments and all.</summary>
    private static string Without(string body, string section)
    {
        string[] lines = body.Split('\n');
        List<string> kept = [];
        bool inside = false;

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith('['))
            {
                inside = line.TrimStart().StartsWith(section, StringComparison.Ordinal);
            }

            if (!inside)
            {
                kept.Add(line);
            }
        }

        return string.Join('\n', kept);
    }

    private static World Evaluated(Ruleset rules, int citizens)
    {
        var world = new World(citizens, rules);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);
        world.EvaluateDistricts();

        return world;
    }

    /// <summary>Every live District row's slot.</summary>
    private static List<int> Districts(World world)
    {
        List<int> slots = [];

        for (int slot = 0; slot < world.Districts.Rows.SlotCount; slot++)
        {
            if (world.Districts.Rows.IsLive(slot))
            {
                slots.Add(slot);
            }
        }

        return slots;
    }

    /// <summary>How many Cells each District holds, keyed by the District's slot.</summary>
    private static Dictionary<int, int> Extents(World world)
    {
        Dictionary<int, int> extents = [];

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (!world.DistrictCells.Rows.IsLive(slot))
            {
                continue;
            }

            int district = world.Districts.Rows.Resolve(world.DistrictCells.District[slot]);

            extents[district] = extents.GetValueOrDefault(district) + 1;
        }

        return extents;
    }

    /// <summary>
    /// Whether a Cell is in the settlement: <b>built on, or ground a Zone is holding vacant for a
    /// trade</b>.
    /// </summary>
    /// <remarks>
    /// <b>The watershed's own input, and this had to widen with it</b> (<c>adr/0165</c>). The extent
    /// was *built Cells only* while every Lot the generator carved was permitted to dwellings and
    /// filled immediately. A commercial block is permitted to a trade, is deliberately left vacant
    /// for demand to claim, and ***counting it as empty made a one-lattice world report eight
    /// concentrations***. <c>Space.DistrictWatershed.HeldForTrade</c> is where the simulation says so;
    /// this mirrors it so the assertions here keep asking the operator's own question.
    /// </remarks>
    private static bool Settled(World world, Cells east, Cells north)
    {
        if (world.BuildingsInCells.Density(east, north) > 0)
        {
            return true;
        }

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && world.Lots.IsVacant(slot)
                && (world.Lots.Zone[slot] & LotTable.Trade) != 0
                && CellGrid.ToCells(world.Lots.East[slot]).Raw == east.Raw
                && CellGrid.ToCells(world.Lots.North[slot]).Raw == north.Raw)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Every Cell in the settlement, as a grid index.</summary>
    /// <remarks>
    /// ⚠ <b>It said <em>every Cell that holds at least one Building</em> until <c>adr/0165</c></b>,
    /// and the name still says so. It is kept because it is what the watershed's extent is compared
    /// against, and <see cref="Settled"/> is now the operator's own question.
    /// </remarks>
    private static List<int> BuiltCells(World world)
    {
        List<int> built = [];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                if (Settled(world, new Cells(east), new Cells(north)))
                {
                    built.Add(CellGrid.Index(new Cells(east), new Cells(north)));
                }
            }
        }

        return built;
    }

    // ---- the table, and what its absence means ---------------------------------------------------

    /// <summary>
    /// A Ruleset with no <c>[districts]</c> derives no Districts, however dense the city is.
    /// </summary>
    /// <remarks>
    /// <b>This is the polarity decision as a test.</b> The threshold is hash-bearing and unratified, so
    /// it must not be defaulted into the binary: a number nobody chose, in a file nobody can see,
    /// ratified by nothing, is exactly what <c>adr/0052</c> refuses. Absence of the table is the
    /// spelling for <em>this city has no Districts</em>.
    /// </remarks>
    [Fact]
    public void A_ruleset_that_states_no_districts_derives_none()
    {
        World world = Evaluated(Shipped("minimal.toml"), citizens: 4_000);

        Assert.False(world.Rules.Districts.Runs);
        Assert.NotEmpty(BuiltCells(world));
        Assert.Empty(Districts(world));
        Assert.Equal(0, world.DistrictCells.Rows.LiveCount);
        Assert.Equal(0, world.DistrictsInCells.Count);
    }

    /// <summary>
    /// One lattice is one District, at every threshold the loader admits.
    /// </summary>
    /// <remarks>
    /// <b>The <c>[Theory]</c> is the point rather than thoroughness.</b> A flat plateau has no saddle
    /// inside it, so no peak ever dies and the threshold is never read — and a test that ran at one
    /// value would leave that indistinguishable from a threshold that happens to suit.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(ShippedPercent)]
    [InlineData(100)]
    public void One_lattice_is_one_district_at_any_threshold(int percent)
    {
        World world = Evaluated(WithDistricts("minimal.toml", percent), citizens: 4_000);

        Assert.Single(Districts(world));
    }

    // ---- two concentrations ---------------------------------------------------------------------

    /// <summary>
    /// <c>twinned.toml</c> comes out as two Districts, at every threshold the loader admits.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(ShippedPercent)]
    [InlineData(100)]
    public void Two_lattices_are_two_districts_at_any_threshold(int percent)
    {
        World world = Evaluated(WithDistricts("twinned.toml", percent), citizens: 4_000);

        Assert.Equal(2, Districts(world).Count);
    }

    /// <summary>The shipped file's own threshold, read from the file rather than restated.</summary>
    [Fact]
    public void The_shipped_twinned_ruleset_states_the_threshold_this_file_tests()
    {
        Assert.Equal(ShippedPercent, Shipped("twinned.toml").Districts.ProminencePercent);
    }

    /// <summary>
    /// The two Districts sit on opposite sides of the gap, and neither reaches across it.
    /// </summary>
    /// <remarks>
    /// <b>The boundary is derived from the Ruleset's own second origin, not read off the answer.</b>
    /// A test that took the midpoint between the two centres it found would pass for any partition
    /// whatever, including one that split a single lattice down the middle.
    /// </remarks>
    [Fact]
    public void Each_district_lies_wholly_on_one_side_of_the_authored_gap()
    {
        Ruleset rules = WithDistricts("twinned.toml", ShippedPercent);
        World world = Evaluated(rules, citizens: 4_000);

        int easternOrigin = rules.Lattices.Max(lattice => lattice.OriginEastTiles);
        Cells frontier = CellGrid.ToCells(new Tiles(easternOrigin));

        Dictionary<int, bool> westOfFrontier = [];

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (!world.DistrictCells.Rows.IsLive(slot))
            {
                continue;
            }

            int district = world.Districts.Rows.Resolve(world.DistrictCells.District[slot]);
            bool west = world.DistrictCells.East[slot].Raw < frontier.Raw;

            if (westOfFrontier.TryGetValue(district, out bool seen))
            {
                Assert.Equal(seen, west);
            }
            else
            {
                westOfFrontier[district] = west;
            }
        }

        Assert.Equal(2, westOfFrontier.Count);
        Assert.Contains(true, westOfFrontier.Values);
        Assert.Contains(false, westOfFrontier.Values);
    }

    /// <summary>The two Districts are about the same size, because the two lattices are.</summary>
    /// <remarks>
    /// <b>Within one Cell</b>, and the tolerance is the generator's own: the population share is
    /// <c>floor(total / lattices)</c> plus one to the earlier lattices, so an odd Building count puts
    /// the extra Building in the west and it may or may not open a Cell.
    /// </remarks>
    [Fact]
    public void The_two_districts_hold_about_the_same_number_of_cells()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 16_000);

        List<int> extents = [.. Extents(world).Values];

        Assert.Equal(2, extents.Count);
        Assert.True(
            Math.Abs(extents[0] - extents[1]) <= 1,
            $"the two Districts hold {extents[0]} and {extents[1]} Cells. The two lattices carry an "
            + "equal share of the population by construction, so a difference of more than one Cell "
            + "means the watershed has put part of one lattice into the other's District.");
    }

    // ---- the partition ---------------------------------------------------------------------------

    /// <summary>Every built Cell is in exactly one District, and nothing else is in any.</summary>
    [Fact]
    public void The_districts_partition_the_built_cells_and_nothing_else()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 4_000);

        List<int> built = BuiltCells(world);
        HashSet<int> filed = [];

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (!world.DistrictCells.Rows.IsLive(slot))
            {
                continue;
            }

            int index = CellGrid.Index(world.DistrictCells.East[slot], world.DistrictCells.North[slot]);

            Assert.True(filed.Add(index), $"Cell {index} was filed under two Districts.");
            Assert.True(
                Settled(world, world.DistrictCells.East[slot], world.DistrictCells.North[slot]),
                $"Cell {index} is neither built on nor held for a trade, and is in a District. "
                + "Empty ground drains nowhere.");
        }

        Assert.Equal(built.Count, filed.Count);
        Assert.All(built, index => Assert.Contains(index, filed));
    }

    /// <summary>A District's centre is one of its own Cells, and is as dense as any of them.</summary>
    [Fact]
    public void A_districts_centre_is_its_densest_cell()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 4_000);

        foreach (int district in Districts(world))
        {
            Cells east = world.Districts.CentreEast[district];
            Cells north = world.Districts.CentreNorth[district];

            Assert.Equal(
                world.Districts.Rows.At(district),
                world.DistrictsInCells.Of(world.DistrictCells, east, north));

            int peak = world.BuildingsInCells.Density(east, north);

            for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
            {
                if (!world.DistrictCells.Rows.IsLive(slot)
                    || world.Districts.Rows.Resolve(world.DistrictCells.District[slot]) != district)
                {
                    continue;
                }

                Assert.True(
                    world.BuildingsInCells.Density(
                        world.DistrictCells.East[slot], world.DistrictCells.North[slot]) <= peak,
                    "a District holds a Cell denser than its own centre, so the flood assigned it to "
                    + "the wrong basin.");
            }
        }
    }

    /// <summary>A Building's District is found through its Cell, and every Building has one.</summary>
    [Fact]
    public void Every_buildings_district_resolves_through_its_cell()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 4_000);

        int found = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot))
            {
                continue;
            }

            Cells east = CellGrid.ToCells(world.Lots.East[lot]);
            Cells north = CellGrid.ToCells(world.Lots.North[lot]);

            Assert.True(
                world.Districts.Rows.IsValid(
                    world.DistrictsInCells.Of(world.DistrictCells, east, north)),
                $"Building {slot} stands in Cell ({east.Raw}, {north.Raw}) and that Cell is in no "
                + "District, so Scope.Pool would have nothing to resolve against.");

            found++;
        }

        Assert.True(found > 0);
    }

    // ---- the road-component clip ------------------------------------------------------------------

    /// <summary>
    /// Two flat plateaus in adjacent Cells, on two road components, are two Districts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The field alone would merge this world and the clip is the only thing that does not.</b>
    /// Every Cell in it holds the same number of Buildings and the built Cells are contiguous, so there
    /// is no saddle anywhere: a watershed reading density and nothing else returns one basin at every
    /// threshold. Two Districts here can only come from the components.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads the FOOT subgraph</b>, which is the weaker of the two — a world reachable on foot
    /// is reachable by car and not conversely — so it is the one that can separate a city that drives
    /// everywhere. The two chains here admit both modes, so the two labellings agree and the test does
    /// not depend on which is read; <c>The_clip_reads_the_foot_subgraph</c> below is what pins that.
    /// </para>
    /// </remarks>
    [Fact]
    public void Two_road_components_are_two_districts_even_where_the_field_is_one_plateau()
    {
        World world = TwoIslands(TravelMode.Any);

        Assert.Equal(1, world.Roads.Connectivity.FootComponents - 1);
        Assert.Equal(2, Districts(world).Count);
    }

    /// <summary>
    /// The same two islands joined into one component are one District.
    /// </summary>
    /// <remarks>
    /// <b>The control, and without it the test above proves nothing.</b> It holds the field, the
    /// Buildings and the Cells fixed and changes only whether the two chains are joined — so a split
    /// that survived this would be a split the geometry was producing rather than the clip.
    /// </remarks>
    [Fact]
    public void The_same_two_islands_joined_are_one_district()
    {
        World world = TwoIslands(TravelMode.Any, joined: true);

        Assert.Equal(1, world.Roads.Connectivity.FootComponents);
        Assert.Single(Districts(world));
    }

    /// <summary>
    /// A link that carries cars and not feet leaves the two Districts apart.
    /// </summary>
    /// <remarks>
    /// <b>This is the fork <c>twinned.toml</c>'s header refuses to hand anybody, asserted rather than
    /// avoided.</b> A car-only link makes the world ONE component for driving and TWO for walking, so
    /// the two subgraphs disagree and the result says which one the clip read. It reads the foot
    /// subgraph, and the reason is that a Pool is a thing Buildings share: what matters is whether
    /// somebody could get between them at all, and the foot graph is the one that answers that for a
    /// city whose Households own no car.
    /// </remarks>
    [Fact]
    public void The_clip_reads_the_foot_subgraph()
    {
        World world = TwoIslands(TravelMode.Car, joined: true);

        Assert.Equal(1, world.Roads.Connectivity.CarComponents);
        Assert.Equal(2, world.Roads.Connectivity.FootComponents);
        Assert.Equal(2, Districts(world).Count);
    }

    /// <summary>
    /// Two chains of Street, each carrying Buildings, in adjacent Cells, optionally joined.
    /// </summary>
    /// <remarks>
    /// <b>Hand-built rather than generated, on <c>RoadFixtures.TwoIslands</c>' reasoning</b> — a test
    /// that drove the generator would be asking whether the generator produces two components, which
    /// is a different question and one no shipped Ruleset answers yes to.
    /// </remarks>
    private static World TwoIslands(TravelMode link, bool joined = false)
    {
        var world = new World(100, WithDistricts("minimal.toml", ShippedPercent));

        Tiles block = new(world.Rules.Roads.BlockTiles);

        Handle<RoadNode>[] ends = new Handle<RoadNode>[2];
        Handle<RoadNode>[] starts = new Handle<RoadNode>[2];

        for (int island = 0; island < 2; island++)
        {
            // Four blocks apiece, laid end to end, so the two islands occupy adjacent Cells with no
            // unbuilt ground between them -- which is what makes the density field one plateau.
            Handle<RoadNode> previous = world.Roads.Nodes.Create(
                new Tiles(island * 4 * block.Raw), Tiles.Zero);

            starts[island] = previous;

            for (int step = 1; step <= 4; step++)
            {
                Handle<RoadNode> next = world.Roads.Nodes.Create(
                    new Tiles(((island * 4) + step) * block.Raw), Tiles.Zero);

                world.Roads.Segments.Create(
                    previous, next, block, RoadKind.Street, TravelMode.Any, TravelMode.Any);

                previous = next;
            }

            ends[island] = previous;
        }

        if (joined)
        {
            world.Roads.Segments.Create(ends[0], starts[1], Tiles.Zero, RoadKind.Street, link, link);
        }

        world.Roads.RebuildDerived();

        // Four Lots per Cell, so every Cell the chains cross holds the same number of Buildings and
        // the field has no gradient in it at all.
        //
        // OFFSET BY FOUR TILES, AND IT IS NOT COSMETIC. A Lot at a multiple of the block sits exactly
        // on an intersection, and CONTEXT.md -> Address is emphatic that an Address is never a Node --
        // so Frontage.Locate gives it no Segment, and a Cell whose lowest-slot Building is that one
        // reported no road component at all. The first version of this fixture did that in all eight
        // Cells and came out as ONE District with every other assertion passing.
        for (int east = 4; east < 8 * block.Raw; east += 8)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(east), Tiles.Zero, zone: 1);

            world.CreateBuilding(lot, kind: 0, Ticks.Zero, Key);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        return world;
    }

    // ---- the index -------------------------------------------------------------------------------

    /// <summary>
    /// A rebuild reproduces the Cell-to-District index exactly, and re-derives nothing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The second half is the one worth having.</b> <see cref="World.RebuildDerived"/> must
    /// restore the Districts the world <em>had</em>, never the ones its field would support now — a
    /// distinction that costs nothing today, because the watershed is deterministic and the world has
    /// not changed, and which becomes the whole reason a District is saved at task 4.
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_the_index_and_does_not_re_evaluate()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 4_000);

        List<int> before = Districts(world);
        int cells = world.DistrictCells.Rows.LiveCount;
        int resident = world.DistrictsInCells.Count;

        world.RebuildDerived();

        Assert.Equal(before, Districts(world));
        Assert.Equal(cells, world.DistrictCells.Rows.LiveCount);
        Assert.Equal(resident, world.DistrictsInCells.Count);

        for (int slot = 0; slot < world.DistrictCells.Rows.SlotCount; slot++)
        {
            if (world.DistrictCells.Rows.IsLive(slot))
            {
                Assert.Equal(
                    slot,
                    world.DistrictsInCells.Slot(
                        world.DistrictCells.East[slot], world.DistrictCells.North[slot]));
            }
        }
    }

    /// <summary>Evaluating twice replaces the Districts rather than adding to them.</summary>
    /// <remarks>
    /// <b>The clear is unconditional, and this is what holds it.</b> A second evaluation that appended
    /// would double the rows and leave <see cref="DistrictResidency"/> pointing at whichever copy was
    /// written last — a defect nothing else here would see, because every assertion above walks the
    /// index rather than the table.
    /// </remarks>
    [Fact]
    public void Evaluating_twice_replaces_rather_than_appends()
    {
        World world = Evaluated(WithDistricts("twinned.toml", ShippedPercent), citizens: 4_000);

        int districts = world.Districts.Rows.LiveCount;
        int cells = world.DistrictCells.Rows.LiveCount;

        world.EvaluateDistricts();

        Assert.Equal(districts, world.Districts.Rows.LiveCount);
        Assert.Equal(cells, world.DistrictCells.Rows.LiveCount);
        Assert.Equal(cells, world.DistrictsInCells.Count);
    }

    // ---- the refusals ----------------------------------------------------------------------------

    /// <summary>A threshold of zero is refused rather than read as "no Districts".</summary>
    [Fact]
    public void A_threshold_of_zero_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            $"{Body("minimal.toml")}\n[districts]\nprominence_percent = 0\n", "test.toml");

        Assert.Null(result.Ruleset);
        Assert.Contains("prominence_percent", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A threshold above a whole peak's height is refused.</summary>
    [Fact]
    public void A_threshold_above_one_hundred_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            $"{Body("minimal.toml")}\n[districts]\nprominence_percent = 101\n", "test.toml");

        Assert.Null(result.Ruleset);
        Assert.Contains("prominence_percent", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>Two <c>[districts]</c> tables are refused.</summary>
    [Fact]
    public void A_second_districts_table_is_refused()
    {
        RulesetLoadResult result = RulesetLoader.Parse(
            $"{Body("minimal.toml")}\n[districts]\nprominence_percent = 50\n"
            + "[districts]\nprominence_percent = 60\n",
            "test.toml");

        Assert.Null(result.Ruleset);
    }

    /// <summary>
    /// <c>twinned.toml</c>, <c>provisioned.toml</c> and <c>oversupplied.toml</c> are the only
    /// shipped Rulesets that state the table, and the last two descend from the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>They are the demonstration files and the others are demonstrations of other things.</b> A
    /// <c>[districts]</c> table in a file with one lattice in it would derive one District and teach
    /// nobody anything, at the cost of a hash-bearing unratified number in eight more files.
    /// </para>
    /// <para>
    /// ⚠ <b><c>oversupplied.toml</c> inherits it from <c>provisioned.toml</c>, which is its base</b>
    /// (milestone 26 task 6) — the two differ by <c>build_threshold_days</c> and <c>cooldown_days</c>
    /// and by nothing else, because <c>adr/0170</c>'s selection model needs over-supply and tier 1
    /// exists to remove it.
    /// </para>
    /// <para>
    /// ⚠ <b><c>provisioned.toml</c> did not choose to state it</b> (milestone 26 task 3). It needs
    /// two centres because a District derivation over one peak returns one basin however it is
    /// written, and once it states <c>[districts]</c> the loader's <c>RefuseUnpricedGoods</c> demands
    /// a <c>[[hinterland]]</c> price for every declared Good — so the table, the second lattice and
    /// the prices arrive together or not at all. ***The exemption is by name and carries its reason,
    /// so a third file growing the table by accident still goes red here.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Only_twinned_states_a_districts_table()
    {
        string[] shipped = Directory.GetFiles(
            Path.Combine(AppContext.BaseDirectory, "Rulesets"), "*.toml");

        Assert.NotEmpty(shipped);

        foreach (string path in shipped)
        {
            string file = Path.GetFileName(path);
            bool states = Shipped(file).Districts.Runs;
            // waged.toml IS provisioned.toml with two [[business]] keys added, so it inherits
            // [districts] for that file's reason and not for one of its own -- oversupplied.toml's
            // note above, arriving a second time. Its header says the same.
            bool expected = file is "twinned.toml" or "provisioned.toml" or "oversupplied.toml"
                or "waged.toml";

            Assert.Equal(expected, states);
        }
    }
}
