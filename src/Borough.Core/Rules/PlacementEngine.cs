using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>
/// Tick phase 6, ahead of the Zone Rules: the Unplaced Pool draining into vacant declared capacity
/// in Buildings that already stand. <c>02 §5.2</c> step 2, and <c>adr/0069</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Until this existed, <c>World.Place</c> had exactly one caller and it was inside
/// <c>ZoneRuleEngine.Create</c>.</b> Nothing put a Household into a Building that already stood, so
/// the only way to be housed was for somebody to raise you a house — one Household deep, once, at
/// construction. Of <c>02 §5.2</c>'s six steps only step 5 existed, and the missing one was read as a
/// missing *number* by two ledger entries before it was read as a missing *mechanism*.
/// </para>
/// <para>
/// <b>The ordering is the decision.</b> <c>02 §1.1</c> calls the phase ordering the determinism
/// contract, and running ahead of the Zone Rules is what makes their create predicate a statement
/// about **vacancy** rather than about population: a Household still in the Pool when a developer
/// looks is one the standing stock could not house. A developer does not build while there are empty
/// flats. That is strictly stronger than what it replaces — the old predicate read a Pool that
/// construction drained one Household at a time, so a wide sample could build ahead of demand by up
/// to the sample size within one trigger.
/// </para>
/// <para>
/// <b>It is blind, on <c>adr/0054</c>'s existing reasoning and not on a new one.</b> Acceptance needs
/// rent, a commute and a tolerance; none exists, so any member would take any dwelling. This moves
/// that same draw to a second site rather than extending the argument, and <c>02 §5.2</c> step 2b's
/// hard filter — *affordable? at least one reachable job in budget?* — lands **here** when the choice
/// model arrives, instead of being retrofitted into a Zone Rule where it has no business being.
/// </para>
/// <para>
/// <b>Sampled rather than exhaustive, which is <c>02 §5.3</c>: sampling is a behaviour model and not
/// an optimisation.</b> A Household considers <c>candidates</c> dwellings and takes the first with
/// room; finding none, it waits for its next occasion. Nothing is recorded about why, because a
/// refusal reason is milestone 9a's and there is no reason to record while the filter is blind.
/// </para>
/// </remarks>
public sealed class PlacementEngine
{
    private readonly World _world;
    private readonly WorldKey _key;

    /// <summary>Where the Pool sample is written. Grown to the widest sample, then reused.</summary>
    private int[] _sample = [];

    private int _tickConsidered;
    private int _tickPlaced;

    private RuleFlow _consideredFlow;
    private RuleFlow _placedFlow;

    /// <param name="world">The tables this drains, and the Ruleset it drains under. Not copied.</param>
    /// <param name="key">The world seed, as the draws' first coordinate.</param>
    public PlacementEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>
    /// Reads what placement did since the last call, and resets the counters.
    /// </summary>
    /// <remarks>
    /// <b>Two flows rather than one, because the interesting quantity is the gap between them.</b>
    /// *Considered* against *placed* is the housing shortage as a rate: a Pool that is being looked at
    /// and not housed is a city out of dwellings, where a Pool that is not being looked at is a
    /// mechanism that has stopped. A single *placed* counter reads identically in both cases, which is
    /// the shape slice 7 task 9 already found once — <c>evaluations − due</c> — and it is worth not
    /// finding a third time.
    /// </remarks>
    public PlacementActivity Drain()
    {
        var activity = new PlacementActivity(_consideredFlow, _placedFlow);

        _consideredFlow = default;
        _placedFlow = default;

        return activity;
    }

    /// <summary>
    /// Runs one pass if this Tick's interval divides, housing whom it can.
    /// </summary>
    /// <param name="tick">The Tick being run: the trigger test and the draws' key.</param>
    public void Place(Ticks tick)
    {
        PlacementRuleset placement = _world.Rules.Placement;

        if (!placement.Runs || tick.Raw % placement.Interval != 0)
        {
            CloseTick();
            return;
        }

        int pool = _world.UnplacedPool.Count;

        if (pool == 0)
        {
            CloseTick();
            return;
        }

        Span<int> into = Scratch(placement.SampleFor(pool));
        int drawn = DrawPool(into, tick, pool);

        for (int i = 0; i < drawn; i++)
        {
            _tickConsidered++;

            // The positions were all drawn against the Pool as it stood at the top of the pass, and
            // Place shrinks it -- swapping the last member into the vacated position. So a position
            // may now be past the end (nobody), or may name somebody other than whoever was drawn
            // (the swapped-in member). The first is skipped and the second is accepted: both are
            // still a draw over the Pool, and re-drawing to keep the sample exact would key the
            // second draw off how many earlier ones happened to succeed. Bounding first is not a
            // nicety -- At past the end hands back a default Handle and Resolve throws on it.
            int position = into[i];

            if (position >= _world.UnplacedPool.Count)
            {
                continue;
            }

            Handle<Household> seeker = _world.UnplacedPool.At(position);
            int slot = _world.Households.Rows.Resolve(seeker);

            if (!TryHouse(seeker, slot, tick))
            {
                continue;
            }

            _tickPlaced++;
        }

        CloseTick();
    }

    /// <summary>
    /// Looks at <c>candidates</c> Buildings and moves <paramref name="seeker"/> into the first with
    /// room.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>First rather than best, and that is the whole of the choice model there is.</b> With no
    /// rent, no commute and no tolerance every candidate with room scores identically, so a ranking
    /// would be a sort over equal keys — which is a hash-bearing tie-break nobody has argued. When
    /// <c>02 §5.4</c> arrives it replaces this line and the candidate list stays.
    /// </para>
    /// <para>
    /// <b>The draw is over <em>Lots</em> rather than over Buildings, and the difference is not
    /// cosmetic.</b> A Lot is a place in the city and the Lot table's slot count is the size of the
    /// city; the Building table's is a <em>recycling</em> table whose freed slots are an artefact of
    /// storage. Drawing over Buildings made <c>candidates</c> mean something the file could not
    /// state — under the shipped Ruleset roughly 55% of Building slots stand freed at any instant, so
    /// three looks bought about 1.3 real ones, and lowering the demolition rate would have silently
    /// raised the effective candidate count. Over Lots, a look that lands on a vacant one found
    /// nothing, which is a thing that happens to somebody looking for somewhere to live.
    /// </para>
    /// </remarks>
    private bool TryHouse(Handle<Household> seeker, int slot, Ticks tick)
    {
        int candidates = _world.Rules.Placement.Candidates;
        int lots = _world.Lots.Rows.SlotCount;

        if (lots == 0)
        {
            return false;
        }

        for (int look = 0; look < candidates; look++)
        {
            // Keyed on the Household's monotonic id rather than its Pool position, so that who a
            // family looks at does not change because somebody ahead of them was housed. The look
            // ordinal separates the candidates within one occasion; the shift count is constant,
            // which is what keeps BOR0204 quiet.
            ulong entity = Randomness.Mix(
                _world.Households.Rows.IdAt(slot) ^ ((ulong)(uint)look << 32));

            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PlacementCandidate);

            int lot = (int)(value % (ulong)(uint)lots);

            if (!_world.Lots.Rows.IsLive(lot))
            {
                continue;
            }

            // A vacant Lot is a look that found nothing, which is a real thing to happen to somebody
            // looking for somewhere to live and is why the draw is over Lots at all.
            int building = _world.Lots.BuildingOn(lot);

            if (building == Rows.NoSlot || !_world.HasRoom(building))
            {
                continue;
            }

            _world.Place(seeker, _world.Buildings.Rows.At(building));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Fills <paramref name="into"/> with Pool positions this pass considers.
    /// </summary>
    /// <remarks>
    /// <b>A draw rather than the front of the queue</b>, which is <c>02 §8</c> rule 5 for the reason
    /// <c>World.Place</c> already states: a Pool that does not fully drain is what a housing shortage
    /// <em>is</em>, and under any fixed order the same Households would stay unhoused for the life of
    /// the city with nothing in any readout to explain why. The Pool is dense, so every position is a
    /// live member and nothing is discarded here.
    /// </remarks>
    private int DrawPool(Span<int> into, Ticks tick, int pool)
    {
        for (int draw = 0; draw < into.Length; draw++)
        {
            ulong entity = Randomness.Mix((ulong)(uint)draw << 32);
            ulong value = Randomness.Draw(_key, entity, tick, PurposeTag.PoolDraw);

            into[draw] = (int)(value % (ulong)(uint)pool);
        }

        return into.Length;
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
        _placedFlow = _placedFlow.Fold(_tickPlaced);

        _tickConsidered = 0;
        _tickPlaced = 0;
    }
}

/// <summary>What the placement pass did over a Census interval.</summary>
/// <param name="Considered">Pool members looked at.</param>
/// <param name="Placed">Of those, the ones that found a dwelling.</param>
public readonly record struct PlacementActivity(RuleFlow Considered, RuleFlow Placed);
