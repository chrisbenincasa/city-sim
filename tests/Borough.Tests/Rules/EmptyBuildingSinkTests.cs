using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0045</c> queue item 13: <b>the dwelling stock's demand-side sink</b> —
/// <c>[[building]] abandoned_when_empty_after_days</c>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The whole class turns on it not being a lifespan, and every assertion here is shaped by
/// that.</b> <c>condemn_after_days</c> reads Failure Pressure, so a kind stating it declines whether
/// anybody wants it or not; this reads occupancy, so <em>only surplus stock dies</em>. It is
/// <c>adr/0069</c>'s build predicate mirrored — a developer builds while the Unplaced Pool is
/// non-empty and gives up on a Building the Pool never came for — and it is what <c>02 §5.5</c>
/// calls redevelopment's floor, the case where nobody wants the land.
/// </para>
/// <para>
/// ⚠ <b>The kind here declares NO Rules at all</b>, which is deliberate rather than lazy: with a
/// starving Rule under it, every assertion below would pass on the ordinary decline path and none of
/// them would be about this mechanism.
/// <see cref="A_building_somebody_lives_in_is_never_abandoned_however_long"/> is the one that would
/// catch a spelling keyed on the wrong predicate, and it is the reason there is a second Household in
/// the fixture at all.
/// </para>
/// <para>
/// ⚠ <b>It ABANDONS rather than demolishing, so <c>adr/0091</c> is untouched</b> — clearing land is
/// bought rather than taken. <see cref="The_shell_stands_and_then_collapses_on_its_own_clock"/> is
/// what says the two halves are the ones already built: the empty house becomes a shell exactly as a
/// condemned one does, and <c>collapses_after_days</c> is the sink under both.
/// </para>
/// </remarks>
public sealed class EmptyBuildingSinkTests
{
    private const byte House = 1;
    private const ushort Housing = 1;

    /// <summary>How many Households the Building has room for.</summary>
    private const int Occupants = 4;

    /// <summary>
    /// How much floor one tenancy takes here — <b>one Tile</b>, so a Lot's width is its ceiling.
    /// </summary>
    /// <remarks>
    /// <b><c>plans/0053</c>: how many a Building holds is DERIVED from the ground it stands on</b>,
    /// so a fixture that wants a ceiling of <c>Occupants</c> states the ground rather than the count.
    /// One Tile per tenancy is the rate that makes the two read as the same sentence — the Lot is
    /// <c>Occupants</c> Tiles wide and one deep, on one storey.
    /// </remarks>
    private const int FloorPerOccupant = 1;

    /// <summary>
    /// A kind with <b>no Rules</b>, so nothing can starve and the only verdict available is this one.
    /// </summary>
    /// <param name="emptiesAfter">Ticks a Building may house nobody. Zero stands empty for ever.</param>
    /// <param name="standsFor">Days the shell stands before it collapses.</param>
    private static Ruleset Emptying(int emptiesAfter, int standsFor = 1) =>
        new(
            resources: [ResourceFamily.Good],
            rules: [],
            kinds:
            [
                new KindDefinition(0, 0, 0, 0)
                {
                    AbandonedWhenEmptyAfterTicks = emptiesAfter,
                    CollapsesAfterDays = standsFor,
                    Houses = true, Premises = true,
                },
            ],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],

            // A Zone Rule that judges and never builds. It has no Unplaced Pool to build against
            // either — nothing here creates one — but stating the intent is what keeps the counts
            // below readable if that ever changes.
            zoneRules: [new ZoneRuleDefinition(House, 1, 4, 4)])
        {
            Capacity = new CapacityRuleset(FloorPerOccupant, 0, 0),
        };

    private static (World World, Simulation Simulation) Built(
        int emptiesAfter, int households, int standsFor = 1)
    {
        var world = new World(1_000, Emptying(emptiesAfter, standsFor));
        var simulation = new Simulation(world, WorldKey.FromSeed(0x5EED_0CCA_5A1D_0FFEUL))
        {
            // O(world) twice per Tick against a phase meant to be O(woken); these tests walk
            // thousands of Ticks to watch a clock cross a threshold.
            VerifyDecideWritesNothing = false,
        };

        Handle<Lot> lot = world.Lots.Create(
            new Tiles(0), new Tiles(0), Housing, wide: new Tiles(Occupants), deep: new Tiles(1));
        Handle<Building> building = world.CreateBuilding(lot, House, Ticks.Zero, simulation.Key);

        for (int held = 0; held < households; held++)
        {
            world.CreateHousehold(building, lifeStage: 0);
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

    private static bool Standing(World world) =>
        world.Buildings.Rows.IsLive(0) && !world.Buildings.IsAbandoned(0);

    private static bool Shell(World world) =>
        world.Buildings.Rows.IsLive(0) && world.Buildings.IsAbandoned(0);

    // ---- the sink -------------------------------------------------------------------------------

    /// <summary>A Building nobody has lived in for its kind's duration is abandoned.</summary>
    /// <remarks>
    /// <b><c>adr/0069</c> has construction house nobody</b>, so this Building is empty from the Tick
    /// it was raised and the clock starts there rather than the first time somebody leaves. ⚠ <b>The
    /// margin is the sweep interval</b>, because the verdict is only taken on a Tick the Zone Rule
    /// samples this Lot — <c>Condemn</c>'s own rule that sampling <em>reads</em> a duration and never
    /// produces one.
    /// </remarks>
    [Fact]
    public void A_building_nobody_moves_into_is_abandoned_when_its_clock_runs_out()
    {
        const int Empties = 512;

        (World world, Simulation simulation) = Built(emptiesAfter: Empties, households: 0);

        Run(simulation, Empties - 64);
        Assert.True(Standing(world), "abandoned before its clock ran out");

        Run(simulation, 128);
        Assert.True(Shell(world), "still standing well past its clock");
    }

    /// <summary>
    /// 🔴 <b>The predicate is EMPTY and not UNDER-OCCUPIED, and this is the assertion that says so.</b>
    /// </summary>
    /// <remarks>
    /// <b>A Building holding one Household of a declared four is three quarters empty and is not a
    /// candidate</b>, however long it sits that way. ***That is what makes this a sink for surplus
    /// stock rather than a rent on under-use***, and it is why the mechanism cannot on its own restore
    /// <c>[[building]] jobs</c>' derived ratio on a demographic world: placement scatters Households
    /// one to a house, so most of the empty capacity in a shrinking city is in houses somebody lives
    /// in. <c>StageDumpTests</c> holds the measured half of that.
    /// </remarks>
    [Fact]
    public void A_building_somebody_lives_in_is_never_abandoned_however_long()
    {
        const int Empties = 512;

        (World world, Simulation simulation) = Built(emptiesAfter: Empties, households: 1);

        Run(simulation, 16 * Empties);

        Assert.True(Standing(world), "a Building with a Household in it was abandoned for emptiness");
    }

    /// <summary>Absent means it stands empty for ever, which is what every other shipped kind means.</summary>
    [Fact]
    public void A_kind_that_states_no_clock_stands_empty_for_ever()
    {
        (World world, Simulation simulation) = Built(emptiesAfter: 0, households: 0);

        Run(simulation, 8_192);

        Assert.True(Standing(world), "a kind stating no empty clock was abandoned anyway");
    }

    /// <summary>
    /// 🔴 <b>The clock is a duration since the Building last emptied, and moving in RESTARTS it.</b>
    /// </summary>
    /// <remarks>
    /// <b>The assertion that catches the most likely wrong implementation</b>, which is a tally of
    /// sweeps that found the Building empty. Under a tally, a Building empty for most of its life
    /// would be abandoned shortly after the last tenant left however recently they left — and a test
    /// asserting only <em>the empty one died</em> would pass. This is <c>adr/0053</c>'s
    /// <em>recovery is total rather than a debt worked off</em>, on a second subject.
    /// </remarks>
    [Fact]
    public void Moving_in_restarts_the_clock_rather_than_pausing_a_tally()
    {
        const int Empties = 512;

        (World world, Simulation simulation) = Built(emptiesAfter: Empties, households: 0);

        // Most of the way to the threshold with nobody in it.
        Run(simulation, Empties - 64);
        Assert.True(Standing(world));

        // Somebody moves in and straight back out. A tally would be almost full; a duration is zeroed
        // by the arrival and restarted by the departure.
        Handle<Household> tenant = world.CreateHousehold(world.Buildings.Rows.At(0), lifeStage: 0);
        Run(simulation, 8);
        world.Unplace(tenant);

        // Past where the ORIGINAL clock would have expired, and well short of the restarted one.
        Run(simulation, 128);
        Assert.True(Standing(world), "the clock was a tally: it survived a tenancy");

        Run(simulation, Empties);
        Assert.True(Shell(world), "the restarted clock never ran out");
    }

    /// <summary>
    /// The shell stands, and then collapses on <c>collapses_after_days</c> like any other.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0091</c> and <c>adr/0172</c> together, and neither is amended here.</b> The city
    /// stops maintaining an empty house; it never demolishes one. What clears the Lot is the same
    /// clock that clears a condemned Building's — which is why the loader requires
    /// <c>collapses_after_days</c> alongside this key, and why an <c>adr/0006</c> collection is not
    /// created by adding a second way in.
    /// </remarks>
    [Fact]
    public void The_shell_stands_and_then_collapses_on_its_own_clock()
    {
        const int Empties = 512;

        (World world, Simulation simulation) = Built(
            emptiesAfter: Empties, households: 0, standsFor: 1);

        Run(simulation, Empties + 64);
        Assert.True(Shell(world));
        Assert.False(world.Lots.IsVacant(0), "the Lot cleared while the shell was supposed to stand");

        Run(simulation, Ticks.PerDay + 64);

        Assert.False(world.Buildings.Rows.IsLive(0), "the shell never collapsed");
        Assert.True(world.Lots.IsVacant(0), "the Lot did not return to vacant");
    }
}
