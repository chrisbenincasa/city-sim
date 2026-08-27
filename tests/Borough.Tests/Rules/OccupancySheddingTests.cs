using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 17 task 3: <c>CONTEXT.md</c> → Failure Pressure's <b>first</b> threshold, which sheds
/// occupancy where the second abandons.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>What this buys is the only negative feedback loop in the build, and the assertions are shaped
/// by that rather than by the key.</b> A premises Rule whose <c>apply</c> is
/// <c>{ derived = "occupancy" }</c> demands less when there are fewer Occupants, so shedding one
/// <em>lowers the demand that caused the shedding</em>. At zero Occupants the band is <c>(0,0)</c>,
/// the Rule fires with zero applications, and <c>RuleEngine.Fire</c> clears <c>StarvedSince</c> — so
/// the Building recovers outright. ***A city that could only ever lose stock now has a way back.***
/// </para>
/// <para>
/// ⚠ <b>The pacing is the part most likely to be broken by a later edit, so it is asserted twice.</b>
/// The sweep runs every <c>[placement] interval</c> Ticks — 32 on every shipped Ruleset — and the
/// threshold is Days. A spelling that shed one Occupant per sweep rather than per elapsed multiple
/// would empty a dwelling in about ninety Ticks and every count below would still be non-zero.
/// ***A mechanism paced by the cadence instead of the clock passes every test that only asks whether
/// it happened.***
/// </para>
/// <para>
/// <b>The clock is deliberately NOT reset on a shed</b>, and
/// <see cref="Shedding_does_not_reset_the_march_toward_condemnation"/> is why: resetting it would also
/// reset progress toward <c>condemn_after_days</c>, so a kind stating both keys would shed for ever
/// and the second threshold would be dead code in every world that used the first.
/// </para>
/// </remarks>
public sealed class OccupancySheddingTests
{
    private static readonly ResourceId Repairs = new(1);

    private const byte House = 1;
    private const ushort Housing = 1;

    /// <summary>How long the starving Rule waits between firings.</summary>
    private const uint Rate = 8;

    /// <summary>How many Households the kind declares room for.</summary>
    private const int Occupants = 4;

    /// <summary>
    /// A kind whose one Rule draws on a Bin nothing fills, so the premises always starve, and whose
    /// demand is <b>derived from occupancy</b> so that shedding changes it.
    /// </summary>
    /// <param name="shedsAfter">Ticks of pressure per Occupant shed. Zero sheds nobody.</param>
    /// <param name="condemnsAfter">Ticks of pressure before the premises are condemned.</param>
    /// <remarks>
    /// ⚠ <b><c>apply</c> is the whole reason this fixture is not <c>DemolishVerbTests</c>' one.</b>
    /// With a fixed band the demand does not move when an Occupant leaves, so shedding buys nothing
    /// and the Building is condemned anyway — which is what <c>rulesets/declining.toml</c> does and
    /// why <c>rulesets/thinned.toml</c> had to change that one word to demonstrate anything.
    /// </remarks>
    private static Ruleset Declining(int shedsAfter, int condemnsAfter) =>
        new(
            resources: [ResourceFamily.Good],
            rules:
            [
                new RuleDefinition(
                    House, Rate, ApplyCount.From(new ReadoutId((ushort)Readout.Occupancy), 100), RuleId.None, false,
                    default, ConditionId.None, 0, 1, 0, 0, 0, 0),
            ],
            kinds:
            [
                new KindDefinition(0, 1, 0, 1)
                {
                    CondemnAfterTicks = condemnsAfter,
                    ShedsOccupantAfterTicks = shedsAfter,
                    CollapsesAfterDays = 1,
                    Occupants = Occupants,
                },
            ],
            inputs: [new Term(new BinRef(Scope.Local, Repairs), 1)],
            outputs: [],
            emissions: [],
            bins: [new BinDeclaration(Repairs, BinCapacity.Of(4))],
            kindRules: [new RuleId(1)],

            // A Zone Rule that judges and never builds -- otherwise it raises a replacement on every
            // Lot this empties and the counts below say nothing.
            zoneRules: [new ZoneRuleDefinition(House, 1, 4, 4)]);

    private static (World World, Simulation Simulation) Built(
        int shedsAfter, int condemnsAfter, int houses = 1)
    {
        var world = new World(1_000, Declining(shedsAfter, condemnsAfter));
        var simulation = new Simulation(world, WorldKey.FromSeed(0x5EED_0CCA_5A1D_0FFEUL))
        {
            // O(world) twice per Tick against a phase meant to be O(woken); these tests walk
            // thousands of Ticks to watch a clock cross a threshold.
            VerifyDecideWritesNothing = false,
        };

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), Housing);
            Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

            for (int held = 0; held < Occupants; held++)
            {
                world.CreateHousehold(building, lifeStage: 0);
            }
        }

        return (world, simulation);
    }

    private static void Run(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>How many Households the first Building still holds.</summary>
    private static int Occupancy(World world, int building = 0)
    {
        int held = 0;

        foreach (int _ in world.Occupants.Walk(building))
        {
            held++;
        }

        return held;
    }

    private static int Shells(World world)
    {
        int shells = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot) && world.Buildings.IsAbandoned(slot))
            {
                shells++;
            }
        }

        return shells;
    }

    // ---- the rung -------------------------------------------------------------------------------

    /// <summary>One Occupant leaves per multiple of the threshold, and the Building keeps standing.</summary>
    [Fact]
    public void The_first_threshold_sheds_one_occupant_and_leaves_the_building_standing()
    {
        const int Sheds = 256;

        (World world, Simulation simulation) = Built(shedsAfter: Sheds, condemnsAfter: 8 * Sheds);

        Assert.Equal(Occupants, Occupancy(world));

        // Past one multiple and comfortably short of two. The margin is the sweep interval, because
        // the rung is only applied on a Tick the Zone Rule samples this Lot.
        Run(simulation, Sheds + 128);

        Assert.Equal(Occupants - 1, Occupancy(world));

        // The point of the rung: the Building is still there, and its Lot is still occupied.
        Assert.Equal(0, Shells(world));
        Assert.True(world.Buildings.Rows.IsLive(0));
    }

    /// <summary>
    /// 🔴 <b>The count follows the CLOCK and not the sweep cadence.</b>
    /// </summary>
    /// <remarks>
    /// <b>The assertion that would catch the most likely wrong implementation.</b> The sweep runs
    /// every 32 Ticks; the threshold here is 256. A rung that shed one Occupant per sweep would have
    /// emptied this Building eight times over by the time the second multiple arrives, and a test
    /// asserting only <em>somebody was shed</em> would pass.
    /// </remarks>
    [Fact]
    public void The_shed_count_is_paced_by_the_threshold_and_not_by_the_sweep()
    {
        const int Sheds = 256;

        (World world, Simulation simulation) = Built(shedsAfter: Sheds, condemnsAfter: 16 * Sheds);

        Run(simulation, Sheds + 128);
        Assert.Equal(Occupants - 1, Occupancy(world));

        Run(simulation, Sheds);
        Assert.Equal(Occupants - 2, Occupancy(world));

        Run(simulation, Sheds);
        Assert.Equal(Occupants - 3, Occupancy(world));
    }

    /// <summary>
    /// 🔴 <b>An emptied Building RECOVERS, which is the whole of what this task buys.</b>
    /// </summary>
    /// <remarks>
    /// A derived Rule at zero occupancy bands to <c>(0,0)</c> and fires with zero applications, so
    /// <c>RuleEngine.Fire</c> clears <c>StarvedSince</c> and the pressure is gone. ***The Building is
    /// not merely un-condemned, it has stopped failing*** — and it is a Building placement can refill
    /// rather than a shell nobody can enter, which is the difference between this rung and the one
    /// below it.
    /// </remarks>
    [Fact]
    public void A_building_shed_to_empty_stops_failing_instead_of_being_condemned()
    {
        const int Sheds = 256;

        // Room for four sheds before the premises verdict: four Occupants at one per multiple.
        (World world, Simulation simulation) = Built(shedsAfter: Sheds, condemnsAfter: 16 * Sheds);

        Run(simulation, (Occupants * Sheds) + 256);

        Assert.Equal(0, Occupancy(world));

        // It is standing, it is not a shell, and the pressure that was about to condemn it is gone.
        Assert.Equal(0, Shells(world));
        Assert.True(world.Buildings.Rows.IsLive(0));

        // Well past the condemnation threshold, and it is still not condemned -- because the clock
        // it was racing was reset by the Rule succeeding, not by the shedding.
        Run(simulation, 16 * Sheds);

        Assert.Equal(0, Shells(world));
        Assert.True(world.Buildings.Rows.IsLive(0));
    }

    /// <summary>
    /// The shed does not reset the march toward condemnation, so the second threshold still lands.
    /// </summary>
    /// <remarks>
    /// <b>The refusal that shaped the implementation, asserted rather than left in a comment.</b>
    /// Resetting <c>StarvedSince</c> on a shed is the obvious spelling and it would make
    /// <c>condemn_after_days</c> unreachable for any kind that also sheds — the Building would thin
    /// out for ever and never fall down. Here there is only room for one shed before the premises
    /// verdict, and the verdict must still arrive.
    /// </remarks>
    [Fact]
    public void Shedding_does_not_reset_the_march_toward_condemnation()
    {
        const int Sheds = 256;

        (World world, Simulation simulation) = Built(shedsAfter: Sheds, condemnsAfter: 2 * Sheds);

        Run(simulation, Sheds + 128);
        Assert.Equal(Occupants - 1, Occupancy(world));

        Run(simulation, Sheds + 128);

        // Abandoned on schedule, with three Households still in it -- which is what a first threshold
        // authored too close to the second looks like, and why the loader refuses one at or past it.
        Assert.Equal(1, Shells(world));
    }

    /// <summary>A kind that states no first threshold sheds nobody, which is every world before this.</summary>
    [Fact]
    public void A_kind_that_states_no_first_threshold_sheds_nobody()
    {
        const int Condemns = 256;

        (World world, Simulation simulation) = Built(shedsAfter: 0, condemnsAfter: Condemns);

        Run(simulation, Condemns - 64);

        Assert.Equal(Occupants, Occupancy(world));

        Run(simulation, 128);

        // It goes straight from full to abandoned, with no rung in between: adr/0091's shell, and
        // the whole of the city this milestone started with.
        Assert.Equal(1, Shells(world));
    }
}
