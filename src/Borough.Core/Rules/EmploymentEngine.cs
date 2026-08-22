using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// Tick phase 6, behind placement: a Citizen with no Workplace looking for one near home and taking
/// the first acceptable job. <c>adr/0081</c>, and <c>02 §5.2</c> step 2b's *at least one reachable job
/// in budget* arriving from the other side.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the first Trip <em>generator</em> in the project</b>, in the sense that matters:
/// milestone 5b built the Trip, the Leg, the Traveller and a real walk search, and every one of them
/// waited on a command to say where somebody was going. What was missing was never the movement — it
/// was a <b>reason to move</b>, and <c>06</c>'s *Mechanisms with no milestone* holds all seven of the
/// corpus's candidate reasons. This supplies one, and it supplies the only one that produces an
/// origin-destination pair from the city's own geography rather than from a fixture.
/// </para>
/// <para>
/// <b>It is <see cref="PlacementEngine"/>'s shape, deliberately and to the letter</b> — a sampled
/// sweep over a population, looking for something with room, taking the first that will have it. The
/// three numbers are the same three (<c>adr/0069</c>'s standing warning, which is why <c>[jobs]</c>
/// predicted them rather than discovering them), the sample is derived from an authored duration
/// (<c>adr/0059</c>), and the choice is satisficing rather than optimising (<c>adr/0017</c>). What
/// differs is that acceptance is no longer blind: placement's remark records that <i>"acceptance needs
/// rent, a commute and a tolerance; none exists"</i>, and this is where the commute exists.
/// </para>
/// <para>
/// <b>Two stages, and merging them would delete the reading the milestone is for.</b>
/// <see cref="BuildingResidency"/> supplies candidates from a box of Cells around home, and the
/// <b>walk decides acceptability</b> — so a Building on the far side of an Arterial with no crossing
/// is a candidate that <em>fails</em>, and the pass reports how often that happens. An index that
/// pre-filtered by reachability would answer the same question with the Severance invisible, which is
/// exactly what <c>03 §3.7</c>'s mechanism has to be able to show.
/// </para>
/// <para>
/// <b>The box is derived from the Commute Budget rather than authored, and that is a refusal to invent
/// a number.</b> A search radius chosen freely is a hash-bearing number with no ratifier, and worse
/// than that: an unbounded or over-wide draw is S2 R4's uniform origin-destination distribution, which
/// R4 measured to be <em>a different city</em> rather than a noisier one — a District-granular route's
/// detour goes from 18.52% to 128.82% between the two draws. So the radius is <b>what a walk within
/// the Budget can cover</b>, and a Ruleset with a <c>[jobs]</c> table and no
/// <c>commute_budget_minutes</c> is refused at load rather than given a default.
/// </para>
/// <para>
/// ⚠ <b>The paragraph above is the reason the box is <em>derived</em>, and it is not a claim that the
/// box <em>filters</em>. It does not, in any world this project builds.</b> Measured 2026-08-14 by
/// <c>JobSearchBoxTests</c>: the box is 67×67 = 4,489 Cells against a golden-fixture city of 100, and
/// every seeking Citizen's box holds <b>100.0%</b> of the Buildings in the world at 4,000, 10,000 and
/// 40,000 Citizens alike. The draw <b>is</b> S2 R4's uniform distribution today. It first narrows at
/// about 160,000 Citizens (11.7%), because a city's width is roughly <c>sqrt(population ÷ 3700)</c> km
/// and only then exceeds the 4.17 km a Budget-length walk covers. <b>The cities are smaller than a
/// commute, so the bound is real and inert</b> — the same shape as <c>foot_crossing_every</c>, which
/// 5a found correct and doing nothing below a threshold of its own.
/// </para>
/// <para>
/// ⚠ <b>And it is inert structurally rather than temporarily, because a time-derived radius grows with
/// whatever mode performs the commute.</b> Everything above is walking speed, since walking is all
/// there is. At 50 km/h the same ceiling reaches <b>41.7 km</b>, so a 1M city 19.2 km across sits
/// inside an 83 km box — and even the <em>fast</em> rung reaches 16.7 km. <b>A box derived from a
/// commute time can only ever filter in a foot-only world.</b> That is not an argument for authoring a
/// radius instead: it is a hash-bearing number with no ratifier either way, and the honest position is
/// that <b>the walk search is the filter and the box is a bound on how much ground the candidate draw
/// reads</b>, which is what it measurably is (5–6% of run time for a 5.3× box). Filed to
/// <c>plans/0002</c> §C; do not quietly re-tune the box to make this comment true.
/// </para>
/// <para>
/// <b>Four flows rather than two, because there are three different ways for this pass to do
/// nothing</b> and a single *employed* counter reads identically across all of them. *Considered −
/// seeking* is the price of sampling the population instead of a list of the unemployed; *seeking −
/// employed* is the shortage; and *beyond* counts the looks that found a real vacancy and could not
/// reach it, which is the geography talking. That is slice 7 task 9's <c>evaluations − due</c> lesson
/// applied before the fact rather than after it.
/// </para>
/// </remarks>
public sealed class EmploymentEngine
{
    private readonly World _world;
    private readonly WorldKey _key;

    /// <summary>Reusable walk-search state. One per engine, never shared across threads.</summary>
    private readonly WalkScratch _walk = new();

    /// <summary>Where the population sample is written. Grown to the widest sample, then reused.</summary>
    private int[] _sample = [];

    private int _tickConsidered;
    private int _tickSeeking;
    private int _tickEmployed;
    private int _tickBeyond;

    /// <summary>
    /// Times the pass walked a search box this Tick: once to size it, then once per candidate drawn.
    /// </summary>
    /// <remarks>
    /// <b>Walks rather than Cells, and the difference is what keeps it affordable.</b> A Cell counter
    /// would cost an increment inside <see cref="Space.BuildingResidency.CountIn"/>'s inner loop, which
    /// is the loop being measured; this costs one increment per call. The Cell figure is the product
    /// of this and the box's own area, and a reader who wants it has both.
    /// </remarks>
    private int _tickBoxWalks;

    /// <summary>
    /// Assignments made this Tick, by <see cref="CommuteRung"/>. Indexed by the enum's value, which
    /// is why <see cref="CommuteRung.Fast"/> being zero is stated as load-bearing where it is
    /// declared.
    /// </summary>
    private readonly int[] _tickRungs = new int[RungCount];

    private RuleFlow _consideredFlow;
    private RuleFlow _seekingFlow;
    private RuleFlow _employedFlow;
    private RuleFlow _beyondFlow;
    private RuleFlow _fastFlow;
    private RuleFlow _moderateFlow;
    private RuleFlow _unsavouryFlow;
    private RuleFlow _boxWalksFlow;

    /// <summary>The members of <see cref="CommuteRung"/>.</summary>
    private const int RungCount = 3;

    /// <param name="world">The tables this writes, and the Ruleset it writes under. Not copied.</param>
    /// <param name="key">The world seed, as the draws' first coordinate.</param>
    public EmploymentEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>Reads what assignment did since the last call, and resets the counters.</summary>
    public EmploymentActivity Drain()
    {
        var activity = new EmploymentActivity(
            _consideredFlow, _seekingFlow, _employedFlow, _beyondFlow,
            _fastFlow, _moderateFlow, _unsavouryFlow, _boxWalksFlow);

        _consideredFlow = default;
        _seekingFlow = default;
        _employedFlow = default;
        _beyondFlow = default;
        _fastFlow = default;
        _moderateFlow = default;
        _unsavouryFlow = default;
        _boxWalksFlow = default;

        return activity;
    }

    /// <summary>
    /// Runs one pass if this Tick's interval divides, employing whom it can.
    /// </summary>
    /// <param name="tick">The Tick being run: the trigger test and the draws' key.</param>
    public void Assign(Ticks tick)
    {
        JobRuleset jobs = _world.Rules.Jobs;

        if (!jobs.Runs || tick.Raw % jobs.Interval != 0)
        {
            CloseTick();
            return;
        }

        // The loader refuses [jobs] without a Budget, so this holds for every Ruleset it built. It is
        // asserted rather than assumed because a Ruleset can also be composed in a test.
        TripRuleset trips = _world.Rules.Trips;

        if (!trips.HasCommuteBudget)
        {
            throw new InvalidOperationException(
                "the Ruleset declares [jobs] but no [trips] commute_budget_minutes, so the assignment "
                + "pass has no search radius. The loader refuses this Ruleset; one composed in code "
                + "must state a Commute Budget or leave Jobs at JobRuleset.None.");
        }

        int population = _world.Citizens.Rows.LiveCount;
        int slots = _world.Citizens.Rows.SlotCount;

        if (population == 0)
        {
            CloseTick();
            return;
        }

        Cells radius = Radius(trips.CommuteBudget, _world.Rules.Roads.WalkSpeed);
        Span<int> into = Scratch(jobs.SampleFor(population));

        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.JobSeeker);

            into[draw] = (int)(value % (ulong)(uint)slots);
        }

        for (int i = 0; i < into.Length; i++)
        {
            int slot = into[i];

            // A freed row is a look that found nobody. It is silent rather than counted, because a
            // recycled slot is an artefact of storage and counting it would make the *considered*
            // flow a reading of the free list. The Citizen table is dense today — nothing kills a
            // Citizen — and the sample is derived from the live count, so if that ever stops being
            // true the pass under-delivers and this comment is where to look.
            if (!_world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            _tickConsidered++;

            if (_world.Buildings.Rows.IsValid(_world.Citizens.Workplace[slot]))
            {
                continue;
            }

            TravelMode mode = _world.ModeOf(slot);

            if (!Home(slot, mode, out Cells east, out Cells north, out Address door))
            {
                continue;
            }

            _tickSeeking++;

            if (TryEmploy(slot, east, north, radius, door, mode, trips, tick))
            {
                _tickEmployed++;
            }
        }

        CloseTick();
    }

    /// <summary>
    /// Looks at <c>candidates</c> Buildings near home and takes the first with a job inside the
    /// Commute Budget.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The draw is uniform over the Buildings in the box, and the Cell-uniform alternative was
    /// built first and rejected on a measurement.</b> <see cref="BuildingResidency.CountIn"/> carries
    /// the reasoning: drawing land rather than rows is <see cref="PlacementEngine.TryHouse"/>'s
    /// Lots-not-Buildings argument and reads better, but the shipped Rulesets hold on the order of
    /// sixty Buildings across 16,384 Cells, so it finds an employer about one occasion in four hundred
    /// and the pass would be unobservable in every world anybody runs.
    /// </para>
    /// <para>
    /// <b>So a Cell full of employers does attract seekers, and that is a claim rather than a
    /// default.</b> Job density draws workers in proportion to it, which is what an industrial
    /// district is for. What it is <em>not</em> is a preference — every Building in the box is equally
    /// likely per job slot, and nothing scores one above another, because scoring is <c>02 §5.4</c>'s
    /// and does not exist.
    /// </para>
    /// <para>
    /// <b>The best rung it drew, not the first acceptable one</b> (<c>adr/0095</c>, <c>adr/0017</c>).
    /// This is satisficing with an <em>ordered preference</em> rather than optimising: the draw is
    /// still <c>candidates</c> Buildings out of a box, nothing is sorted, and nobody looks at a
    /// Building they did not draw. What changed is that <see cref="CommuteRung.Fast"/> beats
    /// <see cref="CommuteRung.Moderate"/> among the ones they did — because a person offered a
    /// twelve-minute walk and a forty-minute walk in the same morning takes the twelve, and a binary
    /// filter cannot express that.
    /// </para>
    /// <para>
    /// <b>A fast candidate ends the search</b>, so the common case still costs one walk. Nothing can
    /// beat the top rung, so continuing would be computing costs whose answer cannot change the
    /// outcome — which is the difference between an ordered preference and a sort.
    /// </para>
    /// <para>
    /// ⚠ <b>The candidate is judged in the seeker's own mode, and that is <c>adr/0008</c> rather than
    /// a refinement.</b> Session F refused a per-mode weight on the Commute Budget so that a walk and
    /// a drive are compared on one clock — which only works if the clock is read in the mode the
    /// journey is actually made in. A driver judged on walking time would refuse jobs they can reach
    /// in ten minutes, and the shortfall would read as a labour-market finding.
    /// </para>
    /// <para>
    /// ⚠ <b>The <em>box</em> is still derived from the Budget at walking speed, and for a driver it is
    /// therefore far too small.</b> That is a live defect rather than a decision here, and it is
    /// already filed to <c>plans/0002</c> §C with a measurement: the box covers 44.9× the golden
    /// fixture's city and holds 100.0% of the Buildings in the world up to about 160,000 Citizens, so
    /// it filters nothing in any world this project can currently build and the walk search is the
    /// real bound. ⚠ <b>It becomes load-bearing the moment a city outgrows it</b>, and at that point a
    /// driver's catchment is silently clipped to a pedestrian's.
    /// </para>
    /// <para>
    /// ⚠ <b>The rung is Separation only, and the grading it produces is therefore half of what
    /// <c>01 §4</c> describes.</b> That section names two scarcities that read as long commutes —
    /// Congestion and Separation — and a walk Leg cannot carry the first, by construction rather
    /// than by omission (<c>03 §3.7</c>). So a city grades worse here when it <em>spreads</em> and
    /// never when it fills up, and the rungs' values are percentiles of a free-flow distribution.
    /// <c>adr/0070</c>: that is stated rather than compensated for. ⚠ <b>A drive Leg does not fix
    /// this yet</b> — 5c task 5 prices one at free-flow, and the congestion term is task 6.
    /// </para>
    /// </remarks>
    private bool TryEmploy(
        int slot, Cells east, Cells north, Cells radius, Address door, TravelMode mode,
        TripRuleset trips, Ticks tick)
    {
        CellRect box = CellRect.At(east, north).Dilate(radius).Clamp();

        if (box.IsEmpty)
        {
            return false;
        }

        _tickBoxWalks++;

        int here = _world.BuildingsInCells.CountIn(box);

        if (here == 0)
        {
            return false;
        }

        int candidates = _world.Rules.Jobs.Candidates;
        ulong id = _world.Citizens.Rows.IdAt(slot);

        CommuteRung best = CommuteRung.Fast;
        int bestBuilding = Rows.NoSlot;
        // What the winning walk cost, kept so that Employ can write it onto the Citizen as their
        // planned commute (adr/0101). The search already paid for it; storing it is what lets the
        // departure be `Shift start - this` instead of a number somebody had to choose.
        TravelTime bestCost = default;
        bool found = false;
        // Whether this *occasion* met a candidate the network could not deliver in time. One flag
        // rather than a count, because adr/0097's memory is denominated in occasions: `candidates`
        // is a Ruleset knob, so a per-candidate tally would be that knob multiplied by the quantity
        // milestone 19 wants to threshold. The per-candidate figure is `_tickBeyond` below, which is
        // an instrument and may depend on the knob.
        bool refusedForReach = false;

        for (int look = 0; look < candidates; look++)
        {
            // Keyed on the Citizen's monotonic id rather than the slot, so who somebody looks at does
            // not change because a row ahead of them was recycled. The look ordinal separates the
            // candidates within one occasion; the shift count is constant, which keeps BOR0204 quiet.
            ulong entity = Randomness.Mix(id ^ ((ulong)(uint)look << 32));
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.JobCandidate);

            _tickBoxWalks++;

            int building = _world.BuildingsInCells.NthIn(
                box, _world.Buildings, (int)(value % (ulong)(uint)here));

            if (building == Rows.NoSlot || !_world.HasJob(building))
            {
                continue;
            }

            // The second stage, and the whole reason the first one is geometric. A vacancy inside the
            // box that the Road Graph cannot deliver within the Budget is a *reachability* failure,
            // and it is counted separately because it is the only quantity in this pass that reports
            // the shape of the network rather than the state of the economy.
            TravelTime cost = WalkRouting.Cost(
                _world.Roads, mode, door, _world.AccessPoint(building, mode), trips.CrossingCost,
                _walk);

            if (!trips.TryRung(cost, out CommuteRung rung))
            {
                _tickBeyond++;
                refusedForReach = true;
                continue;
            }

            if (!found || rung < best)
            {
                found = true;
                best = rung;
                bestBuilding = building;
                bestCost = cost;
            }

            // Nothing beats the top rung, so the remaining draws cannot change the outcome and their
            // walks would be searches whose answer is discarded. Ties keep the earlier draw, which is
            // what makes the result a function of the draw order rather than of the scan order.
            if (rung == CommuteRung.Fast)
            {
                break;
            }
        }

        if (!found)
        {
            // Only where the network refused somebody. An occasion that drew nothing but full
            // employers is a Space refusal throughout, and adr/0097 remembers those nowhere -- a
            // count that moved on them would be a count of joblessness under a reachability name.
            // The two early returns above are the same case: an empty box and a box with no
            // Buildings in it are geometry, and neither has asked the Road Graph anything.
            if (refusedForReach)
            {
                _world.RecordReachFailure(slot);
            }

            return false;
        }

        // The reset is inside World.Employ rather than here, so that it cannot be missed by a second
        // path into employment. adr/0097: it resets on employment and on nothing else.
        _world.Employ(
            _world.Citizens.Rows.At(slot),
            _world.Buildings.Rows.At(bestBuilding),
            bestCost.ToTicksFloor());
        _tickRungs[(int)best]++;

        return true;
    }

    /// <summary>
    /// Where a Citizen starts from: the Cell their dwelling is in, and its pedestrian Access Point.
    /// </summary>
    /// <remarks>
    /// <b>A Citizen with no home does not look for work, and that is a statement about this pass
    /// rather than about employment.</b> A commute is anchored at a dwelling (<c>adr/0081</c>), so
    /// somebody in the Unplaced Pool has no origin to search from — there is no box to draw a Cell
    /// out of. They are counted as *considered* and not as *seeking*, because the queue they are in
    /// is the housing one and <see cref="PlacementEngine"/> is what serves it.
    /// </remarks>
    internal bool Home(int slot, TravelMode mode, out Cells east, out Cells north, out Address door)
    {
        east = default;
        north = default;
        door = default;

        if (!_world.Households.Rows.TryResolve(
                _world.Citizens.HouseholdOf[slot], out int household))
        {
            return false;
        }

        if (!_world.Buildings.Rows.TryResolve(
                _world.Households.Dwelling[household], out int dwelling))
        {
            return false;
        }

        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[dwelling], out int lot))
        {
            return false;
        }

        east = CellGrid.ToCells(_world.Lots.East[lot]);
        north = CellGrid.ToCells(_world.Lots.North[lot]);

        if (!CellGrid.Contains(east, north))
        {
            return false;
        }

        door = _world.AccessPoint(dwelling, mode);

        // A dwelling with no frontage is a hole the Trip model reports (adr/0079), and it is a hole
        // here too: nobody can start a walk from a door that is not on a Segment. Not seeking, for
        // the same reason as no home at all -- there is nothing this pass can do for them.
        return door.Exists;
    }

    /// <summary>
    /// How far a walk within the Commute Budget reaches, in whole Cells.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A straight-line bound on a network distance, so it over-supplies candidates and never
    /// under-supplies them</b> — which is the only direction that is safe. Every Building a walk
    /// within the Budget could reach is inside this box, because no route is shorter than the
    /// straight line; some Buildings inside it are unreachable, and those are precisely the ones the
    /// walk stage rejects and the *beyond* flow counts. Tightening this to the network distance would
    /// be doing the search twice.
    /// </para>
    /// <para>
    /// <b>The rounding is up, at both steps.</b> A Budget that reaches 33 Tiles reaches into the
    /// second Cell, and a box that stopped at the first would make the Budget's last few Tiles
    /// unreachable for no reason a designer could see.
    /// </para>
    /// </remarks>
    internal static Cells Radius(TravelTime budget, Speed walk)
    {
        if (walk.Raw <= 0)
        {
            return Cells.Zero;
        }

        // Q16.16 Ticks times Q16.16 Tiles/Tick is Q32.32 Tiles, in a long because the product of two
        // full-range Q16.16 values does not fit in one. Whole Tiles, rounded up.
        long tiles = IntegerMath.CeilDiv(
            (long)budget.Raw * walk.Raw, (long)Fixed.One * Fixed.One);

        // The map is the ceiling: a Budget long enough to walk across the world gives the whole map,
        // and CellRect.Clamp does the rest.
        long cells = IntegerMath.CeilDiv(
            tiles < CellGrid.WorldTiles ? tiles : CellGrid.WorldTiles, CellGrid.TilesPerCell);

        return new Cells((int)cells);
    }

    /// <summary>A scratch span of at least <paramref name="length"/>, reused between passes.</summary>
    private Span<int> Scratch(int length)
    {
        if (_sample.Length < length)
        {
            _sample = new int[length];
        }

        return _sample.AsSpan(0, length);
    }

    /// <summary>Folds this Tick's counts into the flows and resets them.</summary>
    private void CloseTick()
    {
        _consideredFlow = _consideredFlow.Fold(_tickConsidered);
        _seekingFlow = _seekingFlow.Fold(_tickSeeking);
        _employedFlow = _employedFlow.Fold(_tickEmployed);
        _beyondFlow = _beyondFlow.Fold(_tickBeyond);
        _fastFlow = _fastFlow.Fold(_tickRungs[(int)CommuteRung.Fast]);
        _moderateFlow = _moderateFlow.Fold(_tickRungs[(int)CommuteRung.Moderate]);
        _unsavouryFlow = _unsavouryFlow.Fold(_tickRungs[(int)CommuteRung.Unsavoury]);
        _boxWalksFlow = _boxWalksFlow.Fold(_tickBoxWalks);

        _tickConsidered = 0;
        _tickSeeking = 0;
        _tickEmployed = 0;
        _tickBeyond = 0;
        _tickBoxWalks = 0;
        Array.Clear(_tickRungs);
    }
}

/// <summary>What the assignment pass did over a Census interval.</summary>
/// <remarks>
/// <b><see cref="Employed"/> is the sum of the three rungs, and the redundancy is deliberate.</b> A
/// Census counter is an instrument rather than state, so a derivable reading is not the duplication
/// <c>adr/0064</c> warns about — and a counter that <em>must</em> equal the sum of three others is a
/// free consistency check on the instrument, which is what
/// <c>EmploymentRungTests</c> holds it to. Deleting it would save nothing and lose that.
/// </remarks>
/// <param name="Considered">Live Citizens the pass looked at.</param>
/// <param name="Seeking">Of those, the ones with no Workplace and a home to search from.</param>
/// <param name="Employed">Of those, the ones who took a job.</param>
/// <param name="Beyond">Candidate vacancies rejected because the walk exceeded the ceiling.</param>
/// <param name="Fast">Of those employed, the ones whose commute is <see cref="CommuteRung.Fast"/>.</param>
/// <param name="Moderate">Of those employed, <see cref="CommuteRung.Moderate"/>.</param>
/// <param name="Unsavoury">Of those employed, <see cref="CommuteRung.Unsavoury"/>.</param>
/// <param name="BoxWalks">
/// Times the pass walked a search box: once per seeker to size it, then once per candidate drawn.
/// Multiply by the box's own area for the Cells visited, which is the quantity a cost argument wants.
/// </param>
public readonly record struct EmploymentActivity(
    RuleFlow Considered, RuleFlow Seeking, RuleFlow Employed, RuleFlow Beyond,
    RuleFlow Fast, RuleFlow Moderate, RuleFlow Unsavoury, RuleFlow BoxWalks);
