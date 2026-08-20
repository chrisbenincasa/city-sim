namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>
/// What the Policies did since the last reading: how often they triggered, whom they reached, and
/// where they stopped.
/// </summary>
/// <remarks>
/// <para>
/// <b>Flows, not levels</b>, for <see cref="ZoneActivity"/>'s reason: a trigger has no value at an
/// instant, so each is accumulated between readings and a reading drains it.
/// </para>
/// <para>
/// <b>Six counters and two magnitudes, and the split is kept where it matters rather than here.</b>
/// Task 5 left the money magnitude out on a scope line — <em>counting here and measuring there</em> —
/// and task 7 discharged it by finding that the line it wanted was in the <em>reader</em>, not in the
/// producer. A count and an amount are not commensurable and must not share a block of a report; they
/// are produced by the same method on the same Tick, and draining them separately would be two drains
/// a caller can get one of right. So the counts and the two <see cref="MoneyFlow"/>s ride together
/// and the Census splits them, surfacing these two under its money family
/// (<c>MetricSource.MoneyFlow</c>) and never beside <c>PolicyCounter</c>.
/// </para>
/// <para>
/// <b>The two are denominated in the direction of the treasury rather than in a Ruleset's names for
/// them.</b> <c>rulesets/taxed.toml</c> calls them a levy and a rebate; the Core holds ids and
/// numbers and never the words a human reads, and a Policy with <c>to = "global"</c> moves money to
/// the treasury whatever it is called.
/// </para>
/// </remarks>
/// <param name="Triggers">Policy firings — one per Policy per Tick its interval divides.</param>
/// <param name="Considered">Live members swept. The denominator of everything below.</param>
/// <param name="Applied">Members the transfer actually moved money for.</param>
/// <param name="Floored">
/// ⚠ <b><c>adr/0115</c>'s instrument.</b> Percentage applications that came out zero from a
/// <em>non-zero</em> Readout — the poorest paying nothing while everybody else pays the stated rate,
/// which is a regressive outcome produced by rounding and chosen by nobody. A zero Readout is
/// excluded deliberately: a Household with nothing owes nothing at any unit, and counting it would
/// bury the artefact under the destitute.
/// </param>
/// <param name="Exhausted">
/// Sweeps that stopped early because the payer ran dry — <c>02 §4.2</c>'s <em>pays whom it reaches
/// and reports where it stopped</em>. One per sweep, not per member: what is being counted is the
/// event of running out, and <c>Considered</c> already says how far it got.
/// </param>
/// <param name="Unaffordable">
/// Members skipped because <em>they</em> could not cover the transfer, which is a different fact from
/// the payer running dry and does not stop the sweep. Nobody is entitled to be taxed.
/// </param>
/// <param name="ToTreasury">
/// Money moved <em>into</em> the treasury — every transfer whose <c>to</c> is <c>Scope.Global</c>,
/// summed. One half of the circular flow.
/// </param>
/// <param name="FromTreasury">
/// Money moved <em>out of</em> the treasury. The other half, and it is reported apart from
/// <paramref name="ToTreasury"/> rather than netted: a net is the one figure that cannot say whether
/// a city taxed nothing and paid nothing or taxed heavily and paid it all back, and those are
/// different cities.
/// </param>
public readonly record struct PolicyActivity(
    RuleFlow Triggers,
    RuleFlow Considered,
    RuleFlow Applied,
    RuleFlow Floored,
    RuleFlow Exhausted,
    RuleFlow Unaffordable,
    MoneyFlow ToTreasury = default,
    MoneyFlow FromTreasury = default);

/// <summary>
/// A money magnitude accumulated between two Census readings: how much moved, and the most that
/// moved on any one Tick.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="RuleFlow"/>'s shape in <c>Money</c>'s width, and the width is the whole reason it
/// is a second type.</b> <c>RuleFlow.Peak</c> is an <c>int</c> because it counts events, and a count
/// of events on one Tick has a ceiling the city's size supplies. An amount does not:
/// <c>Quantities/Money.cs</c> argues 64 bits from <em>"income flow at the target population is on
/// the order of 10⁹ per period against an <c>int</c> maximum of 2.1×10⁹"</em>, and a levy sweeping a
/// million Households on one Tick is exactly that period compressed into one reading. Folding an
/// amount through an <c>int</c> peak would overflow at the population this project is built for,
/// silently, in the instrument rather than in the city.
/// </para>
/// <para>
/// <b>Widening <see cref="RuleFlow"/> instead was the alternative and was refused.</b> Every counter
/// family in the Census folds through it, so the change would have reached the whole instrument to
/// serve two members — and the two types being distinct is itself the thing that stops a magnitude
/// being folded into a family of counts by accident.
/// </para>
/// </remarks>
/// <param name="Sum">Everything that moved over the interval.</param>
/// <param name="Peak">The most that moved on any single Tick of it.</param>
public readonly record struct MoneyFlow(long Sum, long Peak)
{
    /// <summary>Folds one Tick's amount in, returning the flow that includes it.</summary>
    /// <param name="tick">What moved on this Tick.</param>
    /// <returns>The extended flow.</returns>
    public MoneyFlow Fold(long tick) => new(Sum + tick, tick > Peak ? tick : Peak);
}

/// <summary>
/// The Sweep family's second member: a Policy triggers on an interval, sweeps a whole population,
/// and moves money between one Bin and another.
/// </summary>
/// <remarks>
/// <para>
/// <b>It sweeps where <see cref="ZoneRuleEngine"/> samples, and <c>02 §4.2</c> makes that a semantic
/// difference rather than a performance one.</b> A developer genuinely does not look at every parcel,
/// so a sample <em>is</em> the behaviour model; a transfer is an <b>entitlement</b>, and paying a
/// random subset of the eligible would be a defect rather than a model. Anything reaching for a
/// sample to make this affordable has confused the two.
/// </para>
/// <para>
/// <b>And a sweep is affordable</b>: <c>02 §4.2</c> prices eight Policies over ten thousand
/// Households at eighty thousand integer comparisons a Day, which is about ten a Tick amortised. What
/// costs is the <em>interval</em>, and that is Ruleset data because <em>a Policy paying daily is a
/// different city from one paying weekly</em>.
/// </para>
/// <para>
/// ⚠ <b>The scan start rotates and the rotation is not decoration.</b> A treasury that empties pays
/// whom the sweep reached; under a fixed order that is the same low-index Households for the life of
/// the city, and the tail would be permanently excluded with nothing in the readout saying why. The
/// start is drawn per trigger — see <c>PurposeTag.PolicyScanStart</c> for why drawn rather than
/// advanced by a stride.
/// </para>
/// <para>
/// <b>It runs first in Tick phase 6, ahead of placement, employment and the Zone Rules.</b> The
/// ordering is the same decision phase 5 → phase 6 already carries: a Household paid this Tick is
/// judged on what it holds now rather than on last Tick's balance, so the passes behind it respond to
/// the city the Policy just changed. Nothing downstream reads a balance yet, which makes this the
/// cheapest moment to fix the order — <c>02 §1.1</c> calls the phase ordering the determinism
/// contract, and a contract is cheapest to write before anybody relies on it.
/// </para>
/// <para>
/// <b>The engine holds no simulation state</b>, for <see cref="RuleEngine"/>'s reason, and no scratch
/// buffer at all: a sweep visits rows in slot order from a computed offset, so there is nothing to
/// draw into.
/// </para>
/// </remarks>
public sealed class PolicyEngine
{
    private readonly World _world;
    private readonly WorldKey _key;

    private int _tickTriggers;
    private int _tickConsidered;
    private int _tickApplied;
    private int _tickFloored;
    private int _tickExhausted;
    private int _tickUnaffordable;

    private RuleFlow _triggerFlow;
    private RuleFlow _consideredFlow;
    private RuleFlow _appliedFlow;
    private RuleFlow _flooredFlow;
    private RuleFlow _exhaustedFlow;
    private RuleFlow _unaffordableFlow;

    private long _tickToTreasury;
    private long _tickFromTreasury;

    private MoneyFlow _toTreasuryFlow;
    private MoneyFlow _fromTreasuryFlow;

    /// <param name="world">The tables this sweeps, and the Ruleset it sweeps under. Not copied.</param>
    /// <param name="key">The world seed, as the scan start's first coordinate.</param>
    public PolicyEngine(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
    }

    /// <summary>Reads what the Policies did since the last call, and resets the counters.</summary>
    public PolicyActivity Drain()
    {
        var activity = new PolicyActivity(
            _triggerFlow, _consideredFlow, _appliedFlow, _flooredFlow, _exhaustedFlow,
            _unaffordableFlow, _toTreasuryFlow, _fromTreasuryFlow);

        _triggerFlow = default;
        _consideredFlow = default;
        _appliedFlow = default;
        _flooredFlow = default;
        _exhaustedFlow = default;
        _unaffordableFlow = default;
        _toTreasuryFlow = default;
        _fromTreasuryFlow = default;

        return activity;
    }

    /// <summary>Tick phase 6: fires every Policy this Tick's interval divides, and sweeps for each.</summary>
    /// <remarks>
    /// <b>Declaration order, and it is load-bearing for the same two reasons a Zone Rule's is.</b> It
    /// decides which of two Policies moves money first — so a tax declared above a transfer funds it
    /// on the same trigger, and the same pair in the other order does not — and the index is a
    /// coordinate of the scan start, so reordering <c>[[policy]]</c> tables changes who is reached
    /// first when the treasury runs dry. Both move the State Hash.
    /// </remarks>
    /// <param name="tick">The Tick being run, which is both the trigger test and the start's key.</param>
    public void Sweep(Ticks tick)
    {
        PolicyDefinition[] policies = _world.Rules.Policies;

        for (int policy = 0; policy < policies.Length; policy++)
        {
            PolicyDefinition definition = policies[policy];

            // adr/0048's two-sided check, in the direction the loader cannot cover: a Ruleset built
            // in code has never been through RulesetLoader, and an interval of zero would divide by
            // it rather than say so.
            if (definition.Interval == 0)
            {
                throw new InvalidOperationException(
                    $"Policy {policy} has an interval of 0. An interval is a reschedule in Ticks and "
                    + "the loader refuses anything below 1; this Ruleset was not built by it.");
            }

            if (tick.Raw % definition.Interval != 0)
            {
                continue;
            }

            _tickTriggers++;

            SweepHouseholds(policy, definition, tick);
        }

        CloseTick();
    }

    /// <summary>One Policy's sweep over every live Household, from a drawn start.</summary>
    /// <remarks>
    /// <para>
    /// <b>Over slots rather than over live rows</b>, skipping the dead, which is
    /// <c>ZoneSample.Draw</c>'s own shape: a start drawn against a live count would need the count
    /// first, and the count is a walk. The rotation is uniform over slots and therefore very slightly
    /// non-uniform over Households — a bias bounded by the free list, which is what a Sweep Rule can
    /// afford and a sampler could not.
    /// </para>
    /// <para>
    /// <b>Exhaustion stops the sweep; a member who cannot pay does not.</b> The two look alike at the
    /// call site and are opposite facts. A treasury that cannot cover the next payment cannot cover
    /// any of the ones behind it either, so continuing would be a walk that pays nobody — and
    /// <c>02 §4.2</c> asks for <em>where it stopped</em>, which only exists if it stops. A Household
    /// that cannot cover a levy says nothing about the next Household, and nobody is entitled to be
    /// taxed.
    /// </para>
    /// </remarks>
    private void SweepHouseholds(int policy, in PolicyDefinition definition, Ticks tick)
    {
        if (definition.Subject != PolicySubject.Household)
        {
            throw new NotSupportedException(
                $"Policy {policy} sweeps {definition.Subject}, and only Household is built. 02 "
                + "section 4.2 names three populations; a Business has a balance and no pass that "
                + "moves it, and a Building population needs the predicate that selects it. The "
                + "loader refuses both by name, so this Ruleset was built in code.");
        }

        int slots = _world.Households.Rows.SlotCount;

        if (slots == 0)
        {
            return;
        }

        int treasury = _world.FindTreasuryBin(definition.Resource);

        if (treasury == Rows.NoSlot)
        {
            throw new InvalidOperationException(
                $"Policy {policy} moves Resource {definition.Resource.Raw} and the treasury holds no "
                + "Bin for it. World.FitTreasury opens one per CONSERVED Resource, and the loader "
                + "refuses a transfer of anything else, so this Ruleset was built in code.");
        }

        ulong draw = Randomness.Draw(_key, (ulong)(uint)policy, tick, PurposeTag.PolicyScanStart);
        int start = (int)(draw % (ulong)(uint)slots);

        for (int step = 0; step < slots; step++)
        {
            int slot = start + step;

            if (slot >= slots)
            {
                slot -= slots;
            }

            if (!_world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            _tickConsidered++;

            if (!Move(definition, slot, treasury, tick, out bool payerDry))
            {
                if (payerDry)
                {
                    _tickExhausted++;
                    return;
                }
            }
        }
    }

    /// <summary>Moves one member's share, or says why it did not.</summary>
    /// <param name="payerDry">
    /// Whether the failure was the <em>source</em> Bin being short rather than the member owing
    /// nothing. Only meaningful when this returns <see langword="false"/>.
    /// </param>
    /// <returns>Whether money moved.</returns>
    private bool Move(
        in PolicyDefinition definition, int household, int treasury, Ticks tick, out bool payerDry)
    {
        payerDry = false;

        long applications = Applications(definition, household);

        if (applications == 0)
        {
            return false;
        }

        long amount = applications * definition.Amount;

        int balance = _world.Bins.Rows.Resolve(_world.Households.Balance[household]);
        int source = definition.From == Scope.Global ? treasury : balance;
        int destination = definition.To == Scope.Global ? treasury : balance;

        if (_world.Bins.LevelAt(source) < amount)
        {
            payerDry = definition.From == Scope.Global;

            if (!payerDry)
            {
                _tickUnaffordable++;
            }

            return false;
        }

        // Through World's doors rather than BinTable.Move, so both writes drain their wait lists.
        // Nothing waits on a balance today -- a Policy never subscribes and no Bin Rule can name a
        // Household's Bin -- and going round them would make that a permanent property of the code
        // rather than a temporary property of the city.
        _world.Withdraw(_world.Bins.Rows.At(source), amount, tick);
        _world.Deposit(_world.Bins.Rows.At(destination), amount, tick);

        _tickApplied++;

        // Read off the Scopes rather than off the Bins the transfer resolved to, because a Bin is
        // the treasury's by ownership and a transfer is the treasury's by direction, and task 5's
        // close-out is the record of what happens when a levy is read off the Bin it spends: the
        // two agree today and a Policy that moved money between two local Bins would make them
        // disagree without either being wrong.
        if (definition.To == Scope.Global)
        {
            _tickToTreasury += amount;
        }

        if (definition.From == Scope.Global)
        {
            _tickFromTreasury += amount;
        }

        return true;
    }

    /// <summary>How many times this Policy applies to one member.</summary>
    /// <remarks>
    /// <b><c>02 §4.1</c>'s apply count, read against a Household instead of a Building</b>, and
    /// <c>RuleEngine.Applications</c>' arithmetic exactly: <c>FloorDiv(readout × percent, 100)</c>,
    /// flooring because a fraction of an application is not an application. The band case is
    /// <em>fixed</em> rather than greedy — a Policy owes a quantum, and there is no stock for it to
    /// work through — so <c>min</c> is what applies and <c>max</c> bounds an author's typo.
    /// <para>
    /// ⚠ <b>The floor is counted here and nowhere else</b>, which is <c>adr/0115</c>'s instrument:
    /// <em>a discipline the loader cannot check needs a counter or it is a comment.</em> A readout's
    /// magnitude is not known at load time, so the regressive rounding that ADR is about can only be
    /// observed at the moment it happens.
    /// </para>
    /// </remarks>
    private long Applications(in PolicyDefinition definition, int household)
    {
        if (!definition.Apply.IsDerived)
        {
            return definition.Apply.Min;
        }

        long readout = Readouts.ReadHousehold(_world, household, definition.Apply.Derived);
        long applications = IntegerMath.FloorDiv(readout * definition.Apply.Percent, 100);

        if (applications == 0 && readout > 0 && definition.Apply.Percent > 0)
        {
            _tickFloored++;
        }

        return applications;
    }

    /// <summary>Folds this Tick's counts into the flows the Census drains.</summary>
    private void CloseTick()
    {
        _triggerFlow = _triggerFlow.Fold(_tickTriggers);
        _consideredFlow = _consideredFlow.Fold(_tickConsidered);
        _appliedFlow = _appliedFlow.Fold(_tickApplied);
        _flooredFlow = _flooredFlow.Fold(_tickFloored);
        _exhaustedFlow = _exhaustedFlow.Fold(_tickExhausted);
        _unaffordableFlow = _unaffordableFlow.Fold(_tickUnaffordable);
        _toTreasuryFlow = _toTreasuryFlow.Fold(_tickToTreasury);
        _fromTreasuryFlow = _fromTreasuryFlow.Fold(_tickFromTreasury);

        _tickTriggers = 0;
        _tickConsidered = 0;
        _tickApplied = 0;
        _tickFloored = 0;
        _tickExhausted = 0;
        _tickUnaffordable = 0;
        _tickToTreasury = 0;
        _tickFromTreasury = 0;
    }
}
