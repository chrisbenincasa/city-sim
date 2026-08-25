using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// <b>A city whose every Street was laid by <c>CommandKind.Connect</c>, built to put the volume-delay
/// function somewhere it is not flat.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the ratifier <c>adr/0052</c>'s 2026-08-15 amendment asks for, and the amendment is why it
/// exists.</b> <c>[traffic]</c>'s α, β and clamp and <c>[households] car_ownership_percent</c> were
/// recorded with <em>5c task 8's long run</em> as their named ratifier — an owner, a date, a
/// purpose-built Ruleset and refuting readings in both directions. It ran and could not fire: load came
/// out at <b>0.0018 / 0.0048 / 0.0110</b> Vehicles per Segment per Tick at 4,000 / 16,000 / 64,000
/// Citizens, so BPR was only ever evaluated where it is nearly flat. <b>The ratifier named a
/// <em>machine</em> and not a <em>world</em>.</b> This file is the world.
/// </para>
/// <para>
/// <b>Congestion is a property of a network's <em>shape</em>, and a Ruleset can scale a lattice without
/// bending one.</b> <c>adr/0090</c> gives the generator land and the player every road, and
/// <c>CommandKind.Populate</c> sizes the paved extent from the population it serves — so the same
/// number sizes both the demand and the supply and <c>v/c</c> peaks at 0.44 whatever the population.
/// The only thing in this build that produces shape is the player's verb, so the fixture uses it.
/// </para>
/// <para>
/// <b>The shape is a dumbbell: two districts of blocks joined by one Street corridor.</b> Everybody in
/// one district who takes a job in the other has exactly one way across, which is the arrangement a
/// generated lattice structurally cannot produce — every block of it has four faces, so every journey
/// has as many routes as it has turns. One corridor is the smallest thing that is a <em>bottleneck</em>
/// rather than a thin patch.
/// </para>
/// <para>
/// ⚠ <b><c>CommandKind.Populate</c> and <c>CommandKind.Connect</c> were mutually exclusive at world
/// creation, and finding that out is what shaped this fixture.</b>
/// <see cref="RoadGenerator.LayInto"/> <em>throws</em> on a world that already has Segments — it is a
/// world-creation pass and says so — and <see cref="SyntheticCity.PopulateInto"/> called it
/// unconditionally, before it built anything. So a Connect-laid city could not be populated by the
/// populator in either order, and this file grew its own population by mirroring that method's three
/// loops. <b>That was a real gap rather than an inconvenience</b>: there was no door in the build
/// through which a player-shaped network got a population, because the only populator was welded to
/// the only generator. Filed rather than worked around silently (<c>adr/0073</c>), and ✅ <b>REPAIRED
/// 2026-08-15 by <c>plans/0003</c> hash-moving queue item 9</b> — the populator's land half and people
/// half are separable now, this fixture calls <see cref="SyntheticCity.PeopleInto"/>, and the copy is
/// gone.
/// </para>
/// <para>
/// <b>The population is created directly rather than through the Input Log, so this fixture is a
/// measurement and not a session.</b> <c>TrafficDump.Step</c> has the same shape for the same reason.
/// What is asserted here is a property of a <em>world</em>; replay equivalence is asserted by
/// <c>ReplayTests</c>, which lays its own Streets by Connect and is the reason this one can.
/// </para>
/// </remarks>
public sealed class ConnectedCityCongestionTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong Seed = 0xC0FFEE_0000_0001UL;

    private static readonly WorldKey Key = WorldKey.FromSeed(Seed);

    // ⚠ A copy of SyntheticCity's private `DwellingKind = 1` used to sit here, and its own remark
    // called itself plans/0012 Cause 1. It went with the copied populator: PeopleInto knows the kind,
    // so the fixture no longer needs to. Item 9 retired two copies rather than one.

    /// <summary>
    /// Blocks of empty ground between the two districts, spanned by a single row of Street.
    /// </summary>
    /// <remarks>
    /// <b>Long enough that the corridor is the journey rather than a detail of it</b>, and short enough
    /// that crossing it stays inside the Commute Budget — a corridor nobody can afford to cross is a
    /// city with no commuters in it, which measures the Budget instead of the road.
    /// </remarks>
    private const int CorridorBlocks = 8;

    /// <summary>Two Days, which is two whole commute cycles after employment has settled.</summary>
    private const int TickCount = 2 * Ticks.PerDay;

    /// <summary>
    /// <c>congested.toml</c>'s <c>block_tiles</c>, as a literal.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A literal because the commands are built before a <c>World</c> exists</b> — the lattice is
    /// the first thing this fixture does and there is nothing to read a Ruleset off yet.
    /// <see cref="The_assumed_block_size_is_the_shipped_one"/> holds it to the file, which is what keeps
    /// it from being a second copy that drifts (<c>plans/0012</c> Cause 1): if the shipped block size
    /// ever moves, that assertion fails rather than this fixture quietly laying its dumbbell on the
    /// wrong grid.
    /// </remarks>
    private const int BlockTiles = 32;

    /// <summary>
    /// How big each half of the dumbbell is, and how many people live on it.
    /// </summary>
    /// <remarks>
    /// <b>Population scales with the square of the district so density is held fixed</b>, which is what
    /// makes <see cref="The_volume_delay_function_is_refutable_on_a_player_laid_network"/>'s ladder a
    /// statement about the <em>corridor</em> rather than about crowding: every rung has the same people
    /// per block and the same one Segment between them.
    /// </remarks>
    private readonly record struct Plan(int DistrictBlocks)
    {
        /// <summary>The first block column of the far district.</summary>
        public int FarColumn => DistrictBlocks + CorridorBlocks;

        public int Population => 4_000 * DistrictBlocks * DistrictBlocks / 16;
    }

    /// <summary>Sixteen blocks a side. Enough to load a corridor and cheap to run.</summary>
    private static Plan Small => new(4);

    // ---- the two claims --------------------------------------------------------------------------

    /// <summary>
    /// <b>A player-laid bottleneck reaches a load the generated lattice never does.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The assertion is a <b>comparison</b> and not a level, for the reason every threshold in this
    /// corpus is one: a peak <c>v/c</c> means nothing alone, because it can be manufactured by lowering
    /// the capacity until it appears — which is exactly what <c>rulesets/congested.toml</c> does, and
    /// says so in its own header. What is structural is that the same Ruleset, the same capacity and
    /// the same population give a flat curve on a generated lattice and a loaded one here. <b>The only
    /// variable is who laid the road.</b>
    /// </para>
    /// <para>
    /// ⚠ <b>Run at the shipped capacity rather than at <c>congested.toml</c>'s demonstration rung.</b>
    /// 400 Vehicles an hour puts <b>1.02</b> Vehicles on a Segment where the shipped 3,600 puts 9.2, so
    /// a reading taken at 400 cannot distinguish <em>the player under-built the road</em> from <em>the
    /// file made the road absurd</em>. The point of a Connect-laid world is that it needs neither.
    /// </para>
    /// <para>
    /// <b>Measured 2026-08-15: 65.1% against 43.4%</b>, on 4,000 Citizens over two Days. The control's
    /// figure is <c>adr/0099</c>'s own 0.44 arriving by a third route, which is worth more than the
    /// comparison — it was measured at 4,000, 16,000 and 64,000 Citizens there and it comes back here
    /// on a differently-built world.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_corridor_carries_a_load_a_generated_lattice_never_reaches()
    {
        Reading corridor = Run(Small, Connected(), connected: true, "connect-laid dumbbell");
        Reading lattice = Run(Small, Connected(), connected: false, "generated lattice");

        Assert.True(corridor.VehicleTicks > 0, "nobody drove at all, so there is nothing to compare.");
        Assert.True(lattice.VehicleTicks > 0, "the control had no traffic, so it is not a control.");

        Assert.True(
            corridor.PeakLoad > lattice.PeakLoad,
            $"the corridor peaked at v/c {Percent(corridor.PeakLoad)} against the generated lattice's "
            + $"{Percent(lattice.PeakLoad)}, so laying the road badly did not load it -- either the "
            + "corridor is not a bottleneck or nobody is crossing it.");
    }

    /// <summary>
    /// <b>The volume-delay function changes what this city does, so its parameters are refutable.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the assertion the ratifier turns on, and it is the one task 8 could not make.</b>
    /// <c>SegmentVolumeTests.A_generated_city_is_never_busy_enough_to_slow_itself_down</c> is its
    /// control: on a generated city the loaded and free-flow runs come out <b>byte-identical</b> at the
    /// shipped capacity, because a peak <c>v/c</c> of 0.44 costs ×1.0054 and the sub-Tick carry absorbs
    /// it. ***A number that cannot change an outcome cannot be refuted by an outcome.*** Here the same
    /// Ruleset priced with and without <c>[traffic]</c> must disagree — and it does.
    /// </para>
    /// <para>
    /// <b>The ladder is printed rather than reduced to one rung, because the onset is the finding.</b>
    /// Measured 2026-08-15 at the shipped 3,600 Vehicles an hour, two Days, density held fixed:
    /// </para>
    /// <code>
    /// district   pop     employed   peak v/c loaded   free-flow   vehicle-Ticks loaded / free
    ///    4x4    4,000      1,185           65.1%         65.1%          6,784 /   6,784
    ///    6x6    9,000      2,507           97.7%         97.7%         19,475 /  19,472
    ///    8x8   16,000      4,470        1,074.3%        130.2%         44,853 /  43,703
    ///  10x10   25,000      7,022        2,767.0%        173.6%        122,725 /  80,978
    /// </code>
    /// <para>
    /// ⚠ <b>The onset is at <c>v/c</c> = 1 and the behaviour past it is a runaway, which no static
    /// reading of BPR predicts.</b> At 8×8 the free-flow world peaks at 130% and the priced one at
    /// <b>1,074%</b> — the function is not merely visible, it is <em>feeding itself</em>: congestion
    /// slows a Vehicle, a slower Vehicle dwells on its Segment longer, longer dwell is higher volume,
    /// and higher volume is more congestion. ***The volume-delay function is a loop and not a
    /// formula***, and the corpus has only ever quoted it as the second. That is what
    /// <c>03 §3.2</c> means by using it *only where it is strong* — and it is the argument for the
    /// Microscopic tier arriving from a direction nobody took it from.
    /// </para>
    /// <para>
    /// ⚠ <b>Both ends are asserted and the bottom rung is not decoration.</b> A fixture that only
    /// showed the function biting would be satisfied by one that always bites, which is a broken
    /// control rather than a loaded city — and the flat rung is what says this world is the shipped
    /// world until the corridor is actually short of road.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_volume_delay_function_is_refutable_on_a_player_laid_network()
    {
        Ruleset priced = Connected();
        Ruleset free = ConnectedWithoutTraffic();

        (Reading Loaded, Reading Free) At(Plan plan) => (
            Run(plan, priced, connected: true, $"{Name(plan)} loaded"),
            Run(plan, free, connected: true, $"{Name(plan)} free-flow"));

        (Reading Loaded, Reading Free) flat = At(Small);
        (Reading Loaded, Reading Free) loadedUp = At(new Plan(8));

        Assert.True(
            flat.Loaded.VehicleTicks > 0 && loadedUp.Loaded.VehicleTicks > 0,
            "nobody drove, so nothing was measured.");

        // The bottom rung: the shipped city, where the function is real and costs nothing anybody can
        // see. Equality rather than "close", because a sub-Tick delay is absorbed exactly.
        Assert.Equal(flat.Free.VehicleTicks, flat.Loaded.VehicleTicks);

        // The top rung, and the whole point of the fixture. Vehicle-Ticks rather than a delay column,
        // because a delay column is what the function COMPUTES and this has to be a reading of what the
        // city DID: a journey priced dearer occupies its Segments for longer.
        Assert.True(
            loadedUp.Loaded.VehicleTicks > loadedUp.Free.VehicleTicks,
            $"at {Name(new Plan(8))} the loaded run held {loadedUp.Loaded.VehicleTicks} Vehicle-Ticks "
            + $"against the free-flow control's {loadedUp.Free.VehicleTicks}. The two runs are the same "
            + "city priced with and without the volume-delay function, so equal totals mean the "
            + "function is decorative here too and this fixture has not moved the ratifier on.");

        // ⚠ And the load is past the knee rather than merely above the control's. v/c = 1 is where BPR
        // stops being a rounding error, so a fixture that peaked at 0.9 would satisfy the assertion
        // above by a margin the sub-Tick carry could swallow on a different seed.
        Assert.True(
            loadedUp.Loaded.PeakLoad > Ratio.One,
            $"the corridor peaked at v/c {Percent(loadedUp.Loaded.PeakLoad)}, which is below capacity "
            + "-- the function is being evaluated on the flat part of its own curve, which is the "
            + "condition that made 5c task 8 unable to ratify these numbers in the first place.");

        // ⚠ The clamp, at both ends, and the ends are what make it a reading rather than a level. See
        // The_clamp_catches_a_tail_and_does_not_bind_routinely for the ladder and what it settles.
        Assert.Equal(0, flat.Loaded.ClampedSegmentTicks);
        Assert.True(
            loadedUp.Loaded.ClampedSegmentTicks > 0,
            "nothing reached the clamp even at a peak past ten times capacity, so clamp_percent is "
            + "unreachable on this fixture and its refuting reading cannot be taken here.");

        // ⚠ And the free-flow control must never clamp, which is the guard the Runs check earns: a
        // Ruleset with no [traffic] has a Clamp of zero, so a comparison alone would report the
        // control clamping hardest in the fixture.
        Assert.Equal(0, loadedUp.Free.ClampedSegmentTicks);
    }

    /// <summary>
    /// <b>The clamp catches a tail; it does not bind routinely — and congestion changes no hiring.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>0002</c> §D1's two rows read on the world they name, and neither comes back the way the
    /// row expected.</b> The readings, taken 2026-08-15 on the ladder above at the shipped 3,600
    /// Vehicles an hour over two Days:
    /// </para>
    /// <code>
    /// rung    pop     peak v/c    clamped Segment-Ticks     employed    beyond   fast/mod/unsav
    ///  4x4   4,000       65.1%        0 of  5,304  0.00%      1,185          0   3,630 / 0 / 0
    ///  6x6   9,000       97.7%        0 of 14,167  0.00%      2,507          0   8,294 / 0 / 0
    ///  8x8  16,000    1,074.3%       12 of 28,948  0.04%      4,470          0  14,436 / 0 / 0
    /// 10x10 25,000    2,767.0%      272 of 51,952  0.52%      7,022          0  22,698 / 0 / 0
    /// </code>
    /// <para>
    /// ⚠ <b><c>clamp_percent</c>'s refuting reading does not fire, and the margin is large.</b> The
    /// stated refutation is <em>a clamp that binds routinely means the curve has stopped discriminating
    /// where the game is played</em>. At 10×10 the peak is <b>6.9× the clamp</b> and the clamp still
    /// takes <b>0.52%</b> of the Segment-Ticks that carried anybody. It is doing exactly the job it was
    /// recovered from S2 R8.0's delay for: bounding a quartic's tail without touching its body.
    /// <b>The share is a steep trend rather than a level</b> — 0.00, 0.00, 0.04, 0.52 — so it is 13×
    /// per rung against a 2.6× peak, and the reading is <em>not routine here</em> rather than
    /// <em>never routine</em>. ***A refuting reading that does not fire is a reading of the world it
    /// was taken in***, and this world is four rungs of one corridor.
    /// </para>
    /// <para>
    /// ⚠ <b><c>car_ownership_percent</c>'s two refuting readings BOTH fire, and they refute the wrong
    /// thing.</b> <c>0002</c> §D1 names <em>jobs beyond budget never leaving zero</em> and <em>the
    /// three rungs collapsing into fast</em>; <c>beyond</c> is <b>0</b> and <c>moderate</c> and
    /// <c>unsavoury</c> are <b>0</b> at every rung. The cause is not the rate. At 100% ownership every
    /// commute is a drive at 50 km/h across a city at most 4 km wide, and <c>adr/0095</c>'s three rungs
    /// are percentiles of a <b>foot-only</b> distribution — which <c>CLAUDE.md</c>'s own Commute Budget
    /// row says in as many words. ***A refuting reading named against one consequence cannot refute a
    /// number whose live consequence is a different one***: this rate is inert on <em>reach</em> and
    /// load-bearing on <em>congestion</em>, and it is the second that the world was built to expose.
    /// </para>
    /// <para>
    /// ⚠ <b>And congestion changes nothing about who works where — asserted as exact equality, which
    /// is the sharpest thing in this file.</b> The loaded and free-flow runs report the same
    /// <c>employed</c> and the same rung counts at every rung, to the Citizen, while their occupancies
    /// differ by <b>51.6%</b> at 10×10. That is <c>adr/0046</c> working as decided — <em>congestion is
    /// a cost paid and never a cost avoided</em> — and it is the corpus sweep's <em>the traffic model
    /// has no feedback term</em> arriving with a number instead of an argument. <b>The day a driver
    /// model closes <c>03 §3.4</c>'s loop, this equality breaks and says so</b>, which is why it is an
    /// assertion rather than a sentence.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_clamp_catches_a_tail_and_does_not_bind_routinely()
    {
        Plan plan = new(8);
        Reading loaded = Run(plan, Connected(), connected: true, $"{Name(plan)} loaded");
        Reading free = Run(plan, ConnectedWithoutTraffic(), connected: true, $"{Name(plan)} free-flow");

        // The clamp is reached and is nowhere near routine. Stated as a fraction of the loaded
        // Segment-Ticks rather than as a count, because a count grows with the fixture and the claim
        // is about a share. A twentieth is far above the 0.52% the ladder's top rung reads and far
        // below anything a person would call routine, so it discriminates without being a tuned edge.
        Assert.True(loaded.ClampedSegmentTicks > 0, "the clamp was never reached, so it is untested.");
        Assert.True(
            loaded.ClampedSegmentTicks * 20 < loaded.LoadedSegmentTicks,
            $"the clamp took {loaded.ClampedSegmentTicks} of {loaded.LoadedSegmentTicks} loaded "
            + "Segment-Ticks, which is routine rather than a tail -- so the curve has stopped "
            + "discriminating where this city is played, and it is clamp_percent that wants moving "
            + "rather than alpha.");

        // car_ownership_percent's two named refuting readings, asserted in the direction they were
        // measured so that a world which stops collapsing them says so. Both are properties of this
        // city's size against a driving population, NOT of the rate -- see the remark.
        Assert.Equal(0, loaded.Beyond);
        Assert.Equal(0, loaded.Moderate);
        Assert.Equal(0, loaded.Unsavoury);
        Assert.True(loaded.Fast > 0, "nobody was hired at all, so the rungs are empty rather than collapsed.");

        // ⚠ adr/0046, as an equality. Congestion is a cost PAID and never a cost AVOIDED, so pricing
        // the road moves what a journey costs and moves nothing about who took which job. This breaks
        // the day a driver model closes 03 §3.4's loop, which is the point of writing it down.
        Assert.Equal(free.Fast, loaded.Fast);
        Assert.Equal(free.Moderate, loaded.Moderate);
        Assert.Equal(free.Unsavoury, loaded.Unsavoury);
        Assert.True(
            loaded.VehicleTicks > free.VehicleTicks,
            "the two runs held the same occupancy, so the equalities above are two identical cities "
            + "rather than one city priced two ways.");
    }

    /// <summary>The block size this fixture assumes is the block size the Ruleset states.</summary>
    [Fact]
    public void The_assumed_block_size_is_the_shipped_one() =>
        Assert.Equal(BlockTiles, Connected().Roads.BlockTiles);

    // ---- the world -------------------------------------------------------------------------------

    private Reading Run(Plan plan, Ruleset rules, bool connected, string what)
    {
        ArgumentNullException.ThrowIfNull(rules);

        Simulation simulation = Start(plan, rules, connected);
        World world = simulation.World;
        RoadSegmentTable segments = world.Roads.Segments;

        long vehicleTicks = 0;
        long loadedSegmentTicks = 0;
        long clampedSegmentTicks = 0;
        long beyond = 0;
        long fast = 0;
        long moderate = 0;
        long unsavoury = 0;
        var peak = Ratio.Zero;

        for (int tick = 1; tick <= TickCount; tick++)
        {
            simulation.Step(new TickInput(default, rulesetHash: 0));

            // Drained every Tick because draining is what a flow's reading does. Summed here rather
            // than sampled at the end for TrafficLongRunTests' reason: these are flows, and a flow
            // read once is one Tick of 4,096 rather than the run.
            EmploymentActivity jobs = simulation.Employment.Drain();

            beyond += jobs.Beyond.Sum;
            fast += jobs.Fast.Sum;
            moderate += jobs.Moderate.Sum;
            unsavoury += jobs.Unsavoury.Sum;

            for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
            {
                if (!segments.Rows.IsLive(slot))
                {
                    continue;
                }

                int forward = segments.VolumeForward[slot];
                int backward = segments.VolumeBackward[slot];

                vehicleTicks += forward + backward;

                // The busier direction, never the sum: capacity is stated per direction, so adding
                // them would price a Segment against half its own road. TrafficDump.Step's rule.
                int busier = forward > backward ? forward : backward;
                Ratio load = segments.LoadOf(slot, busier, segments.FreeFlowOver(slot));

                peak = load > peak ? load : peak;

                if (load <= Ratio.Zero)
                {
                    continue;
                }

                loadedSegmentTicks++;

                // ⚠ Guarded on Runs, not merely on the comparison. A Ruleset with no [traffic] has a
                // Clamp of zero, so every occupied Segment would read as clamped and the free-flow
                // control would report the heaviest clamping in the fixture.
                if (rules.Traffic.Runs && load > rules.Traffic.Clamp)
                {
                    clampedSegmentTicks++;
                }
            }
        }

        _output.WriteLine(
            $"{what,-22} pop {plan.Population,6}  segments {Live(segments),4}  "
            + $"vehicle-Ticks {vehicleTicks,7}  peak v/c {Percent(peak),9}  "
            + $"employed {Employed(world),5}");
        _output.WriteLine(
            $"{"",-22} clamped {clampedSegmentTicks,7} of {loadedSegmentTicks,7} loaded Segment-Ticks "
            + $"({Share(clampedSegmentTicks, loadedSegmentTicks)})  "
            + $"beyond {beyond,6}  rungs fast {fast,5} moderate {moderate,5} unsavoury {unsavoury,5}");

        return new Reading(
            vehicleTicks, peak, clampedSegmentTicks, loadedSegmentTicks, beyond, fast, moderate,
            unsavoury);
    }

    /// <summary>What one run of <see cref="Run"/> is worth reading off it.</summary>
    /// <remarks>
    /// <para>
    /// <b>The clamp is counted in Segment-Ticks rather than in pricing events, and the difference is
    /// worth stating.</b> <c>[traffic] clamp_percent</c>'s refuting reading is <em>a clamp that binds
    /// routinely</em>, which is properly a share of the <em>drives priced</em>; what is countable
    /// without a new column in <c>Borough.Core</c> is a share of the <b>Segment-Ticks that carried
    /// anybody</b>. The two agree in direction and not in denominator — a clamped Segment prices every
    /// vehicle that enters it, so the Segment-Tick share is the road's exposure rather than the
    /// traveller's. ***A proxy is a reading of the thing it is a proxy for only while it is named as
    /// one.***
    /// </para>
    /// <para>
    /// <b>The four job counters are <c>[households] car_ownership_percent</c>'s refuting readings and
    /// not this fixture's own</b> — <c>0002</c> §D1 names <em>reach</em>: <c>jobs beyond budget</c>
    /// never leaving zero, or the three <c>adr/0095</c> rungs collapsing into <em>fast</em>. They are
    /// read here because this is the first world in which owning a car costs anything, which is that
    /// row's stated reason for naming it.
    /// </para>
    /// </remarks>
    private readonly record struct Reading(
        long VehicleTicks,
        Ratio PeakLoad,
        long ClampedSegmentTicks,
        long LoadedSegmentTicks,
        long Beyond,
        long Fast,
        long Moderate,
        long Unsavoury);

    /// <summary>
    /// A world under <paramref name="rules"/>, its Streets laid by the player or by the generator.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Which one is a parameter because inferring it was this fixture's first defect.</b> The
    /// branch was originally <c>rules.Roads.Runs</c> — read as <em>a Ruleset stating no <c>[roads]</c>
    /// cannot be Connected into, so it must be the generated case</em> — and both runs of the
    /// comparison went down the Connect path, producing two identical readings and an assertion that
    /// failed with the same number on both sides. ***A control is stated by the caller or it is not a
    /// control***: the two worlds differ in exactly one thing and the thing is not a property of the
    /// Ruleset, so nothing in the Ruleset could ever have selected it.
    /// </remarks>
    private static Simulation Start(Plan plan, Ruleset rules, bool connected)
    {
        InputLogBuilder builder = new(
            Seed, new WorldConfiguration(plan.Population), rulesetHash: 0);
        Simulation simulation = Replay.Start(builder.Build(), rules);

        // O(world) correctness guard off: this is a measurement over thousands of Ticks and the guard
        // is 95% of such a run (S0a).
        simulation.VerifyDecideWritesNothing = false;

        if (!connected)
        {
            simulation.Step(new TickInput(
                [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

            return simulation;
        }

        // A Tick apart, because a Zone carves against the faces that are standing.
        // LotSubdivider.SubdivideAt reads the StreetGrid, so zoning a block with no Street on it
        // yields nothing -- and since Relot reads a block's Zone off the Lots that survived on it, a
        // block that never had a Lot has nothing to remember it was zoned by.
        simulation.Step(new TickInput(Streets(plan), rulesetHash: 0));
        simulation.Step(new TickInput(Zoning(plan), rulesetHash: 0));

        Populate(simulation.World);

        return simulation;
    }

    /// <summary>
    /// The dumbbell, as one Connect command per Street Segment.
    /// </summary>
    /// <remarks>
    /// <b>Every edge is named by its lower endpoint and an axis</b>, which is <c>StreetAxis</c>'s whole
    /// shape — there is no South and no West, so a block's four faces are two East edges a row apart
    /// and two North edges a column apart, and a grid of them is laid without ever naming an edge
    /// twice.
    /// </remarks>
    private static Command[] Streets(Plan plan)
    {
        List<Command> commands = [];

        Grid(commands, plan, 0);
        Grid(commands, plan, plan.FarColumn);

        // The corridor: one row of Street across the gap, and the only thing joining the two.
        for (int column = plan.DistrictBlocks; column < plan.FarColumn; column++)
        {
            commands.Add(Lay(column, 0, StreetAxis.East));
        }

        return [.. commands];
    }

    /// <summary>Every face of a district-square block grid whose first column is <paramref name="at"/>.</summary>
    private static void Grid(List<Command> into, Plan plan, int at)
    {
        for (int row = 0; row <= plan.DistrictBlocks; row++)
        {
            for (int column = at; column < at + plan.DistrictBlocks; column++)
            {
                into.Add(Lay(column, row, StreetAxis.East));
            }
        }

        for (int column = at; column <= at + plan.DistrictBlocks; column++)
        {
            for (int row = 0; row < plan.DistrictBlocks; row++)
            {
                into.Add(Lay(column, row, StreetAxis.North));
            }
        }
    }

    /// <summary>One <c>Zone</c> per block of both districts.</summary>
    private static Command[] Zoning(Plan plan)
    {
        List<Command> commands = [];

        foreach (int at in (int[])[0, plan.FarColumn])
        {
            for (int column = at; column < at + plan.DistrictBlocks; column++)
            {
                for (int row = 0; row < plan.DistrictBlocks; row++)
                {
                    commands.Add(new Command(
                        CommandKind.Zone, Middle(column), Middle(row), zone: 1));
                }
            }
        }

        return [.. commands];
    }

    /// <summary>
    /// Buildings, Households and Citizens on whatever Lots the zoning produced.
    /// </summary>
    /// <remarks>
    /// <b>This was <see cref="SyntheticCity.PopulateInto"/>'s three loops, copied, because that method
    /// lays roads before it builds anything and the generator throws on a world that already has
    /// Segments.</b> <c>plans/0003</c> hash-moving queue item 9 split the populator's land half from
    /// its people half, so the copy is gone and this calls
    /// <see cref="SyntheticCity.PeopleInto"/> — which is the same code the generated cities run, on
    /// whatever Lots stand. The ratios are therefore the populator's ratios by construction rather
    /// than by two files agreeing, so this city differs from a generated one in its <em>road
    /// network</em> and in nothing else, which is what makes the comparison above mean what it says.
    /// </remarks>
    private static void Populate(World world)
    {
        Assert.True(
            world.Lots.Rows.LiveCount > 0,
            "zoning produced no Lots, so the Streets carried no frontage.");

        // The population is the world's own configuration, which is plan.Population -- see Build.
        SyntheticCity.PeopleInto(world, Key, Ticks.Zero);
    }

    // ---- Rulesets --------------------------------------------------------------------------------

    /// <summary>
    /// <c>congested.toml</c>'s tables at the <b>shipped</b> Street capacity.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The capacity goes back to 3,600 and that is the point of the fixture.</b>
    /// <c>congested.toml</c> exists because a generated city cannot congest at any authored capacity,
    /// so it cuts the road to 400 Vehicles an hour — a Segment holding <b>1.02</b> Vehicles — and its
    /// own header says that makes it a demonstration rather than a city. A Connect-laid world should
    /// not need that, and a reading taken at 400 could not tell the two causes apart.
    /// </remarks>
    private static Ruleset Connected() => Parse(Shipped());

    /// <summary>The same Ruleset with the volume-delay function taken out — the free-flow control.</summary>
    /// <remarks>
    /// Stripped by <b>text</b> and re-parsed, which is <c>TrafficDump.TryFreeFlow</c>'s method and is
    /// chosen over building a <c>Ruleset</c> in C# for <c>GoldenFixtures.Rules</c>' reason: a Ruleset
    /// assembled in code agrees with the loader by construction, so a control built that way is not the
    /// same file with one table removed.
    /// </remarks>
    private static Ruleset ConnectedWithoutTraffic()
    {
        string toml = Shipped();

        // ⚠ The TABLE, not the first mention of the string, and getting that wrong was this fixture's
        // second defect. `[traffic]` appears in a comment 53 lines above the table it names, so
        // IndexOf cut the file mid-sentence and took `[households]` with it -- and a Ruleset with no
        // `[households]` is one in which NOBODY OWNS A CAR. The control came back with 0 vehicle-Ticks
        // against the treatment's 6,784 and the assertion PASSED, reading as `the volume-delay
        // function does everything` when it meant `the control had no cars in it`.
        //
        // A CONTROL THAT DIFFERS IN TWO VARIABLES MEASURES NEITHER, and the tell was there to be read:
        // a free-flow run is the same journeys priced cheaper, so it can hold FEWER Vehicle-Ticks and
        // it cannot hold NONE. A zero on one side of a ratio is never a small number.
        int at = toml.IndexOf("\n[traffic]\n", StringComparison.Ordinal);

        Assert.True(at >= 0, "congested.toml states no [traffic] table, so there is nothing to strip.");

        string stripped = toml[..at];

        // The guard that defect earns, stated positively: what must SURVIVE the strip. Asserting that
        // `[traffic]` is gone would have passed on the broken version too, because it was.
        Assert.Contains("\n[households]\n", stripped, StringComparison.Ordinal);

        return Parse(stripped);
    }

    private static string Shipped() => File
        .ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "congested.toml"))
        .Replace(
            "street_capacity_per_hour       = 400",
            "street_capacity_per_hour       = 3600",
            StringComparison.Ordinal);

    private static Ruleset Parse(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    // ---- small change ----------------------------------------------------------------------------

    private static Command Lay(int column, int row, StreetAxis axis) => new(
        CommandKind.Connect,
        new Tiles(column * BlockTiles),
        new Tiles(row * BlockTiles),
        new ConnectPayload(axis, ConnectAction.Lay, RoadKind.Street).Encode());

    /// <summary>A Tile in the middle of a block, which is what the Zone verb is addressed by.</summary>
    private static Tiles Middle(int block) => new((block * BlockTiles) + (BlockTiles / 2));

    private static string Name(Plan plan) => $"{plan.DistrictBlocks}x{plan.DistrictBlocks}";

    private static int Live(RoadSegmentTable segments)
    {
        int total = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount; slot++)
        {
            total += segments.Rows.IsLive(slot) ? 1 : 0;
        }

        return total;
    }

    private static int Employed(World world)
    {
        int total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot)
                && world.Businesses.Rows.IsValid(world.Citizens.Workplace[slot]))
            {
                total++;
            }
        }

        return total;
    }

    /// <summary>A <see cref="Ratio"/> as a percentage, for something a person reads.</summary>
    private static string Percent(Ratio value) => $"{value.Raw * 100.0 / 65_536.0:F1}%";

    /// <summary>One count against another, as a percentage a person reads.</summary>
    private static string Share(long part, long whole) =>
        whole <= 0 ? "n/a" : $"{part * 100.0 / whole:F2}%";
}
