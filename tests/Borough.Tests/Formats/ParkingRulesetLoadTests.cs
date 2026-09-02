using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// Milestone 7 task 2: the <c>[parking]</c> table and every refusal it states.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every refusal has a test that writes the malformed Ruleset and watches it fire</b>, on
/// <see cref="TrafficRulesetLoadTests"/>' discipline and for <c>adr/0064</c>'s reason.
/// </para>
/// <para>
/// <b>The key carries its denomination in its name</b> — <c>radius_metres</c> rather than
/// <c>radius</c> — for <c>alpha_percent</c>'s reason: nothing in this file distinguishes 400 metres
/// from 400 Tiles, and the second is a shed sixteen times the intended area with no symptom that
/// names the key. ***The name of a quantity is not its denomination***, so the name carries it.
/// </para>
/// <para>
/// ⚠ <b>Metres were chosen over minutes and the tests hold the consequence rather than the
/// choice.</b> <see cref="A_shed_is_the_same_size_however_fast_anybody_walks"/> is the property a key
/// in minutes would not have had, and <see cref="A_shed_wider_than_the_budget_is_refused"/> is the
/// bound minutes would have made free. Both matter; the second is enforceable in either unit and the
/// first is not.
/// </para>
/// </remarks>
public sealed class ParkingRulesetLoadTests
{
    /// <summary>The smallest complete Ruleset. Nothing here needs a Rule or a road to exist.</summary>
    private const string Nothing = """
        [[resource]]
        name = "flour"
        family = "good"
        """;

    /// <summary>A <c>[roads]</c> table, for the tests that need a walking speed to convert through.</summary>
    private const string Streets = """
        [roads]
        block_tiles = 32
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
    /// A <c>[parking]</c> table around <paramref name="body"/>, completed with the key the case under
    /// test is not about.
    /// </summary>
    /// <remarks>
    /// <b><c>shed_keeps</c> is supplied unless the body states it</b>, so that a test about the radius
    /// stays a test about the radius. The alternative — writing both keys into every fixture — makes
    /// each one assert two things, and the day a third required key arrives every unrelated test in the
    /// file fails at once and says nothing about why.
    /// </remarks>
    private static string With(string body) =>
        $"{Nothing}\n\n[parking]\n{body}"
        + (body.Contains("shed_keeps", StringComparison.Ordinal) ? string.Empty : "\nshed_keeps = 24");

    // ---- the absent table -------------------------------------------------------------------------

    /// <summary>
    /// A Ruleset with no <c>[parking]</c> has no Parking Shed, and that is a statement.
    /// </summary>
    /// <remarks>
    /// <b>Absence is the unset spelling</b>, on <c>[households]</c>' rule: every radius in range means
    /// something — a small one is a city of driveways, a large one a city of long walks — so no value
    /// inside the range can do duty as <em>unset</em>. It is also every city this project described
    /// before milestone 7, so omission is behaviour-preserving rather than a placeholder.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_parking_table_has_no_shed()
    {
        Ruleset ruleset = Accepted(Nothing);

        Assert.Equal(ParkingRuleset.None, ruleset.Parking);
        Assert.False(ruleset.Parking.Runs);
    }

    /// <summary>A <c>[parking]</c> table carries the radius through to the core.</summary>
    [Fact]
    public void A_parking_table_carries_the_radius()
    {
        Ruleset ruleset = Accepted(With("radius_metres = 400"));

        Assert.True(ruleset.Parking.Runs);
        Assert.Equal(400, ruleset.Parking.RadiusMetres);
    }

    /// <summary>
    /// <b>The radius is stored in Tiles and rounded up</b>, because a radius is a <em>reach</em>.
    /// </summary>
    /// <remarks>
    /// Rounding down would give a shed silently shorter than its file says — supply the city has and
    /// cannot find, which is the failure mode parking has instead of a crash. <c>CellGrid.FromMetres</c>
    /// rounds up for this reason at the coarser unit; a Tile is 4 m, so the overshoot here is at most
    /// three metres against that method's 127.
    /// </remarks>
    [Theory]
    [InlineData(400, 100)]
    [InlineData(401, 101)]
    [InlineData(403, 101)]
    [InlineData(404, 101)]
    [InlineData(1, 1)]
    public void The_radius_is_tiles_rounded_up(int metres, int tiles)
    {
        Ruleset ruleset = Accepted(With($"radius_metres = {metres}"));

        Assert.Equal(new Tiles(tiles), ruleset.Parking.Radius);
    }

    // ---- the refusals -----------------------------------------------------------------------------

    /// <summary>A <c>[parking]</c> table that states no radius is refused.</summary>
    /// <remarks>
    /// <b>Required inside an optional table</b>, which is <c>[households]</c>' and <c>[traffic]</c>'s
    /// shape: the table is how a designer says <em>this city has sheds</em>, so a table saying so and
    /// then declining to say how big is half a sentence.
    /// </remarks>
    [Fact]
    public void A_parking_table_with_no_radius_is_refused()
    {
        RulesetRefusal refusal = Refused(With("# nothing at all"));

        Assert.Contains("radius_metres", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A radius of zero or less is refused rather than read as <em>no parking</em>.
    /// </summary>
    /// <remarks>
    /// <b>Zero means something different here from what it means one key over</b>, and that is why it
    /// cannot be a default. A kind omitting <c>parked</c> is a real declaration — a tower with no
    /// parking, <c>adr/0009</c>'s own second player-tool row — where <c>radius_metres = 0</c> is a
    /// city whose Car Parks all exist and none can be walked to from anywhere. One is a balance
    /// decision and the other is a sentence nobody meant to write. ⚠ <b>The neighbour used to be
    /// <c>[[building]] parking = 0</c></b>; <c>plans/0053</c> step 3 split it into a truth key and a
    /// rate, and the asymmetry this remark is about survived the split intact.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-400)]
    public void A_radius_with_no_reach_is_refused(int metres)
    {
        RulesetRefusal refusal = Refused(With($"radius_metres = {metres}"));

        Assert.Contains($"radius_metres is {metres}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("finds nothing", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>A second <c>[parking]</c> is refused rather than merged.</summary>
    /// <remarks>
    /// <b>Written as an array of tables, which is <see cref="JobRulesetLoadTests"/>' shape and
    /// not a curiosity.</b> <c>[parking]</c> stated twice is caught by the TOML parser itself and
    /// never reaches the loader, so a test written the obvious way asserts the parser's message
    /// and leaves this guard untested — the exact shape of <c>adr/0064</c>'s finding, where the
    /// one guard with no test was later re-derived as absent from the shape of its absence.
    /// </remarks>
    [Fact]
    public void A_second_parking_table_is_refused()
    {
        RulesetRefusal refusal = Refused($"""
            {Nothing}

            [[parking]]
            radius_metres = 400

            [[parking]]
            radius_metres = 800
            """);

        Assert.Contains("a second [parking]", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>The unknown-section message names <c>[parking]</c>.</b>
    /// </summary>
    /// <remarks>
    /// <b>The reason this task touches that sentence at all.</b> It enumerates the sections in prose,
    /// so it is wrong the moment a twelfth is added and nothing but a test notices — a designer who
    /// mistypes <c>[parkng]</c> is told what the sections are by a list that has stopped being one.
    /// </remarks>
    [Fact]
    public void The_unknown_section_message_names_every_section()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[parkng]\nradius_metres = 400");

        Assert.Contains("[parking]", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("[traffic]", refusal.Reason, StringComparison.Ordinal);
    }

    // ---- the bound the radius has, and does not enforce ----------------------------------------

    /// <summary>
    /// ⚠ <b>A shed wider than the whole Commute Budget loads, and that is a decision rather than
    /// a gap.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0083</c> states one constraint on this number — a shed wider than a Trip can afford
    /// to walk has outer Car Parks that <em>can never be taken</em> — and <b>a guard for it was
    /// written during this task and withdrawn</b>. The Budget is a ceiling on a <b>whole journey</b>
    /// and a parking walk is one <b>Leg</b> inside it, so the only non-arbitrary threshold a loader
    /// could draw is the whole Budget — far looser than the real constraint, and loose enough that
    /// it refuses nothing a designer would plausibly write.
    /// </para>
    /// <para>
    /// <b>What it did refuse was five test fixtures.</b> The <c>WithCeiling</c> idiom takes
    /// <c>rulesets/minimal.toml</c> and drops the Budget to 3, 5 or 10 minutes to force
    /// budget-exceeded behaviour; at the shipped 400 m the guard failed every ceiling of 3 and
    /// cleared 5 by 0.2 minutes. ***A bound stated as a constraint on choosing a number is not
    /// thereby a predicate over two files*** — and one whose margin is a rounding error at a
    /// fixture's chosen value is a guard that will keep biting authors who were editing something
    /// else.
    /// </para>
    /// <para>
    /// <b>This test is the record, so the withdrawal cannot be mistaken for an oversight.</b> The
    /// bound lives in <c>plans/0002</c> §D2 and in <c>minimal.toml</c>'s header, where
    /// <c>adr/0083</c> put it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_shed_wider_than_the_budget_loads_and_the_bound_is_prose()
    {
        // 40 km of shed against a three-minute journey: incoherent, and accepted.
        Ruleset ruleset = Accepted($"""
            {Nothing}

            {Streets}

            [trips]
            crossing_seconds = 30
            commute_fast_minutes = 1
            commute_moderate_minutes = 2
            commute_budget_minutes = 3

            [parking]
            radius_metres = 40000
            shed_keeps = 24
            """);

        Assert.Equal(40_000, ruleset.Parking.RadiusMetres);
    }

    /// <summary>
    /// <b>A shed is the same set of Car Parks however fast anybody walks.</b>
    /// </summary>
    /// <remarks>
    /// <b>The property that decided the unit, asserted directly.</b> Had the radius been authored in
    /// minutes, retuning <c>walk_speed_kph</c> would have moved shed membership everywhere in the city
    /// — a designer making people walk faster would have silently enlarged every Building's parking,
    /// with no key in the file changed and every hash moving. In metres the reach is geometry, and the
    /// walking speed prices the Leg without deciding which Car Parks are in reach of it.
    /// </remarks>
    [Fact]
    public void A_shed_is_the_same_size_however_fast_anybody_walks()
    {
        string slow = Streets;
        string fast = Streets.Replace(
            "walk_speed_kph = 5", "walk_speed_kph = 9", StringComparison.Ordinal);

        Tiles atFive = Accepted($"{Nothing}\n\n{slow}\n\n[parking]\nradius_metres = 400\nshed_keeps = 24")
            .Parking.Radius;
        Tiles atNine = Accepted($"{Nothing}\n\n{fast}\n\n[parking]\nradius_metres = 400\nshed_keeps = 24")
            .Parking.Radius;

        Assert.Equal(atFive, atNine);
        Assert.Equal(new Tiles(100), atFive);
    }

    // ---- the cap ----------------------------------------------------------------------------------

    /// <summary>A <c>[parking]</c> table carries the cap through to the core.</summary>
    [Fact]
    public void A_parking_table_carries_the_shed_cap()
    {
        Ruleset ruleset = Accepted(With("radius_metres = 400\nshed_keeps = 24"));

        Assert.Equal(24, ruleset.Parking.ShedKeeps);
        Assert.Equal(24, ruleset.Parking.Keeps);
    }

    /// <summary>A <c>[parking]</c> table that states no cap is refused.</summary>
    /// <remarks>
    /// <b>Required rather than defaulted, for <c>radius_metres</c>' reason and one of its own.</b> A
    /// default would have to be a number, the number would be hash-bearing at milestone 7 task 4, and
    /// it would arrive in every Ruleset that never mentioned it — which is a hash-bearing number
    /// chosen by omission, the case <c>adr/0052</c> exists to prevent.
    /// </remarks>
    [Fact]
    public void A_parking_table_with_no_cap_is_refused()
    {
        RulesetRefusal refusal = Refused($"{Nothing}\n\n[parking]\nradius_metres = 400");

        Assert.Contains("shed_keeps", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// A cap of zero or less is refused rather than read as <em>keep everything</em>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Zero is not merely meaningless here, it is the one value that silently buys the exhaustive
    /// ball.</b> <c>ParkingShed</c>'s early exit fires on a <em>full</em> kept set, and a kept set of
    /// zero is never full — so <c>shed_keeps = 0</c> would load, find nothing, and walk the whole
    /// radius doing it, at roughly three and a half times the cost of a working shed.
    /// <b>A performance cliff must not be reachable by a value that reads as <em>off</em>.</b>
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-24)]
    public void A_shed_that_keeps_nothing_is_refused(int keeps)
    {
        RulesetRefusal refusal = Refused(With($"radius_metres = 400\nshed_keeps = {keeps}"));

        Assert.Contains($"shed_keeps is {keeps}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("finds nothing", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>An absurd cap is refused, and the ceiling is arithmetic rather than taste.</b>
    /// </summary>
    /// <remarks>
    /// <c>[traffic] beta</c>'s precedent. A shed is materialised for <em>every Building</em> at this
    /// width, so the key multiplies the whole city rather than one query: at the 1,000,000-Citizen
    /// target's 84,320 Buildings, 24 is 8.0 MiB and 4,096 would be 1.3 GiB. What is refused is the rung
    /// where a plausible typo stops being a tuning choice and becomes an allocation failure carrying no
    /// line number.
    /// </remarks>
    [Theory]
    [InlineData(1025)]
    [InlineData(4096)]
    public void A_shed_cap_past_any_measured_radius_is_refused(int keeps)
    {
        RulesetRefusal refusal = Refused(With($"radius_metres = 400\nshed_keeps = {keeps}"));

        Assert.Contains($"shed_keeps is {keeps}", refusal.Reason, StringComparison.Ordinal);
        Assert.Contains("per-city cost", refusal.Reason, StringComparison.Ordinal);
    }

    /// <summary>The largest accepted cap loads, so the boundary is tested from both sides.</summary>
    [Fact]
    public void The_largest_shed_cap_loads()
    {
        Assert.Equal(1024, Accepted(With("radius_metres = 400\nshed_keeps = 1024")).Parking.ShedKeeps);
    }

    /// <summary>
    /// <b>The reach and the supply are separate keys, and a file may state either without the
    /// other.</b>
    /// </summary>
    /// <remarks>
    /// <c>[capacity] floor_tiles_per_parking_space</c> is how many Vehicles a Building holds — how
    /// much floor one space takes, divided into the floor a Building has; <c>[parking]
    /// radius_metres</c> is how far somebody will walk from one. A file with supply and no radius is a
    /// city whose parking exists and cannot be reached, and a file with a radius and no supply is a
    /// shed with nothing in it. <b>Both load</b>, because a designer retunes them for separate reasons
    /// and refusing the pair would make the two one key wearing two names.
    /// </remarks>
    [Fact]
    public void The_reach_and_the_supply_are_independent()
    {
        const string Housing = """
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "dwelling"

            [capacity]
            floor_tiles_per_parking_space = 6
            """;

        Ruleset supplyOnly = Accepted(Housing);

        Assert.Equal(6, supplyOnly.Capacity.FloorTilesPerParkingSpace);
        Assert.False(supplyOnly.Parking.Runs);

        Ruleset reachOnly = Accepted(With("radius_metres = 400"));

        Assert.True(reachOnly.Parking.Runs);
    }
}
