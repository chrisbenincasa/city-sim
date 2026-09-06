using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Determinism;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// <b>Insolvency: the consequence a failed payday never had.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>plans/0065</c>. Before this, a Business could take in less than it paid out at every payday for
/// the life of the world and the only trace was a readout counter that survived one Tick.
/// </para>
/// <para>
/// 🔴 <b>Losing your premises and going bankrupt are two different failures</b>, and the first draft
/// of the mechanism gave them one verb. A trade between premises is solvent and waits in the
/// Unpremised Pool; ***a bankrupt one is wound up***. These assert the second and leave the first
/// alone.
/// </para>
/// </remarks>
public sealed class InsolvencyTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const int Population = 2_000;

    [Fact]
    public void A_trade_that_cannot_pay_its_staff_is_eventually_wound_up()
    {
        (World world, Simulation simulation) = Start();

        int before = LiveBusinesses(world);
        int bankrupted = 0;
        int shortest = int.MaxValue;

        for (int tick = 0; tick < 120 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if (simulation.LastPayroll.Bankrupted > 0)
            {
                bankrupted += simulation.LastPayroll.Bankrupted;
                shortest = int.Min(shortest, tick);
            }
        }

        _output.WriteLine(
            $"{before} Businesses at the start, {LiveBusinesses(world)} at the end, "
            + $"{bankrupted} wound up, first at Tick {shortest}.");

        Assert.True(bankrupted > 0, "nothing ever went bankrupt, so this test asserts nothing.");

        // The money that was in a wound-up till left the supply with it. adr/0142's conservation
        // check is what would catch a bankruptcy that quietly minted or destroyed money.
        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>The count is CONSECUTIVE, so a payroll met in full is a recovery and not a slower decline.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Without the reset this is <c>adr/0006</c> read backwards</b>: the collection would not
    /// grow, but the population would drain with elapsed time — every Business in the city retired
    /// eventually, on a long enough run, whatever its trade.
    /// </remarks>
    [Fact]
    public void A_payroll_met_in_full_resets_the_count()
    {
        (World world, Simulation simulation) = Start();

        for (int tick = 0; tick < 60 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        bool anySolvent = false;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = world.Businesses.Kind[slot];

            if (kind == 0 || kind > world.Rules.BusinessKindCount)
            {
                continue;
            }

            int threshold = world.Rules.BusinessKind(kind).GoesBankruptAfterShortPaydays;

            // Nothing live may sit at or above its own threshold: the sweep winds it up on the payday
            // it crosses. A row above it would mean the consequence had been skipped.
            if (threshold > 0)
            {
                Assert.True(
                    world.Businesses.ShortPaydays[slot] < threshold,
                    $"business slot {slot} is live at {world.Businesses.ShortPaydays[slot]} short "
                    + $"paydays against a threshold of {threshold}.");
            }

            anySolvent |= world.Businesses.ShortPaydays[slot] == 0;
        }

        Assert.True(anySolvent, "no live Business had a clean payroll, so recovery is untested.");
    }

    /// <summary>
    /// <b>A bankruptcy empties a Building; it does not demolish one.</b>
    /// </summary>
    [Fact]
    public void The_premises_are_left_standing()
    {
        (World world, Simulation simulation) = Start();

        int buildings = world.Buildings.Rows.LiveCount;
        int wound = 0;

        for (int tick = 0; tick < 120 * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
            wound += simulation.LastPayroll.Bankrupted;
        }

        _output.WriteLine(
            $"{wound} wound up; Buildings {buildings} -> {world.Buildings.Rows.LiveCount}.");

        Assert.True(wound > 0, "nothing went bankrupt, so this asserts nothing.");
    }

    private static int LiveBusinesses(World world) => world.Businesses.Rows.LiveCount;

    private static (World World, Simulation Simulation) Start()
    {
        RulesetLoadResult parsed = RulesetLoader.Parse(
            File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Rulesets", "insolvent.toml")),
            "insolvent.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, parsed.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        return (world, simulation);
    }
}
