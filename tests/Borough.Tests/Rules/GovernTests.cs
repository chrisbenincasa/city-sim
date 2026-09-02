using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>01 §2</c>'s fourth verb — <b>the first command whose effect outlives the Tick it was issued
/// on.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Every other verb writes the world and is done.</b> <c>Zone</c> paints a Lot, <c>Connect</c>
/// lays a Segment, <c>Demolish</c> clears one. <c>Govern</c> sets a parameter that a Sweep Rule then
/// reads on every trigger for the rest of the run, which is why it is the first verb to need a table
/// of its own rather than a write into one that already existed.
/// </para>
/// <para>
/// ⚠ <b>The two tests that matter here are the reload ones</b>, because they are the only place the
/// design is visible. A governed amount that simply overwrote the Ruleset would pass the first test
/// and fail both of those, and the failure would be silent in every world that never reloads.
/// </para>
/// </remarks>
public sealed class GovernTests
{
    // 500 rather than the 2,000 PolicyTests uses, and Ticks.PerDay + 1 rather than two whole Days:
    // this class asserts EQUIVALENCES between cities, so it needs enough Households for a levy to
    // separate two amounts and two triggers to prove the second one still obeys. Neither is a
    // measurement, and the suite is the default working loop -- see adr/0121.
    private const int Citizens = 500;

    /// <summary>
    /// 🔴 <b>A governed Policy is indistinguishable from one the designer declared that way.</b>
    /// </summary>
    /// <remarks>
    /// <b>Stated as an equivalence rather than as an arithmetic</b>, and deliberately: what one levy
    /// collects over two Days depends on how many Households are live, how many can afford it and how
    /// often the interval divides the Tick, and asserting a computed total would be testing those
    /// three things instead of this one. ***Two cities differing only in HOW the amount was set must
    /// collect the same money.***
    /// </remarks>
    [Fact]
    public void Governing_replaces_the_amount_the_designer_declared()
    {
        Assert.Equal(Collected(declared: 25), Collected(declared: 10, governTo: 25));
    }

    /// <summary>And the equivalence above is not vacuous — the amount does drive the take.</summary>
    /// <remarks>
    /// ⚠ <b>Without this, every equality in this class would pass against an engine that ignored the
    /// amount entirely</b> and collected the same money whatever anybody set.
    /// </remarks>
    [Fact]
    public void The_amount_is_what_decides_the_take()
    {
        Assert.NotEqual(Collected(declared: 25), Collected(declared: 10));
    }

    /// <summary>The verb itself reaches the table, which every equivalence above assumes.</summary>
    [Fact]
    public void The_command_reaches_the_table()
    {
        (World world, Simulation simulation) = City(Levy("levy", 10));

        Govern(simulation, policy: 0, amount: 25);

        Assert.Equal(1, world.Policies.Governed[0]);
        Assert.Equal(25, world.Policies.Amount[0]);
    }

    /// <summary>
    /// <b>A reload still retunes a Policy nobody has governed</b> — <c>adr/0015</c>'s acceptance test.
    /// </summary>
    [Fact]
    public void A_reload_retunes_an_ungoverned_policy()
    {
        Assert.Equal(Collected(declared: 40), Collected(declared: 10, reloadTo: 40));
    }

    /// <summary>
    /// 🔴 <b>And it does NOT retune a governed one, which is the whole reason the flag exists.</b>
    /// </summary>
    /// <remarks>
    /// Without <c>PolicyTable.Governed</c> the designer's edit and the player's decision are the same
    /// column and one of them has to lose silently. <b>This is the test that fails if somebody
    /// "simplifies" the table to a single amount seeded from the Ruleset</b> — the reload would
    /// overwrite the player's 25 with the designer's 40 and nothing else here would notice.
    /// </remarks>
    [Fact]
    public void A_reload_does_not_overrule_a_governed_policy()
    {
        Assert.Equal(Collected(declared: 25), Collected(declared: 10, governTo: 25, reloadTo: 40));
    }

    /// <summary>
    /// 🔴 <b>A governed amount follows its Policy's NAME through a reorder, not its index.</b>
    /// </summary>
    /// <remarks>
    /// <b>The defect this is written against</b>: govern the first of two Policies, then reload a file
    /// that declares them the other way round. Keyed by index the player's figure would land on the
    /// Policy they did not touch; keyed by name it follows the one they did. ⚠ <b>Both Policies are
    /// levies of the same shape</b>, so nothing but the name distinguishes them and an index-keyed
    /// implementation cannot pass by luck.
    /// </remarks>
    [Fact]
    public void A_governed_amount_follows_its_name_through_a_reorder()
    {
        (World world, Simulation simulation) = City(Levy("first", 10) + Levy("second", 10));

        Govern(simulation, policy: 0, amount: 25);

        world.Adopt(
            Parse(Levy("second", 10) + Levy("first", 10)), 0, Ticks.Zero, WorldKey.FromSeed(Seed));

        Assert.Equal(0, world.Policies.Governed[0]);
        Assert.Equal(1, world.Policies.Governed[1]);
        Assert.Equal(25, world.Policies.Amount[1]);
    }

    /// <summary>A governed Policy the designer has deleted loses its amount rather than moving.</summary>
    [Fact]
    public void A_governed_amount_whose_policy_is_gone_is_dropped()
    {
        (World world, Simulation simulation) = City(Levy("levy", 10));

        Govern(simulation, policy: 0, amount: 25);

        world.Adopt(Parse(Levy("renamed", 10)), 0, Ticks.Zero, WorldKey.FromSeed(Seed));

        Assert.Equal(0, world.Policies.Governed[0]);
    }

    /// <summary>An unnameable Policy is refused, because its row could not survive a reload.</summary>
    [Fact]
    public void Governing_a_policy_with_no_name_is_refused()
    {
        (World _, Simulation simulation) = City($$"""

            [[policy]]
            sweeps = "household"
            interval = {{Ticks.PerDay}}
            apply = { min = 1, max = 1 }
            transfer = { from = "local", to = "global", resource = "money", amount = 10 }
            """);

        InvalidOperationException refused =
            Assert.Throws<InvalidOperationException>(() => Govern(simulation, 0, 25));

        Assert.Contains("states no name", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>And so is a Policy this Ruleset does not declare.</summary>
    [Fact]
    public void Governing_a_policy_the_ruleset_does_not_declare_is_refused()
    {
        (World _, Simulation simulation) = City(Levy("levy", 10));

        InvalidOperationException refused =
            Assert.Throws<InvalidOperationException>(() => Govern(simulation, 7, 25));

        Assert.Contains("declares 1", refused.Message, StringComparison.Ordinal);
    }

    private const int Seed = 20_260_830;

    private static string Levy(string name, int amount) => $$"""

        [[policy]]
        name = "{{name}}"
        sweeps = "household"
        interval = {{Ticks.PerDay}}
        apply = { min = 1, max = 1 }
        transfer = { from = "local", to = "global", resource = "money", amount = {{amount}} }
        """;

    private static Ruleset Parse(string policies)
    {
        RulesetLoadResult result = RulesetLoader.Parse(Endowed + policies, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static (World World, Simulation Simulation) City(string policies)
    {
        var key = WorldKey.FromSeed(Seed);
        var world = new World(Citizens, Parse(policies), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>What the treasury holds after a fixed run, with the amount set however asked.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Nothing here spends a Tick before the run starts, and that is load-bearing.</b> A Policy's
    /// interval divides Tick 0, so <em>every</em> Policy fires on the first Tick of a run — a city
    /// governed or reloaded after one Step has already collected one trigger at the old amount, and
    /// comparing it with a city that never had an old amount measures that trigger rather than the
    /// verb. ***This cost a red test to find, and the arithmetic version of it would have hidden the
    /// reason.***
    /// </para>
    /// <para>
    /// <b>So the governed amount is written straight into the table here</b>, and the command path
    /// that writes it is covered by <see cref="The_command_reaches_the_table"/> and the two refusals.
    /// </para>
    /// </remarks>
    private static long Collected(int declared, int? governTo = null, int? reloadTo = null)
    {
        (World world, Simulation simulation) = City(Levy("levy", declared));

        if (governTo is not null)
        {
            world.Policies.Govern(0, governTo.Value);
        }

        if (reloadTo is not null)
        {
            world.Adopt(
                Parse(Levy("levy", reloadTo.Value)), 0, Ticks.Zero, WorldKey.FromSeed(Seed));
        }

        Step(simulation, Ticks.PerDay + 1);

        return Treasury(world).Raw;
    }

    private static void Govern(Simulation simulation, int policy, int amount)
    {
        Command[] commands = [Command.Govern(policy, amount)];

        simulation.Step(new TickInput(commands, 0));
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    private static int Households(World world)
    {
        int live = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                live++;
            }
        }

        return live;
    }

    private static Money Treasury(World world)
    {
        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        return new Money(world.Bins.LevelAt(bins[0]));
    }

    private const string Endowed = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = 3
        bins = [ { resource = "sundries", capacity = 48 } ]

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
}
