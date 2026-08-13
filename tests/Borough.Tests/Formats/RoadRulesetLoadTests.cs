using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 5a: <c>[roads]</c>, the conversions inside it, and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire.</b> That is the
/// discipline the loader's guards are held to, and <c>adr/0064</c> is why it is written down here:
/// the one guard in this loader that shipped without a test was re-derived as <em>absent</em> by a
/// later reader, from nothing but the shape of its absence in the suite. A guard with no test is
/// invisible to every future reader, including the one about to decide it does not exist.
/// </para>
/// <para>
/// <b>The refusals here share <c>[layers]</c>' symptom</b>: each one produces a Ruleset that loads
/// clean, runs for ever and quietly does nothing a reader could point at — an Arterial that is laid,
/// counted and absent from the graph; a network cut in two for anybody on foot. Nothing in a running
/// city distinguishes those from a designer's intent, which is why they are caught at the one moment
/// a human is looking at the file.
/// </para>
/// <para>
/// <b>The polarity is <c>[placement]</c>'s and not <c>[layers]</c>'</b>: no table means <em>no
/// roads</em> rather than <em>the documented defaults</em>, because a default would put eleven
/// hash-bearing numbers in the binary that nobody authored. Its consequence is the other half of what
/// is tested here — once the table is present, every key in it is required.
/// </para>
/// </remarks>
public sealed class RoadRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    /// <summary>
    /// The smallest complete <c>[roads]</c> table: every required key, every value in range.
    /// </summary>
    /// <remarks>
    /// The numbers are <c>rulesets/minimal.toml</c>'s, so a test that edits one key is asking what
    /// the shipped city would do with that key wrong rather than what some other city would.
    /// </remarks>
    private const string Streets = """
        [roads]
        block_tiles = 32
        arterial_count = 8
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000
        """;

    private static Ruleset Accepted(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static RulesetRefusal Refused(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.False(result.Ok, "the Ruleset was accepted.");

        return result.Refusals[0];
    }

    /// <summary>
    /// <see cref="Streets"/> with one key restated, at that key's own line.
    /// </summary>
    /// <remarks>
    /// A substitution rather than an appended override, because a repeated TOML key is the file's own
    /// error and would be refused before the loader could say anything about the value — and because
    /// the line a refusal reports is the line the reader has to go and edit.
    /// </remarks>
    private static string RoadsWith(string key, string value) =>
        string.Join('\n', Streets.Split('\n').Select(
            line => KeyOf(line) == key ? $"{key} = {value}" : line));

    /// <summary>
    /// <see cref="Streets"/> with one key deleted, which is what "required" is tested against.
    /// </summary>
    private static string RoadsWithout(string key) =>
        string.Join('\n', Streets.Split('\n').Where(line => KeyOf(line) != key));

    /// <summary>The key a <c>[roads]</c> line states, or the empty string for the table header.</summary>
    private static string KeyOf(string line) =>
        line.Split('=') is [string key, _] ? key.Trim() : string.Empty;

    // ---- the absent table -----------------------------------------------------------------------

    /// <summary>
    /// <b>The absence means no roads, and it is stated by the type rather than inferred.</b>
    /// </summary>
    /// <remarks>
    /// The opposite polarity to <c>[layers]</c>, and the assertion that pins which one this table
    /// has. A defaulted road network would be a city laced with roads its designer never asked for,
    /// at a density that decides Segment count and therefore every routing figure downstream.
    /// </remarks>
    [Fact]
    public void A_Ruleset_with_no_roads_table_is_a_complete_Ruleset_with_no_roads()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(RoadRuleset.None, ruleset.Roads);
        Assert.False(ruleset.Roads.Runs);
        Assert.Equal(0, ruleset.Roads.BlockTiles);
    }

    // ---- the well-formed table ------------------------------------------------------------------

    [Fact]
    public void A_well_formed_roads_table_reaches_the_Ruleset_key_by_key()
    {
        RoadRuleset roads = Accepted($"""
            {Nothing}

            {Streets}
            """).Roads;

        Assert.True(roads.Runs);
        Assert.Equal(32, roads.BlockTiles);
        Assert.Equal(8, roads.ArterialCount);
        Assert.Equal(512, roads.ArterialJunctionTiles);
        Assert.Equal(4, roads.FootCrossingEvery);
        Assert.Equal(40, roads.FootPathsPerThousandBlocks);
    }

    /// <summary>
    /// <b>The exchange rate lives outside the simulation</b> (<c>02 §2</c>: no seconds and no metres
    /// in the library), so the loader is the only place km/h is ever seen — and this is where the
    /// conversion is checked against the constructor rather than against a copied constant.
    /// </summary>
    [Fact]
    public void An_authored_speed_in_kilometres_per_hour_becomes_Tiles_per_Tick()
    {
        RoadRuleset roads = Accepted($"""
            {Nothing}

            {Streets}
            """).Roads;

        Assert.Equal(Speed.FromKilometresPerHour(50), roads.StreetSpeed);
        Assert.Equal(Speed.FromKilometresPerHour(90), roads.ArterialSpeed);
        Assert.Equal(Speed.FromKilometresPerHour(5), roads.WalkSpeed);

        Assert.Equal(Speed.FromKilometresPerHour(50), roads.SpeedFor(RoadKind.Street));
        Assert.Equal(Speed.FromKilometresPerHour(90), roads.SpeedFor(RoadKind.Arterial));
        Assert.Equal(Speed.FromKilometresPerHour(5), roads.SpeedFor(RoadKind.FootPath));
    }

    /// <summary>
    /// <b>A Day is <c>CONTEXT.md</c>'s only time unit above the Tick</b>, so the hourly figure a human
    /// authors is multiplied by 24 exactly and never divided again at runtime.
    /// </summary>
    [Fact]
    public void An_authored_capacity_per_hour_becomes_Vehicles_per_Day()
    {
        RoadRuleset roads = Accepted($"""
            {Nothing}

            {Streets}
            """).Roads;

        Assert.Equal(3_600 * 24, roads.StreetCapacityPerDay);
        Assert.Equal(12_000 * 24, roads.ArterialCapacityPerDay);
        Assert.Equal(1_000 * 24, roads.FootPathCapacityPerDay);

        Assert.Equal(3_600 * 24, roads.CapacityFor(RoadKind.Street));
        Assert.Equal(12_000 * 24, roads.CapacityFor(RoadKind.Arterial));
        Assert.Equal(1_000 * 24, roads.CapacityFor(RoadKind.FootPath));
    }

    // ---- the shape of the table -----------------------------------------------------------------

    [Fact]
    public void A_second_roads_table_is_refused_rather_than_merged()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[roads]]
            block_tiles = 32

            [[roads]]
            block_tiles = 64
            """);

        Assert.Contains("a second [roads]", refusal.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_section_names_roads_among_the_ones_that_exist()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [road]
            block_tiles = 32
            """);

        Assert.Contains("[roads]", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("is not a Ruleset section", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Every key is required once the table is present</b>, because the author has said the world
    /// has roads and there is no number here the engine can derive.
    /// </summary>
    /// <remarks>
    /// The refusal is reported against the <em>table</em> rather than a line, which is the only place
    /// it can be: a key that is not there has no line. That is why the line assertion is 5 for every
    /// one of these and the key's own line for a value that is merely wrong.
    /// </remarks>
    [Theory]
    [InlineData("block_tiles")]
    [InlineData("arterial_count")]
    [InlineData("arterial_junction_tiles")]
    [InlineData("foot_crossing_every")]
    [InlineData("foot_paths_per_thousand_blocks")]
    [InlineData("street_speed_kph")]
    [InlineData("arterial_speed_kph")]
    [InlineData("walk_speed_kph")]
    [InlineData("street_capacity_per_hour")]
    [InlineData("arterial_capacity_per_hour")]
    [InlineData("foot_path_capacity_per_hour")]
    public void A_missing_roads_key_is_refused_by_name(string key)
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            {RoadsWithout(key)}
            """);

        Assert.Contains($"no {key}.", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(5, refusal.Line);
    }

    // ---- the ranges -----------------------------------------------------------------------------

    /// <summary>
    /// One <c>[InlineData]</c> per boundary, on both sides of every range the table states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two spatial maxima are the map's width, and they are tested elsewhere</b> — see
    /// <see cref="The_two_spatial_maxima_are_the_maps_own_width"/>. They cannot live in an
    /// <c>[InlineData]</c>, because an attribute argument must be a literal and <b>a literal here is a
    /// premise about <see cref="CellGrid.WorldTiles"/> that goes stale silently when the map moves</b>
    /// — which is exactly what happened on 2026-08-13, when the map went from 4,096 Tiles to 16,384 and
    /// <c>4097</c> stopped being out of range. <c>foot_crossing_every</c> has no maximum of its own, so
    /// its upper boundary is where the authored figure stops fitting in the column at all.
    /// </para>
    /// <para>
    /// <b>The speed ceiling is a property of the representation</b>: 682 km/h is where Q16.16 Tiles
    /// per Tick runs out (<c>adr/0071</c>), and no road is anywhere near it. The capacity ceiling is
    /// the same kind of number one unit up — where the per-hour figure times 24 stops fitting.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("block_tiles", "0", "It is the Street grid spacing")]
    [InlineData("arterial_count", "-1", "how many freeform Arterials")]
    [InlineData("arterial_count", "1025", "how many freeform Arterials")]
    [InlineData("arterial_junction_tiles", "0", "at least 1 Tile")]
    [InlineData("foot_crossing_every", "-1", "no crossings at all")]
    [InlineData("foot_crossing_every", "2147483648", "no crossings at all")]
    [InlineData("foot_paths_per_thousand_blocks", "-1", "count per thousand blocks")]
    [InlineData("foot_paths_per_thousand_blocks", "1001", "count per thousand blocks")]
    [InlineData("street_speed_kph", "0", "free-flow speed in km/h")]
    [InlineData("street_speed_kph", "683", "Q16.16 Tiles per Tick runs out")]
    [InlineData("arterial_speed_kph", "0", "free-flow speed in km/h")]
    [InlineData("arterial_speed_kph", "683", "Q16.16 Tiles per Tick runs out")]
    [InlineData("walk_speed_kph", "0", "free-flow speed in km/h")]
    [InlineData("walk_speed_kph", "683", "Q16.16 Tiles per Tick runs out")]
    [InlineData("street_capacity_per_hour", "0", "flow capacity in Vehicles per hour")]
    [InlineData("street_capacity_per_hour", "89478486", "stops fitting")]
    [InlineData("arterial_capacity_per_hour", "0", "flow capacity in Vehicles per hour")]
    [InlineData("arterial_capacity_per_hour", "89478486", "stops fitting")]
    [InlineData("foot_path_capacity_per_hour", "0", "flow capacity in Vehicles per hour")]
    [InlineData("foot_path_capacity_per_hour", "89478486", "stops fitting")]
    public void A_road_number_outside_its_range_is_refused_by_name(
        string key, string value, string because)
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            {RoadsWith(key, value)}
            """);

        Assert.Contains($"{key} = {value} is out of range.", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains(because, refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The line a range refusal reports is the key's own, which is the line to go and edit.</summary>
    [Fact]
    public void A_range_refusal_reports_the_line_the_wrong_number_is_on()
    {
        // Line 5 is [roads] and block_tiles is the first key under it.
        Assert.Equal(6, Refused($"""
            {Nothing}

            {RoadsWith("block_tiles", "0")}
            """).Line);

        // The eleventh key, eleven lines below the header.
        Assert.Equal(16, Refused($"""
            {Nothing}

            {RoadsWith("foot_path_capacity_per_hour", "0")}
            """).Line);
    }

    [Theory]
    [InlineData("block_tiles", "1")]
    [InlineData("arterial_count", "0")]
    [InlineData("arterial_count", "1024")]
    [InlineData("arterial_junction_tiles", "1")]
    [InlineData("foot_crossing_every", "0")]
    [InlineData("foot_crossing_every", "2147483647")]
    [InlineData("foot_paths_per_thousand_blocks", "0")]
    [InlineData("foot_paths_per_thousand_blocks", "1000")]
    [InlineData("street_speed_kph", "1")]
    [InlineData("street_speed_kph", "682")]
    [InlineData("arterial_speed_kph", "1")]
    [InlineData("arterial_speed_kph", "682")]
    [InlineData("walk_speed_kph", "1")]
    [InlineData("walk_speed_kph", "682")]
    [InlineData("street_capacity_per_hour", "1")]
    [InlineData("street_capacity_per_hour", "89478485")]
    [InlineData("arterial_capacity_per_hour", "1")]
    [InlineData("foot_path_capacity_per_hour", "1")]
    public void A_road_number_on_its_own_boundary_is_accepted(string key, string value)
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            {RoadsWith(key, value)}
            """);

        Assert.True(ruleset.Roads.Runs);
    }

    /// <summary>
    /// <b>The two spatial maxima are the map's own width, and this test computes it rather than
    /// spelling it.</b>
    /// </summary>
    /// <remarks>
    /// A block or a Junction spacing wider than the world is a number that lays nothing, so the
    /// ceiling is <see cref="CellGrid.WorldTiles"/> and the first refused value is one Tile past it.
    /// <b>Both sides are derived, which is the point of the test existing separately.</b> These two
    /// cases sat in the range <c>[Theory]</c> as the literals <c>4096</c> and <c>4097</c> until the map
    /// went to 512 Cells; the accepted one stayed green while ceasing to test a boundary at all, and
    /// the refused one failed. <b>An assertion that goes green by accident is worse than one that goes
    /// red</b>, and only the second of the pair announced itself.
    /// </remarks>
    [Fact]
    public void The_two_spatial_maxima_are_the_maps_own_width()
    {
        string wide = $"{CellGrid.WorldTiles + 1}";
        string exact = $"{CellGrid.WorldTiles}";

        foreach ((string key, string because) in new[]
        {
            ("block_tiles", "at most the width of the map"),
            ("arterial_junction_tiles", "at least 1 Tile"),
        })
        {
            RulesetRefusal refusal = Refused($"""
                {Nothing}

                {RoadsWith(key, wide)}
                """);

            Assert.Contains(
                $"{key} = {wide} is out of range.", refusal.Reason, StringComparison.Ordinal);
            Assert.Contains(because, refusal.Reason, StringComparison.Ordinal);
        }

        Assert.True(Accepted($"""
            {Nothing}

            {RoadsWith("block_tiles", exact)}
            """).Roads.Runs);
    }

    // ---- the refusal that is a property of two numbers together ---------------------------------

    /// <summary>
    /// <b>An Arterial that cannot reach a second Junction has no Segment at all.</b>
    /// </summary>
    /// <remarks>
    /// The file would read as "rare Arterials" and behave as "no Arterials", with a Segment count
    /// that still looks healthy because every Street is there. Neither number is out of range on its
    /// own, which is what makes this a joint check rather than a range one — and the refusal is
    /// reported against the spacing, because that is the number a designer meant to state in Tiles.
    /// </remarks>
    [Fact]
    public void An_Arterial_spacing_wider_than_the_map_is_refused_when_there_are_Arterials()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            {RoadsWith("arterial_junction_tiles", $"{CellGrid.WorldTiles}")}
            """);

        Assert.Contains("no Arterial can reach a second Junction", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("absent from the graph", refusal.Reason, StringComparison.Ordinal);
        Assert.Equal(8, refusal.Line);
    }

    [Fact]
    public void The_same_spacing_is_accepted_where_no_Arterial_is_laid()
    {
        // Nothing is wrong with the number; what is wrong is an Arterial that cannot use it. With
        // no Arterials the key states a spacing for a road kind the file does not build.
        Ruleset ruleset = Accepted($"""
            {Nothing}

            {RoadsWith("arterial_junction_tiles", $"{CellGrid.WorldTiles}").Replace(
                "arterial_count = 8", "arterial_count = 0", StringComparison.Ordinal)}
            """);

        Assert.Equal(0, ruleset.Roads.ArterialCount);
        Assert.Equal(CellGrid.WorldTiles, ruleset.Roads.ArterialJunctionTiles);
    }

    [Fact]
    public void A_spacing_one_Tile_inside_the_map_is_accepted_with_Arterials()
    {
        Ruleset ruleset = Accepted($"""
            {Nothing}

            {RoadsWith("arterial_junction_tiles", $"{CellGrid.WorldTiles - 1}")}
            """);

        Assert.Equal(8, ruleset.Roads.ArterialCount);
        Assert.Equal(CellGrid.WorldTiles - 1, ruleset.Roads.ArterialJunctionTiles);
    }

    // ---- the stated absences --------------------------------------------------------------------

    /// <summary>
    /// <b>Three zeroes here are statements rather than errors</b>, and each one names a city.
    /// </summary>
    /// <remarks>
    /// No Arterials is a city of Streets alone, which is a city where Severance cannot happen; no
    /// crossings is a network cut in two for anybody on foot; no cut-throughs is a city where every
    /// pedestrian walks the block. All three are cities somebody may want to run, and refusing them
    /// would be the loader deciding a design question. Contrast <c>block_tiles = 0</c>, which is not
    /// a city at all, and a capacity of zero, which is a division by zero wherever it is read.
    /// </remarks>
    [Fact]
    public void A_world_of_Streets_alone_with_no_crossings_and_no_cut_throughs_is_legal()
    {
        string toml = RoadsWith("arterial_count", "0");
        toml = string.Join('\n', toml.Split('\n').Select(line => KeyOf(line) switch
        {
            "foot_crossing_every" => "foot_crossing_every = 0",
            "foot_paths_per_thousand_blocks" => "foot_paths_per_thousand_blocks = 0",
            _ => line,
        }));

        RoadRuleset roads = Accepted($"""
            {Nothing}

            {toml}
            """).Roads;

        Assert.True(roads.Runs);
        Assert.Equal(0, roads.ArterialCount);
        Assert.Equal(0, roads.FootCrossingEvery);
        Assert.Equal(0, roads.FootPathsPerThousandBlocks);
    }

    // ---- the shipped Ruleset --------------------------------------------------------------------

    /// <summary>
    /// <c>rulesets/minimal.toml</c> states the road numbers, and this is where they are read back.
    /// </summary>
    /// <remarks>
    /// <b>Every one of them is hash-bearing and unratified</b> (<c>plans/0002</c> §D), so the
    /// assertion is against the literals the file states rather than against any constant in the
    /// binary — a future edit that moved a default and the file together would otherwise pass here
    /// while changing every Segment in the city.
    /// </remarks>
    [Fact]
    public void The_shipped_ruleset_states_the_road_numbers_it_says_it_does()
    {
        RoadRuleset roads = GoldenFixtures.Rules().Roads;

        Assert.True(roads.Runs);

        // 32 Tiles is one Street on every Cell boundary, which is the rung that reproduces
        // CONTEXT.md -> Segment's ~30,000-Segment placeholder.
        Assert.Equal(CellGrid.TilesPerCell, roads.BlockTiles);

        // Zero since 2026-08-13, and the file says at length why: an Arterial is a player tool that
        // adr/0077 refuses in the command and adr/0090 keeps out of the generator, it grants no
        // frontage so it can only take Lots away, and the 240-configuration sweep measured these
        // eight severing 0.0% on this lattice. Severance is demonstrated by rulesets/severance.toml.
        Assert.Equal(0, roads.ArterialCount);
        Assert.Equal(512, roads.ArterialJunctionTiles);
        Assert.Equal(4, roads.FootCrossingEvery);
        Assert.Equal(40, roads.FootPathsPerThousandBlocks);

        Assert.Equal(Speed.FromKilometresPerHour(50), roads.StreetSpeed);
        Assert.Equal(Speed.FromKilometresPerHour(90), roads.ArterialSpeed);
        Assert.Equal(Speed.FromKilometresPerHour(5), roads.WalkSpeed);

        Assert.Equal(3_600 * 24, roads.StreetCapacityPerDay);
        Assert.Equal(12_000 * 24, roads.ArterialCapacityPerDay);
        Assert.Equal(1_000 * 24, roads.FootPathCapacityPerDay);
    }
}
