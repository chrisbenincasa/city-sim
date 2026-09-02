using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>adr/0103</c>'s accumulator — <b>the first thing in this design that a Household remembers.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>A Need is a relative scalar where 0 is ideal and negative values are deficit</b>
/// (<c>CONTEXT.md</c> → Need, <c>04 §2</c>), and <c>04 §6</c> step 6 is the whole mechanism: it
/// <em>"falls while unmet and recovers when met, so a dry afternoon and a dry month are one mechanism
/// at two depths, and nothing else in this chain has to remember anything."</em>
/// </para>
/// <para>
/// ⚠ <b>Two of the four Needs are absent on purpose.</b> <c>adr/0103</c> calls a degradation rule for
/// Education and Health <em>"owed and deliberately undesigned"</em> — <em>undesigned</em> and not
/// <em>refused</em> (<c>adr/0070</c>), so ***nothing here is evidence about them***.
/// </para>
/// <para>
/// 🔴 <b>And nothing READS a Need yet.</b> The reader is <c>adr/0102</c>'s housed Departure and
/// <c>02 §5.4</c>'s aggregation form is open. These assert the accumulator and nothing else.
/// </para>
/// </remarks>
public sealed class NeedTests
{
    private const int Citizens = 500;
    private const int Seed = 20_260_830;

    /// <summary>A Household whose larder nothing fills goes hungry.</summary>
    [Fact]
    public void A_failed_occasion_degrades_sustenance()
    {
        World world = Run(Starving, (Ticks.PerDay * 2) + 1);

        Assert.True(Deepest(world) < 0, "no Household went short on a world with no restock.");
    }

    /// <summary>And one whose Rules fire never does.</summary>
    /// <remarks>
    /// <b>The control.</b> Without it every assertion here would pass against an engine that
    /// degraded a Need every Tick regardless of failure.
    /// </remarks>
    [Fact]
    public void A_met_occasion_leaves_sustenance_at_the_ideal()
    {
        World world = Run(Fed, (Ticks.PerDay * 2) + 1);

        Assert.Equal(0, Deepest(world));
    }

    /// <summary>
    /// 🔴 <b>The floor holds, and it is <c>adr/0006</c> that makes this the load-bearing test.</b>
    /// </summary>
    /// <remarks>
    /// A Need that fell for ever is a magnitude trending downward at steady state, which
    /// <c>adr/0003</c> extends <c>adr/0006</c> to cover. ⚠ <b>It would fail in a later milestone for
    /// a reason nobody would look here for</b>, so the bound is asserted rather than left to the run.
    /// </remarks>
    [Fact]
    public void Sustenance_never_falls_below_the_floor()
    {
        World world = Run(Starving, Ticks.PerDay * 24);

        Assert.True(Deepest(world) >= Floor, $"a Household reached {Deepest(world)}, past {Floor}.");
    }

    /// <summary>And never rises above the ideal, however well fed.</summary>
    /// <remarks>
    /// <b>0 is ideal rather than a starting point</b> — a Household cannot bank surplus meals, which
    /// is <c>04 §2</c>'s <em>Goods are absolute; Needs are relative</em>. The stockpile is the Bin.
    /// </remarks>
    [Fact]
    public void Sustenance_never_rises_above_the_ideal()
    {
        World world = Run(Fed, Ticks.PerDay * 2);

        Assert.True(Highest(world) <= 0, $"a Household reached {Highest(world)}, above the ideal.");
    }

    /// <summary>
    /// 🔴 <b>Twice as long unmet is twice as deep — the property the first build did not have.</b>
    /// </summary>
    /// <remarks>
    /// <b>Every other test in this class passes against a TALLY</b>, which is what shipped first: the
    /// Need moved on a failed occasion, and <c>RuleEngine.Stop</c> sleeps a blocked Rule on its Bin, so
    /// a shortage nothing ends bought one step and then silence. ***A dry afternoon and a dry month
    /// read the same depth***, which is the one thing <c>04 §6</c> step 6 forbids. A tally makes this
    /// assertion an equality.
    /// </remarks>
    [Fact]
    public void The_depth_is_a_duration_and_not_a_tally()
    {
        int near = Deepest(Run(Deep, (Ticks.PerDay * 3) + 1));
        int far = Deepest(Run(Deep, (Ticks.PerDay * 6) + 1));

        Assert.True(near < 0, "nobody went short in three Days.");
        Assert.True(far <= near * 2, $"six Days reached {far}, not twice three Days' {near}.");
    }

    /// <summary>A Ruleset stating no <c>[needs]</c> has no Needs at all.</summary>
    /// <remarks>
    /// ⚠ <b>Reached by omitting the table rather than by zeroing its keys</b>, which is
    /// <c>[market]</c>'s and <c>[traffic]</c>'s shape. Every Ruleset shipped before milestone 28 is
    /// such a city, so this is the behaviour twelve files depend on.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_needs_table_moves_nothing()
    {
        World world = Run(Starving.Replace(NeedsTable, string.Empty, StringComparison.Ordinal),
            (Ticks.PerDay * 2) + 1);

        Assert.Equal(0, Deepest(world));
    }

    /// <summary>
    /// 🔴 <b>Education and Health are still refused BY NAME, and the message now says where to
    /// go.</b>
    /// </summary>
    /// <remarks>
    /// <b>The refusal survived and its reason did not.</b> It used to end <em>"its degradation rule
    /// is owed and DELIBERATELY UNDESIGNED"</em>, and <c>docs/deferred.md</c> named the exact thing
    /// that would end that — <em>a civic Building a Household draws on</em>. <c>ServiceEngine</c> is
    /// it, so the key is refused here for the same reason as ever, which is that a Resource is the
    /// wrong door, and the message names the right one. ***A refusal that points somewhere is a
    /// different sentence from one that only says no.***
    /// </remarks>
    [Fact]
    public void A_need_with_no_good_behind_it_is_refused_by_name()
    {
        RulesetLoadResult result = Parse(Starving.Replace(
            "need = \"sustenance\"", "need = \"education\"", StringComparison.Ordinal));

        Assert.False(result.Ok);
        Assert.Contains("ATTENDING", result.Describe(), StringComparison.Ordinal);
        Assert.Contains("serves", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A floor at or above zero is refused, because it would mean nobody can go short.</summary>
    [Fact]
    public void A_floor_that_is_not_a_deficit_is_refused()
    {
        RulesetLoadResult result = Parse(Starving.Replace(
            $"floor = {Floor}", "floor = 0", StringComparison.Ordinal));

        Assert.False(result.Ok);
        Assert.Contains("deepest deficit", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>And a step of zero, which is the table omitted written the long way.</summary>
    [Fact]
    public void A_step_that_does_not_move_is_refused()
    {
        RulesetLoadResult result = Parse(Starving.Replace(
            "sustenance_degrade = 4", "sustenance_degrade = 0", StringComparison.Ordinal));

        Assert.False(result.Ok);
        Assert.Contains("sustenance_degrade", result.Describe(), StringComparison.Ordinal);
    }

    private const int Floor = -40;

    private static int Deepest(World world) => Extreme(world, deepest: true);

    private static int Highest(World world) => Extreme(world, deepest: false);

    private static int Extreme(World world, bool deepest)
    {
        int found = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            int value = world.Households.Sustenance[slot];

            if (deepest ? value < found : value > found)
            {
                found = value;
            }
        }

        return found;
    }

    private static RulesetLoadResult Parse(string text) =>
        RulesetLoader.Parse(text, "test.toml");

    private static World Run(string text, int ticks)
    {
        RulesetLoadResult result = Parse(text);

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, result.Ruleset!, key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }

    private const string NeedsTable = """
        [needs]
        sustenance_degrade = 4
        sustenance_recover = 1
        satisfaction_degrade = 1
        satisfaction_recover = 1
        floor = -40

        """;

    /// <summary>A world whose tenants consume and whose larder nothing refills.</summary>
    private static readonly string Starving = City(restocks: false);

    /// <summary>The same world with a producer, so every occasion is met.</summary>
    private static readonly string Fed = City(restocks: true);

    /// <summary>Starving, with a floor too deep to reach — so the depth is the duration alone.</summary>
    private static readonly string Deep =
        Starving.Replace($"floor = {Floor}", "floor = -100000", StringComparison.Ordinal);

    private static string City(bool restocks) => NeedsTable + $$"""
        [[resource]]
        name = "sundries"
        family = "good"
        need = "sustenance"

        [[resource]]
        name = "money"
        family = "money"

        [[rule]]
        name    = "consume"
        kind    = "dwelling"
        rate    = 32
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "sundries", amount = 4 } ]
        outputs = []
        {{(restocks ? Restock : string.Empty)}}
        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [ { resource = "sundries", capacity = 48, owner = "occupant" } ]

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

        [lots]
        lots_per_segment = 5
        setback_tiles = 2

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000
        """;

    private const string Restock = """

        [[rule]]
        name    = "restock"
        kind    = "dwelling"
        rate    = 16
        apply   = { min = 1, max = 1 }
        inputs  = []
        outputs = [ { scope = "local", resource = "sundries", amount = 8 } ]
        """;
}
