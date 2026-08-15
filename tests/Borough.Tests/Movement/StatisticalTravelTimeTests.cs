using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// That a journey takes the time it was quoted, which is the whole of what <em>Statistical</em> means.
/// </summary>
/// <remarks>
/// <para>
/// <c>03 §3.1</c> defines the tier as <em>time-advanced; travel time from distance / speed</em>, and
/// <c>adr/0071</c> spends an ADR establishing that travel time is <b>sub-Tick</b> — a 32-Tile Street at
/// 50 km/h is <b>0.22</b> Ticks under <c>adr/0094</c>'s clock. A vehicle therefore crosses about 4.6
/// Segments in a Tick, and a model that moves it one is not the tier it claims to be.
/// </para>
/// <para>
/// ⚠ <b>It moved one, from milestone 5c task 6 until this file was written.</b>
/// <c>TripEngine.AdvanceTravellers</c> handled each Traveller once per call and went on to the next
/// slot, so the sub-Tick carry could only ever <em>add</em> delay: a journey took
/// <c>hops + floor(total cost)</c> where its Leg was priced at <c>total cost</c>, about <b>5.6×</b> at
/// the shipped geometry. That is <c>adr/0041</c>'s <i>a vehicle crosses about one Segment per Tick</i>
/// — a sentence that ADR states as following from <c>TicksPerDay = 8192</c>, which <c>adr/0094</c>
/// retired. Task 6 found the premise expired, repaired the volume/capacity ratio, and did not check
/// the advance loop, which encodes the same assumption. ***A premise that expires retires every site
/// resting on it, and finding one of them is not finding them.***
/// </para>
/// <para>
/// ⚠ <b>Nothing in the suite could have caught it, and the reason is a coverage hole rather than an
/// oversight.</b> The defect reaches only a <em>drive</em>: a walk Leg carries a cost and no path
/// (<c>adr/0075</c>), so <c>BeginLeg</c> advances it whole and the floor never applies. No shipped
/// Ruleset except <c>congested.toml</c> states <c>[households]</c>, so nobody drives in any committed
/// baseline — and the repair moved <b>no State Hash at all</b>, which by <c>05 §4</c>'s own test reads
/// as an <em>optimisation</em>. It is not one. ***A design change that no baseline covers comes back
/// reading as an optimisation***, which is <c>adr/0089</c>'s map flip finding on a second mechanism.
/// </para>
/// <para>
/// <b>So this file drives on purpose</b>, and asserts the quoted time against the taken time rather
/// than any level, because a level is what the milestone's other instruments already read.
/// </para>
/// </remarks>
public sealed class StatisticalTravelTimeTests(ITestOutputHelper output)
{
    /// <summary>Everybody drives, so the Legs under test are the ones that carry a route.</summary>
    private const string Households = "\n[households]\ncar_ownership_percent = 100\n";

    /// <summary>Past the assignment pass's first sample, so there are commutes to watch.</summary>
    private const int Settle = 2_100;

    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// ⚠ <b>A drive takes the time its Leg was priced at, to within the Tick it is measured in.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A stopwatch on individual journeys, because the aggregate forms of this measurement are all
    /// wrong in ways that look right.</b> The first cut counted departures as growth in
    /// <c>TripTable.Rows.SlotCount</c> and read <b>0.05</b> a Tick against a true rate near 6: slots
    /// are only allocated when the free list is empty, so a table that recycles stops counting the
    /// moment it reaches its high-water mark. ***A high-water mark is not a flow***, which is exactly
    /// what <c>CensusCounter</c>'s own remarks say about <c>Slots</c> against <c>Live</c> — read here
    /// by somebody who had the distinction in front of him and used the wrong one anyway.
    /// </para>
    /// <para>
    /// <b>Compared against the planned cost, which is the number the Citizen judged the journey by.</b>
    /// A Leg's cost is computed once at planning at free flow (<c>adr/0099</c>), and on a city that
    /// never reaches a load where the volume-delay function bites — every generated one, task 6's
    /// finding 3 — the executed cost is that same number. So the two are commensurable here, and the
    /// test asserts that rather than assuming it by checking the city stays under capacity.
    /// </para>
    /// <para>
    /// ⚠ <b>Journeys that begin and end inside one Tick are invisible to this and that is the safe
    /// direction.</b> A Traveller is only ever observed at a Tick boundary, so a sub-Tick journey is
    /// never seen live and never sampled — and those are the <em>fastest</em> journeys, so excluding
    /// them can only make the measured duration longer than the truth. A bound that survives a
    /// conservative sample survives.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_drive_takes_the_time_it_was_quoted()
    {
        Simulation simulation = Start(16_000);

        Dictionary<Handle<Traveller>, (ulong Start, long Quoted)> watched = [];
        long takenTicks = 0;
        long quotedTicks = 0;
        int journeys = 0;

        for (int tick = 0; tick < Settle; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            World world = simulation.World;
            TravellerTable travellers = world.Travellers;
            ulong now = simulation.Tick.Raw;

            for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
            {
                if (!travellers.Rows.IsLive(slot))
                {
                    continue;
                }

                Handle<Traveller> handle = travellers.Rows.At(slot);

                if (!watched.ContainsKey(handle))
                {
                    watched[handle] = (now, QuotedRaw(world, slot));
                }
            }

            // Anything whose handle no longer resolves finished on this Tick or the one before it.
            foreach (Handle<Traveller> handle in watched.Keys.ToArray())
            {
                if (travellers.Rows.TryResolve(handle, out _))
                {
                    continue;
                }

                (ulong start, long quoted) = watched[handle];

                watched.Remove(handle);

                takenTicks += (long)(now - start);
                quotedTicks += quoted;
                journeys++;
            }
        }

        Assert.True(journeys > 256, $"only {journeys} journeys completed; nothing was measured.");

        double taken = (double)takenTicks / journeys;
        double planned = quotedTicks / 65_536.0 / journeys;

        _output.WriteLine($"journeys watched  {journeys}");
        _output.WriteLine($"duration, taken   {taken:F2} Ticks");
        _output.WriteLine($"duration, quoted  {planned:F2} Ticks");

        // Two Ticks of slack, and it is the measurement's rather than the model's. A journey is only
        // ever observed at a Tick boundary, so both its start and its end are rounded outward by up to
        // one -- and the defect this bounds was 5.6x on a journey quoted at about two Ticks, which no
        // slack of this shape can hide.
        Assert.True(
            taken <= planned + 2.0,
            $"a drive quoted at {planned:F2} Ticks took {taken:F2}. The advance loop is stepping one "
            + "Segment a Tick again, which prices every drive at about 5x its own Leg.");
    }

    /// <summary>
    /// <b>A Traveller crosses more than one Segment in a Tick, which is what its own speed says.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The structural form of the assertion above.</b> The duration bound is an average and could
    /// in principle be met by a city whose routes are all one Segment long; this holds the mechanism
    /// directly — a cursor moving several hops inside one call to phase 4.
    /// </para>
    /// <para>
    /// ⚠ <b>Its first version measured the wrong quantity and passed against the defect.</b> It read
    /// the hops <em>remaining</em> ahead of each cursor and asserted that some route was longer than
    /// one Segment — which is true of every city here and says nothing whatever about how fast the
    /// cursor moves. Run against the one-Segment-a-Tick loop it reported <i>27 Segments</i> and passed.
    /// ***A test that cannot fail against the defect it names is documentation***, and this one was
    /// caught only because the defect was still available to run it against. What discriminates is the
    /// <b>decrease</b> in remaining hops between two Ticks, which is hops crossed and is exactly 1 when
    /// the loop is broken.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_traveller_crosses_several_segments_in_one_tick()
    {
        Simulation simulation = Start(16_000);

        Dictionary<Handle<Traveller>, int> remaining = [];
        int deepest = 0;

        for (int tick = 0; tick < Settle; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            TravellerTable travellers = simulation.World.Travellers;
            Dictionary<Handle<Traveller>, int> now = [];

            for (int slot = 0; slot < travellers.Rows.SlotCount; slot++)
            {
                if (!travellers.Rows.IsLive(slot) || travellers.CurrentHop[slot] == Rows.NoSlot)
                {
                    continue;
                }

                Handle<Traveller> handle = travellers.Rows.At(slot);
                int ahead = Ahead(simulation.World, travellers.CurrentHop[slot]);

                now[handle] = ahead;

                if (remaining.TryGetValue(handle, out int was))
                {
                    deepest = Math.Max(deepest, was - ahead);
                }
            }

            remaining = now;
        }

        _output.WriteLine($"most Segments crossed by one Traveller in one Tick: {deepest}");

        // A Street is 0.22 Ticks free-flow, so the honest expectation is about 4. Asserted at 2 rather
        // than at 4 because what is being held is that the floor is gone, and pinning the exact rate
        // would make this a test of the road geometry the fixture happens to have.
        Assert.True(
            deepest >= 2,
            $"no Traveller ever crossed more than {deepest} Segment(s) in a Tick, so the advance loop "
            + "is still stepping one a Tick whatever the free-flow crossing costs.");
    }

    /// <summary>How many Segments of this Leg's route are still ahead of the cursor.</summary>
    private static int Ahead(World world, int hop)
    {
        RouteHopTable hops = world.RouteHops;
        int count = 0;

        for (int at = hop; at >= 0; at = hops.Next[at] - 1)
        {
            count++;
        }

        return count;
    }

    /// <summary>The Q16.16 cost of every Leg of the Trip this Traveller is on, summed.</summary>
    /// <remarks>
    /// <b>Every Leg rather than the one being driven</b>, because what is being compared is the
    /// journey: <c>adr/0008</c> makes a car commute <c>walk → drive → walk</c>, and the two flanking
    /// walks are zero-length by construction today, so the sum is the drive plus nothing. Taking the
    /// drive Leg alone would agree today and stop agreeing the moment milestone 8 gives parking a real
    /// endpoint — which is the case this measurement most needs to keep working.
    /// </remarks>
    private static long QuotedRaw(World world, int travellerSlot)
    {
        LegTable legs = world.Legs;

        if (!world.Trips.Rows.TryResolve(world.Travellers.Trip[travellerSlot], out int trip))
        {
            return 0;
        }

        long total = 0;

        for (int leg = world.Trips.LegHead[trip] - 1; leg >= 0; leg = legs.Next[leg] - 1)
        {
            TravelTime time = legs.Time[leg];

            total += time.IsImpassable ? 0 : time.Raw;
        }

        return total;
    }

    /// <summary>A city in which every Household keeps a car. <c>VolumeDelayReachTests</c>' fixture.</summary>
    private static Simulation Start(int population)
    {
        RulesetLoadResult rules = RulesetLoader.Parse(
            File.ReadAllText(GoldenFixtures.RulesetPath) + Households, "test.toml");

        Assert.True(rules.Ok, rules.Describe());

        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), rules.Ruleset!);

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }
}
