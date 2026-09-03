using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Tests.Golden;

/// <summary>
/// What the golden session <em>reaches</em>, as distinct from what it hashes to.
/// </summary>
/// <remarks>
/// <para>
/// <b>A baseline records what a run did, so a change that narrows what the run reaches is invisible
/// in it by construction.</b> That sentence is slice 10 task 11's finding and it is the whole reason
/// this file exists: a derived sample of 1 stopped the committed trace ever landing on the Zone
/// Rule's create branch, every hash still moved, every test still passed, and half a mechanism went
/// uncovered for a slice. <c>GoldenHashTests</c> can only ever say <i>the number is the number</i>.
/// These tests say <i>the run went through the door</i>.
/// </para>
/// <para>
/// <b>5a-bis made the hazard concrete rather than theoretical.</b> Zoning a Tile now zones the
/// <em>block</em> it falls in, and eight of the session's eleven original commands named Tiles inside
/// a single 32-Tile block that the populator had already carved. Re-recording without moving them
/// would have retired the <c>zone</c> verb from the baseline while producing a full set of freshly
/// correct hashes — the failure mode exactly, on its second outing in three slices.
/// </para>
/// <para>
/// <b>Every assertion here is an inequality or a branch, never a hash.</b> Pinning a Lot count in
/// two places would put this file in competition with the committed trace, and
/// <c>plans/0012</c> <i>Cause 1</i> is about what happens next.
/// </para>
/// </remarks>
public sealed class GoldenSessionCoverageTests
{
    /// <summary>The block the session bulldozes a face off, zones, restores and bulldozes again.</summary>
    private static (int Column, int Row) Edited => (3, 0);

    // ⚠ (5, 5) was tried first and cost one Lot that nothing here could see. The bulldozed south face
    // of a block is the NORTH face of the block below it, so at (5, 5) the edit reached into (5, 4) --
    // which the populator subdivides -- and the session's carve count came out one short of the
    // per-block sum. At (50, 50) the question never arose, because the populator paved one row.
    // A road edit is not local to the block that names it, and it takes a neighbour with it.
    //
    // ⚠ BOTH OF THESE MOVED TO ROW 0 AT plans/0055, with every Zone command in the session --
    // GoldenFixtures.Session() carries why there was nowhere further to go. AND ROW 0 ANSWERS THE
    // PARAGRAPH ABOVE OUTRIGHT: the south face of a row-0 block is the map's boundary street, so
    // there is no block below for the edit to reach into. The hazard is not avoided by a margin here,
    // it is absent.

    /// <summary>The block the session strips of all four faces before zoning it.</summary>
    private static (int Column, int Row) Stripped => (7, 0);

    /// <summary>
    /// <b>The lattice spacing the fixture states is the one the Ruleset states.</b>
    /// </summary>
    /// <remarks>
    /// <see cref="GoldenFixtures.BlockTiles"/> is a copy of <c>[roads] block_tiles</c>, held here
    /// rather than read so that retuning the Ruleset breaks a test instead of silently relocating
    /// every command in the session. This is the test it breaks.
    /// </remarks>
    [Fact]
    public void The_fixtures_lattice_is_the_rulesets_lattice()
    {
        Assert.True(
            GoldenFixtures.Catalogue().TryResolve(GoldenFixtures.RulesetHash, out Ruleset rules),
            "the golden Ruleset is not in its own catalogue");

        Assert.Equal(GoldenFixtures.BlockTiles, rules.Roads.BlockTiles);
    }

    /// <summary>
    /// <b>Every Zone command in the session carves land, and none of them lands on another's block.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Against a populate-only control, because the populator carves too.</b> The session's own
    /// <c>populate</c> subdivides along lattice row 0 until it has land for the Buildings it wants, so
    /// a raw Lot count says nothing about the commands. The control is the same log with every command
    /// but <c>populate</c> removed; the difference is what the player did.
    /// </para>
    /// <para>
    /// <b>The two deliberate exceptions are named rather than averaged away.</b>
    /// <see cref="Stripped"/> is zoned after all four of its faces are gone and yields nothing — that
    /// is how the refusal branch reaches the baseline at all — and <see cref="Edited"/> ends the run
    /// three Lots short because the session bulldozes its south face again at Tick 300. Folding either
    /// into a tolerance is what would let an accidental no-op back in.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_zone_command_in_the_session_reaches_land_it_can_carve()
    {
        int zoneCommands = 0;
        InputLog session = GoldenFixtures.Session();

        for (int i = 0; i < session.Count; i++)
        {
            if (session.Entry(i).Command.Kind == CommandKind.Zone)
            {
                zoneCommands++;
            }
        }

        int carved = At(session, Applied).Lots.Rows.LiveCount
            - At(PopulateOnly(session), Applied).Lots.Rows.LiveCount;

        // Eleven ordinary blocks at eight Lots each, plus Edited's five, plus Stripped's nothing.
        int expected = ((zoneCommands - 2) * LotsPerBlock) + (LotsPerBlock - 3);

        Assert.True(
            carved == expected,
            $"the session's {zoneCommands} zone commands carved {carved} Lots where {expected} were "
            + "expected. A shortfall that is a multiple of a block face means two commands are naming "
            + "the same block, or a block the populator already carved -- which costs the baseline the "
            + "coverage rather than the correctness, so no hash test can see it. Move the command, do "
            + "not lower this.");
    }

    /// <summary>
    /// <b>The session refuses a block it has stripped of frontage, and refusal is silent.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §2.2</c>'s third rule — <i>"land that cannot be given frontage stays unlotted and
    /// undevelopable"</i> — is the half of the subdivider that produces nothing, so it is the half a
    /// baseline covers by accident and never on purpose. The session bulldozes all four faces of
    /// <see cref="Stripped"/> in one Tick and zones it in the next.
    /// </remarks>
    [Fact]
    public void The_session_zones_a_block_with_no_frontage_and_gets_nothing()
    {
        Assert.Equal(0, LotsOn(At(GoldenFixtures.Session(), Applied), Stripped));
    }

    /// <summary>
    /// <b>A road edit re-subdivides: the session both frees Lots and creates them after the fact.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three readings of one block, because the two directions are separate branches.</b> At Tick
    /// 131 <see cref="Edited"/> has been zoned with its south face already gone, so it holds the seven
    /// Lots its three remaining faces can carry rather than ten. At Tick 201 the face has been laid
    /// back and <c>Resubdivide</c> has found a block that gained frontage — three more. At Tick 301 it
    /// is gone again and the three are freed, because they are vacant.
    /// </para>
    /// <para>
    /// <b>Seven and not eight, and the asymmetry is real.</b> A block takes the Left side of its south
    /// face, and Left is the even indices of five — three of them — against Right's two. So a block's
    /// four faces carry 3, 2, 2 and 3, which is odd-and-even house numbering showing through
    /// (<c>adr/0078</c>) rather than an off-by-one.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_road_edit_in_the_session_re_subdivides_in_both_directions()
    {
        InputLog session = GoldenFixtures.Session();

        Assert.Equal(LotsPerBlock - 3, LotsOn(At(session, new Ticks(131)), Edited));
        Assert.Equal(LotsPerBlock, LotsOn(At(session, new Ticks(201)), Edited));
        Assert.Equal(LotsPerBlock - 3, LotsOn(At(session, new Ticks(301)), Edited));
    }

    /// <summary>
    /// <b>The committed session employs somebody, and it condemns a Building somebody works in.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this file exists for, applied to 5b-bis's mechanism.</b> Every hash in the baseline
    /// would stay green if the assignment pass stopped running — a change that narrows what a run
    /// *reaches* produces a full set of freshly correct numbers, which is slice 10 task 11's finding
    /// and the reason this class was written. So the session is held to <em>reaching</em> the pass
    /// rather than to any particular hash it produces.
    /// </para>
    /// <para>
    /// <b>The second assertion is the one worth having.</b> Employment on its own would be satisfied
    /// by a city that hires everybody once and never changes; what makes the baseline cover the
    /// interesting path is that this session <b>demolishes</b>, so a Workplace handle is severed under
    /// a worker and the Citizen re-enters assignment. That is the asymmetry task 2 decided — a
    /// bulldozed employer leaves a severed handle where a lowered ceiling clears one — and it is
    /// executed here rather than only asserted in a unit test.
    /// </para>
    /// <para>
    /// ⚠ <b>What the session does <em>not</em> reach is the Commute Budget refusing a walk</b>, and
    /// that is asserted in <c>JobAssignmentTests</c> instead, in both directions. This world is 1,000
    /// Citizens on one contiguous strip of blocks, so nothing in it is twenty minutes from anything.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_session_employs_people_and_bulldozes_an_employer()
    {
        World world = At(GoldenFixtures.Session(), new Ticks(GoldenFixtures.Ticks));

        int employed = 0;
        int severed = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            Handle<Business> workplace = world.Citizens.Workplace[slot];

            if (world.Businesses.Rows.IsValid(workplace))
            {
                employed++;
            }
            else if (!workplace.Equals(default))
            {
                severed++;
            }
        }

        Assert.True(employed > 0, "nobody in the committed session holds a job.");
        Assert.True(severed > 0, "the committed session never bulldozes an employer.");
    }

    /// <summary>
    /// <b>The committed session generates commute Trips, and until task 5 it contained no Trip at
    /// all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This closes a hole task 3 opened and named.</b> The <c>[trips]</c> table shipped with the
    /// whole Trip model sitting outside the committed baseline — the golden session holds no
    /// <c>trip</c> command, so nothing in the trace exercised <c>TripEngine</c>, the Traveller cursor
    /// or a Fate. A generator fixes that without a command, which is the difference between
    /// <c>adr/0080</c>'s door and <c>adr/0081</c>'s mechanism.
    /// </para>
    /// <para>
    /// <b>Read in flight rather than at release, because a Fate frees the row.</b> A Trip that has
    /// ended is gone by the end of the Tick it ended on, so the only Trips a finished run can be asked
    /// about are the ones still walking — which is also why this asserts an inequality rather than a
    /// count.
    /// </para>
    /// <para>
    /// ⚠ <b>Sampled across the run rather than at the end, since 2026-08-13.</b> It read the final
    /// Tick alone until <c>adr/0094</c> took the Day to 2048, and then found <b>nothing at all</b> —
    /// for two reasons at once, neither of them a regression. The departure window is
    /// <c>ceil(TICKS_PER_DAY ÷ commute_peak_factor)</c>, which fell from 2,731 to <b>683</b>, so every
    /// departure now happens in the first third of the session instead of being spread past its end;
    /// and a walk costs four times fewer Ticks, so a Trip is in flight for a quarter as long. <b>The
    /// session went from catching the tail of a departure wave to finishing an entire Day's commuting
    /// with time to spare</b>, and reading one instant found the quiet after it.
    /// </para>
    /// <para>
    /// The paragraph this replaces said the session <em>"reaches three quarters of the departure
    /// phases and no Citizen in it departs twice"</em>. It now covers <b>every</b> phase, because the
    /// session is 2,048 Ticks and a Day is 2,048 Ticks — <b>coverage went up and the assertion that
    /// measured it went to zero</b>, which is a reading taken at one instant behaving as if it were a
    /// reading over a run.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_session_sends_people_to_work_without_a_trip_command()
    {
        InputLog session = GoldenFixtures.Session();
        Simulation simulation = Replay.Start(session, GoldenFixtures.Catalogue());
        simulation.VerifyDecideWritesNothing = false;

        int busiest = 0;
        int samples = 0;

        // One pass, looked at every Stride Ticks. A Fate frees the row, so no single instant can be
        // relied on to hold a Trip -- what is asserted is that some instant does, which is what "the
        // session generates commutes" means and what the final Tick alone was standing in for.
        for (int tick = Stride; tick <= GoldenFixtures.Ticks; tick += Stride)
        {
            Replay.Trace(simulation, session, new Ticks(Stride), Stride, []);
            samples++;

            World world = simulation.World;
            int commuting = 0;

            for (int slot = 0; slot < world.Trips.Rows.SlotCount; slot++)
            {
                if (world.Trips.Rows.IsLive(slot)
                    && (TripPurpose)world.Trips.Purpose[slot] == TripPurpose.Commute)
                {
                    commuting++;
                }
            }

            busiest = commuting > busiest ? commuting : busiest;
        }

        Assert.True(busiest > 0, $"the committed session generates no commute Trip at any of "
            + $"{samples} samples across its run, so nothing in the baseline covers the commute "
            + "generator.");
    }

    /// <summary>
    /// How often this test looks at the city, in Ticks.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A DEPARTURE WINDOW IS NOT A STRIDE, and this test read one with the other twice.</b>
    /// It sampled at <c>Ticks × sample ÷ 8</c>, so the interval between looks was a fixed fraction of
    /// the session and moved whenever the session's length did. Departures do not: they fall in the
    /// first <c>ceil(TICKS_PER_DAY ÷ commute_peak_factor)</c> = <b>683</b> Ticks of each Day, for
    /// ever. At 2,048 Ticks the stride was 256 and two looks landed inside a window; milestone 17
    /// took the session to 8,192, the stride to 1,024, and <b>every one of the eight looks landed in
    /// the quiet after a wave</b> — 1,024, 2,048, 3,072 … all past 683 and all short of the next
    /// Day's. The city was commuting exactly as much as before.
    /// <para>
    /// ⚠ <b>This is the second time, and the first is recorded three paragraphs up</b>: <c>adr/0094</c>
    /// took the Day to 2,048, the window fell 2,731 → 683, and reading the final Tick alone found
    /// nothing. Sampling was the repair, and it inherited the same defect one level along. ***A
    /// sample rate derived from the run's length cannot see a phenomenon whose period is derived from
    /// the Day***, and no session length is safe from it — 8 is a count where a bound was needed.
    /// </para>
    /// <para>
    /// <b>So the stride is stated against the window rather than against the session.</b> 256 Ticks
    /// is comfortably inside 683, so at least two looks fall in every departure wave whatever the
    /// session's length becomes. It is also one pass now rather than eight replays: <c>Replay.Trace</c>
    /// advances a simulation from wherever it is, so the run is walked once and looked at on the way.
    /// </para>
    /// </remarks>
    private const int Stride = 256;

    /// <summary>
    /// <b>The driving session parks cars and walks at both ends of the drive.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Milestone 7's Definition of done: <i>the walk Leg's cost is non-zero for at least one Citizen
    /// in the committed golden session, so the baseline covers the mechanism.</i> A trace cannot say
    /// this — every sample in <c>driving-session-trace.txt</c> would be just as green over a city in
    /// which every Car Park sat empty and every walk cost nothing, which is precisely what the first
    /// session's numbers <em>are</em>. The obligation is a claim about what the run reaches, so it is
    /// a test here rather than a number there.
    /// </para>
    /// <para>
    /// <b>Both ends, because they are different mechanisms and only one of them is bounded.</b> The
    /// walk <em>from</em> the car is the arrival: <c>World.TryChooseParking</c> picked a space inside
    /// a ball of <c>[parking] radius_metres</c> around the destination's door, so this walk exists
    /// the first time anybody drives anywhere. The walk <em>to</em> the car is the departure, and it
    /// costs nothing until somebody has parked away from home and come back — which is what
    /// <see cref="GoldenFixtures.DrivingTicks"/> is two Days for, and its remarks carry the counts.
    /// </para>
    /// <para>
    /// ⚠ <b>It observes every Tick rather than sampling, and that is not thoroughness.</b>
    /// <c>TripEngine.Release</c> frees a Leg as its Trip resolves, so a Leg exists for the length of
    /// one journey and no instant holds them all — the failure milestone 7 task 5 paid for with a
    /// test that walked <c>world.Legs</c> after the run and found <b>zero</b> Legs of either mode.
    /// The eight-sample shape of <see cref="The_session_sends_people_to_work_without_a_trip_command"/>
    /// works there because a Trip outlives its Legs; it would not work here.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_driving_session_walks_to_and_from_a_car()
    {
        InputLog session = GoldenFixtures.DrivingSession();
        Simulation simulation = Replay.Start(session, GoldenFixtures.DrivingCatalogue());
        simulation.VerifyDecideWritesNothing = false;

        int drives = 0;
        int arrival = 0;
        int departure = 0;

        for (int step = 0; step < GoldenFixtures.DrivingTicks; step++)
        {
            // A cadence no Tick in this run divides, so Trace steps without folding a State Hash.
            // Hashing the world 4,096 times to reach a claim about Legs would be the run's whole
            // cost spent on the one thing this test is not about.
            Replay.Trace(simulation, session, new Ticks(1), int.MaxValue, []);

            LegTable legs = simulation.World.Legs;

            for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
            {
                if (!legs.Rows.IsLive(slot) || (TravelMode)legs.Mode[slot] != TravelMode.Car)
                {
                    continue;
                }

                drives++;
                departure = Longer(legs, Preceding(legs, slot), departure);
                arrival = Longer(legs, legs.Next[slot] - 1, arrival);
            }
        }

        Assert.True(drives > 0, "nobody in the driving session ever drove, so the whole of "
            + "congested.toml's [households] table is outside the committed trace. The session's "
            + "Ruleset or its populate command has moved.");

        Assert.True(arrival > 0, "every walk FROM a car in the driving session cost nothing, so the "
            + "baseline covers parking supply and not the walk it buys. adr/0008 makes the walk a "
            + "simulated Leg; a zero everywhere means the endpoint swap is not reaching the Car "
            + "Park's own Address.");

        Assert.True(departure > 0, "every walk TO a car in the driving session cost nothing, which is "
            + "what a session too short to reach a second journey looks like -- a Citizen's first "
            + "drive starts at their own kerb because they hold no space yet. Check "
            + "GoldenFixtures.DrivingTicks before looking at the simulation.");
    }

    /// <summary>
    /// The greater of <paramref name="best"/> and the cost of the foot Leg at <paramref name="slot"/>.
    /// </summary>
    private static int Longer(LegTable legs, int slot, int best)
    {
        if (slot < 0 || !legs.Rows.IsLive(slot) || (TravelMode)legs.Mode[slot] != TravelMode.Foot)
        {
            return best;
        }

        int cost = (int)legs.Time[slot].Raw;

        return cost > best ? cost : best;
    }

    /// <summary>The Leg whose <c>Next</c> is <paramref name="target"/>, or <c>-1</c>.</summary>
    /// <remarks>
    /// A Leg list is singly linked — <c>adr/0075</c>, and a back pointer would be a second copy of
    /// the order — so the Leg before one is found by looking, exactly as <c>TripDump</c> and
    /// <c>ParkingDump</c> do it.
    /// </remarks>
    private static int Preceding(LegTable legs, int target)
    {
        for (int slot = 0; slot < legs.Rows.SlotCount; slot++)
        {
            if (legs.Rows.IsLive(slot) && legs.Next[slot] - 1 == target)
            {
                return slot;
            }
        }

        return -1;
    }

    /// <summary>How many Lots one block of the lattice carries when all four of its faces are Streets.</summary>
    private const int LotsPerBlock = 8;

    /// <summary>
    /// The Tick every command has been applied by, and no further.
    /// </summary>
    /// <remarks>
    /// <b>Short of the run's 2,048 on purpose.</b> The Zone Rules demolish, and a Lot count taken at
    /// the end would be measuring them rather than the subdivider.
    /// </remarks>
    private static Ticks Applied => new(402);

    /// <summary><paramref name="session"/>'s world, run to <paramref name="tick"/> and no further.</summary>

    private static World At(InputLog session, Ticks tick)
    {
        Simulation simulation = Replay.Start(session, GoldenFixtures.Catalogue());
        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        simulation.VerifyDecideWritesNothing = false;

        Replay.Trace(simulation, session, tick, (int)tick.Raw, []);

        return simulation.World;
    }

    /// <summary>
    /// The same session with every command but <c>populate</c> removed.
    /// </summary>
    /// <remarks>
    /// <b>The control, and it is built from the session rather than beside it</b> — a hand-written
    /// twin would be a second copy of the seed, the sizing and the Ruleset hash, and the useful
    /// property of a control is that it differs in exactly one thing.
    /// </remarks>
    private static InputLog PopulateOnly(InputLog session)
    {
        InputLogBuilder builder = new(
            session.Seed, session.Configuration, session.RulesetHash);

        for (int i = 0; i < session.Count; i++)
        {
            (Ticks tick, Command command) = session.Entry(i);

            if (command.Kind == CommandKind.Populate)
            {
                builder.Append(tick, command);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// How many Lots belong to a block — <b>which is not how many fall inside its bounds</b>.
    /// </summary>
    /// <remarks>
    /// A Lot sits <em>on</em> a Segment, so half of every face's Lots belong to the block across the
    /// street. The side is what decides, exactly as <c>LotSubdivider.BlockOf</c> decides it, and this
    /// walks the frontage rather than the coordinates so that a Lot which has lost its Street counts
    /// for neither block.
    /// </remarks>
    private static int LotsOn(World world, (int Column, int Row) block)
    {
        StreetGrid streets = world.Roads.Streets;
        int count = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot) || !world.Lots.HasFrontage(slot))
            {
                continue;
            }

            var side = (StreetSide)world.Lots.Side[slot];
            int east = world.Lots.East[slot].Raw;
            int north = world.Lots.North[slot].Raw;

            int column = Borough.Core.Arithmetic.IntegerMath.FloorDiv(east, streets.BlockTiles);
            int row = Borough.Core.Arithmetic.IntegerMath.FloorDiv(north, streets.BlockTiles);

            if (north == row * streets.BlockTiles)
            {
                if (side == StreetSide.Right)
                {
                    row--;
                }
            }
            else if (side == StreetSide.Left)
            {
                column--;
            }

            if (column == block.Column && row == block.Row)
            {
                count++;
            }
        }

        return count;
    }
}
