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

    private RuleFlow _consideredFlow;
    private RuleFlow _seekingFlow;
    private RuleFlow _employedFlow;
    private RuleFlow _beyondFlow;

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
            _consideredFlow, _seekingFlow, _employedFlow, _beyondFlow);

        _consideredFlow = default;
        _seekingFlow = default;
        _employedFlow = default;
        _beyondFlow = default;

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

            if (!Home(slot, out Cells east, out Cells north, out Address door))
            {
                continue;
            }

            _tickSeeking++;

            if (TryEmploy(slot, east, north, radius, door, trips, tick))
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
    /// <b>First acceptable rather than nearest or best</b> (<c>adr/0017</c>). Nearest would need the
    /// candidates sorted by a cost that has to be computed for all of them, which is optimising with
    /// extra steps and is the thing <c>CONTEXT.md</c> refuses when it says there is no proximity
    /// scope. The Budget is the filter; the order is the draw.
    /// </para>
    /// </remarks>
    private bool TryEmploy(
        int slot, Cells east, Cells north, Cells radius, Address door, TripRuleset trips, Ticks tick)
    {
        CellRect box = CellRect.At(east, north).Dilate(radius).Clamp();

        if (box.IsEmpty)
        {
            return false;
        }

        int here = _world.BuildingsInCells.CountIn(box);

        if (here == 0)
        {
            return false;
        }

        int candidates = _world.Rules.Jobs.Candidates;
        ulong id = _world.Citizens.Rows.IdAt(slot);

        for (int look = 0; look < candidates; look++)
        {
            // Keyed on the Citizen's monotonic id rather than the slot, so who somebody looks at does
            // not change because a row ahead of them was recycled. The look ordinal separates the
            // candidates within one occasion; the shift count is constant, which keeps BOR0204 quiet.
            ulong entity = Randomness.Mix(id ^ ((ulong)(uint)look << 32));
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.JobCandidate);

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
                _world.Roads, door, _world.PedestrianAccessPoint(building), trips.CrossingCost, _walk);

            if (!trips.WithinBudget(cost))
            {
                _tickBeyond++;
                continue;
            }

            _world.Employ(_world.Citizens.Rows.At(slot), _world.Buildings.Rows.At(building));

            return true;
        }

        return false;
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
    private bool Home(int slot, out Cells east, out Cells north, out Address door)
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

        door = _world.PedestrianAccessPoint(dwelling);

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
    private static Cells Radius(TravelTime budget, Speed walk)
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

        _tickConsidered = 0;
        _tickSeeking = 0;
        _tickEmployed = 0;
        _tickBeyond = 0;
    }
}

/// <summary>What the assignment pass did over a Census interval.</summary>
/// <param name="Considered">Live Citizens the pass looked at.</param>
/// <param name="Seeking">Of those, the ones with no Workplace and a home to search from.</param>
/// <param name="Employed">Of those, the ones who took a job.</param>
/// <param name="Beyond">Candidate vacancies rejected because the walk exceeded the Budget.</param>
public readonly record struct EmploymentActivity(
    RuleFlow Considered, RuleFlow Seeking, RuleFlow Employed, RuleFlow Beyond);
