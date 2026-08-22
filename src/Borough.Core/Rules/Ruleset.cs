namespace Borough.Core.Rules;

using Borough.Core.Determinism;
using Borough.Core.Arithmetic;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;

/// <summary>
/// Where a Bin Rule's term is drawn from or written to. <c>02 §4.1</c>'s four, and there are four.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is no proximity scope.</b> Nearest-first selection among nearby options — a Parking Shed,
/// an Amenity set, a Provider List — always belongs to something that moves. <em>Movers choose;
/// Rules transform.</em> The one non-local scope is the District Pool, and a District is bounded by
/// where transport can be <em>ignored</em> where a shed is bounded by where it must be
/// <em>measured</em>, so the two radii cannot coincide.
/// </para>
/// <para>
/// <b>A scope answers <em>whose is it</em>, not <em>where do I look</em></b> (<c>adr/0050</c>). A
/// <see cref="Local"/> term is free because the Bin already belongs to the Building; a term crossing
/// an <b>ownership boundary</b> is a <b>trade</b>, and the Good moves one way while money moves the
/// other at the prevailing price. <b>No payment is ever authored in a Rule</b> — the price is
/// emergent, the quantity is the term's amount, and the counterparty follows from the scope — which
/// is what lets a term's amount stay a fixed integer permanently, since a derived apply count
/// provably cannot express a variable <em>rate</em>: the count cancels out of a ratio.
/// </para>
/// </remarks>
public enum Scope : byte
{
    /// <summary>Bins on the Building running the Rule. <b>Free — the Bin is already its own.</b></summary>
    Local = 0,

    /// <summary>
    /// Bins on the Building's District Pool. Requires road connectivity.
    /// </summary>
    /// <remarks>
    /// <b>A market, not a wider Bin lookup</b> (<c>adr/0050</c>). This term crosses an ownership
    /// boundary, so it is a purchase settled atomically with the Rule: the Rule fails on the Pool Bin
    /// when the District is out of stock and on the <em>money</em> Bin when the buyer cannot afford
    /// it, which is <c>adr/0024</c>'s bankruptcy-versus-starvation diagnosis falling out of the wait
    /// list rather than needing a mechanism. Implementing it as a lookup ships an unconserved economy.
    /// </remarks>
    Pool = 1,

    /// <summary>City-wide Bins — the treasury, aggregate statistics.</summary>
    Global = 2,

    /// <summary>
    /// Map Layer cells under the footprint. <b>Write-only</b>, and therefore never a Bin.
    /// </summary>
    /// <remarks>
    /// It appears here because a Ruleset author writes <c>scope = "map"</c>, and nowhere else: a map
    /// term is a <see cref="MapEmission"/> rather than a <see cref="Term"/>, because a Layer cell has
    /// no capacity to exceed, so a map output can never fail and no Rule ever waits on one.
    /// </remarks>
    Map = 3,
}

/// <summary>
/// Which Readout a derived apply count reads. A dense small integer.
/// </summary>
/// <remarks>
/// <b>The readable set is declared in the simulation</b> (<c>02 §4.1</c>), so this enumeration is the
/// core's and the names are <c>Borough.Formats</c>'s. That inversion is what discharges
/// <c>LEGIBLE CAUSE</c> by construction rather than by reference: no Rule can act on a quantity the
/// player has no way to inspect, because the declaration <em>is</em> what an inspector reads.
/// <b>Slice 7 task 7 populates it; until then every <c>derived</c> name refuses, which is correct
/// rather than provisional — no Readouts are declared, so none can be named.</b>
/// </remarks>
public readonly record struct ReadoutId(ushort Raw)
{
    /// <summary>No Readout. The apply count is a band instead.</summary>
    public static ReadoutId None => new(0);

    /// <summary>True when this names no Readout.</summary>
    public bool IsNone => Raw == 0;
}

/// <summary>
/// Which reportable condition a chain's terminal records. A dense small integer.
/// </summary>
/// <remarks>
/// <b>A Rule that carries one is a reporting terminal, and that is the whole discriminator.</b>
/// <c>02 §4.1</c>: a terminal <em>records a condition and leaves the chain failed</em>. Were it an
/// ordinary Rule it would succeed — recording has no input that can be short — and re-arm the head on
/// <c>rate</c>, walking the whole chain every <c>rate</c> Ticks for as long as the shortage lasts.
/// That is the polling cost subscription exists to remove, reintroduced through the last link, and
/// the corpus's own worked example contained it.
/// </remarks>
public readonly record struct ConditionId(ushort Raw)
{
    /// <summary>Not a reporting terminal.</summary>
    public static ConditionId None => new(0);

    /// <summary>True when this Rule records no condition.</summary>
    public bool IsNone => Raw == 0;
}

/// <summary>Which Bin a term addresses: a scope and a Resource, and nothing else.</summary>
/// <remarks>
/// <b>The unit the <c>fills</c> refusal is computed over.</b> <c>adr/0045</c>: a fallback chain is a
/// source ladder over <em>one Bin</em>, so what a link must relieve is a scope-and-Resource pair, not
/// a quantity. Separating it from <see cref="Term"/> is what lets that check be set arithmetic.
/// </remarks>
public readonly record struct BinRef(Scope Scope, ResourceId Resource);

/// <summary>One input or output of a Bin Rule: which Bin, and how much per application.</summary>
public readonly record struct Term(BinRef Bin, int Amount);

/// <summary>
/// A write to a Map Layer cell under the Building's footprint.
/// </summary>
/// <remarks>
/// <b>Its own type rather than a <see cref="Term"/> with a funny scope</b>, because <c>02 §4.1</c>
/// makes it a different kind of thing: a Layer cell has no capacity, so this cannot fail, cannot be
/// subscribed to, and cannot appear as an input. Spelling it as a Bin term with <c>Scope.Map</c>
/// would have made all three of those runtime checks instead of unrepresentable states.
/// </remarks>
public readonly record struct MapEmission(Layer Layer, int Amount)
{
    /// <summary>
    /// Whether a Rule may emit into <paramref name="layer"/> at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Only pollution accumulates from a source.</b> Land value is chased towards a target and
    /// Sealing is a property of a footprint, so neither is a quantity a Rule adds per application —
    /// which makes <c>layer = "land-value"</c> in a Rule's outputs a sentence with no meaning rather
    /// than a value out of range.
    /// </para>
    /// <para>
    /// <b>It lives here so that the loader and the engine cannot disagree.</b> The refusal belongs at
    /// the parse site under <c>adr/0048</c>, and the engine keeps its own throw as a backstop; a
    /// predicate stated twice is two predicates, and the one that drifts is the one nobody runs.
    /// </para>
    /// </remarks>
    public static bool IsEmittable(Layer layer) => layer == Layer.IndustrialPollution;
}

/// <summary>
/// How many times one Rule evaluation applies: a band, or a count derived from a Readout.
/// </summary>
/// <remarks>
/// <para>
/// <b>Either <c>{min, max}</c> or <c>derived</c>, never both</b> (<c>02 §4.1</c>), and the two
/// factories are what make that structural. Admitting both on one Rule would collide their failure
/// semantics: below <c>min</c> is a failure and a derived zero is a <em>success</em>.
/// </para>
/// <para>
/// <b><c>min = max</c> is the fixed case, not a second mechanism.</b> One form spells both, and which
/// a Rule uses is a modelling decision fixed at design time — <em>greedy when the actor works through
/// its stock, fixed when the actor owes a quantum.</em> A bakery bakes the flour it has; Upkeep draws
/// a defined quantum and must never draw more because the treasury happens to be full.
/// </para>
/// </remarks>
public readonly record struct ApplyCount
{
    private ApplyCount(int min, int max, ReadoutId derived, int percent)
    {
        Min = min;
        Max = max;
        Derived = derived;
        Percent = percent;
    }

    /// <summary>The floor. Below it the Rule fails and subscribes. Meaningless when derived.</summary>
    public int Min { get; }

    /// <summary>The ceiling. Meaningless when derived.</summary>
    public int Max { get; }

    /// <summary>The Readout <c>n</c> is computed from, or <see cref="ReadoutId.None"/>.</summary>
    public ReadoutId Derived { get; }

    /// <summary>
    /// The percentage of the Readout applied, <c>100</c> being one application per unit. Meaningless
    /// when banded.
    /// </summary>
    /// <remarks>
    /// <b>A percentage rather than a general ratio, because that is the shape <c>02 §4.1</c> names</b>
    /// — <em>"15% of gross income is one unit of money applied <c>gross_income × 15 / 100</c>
    /// times"</em>, and <c>CONTEXT</c> → Policy prefers percentages to flat amounts for the same
    /// reason: <em>a percentage is an apply count; a flat amount is an amount.</em> A designer needing
    /// a finer scale than one part in a hundred is the trigger for widening this, and it should widen
    /// to an explicit numerator and denominator rather than to an expression.
    /// </remarks>
    public int Percent { get; }

    /// <summary>Whether <c>n</c> is consulted rather than bounded.</summary>
    public bool IsDerived => !Derived.IsNone;

    /// <summary>A greedy or fixed count: apply as often as the inputs allow, within the band.</summary>
    public static ApplyCount Band(int min, int max) => new(min, max, ReadoutId.None, 0);

    /// <summary>A count computed from a Readout. Integer arithmetic, no expression language.</summary>
    /// <param name="readout">The declared Readout to consult.</param>
    /// <param name="percent">Percentage of it to apply; <c>100</c> is once per unit.</param>
    public static ApplyCount From(ReadoutId readout, int percent = 100) =>
        new(0, 0, readout, percent);
}

/// <summary>
/// Which family a Resource belongs to. <c>CONTEXT</c> → Resource's one boolean, grown to three.
/// </summary>
/// <remarks>
/// <para>
/// <b>The family is the axis every exception to <em>"it's a Good"</em> turned out to lie on</b>, and
/// it decides three unlike things: how the Resource moves between Districts, whether its Bin has a
/// ceiling, and — once Utilities exist — whether its Bin carries over between periods.
/// </para>
/// <para>
/// <b>The player never sees this word.</b> <c>CONTEXT</c> → Resource is explicit that Resource is a
/// mechanism-level term and the player sees the families by their own names, because a food chain and
/// a power grid are different things to think about even when they are the same thing to compute.
/// </para>
/// </remarks>
public enum ResourceFamily : byte
{
    /// <summary>Not declared. A Ruleset carrying this failed to load.</summary>
    None = 0,

    /// <summary>Moves between Districts as a Shipment — a Vehicle, on the Road Graph, in the traffic.</summary>
    Good = 1,

    /// <summary>Flows along the District adjacency graph. No Vehicle, no congestion.</summary>
    Utility = 2,

    /// <summary>
    /// A conserved stock that flows without transport, and the only Resource with no inter-District
    /// movement at all.
    /// </summary>
    /// <remarks>
    /// <b>Conserved means never created or destroyed inside the city</b> (<c>adr/0024</c>), the
    /// Outside Connection being its only source and sink. That is a property no other family has —
    /// flour legitimately becomes bread — and it is why the loader refuses a Rule whose money terms do
    /// not balance. Its Bin is <b>unbounded</b>: no physical ceiling exists, and a finite one would
    /// mean a Business too full of money to be paid.
    /// </remarks>
    Money = 3,
}

/// <summary>
/// The ceiling on a Bin: a finite number of units, or none at all.
/// </summary>
/// <remarks>
/// <b>An explicit unbounded rather than a large sentinel</b>, which <c>CONTEXT</c> → Resource asks for
/// by name. The distinction is not fussiness: a designer reading <c>capacity = 2147483647</c> cannot
/// tell a ceiling from its absence, and a designer *writing* it has authored a number that is nearly
/// a leak. Held as <see cref="int.MaxValue"/> underneath so that <c>capacity − level</c> — the form
/// <c>CONTEXT</c> prescribes precisely so it cannot overflow — stays one subtraction with no branch.
/// </remarks>
public readonly record struct BinCapacity
{
    private BinCapacity(long units, bool unbounded)
    {
        Units = units;
        IsUnbounded = unbounded;
    }

    /// <summary>
    /// The ceiling in units. <b>Meaningful even when unbounded, which is <c>adr/0065</c>.</b>
    /// </summary>
    /// <remarks>
    /// The old wording here was <em>meaningless, and never authored, when unbounded</em>, and it was
    /// half true: never authored is still right, and meaningless was the sentence that let a sentinel
    /// escape. <c>adr/0031</c> asked for an explicit unbounded marker precisely so that <em>a very
    /// large number pretending to be a bound</em> would not reach anything that divides by it, and it
    /// escaped anyway — <see cref="Borough.Core.Rules.BinTable.Create"/> writes this and drops
    /// <see cref="IsUnbounded"/>, so every consumer downstream has only ever seen the sentinel.
    /// <b>Under <c>adr/0065</c> the sentinel is honest instead of hidden</b>: there is no unbounded,
    /// only a ceiling far enough away that approaching it is a defect rather than a design limit, and
    /// <see cref="long.MaxValue"/> is ~10⁸× beyond any plausible city total.
    /// </remarks>
    public long Units { get; }

    /// <summary>Whether this Bin has no ceiling a Ruleset authored.</summary>
    public bool IsUnbounded { get; }

    /// <summary>A Bin with no ceiling, which is every Money Bin and nothing else.</summary>
    public static BinCapacity Unbounded => new(long.MaxValue, unbounded: true);

    /// <summary>A Bin holding at most <paramref name="units"/>.</summary>
    public static BinCapacity Of(long units)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(units);
        return new BinCapacity(units, unbounded: false);
    }
}

/// <summary>One Bin a Building kind is given when it is built: which Resource, and its capacity.</summary>
public readonly record struct BinDeclaration(ResourceId Resource, BinCapacity Capacity);

/// <summary>
/// One Bin Rule, as ids and integers. The slices index into <see cref="Ruleset"/>'s flat arrays.
/// </summary>
public readonly record struct RuleDefinition(
    byte Kind,
    uint Rate,
    ApplyCount Apply,
    RuleId OnFail,
    bool HasFills,
    BinRef Fills,
    ConditionId Reports,
    int InputFirst,
    int InputCount,
    int OutputFirst,
    int OutputCount,
    int EmissionFirst,
    int EmissionCount)
{
    /// <summary>Whether this Rule records a condition and leaves the chain failed.</summary>
    public bool IsTerminal => !Reports.IsNone;
}

/// <summary>One Building kind: the Bins it is given, the Rules it runs, and when it falls down.</summary>
public readonly record struct KindDefinition(
    int BinFirst, int BinCount, int RuleFirst, int RuleCount)
{
    /// <summary>
    /// How many firings a Rule of this kind may miss, starved, before the Building is condemned.
    /// Zero means it never is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>In missed firings rather than in Ticks</b> (<c>adr/0053</c>). A Rule fires every
    /// <c>rate</c> Ticks when healthy, so silence of <c>N × rate</c> is <c>N</c> missed firings —
    /// dimensionless, and immune to a Ruleset that retunes every rate and would otherwise have
    /// silently retuned every Building's lifespan.
    /// </para>
    /// <para>
    /// <b>On the kind rather than on the Zone Rule</b> (<c>adr/0055</c>, and task 3 of
    /// <c>plans/0014</c>). Any Zone Rule may sample any Lot, so a threshold declared per Zone Rule
    /// would make a Building's mortality depend on which Rule happened to look at it — where pressure
    /// is a property of the Building and of nothing else.
    /// </para>
    /// <para>
    /// <b>An init property rather than a positional parameter, and zero meaning immortal.</b>
    /// Decline is content a Ruleset opts into: a kind that falls down is a stronger claim than one
    /// that does not, and the default is the behaviour of every Ruleset written before this existed.
    /// The failure it could hide — a Zone Rule that condemns nothing because nobody wrote a threshold
    /// — is a Ruleset that loads clean and does nothing, which is the refusal class task 3
    /// established; it is left unrefused because a growth-only Ruleset is coherent and would be
    /// refused with it.
    /// </para>
    /// </remarks>
    public int CondemnAfter { get; init; }

    /// <summary>
    /// How many Occupants a Building of this kind may hold. Zero means it houses nobody.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A property of the Ruleset in force rather than of the Building</b> (<c>adr/0068</c>), which
    /// is <c>adr/0064</c> applied a second time: it is an authored number keyed on the kind, read at
    /// a write site, pointed at by no live state — the same three properties that said a Bin's
    /// ceiling should not be a saved column either. **There is no column at all here**, because the
    /// Building already carries its kind and the only read is a guard that runs once per placement;
    /// a Bin needed one because <c>SpaceAt</c> is on the hot path and would have paid an owner
    /// resolve and a walk on every check.
    /// </para>
    /// <para>
    /// <b>Zero rather than absent, and most kinds mean it.</b> A factory declaring nothing houses
    /// nobody, which is the common case and wants no ceremony. The failure it could hide is a
    /// dwelling kind whose author forgot the line, and that surfaces immediately and loudly: nobody
    /// can move in and the Unplaced Pool stops draining, which is the opposite of the silent
    /// narrowing <c>02 §4.3</c>'s <c>apply</c> defect was.
    /// </para>
    /// <para>
    /// <b>A kind the Ruleset does not declare at all is a different thing from one declaring zero</b>
    /// — see <see cref="Entities.World.TryDeclaredOccupancy"/>. Dereliction must not evict, and the
    /// two cases would be indistinguishable if this number were the only answer.
    /// </para>
    /// </remarks>
    public int Occupants { get; init; }

    /// <summary>
    /// How many Citizens a Building of this kind employs. Zero means it employs nobody.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Occupants"/>'s rule a second time, and it transplants for the reason that one
    /// did rather than by analogy</b> (<c>adr/0068</c>, milestone 5b-bis task 2). It is an authored
    /// number keyed on the kind, read at a write site, and pointed at by no live state — so it is a
    /// property of the Ruleset in force, and lowering it reaches every Building already standing.
    /// The overflow is <b>dismissed</b> rather than left to drain: <c>adr/0064</c>'s <em>leave it to
    /// drain</em> turns on a Bin having a consumer, and a job has a holder and no consumer exactly as
    /// occupancy does — nothing would ever spend a Building's surplus employment down.
    /// </para>
    /// <para>
    /// <b>Zero rather than absent, and most kinds mean it.</b> A dwelling employs nobody, which is
    /// every kind in every shipped Ruleset at the time this was written, and it wants no ceremony. A
    /// kind the Ruleset does not declare at all is a different thing again — see
    /// <see cref="Entities.World.TryDeclaredJobs"/> — because dereliction must not sack a District.
    /// </para>
    /// <para>
    /// <b>It counts Citizens and never Households</b>, which is the one place this is not a copy of
    /// <see cref="Occupants"/>. <c>CONTEXT.md</c> → Household holds the Provider List and the money;
    /// employment is on the <see cref="Entities.Citizen"/>, with <c>Experience</c> and
    /// <c>SkillTier</c> beside it, and a Household with two adults working different sides of the
    /// city is the case a per-Household count could not express.
    /// </para>
    /// </remarks>
    public int Jobs { get; init; }

    /// <summary>
    /// How many Vehicles a Building of this kind can park. Zero means it provides no parking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Occupants"/>'s rule a third time</b> (<c>adr/0068</c>, <c>adr/0120</c>,
    /// milestone 7 task 1), and it transplants for the reason the others did rather than by analogy:
    /// an authored number keyed on the kind, read at a write site, pointed at by no live state — so
    /// it is a property of the Ruleset in force, and lowering it reaches every Building already
    /// standing. The overflow is <b>dismissed</b> rather than left to drain, on <see cref="Jobs"/>'
    /// side of <c>adr/0064</c>'s line: a Bin is left to drain because it has a consumer, and a parked
    /// car has a <em>holder</em> and no consumer exactly as a job does, so nothing would ever spend a
    /// Building's surplus parking down.
    /// </para>
    /// <para>
    /// <b>Zero rather than absent, and it is the one of these three where zero is the interesting
    /// value.</b> A tower with no parking is a real building and a real balance decision — it is the
    /// second row of <c>adr/0009</c>'s own player-tool table, <em>a detached house carries a
    /// driveway, a tower may not</em> — where a dwelling employing nobody is merely the common case.
    /// So this key needs no <em>unset</em> spelling: every value in range means something, and
    /// absence means the same as zero because a kind that says nothing about parking provides none.
    /// </para>
    /// <para>
    /// <b>It counts Vehicles, never Citizens and never Households.</b> That is not pedantry about
    /// units: <see cref="Entities.World.ModeOf"/> drives every member of a car-owning Household, so
    /// the three quantities differ by construction and a Car Park sized in people would be sized in
    /// the wrong currency (<c>adr/0119</c>).
    /// </para>
    /// </remarks>
    public int Parking { get; init; }

    /// <summary>
    /// How many Households a Building of this kind admits from the Outside per Day. Zero means this
    /// kind is not an Outside Connection at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Its presence is what declares the kind a gate</b> (<c>adr/0088</c>, milestone 11 task 1).
    /// There is no <c>outside_connection = true</c> beside it, and that is a decision rather than a
    /// saving: a boolean and a ceiling would be two spellings of one fact, and the pair could
    /// disagree — a gate declaring no throughput, or a throughput on a kind that is not a gate. The
    /// loader refuses a stated zero, so <b>every kind carrying this number is a gate and every gate
    /// carries a usable one</b>, exactly as <c>adr/0101</c>'s Shift band is made meaningful by being
    /// refused in both directions rather than by a third field saying whether it was stated.
    /// </para>
    /// <para>
    /// ⚠ <b>The name carries the unit because an unnamed unit is what cost this milestone a
    /// decision.</b> <c>adr/0088</c> makes an Outside Connection's throughput
    /// <c>min(declared ceiling, the Access Point's Segment capacity)</c> and calls which of the two
    /// binds *"the whole readout"* — and that second operand,
    /// <see cref="Space.RoadSegmentTable.CapacityPerDay"/>, is **whole Vehicles per Day**. That
    /// record was written about <b>Goods</b>. Milestone 11 moves <b>people</b>, and under
    /// <c>adr/0098</c> whether an arriving Household is a Vehicle at all is a property of the Ruleset
    /// in force — <c>minimal.toml</c> declares no <c>[households]</c>, so nobody drives and the
    /// Segment term bounds nothing. ***The two numbers were never in the same unit and nothing said
    /// so, because the formula was written down and the denominators were not.***
    /// <c>plans/0035</c> decision 9 moved the <c>min()</c> to milestone <b>12</b> with freight, and
    /// this key was named <c>arrivals_per_day</c> so the next reader cannot repeat it —
    /// <c>CLAUDE.md</c>'s <i>name a number after what it measures</i>.
    /// </para>
    /// <para>
    /// <b>Per Day rather than per Tick.</b> A Day is <c>CONTEXT.md</c>'s only time unit above the
    /// Tick, it is what <see cref="Space.RoadSegmentTable.CapacityPerDay"/> already uses for the
    /// quantity this one will one day be <c>min()</c>ed against, and it is the unit a designer means
    /// — *this port takes a hundred families a day* is a sentence somebody has a reason for, where a
    /// per-Tick rate is a sentence nobody does.
    /// </para>
    /// <para>
    /// <b>It bounds arrivals and nothing else</b>, which is what makes it ratifiable at 11 at all
    /// (<c>plans/0035</c> decision 6). Departures leave through a gate too and are not metered by it:
    /// the ceiling is <c>CONTEXT.md</c> → Outside Connection's *"infrastructure the player built"*,
    /// and a city does not build capacity to make people leave.
    /// </para>
    /// </remarks>
    public int ArrivalsPerDay { get; init; }

    /// <summary>
    /// The earliest in-world hour a job of this kind starts at. See <see cref="ShiftStartLatestHour"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A band rather than an hour, and each Building draws inside it</b> (<c>adr/0101</c>). A kind
    /// authoring a single start hour would give every Building of that kind the same one, and the
    /// corpus has exactly one Building kind — so the whole city would leave at one moment and the
    /// result would be a <em>sharper</em> peak than the uniform window this replaces, not a shape. The
    /// band is the thing a designer has a reason for (<em>bakeries open early, offices at nine</em>)
    /// and the Building's own hour is drawn from it against the Building's monotonic id, which is
    /// <c>adr/0059</c> once more: state the reason, derive the instance.
    /// </para>
    /// <para>
    /// <b>Hours rather than Ticks, because that is what a designer means.</b> The conversion is
    /// <see cref="Quantities.Ticks.AtHour"/> and it rounds — an hour is 85.33 Ticks and does not
    /// divide the Day.
    /// </para>
    /// </remarks>
    public int ShiftStartEarliestHour { get; init; }

    /// <summary>
    /// The latest in-world hour a job of this kind starts at, inclusive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There is no <em>undeclared</em> value here and there does not need to be, because the loader
    /// closes the case at the door.</b> A kind with <see cref="Jobs"/> above zero is refused unless it
    /// states both bounds, and a kind stating either bound is refused unless it employs somebody — so
    /// the pair is meaningful at every site that reads it and the defaulted <c>0, 0</c> is
    /// <em>unreachable</em> rather than ambiguous.
    /// </para>
    /// <para>
    /// That matters because <b>midnight is a legitimate answer</b>, so a zero meaning <em>unset</em>
    /// would be a placeholder sitting inside the range of legitimate answers — the trap session F
    /// named and <c>adr/0098</c> dodged by omitting a whole table. A key cannot be omitted alone the
    /// way a table can, so <b>a refusal is the cheaper form of the same discipline</b>.
    /// </para>
    /// <para>
    /// <b>Equal bounds are permitted and mean a kind whose shifts all start together.</b> That is a
    /// coherent workplace rather than a degenerate one, so it is not refused; what is refused is a
    /// band running backwards.
    /// </para>
    /// </remarks>
    public int ShiftStartLatestHour { get; init; }
}

/// <summary>
/// One Zone Rule: a time trigger, a sample of Lots, and the kind it builds on those that qualify.
/// </summary>
/// <remarks>
/// <para>
/// <b>The second of <c>02 §4</c>'s two execution models, and the first Sweep Rule.</b> A Bin Rule is
/// attached to a Building and proposes in Tick phase 2 to be settled in phase 3; a Zone Rule is
/// attached to the city, fires on <see cref="Interval"/>, and acts where it runs, in phase 6. That
/// difference in *when an effect becomes visible within a Tick* is <c>adr/0033</c>'s observable
/// difference and the reason a mechanism may not change family for performance.
/// </para>
/// <para>
/// <b><see cref="Zone"/> is a bit index and it scopes what this Rule may build — never which Lots it
/// looks at</b> (<c>adr/0055</c>). The sample is drawn from every Lot; the bit is a term in the create
/// predicate. Filtering the population by it instead would let a player repaint a Lot and make the
/// Building on it unreachable, and therefore immortal.
/// </para>
/// <para>
/// <b><see cref="Interval"/> is Ruleset data rather than a scheduling knob</b>, which <c>02 §4.2</c>
/// settles in one sentence — *a Policy paying daily is a different city from one paying weekly*. It is
/// hash-bearing, and it is in <c>plans/0002</c> §D with a named ratifier, per <c>adr/0052</c>.
/// </para>
/// <para>
/// <b><see cref="RevisitTicks"/> is a duration and the sample is derived from it</b> (<c>adr/0059</c>).
/// It replaces an absolute count of Lots per trigger, which was the wrong unit: a constant count makes
/// the quantity the city actually feels — a fraction of the city per cycle — inversely proportional to
/// the size of the city, so the shipped Ruleset visited a Lot once per 0.12 Day at 1,000 Citizens and
/// once per 117 Days at 1,000,000, and built nothing at all at target scale. It is hash-bearing and
/// needs no ratifier, because its default is <see cref="Ticks.PerDay"/> and therefore derived.
/// </para>
/// </remarks>
/// <param name="Kind">The Building kind this Rule builds. Never zero — the loader refuses that.</param>
/// <param name="Zone">Which permission bit admits <paramref name="Kind"/> here.</param>
/// <param name="Interval">Ticks between triggers.</param>
/// <param name="RevisitTicks">How long the industry takes to look at every Lot once.</param>
public readonly record struct ZoneRuleDefinition(byte Kind, byte Zone, uint Interval, int RevisitTicks)
{
    /// <summary>The permission set a Lot must carry for this Rule to build on it.</summary>
    /// <remarks>
    /// Through <see cref="IntegerMath.ShiftLeft"/> rather than <c>&lt;&lt;</c>, per <c>BOR0204</c>:
    /// C# masks a shift count against the operand width, so an out-of-range <see cref="Zone"/> would
    /// silently wrap to a valid bit rather than throwing. The loader refuses such a bit at load time;
    /// this is the second side of that check, and the analyser is what insisted on it.
    /// </remarks>
    public ushort Admits => (ushort)IntegerMath.ShiftLeft(1, Zone);

    /// <summary>
    /// How many Lots one trigger evaluates in a city of <paramref name="lots"/> Lot slots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>ceil(lots × interval ÷ revisit_ticks)</c>, and the ceiling is the half that matters</b>
    /// (<c>adr/0059</c>). Flooring returns <b>zero</b> for any city smaller than
    /// <c>revisit_ticks ÷ interval</c> Lots — 256 at the defaults, which is every fixture in the suite
    /// and every city a player opens on — so it would make the mechanism silently stop existing on
    /// small worlds. That is the defect this derivation replaces, wearing the opposite sign.
    /// </para>
    /// <para>
    /// <b>The ceiling is also why a small city revisits <em>faster</em> than the file asks for</b>,
    /// rather than within rounding of it: at 132 Lots and the defaults the exact answer is 0.52 and the
    /// answer given is 1, so the industry surveys that city roughly twice a Day. The error is bounded
    /// by one Lot a trigger and its sign is toward doing the work, which is the direction a rounding
    /// error in a mechanism nobody can see should point.
    /// </para>
    /// <para>
    /// <b>In <see cref="long"/> because the product overflows an <see cref="int"/> and the answer does
    /// not.</b> 900,000 Lots at an interval of 8,191 is 7.4e9. The result is bounded by
    /// <paramref name="lots"/> wherever the loader has been through the file, since it refuses a
    /// revisit period shorter than the interval.
    /// </para>
    /// </remarks>
    /// <param name="lots">The Lot table's <em>slot</em> count, which is the population sampled from.</param>
    /// <exception cref="InvalidOperationException">
    /// <see cref="RevisitTicks"/> is not positive, which only a Ruleset built in code can be.
    /// </exception>
    public int SampleFor(int lots)
    {
        if (RevisitTicks < 1)
        {
            throw new InvalidOperationException(
                $"a Zone Rule has a revisit period of {RevisitTicks}. It is how long the development "
                + "industry takes to look at every Lot once, so it divides; the loader refuses "
                + "anything below the trigger interval, and this Ruleset was not built by it.");
        }

        return (int)IntegerMath.CeilDiv((long)lots * Interval, RevisitTicks);
    }
}

/// <summary>
/// Which population a <see cref="PolicyDefinition"/> sweeps.
/// </summary>
/// <remarks>
/// <b><c>02 §4.2</c> names three — <em>Households, Businesses, or Buildings matching a predicate</em>
/// — and one is built.</b> The other two are declared and throw by name where they would be resolved,
/// on <see cref="Scope.Pool"/>'s precedent, because a named hole is better than a case that silently
/// falls through and sweeps nobody. A Policy that swept nobody would report a trigger, no
/// applications and no failure, which is the silent non-event <c>02 §4.1</c> bans.
/// </remarks>
public enum PolicySubject : byte
{
    /// <summary>
    /// Reserved, and never a declared Policy's subject. A zeroed row must not read as whichever
    /// population happened to be declared first — <see cref="BinOwnerKind.None"/>'s reason.
    /// </summary>
    None = 0,

    /// <summary>Every live Household. The only one built (<c>plans/0033</c> task 5).</summary>
    Household = 1,

    /// <summary>
    /// Every live Business. <b>Declared and not yet swept</b> — a Business has a balance and nothing
    /// that moves it.
    /// </summary>
    Business = 2,

    /// <summary>
    /// Every standing Building matching a predicate. <b>Declared and not yet swept</b>, and it needs
    /// the predicate first.
    /// </summary>
    Building = 3,
}

/// <summary>
/// One Policy: the Sweep family's second member, and <c>02 §4.2</c>'s <em>Flow</em> kind.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Policy sweeps where a Zone Rule samples, and that is a semantic difference rather than a
/// performance one</b> (<c>02 §4.2</c>). A developer genuinely does not evaluate every parcel, so a
/// <see cref="ZoneRuleDefinition"/>'s sample <em>is</em> the behaviour model; a transfer is an
/// <b>entitlement</b>, and paying a random subset of the eligible would be a defect rather than a
/// model. Anything reaching for a sample to make a Policy affordable has confused the two.
/// </para>
/// <para>
/// ⚠ <b>The transfer is a direction and an amount rather than two term lists, and that is what makes
/// conservation unrepresentable-wrong.</b> A <c>[[rule]]</c>'s terms are free-form, so
/// <c>RulesetLoader</c> needs <b>refusal 4</b> — <em>every money term needs a counterparty; a cost
/// paid to nobody is a leak, not a cost</em> — to catch a Rule that draws money and returns none. A
/// Policy names <see cref="From"/> and <see cref="To"/>, so the same quantity leaves one Bin and
/// enters the other by construction and there is no unbalanced shape to refuse.
/// ***A transfer written as a direction cannot leak; one written as two lists has to be checked.***
/// </para>
/// <para>
/// <b><see cref="Interval"/> is Ruleset data rather than a scheduling knob</b>, which <c>02 §4.2</c>
/// settles in the sentence this type is the first instance of — <em>a Policy paying daily is a
/// different city from one paying weekly</em>. A Sweep Rule never subscribes, so there is nothing for
/// a scheduler to be clever about.
/// </para>
/// <para>
/// <b>Only <see cref="Scope.Local"/> and <see cref="Scope.Global"/> are authorable.</b>
/// <see cref="Scope.Pool"/> is a <em>market</em> whose payment is implicit at the prevailing price
/// (<c>adr/0050</c>), so a Policy writing one would be authoring a payment the design says is never
/// authored; <see cref="Scope.Map"/> is not a Bin at all. The loader refuses both by name.
/// </para>
/// </remarks>
/// <param name="Subject">Which population this sweeps.</param>
/// <param name="Interval">Ticks between triggers. <b>Hash-bearing.</b></param>
/// <param name="Apply">
/// How much moves per member: a band, or a percentage of a Readout scoped to
/// <paramref name="Subject"/>. <b>Hash-bearing.</b>
/// </param>
/// <param name="From">Whose Bin the money leaves.</param>
/// <param name="To">Whose Bin it enters.</param>
/// <param name="Resource">The conserved Resource moved. <b>Structure</b>, not a number.</param>
/// <param name="Amount">How much one application moves. <b>Hash-bearing.</b></param>
public readonly record struct PolicyDefinition(
    PolicySubject Subject,
    uint Interval,
    ApplyCount Apply,
    Scope From,
    Scope To,
    ResourceId Resource,
    int Amount);

/// <summary>
/// One <c>[[hinterland]]</c> table — <b>the economy behind one map edge</b> (<c>adr/0088</c>,
/// <c>adr/0131</c>, milestone 11 task 2).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is the one authored anchor under every price in the design</b> (<c>adr/0026</c>,
/// <c>adr/0050</c>) — <c>CONTEXT.md</c> → Hinterland: *"a designer authors four objects and never
/// writes a price anywhere else."* Four edges means four of these, drifting independently, and that
/// is what makes the Outside legible: a single hidden anchor has no referent, where four comparable
/// markets are each other's referent.
/// </para>
/// <para>
/// 🔴 <b>Two fields, and the shortness is the decision rather than a stub.</b>
/// <c>adr/0131</c>: <b>a Hinterland field is authored in the milestone that reads it.</b> The rest of
/// what <c>CONTEXT.md</c> lists arrives with its reader — a price per Good at 13, a median wage at
/// 15, median rent and service levels and the commute figure at 16 with the comparison. ⚠ <b>Depth
/// and a recovery rate are the ones worth naming as absent</b>, because a stock is the first thing a
/// reader expects here: they are at <b>16</b> because <c>CONTEXT.md</c>'s drawdown gets both its
/// properties — *raises its rate* and *skews its mix* — from taking the most willing first, and the
/// willingness ordering <em>is</em> the comparison. A stock decrementing with nothing to order by can
/// express only availability, arrivals then none. ***A stock without an ordering is a wall, whatever
/// the design calls it*** — and the wall is the population ceiling that entry refuses by name.
/// </para>
/// <para>
/// <b>Keyed by <see cref="Edge"/> and not by declaration order</b>, which is where this parts company
/// from <see cref="PolicyDefinition"/> and <see cref="ZoneRuleDefinition"/>. Those two are iterated
/// and their index is a coordinate of a draw, so reordering the tables moves the State Hash. A
/// Hinterland is <em>looked up</em> — by the edge a gate stands on — so its order is not content, and
/// the loader refuses a second table for one edge rather than letting the later one win.
/// </para>
/// <para>
/// ⚠ <b>Nothing pairs a gate with a Hinterland at load, and it could not.</b> Which edge an Outside
/// Connection stands on is a property of where it was <em>placed</em>
/// (<see cref="Entities.World.EdgeOf"/>), not of the Ruleset, so a file declaring a gate kind and no
/// <c>[[hinterland]]</c> is not refusable here — the loader cannot see a world. The pairing is the
/// arrival path's, and it is milestone 11 task 4's.
/// </para>
/// </remarks>
/// <param name="Edge">
/// Which map edge this economy sits behind. <b>The identity</b>, and never
/// <see cref="MapEdge.None"/> in a loaded Ruleset.
/// </param>
/// <param name="EmigrantBalanceMin">
/// The floor of what a Household crossing into the city brings with it. <b>Hash-bearing</b> once
/// arrival draws it.
/// </param>
/// <param name="EmigrantBalanceMax">The ceiling of the same band. <b>Hash-bearing.</b></param>
public readonly record struct HinterlandDefinition(
    MapEdge Edge, Money EmigrantBalanceMin, Money EmigrantBalanceMax)
{
    /// <summary>
    /// Whether an emigrant from here carries anything at all.
    /// </summary>
    /// <remarks>
    /// <b>A Hinterland whose emigrants arrive penniless is a real economy, not an unset field</b> —
    /// which is why a zero band is accepted where <see cref="KindDefinition.ArrivalsPerDay"/>'s zero
    /// is refused. A gate admitting nobody is a door that never opens; a Household arriving with
    /// nothing still arrives, still joins the Unplaced Pool and still has to be housed.
    /// </remarks>
    public bool Endows => EmigrantBalanceMax.Raw > 0;

    /// <summary>
    /// What the Household whose never-reused id is <paramref name="entityId"/> carries across.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="HouseholdRuleset.OpeningBalance"/>'s draw, on a different tag and a different
    /// band</b>, and every argument that one makes applies here unchanged: uniform over the band with
    /// no shape parameter, because a skew is a second decision with a number in it and nothing has
    /// measured which (<c>adr/0052</c>); drawn on the Household's own id at <see cref="Ticks.Zero"/>,
    /// because it answers <em>what sort of Household is this</em>; and consumed once rather than
    /// re-derived, because an endowment is issued and then spent.
    /// </para>
    /// <para>
    /// ⚠ <b>The two draws use different <see cref="PurposeTag"/>s and the reason is not the obvious
    /// one.</b> The populations do not overlap — a Household is founded <em>or</em> it arrives — so
    /// sharing would collide with nothing and would still be wrong: the same id takes the same
    /// fraction of whichever span it is given, so the family that would have been richest at the
    /// founding is the richest emigrant from every edge. <c>PurposeTag.EmigrantBalance</c> carries
    /// the argument in full.
    /// </para>
    /// <para>
    /// <b>The four edges differ deliberately, and the difference is the whole point of the object.</b>
    /// Four identical Hinterlands would make edge selection inert, which is the one thing
    /// <c>CONTEXT.md</c> → Hinterland says the Outside needs in order to be legible: *four comparable
    /// markets are each other's referent.*
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed.</param>
    /// <param name="entityId">The Household's monotonic id — never its slot, which is recycled.</param>
    public Money EmigrantBalance(WorldKey key, ulong entityId)
    {
        long span = (EmigrantBalanceMax - EmigrantBalanceMin).Raw + 1;

        if (span <= 1)
        {
            return EmigrantBalanceMin;
        }

        ulong draw = Randomness.Draw(key, entityId, Ticks.Zero, PurposeTag.EmigrantBalance);

        return EmigrantBalanceMin + new Money((long)(draw % (ulong)span));
    }
}

/// <summary>
/// Everything the placement pass takes from the Ruleset: how often it runs, how long it takes to
/// look at everybody, and how many dwellings a Household considers per occasion.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>02 §5.2</c> step 2, which had no implementation and no owner until <c>adr/0069</c>.</b>
/// Placement drains the Unplaced Pool into vacant declared capacity in Buildings that already stand,
/// which is the thing that was missing when a Zone Rule raising a Building was the only door into a
/// dwelling in the entire simulation.
/// </para>
/// <para>
/// <b>Absent means the city does not house people</b>, which is a coherent Ruleset and is what every
/// fixture written before this meant. It is <em>not</em> given working defaults: the numbers below
/// are hash-bearing and <c>CLAUDE.md</c>'s rule is that a tuning value belongs in the file rather
/// than in the binary, so a default here would be the <c>const</c> that rule forbids wearing a
/// different hat. <see cref="LayerRuleset.Default"/> can have one because its default *is* the prior
/// behaviour; there is no prior behaviour here.
/// </para>
/// </remarks>
/// <param name="Interval">Ticks between passes. Zero means placement does not run.</param>
/// <param name="RevisitTicks">How long the pass takes to look at every member of the Pool once.</param>
/// <param name="Candidates">Dwellings a Household considers per occasion — <c>02 §5.3</c>'s <c>N</c>.</param>
/// <param name="GivesUpAfterDays">
/// How long a Household keeps looking before it gives up and leaves. <b>Zero means nobody ever gives
/// up</b>, which is only a coherent Ruleset in a file with no gate in it.
/// </param>
public readonly record struct PlacementRuleset(
    uint Interval, int RevisitTicks, int Candidates, int GivesUpAfterDays)
{
    /// <summary>A Ruleset whose city houses nobody.</summary>
    public static PlacementRuleset None => default;

    /// <summary>Whether placement runs at all.</summary>
    public bool Runs => Interval != 0;

    /// <summary>Whether a Household in the Pool ever gives up.</summary>
    public bool GivesUp => GivesUpAfterDays > 0;

    /// <summary>
    /// How long a Household keeps looking, in Ticks.
    /// </summary>
    /// <remarks>
    /// <b>The file states Days and the engine holds Ticks</b>, which is
    /// <c>[[building]] arrivals_per_day</c>'s idiom and the same reason: the felt quantity is *how
    /// long a family waits for a home*, and a Tick is a sampling rate rather than a unit anybody
    /// thinks in (<c>adr/0094</c>). Authoring it in Ticks would make the felt quantity move whenever
    /// <c>TICKS_PER_DAY</c> did.
    /// </remarks>
    public long GivesUpAfterTicks => (long)GivesUpAfterDays * Ticks.PerDay;

    /// <summary>
    /// How many occasions a Household gets before it gives up, at the cadence in force.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived, and this is the direction <c>adr/0130</c> is about.</b> The Ruleset authors the
    /// duration and the count falls out of <see cref="RevisitTicks"/> — so retuning the placement
    /// cadence changes how many looks a family gets and <em>not</em> how long they wait, which is
    /// what somebody editing <c>[placement]</c> would expect. Authored the other way round, the felt
    /// quantity would move every time a cadence was tuned (<c>adr/0059</c>, one level down).
    /// </para>
    /// <para>
    /// ⚠ <b>Nothing bounds on this and it is Evidence.</b> It is the *over 4 months* half of
    /// <c>00-vision.md</c>'s <i>"Considered 20 dwellings over 4 months"</i> — a description of what a
    /// Household got, reported beside the count of what it saw. Bounding on it would put the Pool's
    /// sink behind a random draw: the sample is drawn rather than swept, so a Household that is never
    /// picked accrues no occasions and would never leave, which is exactly the
    /// <see href="../../docs/adr/0006-no-collection-grows-with-elapsed-time.md">adr/0006</see> hole
    /// the bound exists to close.
    /// </para>
    /// </remarks>
    public long OccasionsBeforeGivingUp =>
        RevisitTicks <= 0 ? 0 : IntegerMath.FloorDiv(GivesUpAfterTicks, RevisitTicks);

    /// <summary>
    /// How many Pool members one pass considers, given <paramref name="pool"/> of them.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0059</c>'s derivation, over the Pool instead of over the Lot table.</b> The file
    /// states a <em>duration</em> — how long a Household waits between being looked at — and the count
    /// falls out of it, so the mechanism does not silently stop existing as the city grows. That is
    /// the defect <c>adr/0059</c> was written for, and placement would have had it in the identical
    /// form: an absolute count against a Pool that scales with the population is a city that houses a
    /// steadily smaller fraction of the people waiting.
    /// <para>
    /// The ceiling is load-bearing for the same reason it is there: flooring returns <b>zero</b> for
    /// any Pool smaller than <c>revisit_ticks ÷ interval</c>, which is every fixture in the suite and
    /// every city a player opens on, so placement would appear not to exist on small worlds.
    /// </para>
    /// </remarks>
    /// <param name="pool">How many Households are unplaced.</param>
    public int SampleFor(int pool)
    {
        if (RevisitTicks < Interval)
        {
            throw new InvalidOperationException(
                $"placement has a revisit period of {RevisitTicks} Ticks and an interval of "
                + $"{Interval}. The period is how long the pass takes to look at every unplaced "
                + "Household once, so it divides; the loader refuses anything below the interval, "
                + "and this Ruleset was not built by it.");
        }

        return (int)IntegerMath.CeilDiv((long)pool * Interval, RevisitTicks);
    }
}

/// <summary>
/// The <c>[roads]</c> table — <b>what a Ruleset says about the shape and speed of the road
/// network</b>, realised by <c>Borough.Core.Space.RoadGenerator</c> and read on every rebuild of the
/// Road Graph's derived columns.
/// </summary>
/// <remarks>
/// <para>
/// <b>Ruleset data rather than a parameter struct or a <c>const</c>, on the rule that put
/// <c>[layers]</c> and <c>[placement]</c> in TOML</b> (<c>adr/0015</c>: <i>everything the designer
/// would want to change lives in the Ruleset and is hot-reloadable</i>). The spike carried these as
/// <c>GraphParameters</c>, a swept benchmark axis, which is the right shape for a measurement and the
/// wrong one for content.
/// </para>
/// <para>
/// <b>The whole table is optional and its absence means there are no roads</b>, which is
/// <see cref="PlacementRuleset"/>'s polarity rather than <c>[layers]</c>'s. A default here would put
/// eight hash-bearing numbers in the binary with nobody having authored them (<c>adr/0052</c>), and
/// the failure that hides is quiet: a city laced with roads its author never asked for. A city with
/// no roads is loud — <c>--roads</c> refuses to print, every catchment query still throws, and no Lot
/// has frontage — which is <c>HONEST DEGRADATION</c> choosing the visible failure.
/// </para>
/// <para>
/// <b>The speeds are authored in km/h and the capacities in Vehicles per hour, and both are converted
/// exactly at load.</b> <c>02 §2</c> is categorical that there are no seconds in the library and no
/// metres; the conversion therefore happens where a human authors a number and never at runtime,
/// which is the arrangement <c>adr/0071</c> carries over from the spike unchanged.
/// </para>
/// <para>
/// <b>Every number here is hash-bearing and none is ratified.</b> The three speeds and the road
/// density have named ratifiers in <c>plans/0002</c> §D and no values; <c>adr/0071</c> chose their
/// <em>representation</em> and states in terms that choosing one ratifies no value.
/// </para>
/// </remarks>
/// <param name="BlockTiles">
/// Street grid spacing in Tiles — the block, and <b>the axis that sets Segment count</b>. A Cell is
/// 32 Tiles, so 32 is one Street on every Cell boundary.
/// </param>
/// <param name="ArterialCount">How many freeform Arterials cross the map.</param>
/// <param name="ArterialJunctionTiles">Tiles of Arterial between authored Junction pieces.</param>
/// <param name="FootCrossingEvery">
/// Keep a foot crossing at every <i>n</i>th Street an Arterial severs. <b>Severance's dial</b> —
/// between junctions an Arterial carries no pedestrian Arcs at all, so this decides whether a
/// neighbourhood is cut off from the shops that served it.
/// </param>
/// <param name="FootPathsPerThousandBlocks">Foot-only block cut-throughs per thousand blocks.</param>
/// <param name="StreetSpeed">A Street's free-flow speed.</param>
/// <param name="ArterialSpeed">An Arterial's free-flow speed.</param>
/// <param name="WalkSpeed">
/// Walking pace. <b>Both a foot-only Segment's free-flow speed and the ceiling on the foot traversal
/// of any Segment</b> — a pedestrian walks at walking pace on a boulevard and in a lane alike.
/// </param>
/// <param name="StreetCapacityPerDay">A Street's flow capacity, whole Vehicles per Day.</param>
/// <param name="ArterialCapacityPerDay">An Arterial's flow capacity, whole Vehicles per Day.</param>
/// <param name="FootPathCapacityPerDay">
/// A foot-only Segment's flow capacity. Nominal and non-zero, so that <c>volume / capacity</c> is
/// defined on a Segment no Vehicle can enter rather than dividing by zero.
/// </param>
public readonly record struct RoadRuleset(
    int BlockTiles,
    int ArterialCount,
    int ArterialJunctionTiles,
    int FootCrossingEvery,
    int FootPathsPerThousandBlocks,
    Speed StreetSpeed,
    Speed ArterialSpeed,
    Speed WalkSpeed,
    int StreetCapacityPerDay,
    int ArterialCapacityPerDay,
    int FootPathCapacityPerDay)
{
    /// <summary>A Ruleset whose world has no roads.</summary>
    public static RoadRuleset None => default;

    /// <summary>Whether there are roads at all. <see cref="BlockTiles"/> is what a graph cannot lack.</summary>
    public bool Runs => BlockTiles != 0;

    /// <summary>The free-flow speed of a Segment of this kind, in the Ruleset currently in force.</summary>
    public Speed SpeedFor(RoadKind kind) => kind switch
    {
        RoadKind.Arterial => ArterialSpeed,
        RoadKind.FootPath => WalkSpeed,
        _ => StreetSpeed,
    };

    /// <summary>The flow capacity of a Segment of this kind, in the Ruleset currently in force.</summary>
    public int CapacityFor(RoadKind kind) => kind switch
    {
        RoadKind.Arterial => ArterialCapacityPerDay,
        RoadKind.FootPath => FootPathCapacityPerDay,
        _ => StreetCapacityPerDay,
    };
}

/// <summary>
/// One <c>[[lattice]]</c> table — <b>where the generator lays a Street lattice</b>, and the only
/// thing a Ruleset can say about <em>where development is</em>.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Lattice is authored and a Settlement, a District and a centre are all derived, which is the
/// whole reason this is not called any of them.</b> <c>CONTEXT.md</c> → Settlement is a commute shed:
/// <i>"connectivity is transitive, so a contiguously-developed lattice is one Settlement however large
/// the graph"</i>. Two Lattices joined by a road within the Commute Budget are therefore <b>one</b>
/// Settlement, and a file authoring two <c>[[settlement]]</c> tables that produced one Settlement
/// would be contradicting the term it borrowed. <c>adr/0134</c>'s <b>centre</b> is the same hazard one
/// level down — a centre is a prominence peak the watershed <em>finds</em>, and a key that authored
/// one would be authoring the answer.
/// </para>
/// <para>
/// <b>Two numbers and no third, because the extent and the population share are derived.</b> A
/// Lattice paves what its share of the world's Lots needs — <c>SyntheticCity.PavedTiles</c>, which is
/// the expression that already sized the single lattice — and the shares are equal. Authoring either
/// would put two more unratified hash-bearing numbers per table into a file whose whole content is a
/// gap (<c>adr/0052</c>), and neither is what the gap is made of. <b>The origins are the gap.</b>
/// </para>
/// <para>
/// <b>The absence of the table is one Lattice at the origin corner</b>, which is what every world this
/// build could generate before 2026-08-22 — so no shipped Ruleset's State Hash moves by this key
/// arriving. It is <c>[layers]</c>'s polarity rather than <c>[roads]</c>'s, and legitimately: there
/// <em>is</em> an earlier behaviour to preserve here, and it is exactly one Lattice at (0, 0).
/// </para>
/// <para>
/// ⚠ <b>World-creation data, not tuning.</b> Where the land is cannot be reloaded — a standing city
/// does not move — so a second Lattice appearing on a hot reload would describe ground that has no
/// roads on it. It is not yet in <c>RulesetLoader.Reload</c>'s frozen set for the reason that set is
/// small: <c>LayerConstants</c> is what a world records, and a world records no Lattice. <b>What it
/// records is the graph the Lattices produced</b>, and <c>RoadGenerator.LayInto</c> refuses a world
/// that already has Segments.
/// </para>
/// </remarks>
/// <param name="OriginEastTiles">
/// The Tile the lattice's west edge stands on. A multiple of <c>[roads] block_tiles</c>, so that every
/// Node in the world sits on one block grid and the link between two Lattices is a whole number of
/// blocks.
/// </param>
/// <param name="OriginNorthTiles">The Tile the lattice's south edge stands on. Same constraint.</param>
public readonly record struct LatticeDefinition(int OriginEastTiles, int OriginNorthTiles);

/// <summary>
/// The <c>[lots]</c> table — <b>how zoned land is carved into parcels</b> (<c>adr/0078</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>One number, and it is the only one the subdivider takes.</b> <c>02 §2.2</c> asks for
/// <i>"Lot depth and width targets"</i>; width is this, and <b>depth does not exist</b> — a Lot has
/// no extent in <c>LotTable</c>, so a depth would be a hash-bearing world-creation number chosen for
/// a consumer nobody has designed. Lots hang on Segments and everything else is block interior,
/// structurally.
/// </para>
/// <para>
/// <b><see cref="LotsPerSegment"/> is derived rather than chosen</b>, which is why this table has one
/// key instead of the two <c>plans/0022</c> predicted. It is <c>CONTEXT.md</c> → Address's own working
/// figure — <i>"five Buildings share a Segment"</i> — which is the premise of the decision that keeps
/// an Address off a Node and therefore of the ~30,000-Segment figure every routing cost is priced
/// against. If that figure moves, this moves with it and one sentence fixes both.
/// </para>
/// </remarks>
/// <param name="LotsPerSegment">How many Lots one Street Segment carries, both sides together.</param>
public readonly record struct LotRuleset(int LotsPerSegment)
{
    /// <summary>A Ruleset whose land cannot be subdivided at all.</summary>
    public static LotRuleset None => default;

    /// <summary>Whether the subdivider runs.</summary>
    public bool Runs => LotsPerSegment > 0;
}

/// <summary>
/// The <c>[trips]</c> table — <b>what a Ruleset says about travelling</b>: what a crossing costs
/// (<c>adr/0074</c>) and where the Commute Budget's three rungs fall (<c>adr/0095</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The Budget is three edges and only the last one refuses</b> (<c>adr/0095</c>).
/// <see cref="Fast"/> and <see cref="Moderate"/> grade a commute that happens anyway;
/// <see cref="CommuteBudget"/> is the <em>ceiling</em>, and it alone produces
/// <see cref="Movement.TripFate.ExceededCommuteBudget"/>. The reason is that
/// <c>adr/0017</c>'s satisficing rule is supposed to be graded and a single threshold makes a cliff
/// of it: a city whose commutes creep from twelve minutes to nineteen reports <b>zero</b>, and then
/// reports a cliff, arriving exactly when <c>01 §4</c>'s spatial fix has stopped being cheap.
/// </para>
/// <para>
/// <b>The three are all-or-nothing.</b> A Ruleset states every rung or no Budget at all — there is no
/// ceiling without grading, because a default for either lower rung would be the thing
/// <c>adr/0052</c> forbids, a hash-bearing number chosen because the schema wanted one. The loader
/// refuses a set that is not strictly increasing.
/// </para>
/// <para>
/// <b>Four numbers, all hash-bearing, and the ceiling is unset in a different sense from the
/// rest.</b> The crossing cost
/// is <em>chosen with a named ratifier</em> under <c>adr/0052</c> — a candidate value the first long
/// run reports the walk-Leg distribution at, against zero. The Commute Budget cannot be chosen the
/// same way, because it is a <b>percentile of a distribution that does not exist until commutes
/// do</b>, and picking one now would fabricate the thing the milestone exists to measure.
/// </para>
/// <para>
/// <b>So an omitted budget means there is no ceiling, and that is a stated city rather than a
/// placeholder.</b> Session F's finding is the constraint: <i>a placeholder whose value sits inside
/// the range of legitimate answers cannot announce itself</i>. Every minute count is a legitimate
/// budget, so no minute count can mean <i>unset</i> — <see cref="TravelTime.Impassable"/> can,
/// because it is outside the range and nothing can author it. A city with no Budget refuses no Trip
/// for its length, <see cref="Movement.TripFate.ExceededCommuteBudget"/> stays structurally
/// unreachable, and the Census says so. <b>That is also the only city in which the unconditioned
/// cost distribution can be measured</b>, since a Budget in force censors the distribution it would
/// be a percentile of.
/// </para>
/// <para>
/// <b>The absent table means the city has no Trip model at all</b>, which is <c>[roads]</c>'s
/// polarity rather than <c>[layers]</c>'s. It is carried as an impassable <em>crossing</em> rather
/// than as a flag because every other optional table has a key that cannot legitimately be zero and
/// this one does not: zero is a perfectly good crossing cost — it is rung 1, what the corpus had by
/// omission — so <c>crossing == 0</c> could never have meant <i>absent</i>. Under the sentinel a
/// consumer that skips the refusal produces an impassable Leg rather than a silently free crossing,
/// which fails loudly in the direction <c>HONEST DEGRADATION</c> asks for.
/// </para>
/// </remarks>
/// <param name="CrossingCost">
/// What it costs on foot to reach the other side of a Segment. <b>Charged exactly when two Addresses
/// share a Segment and differ in side</b> (<c>adr/0074</c>) and silent everywhere else, because
/// <i>the same side</i> stops meaning anything once a route turns a corner.
/// </param>
/// <param name="Fast">
/// The top of the <see cref="CommuteRung.Fast"/> band. <b>Refuses nothing</b> — it is where the
/// grading starts, not where anything fails.
/// </param>
/// <param name="Moderate">
/// The top of the <see cref="CommuteRung.Moderate"/> band, and equally toothless. A commute above it
/// and below the ceiling is <see cref="CommuteRung.Unsavoury"/> and still happens.
/// </param>
/// <param name="CommuteBudget">
/// <b>The ceiling</b>, and the only one of the three edges that produces a Trip Fate: the line
/// between a Trip that completes and one whose Fate is <i>exceeded commute budget</i>, or
/// <see cref="TravelTime.Impassable"/> for a city that has no such line.
/// </param>
public readonly record struct TripRuleset(
    TravelTime CrossingCost, TravelTime Fast, TravelTime Moderate, TravelTime CommuteBudget)
{
    /// <summary>
    /// A Ruleset whose city does not travel. <b>Not <c>default</c></b> — see the type's remarks: a
    /// zeroed crossing cost is a legitimate authored value, so absence needs the sentinel.
    /// </summary>
    public static TripRuleset None => new(
        TravelTime.Impassable, TravelTime.Impassable, TravelTime.Impassable, TravelTime.Impassable);

    /// <summary>Whether there is a Trip model at all.</summary>
    public bool Runs => !CrossingCost.IsImpassable;

    /// <summary>Whether a Trip's length can make it fail.</summary>
    public bool HasCommuteBudget => !CommuteBudget.IsImpassable;

    /// <summary>
    /// Whether a Trip costing <paramref name="cost"/> is one this city's people will make.
    /// </summary>
    /// <remarks>
    /// <b>An impassable cost answers <c>false</c> and it is not over budget</b> — it is no route, a
    /// different Fate with a different diagnosis, and the caller tests for it first. Answering
    /// <c>false</c> here as well is a backstop rather than the meaning: a caller that forgot must not
    /// send somebody down a route that does not exist.
    /// </remarks>
    public bool WithinBudget(TravelTime cost) => !cost.IsImpassable && cost <= CommuteBudget;

    /// <summary>
    /// Which band <paramref name="cost"/> falls in, or <c>false</c> when it is over the ceiling or
    /// has no route at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rung is derived from the cost every time it is asked for and is never stored</b>
    /// (<c>adr/0095</c>). It is a function of a Trip's cost and the Ruleset in force, and the Ruleset
    /// is hot-reloadable, so a stored rung would be <c>adr/0064</c>'s frozen-at-construction defect
    /// on a third axis: retuning a rung would grade the commutes made after the reload and leave
    /// every standing one carrying the old file's opinion.
    /// </para>
    /// <para>
    /// <b>Failing for <em>no route</em> and for <em>over the ceiling</em> in the same way is safe
    /// here and would not be at the Trip's write site.</b> This answers <i>is there a rung</i>, and
    /// there is no rung for either; <see cref="Movement.TripEngine"/> separates them because they are
    /// different Fates with different diagnoses, and it tests for impassability first.
    /// </para>
    /// </remarks>
    public bool TryRung(TravelTime cost, out CommuteRung rung)
    {
        rung = CommuteRung.Fast;

        if (!WithinBudget(cost))
        {
            return false;
        }

        rung = cost <= Fast ? CommuteRung.Fast
            : cost <= Moderate ? CommuteRung.Moderate
            : CommuteRung.Unsavoury;

        return true;
    }
}

/// <summary>
/// The <c>[traffic]</c> table — <b>what a Segment costs to drive when other people are on it</b>.
/// The volume-delay function, and it is BPR (<c>CONTEXT.md</c> → Volume-Delay Function).
/// </summary>
/// <remarks>
/// <para>
/// <b><c>free_flow × (1 + α(v/c)^β)</c>, evaluated on one Segment's own volume over its own
/// capacity.</b> <c>03 §3.2</c>'s whole argument for the fidelity ladder is that this formula is used
/// <em>only where it is strong</em> — unstressed Segments, where it sits near free-flow — and that the
/// saturated regime, where a memoryless function of one Segment cannot represent queueing or
/// spillback, is handled by the Microscopic tier instead.
/// </para>
/// <para>
/// ⚠ <b>The units do not match and the conversion is the load-bearing part.</b>
/// <see cref="Space.RoadSegmentTable.CapacityPerDay"/> counts Vehicles <em>passing</em> — a flow —
/// and <see cref="Space.RoadSegmentTable.VolumeForward"/> counts Vehicles <em>present</em> — a stock.
/// BPR's ratio is flow over flow. What converts one to the other is <c>adr/0041</c>'s own assumption
/// that <b>a vehicle crosses about one Segment per Tick</b>, which makes *present this Tick* and
/// *passing this Tick* the same number, so the denominator is <b>capacity per Tick</b>. That is exact
/// at the shipped figures rather than approximate: a Street's 3,600 Vehicles an hour is one a second,
/// a Tick is 42.1875 s (<c>adr/0094</c>), and <c>86,400 ÷ 2,048 = 42.1875</c>. ***The unit of a
/// quantity is not its denomination*** — the two columns are both in Vehicles and belong to different
/// kinds.
/// </para>
/// <para>
/// ⚠ <b>The crossing rate that licenses it is <em>measured at 0.79–0.83</em>, not 1.0</b> (S2 R2a,
/// reported free-flow and at the morning peak). So the ratio is overstated by about a fifth and the
/// VDF reads slightly busier than the city is. Recorded rather than corrected, because a correction
/// factor here would be a fourth hash-bearing number with no ratifier at all, and the direction is
/// conservative — it promotes sooner, which is the side <c>03 §3.4</c>'s self-correction argument
/// wants to err on.
/// </para>
/// <para>
/// <b>The three numbers have a <em>source</em> and are still unratified.</b> α = 0.15 and β = 4 are
/// textbook BPR and are what S2 ran; ⚠ the clamp is recoverable from S2 R8.0's own published figure —
/// *"an arc at the clamp costs **39.4×** free-flow"* — since <c>1 + 0.15 × 4⁴ = 39.4</c> exactly, so
/// S2's clamp was <c>v/c = 4</c>. <b>A source is not a ratification</b>: S2 is a synthetic harness
/// and every figure in this corpus that moved from a fixture to a real world moved the same way.
/// <c>adr/0052</c>, and the row is in <c>plans/0002</c> §D.
/// </para>
/// <para>
/// ⚠ <b>Past the clamp the multiplier is constant, and that is a blindness rather than a safeguard.</b>
/// S2 R8.3 measured it directly: above the ceiling *the router cannot tell a bad jam from a
/// catastrophic one*, and the share of readings past it is the column that showed Sight earning its
/// keep. The clamp exists because <c>β = 4</c> is a quartic and an unclamped <c>v/c</c> of 10 costs
/// 1,501× free-flow, which is positive feedback — a jammed arc's Travellers dwell longer, so its
/// volume rises further.
/// </para>
/// </remarks>
/// <param name="Alpha">BPR's α. The delay at exactly capacity, as a fraction of free-flow.</param>
/// <param name="Beta">BPR's β. The exponent, a small whole number.</param>
/// <param name="Clamp">The largest <c>v/c</c> the function will read. Above it, cost is constant.</param>
public readonly record struct TrafficRuleset(Ratio Alpha, int Beta, Ratio Clamp)
{
    /// <summary>
    /// A Ruleset whose roads never slow down.
    /// </summary>
    /// <remarks>
    /// <b>Free-flow everywhere is the meaning of omission</b>, on <see cref="JobRuleset.None"/>'s
    /// polarity — and here it is also the behaviour every Ruleset had before 5c task 6, so a file that
    /// states no <c>[traffic]</c> keeps exactly the city it had.
    /// </remarks>
    public static TrafficRuleset None => default;

    /// <summary>Whether this city's roads slow down at all.</summary>
    public bool Runs => Beta > 0;

    /// <summary>
    /// What a Segment costs to traverse at <paramref name="load"/>, given its free-flow time.
    /// </summary>
    /// <remarks>
    /// <b>Integer-only and left-to-right</b>: the power is <see cref="Beta"/> repeated
    /// <see cref="Ratio"/> multiplications rather than a call to anything, because there is no
    /// <c>Math.Pow</c> in this project and a Q16.16 multiply is the whole of what is needed for an
    /// exponent that is a small whole number.
    /// </remarks>
    public TravelTime Apply(TravelTime freeFlow, Ratio load)
    {
        if (!Runs || freeFlow.IsImpassable || load <= Ratio.Zero)
        {
            return freeFlow;
        }

        Ratio ratio = load > Clamp ? Clamp : load;
        Ratio power = ratio;

        for (int i = 1; i < Beta; i++)
        {
            power *= ratio;
        }

        return freeFlow * (Ratio.One + (Alpha * power));
    }
}

/// <summary>
/// The <c>[households]</c> table — <b>what a Household is beyond where it lives</b>. Today that is
/// one thing: whether it keeps a car.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is <c>01 §8</c> ledger #3's <em>stated</em> simple assumption, not a stand-in for it.</b>
/// That entry — <i>is car ownership a choice?</i> — has been live and half-answered since session
/// five, which settled the half that matters: <b>ownership is a persistent Household state</b>, with
/// a purchase price and a per-Day running cost. What is still open is only whether it is
/// <em>endogenous</em>, bought when commutes get bad and sold under pressure, and <c>01 §8</c> says
/// of that in its own words: <i>every Household owning a car is the simple assumption… only becomes
/// interesting once transit exists</i>. Transit has no milestone. So an exogenous rate is the design
/// being followed rather than <c>adr/0070</c>'s <i>given X does not exist, should Y compensate</i>.
/// </para>
/// <para>
/// ⚠ <b>Mode choice, as against car ownership, is <em>undesigned</em> and this table is not it.</b>
/// A Household here owns a car or does not, and a Citizen of one that does drives to work. Nobody
/// weighs a walk against a drive on the day, and nothing in the corpus specifies how they would —
/// mode choice appears in no milestone in <c>06</c> <em>and</em> in none of its
/// <i>Mechanisms with no milestone</i> rows, because that inventory's own opening line is
/// <i>every row below is settled by an ADR</i>. ⚠ <b>An inventory of unplaced mechanisms cannot list
/// a mechanism nobody designed</b>, which is why this reached a task before anybody noticed.
/// </para>
/// <para>
/// <b>Ownership is <em>derived from the rate</em> every time it is asked for and is never a column</b>
/// — <c>adr/0068</c>'s rule and <see cref="TripRuleset.TryRung"/>'s, arriving on a fourth axis. A
/// saved bit would be <c>adr/0064</c>'s frozen-at-construction defect: retuning the rate would move
/// the Households created after the reload and leave every standing one carrying the old file's
/// opinion, which makes a key in a hot-reloadable file silently world-creation-fixed. See
/// <c>PurposeTag.CarOwnership</c> for why the draw gives a <em>nested</em> set and therefore why the
/// reload does not churn the city.
/// </para>
/// <para>
/// ⚠ <b>One car per Household, and every working member drives it.</b> A Household of three workers
/// with one car puts three cars on the road here. That is wrong about a city and it is what
/// <c>01 §8</c>'s simple assumption says; a per-Citizen licence or a shared-vehicle constraint is a
/// second mechanism, and inventing one to patch this would be choosing a number for a consumer
/// nobody has designed.
/// </para>
/// </remarks>
/// <param name="CarOwnershipPercent">
/// The share of Households keeping a car, 0–100. <b>Hash-bearing</b>: it decides who drives, which
/// decides what a commute costs, which decides who takes which job.
/// </param>
/// <param name="OpeningBalanceMin">
/// The bottom of the band a Household is endowed from when the populator creates it, inclusive.
/// <b>Hash-bearing.</b> <see cref="Money.Zero"/> with <paramref name="OpeningBalanceMax"/> also zero
/// means the populator endows nobody, which is what every Ruleset written before milestone 10 task 5
/// meant by saying nothing.
/// </param>
/// <param name="OpeningBalanceMax">
/// The top of that band, inclusive. <b>Hash-bearing.</b>
/// </param>
public readonly record struct HouseholdRuleset(
    int CarOwnershipPercent, Money OpeningBalanceMin, Money OpeningBalanceMax)
{
    /// <summary>The whole range a rate may be authored in.</summary>
    public const int MaxPercent = 100;

    /// <summary>Whether the populator endows the Households it creates.</summary>
    public bool Endows => OpeningBalanceMax.Raw > 0;

    /// <summary>
    /// What the Household whose never-reused id is <paramref name="entityId"/> is founded with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A band rather than one figure, and the band is what makes <c>adr/0115</c>'s instrument
    /// readable at all.</b> That ADR's concern is a percentage that floors to zero on a small balance
    /// and to the stated rate on every other — a <em>distributional</em> artefact. A single opening
    /// figure has no distribution: every Household either floors or none does, so the counter reads
    /// 0% or 100% and says nothing about the city. ***An instrument that measures a spread cannot be
    /// read in a world that has none.***
    /// </para>
    /// <para>
    /// <b>Uniform over the band, with no shape parameter.</b> A real wealth distribution is skewed and
    /// this one is not; a skew is a second decision with a number in it and nothing has measured which
    /// (<c>adr/0052</c>). <c>adr/0101</c>'s triangular Shift draw is the counter-example that shows
    /// what a warrant looks like — it borrowed its shape from inside the mechanism, because a return
    /// is the sum of two uniforms — and there is no such argument here.
    /// </para>
    /// <para>
    /// <b>Drawn on the Household's own id at <see cref="Ticks.Zero"/></b>, which is
    /// <see cref="OwnsCar"/>'s form: it answers <em>what sort of Household is this</em> rather than
    /// <em>what happens now</em>. Unlike ownership it is <em>not</em> re-derived on every read — an
    /// endowment is issued once and then spent, so it is drawn at creation and lives in the balance
    /// Bin afterwards. Retuning the band therefore moves the Households created after the reload and
    /// leaves every standing one holding what it holds, which is correct: money already issued is
    /// money in the world, and a hot reload that redistributed it would be a Ruleset editing history.
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed.</param>
    /// <param name="entityId">The Household's monotonic id — never its slot, which is recycled.</param>
    public Money OpeningBalance(WorldKey key, ulong entityId)
    {
        long span = (OpeningBalanceMax - OpeningBalanceMin).Raw + 1;

        if (span <= 1)
        {
            return OpeningBalanceMin;
        }

        ulong draw = Randomness.Draw(key, entityId, Ticks.Zero, PurposeTag.OpeningBalance);

        return OpeningBalanceMin + new Money((long)(draw % (ulong)span));
    }

    /// <summary>
    /// A Ruleset whose city keeps no cars.
    /// </summary>
    /// <remarks>
    /// <b>Nobody drives is the meaning of omission, and it is not a placeholder.</b> Zero sits inside
    /// the range of legitimate answers, which session F's rule says a placeholder must not — so it is
    /// reached by the <em>absence of the table</em> rather than by a defaulted key, exactly as
    /// <see cref="JobRuleset.None"/> is. A file that states <c>[households]</c> must state the rate.
    /// </remarks>
    public static HouseholdRuleset None => default;

    /// <summary>Whether any Household in this city keeps a car.</summary>
    public bool Runs => CarOwnershipPercent > 0;

    /// <summary>
    /// Whether the Household whose never-reused id is <paramref name="entityId"/> keeps a car.
    /// </summary>
    /// <param name="key">The world seed.</param>
    /// <param name="entityId">The Household's monotonic id — never its slot, which is recycled.</param>
    public bool OwnsCar(WorldKey key, ulong entityId) =>
        Randomness.Draw(key, entityId, Ticks.Zero, PurposeTag.CarOwnership) % MaxPercent
            < (ulong)(uint)CarOwnershipPercent;
}

/// <summary>
/// The <c>[parking]</c> table — <b>how far a driver will walk from a Car Park</b> (<c>adr/0009</c>,
/// milestone 7 task 2).
/// </summary>
/// <remarks>
/// <para>
/// <b>One number, and it is the shed's whole shape.</b> A Parking Shed is the ordered set of Car
/// Parks within this reach of a Building's pedestrian Access Point; arrival takes the nearest with
/// space, and the walk from it is a Leg. So the radius decides which Car Park a car takes, which
/// decides the walk Leg, which counts against the Commute Budget, which decides whether the Trip
/// fails — <b>hash-bearing</b> by four steps, and unratified since 2018-era prose.
/// </para>
/// <para>
/// <b>Authored in metres and stored in <see cref="Tiles"/>, and the unit was a decision.</b> Every
/// measurement this corpus holds about the quantity is in metres — S2 R5.6's <em>110 Car Parks found
/// at 400 m against 596 at 800 m</em> — so a key in metres is the one a reader can check against the
/// evidence without converting, which is <c>plans/0012</c> <b>Cause 5</b>'s reading half: quote the
/// sentence, not the digits. ⚠ <b>Minutes were the live alternative and were refused</b>: the shed's
/// bound is stated in the Commute Budget's currency, which would have made the upper bound a refusal
/// rather than a sentence — but a key in minutes invites the one derivation
/// <c>adr/0083</c> forbids by name (<em>five minutes at 5 km/h is 417 m</em>), and it would make shed
/// membership move when a designer retunes <c>walk_speed_kph</c>. <b>A radius in metres is the same
/// set of Car Parks however fast anybody walks.</b>
/// </para>
/// <para>
/// ⚠ <b><c>adr/0083</c>'s upper bound is stated and not enforced, and a guard for it was written and
/// withdrawn.</b> A shed wider than the Commute Budget's walk allowance has outer Car Parks that can
/// never be taken — but the Budget is a ceiling on a <b>whole journey</b> and a parking walk is one
/// <b>Leg</b> inside it, so the only non-arbitrary threshold available is the whole Budget, which is
/// far looser than the real constraint. ***A bound stated as a constraint on choosing a number is not
/// thereby a predicate over two files.*** It lives in <c>plans/0002</c> §D2 and in
/// <c>minimal.toml</c>'s header.
/// </para>
/// </remarks>
/// <param name="RadiusMetres">
/// How far a driver will walk from a Car Park, in metres, as authored. <b>Kept as authored rather
/// than only as <see cref="Radius"/></b>: a diagnostic that reported a rounded Tile count would be
/// reporting a number the designer never wrote, and reload comparison is against the file.
/// </param>
public readonly record struct ParkingRuleset(int RadiusMetres, int ShedKeeps)
{
    /// <summary>
    /// A Ruleset whose cities have no Parking Shed.
    /// </summary>
    /// <remarks>
    /// <b>Absence is the unset spelling, on <see cref="HouseholdRuleset.None"/>'s rule.</b> Every
    /// radius in range means something — a small one is a city of driveways, a large one a city of
    /// long walks — so no value inside the range can do duty as <em>unset</em>, and the absence has
    /// to be the absence of the table. A file that states <c>[parking]</c> must state the radius.
    /// </remarks>
    public static ParkingRuleset None => default;

    /// <summary>Whether this city has a Parking Shed at all.</summary>
    public bool Runs => RadiusMetres > 0;

    /// <summary>
    /// The reach, in <see cref="Tiles"/> — what the shed is actually built against.
    /// </summary>
    /// <remarks>
    /// <b>Rounded up</b> (<see cref="Tiles.FromMetres"/>), because a radius is a reach and a shed
    /// silently shorter than its file says is supply the city has and cannot find.
    /// </remarks>
    public Tiles Radius => Tiles.FromMetres(RadiusMetres);

    /// <summary>
    /// How many Car Parks a Building's shed holds — the nearest <see cref="ShedKeeps"/> of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This and <see cref="RadiusMetres"/> are not two knobs for one thing, and which one binds is
    /// a property of the world rather than of the file.</b> The <b>cap bounds the work</b> and binds
    /// where supply is dense; the <b>radius bounds the walk</b> and binds where supply is sparse —
    /// because with fewer than this many Car Parks in reach the kept set never fills, the ball's early
    /// exit never fires, and it walks the whole radius. Measured on a generated city at 400 m, the
    /// <em>minimum</em> over every Building is 35 Car Parks in range, so there the cap binds at every
    /// door and the radius binds nowhere. A player's thinly-built edge is the other case.
    /// </para>
    /// <para>
    /// ⚠ <b>World-creation, and it must not be hot-reloadable</b>, on <c>[layers] kernel_metres</c>'
    /// precedent: the shed table is fixed-width, so a reload that changed this would reallocate every
    /// shed in the city.
    /// </para>
    /// </remarks>
    public int Keeps => ShedKeeps;
}

/// <summary>
/// The <c>[jobs]</c> table — <b>how a Citizen with no Workplace comes to have one</b>
/// (<c>adr/0081</c>, <c>adr/0017</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Three numbers, and they are <see cref="PlacementRuleset"/>'s three because this is
/// <see cref="PlacementRuleset"/>'s shape</b>: a sampled sweep over a population, looking for
/// something with room. <c>adr/0069</c> is the standing warning that such a pass needs exactly these
/// and that its ADR will predict none of them — so they are named in advance here rather than
/// discovered, and each carries a <c>plans/0002</c> §D row.
/// </para>
/// <para>
/// <b>They are their own table rather than <c>[placement]</c>'s</b>, because a Household looks for a
/// home at a different rate than a person looks for work, and one cadence could not be retuned for
/// either without moving the other. What they share is the <em>derivation</em>: the file states a
/// <b>duration</b> and the engine derives the sample from it (<c>adr/0059</c>), so the mechanism does
/// not silently stop existing as the city grows.
/// </para>
/// <para>
/// <b>There is no search radius here, and that absence is the decision.</b> A candidate has to come
/// from somewhere near home or the commute distribution is S2 R4's uniform draw — the fabricated
/// number this milestone's named risk is about. The box is <b>derived from the Commute Budget and
/// the walking speed</b>: it is what a walk within the Budget can reach, so looking outside it is
/// looking where nothing could be accepted. That is why the loader refuses a <c>[jobs]</c> table in
/// a Ruleset with no <c>commute_budget_minutes</c> — the pass would have no bound, and the bound
/// nobody authored is the one that would have to be invented.
/// </para>
/// </remarks>
/// <param name="Interval">Ticks between passes. Zero means nobody is ever assigned work.</param>
/// <param name="RevisitTicks">How long the pass takes to look at every Citizen once.</param>
/// <param name="Candidates">Places a Citizen looks at per occasion — <c>02 §5.3</c>'s <c>N</c>.</param>
/// <param name="ShiftHoursMin">The shortest working day a Citizen may draw, in in-world hours.</param>
/// <param name="ShiftHoursMax">The longest. <b>Drawn per Citizen, converted to Ticks before the draw
/// so the result is continuous</b> — the band is authored in hours because a designer has a reason for
/// <em>six to ten</em> and none for a range of Ticks (<c>adr/0059</c>), and drawing in the authored
/// unit would quantise every return in the city onto a handful of Ticks.</param>
/// <param name="ArriveEarlyMaxMinutes">
/// How long before their Shift starts a Citizen aims to be there, drawn per Citizen and persisting.
/// <b>What separates a departure from an exact hour mark</b>: departure is
/// <c>start − commute − margin</c>, and on a generated city the commute alone is about four minutes
/// against an eighty-five-Tick hour. <c>adr/0101</c>.
/// </param>
public readonly record struct JobRuleset(
    uint Interval,
    int RevisitTicks,
    int Candidates,
    int ShiftHoursMin,
    int ShiftHoursMax,
    int ArriveEarlyMaxMinutes)
{
    /// <summary>A Ruleset whose city assigns nobody to work.</summary>
    public static JobRuleset None => default;

    /// <summary>
    /// How long <paramref name="id"/> works, in Ticks. Drawn once against the band in force.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This replaced <c>commute_peak_factor</c>, and the replacement <em>retires</em> a number
    /// rather than renaming one</b> (<c>adr/0101</c>). That key authored a departure window and the
    /// engine derived the peak from it; under a Day with a shape the peak is a <em>reading</em> — what
    /// the profile comes out as, given where the jobs are and how long people work. A dial that states
    /// its own answer cannot be ratified by measuring the answer, which is why the old row's ratifier
    /// had to be re-stated twice before it could refute anything.
    /// </para>
    /// <para>
    /// <b>A draw and not a column, because a Shift length is a property of a person.</b> It is a pure
    /// function of the Citizen's monotonic id, the world seed and the band in force, so it costs no
    /// storage, survives a rebuild exactly, and a retuned band reaches the standing city —
    /// <c>adr/0064</c>'s disposition, which is the same one occupancy, the jobs ceiling and car
    /// ownership have. ⚠ <b>The departure offset beside it is <em>not</em> like this and cannot be</b>:
    /// that is what a journey cost when a job was taken, and no function of an id recovers a fact
    /// about a past world. <em>A value drawn once is derivable and a value measured once is not.</em>
    /// </para>
    /// <para>
    /// <b>Uniform across the band, which is the only shape with evidence behind it — and the evening
    /// peak is what will refute it.</b> Nothing in the corpus has measured a distribution of working
    /// hours. A uniform draw over a wide band spreads the evening return over the whole band, so if
    /// the measured profile comes back with an evening that is flat where a city's is peaked, the
    /// answer is a narrower band rather than a shape invented here. That is the ratifier <c>adr/0101</c>
    /// names, and it refutes in a direction that says what to do.
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed, as the draw's first coordinate.</param>
    /// <param name="id">The Citizen's monotonic, never-reused id.</param>
    public Ticks ShiftLengthOf(WorldKey key, ulong id)
    {
        int first = Ticks.AtHour(ShiftHoursMin);
        int last = Ticks.AtHour(ShiftHoursMax);
        int span = last - first + 1;

        if (span < 1)
        {
            return new Ticks((ulong)(uint)Ticks.AtHour(Ticks.HoursPerDay));
        }

        ulong value = Randomness.Draw(key, Randomness.Mix(id), Ticks.Zero, PurposeTag.ShiftLength);

        return new Ticks((ulong)(uint)(first + (int)(value % (ulong)(uint)span)));
    }

    /// <summary>
    /// How far ahead of their Shift <paramref name="id"/> aims to arrive. Drawn once, in Ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The continuous term, and the profile is what asked for it.</b> The first cut of
    /// <c>adr/0101</c> had exactly one continuous quantity in the arithmetic — the commute a Citizen
    /// subtracts from their Shift start — and in a 4,000-Citizen city that is about <b>four
    /// minutes</b> against an hour of <b>85 Ticks</b>. So it could not smear an hourly quantisation
    /// into anything, and the measured morning came out as five near-equal bars: a <em>plateau</em>
    /// with holes either side rather than a peak.
    /// </para>
    /// <para>
    /// <b>It is per Citizen and not per Workplace, and that is the whole reason it broadens
    /// anything.</b> Workplaces genuinely do open on the hour — that texture is wanted and is not
    /// what was wrong — so the spread has to come from the people, as it does in life: some arrive
    /// with a quarter of an hour to spare and some cut it fine. A jitter on the Building would move
    /// the anchor without spreading the staff behind it.
    /// </para>
    /// <para>
    /// ⚠ <b>It does not fill the holes and is not meant to.</b> Nothing departs between 10:00 and
    /// 11:00 because no kind's band starts a Shift then; that is a property of the <em>band</em>, and
    /// widening or shaping it is a separate decision with separate evidence.
    /// </para>
    /// </remarks>
    public Ticks PunctualityOf(WorldKey key, ulong id)
    {
        if (ArriveEarlyMaxMinutes <= 0)
        {
            return Ticks.Zero;
        }

        int span = Ticks.AtMinute(ArriveEarlyMaxMinutes) + 1;
        ulong value = Randomness.Draw(
            key, Randomness.Mix(id), Ticks.Zero, PurposeTag.CommutePunctuality);

        return new Ticks(value % (ulong)(uint)span);
    }

    /// <summary>Whether the assignment pass runs at all.</summary>
    public bool Runs => Interval != 0;

    /// <summary>
    /// How many Citizens one pass considers, given <paramref name="citizens"/> of them.
    /// </summary>
    /// <remarks>
    /// <b>Over the whole population rather than over the unemployed, which is <c>adr/0059</c>'s own
    /// shape rather than <see cref="PlacementRuleset.SampleFor"/>'s.</b> A Zone Rule's revisit period
    /// is how long it takes to look at every <em>Lot</em>, not every vacant one, and this is the same
    /// choice for the same reason: there is no list of the unemployed to draw from, and maintaining
    /// one would be a derived collection whose only consumer is a denominator. A look that lands on
    /// somebody who already works is a look that found nothing, exactly as a look at an occupied Lot
    /// is — and the *considered* flow counts it, so the waste is reported rather than hidden.
    /// </remarks>
    /// <param name="citizens">How many Citizens the city holds.</param>
    public int SampleFor(int citizens)
    {
        if (RevisitTicks < Interval)
        {
            throw new InvalidOperationException(
                $"job assignment has a revisit period of {RevisitTicks} Ticks and an interval of "
                + $"{Interval}. The period is how long the pass takes to look at every Citizen once, "
                + "so it divides; the loader refuses anything below the interval, and this Ruleset "
                + "was not built by it.");
        }

        return (int)IntegerMath.CeilDiv((long)citizens * Interval, RevisitTicks);
    }
}

/// <summary>
/// The Ruleset the interpreter runs: ids and integers, validated, with no string anywhere in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Built by <c>Borough.Formats</c>, never read from disk here</b> (<c>adr/0048</c>). The parser
/// resolves every name to an id and runs every refusal, because a refusal's entire output is a
/// sentence a human reads and <c>adr/0002</c> forbids the core from producing one. What crosses the
/// boundary is integers and strings, and what survives the crossing is only the integers.
/// </para>
/// <para>
/// <b>The core's whole defence against drift is one cheap assertion</b> — <see cref="Rule"/> refuses
/// an id it does not know — and nothing beyond it. A second validator here would be the same rules
/// implemented twice, which is two things to keep in step rather than one.
/// </para>
/// <para>
/// <b>Not simulation state, and not folded into the State Hash.</b> What names a Ruleset in the hash
/// is its <em>content</em> hash, carried in the Input Log (<c>05 §7</c>), because two runs against
/// different Rules are two different simulations and the log is where that is recorded.
/// </para>
/// </remarks>
public sealed class Ruleset
{
    private readonly ResourceFamily[] _resources;
    private readonly RuleDefinition[] _rules;
    private readonly KindDefinition[] _kinds;
    private readonly Term[] _inputs;
    private readonly Term[] _outputs;
    private readonly MapEmission[] _emissions;
    private readonly BinDeclaration[] _bins;
    private readonly RuleId[] _kindRules;
    private readonly ZoneRuleDefinition[] _zoneRules;

    /// <param name="resources">One family per Resource, indexed by <c>ResourceId.Raw - 1</c>.</param>
    /// <param name="rules">One per Rule, indexed by <c>RuleId.Raw - 1</c>.</param>
    /// <param name="kinds">One per Building kind, indexed by <c>kind - 1</c>.</param>
    /// <param name="inputs">Every Rule's inputs, concatenated.</param>
    /// <param name="outputs">Every Rule's Bin outputs, concatenated.</param>
    /// <param name="emissions">Every Rule's Map Layer writes, concatenated.</param>
    /// <param name="bins">Every kind's Bin declarations, concatenated.</param>
    /// <param name="kindRules">Every kind's Rules, concatenated.</param>
    /// <param name="zoneRules">One per Zone Rule, in declaration order.</param>
    public Ruleset(
        ResourceFamily[] resources,
        RuleDefinition[] rules,
        KindDefinition[] kinds,
        Term[] inputs,
        Term[] outputs,
        MapEmission[] emissions,
        BinDeclaration[] bins,
        RuleId[] kindRules,
        ZoneRuleDefinition[] zoneRules)
    {
        ArgumentNullException.ThrowIfNull(zoneRules);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(kinds);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(outputs);
        ArgumentNullException.ThrowIfNull(emissions);
        ArgumentNullException.ThrowIfNull(bins);
        ArgumentNullException.ThrowIfNull(kindRules);
        ArgumentNullException.ThrowIfNull(resources);

        _resources = resources;
        _rules = rules;
        _kinds = kinds;
        _inputs = inputs;
        _outputs = outputs;
        _emissions = emissions;
        _bins = bins;
        _kindRules = kindRules;
        _zoneRules = zoneRules;
    }

    /// <summary>The Ruleset a world has before one is loaded. Declares nothing and runs nothing.</summary>
    public static Ruleset Empty { get; } = new([], [], [], [], [], [], [], [], []);

    /// <summary>
    /// Everything the Map Layers take from the Ruleset: the cadence, the rates, and the kernel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An init property rather than a positional parameter, following
    /// <see cref="KindDefinition.CondemnAfter"/>'s precedent</b>, and for the same reason:
    /// <see cref="LayerRuleset.Default"/> is what every Ruleset written before slice 8 ran on, so the
    /// default is the existing behaviour rather than a placeholder. A file with no <c>[layers]</c>
    /// table is a complete Ruleset.
    /// </para>
    /// <para>
    /// <b>It is what makes the cadence hot-reloadable at all.</b> Before this the schedule reached
    /// <c>World</c> only as a constructor argument and could therefore only be set once.
    /// <c>adr/0044</c> measured the cadence hash-bearing <em>and</em> ordinary tuning, and tuning that
    /// cannot be changed without recreating the world fails <c>adr/0015</c>'s acceptance test.
    /// </para>
    /// <para>
    /// <b>It carries a world-creation number as well</b> — <see cref="LayerConstants"/> — which is
    /// deliberate. The two categories travel in one file because a designer edits them in one sitting;
    /// keeping them in separate <em>types</em> is what lets the reload path apply one and refuse the
    /// other without either being a special case somebody has to remember.
    /// </para>
    /// </remarks>
    public LayerRuleset Layers { get; init; } = LayerRuleset.Default;

    /// <summary>
    /// How the Pool is drained into standing vacancy — <c>02 §5.2</c> step 2, <c>adr/0069</c>.
    /// </summary>
    /// <remarks>
    /// <b>An init property on <see cref="Layers"/>' precedent, and the default is the opposite one.</b>
    /// A Ruleset with no <c>[placement]</c> houses nobody, where a Ruleset with no <c>[layers]</c>
    /// diffuses on the standing schedule. The difference is that a Layer default is the behaviour every
    /// earlier Ruleset already had, and placement has no earlier behaviour to preserve — inventing one
    /// would put three hash-bearing numbers in the binary.
    /// </remarks>
    public PlacementRuleset Placement { get; init; } = PlacementRuleset.None;

    /// <summary>
    /// The shape and speed of the road network — <c>06</c> milestone 5a, <c>plans/0020</c>.
    /// </summary>
    /// <remarks>
    /// <b>An init property on <see cref="Placement"/>'s precedent, and it takes the same polarity for
    /// the same reason.</b> A Ruleset with no <c>[roads]</c> has no Road Graph, because there is no
    /// earlier behaviour to preserve and inventing one would put eight hash-bearing numbers in the
    /// binary that nobody authored. The difference from <c>[placement]</c> is only in how loudly the
    /// absence reads: a city that houses nobody grows a Pool, and a city with no roads cannot answer a
    /// catchment query at all.
    /// </remarks>
    public RoadRuleset Roads { get; init; } = RoadRuleset.None;

    /// <summary>
    /// <b>Where the generator lays Street lattices</b> — every <c>[[lattice]]</c> table, in
    /// declaration order.
    /// </summary>
    /// <remarks>
    /// <b>Empty means one Lattice at the origin corner</b>, which is the only world this build could
    /// generate until milestone 12 task 1, so no shipped Ruleset moves by this arriving. Order is
    /// content here, unlike <see cref="Hinterlands"/>: a Lattice is never looked up, it is
    /// <em>laid</em>, and consecutive ones are linked by a Street corridor — so swapping two tables
    /// swaps which pair the corridor joins and moves the State Hash.
    /// </remarks>
    public LatticeDefinition[] Lattices { get; init; } = [];

    /// <summary>
    /// The <c>[lots]</c> table in force — how zoned land is carved into parcels (<c>adr/0078</c>).
    /// </summary>
    public LotRuleset Lots { get; init; } = LotRuleset.None;

    /// <summary>
    /// The <c>[trips]</c> table in force — the crossing cost and the Commute Budget (5b-bis task 3).
    /// </summary>
    /// <remarks>
    /// <b>An init property on <see cref="Roads"/>' precedent, and it takes the same polarity.</b> A
    /// Ruleset with no <c>[trips]</c> has no Trip model, so the <c>trip</c> command and
    /// <c>--trips</c> refuse rather than costing a journey against numbers nobody authored.
    /// </remarks>
    public TripRuleset Trips { get; init; } = TripRuleset.None;

    /// <summary>
    /// <b>How a Citizen with no Workplace comes to have one</b> — the <c>[jobs]</c> table.
    /// </summary>
    /// <remarks>
    /// <see cref="JobRuleset.None"/> when the file states no <c>[jobs]</c>, and that is a city where
    /// nobody is ever assigned work rather than one where everybody is. <b>A <c>[jobs]</c> table is
    /// refused in a Ruleset with no <c>[trips] commute_budget_minutes</c></b>: the pass's search box
    /// is derived from the Budget, so without one it has no bound at all.
    /// </remarks>
    public JobRuleset Jobs { get; init; } = JobRuleset.None;

    /// <summary>
    /// <b>What a Household is, beyond where it lives</b> — the <c>[households]</c> table (5c task 5).
    /// </summary>
    /// <remarks>
    /// <see cref="HouseholdRuleset.None"/> when the file states no <c>[households]</c>, and that is a
    /// city where nobody keeps a car rather than one where everybody does — <see cref="Jobs"/>'
    /// polarity, for <see cref="Jobs"/>' reason.
    /// </remarks>
    public HouseholdRuleset Households { get; init; } = HouseholdRuleset.None;

    /// <summary>
    /// <b>How far a driver will walk from a Car Park</b> — the <c>[parking]</c> table (milestone 7
    /// task 2).
    /// </summary>
    /// <remarks>
    /// <see cref="ParkingRuleset.None"/> when the file states no <c>[parking]</c>, and that is a city
    /// with no Parking Shed rather than one whose sheds are empty — <see cref="Jobs"/>' polarity, for
    /// <see cref="Jobs"/>' reason. ⚠ <b>It is independent of <c>[[building]] parking</c></b>, which is
    /// the <em>supply</em>: a file may state Car Parks and no radius, which is a city whose parking
    /// exists and cannot be reached, and the two are separate keys because a designer retunes them for
    /// separate reasons.
    /// </remarks>
    public ParkingRuleset Parking { get; init; } = ParkingRuleset.None;

    /// <summary>
    /// <b>What a Segment costs to drive when other people are on it</b> — the <c>[traffic]</c> table
    /// (5c task 6).
    /// </summary>
    /// <remarks>
    /// <see cref="TrafficRuleset.None"/> when the file states no <c>[traffic]</c>, and that is a city
    /// whose roads never slow down — which is also the city every Ruleset described before this table
    /// existed, so omission preserves behaviour exactly.
    /// </remarks>
    public TrafficRuleset Traffic { get; init; } = TrafficRuleset.None;

    /// <summary>
    /// <b>The Policies in force</b> — every <c>[[policy]]</c> table, in declaration order
    /// (<c>plans/0033</c> task 5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An <c>init</c> property rather than a tenth constructor parameter, and the reason is not
    /// laziness.</b> This class already argues the split: a positional parameter is for something a
    /// Ruleset is <em>not complete without</em>, and an empty Policy set is a complete Ruleset — it is
    /// every Ruleset written before this task. A tenth parameter would also have touched 56
    /// construction sites, all but one of them fixtures, to say <c>[]</c>.
    /// </para>
    /// <para>
    /// <b>Declaration order is load-bearing</b>, as it is for <see cref="ZoneRules"/>: the index is a
    /// coordinate of the scan-start draw, so reordering <c>[[policy]]</c> tables changes which
    /// Households a Policy reaches first when its payer runs dry, and moves the State Hash. That is
    /// the Ruleset being content rather than configuration.
    /// </para>
    /// <para>
    /// <b>No id lookup</b>, for <see cref="ZoneRules"/>' reason: nothing in a Ruleset refers to a
    /// Policy, so giving it an id would invent a reference nothing holds. Its name exists in
    /// <c>Borough.Formats</c> for refusal messages and nowhere else. An array rather than a span
    /// because a span cannot be <c>init</c>, which is <see cref="ResourceKeys"/>' shape exactly.
    /// </para>
    /// </remarks>
    public PolicyDefinition[] Policies { get; init; } = [];

    /// <summary>
    /// <b>The Hinterlands behind the map's edges</b> — every <c>[[hinterland]]</c> table, at most one
    /// per edge (<c>adr/0131</c>, milestone 11 task 2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An <c>init</c> property on <see cref="Policies"/>' argument</b>: a positional parameter is
    /// for something a Ruleset is not complete without, and a Ruleset with no Outside is every
    /// Ruleset written before this milestone.
    /// </para>
    /// <para>
    /// ⚠ <b>Order is not content here, and that is the difference from <see cref="Policies"/>.</b>
    /// A Policy's index is a coordinate of its scan-start draw, so reordering <c>[[policy]]</c>
    /// tables moves the State Hash. Nothing draws on a Hinterland's index: it is reached through
    /// <see cref="TryHinterland"/> by the edge a gate stands on, and the loader refuses two tables
    /// for one edge — so the array is a lookup with four slots at most and its order is incidental.
    /// </para>
    /// <para>
    /// <b>Empty means the city has no Outside authored</b>, which is not the same as having no gate.
    /// A gate is a Building kind and a Hinterland is a market; a world can hold the first with the
    /// second missing, and what that costs is paid at arrival rather than at load.
    /// </para>
    /// </remarks>
    public HinterlandDefinition[] Hinterlands { get; init; } = [];

    /// <summary>
    /// The economy behind <paramref name="edge"/>, when one is authored.
    /// </summary>
    /// <remarks>
    /// <b>A walk rather than an index, because there are at most four</b> and
    /// <see cref="MapEdge"/> is not dense from zero — <see cref="MapEdge.None"/> occupies the first
    /// slot and is never a Hinterland's edge. A four-element linear scan is cheaper than the
    /// arithmetic that would avoid it, and it needs no invariant tying an array position to an enum
    /// value.
    /// </remarks>
    /// <param name="edge">The edge a gate stands on. <see cref="MapEdge.None"/> matches nothing.</param>
    /// <param name="hinterland">The market behind it, when there is one.</param>
    public bool TryHinterland(MapEdge edge, out HinterlandDefinition hinterland)
    {
        if (edge != MapEdge.None)
        {
            foreach (HinterlandDefinition candidate in Hinterlands)
            {
                if (candidate.Edge == edge)
                {
                    hinterland = candidate;
                    return true;
                }
            }
        }

        hinterland = default;
        return false;
    }

    /// <summary>
    /// What each Resource <em>is</em>, independent of the id this Ruleset filed it under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Ids are positional and a designer's edit moves them, which is the fact slice 8's degradation
    /// ran into and nothing in the corpus had noticed.</b> A <see cref="ResourceId"/> is declaration
    /// order — delete the first <c>[[resource]]</c> and every id below it shifts up one. A live Bin row
    /// holds that id, so the same row silently starts naming a different Good. <c>02 §4.3</c> describes
    /// the reload degradations as though ids were stable across two files, and nothing makes them so.
    /// </para>
    /// <para>
    /// <b>A key per declaration is what makes two Rulesets comparable at all.</b> It is the content
    /// hash of the declared name, computed by the loader — a number, so it crosses into the core under
    /// <c>adr/0048</c>'s own rule, where the name it was made from does not. The core never renders it
    /// and never resolves a name with it; it only asks whether two declarations are the same thing.
    /// </para>
    /// <para>
    /// <b>Empty means positional</b>, which is the behaviour of every Ruleset written in code. A
    /// fixture that declares two Resources and reloads to a fixture that declares two Resources means
    /// them to correspond, and there are no names in the tree to hash.
    /// </para>
    /// </remarks>
    public ulong[] ResourceKeys { get; init; } = [];

    /// <inheritdoc cref="ResourceKeys"/>
    /// <remarks><inheritdoc cref="ResourceKeys" path="/remarks"/></remarks>
    public ulong[] KindKeys { get; init; } = [];

    /// <summary>What the Resource with this id is, as a key comparable across Rulesets.</summary>
    public ulong ResourceKey(ResourceId resource) =>
        ResourceKeys.Length == 0 ? resource.Raw : ResourceKeys[resource.Raw - 1];

    /// <summary>What the Building kind with this id is, as a key comparable across Rulesets.</summary>
    public ulong KindKey(byte kind) => KindKeys.Length == 0 ? kind : KindKeys[kind - 1];

    /// <summary>This Ruleset with different Map Layer data, and everything else shared.</summary>
    /// <remarks>
    /// <para>
    /// <b>What <c>with</c> would give a record, spelled by hand because this is a class.</b> It exists
    /// for the two callers that legitimately hold Rules and Layer data separately: a world constructed
    /// from a stated cadence and no Rules — <c>adr/0044</c>'s measurement door — and a test that
    /// reloads a Ruleset differing only in cadence. The arrays are shared rather than copied because a
    /// loaded Ruleset is immutable.
    /// </para>
    /// <para>
    /// ⚠ <b>It copied three of the ten <c>init</c> properties and dropped the rest, and it had done so
    /// since the fourth one was added.</b> <c>Placement</c>, <c>Roads</c>, <c>Lots</c>, <c>Trips</c>,
    /// <c>Jobs</c>, <c>Households</c> and <c>Traffic</c> all came back at their defaults, so a Ruleset
    /// put through here lost its road geometry, its Commute Budget and its car ownership rate in
    /// silence. Found on milestone 10 task 5, by an eighth property needing to be threaded and the
    /// other seven not being there. ***Spelling <c>with</c> by hand makes adding a field a two-site
    /// edit, and the second site is the one nothing points at*** — <c>plans/0012</c> <b>Cause 1</b>
    /// where the second copy is not a document but an omission. <b>Every property added to this class
    /// belongs in this list.</b>
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>It happened again, and the paragraph above was already here when it did.</b>
    /// <see cref="Parking"/> was added by milestone 7 and never reached this list, so a Ruleset put
    /// through here came back with no Parking Shed at all — found on milestone 11 task 2 by a twelfth
    /// property needing to be threaded, which is the same way the first seven were found. ***A rule
    /// written in prose beside the code it governs is not a check on that code***, and two sightings
    /// one milestone apart is the evidence. <see cref="RulesetShape"/> is not the guard either:
    /// it compares <em>structure</em>, so a Ruleset losing its radius compares equal. The guard is
    /// <c>RulesetWithLayersTests</c>, which enumerates this class's properties and holds this list to
    /// them — <c>RefusalCountTests</c>' shape, one level in: code against code rather than a document
    /// against code.
    /// </para>
    /// </remarks>
    public Ruleset WithLayers(LayerRuleset layers) =>
        new(_resources, _rules, _kinds, _inputs, _outputs, _emissions, _bins, _kindRules, _zoneRules)
        {
            Layers = layers,
            Placement = Placement,
            Roads = Roads,
            Lattices = Lattices,
            Lots = Lots,
            Trips = Trips,
            Jobs = Jobs,
            Households = Households,
            Traffic = Traffic,
            Parking = Parking,
            Policies = Policies,
            Hinterlands = Hinterlands,
            ResourceKeys = ResourceKeys,
            KindKeys = KindKeys,
        };

    /// <summary>How many Resources are declared. Ids run <c>1..ResourceCount</c>.</summary>
    public int ResourceCount => _resources.Length;

    /// <summary>The family a Resource belongs to.</summary>
    /// <remarks>
    /// <b>Every Resource has one and the loader refuses a declaration without it</b>, so there is no
    /// <see cref="ResourceFamily.None"/> in a loaded Ruleset. Reaching it means a Ruleset was built in
    /// code rather than parsed.
    /// </remarks>
    public ResourceFamily Family(ResourceId resource) => _resources[resource.Raw - 1];

    /// <summary>Whether this Resource is conserved — never created or destroyed inside the city.</summary>
    /// <remarks>
    /// Money and nothing else (<c>adr/0024</c>). Flour legitimately becomes bread; a pound never
    /// becomes anything, it only changes hands.
    /// </remarks>
    public bool IsConserved(ResourceId resource) => Family(resource) == ResourceFamily.Money;

    /// <summary>How many Rules are declared. Ids run <c>1..RuleCount</c>.</summary>
    public int RuleCount => _rules.Length;

    /// <summary>How many Building kinds are declared. Ids run <c>1..KindCount</c>.</summary>
    public int KindCount => _kinds.Length;

    /// <summary>
    /// Every Zone Rule, in declaration order — which is the order a trigger evaluates them in.
    /// </summary>
    /// <remarks>
    /// <b>A span rather than an id lookup, because a Zone Rule is never referred to by name.</b>
    /// Nothing points at one: a Bin Rule is named by an <c>on_fail</c> and a kind is named by a Rule,
    /// but a Zone Rule is only ever iterated. So it needs no id type, and giving it one would invent
    /// a reference nothing holds.
    /// <para>
    /// Declaration order is <c>02 §4.2</c>'s tie-break between two Zone Rules contending for one Lot,
    /// and it is a Ruleset-authored order rather than an incidental one — which is why it is stable
    /// here and why the *scan start within a Rule* is rotated per trigger instead.
    /// </para>
    /// </remarks>
    public ReadOnlySpan<ZoneRuleDefinition> ZoneRules => _zoneRules;

    /// <summary>
    /// One Rule, or a throw. <b>This is <c>adr/0048</c>'s drift assertion, and it is the only one.</b>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">No Rule carries that id.</exception>
    public RuleDefinition Rule(RuleId id)
    {
        if (id.IsNone || id.Raw > _rules.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id.Raw,
                $"no Rule carries id {id.Raw}; this Ruleset declares {_rules.Length}. "
                + "adr/0048: the interpreter refuses an unknown id rather than trusting the table.");
        }

        return _rules[id.Raw - 1];
    }

    /// <summary>Whether this Ruleset declares a Building kind with that id.</summary>
    /// <remarks>
    /// <b>Asked rather than caught, and it exists so that <see cref="Kind"/> can keep throwing.</b>
    /// An undeclared kind reaching <see cref="BinsOf"/> is <c>adr/0048</c>'s two-sided drift check
    /// firing — the loader refuses an unknown <em>name</em> and the interpreter refuses an unknown
    /// <em>id</em> — and weakening it to an empty span would delete the check to serve one caller.
    /// <para>
    /// The one caller that legitimately needs to ask is Building creation, because a Building of an
    /// undeclared kind is a <em>representable situation the corpus already names</em>: <c>02 §4.3</c>
    /// says a reload marks Buildings whose kind no longer exists <b>derelict rather than deleted</b>.
    /// The commonest instance is the whole of Phase 1 so far — a world running on
    /// <see cref="Empty"/>, where no kind is declared and every Building is unfitted.
    /// </para>
    /// </remarks>
    public bool Declares(byte kind) => kind != 0 && kind <= _kinds.Length;

    /// <summary>One Building kind, or a throw.</summary>
    /// <exception cref="ArgumentOutOfRangeException">No kind carries that id.</exception>
    public KindDefinition Kind(byte kind)
    {
        if (kind == 0 || kind > _kinds.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                $"no Building kind carries id {kind}; this Ruleset declares {_kinds.Length}.");
        }

        return _kinds[kind - 1];
    }

    /// <summary>What the Rule spends, per application.</summary>
    public ReadOnlySpan<Term> Inputs(RuleId id) => Slice(_inputs, Rule(id).InputFirst, Rule(id).InputCount);

    /// <summary>What the Rule produces into Bins, per application.</summary>
    public ReadOnlySpan<Term> Outputs(RuleId id) =>
        Slice(_outputs, Rule(id).OutputFirst, Rule(id).OutputCount);

    /// <summary>What the Rule writes to Map Layers, per application. Cannot fail.</summary>
    public ReadOnlySpan<MapEmission> Emissions(RuleId id) =>
        Slice(_emissions, Rule(id).EmissionFirst, Rule(id).EmissionCount);

    /// <summary>The Bins a Building of this kind is given when it is built.</summary>
    public ReadOnlySpan<BinDeclaration> BinsOf(byte kind) =>
        Slice(_bins, Kind(kind).BinFirst, Kind(kind).BinCount);

    /// <summary>The Rules a Building of this kind runs.</summary>
    public ReadOnlySpan<RuleId> RulesOf(byte kind) =>
        Slice(_kindRules, Kind(kind).RuleFirst, Kind(kind).RuleCount);

    private static ReadOnlySpan<T> Slice<T>(T[] all, int first, int count) =>
        all.AsSpan(first, count);
}
