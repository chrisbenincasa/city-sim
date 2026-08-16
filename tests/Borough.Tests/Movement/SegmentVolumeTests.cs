using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Invariants;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// Milestone 5c task 6: <b><c>adr/0041</c>'s volume attribution, running for the first time.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>That ADR shipped in 5a with columns nothing incremented</b>, and 5b's close-out found the reason
/// was not the one everybody had: the expectation was that a vehicular Leg would fix it, and
/// <c>adr/0075</c>'s <i>a Leg is a plan</i> meant there was no <b>next Segment</b> to move to. Task 6
/// supplies one — a route stored per in-flight drive Leg and a cursor on the Traveller — so the
/// increment finally has somewhere to happen.
/// </para>
/// <para>
/// ⚠ <b>None of this is reached by any shipped Ruleset, and that is deliberate rather than an
/// oversight.</b> No shipped file states <c>[households]</c>, so nobody drives, so there are no drive
/// Legs to attribute. Stating <c>[traffic]</c> in a file where it cannot act would be three unratified
/// numbers accumulating authority without ever being exercised, which is what <c>adr/0052</c> is
/// about. <b>The place both tables are stated together is 5c task 8's long run</b>, which is their
/// named ratifier — so until then this suite is the only thing that runs the mechanism, and it says so
/// here rather than leaving a reader to infer it from a green board.
/// </para>
/// </remarks>
public sealed class SegmentVolumeTests(ITestOutputHelper output)
{
    private const string Traffic = "\n[traffic]\nalpha_percent = 15\nbeta = 4\nclamp_percent = 400\n";

    private readonly ITestOutputHelper _output = output;

    private static Ruleset Rules(int ownership, bool congestion, int capacityPerHour = 3_600)
    {
        string toml = File.ReadAllText(GoldenFixtures.RulesetPath)
                .Replace(
                    "street_capacity_per_hour       = 3600",
                    $"street_capacity_per_hour       = {capacityPerHour}",
                    StringComparison.Ordinal)
            + $"\n[households]\ncar_ownership_percent = {ownership}\n"
            + (congestion ? Traffic : string.Empty);

        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    private static Simulation Start(Ruleset rules, int population)
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(population), GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), rules);

        // The populator is a command, not a constructor argument (adr/0080's precedent and
        // CommandKind.Populate's own): a world stepped without it is an empty map with roads on it.
        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }

    private static long TotalVolume(World world)
    {
        RoadSegmentTable segments = world.Roads.Segments;
        long total = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            if (segments.Rows.IsLive(slot))
            {
                total += segments.VolumeForward[slot] + segments.VolumeBackward[slot];
            }
        }

        return total;
    }

    private static int Driving(World world)
    {
        int driving = 0;

        for (int slot = 0; slot < world.Travellers.Rows.SlotCount; slot++)
        {
            if (world.Travellers.Rows.IsLive(slot)
                && (TravelMode)world.Legs.Mode[world.Travellers.CurrentLeg[slot]] != TravelMode.Foot)
            {
                driving++;
            }
        }

        return driving;
    }

    // ---- conservation ---------------------------------------------------------------------------

    /// <summary>
    /// ⚠ <b>Invariant 37 was written vacuously true and is load-bearing from this Tick on, with no
    /// edit to it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 5b shipped <c>Invariant.SegmentVolumeIsConserved</c> knowing both sides were structurally zero,
    /// against slice 5 task 7's precedent of withholding a vacuous assertion — the distinction being
    /// between an assertion whose <em>shape</em> is wrong until the world changes and one that is
    /// <b>correct and temporarily trivial</b>. <b>This test is the payment on that judgement</b>: the
    /// assertion is unchanged, both sides are now non-zero, and it holds.
    /// </para>
    /// <para>
    /// <b>Checked every Tick rather than at the end</b>, because the failure this guards is a leak —
    /// an increment with no matching decrement — and a leak that is repaired by the last Traveller
    /// arriving is invisible to a final reading.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_vehicle_is_on_exactly_one_segment_on_every_tick()
    {
        Simulation simulation = Start(Rules(100, congestion: true), 4_000);
        World world = simulation.World;

        long peak = 0;

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            long volume = TotalVolume(world);

            Assert.Equal(Driving(world), volume);

            peak = Math.Max(peak, volume);
        }

        _output.WriteLine($"peak vehicles on the road: {peak}");

        // A run in which nothing ever drove would satisfy the equality above trivially, which is the
        // exact failure mode the invariant shipped with. The assertion that it is no longer trivial
        // belongs here rather than in the invariant, because the invariant is about every world.
        Assert.True(peak > 0, "no vehicle was ever on a Segment, so the conservation check is vacuous.");
    }

    /// <summary>
    /// <b>The road empties completely when the last Traveller arrives.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0006</c>'s collection rule applied to a quantity rather than a table: a volume that
    /// settles above zero is a vehicle that entered a Segment and never left, and it presents to a
    /// player as a road that is busy forever with nothing on it.
    /// </remarks>
    [Fact]
    public void A_city_that_has_stopped_travelling_has_no_traffic_on_it()
    {
        Simulation simulation = Start(Rules(100, congestion: true), 1_000);
        World world = simulation.World;

        for (int tick = 0; tick < 256; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
        }

        // Long enough for every commute armed in the window above to finish, and not so long that the
        // next Day's departures have begun -- CommuteRoster spreads departures over TICKS_PER_DAY
        // divided by the peak factor, so the quiet stretch is the rest of the Day.
        for (int tick = 0; tick < 900; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
        }

        Assert.Equal(0, Driving(world));
        Assert.Equal(0, TotalVolume(world));
    }

    /// <summary>
    /// <b>No Segment ever holds a negative count, in either direction.</b>
    /// </summary>
    /// <remarks>
    /// The conservation sum above would pass on a world with one Segment at −3 and another at +3, so
    /// it cannot catch a direction bit read differently on entry and exit. <b>That is the specific
    /// mistake the stored bit exists to prevent</b>, and this is what would fire if it were derived
    /// from the graph at each end instead.
    /// </remarks>
    [Fact]
    public void No_segment_direction_ever_goes_negative()
    {
        Simulation simulation = Start(Rules(100, congestion: true), 2_000);
        RoadSegmentTable segments = simulation.World.Roads.Segments;

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (!segments.Rows.IsLive(slot))
                {
                    continue;
                }

                Assert.True(segments.VolumeForward[slot] >= 0, $"Segment {slot} forward went negative.");
                Assert.True(segments.VolumeBackward[slot] >= 0, $"Segment {slot} backward went negative.");
            }
        }
    }

    /// <summary>
    /// <b>A Segment slot that comes back from the free list carries no traffic.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The one shape of <c>adr/0006</c> leak this table can hold, and it is on the player's path
    /// rather than the simulation's.</b> <see cref="TripEngine"/>'s <c>Enter</c> and <c>Leave</c> are a
    /// matched pair keyed on the same stored direction bit, so on a standing road the count is
    /// conserved by construction. What that pair does not cover is a Segment <em>bulldozed with a
    /// vehicle on it</em>: it is freed at a non-zero volume, <c>Leave</c> then correctly declines to
    /// decrement a Segment that no longer exists, and the next <c>LayStreet</c> takes the slot back. If
    /// the count came back with it, the new Street would carry a Vehicle nobody could ever take off
    /// it, in a <b>saved and hashed</b> column -- a permanent capacity loss no per-Tick check would
    /// see, because the invariant would simply be wrong by a constant from that Tick on.
    /// </para>
    /// <para>
    /// ⚠ <b>It holds, and reading the create says it does not.</b> <c>RoadSegmentTable.Create</c>
    /// assigns six saved columns and <c>Epoch</c> and never touches the two volume ones -- they
    /// arrived in 5b, were incremented by nothing for two milestones, and were never added to it.
    /// <c>Rows.AllocateSlot</c> zeroes nothing either. The guarantee is at the <b>other end of the
    /// recycle</b>: <c>Rows.FreeSlot</c> clears every column, and it does so for the State Hash's sake
    /// rather than for this one's -- zeroing on free is what lets the fold walk every slot in index
    /// order with no liveness branch. ***A create that sets most of its columns and a free that clears
    /// all of them read as opposite guarantees, and only one end of a recycle has to hold.*** Written
    /// here after concluding the opposite from <c>Create</c> alone, which is <c>adr/0093</c> exactly:
    /// the create is where to look and never what was found.
    /// </para>
    /// <para>
    /// <b>Asserted rather than left to the substrate, because the substrate's reason is not this
    /// one.</b> <c>FreeSlot</c>'s clear is documented as a hash property, so a change that narrowed it
    /// to the columns the hash folds -- or that moved zeroing to allocation and skipped what a create
    /// already sets -- would be reasonable on its own terms and would arm this leak silently.
    /// <c>BulldozeStreet</c> is the only thing in the project that frees a Segment from a running
    /// world (<c>adr/0090</c>: the generator makes land and the player makes every road) and
    /// <c>CommandKind.Populate</c> cannot reach it, so no long run will ever cover this.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_bulldozed_segments_slot_comes_back_empty()
    {
        Simulation simulation = Start(Rules(100, congestion: true), 4_000);
        World world = simulation.World;

        int occupied = Rows.NoSlot;
        (int Column, int Row, StreetAxis Axis) edge = default;

        // Until somebody is actually on a Street. A Segment freed at zero recycles to zero whatever
        // the substrate does, so a test that bulldozed an empty one would hold with the clear removed
        // -- which is the change this exists to catch.
        for (int tick = 0; tick < 512 && occupied == Rows.NoSlot; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            (occupied, edge) = FirstOccupiedStreet(world);
        }

        Assert.NotEqual(Rows.NoSlot, occupied);

        RoadSegmentTable segments = world.Roads.Segments;
        int carried = segments.VolumeForward[occupied] + segments.VolumeBackward[occupied];

        _output.WriteLine($"bulldozing block {edge.Column},{edge.Row} carrying {carried} Vehicle(s)");

        Assert.True(world.Roads.BulldozeStreet(edge.Column, edge.Row, edge.Axis));
        Assert.True(world.Roads.LayStreet(edge.Column, edge.Row, edge.Axis));

        // The free list is LIFO and nothing allocated in between, so the relaid Street is the same
        // slot. Asserted rather than assumed: if it ever stops being the same slot this test stops
        // testing anything, and would say so by failing here rather than by passing quietly.
        int relaid = world.Roads.Streets.SegmentOn(edge.Column, edge.Row, edge.Axis);

        Assert.Equal(occupied, relaid);
        Assert.Equal(0, segments.VolumeForward[relaid]);
        Assert.Equal(0, segments.VolumeBackward[relaid]);
    }

    /// <summary>The first lattice edge whose Street has a vehicle on it, and where it is.</summary>
    private static (int Slot, (int Column, int Row, StreetAxis Axis) Edge) FirstOccupiedStreet(
        World world)
    {
        StreetGrid streets = world.Roads.Streets;
        RoadSegmentTable segments = world.Roads.Segments;

        for (int row = 0; row < streets.Span; row++)
        {
            for (int column = 0; column < streets.Span; column++)
            {
                foreach (StreetAxis axis in (StreetAxis[])[StreetAxis.East, StreetAxis.North])
                {
                    int slot = streets.SegmentOn(column, row, axis);

                    if (slot != Rows.NoSlot
                        && segments.VolumeForward[slot] + segments.VolumeBackward[slot] > 0)
                    {
                        return (slot, (column, row, axis));
                    }
                }
            }
        }

        return (Rows.NoSlot, default);
    }

    /// <summary>
    /// <b>Both directions of the graph are used, so the direction bit is doing work.</b>
    /// </summary>
    /// <remarks>
    /// A recorder that always wrote <c>forward</c> would pass every test above. <c>adr/0041</c>
    /// attributes per direction precisely because a one-way street and a congested inbound carriageway
    /// are different facts about the same Segment, and a city whose backward volume is never non-zero
    /// has thrown that away without saying so.
    /// </remarks>
    [Fact]
    public void Traffic_runs_in_both_directions()
    {
        Simulation simulation = Start(Rules(100, congestion: true), 4_000);
        RoadSegmentTable segments = simulation.World.Roads.Segments;

        long forward = 0;
        long backward = 0;

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (segments.Rows.IsLive(slot))
                {
                    forward += segments.VolumeForward[slot];
                    backward += segments.VolumeBackward[slot];
                }
            }
        }

        _output.WriteLine($"vehicle-Ticks forward {forward}, backward {backward}");

        Assert.True(forward > 0, "nothing ever drove A to B.");
        Assert.True(backward > 0, "nothing ever drove B to A.");
    }

    // ---- the function actually biting -----------------------------------------------------------

    /// <summary>
    /// ⚠ <b>Congestion makes the same city's journeys take longer, and this is the whole deliverable —
    /// but only on a city whose roads are too narrow for it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two identical runs differing in one table.</b> Everything else — seed, population, Ruleset,
    /// command log — is the same, so the difference in how long vehicles spend on the road is the
    /// volume-delay function and nothing else.
    /// </para>
    /// <para>
    /// ⚠ <b>The Street capacity is cut to 200 Vehicles an hour, and it has to be.</b> At the shipped
    /// 3,600 the busiest Segment in a generated city reaches <c>v/c</c> 0.44 and the function multiplies
    /// every cost by <b>1.0054</b>, which vanishes under the sub-Tick carry and comes back as a
    /// byte-identical run — <see cref="A_generated_city_is_never_busy_enough_to_slow_itself_down"/>
    /// asserts exactly that, so this pair of tests is one finding stated from both sides. A road too
    /// narrow for its traffic is not an artificial fixture: it is <c>adr/0090</c>'s city, where the
    /// player lays every Segment and can lay too few.
    /// </para>
    /// <para>
    /// <b>The quantity compared is vehicle-Ticks, not a Trip count.</b> A congested run does not make
    /// fewer Trips — the Commute Budget is judged on the <em>plan</em>, at free-flow, before anybody
    /// sets off — it makes the same Trips take longer, so the count is flat by construction and the
    /// occupancy is where the effect lands. ***A ruler must not move with the thing it measures.***
    /// </para>
    /// </remarks>
    [Fact]
    public void A_congested_city_keeps_its_vehicles_on_the_road_longer()
    {
        long free = Occupancy(congestion: false, capacityPerHour: 200);
        long loaded = Occupancy(congestion: true, capacityPerHour: 200);

        _output.WriteLine($"vehicle-Ticks: free-flow {free}, with a volume-delay function {loaded}");
        _output.WriteLine($"ratio {(double)loaded / free:F3}");

        Assert.True(free > 0, "the free-flow control put nobody on the road.");
        Assert.True(
            loaded > free,
            "the volume-delay function did not slow anything down, so it is inert on this city.");
    }

    /// <summary>
    /// ⚠ <b>At the shipped road capacity the function changes nothing at all, and that is asserted
    /// rather than left to be discovered.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A negative assertion, written so the fact cannot rot.</b> The busiest Segment a generated
    /// city produces holds 4–6 Vehicles against a capacity stock of 9.2, and BPR at <c>v/c</c> 0.44 is
    /// ×1.0054 — smaller than the Q16.16 resolution of a 0.22-Tick Street once the sub-Tick carry has
    /// absorbed it. <b>So the run is identical</b>, and somebody reading a flat congestion figure off
    /// 5c task 8's long run needs to find this test rather than conclude the mechanism is broken.
    /// </para>
    /// <para>
    /// <b>It fails in the useful direction.</b> Anything that raises demand relative to supply — a
    /// player laying less road, a denser lattice, a higher car ownership, a longer commute — breaks
    /// this assertion, and breaking it is the signal that the function has started to matter.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_generated_city_is_never_busy_enough_to_slow_itself_down()
    {
        long free = Occupancy(congestion: false, capacityPerHour: 3_600);
        long loaded = Occupancy(congestion: true, capacityPerHour: 3_600);

        _output.WriteLine($"vehicle-Ticks at the shipped capacity: free-flow {free}, loaded {loaded}");

        Assert.Equal(free, loaded);
    }

    private static long Occupancy(bool congestion, int capacityPerHour)
    {
        Simulation simulation = Start(Rules(100, congestion, capacityPerHour), 4_000);
        long total = 0;

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));
            total += TotalVolume(simulation.World);
        }

        return total;
    }

    /// <summary>
    /// <b>Free-flow is what an empty road costs, so the two runs agree before anybody sets off.</b>
    /// </summary>
    /// <remarks>
    /// The control for the test above. If the function charged a delay at zero volume, the comparison
    /// would be measuring a constant offset rather than congestion — and a lone vehicle would be
    /// driving slower than the speed limit the Ruleset states.
    /// </remarks>
    [Fact]
    public void The_first_vehicle_onto_an_empty_city_pays_nothing_for_congestion()
    {
        Simulation congested = Start(Rules(100, congestion: true), 1_000);
        Simulation free = Start(Rules(100, congestion: false), 1_000);

        // One Tick: the first departures are armed and nobody has met anybody yet.
        congested.Step(new TickInput([], rulesetHash: 0));
        free.Step(new TickInput([], rulesetHash: 0));

        Assert.Equal(TotalVolume(free.World), TotalVolume(congested.World));
    }

    // ---- the sub-Tick carry ---------------------------------------------------------------------

    /// <summary>
    /// ⚠ <b>Without the sub-Tick carry every drive would be instantaneous, and this is the proof.</b>
    /// </summary>
    /// <remarks>
    /// A 32-Tile Street at 50 km/h costs <b>0.22 Ticks</b> (<c>adr/0071</c>, restated by
    /// <c>adr/0094</c> under the 2048-Tick Day). Flooring each hop on its own would round every one of
    /// them to nothing, so a twenty-Segment commute would arrive on the Tick it left and no vehicle
    /// would ever be observed on a road. <b>The observable consequence is that a drive occupies more
    /// than one Tick</b>, which is what this asserts — and it is asserted on the free-flow run, so it
    /// cannot be satisfied by congestion instead.
    /// </remarks>
    [Fact]
    public void A_drive_takes_more_than_a_single_tick()
    {
        Simulation simulation = Start(Rules(100, congestion: false), 4_000);
        World world = simulation.World;

        int ticksWithTraffic = 0;

        for (int tick = 0; tick < 512; tick++)
        {
            simulation.Step(new TickInput([], rulesetHash: 0));

            if (TotalVolume(world) > 0)
            {
                ticksWithTraffic++;
            }
        }

        _output.WriteLine($"{ticksWithTraffic} of 512 Ticks had a vehicle on the road");

        Assert.True(
            ticksWithTraffic > 1,
            "no Tick but one had traffic on it, so every drive completed in the Tick it started.");
    }
}
