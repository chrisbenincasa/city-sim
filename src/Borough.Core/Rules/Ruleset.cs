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

/// <summary>
/// Whose level a Bin holds. <b>The premises declare every Bin's capacity either way</b>
/// (<c>adr/0141</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>It is a property of the Bin and not of the Building</b>, which is what makes one kind able to
/// declare both: <c>rulesets/minimal.toml</c>'s dwelling keeps <c>repairs</c> — the roof — and hands
/// <c>sundries</c> to whoever lives there. The test is <c>CONTEXT.md</c> → Building's, stated as a
/// question about leaving: ***does the Bin empty when the tenant goes?*** Flour goes with the baker;
/// the roof does not.
/// </para>
/// <para>
/// ⚠ <b>Capacity does not move with it</b> (<c>adr/0141</c>, and it is the half found by trying to
/// route around it). <see cref="Borough.Core.Entities.World.CreateBin"/> reads its ceiling from the
/// <em>building</em> kind at the creation site, and neither a Household nor a Business has a kind
/// byte to read one from. ***A shop holds what fits in the shop, and what is in it is the
/// shopkeeper's.***
/// </para>
/// <para>
/// <b><see cref="Premises"/> is zero so that absence is the shipped behaviour.</b> Every Bin in
/// every Ruleset written before this existed belongs to its Building, and a defaulted enum saying so
/// is the difference between a migration and a rewrite.
/// </para>
/// </remarks>
public enum BinTenancy : byte
{
    /// <summary>The Building's own. It stays when the tenant leaves.</summary>
    Premises = 0,

    /// <summary>The Occupant's. <b>It leaves with them</b>, which is the whole definition.</summary>
    Occupant = 1,

    /// <summary>
    /// The Business tenanting the premises'. <b>A second Occupant and not a second kind of
    /// premises</b> (<c>adr/0147</c>, <c>adr/0166</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Occupant"/> could not simply be widened to cover it, and
    /// <c>rulesets/minimal.toml</c> is why.</b> That file's <c>dwelling</c> kind declares an occupant
    /// Bin, two occupant Rules <em>and</em> <c>business = "shop"</c>, so under <c>adr/0148</c> one
    /// kind holds a Household occupant and a Business occupant in the same Building, taking two of
    /// the same four <c>occupants</c> slots. ***A two-valued tenancy would have given every
    /// instantiated shop a larder and a <c>consume</c>***, on every shipped world, silently.
    /// </para>
    /// <para>
    /// ⚠ <b>A money Resource may be declared with this owner and no <c>capacity</c>, and such a
    /// declaration opens no Bin</b> — <c>World.OpenBalance</c> still does, at <c>long.MaxValue</c>.
    /// It exists so the tenancy derivation has something to look up for a term addressing money,
    /// which is <c>adr/0166</c>'s whole decline half: a money Bin is never declared, so
    /// <c>RulesetLoader.ApplyTenancies</c> found no declaration and fell through to
    /// <see cref="Premises"/> — ***deriving the bankruptcy Rule to the landlord***, silently rather
    /// than by refusal. <b>The alternative was an <c>owner</c> key on a <c>[[rule]]</c>, which
    /// <c>adr/0141</c> refuses by name and <c>rulesets/taxed.toml</c>'s header quotes.</b> ***The
    /// oddity of a declaration that allocates nothing is the price of leaving that ADR alone***, and
    /// it is <c>plans/0044</c> open decision 4, settled rather than stumbled into.
    /// </para>
    /// </remarks>
    Business = 2,
}

/// <summary>
/// One Bin a Building kind is given when it is built: which Resource, its capacity, and whose level
/// it is.
/// </summary>
/// <remarks>
/// <b>The kind declares an Occupant's Bin as well as its own, and that is not a contradiction.</b>
/// <c>adr/0141</c> splits <em>who declares the ceiling</em> from <em>who holds the level</em>, and
/// this type is the first place both appear together: the premises answer the first for every Bin on
/// the Lot, and <see cref="Tenancy"/> answers the second.
/// </remarks>
public readonly record struct BinDeclaration(
    ResourceId Resource, BinCapacity Capacity, BinTenancy Tenancy = BinTenancy.Premises);

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

    /// <summary>
    /// Whose Rule this is: the premises', the Household Occupant's, or the Business Occupant's.
    /// <b>Derived from the Rule's own <see cref="Scope.Local"/> terms, never authored</b>
    /// (<c>adr/0141</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Derived rather than authored because the Bins already answer it.</b> <c>adr/0141</c>
    /// declined to split Rules from their Bins — *"a Rule whose Bins all belong to a tenant is a
    /// tenant's Rule wearing the premises' name"* — so an <c>owner</c> key on a <c>[[rule]]</c> would
    /// be a second, authorable statement of a fact the terms already make, and the failure mode of
    /// two statements of one fact is that they disagree. <b>The loader computes it and refuses a Rule
    /// whose local terms disagree</b>, which is <c>adr/0050</c> at the parse site: a term crossing an
    /// ownership boundary is a <em>trade</em>, and a trade is <see cref="Scope.Pool"/> rather than a
    /// second <see cref="Scope.Local"/>.
    /// </para>
    /// <para>
    /// <b>A Rule with no local term at all is the premises'.</b> Nothing about it leaves with a
    /// tenant, and the alternative — refusing it — would refuse a Rule that only emits into a Map
    /// Layer, which is a coherent thing for a building to do.
    /// </para>
    /// <para>
    /// <b>An init property rather than a positional parameter</b>, on
    /// <see cref="KindDefinition.CondemnAfterTicks"/>'s precedent and for its reason: the default is the
    /// behaviour of every Ruleset written before this existed, and the forty-odd construction sites
    /// in the test suite are all stating it.
    /// </para>
    /// </remarks>
    public BinTenancy Tenancy { get; init; }
}

/// <summary>One Building kind: the Bins it is given, the Rules it runs, and when it falls down.</summary>
public readonly record struct KindDefinition(
    int BinFirst, int BinCount, int RuleFirst, int RuleCount)
{
    /// <summary>
    /// How many TICKS a Rule of this kind may starve continuously before the premises are condemned
    /// and the Building is abandoned. Zero means it never is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A DURATION, AND IT WAS A COUNT OF MISSED FIRINGS UNTIL MILESTONE 17.</b>
    /// <c>adr/0053</c> chose the firing count so that a Ruleset retuning every <c>rate</c> would not
    /// silently retune every Building's lifespan. That reasoning is sound and it solved the wrong
    /// half: it protects the number from a cadence edit and leaves the <em>designer</em> unable to
    /// see what the number means. <c>condemn_after = 4</c> against <c>upkeep</c>'s rate of 16 is 64
    /// Ticks — <b>45 in-world minutes</b> — and it stood in all eighteen shipped Rulesets without
    /// one author ever writing a different value, because nothing on the page said it was
    /// three quarters of an hour.
    /// </para>
    /// <para>
    /// ⚠ <b>THE RULESET AUTHORS DAYS AND THIS FIELD HOLDS TICKS.</b> The designer-facing key is
    /// <c>condemn_after_days</c> and <c>RulesetLoader</c> multiplies it up, which is
    /// <c>adr/0048</c>'s division doing exactly what it is for — <i>the parse site is where a
    /// Ruleset is validated, and only integers cross into the core</i>. A Day is the coarsest unit
    /// that makes the mistake above unwritable on the page: the shortest decline a Ruleset can
    /// author is 2,048 Ticks, and 45 minutes is not expressible at all. <b>A fixture built in code
    /// may still hold any Tick count</b>, and most of them do, because a test that had to run a
    /// whole in-world Day to watch one Building fall down would be a four-minute assertion.
    /// </para>
    /// <para>
    /// <b>The PREMISES only</b> (<c>adr/0141</c>). This threshold judges the Rules the premises own —
    /// <c>upkeep</c> and its kin — and the tenants are judged separately against
    /// <see cref="TenancyEndsAfterTicks"/>. ⚠ <b>ONE key drove both verdicts until milestone 17</b>,
    /// so stripping decline from a world also stopped every tenancy in it from ever ending, which is
    /// how <c>rulesets/evicted.toml</c> — the one file whose whole purpose is a tenancy that ends —
    /// silently stopped demonstrating it.
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
    /// <para>
    /// <b>Hash-bearing and UNRATIFIED</b>, held in <c>plans/0002</c> §D1.
    /// </para>
    /// </remarks>
    public int CondemnAfterTicks { get; init; }

    /// <summary>
    /// How many TICKS a tenant's own Rule may starve continuously before the tenancy ends and the
    /// Occupant is unplaced. Zero means a tenancy of this kind never ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The tenant half of <c>adr/0141</c>'s split, and it is a separate number because it is a
    /// separate question.</b> The ADR split the <em>verdict</em> — a tenant's Failure Pressure ends
    /// the tenancy, the premises' condemns the Building — and stopped at the threshold, leaving
    /// <see cref="CondemnAfterTicks"/> answering both. ⚠ <b>That is not a tidiness defect</b>: it
    /// means a world cannot demonstrate a failing tenant without also demolishing its housing stock,
    /// and it cannot demonstrate decline without also evicting every tenant, so the two mechanisms
    /// could never be shown or tested apart.
    /// </para>
    /// <para>
    /// <b>On the PREMISES kind, and that is deliberate rather than convenient.</b> The obvious home
    /// is the tenant's own kind — but a Household has no kind byte at all (<c>adr/0141</c> found the
    /// same thing about Bin capacity and reached the same answer), and a Business does. A threshold
    /// that existed for one kind of tenant and not the other would be unaskable in
    /// <c>ZoneRuleEngine.Condemn</c>'s occupant walk, which does not know which it is holding.
    /// <b>So it is a property of the lease rather than of the tenant</b>, which is
    /// <c>adr/0141</c>'s own <i>the premises own the capacity</i> reaching the one thing left
    /// unassigned.
    /// </para>
    /// <para>
    /// <b>Authored as <c>tenancy_ends_after_days</c> and held in Ticks</b>, for
    /// <see cref="CondemnAfterTicks"/>'s reason, and hash-bearing and
    /// <b>UNRATIFIED</b>, held in <c>plans/0002</c> §D1.
    /// </para>
    /// </remarks>
    public int TenancyEndsAfterTicks { get; init; }

    /// <summary>
    /// How many TICKS the premises may starve continuously before the Building sheds one Occupant.
    /// Zero means it sheds none, and it must be shorter than <see cref="CondemnAfterTicks"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>CONTEXT.md</c> → Failure Pressure's FIRST threshold, and it is the one that makes
    /// decline reversible by the city.</b> <i>"Past a threshold it loses occupancy and quality; past a
    /// further one it is abandoned."</i> The further one is <see cref="CondemnAfterTicks"/> and has
    /// existed since slice 10; this is the rung below it. ⚠ <b>Quality is deliberately not here</b> —
    /// it has no column, no term and no definition, so inventing one would put a hash-bearing number
    /// under a word no ADR defines (<c>plans/0045</c> decision 2).
    /// </para>
    /// <para>
    /// <b>What it buys is a NEGATIVE FEEDBACK LOOP, which is the only one in the build.</b> A premises
    /// Rule's demand scales with occupancy — <c>upkeep</c>'s <c>apply</c> is
    /// <c>{ derived = "occupancy" }</c> — so shedding an Occupant <em>lowers the demand that caused
    /// the shedding</em>. A Building that would have been abandoned instead thins out until it can
    /// cope, and at zero Occupants a derived Rule bands to <c>(0,0)</c> and fires with zero
    /// applications, which clears <c>StarvedSince</c> outright. ***So the correction terminates, and
    /// it terminates at a Building placement can refill rather than at a ruin.***
    /// </para>
    /// <para>
    /// 🔴 <b>THE PACING IS STATELESS AND THAT IS THE DESIGN, not a saving.</b> The target occupancy is
    /// <c>declared − elapsed / this</c>, so one Occupant goes at each multiple of the threshold and the
    /// count is a pure function of how long the premises have been failing. **The obvious spelling —
    /// shed one and reset the clock — was refused**: it also resets progress toward
    /// <see cref="CondemnAfterTicks"/>, so a kind stating both would shed for ever and never be
    /// condemned, and the second threshold would become dead code in every world that used the first.
    /// ⚠ <b>A column recording the last shed was refused as state pointed at by nothing</b> — it
    /// would be live state pointed at by nothing, where the same fact is already derivable from
    /// <c>StarvedSince</c>.
    /// </para>
    /// <para>
    /// <b>Shorter than <see cref="CondemnAfterTicks"/>, and the loader enforces it</b>
    /// (<c>adr/0048</c>): a first threshold at or past the second never fires, because the premises
    /// verdict is taken first and abandonment empties the Building. ***A key that can be authored into
    /// inertness is a key that will be***, which is <c>adr/0130</c>'s reason for refusing a stated
    /// zero arriving as a relation between two numbers rather than a range on one.
    /// </para>
    /// <para>
    /// <b>Authored as <c>sheds_occupant_after_days</c> and held in Ticks</b>, for
    /// <see cref="CondemnAfterTicks"/>'s reason, and hash-bearing and <b>UNRATIFIED</b>, held in
    /// <c>plans/0002</c> §D1.
    /// </para>
    /// </remarks>
    public int ShedsOccupantAfterTicks { get; init; }

    /// <summary>
    /// How many Days an abandoned Building of this kind stands before it collapses and its Lot
    /// returns to vacant.
    /// </summary>
    /// <remarks>
    /// <b>The sink for abandoned stock, and it is REQUIRED of any kind that can be abandoned and
    /// refused of any kind that cannot</b> — exactly <c>adr/0130</c>'s disposition for
    /// <c>gives_up_after_days</c>, and for the same reason: <i>a Pool with an inflow and no sink is
    /// <c>adr/0006</c></i>. A kind with a <see cref="CondemnAfterTicks"/> collects shells, so a kind with
    /// one and no collapse duration is an unbounded collection a Ruleset can author, and the loader
    /// refuses it rather than letting a city discover it at Tick 100,000.
    /// <para>
    /// 🔴 <b>MEASURED, not reasoned.</b> Milestone 17 task 1 made abandonment leave the shell
    /// standing with no sink at all, and the result was not a slow leak — <b>the city converts
    /// entirely to shells</b>: zero jobs, a land value field peaking at zero, and a divide-by-zero in
    /// the placement pass, across 19 tests in eight subsystems. ***A player-only sink cannot satisfy
    /// a Definition of done that requires a steady state at 100,000 Ticks with no player present.***
    /// </para>
    /// <para>
    /// <b>A duration in Days rather than a count of sweeps</b> (<c>adr/0059</c>, <c>adr/0130</c>):
    /// authoring the count would make the felt quantity move whenever a cadence was retuned. It is on
    /// the <em>kind</em> for <see cref="CondemnAfterTicks"/>'s reason — a concrete tower and a timber shed
    /// do not stand the same length of time — and it is <b>hash-bearing and UNRATIFIED</b>, held in
    /// <c>plans/0002</c> §D1.
    /// </para>
    /// </remarks>
    public int CollapsesAfterDays { get; init; }

    /// <summary>
    /// How many Ticks a Building of this kind may house <b>nobody</b> before the city abandons it.
    /// Zero means it stands empty for ever.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The stock's demand-side sink, and it is not a lifespan.</b>
    /// <see cref="CondemnAfterTicks"/> reads Failure Pressure, so a kind stating it declines whether
    /// anybody wants it or not; this reads occupancy, so only <em>surplus</em> stock dies. It is the
    /// mirror of <c>adr/0069</c>'s build predicate — a developer builds while the Unplaced Pool is
    /// non-empty, and gives up on a Building the Pool never came for — and it is what <c>02 §5.5</c>
    /// calls redevelopment's floor, <i>the case where nobody wants the land</i>.
    /// </para>
    /// <para>
    /// 🔴 <b>The world it was built for is <c>rulesets/aged.toml</c>, where the finding was not a
    /// leak.</b> A demographic city loses two thirds of its Households over 400 Days and keeps every
    /// one of its houses: <c>adr/0006</c> is green throughout, because the stock is bounded by peak
    /// demand and converges. ***What is unbounded is the RATIO whose denominator is free to fall*** —
    /// <c>posts per Citizen</c>, which is what <c>[[building]] jobs</c> was derived against. A
    /// monotone numerator over a falling denominator trends nowhere a collection check is looking.
    /// </para>
    /// <para>
    /// <b>It ABANDONS rather than demolishing, and that is <c>adr/0091</c> untouched.</b> Clearing
    /// land is bought rather than taken; nothing here razes anything. The Building becomes a shell on
    /// its Lot exactly as a condemned one does, and <see cref="CollapsesAfterDays"/> is the sink under
    /// both — which is why the loader requires that key alongside this one.
    /// </para>
    /// <para>
    /// ⚠ <b>It counts Households and not tenants</b> — see
    /// <see cref="Entities.BuildingTable.EmptySince"/>, which carries the reason and the trap.
    /// </para>
    /// <para>
    /// <b>Ticks here and Days in the file</b>, on <see cref="CondemnAfterTicks"/>'s precedent, and
    /// hash-bearing and <b>PROVISIONAL</b> — chosen by taste under <c>plans/0045</c> standing order 4,
    /// naming no ratifier.
    /// </para>
    /// </remarks>
    public int AbandonedWhenEmptyAfterTicks { get; init; }

    /// <summary>
    /// Whether a <b>Household</b> may take a tenancy in a Building of this kind.
    /// <b>Whether, never how many.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS REPLACED <c>occupants</c>, AND THE TWO ARE NOT THE SAME QUESTION ASKED DIFFERENTLY.</b>
    /// A count on the kind made every dwelling in the world hold the same number whatever ground it
    /// stood on; how many a Building holds is <b>derived</b> now, from its floor area over
    /// <c>[capacity] floor_tiles_per_occupant</c> (<c>plans/0053</c> step 3). ***The kind says what
    /// it does and the ground says how much of it.*** <c>adr/0068</c> carries a banner saying which
    /// half of its title went.
    /// </para>
    /// <para>
    /// <b><c>false</c> and an absent key are the same city, which is what a truth key buys.</b> A
    /// factory houses nobody and wants no ceremony for it. ⚠ <b>The failure this could hide is a
    /// dwelling kind whose author forgot the line, and it surfaces immediately and loudly</b>:
    /// nobody can move in and the Unplaced Pool stops draining — the opposite of the silent
    /// narrowing <c>02 §4.3</c>'s <c>apply</c> defect was.
    /// </para>
    /// <para>
    /// <b>A kind the Ruleset does not declare at all is a different thing from one declaring
    /// <c>false</c></b> — see <see cref="Entities.World.TryDeclaredOccupancy"/>. Dereliction must not
    /// evict, so a derelict kind keeps the Occupants it has and admits nobody new, where a kind
    /// stating <c>houses = false</c> has a real ceiling of zero.
    /// </para>
    /// <para>
    /// 🔴 <b>THIS WAS <c>tenanted</c>, AND IT MEANT BOTH KINDS OF TENANT AT ONCE.</b>
    /// <c>plans/0054</c> F1: <c>adr/0147</c> gives a Building one ceiling that a Household and a
    /// Business compete for, which is right — a shop on the ground floor takes a flat's worth of
    /// premises — but the <em>permission</em> was one boolean, so ***a kind that wanted a trade had
    /// to declare itself housing, and then families moved into the warehouse.*** An office, a
    /// supermarket and a depot were all unwritable. The ceiling is unchanged and undivided; what
    /// split is who may claim from it. See <see cref="Premises"/>.
    /// </para>
    /// </remarks>
    public bool Houses { get; init; }

    /// <summary>
    /// Whether a <b>Business</b> may take a tenancy in a Building of this kind.
    /// <b>Whether, never how many.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Houses"/>' other half, over the same ceiling</b> (<c>adr/0147</c>). A kind
    /// declaring both is mixed use — the flats above the shop — and one declaring neither is a
    /// Building nobody occupies, which a warehouse and a monument both are. ***The two are
    /// permissions and not capacities***: neither adds a tenancy, and a Building's tenancies are its
    /// floor area over <c>[capacity] floor_tiles_per_occupant</c> whichever of them is set.
    /// </para>
    /// <para>
    /// <b>It governs BOTH ways a trade arrives.</b> <see cref="Business"/> instantiates one at
    /// construction (<c>adr/0148</c>) and <c>PlacementEngine</c> premises one out of the unpremised
    /// pool (<c>adr/0147</c>), and a kind that admits neither admits both of nothing. ⚠ <b>A kind
    /// declaring <see cref="Business"/> and not this is REFUSED at load</b> rather than defaulted
    /// true: the trade would arrive with nowhere to sit, and a key that turned itself on could not
    /// be told from one the author meant.
    /// </para>
    /// <para>
    /// ⚠ <b>The word is the vocabulary's</b> — <c>CONTEXT.md</c> → Premises, where a Household is
    /// <em>housed</em> and a Business is <em>premised</em>, and <c>World.Premise</c> is already the
    /// method that does it. ***Two verbs the design already had, arriving as the two keys it was
    /// missing.***
    /// </para>
    /// </remarks>
    public bool Premises { get; init; }

    /// <summary>
    /// The Business kind a Building of this kind comes with, or <c>0</c> for none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0148</c>: a premises kind may declare its trade, and construction instantiates it.</b>
    /// A Zone Rule raising a Building of this kind creates a <see cref="Entities.Business"/> of this
    /// trade, already premised, taking one of the Building's tenancies — <see cref="Premises"/> must
    /// admit it, and there are as many as its floor area divides into. ***It is drawn from no
    /// pool***, which is why this does not reach
    /// <c>adr/0069</c>'s <em>construction houses nobody</em>: nothing is taken out of the Unplaced Pool
    /// and nothing out of the unpremised one, so no demand signal is drained.
    /// </para>
    /// <para>
    /// <b>It is IDENTITY rather than tuning, and <see cref="RulesetShape"/> compares it</b> — alone
    /// among this type's members. Repointing a kind at a different trade on reload would leave every
    /// standing Building of it holding a shop of the wrong trade, which is the Bins-and-Rules case
    /// exactly (<c>02 §4.3</c>), so a reload that moves it is refused rather than migrated.
    /// </para>
    /// <para>
    /// ⚠ <b>The Business it makes carries no flag.</b> A kind-declared trade and a founded one differ
    /// in how they arrive and in nothing afterwards — condemn its premises and it lands in the
    /// unpremised pool with <c>adr/0144</c>'s empty balance like any other. <b>It has no founder</b>,
    /// because <c>adr/0146</c> governs founding and nobody founded this.
    /// </para>
    /// </remarks>
    public byte Business { get; init; }

    /// <summary>
    /// Whether a Building of this kind parks Vehicles at all. <b>Whether, and never how many.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Houses"/>' shape on the second axis</b> (<c>plans/0053</c>): capacity is
    /// geometry and behaviour is content, so the count divides floor area — <c>[capacity]
    /// floor_tiles_per_parking_space</c> — and what a kind knows, which no arithmetic could derive,
    /// is whether it carries parking at all.
    /// </para>
    /// <para>
    /// 🔴 <b>This key exists because a city-wide rate alone could not express <c>adr/0009</c>'s own
    /// second player-tool row</b> — <em>a detached house carries a driveway, a tower may not</em>.
    /// A rate with no predicate beside it gives every Building parking in proportion to its floor,
    /// which makes the tower the biggest provider in the city and says the reverse of what that ADR
    /// wants sayable. ***A parking minimum is a property of the city; an exemption from it is a
    /// property of the kind.***
    /// </para>
    /// </remarks>
    public bool Parked { get; init; }


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
    /// Which Need a Building of this kind is attended for; <b><see cref="Need.None"/> means it is
    /// not a service Building</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="ArrivalsPerDay"/>'s shape exactly, and taken deliberately rather than by
    /// analogy.</b> A gate is a <c>[[building]]</c> kind that states one extra key, and stating it is
    /// what makes the kind a gate — there is no separate <c>[[gate]]</c> table and no
    /// <c>is_gate = true</c> to disagree with the key. A school is the same: <c>serves</c> is both the
    /// declaration and the content, so a kind cannot be a service and decline to say what for.
    /// </para>
    /// <para>
    /// 🔴 <b>ONLY <c>adr/0032</c>'s ATTENDED services live here, and the other two modes are not
    /// missing.</b> That record sorts services by <em>who moves</em>: Attended (the Household
    /// travels — education, health, recreation), Dispatched (the Service travels — fire, police) and
    /// Networked (nobody moves — power, water, sewage). ***Only the Attended mode is a Need at all***
    /// — a Dispatched service is answered by <c>adr/0030</c>'s dispatch Trip and a Networked one by
    /// flow over the District graph, and neither is a scalar a Household carries. A key here naming
    /// <c>fire</c> would be filing a Service under the wrong mode, so
    /// <c>RulesetLoader</c> refuses it by name.
    /// </para>
    /// <para>
    /// ⚠ <b>There is no catchment key, and its absence is the decision.</b> <c>01 §2</c> describes
    /// this verb as <em>"place a Building with a catchment"</em> and <c>adr/0032</c> demoted the
    /// catchment from <b>mechanism</b> to <b>overlay</b>: coverage is composed from the same
    /// reachability the Trips use, so it is <em>derived</em> and authoring it would be authoring an
    /// answer the Road Graph already has. The single case that decided it is worth keeping in view —
    /// ***a school across an uncrossable Arterial is 200 m away and unreachable***, which a distance
    /// field calls excellent and a Trip calls impossible.
    /// </para>
    /// <para>
    /// ⚠ <b>A service kind is still an ordinary Building in every other respect.</b> It stands on a
    /// Lot, seals its footprint, may declare <see cref="Houses"/> or <see cref="Premises"/>, and can
    /// be condemned. What <c>adr/0026</c> adds and this build does not have is that its staffing is
    /// <em>demand-determined by catchment</em> — ***you cannot fix unemployment by hiring everyone as
    /// a teacher, because the number of teachers is set by the number of children*** — and that is
    /// <c>adr/0070</c> <em>unbuilt</em>: a school here employs whatever its floor area divides into,
    /// like any other kind.
    /// </para>
    /// </remarks>
    public Need Serves { get; init; }

    /// <summary>Whether a Building of this kind is attended for some Need.</summary>
    public bool IsService => Serves != Need.None;
}

/// <summary>
/// What a <c>[[business]]</c> trade declares: how many Citizens it employs, and the band its Shifts
/// start in.
/// </summary>
/// <remarks>
/// <para>
/// <b>The second kind namespace grows a definition</b> (<c>adr/0141</c>, milestone 27 task 7). Task 6
/// shipped <c>[[business]]</c> as names only, because a trade that declares nothing but its name buys
/// <em>identity</em> — a Business row can name its trade and keep that name across a reload — and a
/// read pass with no keys to read would be a walk over nothing. This is what it reads once there are
/// keys.
/// </para>
/// <para>
/// ⚠ <b>Two of <c>adr/0141</c>'s three, and the third is not this milestone's.</b> That ADR's
/// <em>Declares</em> row gives the trade <c>jobs</c>, shift hours <em>and the wage</em>. The wage
/// arrives with <c>adr/0026</c> at milestone 15 — <c>docs/06</c> places it there and
/// <see cref="Readouts"/> says the same — so there is no wage key here and a Ruleset stating one is
/// refused as unknown. <b>A doc comment in <c>RulesetShape</c> claimed all three arrived together and
/// was wrong about one</b> (<c>plans/0012</c>, Cause 4).
/// </para>
/// <para>
/// <b>Every member is <em>tuning</em>, which is why no shape check compares them.</b>
/// <see cref="RulesetShape"/> compares a Building kind's identity, its Bins and its Rules and
/// <b>does not compare a Building kind's occupancy or parking</b> — a ceiling is read at a write site and
/// pointed at by no live state, so lowering it reaches every Building already standing and dismisses
/// the overflow (<c>adr/0068</c>, <c>adr/0064</c>). ***The same is true here member for member***, so
/// a reload that retunes a trade needs no migration and produces no
/// <see cref="RulesetChange"/>. ⚠ <b>Hash-bearing all the same</b>: retuning moves the standing city.
/// </para>
/// <para>
/// ⚠ <b>This is where employment lives now, and the Building kind states none of it.</b> A Workplace
/// is a <see cref="Entities.Business"/> handle as of milestone 27 task 7, and
/// <c>[[building]] jobs</c> with its Shift band is <b>refused at load</b> rather than ignored
/// (<c>adr/0148</c>). ***A Building employs nobody; the trade tenanting it does.***
/// </para>
/// </remarks>
public readonly record struct BusinessKindDefinition
{
    /// <summary>
    /// The earliest in-world hour a job of this trade starts at. Paired with
    /// <see cref="ShiftStartLatestHour"/>.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0101</c>'s Shift band, on the trade rather than the premises</b>, which is that ADR's
    /// own word arriving where it pointed: a Shift start hour belongs to the <em>Workplace</em>, and a
    /// Workplace is where you are employed. ⚠ <b>A trade with no hours is half a mechanism</b>, and the
    /// defaulted <c>0</c> is refused rather than defaulted because <b>midnight is a real answer</b>
    /// and would be a placeholder that could not announce itself.
    /// </remarks>
    public int ShiftStartEarliestHour { get; init; }

    /// <summary>
    /// The latest in-world hour a job of this trade starts at. Equal bounds mean a trade whose Shifts
    /// all start together.
    /// </summary>
    public int ShiftStartLatestHour { get; init; }

    /// <summary>
    /// What one job of this trade pays a Citizen for one Day worked, in the smallest money unit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The third of <c>adr/0141</c>'s three, and the corpus said it would arrive at milestone 15.</b>
    /// It arrives here instead, under <c>plans/0045</c>'s amnesty, because the money loop had exactly
    /// one direction: a Household could be taxed and a Business could be levied, and nothing anywhere
    /// paid anybody. ⚠ <b>PROVISIONAL</b> — chosen by taste rather than ratified, with no <c>§D</c>
    /// row and no ratifier, which is what the amnesty suspends <c>adr/0052</c> in order to allow.
    /// </para>
    /// <para>
    /// 🔴 <b>A DAILY RATE AND NOT WHAT LANDS ON PAYDAY, and the distinction is the whole reason
    /// <see cref="PayPeriodDays"/> is worth having.</b> Payday moves this multiplied by the Days
    /// actually worked, so moving a trade from daily to weekly pay changes <em>when</em> its workers
    /// are paid and never <em>how much</em> — the rhythm becomes a variable you can turn on its own.
    /// ***Declaring the lump instead would make every daily-against-weekly comparison a comparison of
    /// two different incomes***, and no reading off it could be attributed to the period.
    /// </para>
    /// <para>
    /// ⚠ <b>Optional, and zero means a trade that pays nothing</b> rather than a trade that has
    /// forgotten to say. That is the honest default for the nine shipped files that predate wages: a
    /// world whose trades all pay zero behaves exactly as it did before this key existed.
    /// </para>
    /// </remarks>
    public int WagePerDay { get; init; }

    /// <summary>
    /// How many Days pass between paydays for this trade. <c>1</c> is paid daily.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A property of the trade, the way its Shift band is</b> — a grocer may pay weekly while a
    /// shop pays daily, and that is the sort of difference a second kind table exists to express
    /// (<c>adr/0141</c>). Nothing about it is global, and it introduces no new unit of time:
    /// <c>7</c> is a week because seven Days is a week, and <c>CONTEXT.md</c> needs no new noun.
    /// </para>
    /// <para>
    /// ⚠ <b>Paydays are STAGGERED across Businesses and the offset is derived, not stored.</b> Every
    /// weekly trade paying on the same Day would put the city's whole payroll on one Tick — a cost
    /// spike, and a city where nobody is ever paid on a Tuesday. The offset is
    /// <c>hash(world_seed, business id, <see cref="Determinism.PurposeTag.WagePayday"/>)</c> taken
    /// against this, so it needs no column, survives a reload for free, and is stable for the life of
    /// the Business because a row id is monotonic and never reused.
    /// </para>
    /// <para>
    /// ⚠ <b>Refused at zero and refused negative.</b> A period of zero is a division by it, and a
    /// negative one is not a sentence anybody meant to write. <b>Required wherever
    /// <see cref="WagePerDay"/> is stated and refused without it</b>, in both directions: a rate with
    /// no period never pays and a period with no rate pays nothing, and each is half a mechanism.
    /// </para>
    /// </remarks>
    public int PayPeriodDays { get; init; }
}


/// <summary>
/// One of <c>adr/0011</c>'s Life Stages: a countdown floor, the window it is drawn over, and the
/// stage it exits to.
/// </summary>
/// <remarks>
/// <para>
/// <b>A stage is a duration and a successor, and nothing else yet.</b> <c>adr/0011</c> hangs six
/// preference axes off this table and calls it <em>"the most load-bearing data in the design"</em> —
/// dwelling size, mixed-use tolerance, job-access weighting, school access, rent elasticity,
/// willingness to move. <b>Every one of them is a CONSUMER of a Life Stage rather than part of
/// one</b>, and <c>adr/0027</c> owns the drawing of them. <c>plans/0046</c> builds the clock; the
/// readers arrive after, and adding a field here before its reader exists is the dead-column disease
/// this milestone was opened to cure.
/// </para>
/// <para>
/// <b>The successor is AUTHORED and not derived from declaration order</b>, which is the one shape
/// decision in this type. <c>adr/0011</c>'s chain is not a line: <em>Young</em> exits to
/// <em>Family</em> or to <em>Childless</em> depending on a fertility decision, and <em>Childless</em>
/// and <em>Empty Nest</em> are separate terminals that <em>"behave identically going forward and are
/// deliberately kept separate"</em> because they are different diagnoses. ***An order-derived
/// successor would make the file's line order a mechanism***, and would silently re-route the chain
/// the first time somebody added a stage in the middle.
/// </para>
/// </remarks>
public readonly record struct LifeStageDefinition
{
    /// <summary>
    /// <c>N</c>: the fewest Days a Household spends in this stage. <b>A floor and not a length.</b>
    /// </summary>
    /// <remarks>
    /// <b>In Days, because there is no calendar and <c>CONTEXT.md</c> bans inventing one.</b>
    /// <em>Year</em>, <em>month</em> and <em>season</em> are named in its <i>Terms we deliberately do
    /// not use</i>; the precedent for anything wanting a longer unit is
    /// <see cref="BusinessKindDefinition.PayPeriodDays"/>, which is a week because seven Days is a
    /// week. ⚠ <b>Refused below 1.</b> A stage of zero Days is entered and left on the same Day, so a
    /// chain of them collapses the whole life into one Tick.
    /// </remarks>
    public int DurationDays { get; init; }

    /// <summary>
    /// <c>W</c>: the width of the window the countdown is drawn over, uniform on <c>[N, N+W)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is the load-bearing half and it is easy to read as decoration.</b> Without it every
    /// Household created at Tick 0 leaves this stage on the same Day, and keeps doing so at every
    /// stage for the whole run — so the founding generation stays a single cohort and the city
    /// breathes in lockstep. ***That echo would read as a demographic mechanism and is an artefact of
    /// world creation.*** <c>adr/0011</c>'s amendment adds <c>W</c> for exactly this and calls it
    /// hash-bearing.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero is ALLOWED and means the lockstep world above.</b> It is refused nowhere because a
    /// file demonstrating the echo is a legitimate thing to author — and because a key whose zero is
    /// a real answer must not be defaulted, which is
    /// <see cref="BusinessKindDefinition.ShiftStartEarliestHour"/>'s rule arriving here. <b>Refused
    /// negative</b>, which is a window that runs backwards.
    /// </para>
    /// </remarks>
    public int SpreadDays { get; init; }

    /// <summary>
    /// The stage a Household enters when this one ends; <b>zero means this stage is terminal</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Terminal is spelled as an absent key rather than as a self-reference</b>, and the loader
    /// refuses the self-reference outright. A stage naming itself is a Household that transitions for
    /// ever and arrives back where it was — indistinguishable from a typo, and expensive in a way
    /// nothing reports.
    /// </para>
    /// <para>
    /// ⚠ <b>Terminal now means DISSOLVES</b> (<c>plans/0046</c> stage 2), so the last stage in a chain
    /// is where a Household ends rather than where it piles up. A stage table whose chain has no
    /// terminal is a city nothing can leave, which the loader refuses.
    /// </para>
    /// </remarks>
    public byte NextStage { get; init; }

    /// <summary>
    /// The fewest children a Household bears on leaving this stage. <b>Zero is the whole point.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A BAND and not a count, because <c>adr/0011</c> refuses a constant here</b> — ***"two
    /// children per Household is exact Citizen replacement… that threshold falls out of conservation
    /// rather than being chosen"***. A fixed count would make Replacement Rate a restatement of the
    /// Ruleset rather than a reading of the city, and the diagnosis the ADR wants to show a player
    /// (<em>"your city averages 1.4 children; replacement is 2.0"</em>) would be arithmetic on a
    /// declared number.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero drawn is a REAL answer and routes to <see cref="ChildlessStage"/></b>, not to
    /// <see cref="NextStage"/>. That is <c>adr/0011</c>'s Young exit exactly: <em>"Zero sends the
    /// Household to Childless; otherwise to Family."</em> It is what makes a childless stage
    /// reachable at all, and until <c>plans/0046</c> stage 3 no shipped world could reach one.
    /// </para>
    /// <para>
    /// ⚠ <b>The draw is UNCONDITIONED and that is <em>undesigned</em> rather than a decision.</b>
    /// <c>adr/0011</c> conditions fertility on housing cost, dwelling size and job security, and the
    /// discrete-choice machinery all three would read does not exist. Under
    /// <c>adr/0070</c> that is not evidence about what fertility should be — it is a mechanism
    /// nobody has built, and the answer is build it. ***Do not read a number out of a run of this as
    /// a statement about affordability.***
    /// </para>
    /// </remarks>
    public int ChildrenMin { get; init; }

    /// <summary>The most children a Household bears on leaving this stage, inclusive.</summary>
    /// <remarks>
    /// <b>Inclusive, unlike <see cref="SpreadDays"/>' half-open window, and the difference is not an
    /// inconsistency.</b> A duration's window is a spread around a floor and reads naturally as
    /// <c>[N, N+W)</c>; a child count is a small enumerated set and an author writing
    /// <c>children_max = 3</c> means three children to be possible. ⚠ <b>Equal to
    /// <see cref="ChildrenMin"/> is allowed and means every Household bears the same number</b> —
    /// the lockstep answer, refused nowhere for <see cref="SpreadDays"/>' reason.
    /// </remarks>
    public int ChildrenMax { get; init; }

    /// <summary>
    /// Where a Household goes when it draws zero children; <b>zero means no stage bears here</b>.
    /// </summary>
    /// <remarks>
    /// <b>Its presence is what makes this stage a bearing stage</b>, so there is no separate
    /// <c>bears = true</c> key to disagree with it — the same shape
    /// <see cref="ChildrenBecome"/> uses one stage along. A stage stating a child band and no
    /// childless successor is refused: it would draw zero and have nowhere to send anybody.
    /// </remarks>
    public byte ChildlessStage { get; init; }

    /// <summary>
    /// The stage this one's children form on leaving it; <b>zero means the children do not leave</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0011</c>'s Mature Family exit</b> — ***"the children become adults and form new Young
    /// Households, entering the Unplaced Pool"*** — and it is <b>authored rather than taken as stage
    /// 1</b>, for the reason <see cref="NextStage"/> is: a successor read off declaration order is a
    /// chain nobody wrote down, and the test that pins it is
    /// <c>The_successor_is_authored_rather_than_taken_from_declaration_order</c>.
    /// </para>
    /// <para>
    /// 🔴 <b>The Citizens are MOVED and never created, which is what makes conservation testable.</b>
    /// <c>adr/0011</c>: ***"Citizen count is conserved across the spawn transition — children become
    /// the adults of the new Households — which makes the invariant testable rather than
    /// asserted."*** The children were born at the bearing stage's exit; this transition rehomes
    /// them.
    /// </para>
    /// <para>
    /// ⚠ <b>ONE new Household per child, so Households grow where Citizens are conserved.</b> Two
    /// children replace two adults exactly, and they arrive as two Households of one adult rather
    /// than one of two — because pairing them would be a rule about who partners with whom, and
    /// nothing in the design says. ***Household count is not conserved here and Citizen count is***,
    /// which is the invariant <c>adr/0011</c> names.
    /// </para>
    /// </remarks>
    public byte ChildrenBecome { get; init; }

    /// <summary>The youngest a Citizen is when it becomes an adult in this stage, in Days.</summary>
    /// <remarks>
    /// <para>
    /// <b>This is <c>Citizens.Age</c>'s writer, and it is the amnesty queue item's literal ask.</b>
    /// The column has been declared, saved and hashed since the table was written and nothing has
    /// ever written it.
    /// </para>
    /// <para>
    /// 🔴 <b>A DRAWN age and not a LIVED one, which is <c>adr/0011</c> word for word</b> —
    /// ***"Adults carry a static age drawn on formation"***, and Citizens do not age. A lived age
    /// would need the Day of birth kept per Citizen, and nothing reads one: the schooling tier the
    /// ADR cares about is ***"derived from the Household's stage rather than from a per-Citizen
    /// counter"***. ⚠ <b>So this number does not advance and no run makes anybody older.</b>
    /// </para>
    /// <para>
    /// ⚠ <b>A child is age ZERO and that is the only marker of childhood there is</b>, which is what
    /// <c>World.IsOfWorkingAge</c> reads: <c>plans/0046</c> stage 4 stops a child taking a job or
    /// founding a Business. ***It moved the labour supply by 15% on <c>aged.toml</c>*** — 1,411 of
    /// 1,411 employed before the gate and 1,200 of 1,411 after — which is why that stage was kept
    /// apart from generation rather than landed with it.
    /// </para>
    /// </remarks>
    public int AdultAgeMinDays { get; init; }

    /// <summary>The oldest a Citizen is when it becomes an adult in this stage, in Days, inclusive.</summary>
    public int AdultAgeMaxDays { get; init; }

    /// <summary>
    /// <c>B</c>: where this stage sits on the space-against-centrality axis, as a percent.
    /// <b>0 wants room to breathe, 100 wants the middle of the city, 50 has no opinion.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is <c>adr/0027</c>'s <em>base</em>, and it is half a number.</b> The other half is
    /// <see cref="CentralitySpreadPercent"/>, and the pair is deliberately the same shape as
    /// <see cref="DurationDays"/> and <see cref="SpreadDays"/> one field up — a floor and a width,
    /// authored per stage, drawn per Household. ***A stage supplies the range; it never supplies the
    /// value.***
    /// </para>
    /// <para>
    /// ⚠ <b>50 is the neutral value and it is the DEFAULT, so an author who says nothing gets the
    /// city they already had.</b> A Household at 50 scores every candidate identically, which
    /// collapses to the first-with-room accept that placement did before any of this existed. That
    /// continuity is the point: the mechanism arrives as a widening rather than as a replacement,
    /// and a world that declines to author it cannot be moved by it.
    /// </para>
    /// <para>
    /// 🔴 <b>PROVISIONAL wherever it is authored</b> — <c>plans/0045</c> standing order 4 suspends
    /// <c>adr/0052</c>, so the shipped values are chosen by taste, name no ratifier and open no §D
    /// row. ⚠ <b>What would ratify one is a city with a housing market in it</b>, because a
    /// preference for the centre is only meaningful against something that makes the centre cost
    /// more, and <c>rent</c> is unbuilt.
    /// </para>
    /// </remarks>
    public int CentralityBasePercent { get; init; }

    /// <summary>
    /// <c>W</c>: how wide this stage's opinion is, as a percent added on top of the base.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SPREAD IS THE DESIGN AND THE BASE IS THE DETAIL, which is the opposite of how the
    /// pair reads.</b> <c>adr/0027</c>: <em>"the spread encodes how much a stage agrees with
    /// itself, and that is a real design lever rather than a tuning detail"</em>. Authoring a width
    /// is authoring <b>how much a demographic is a demographic</b> — a narrow stage behaves
    /// predictably in aggregate, a wide one produces divergent behaviour from identical
    /// circumstances.
    /// </para>
    /// <para>
    /// <b>The ADR's own worked example is the one to copy</b>: Empty Nest widest, because real
    /// retirement choices genuinely diverge — some downsize into walkable centres and others leave
    /// for somewhere quiet — and Family narrowest, because schools matter to nearly all of them.
    /// ***The disagreement about which way retirees move was the evidence that this is a range and
    /// not a value.***
    /// </para>
    /// <para>
    /// ⚠ <b>Zero is ALLOWED and means the stage agrees with itself completely</b> — every Household
    /// in it draws the base exactly. It is refused nowhere, for <see cref="SpreadDays"/>'s reason: a
    /// file demonstrating a monolithic demographic is a legitimate thing to author, and a key whose
    /// zero is a real answer must not be defaulted into meaning something else.
    /// </para>
    /// <para>
    /// ⚠ <b>The failure this key can cause is quiet, and <c>adr/0027</c> names it</b>: ranges so wide
    /// that stages stop being distinguishable in aggregate. The point of a stage is that it produces
    /// a <em>trend</em>; if Family and Empty Nest behave the same at the population level, the widths
    /// have eaten the mechanism they were meant to soften. <b>The recovery is narrower ranges and
    /// never constants.</b>
    /// </para>
    /// </remarks>
    public int CentralitySpreadPercent { get; init; }

    /// <summary>Whether a Household leaving this stage draws a child count.</summary>
    public bool Bears => ChildlessStage != 0;

    /// <summary>Whether a Household leaving this stage sends its children out to form their own.</summary>
    public bool Spawns => ChildrenBecome != 0;
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
    /// <summary>
    /// <b>How much elapsed unserved need, in <em>household-Days</em>, raises a Building of this kind.</b>
    /// Zero means the Rule uses the tier-0 predicate instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0163</c>'s tier 1 threshold, and <c>adr/0170</c>'s entry cost.</b> Demand is the sum
    /// of <c>tick − StarvedSince</c> over the buyers waiting on a District's market row for this
    /// kind's Good, so its unit is <em>Ticks of one household's hunger</em> and eight household-Days
    /// is one household hungry for eight Days or eight hungry for one.
    /// </para>
    /// <para>
    /// ⚠ <b>In Days rather than in Ticks</b>, which is <c>adr/0168</c>'s direction and <c>adr/0059</c>'s
    /// before it: ***author the felt quantity and derive the count***. A Tick figure here would be a
    /// number no designer could read, and the shortest expressible threshold being one household-Day
    /// is the point rather than a limitation.
    /// </para>
    /// <para>
    /// 🔴 <b>It is an ENTRY COST and not a forecast</b> (<c>adr/0170</c>). Raising a shop costs nobody
    /// anything today — no capital, no means test — so this is the only brake on birth, standing in
    /// for a capitalisation band that belongs to milestone 27. ***It may be loose; it may not be
    /// zero.***
    /// </para>
    /// </remarks>
    public int BuildThresholdDays { get; init; }

    /// <summary>
    /// <b>How long after raising one Building of this kind in a District before another may be raised
    /// there.</b> Zero means no cooldown.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The across-trigger damper, and it is NOT the claim.</b> The claim stops several Lots sampled
    /// in <em>one</em> pass from answering the same hunger; this stops the <em>next</em> pass building
    /// again before the shop just raised has had time to stock up and sell anything. Between them they
    /// are what <c>adr/0163</c> means by *a claim makes the demand a stock that answering it depletes*
    /// — one within a sweep, one across sweeps.
    /// </para>
    /// <para>
    /// ⚠ <b>Per DISTRICT, which is what makes it legal under <c>adr/0163</c>.</b> That record refuses a
    /// build-rate throttle because *"a throttle cannot tell five shops for one hungry neighbourhood
    /// from five shops for five"* — and a **global** throttle cannot. This one is keyed on the market
    /// row, so five hungry Districts each raise a shop on the same trigger and one hungry District
    /// raises one. ***The refusal was of a throttle that could not discriminate, and the reach unit is
    /// what supplies the discriminator.***
    /// </para>
    /// </remarks>
    public int CooldownDays { get; init; }

    /// <summary>Whether this Rule uses <c>adr/0163</c>'s tier-1 demand signal rather than the Pool.</summary>
    public bool ReadsDemand => BuildThresholdDays > 0;

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

    /// <summary>Every live Household. The first one built (<c>plans/0033</c> task 5).</summary>
    Household = 1,

    /// <summary>
    /// Every live Business. Built by <c>adr/0149</c>, milestone 27 task 9.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is the same loop over a different table, and that is the finding rather than the
    /// implementation.</b> A Business's balance is a Bin exactly as a Household's is
    /// (<c>adr/0114</c>), so what a Policy needs from its subject is <em>a slot count, a liveness
    /// test and a balance Bin</em> — three lines — and the reason this was not built at task 5 is
    /// that there were no Businesses. ***The candidate that looked closer, a Bin Rule armed on a
    /// trade, is a much larger mechanism***: <c>RuleEngine.Fire</c> resolves a Building from the
    /// Rule Instance, so a Business-subject Bin Rule needs the Bin engine's Building-centricity
    /// unpicked and not merely a branch. <c>adr/0149</c> records that attempt and its withdrawal.
    /// </remarks>
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
/// what <c>CONTEXT.md</c> lists arrives with its reader — a median wage at 15, median rent and
/// service levels and the commute figure at 16 with the comparison. ⚠ <b>The price per Good arrived
/// EARLY, at 12 rather than at 13</b>, and it arrived by <c>adr/0131</c>'s own rule working rather
/// than failing: <c>adr/0135</c> gave the Pool price a ceiling, which made 12 the milestone that
/// reads it. <b>It is not a third field</b> — it lives in
/// <see cref="Ruleset.HinterlandPrices"/> beside this collection rather than inside this struct, for
/// the reason recorded there. ⚠ <b>Depth
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
/// The <c>[founding]</c> table: what it costs a Household to found a Business, and how often every
/// Household reconsiders founding one.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0145</c>'s founding channel, and the whole table is two numbers because the trigger is
/// deliberately thin.</b> A Household founds on its own <em>means</em> and never on the city's
/// <em>need</em> — so there is no threshold on shop count, no vacancy term and no demand key, and the
/// absence of those keys is the decision rather than an omission. ***A key that read a shortage would
/// be the RCI meter this design refuses, whatever it was called.***
/// </para>
/// <para>
/// ⚠ <b><see cref="ReconsiderTicks"/> is a DURATION and the sample is derived from it</b>
/// (<c>adr/0059</c>): the file states how long it takes every Household to consider founding once, and
/// the engine divides. Authoring a count instead would make the quantity the city actually feels — the
/// fraction of it starting a business per cycle — depend on how big the city is.
/// </para>
/// <para>
/// <b>There is no arrival band here and that is not an oversight.</b> <c>adr/0145</c>'s other channel
/// is a Business arriving through a gate, and what an immigrant carries belongs to the
/// <see cref="HinterlandDefinition"/> it comes from, exactly as a Household's does. ***Two channels,
/// two homes, because the numbers answer to different worlds.***
/// </para>
/// </remarks>
public readonly record struct FoundingRuleset(Money FoundingBand, int ReconsiderTicks)
{
    /// <summary>A Ruleset in which no Household ever founds a Business.</summary>
    public static FoundingRuleset None => default;

    /// <summary>Whether the founding channel runs at all.</summary>
    /// <remarks>
    /// <b>Keyed on the period rather than on the band</b>, because a band of zero is a coherent world
    /// — a shop founded with no capital, which then cannot pay for anything and gives up — whereas a
    /// period of zero is not a cadence at all. ⚠ <b>The loader refuses a zero period</b>, so this
    /// reads <c>false</c> only for a file that states no <c>[founding]</c> table.
    /// </remarks>
    public bool Runs => ReconsiderTicks != 0;

    /// <summary>
    /// How many Households to look at this pass, given how many there are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="PlacementRuleset.SampleFor"/>'s derivation with a different population</b>, and
    /// the same <c>adr/0059</c> argument underneath it. ⚠ <b>The draw is with REPLACEMENT</b>, so this
    /// is a rate and not coverage: about <c>1/e</c> of Households go unlooked-at in any period, and a
    /// reader wanting *every Household considered once* will not get it from this number.
    /// </para>
    /// <para>
    /// ⚠ <b>It divides by the period and multiplies by the trigger interval</b>, which is
    /// <c>[placement]</c>'s — the founding pass runs on placement's trigger rather than owning one.
    /// A second cadence would be a second thing to tune for no stated benefit, and the loader refuses
    /// a <c>[founding]</c> table in a file with no <c>[placement]</c>.
    /// </para>
    /// </remarks>
    /// <param name="households">How many Households the sample is drawn from.</param>
    /// <param name="interval">The <c>[placement]</c> trigger interval, in Ticks.</param>
    public int SampleFor(int households, uint interval)
    {
        if (ReconsiderTicks < 1)
        {
            throw new InvalidOperationException(
                $"[founding] has a reconsider period of {ReconsiderTicks}. It is how long every "
                + "Household takes to consider founding once, so it divides; the loader refuses "
                + "anything below the placement interval, and this Ruleset was not built by it.");
        }

        return (int)IntegerMath.CeilDiv((long)households * interval, ReconsiderTicks);
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
/// The <c>[districts]</c> table — <b>when a concentration of Buildings is a centre</b>
/// (<c>adr/0134</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>One key, because a watershed only needs to be told what counts as a peak.</b> Everything else a
/// District has — where it starts, where it stops, how many there are — falls out of the
/// Building-density field and the road components. <c>adr/0134</c>'s claim is exactly that: the count
/// follows the centres, so there is no count to author, no extent to author and no ceiling to author.
/// </para>
/// <para>
/// <b>Absence means no Districts are derived at all</b>, on <see cref="RoadRuleset.None"/>'s polarity
/// and for a sharper reason than convenience. The threshold is hash-bearing and unratified
/// (<c>adr/0052</c>), and a default in the binary is a hash-bearing number nobody chose, in a file
/// nobody can see, ratified by nothing. A file that wants Districts states the table.
/// </para>
/// <para>
/// ⚠ <b>Nothing in this project reads a District yet.</b> <c>Scope.Pool</c> still throws; the Pool
/// Bins are milestone 12 task 5. So a Ruleset that states this table gets rows in
/// <see cref="Space.DistrictTable"/> and no behaviour, which is what makes the derivation testable
/// before anything depends on it being right.
/// </para>
/// </remarks>
/// <param name="ProminencePercent">
/// How far a peak must stand above the saddle that joins it to a taller peak, as a percentage
/// <b>of its own height</b>, before it is a centre of its own.
/// <para>
/// <b>Relative rather than absolute, and that is the decision this key encodes.</b> An absolute
/// Building count would be tied without saying so to <c>[lots] lots_per_segment</c>, which is what
/// makes a built Cell on the shipped lattice hold the number of Buildings it holds: the same authored
/// number would mean <em>a large fraction of a peak</em> on one lattice and a rounding error on
/// another.
/// </para>
/// <para>
/// ⚠ <b>Hash-bearing and UNRATIFIED</b> — <c>plans/0002</c> §D1, whose ratifier is milestone 15. The
/// Building-density field is flat on every shipped Ruleset, so no world that exists today can tell one
/// value of this from another; that is a reason to name the ratifier and not a reason to withhold the
/// number (<c>adr/0052</c>, and <c>plans/0037</c> decision 3).
/// </para>
/// </param>
/// <param name="RevisitTicks">
/// How often the extent is re-derived, in Ticks. <b>The cadence, and it is hash-bearing</b>
/// (<c>adr/0134</c>).
/// <para>
/// ⚠ <b>It is NOT a Map Layer cadence and must not be added to <c>[layers]</c> by resemblance.</b>
/// <c>adr/0044</c> owns that one and it is a different number about a different field: a Layer's
/// cadence decides when a source becomes visible to a Rule, and this decides how often a boundary is
/// allowed to have moved. They are also read in different phases.
/// </para>
/// <para>
/// <b>Slow on purpose.</b> Re-evaluation is one of <c>adr/0134</c>'s three stability mechanisms, and
/// the argument is borrowed rather than new — <c>04 §4</c> damps prices because <em>"an undamped price
/// signal produces the same oscillation pathology as undamped congestion feedback"</em>, and it
/// transfers to a boundary unchanged.
/// </para>
/// </param>
/// <param name="HysteresisPercent">
/// How decisively the field must favour a new District before a Cell changes, as a percentage of the
/// level at which its own basin reaches it.
/// <para>
/// <b><c>adr/0134</c>'s second mechanism, and its words are <em>"a Cell changes District only when the
/// field difference clears a band, never on a tie"</em>.</b> The field difference here is exact rather
/// than analogous: a Cell's basin reaches it at some flood level, and a rival basin reaches it at the
/// level the two basins first touched. ⚠ <b>Everything a basin gains BELOW that touch level is reached
/// by both at the same level and is therefore a genuine tie</b> — the watershed's answer there is a
/// scan order, not a finding, and this is the key that stops a scan order being felt.
/// </para>
/// <para>
/// ⚠ <b>Hash-bearing and UNRATIFIED</b>, on <see cref="ProminencePercent"/>'s footing exactly and for
/// its reason: the field is flat on every shipped Ruleset, so no boundary on any world that exists is
/// ever contested. <c>plans/0002</c> §D1, ratifier milestone 15.
/// </para>
/// </param>
/// <param name="MigrateCells">
/// The most Cells that may change District in one re-evaluation. <b>The damping bound.</b>
/// <para>
/// 🔴 <b>It bounds how far a boundary MOVES and not how much work the evaluation does.</b>
/// <c>adr/0134</c>: <em>"a boundary migrates by at most a bounded number of Cells per evaluation, so it
/// never jumps."</em> ***A work bound would be a profiler's number, and sizing a District from a
/// profiler is what <c>plans/0037</c> forbids by name*** — extent decides pooling, which is a change to
/// the city and not an optimisation.
/// </para>
/// <para>
/// ⚠ <b>Only a Cell moving from one District to ANOTHER counts against it.</b> A Cell joining its first
/// District — new ground, newly built on — is growth rather than migration, and counting it would
/// freeze a growing city's boundaries against a budget its own construction was spending. A Cell whose
/// District is being destroyed moves for free too, because the alternative is membership of a row that
/// no longer exists.
/// </para>
/// </param>
public readonly record struct DistrictRuleset(
    int ProminencePercent, int RevisitTicks, int HysteresisPercent, int MigrateCells)
{
    /// <summary>A Ruleset whose city has no Districts.</summary>
    /// <remarks>
    /// <b>Absence is the unset spelling, on <see cref="ParkingRuleset.None"/>'s rule.</b> Every
    /// percentage in range means something — a low one splits a city at every dip, a high one keeps it
    /// whole — so no value inside the range can do duty as <em>unset</em>.
    /// </remarks>
    public static DistrictRuleset None => default;

    /// <summary>Whether this city has Districts at all.</summary>
    public bool Runs => ProminencePercent > 0;

    /// <summary>Whether a re-evaluation falls on this Tick.</summary>
    /// <remarks>
    /// <b>Tick 0 is excluded because world creation has already evaluated</b> — re-running the
    /// watershed on the first Step would be the same answer computed twice, and at task 5 it would be
    /// a District destroyed and recreated before anything had a chance to use it.
    /// </remarks>
    public bool RevisitsOn(Ticks tick) =>
        Runs && tick.Raw > 0 && tick.Raw % (ulong)RevisitTicks == 0;
}

/// <param name="DecayPercent">
/// How much of the standing consumption rate survives one Day, as a percentage. The rest of the
/// weight goes to the Day just ended. <b>Hash-bearing.</b>
/// </param>
/// <param name="MoveCapPercent">
/// The furthest a price may travel in one recompute, as a percentage of the ceiling.
/// <b>Hash-bearing</b>, and it is what stops <c>04 §4</c>'s predicted oscillation from being a
/// square wave.
/// </param>
/// <summary>
/// Which of <c>adr/0103</c>'s four Needs a Resource or a service kind feeds.
/// </summary>
/// <remarks>
/// <para>
/// <b>All four, and the split is now between what feeds them rather than between which exist.</b>
/// <c>adr/0103</c> closes the set at Sustenance, Satisfaction, Education and Health. The first two
/// are <em>bought</em> — a Resource declares <c>[[resource]] need</c> and a Rule firing is the
/// occasion. The last two are <em>attended</em> (<c>adr/0032</c>): a Household travels to a service
/// Building, which declares <see cref="KindDefinition.Serves"/>, and the Trip is the occasion.
/// ***So a Resource still cannot feed Education or Health, and <c>RulesetLoader.ReadNeed</c> still
/// refuses both by name — but the reason changed from <em>undesigned</em> to <em>wrong door</em>.***
/// </para>
/// <para>
/// 🔴 <b>The last two were parked and <c>docs/deferred.md</c> named the exact thing that would
/// un-park them</b>: <em>"A civic Building that a Household draws on. <c>Service</c> is the unapplied
/// verb and <c>School</c> is zero files; the moment one exists, the occasion exists and the
/// degradation rule follows from it rather than being chosen."</em> The verb is applied, so the
/// occasion exists and the rule followed. ***Nothing here was chosen that the trigger did not
/// supply.***
/// </para>
/// <para>
/// ⚠ <b>Education and Health are ONE mechanism and not two.</b> <c>adr/0032</c>: <em>"Health belongs
/// with schools, not with fire. A clinic is visited routinely; it is Attended."</em> Refusing
/// <c>serves = "health"</c> while allowing <c>education</c> would re-draw the line that record
/// removed, and would need an argument nobody has.
/// </para>
/// </remarks>
public enum Need : byte
{
    /// <summary>A Resource that feeds no Need. Every Resource in every Ruleset until milestone 28.</summary>
    None = 0,

    /// <summary>Fed by Food. <c>04 §1</c>: <em>fails fast and hard</em>, and produces crises.</summary>
    Sustenance = 1,

    /// <summary>Fed by Consumer Goods. <em>Fails slowly and softly</em>, and produces decline.</summary>
    Satisfaction = 2,

    /// <summary>
    /// Fed by attending a school. <b>The first Need whose occasion is a Trip and not a Rule
    /// firing</b> (<c>adr/0032</c>).
    /// </summary>
    /// <remarks>
    /// <b>It has no Good and never will.</b> <c>04 §1</c>'s Good → Need diagram is not a total map,
    /// which is what <c>ReadNeed</c>'s refusal has always said; what it lacked was somewhere to point
    /// instead, and <see cref="KindDefinition.Serves"/> is now that place.
    /// </remarks>
    Education = 3,

    /// <summary>
    /// Fed by attending a clinic. <b><see cref="Education"/>'s twin, and deliberately not its
    /// sibling-by-analogy</b> — <c>adr/0032</c> sorts services by <em>who moves</em>, and both of
    /// these are Attended.
    /// </summary>
    Health = 4,
}

/// <summary>
/// How far a Need moves on a failed occasion and on a met one, and how far down it may go.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Need is a relative scalar where 0 is ideal and negative values are deficit</b>
/// (<c>CONTEXT.md</c> → Need, <c>04 §2</c>). It is <em>not</em> a stockpile — it expresses <em>how
/// well is this Household doing</em> — so these are steps on a scale nobody banks.
/// </para>
/// <para>
/// 🔴 <b>Every number here is PROVISIONAL and chosen by taste</b>, under <c>plans/0045</c> standing
/// order 4, which suspends <c>adr/0052</c>. No ratifier, no <c>plans/0002</c> §D row. <c>adr/0102</c>
/// authorises no number and never did; what the corpus specifies is the <em>representation</em> and
/// the <em>trigger</em>, and it stops there.
/// </para>
/// <para>
/// ⚠ <b><see cref="Floor"/> is required rather than a convenience.</b> A Need that fell for ever
/// would be a magnitude trending downward at steady state, which is <c>adr/0006</c> as
/// <c>adr/0003</c> extends it to quantities — the one invariant a long run is written to catch. ***A
/// Household in a city with no food is destitute at some depth and no more destitute after that.***
/// </para>
/// <para>
/// ⚠ <b>A degrade is a RATE PER DAY and a recover is a STEP PER OCCASION, and the asymmetry is
/// deliberate.</b> A Rule that fires is being visited, so a meal is an event; a Rule that is blocked
/// is asleep on its Bin, so a shortage is a <em>state</em> and only a duration can measure it —
/// <c>adr/0053</c>'s finding about the Building, arriving at the Household.
/// </para>
/// <para>
/// <b>Sustenance and Satisfaction get separate rates because <c>04 §1</c> distinguishes them by
/// speed</b>: <em>"Food fails fast and hard"</em> against <em>"Consumer Goods fail slowly and
/// softly"</em>. One shared pair of rates would collapse the crisis chain and the decline chain into
/// one, which is the distinction that section exists to draw.
/// </para>
/// </remarks>
/// <param name="SustenanceDegrade">What a DAY unmet costs. Positive, and a rate.</param>
/// <param name="SustenanceRecover">What one met Sustenance occasion returns. Positive.</param>
/// <param name="SatisfactionDegrade">What a DAY unmet costs. Positive, and a rate.</param>
/// <param name="SatisfactionRecover">What one met Satisfaction occasion returns. Positive.</param>
/// <param name="EducationDegrade">What a DAY without a reachable school costs. Positive, a rate.</param>
/// <param name="EducationRecover">What one attended school run returns. Positive.</param>
/// <param name="HealthDegrade">What a DAY without a reachable clinic costs. Positive, a rate.</param>
/// <param name="HealthRecover">What one attended clinic visit returns. Positive.</param>
/// <param name="Floor">The deepest deficit a Need may reach. Negative, or zero for no Needs.</param>
public readonly record struct NeedRuleset(
    int SustenanceDegrade,
    int SustenanceRecover,
    int SatisfactionDegrade,
    int SatisfactionRecover,
    int EducationDegrade,
    int EducationRecover,
    int HealthDegrade,
    int HealthRecover,
    int Floor)
{
    /// <summary>A Ruleset in which no Household has a Need at all.</summary>
    public static NeedRuleset None => default;

    /// <summary>Whether any Need moves in this city.</summary>
    public bool Runs => Floor < 0;

    /// <summary>
    /// Whether any <em>attended</em> Need moves in this city — <c>adr/0032</c>'s Education and
    /// Health.
    /// </summary>
    /// <remarks>
    /// <b>Asked separately from <see cref="Runs"/> because the two halves have different engines.</b>
    /// A file stating <c>[needs]</c> and declaring no service kind has bought Needs and no attended
    /// ones, and <c>ServiceEngine</c> must stay silent in it rather than sweeping a population whose
    /// answer cannot move. ⚠ <b>It keys on the rates and not on the kinds</b>, because the kinds are
    /// <see cref="Ruleset.ServesAny"/>'s question and a Ruleset can legally have one without the
    /// other.
    /// </remarks>
    public bool Attends => Runs && (EducationDegrade > 0 || HealthDegrade > 0);

    /// <summary>What one failed occasion costs the given Need.</summary>
    public int DegradeOf(Need need) =>
        need == Need.Sustenance ? SustenanceDegrade
        : need == Need.Satisfaction ? SatisfactionDegrade
        : need == Need.Education ? EducationDegrade
        : need == Need.Health ? HealthDegrade
        : 0;

    /// <summary>What one met occasion returns to the given Need.</summary>
    public int RecoverOf(Need need) =>
        need == Need.Sustenance ? SustenanceRecover
        : need == Need.Satisfaction ? SatisfactionRecover
        : need == Need.Education ? EducationRecover
        : need == Need.Health ? HealthRecover
        : 0;
}

/// <summary>
/// The <c>[market]</c> table — <b>how fast a Pool price is allowed to move</b>
/// (<c>adr/0135</c>, milestone 12 task 6).
/// </summary>
/// <remarks>
/// <para>
/// <b>Two keys, and neither of them is a price.</b> The price itself is never authored — it is
/// <see cref="Ruleset.ImportCeiling"/> on the day a Pool is opened and a damped tâtonnement
/// afterwards. What a designer authors here is the <em>damping</em>: how much of the smoothed
/// consumption rate survives a Day, and how far the price may travel in one recompute.
/// <c>04 §4</c> asks for three properties — damped, local, bounded. <b>Local</b> is the Pool being
/// per-District and is not a number; <b>bounded</b> is <see cref="Ruleset.ImportCeiling"/> and is
/// not a number either; <b>damped</b> is this table, and it is the only one of the three that
/// needed keys.
/// </para>
/// <para>
/// <b>Absence means every trade clears at the ceiling for ever</b>, on
/// <see cref="TrafficRuleset.None"/>'s polarity — a real Ruleset choice rather than a defaulted
/// one, and the city that every file without this table has. <c>adr/0135</c> rejected *"clear every
/// trade at the ceiling"* as this milestone's **shipped** answer, on the ground that a constant
/// price is a price mechanism nobody can tell from an absent one. ⚠ <b>That refusal is about what
/// the milestone ships, not about what a Ruleset may say</b> — the eleven files that state no
/// <c>[market]</c> keep exactly the city they had, which is the thing that makes this key addable
/// at all.
/// </para>
/// <para>
/// ⚠ <b><see cref="Runs"/> keys on the cap and not on the decay, and the reason is that zero decay
/// means something.</b> <see cref="DecayPercent"/> at 0 is *no smoothing* — the rate is the Day's
/// own consumption, which is a legitimate and rather twitchy market. A zero
/// <see cref="MoveCapPercent"/> is different in kind: it says the price may not move, which is
/// exactly the city an omitted table describes. ***A value inside a key's range that duplicates
/// absence is the one that may spell it***, which is <see cref="DistrictRuleset.None"/>'s rule
/// applied to a table where only one of the two keys qualifies.
/// </para>
/// <para>
/// 🔴 <b>Both numbers are UNRATIFIED and <c>plans/0002</c> §D1 holds a row apiece.</b>
/// <c>adr/0135</c> predicted *"two or three"* and it is two: the third — an initial price — does not
/// exist, because a new Pool opens at the ceiling and a Pool nobody trades in stays there, which
/// keeps <c>adr/0045</c>'s ladder ordered from Tick 0 without anybody choosing a seed.
/// ***A number that was going to be needed and then was not is worth saying out loud***, because
/// the alternative is a §D row for a key that never shipped.
/// </para>
/// </remarks>
public readonly record struct MarketRuleset(int DecayPercent, int MoveCapPercent)
{
    /// <summary>A Ruleset whose Pool prices never leave the ceiling.</summary>
    public static MarketRuleset None => default;

    /// <summary>Whether a Pool price moves at all in this city.</summary>
    public bool Runs => MoveCapPercent > 0;

    /// <summary>Whether the Day-boundary recompute falls on this Tick.</summary>
    /// <remarks>
    /// <b>Tick 0 is excluded because no Day has elapsed to consume anything in.</b> The first
    /// recompute would read a rate of zero and keep the seeded price, so it is the same answer
    /// arrived at more expensively — <see cref="DistrictRuleset.RevisitsOn"/>'s exclusion for the
    /// same reason in different words.
    /// </remarks>
    public bool RepricesOn(Ticks tick) =>
        Runs && tick.Raw > 0 && tick.Raw % Ticks.PerDay == 0UL;

    /// <summary>
    /// The consumption rate carried into the next Day.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An exponential moving average, and the Day is its unit.</b> <paramref name="standing"/>
    /// and the return are both <em>units of the Good per Day</em>; <paramref name="consumed"/> is
    /// what the Day just ended actually drew. ***Naming the units is the point of keeping this
    /// separate from the Day's own bucket*** — an accumulator that decayed in place would be a rate
    /// multiplied by a constant nobody could name, which is <c>plans/0012</c> <b>Cause 5</b> built
    /// rather than quoted.
    /// </para>
    /// <para>
    /// 🔴 <b>It ROUNDS rather than flooring, and that was found by a test rather than reasoned to.</b>
    /// Flooring makes any draw below <c>100 / (100 - DecayPercent)</c> units a Day fold to a standing
    /// rate of <b>zero</b>, which <see cref="Reprice"/> reads as *no trades* — so a Pool genuinely
    /// being drawn from at one unit a Day would have had a frozen price and looked exactly like a Pool
    /// nobody had touched. ⚠ <b>The threshold moves with the DAMPING</b>, so a designer retuning
    /// <see cref="DecayPercent"/> would silently change which markets have prices at all: that is the
    /// *knob that switches the mechanism off while reading as a setting* pattern the loader refuses
    /// three times over, arriving inside the arithmetic where no refusal can reach it.
    /// </para>
    /// <para>
    /// ⚠ <b>What rounding costs is that a rate of 1 never decays back to 0</b>, so *no trades* is only
    /// reachable before a Pool's first ever draw. <b>That is a bounded magnitude and not
    /// <c>adr/0006</c>'s growth</b>, and it is arguably the better city: a Pool that has stopped
    /// selling prices from its cover — which for a stocked one collapses toward nothing — rather than
    /// freezing at whatever it last charged. ***A market that stops clearing is worth less, not the
    /// same.*** ⚠ <b>Whether one unit a Day is fine enough resolution is MEASURABLE and is now
    /// REACHABLE, which it was not when this was written</b> — it needed a world with real consumption
    /// in it <em>and</em> a cover that is not structurally zero, and <c>rulesets/oversupplied.toml</c>
    /// is both as of <c>adr/0171</c>. Still unmeasured, and still filed rather than guessed at
    /// (<c>adr/0043</c>).
    /// </para>
    /// </remarks>
    /// <param name="standing">The rate at the end of the previous Day, in units per Day.</param>
    /// <param name="consumed">What was drawn from the Pool during the Day just ended.</param>
    public long Smooth(long standing, long consumed) =>
        IntegerMath.RoundDiv((standing * DecayPercent) + (consumed * (100 - DecayPercent)), 100);

    /// <summary>
    /// Where a Pool price stands after one Day's recompute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The tâtonnement, and it is a cover ratio rather than an excess-demand term.</b> The target
    /// is <c>ceiling / cover</c>, where <c>cover</c> is how many Days the standing level would last
    /// at the standing rate. One Day of cover prices at the ceiling; ten Days of cover price at a
    /// tenth of it; an empty Pool prices at the ceiling because there is nothing to undercut it
    /// with. ⚠ <b>Cover below one Day does not price ABOVE the ceiling</b> — the ratio is clamped by
    /// taking <c>max(level, rate)</c> as the denominator, and the ceiling is a ceiling.
    /// </para>
    /// <para>
    /// ⚠ <b>A rate of zero keeps the standing price rather than raising it.</b> No trades is an
    /// absence of information and not evidence of scarcity, and the two are easy to conflate because
    /// both leave <paramref name="consumed"/> at zero. ***A market with no trades in it has nothing
    /// to say about what things are worth.***
    /// </para>
    /// <para>
    /// <b>The floor is zero and a glut really can take a price to nothing</b>, which is decision 4's
    /// answer and task 10's material: a Provider selling into a saturated Pool earns less than it
    /// spent, and bankruptcy is the observable that distinguishes this market from a decorative one.
    /// </para>
    /// </remarks>
    /// <param name="price">Where the price stands now.</param>
    /// <param name="ceiling">The import ceiling for this Good — <see cref="Ruleset.ImportCeiling"/>.</param>
    /// <param name="level">What the Pool holds.</param>
    /// <param name="rate">The smoothed consumption rate, in units per Day.</param>
    public Money Reprice(Money price, Money ceiling, long level, long rate)
    {
        if (!Runs || rate <= 0) return price;

        long cover = level > rate ? level : rate;
        long target = IntegerMath.FloorDiv(ceiling.Raw * rate, cover);
        long cap = IntegerMath.FloorDiv(ceiling.Raw * MoveCapPercent, 100);

        long step = target - price.Raw;
        if (step > cap) step = cap;
        else if (step < -cap) step = -cap;

        long moved = price.Raw + step;

        // ⚠ ZERO IS THE FLOOR ON PURPOSE and a floor of one was tried and reverted. A Provider selling
        // into a saturated market earns less than it spent, and bankruptcy is the observable that
        // tells this market from a decorative one -- plans/0037 decision 4, settled with the user in
        // the room, and PoolPriceTests.A_glut_walks_the_price_to_nothing asserts it.
        //
        // ⚠ A zero price DOES make a buyer's money requirement zero, because RuleEngine.PoolDraw
        // charges the money leg as `amount x price`. That is not repaired here by refusing the zero:
        // it is repaired by World.RingEveryMoneyBin, which drains the money Bins whenever a reprice
        // moves anything, so a buyer whose requirement has changed underneath it is woken rather than
        // left asleep. ***The price is allowed to reach zero; what is not allowed is nobody noticing.***
        if (moved < 0) moved = 0;
        else if (moved > ceiling.Raw) moved = ceiling.Raw;
        return new Money(moved);
    }
}

/// <summary>
/// The <c>[capacity]</c> table — <b>how much floor one of anything takes</b>.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>IT EXISTS BECAUSE ENUMERATING CAPACITY PER KIND DOES NOT SCALE, AND THE EVIDENCE WAS IN
/// <c>rulesets/</c> THE WHOLE TIME.</b> Thirty-nine <c>[[building]] occupants</c> declarations across
/// the shipped files carried exactly <b>two</b> distinct kinds and <b>three</b> distinct values, and
/// thirty-two <c>[[business]] jobs</c> declarations carried two. ***A number repeated thirty times is
/// not thirty decisions***, and a game whose cities are meant to run to a million people cannot ask an
/// author to write one down for every combination of form and use.
/// </para>
/// <para>
/// 🔴 <b><c>adr/0068</c> DECIDED THE OPPOSITE AND ITS PREMISE IS GONE.</b> That record argues from
/// <c>adr/0064</c> that occupancy belongs on the kind because <em>there is no distinguishing
/// property</em> — no two Buildings of one kind differed in any way the engine could read. <b>There is
/// one now.</b> A Lot carries a parcel, the parcel carries a footprint and the footprint carries a
/// floor area, and those differ Building by Building. ***The decision was right on the day and its
/// reason expired two commits before this one.*** ⚠ <b>No ADR records the change</b>, because
/// <c>plans/0045</c>'s amnesty forbids writing one; <c>adr/0068</c> is annotated and the argument is in
/// <c>plans/0053</c>.
/// </para>
/// <para>
/// <b>Three rates, and each has a source outside this repository.</b> A Tile is 16 m². A dwelling of
/// about 96 m² is <b>6</b> Tiles; an office worker at about 16 m² is <b>1</b>; a parking minimum of one
/// space per dwelling is <b>6</b> again. ⚠ <b>They are still chosen and still unratified</b> — what
/// changed is that there are three of them instead of thirty-nine, and that each is a quantity somebody
/// outside this project has measured.
/// </para>
/// <para>
/// ⚠ <b>A kind still says WHETHER it houses; only the count derives.</b>
/// <see cref="KindDefinition.Houses"/> is the whole of what <c>occupants</c> became — capacity is
/// geometry and behaviour is content, and a school is not a boarding house because it is large.
/// </para>
/// <para>
/// ⚠ <b>Absence is a city in each case.</b> No <c>[capacity]</c> table means no Building holds anybody;
/// no <c>floor_tiles_per_job</c> means nobody is employed anywhere; no
/// <c>floor_tiles_per_parking_space</c> means the city has no parking. <b>A parking minimum is a
/// property of a city and not of a building</b>, which is where it sits in every real planning code and
/// is a better home than the per-kind key it replaces.
/// </para>
/// <para>
/// 🔴 <b><see cref="FloorTilesPerPlace"/> IS THE FOURTH RATE AND ITS ABSENCE MEANS THE OPPOSITE
/// OF THE OTHER THREE.</b> They are supplies, so an absent one is a city with none of that thing; this
/// one is a <em>ceiling</em>, so an absent one is a city where no service Building is ever full.
/// ***An unstated bound is no bound***, and it has to be that way round: the other reading would make
/// every school in every Ruleset shipped before this key existed serve nobody, silently, on the day
/// the key was added. ⚠ <b>The cost is that it is opt-in, and a key nobody opts into is a mechanism
/// nobody reviews</b> — which is why <c>RulesetLoader</c> refuses it in a file that declares no
/// service kind rather than letting it sit inert.
/// </para>
/// <para>
/// ⚠ <b>It has no anchor outside this repository and the other three do.</b> A dwelling, a desk and
/// a parking space are quantities somebody has measured; ***the standing city says nothing about a
/// school place*** — no kind has ever declared a place count, so there is no thirty-nine-declaration
/// division to perform and nothing to divide. It is chosen, under <c>plans/0045</c> standing order 4,
/// and what a reading of the shipped world does with it belongs in <c>rulesets/schooled.toml</c>'s
/// header rather than folded into this sentence.
/// </para>
/// </remarks>
/// <param name="FloorTilesPerOccupant">How much floor one tenancy takes. Zero means nobody is housed.</param>
/// <param name="FloorTilesPerJob">How much floor one job takes. Zero means nobody is employed.</param>
/// <param name="FloorTilesPerParkingSpace">How much floor one parking space is owed. Zero means none.</param>
/// <param name="FloorTilesPerPlace">
/// How much floor one attendance a Day takes at a service Building. <b>Zero means no service Building
/// is ever full</b> — a ceiling and not a supply, so its absence removes a bound instead of removing
/// a thing.
/// </param>
public readonly record struct CapacityRuleset(
    int FloorTilesPerOccupant,
    int FloorTilesPerJob,
    int FloorTilesPerParkingSpace,
    int FloorTilesPerPlace = 0)
{
    /// <summary>A Ruleset in which nothing holds anybody.</summary>
    public static CapacityRuleset None => default;

    /// <summary>How many of something a floor area holds, at a rate. Zero rate means none.</summary>
    /// <remarks>
    /// <b>At least one where the rate is stated and any floor exists</b>, which is the one place this
    /// rounds rather than truncating. ***A Building with a wall on it holds somebody***; a footprint
    /// smaller than one dwelling is a small dwelling and not an empty lot.
    /// </remarks>
    public static int Holds(int floorTiles, int rate)
    {
        if (rate <= 0 || floorTiles <= 0)
        {
            return 0;
        }

        int held = Arithmetic.IntegerMath.FloorDiv(floorTiles, rate);

        return held < 1 ? 1 : held;
    }
}

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
/// 🔴 <b><see cref="SetbackTiles"/> IS THE SECOND KEY AND IT ARRIVED BECAUSE THE SIMULATION AND THE
/// DRAWING DISAGREED ABOUT WHAT A BUILDING COVERS.</b> The core sealed the <em>whole parcel</em> —
/// garden included — while the shell drew a wall on 55–100% of the frontage and 45–85% of the depth,
/// so ***Sealing was about twice what the picture showed*** and the four numbers that decided it lived
/// in <c>Borough.Godot</c>, where no Ruleset could retune them and no State Hash could see them. That
/// is <c>adr/0015</c>'s rule — a number a designer would want to change is Ruleset data — applied to
/// numbers that had escaped it by being drawing constants.
/// </para>
/// <para>
/// ⚠ <b>ONE KEY REPLACES FOUR, AND THE SHAPE IS DERIVED FROM IT.</b> A setback is a length rather
/// than a fraction, so <b>coverage rises with the parcel</b> without anybody stating that it should:
/// at the shipped lattice a detached plot is 6 × 6 Tiles and keeps about <b>44%</b>, a terrace's
/// 6 × 16 keeps <b>58%</b>, and a slab's 16 × 16 keeps <b>77%</b>. ***A fraction would have made every
/// density cover the same share of its ground***, which is the failure the shell's own
/// <c>DepthFillLow</c> comment describes one level down. ⚠ <b>The four setbacks are drawn
/// independently per parcel</b>, so a street varies and no two houses sit on the same line —
/// see <see cref="Determinism.PurposeTag.BuildingFootprint"/>, which draws on the ground rather than
/// on the Lot.
/// </para>
/// <para>
/// <b><see cref="LotsPerSegment"/> is derived rather than chosen</b>, which is why this table had one
/// key instead of the two <c>plans/0022</c> predicted. It is <c>CONTEXT.md</c> → Address's own working
/// figure — <i>"five Buildings share a Segment"</i> — which is the premise of the decision that keeps
/// an Address off a Node and therefore of the ~30,000-Segment figure every routing cost is priced
/// against. If that figure moves, this moves with it and one sentence fixes both.
/// </para>
/// </remarks>
/// <param name="LotsPerSegment">How many Lots one Street Segment carries, both sides together.</param>
/// <param name="SetbackTiles">
/// The most ground a Building leaves on each side of its parcel, in Tiles.
/// </param>
public readonly record struct LotRuleset(int LotsPerSegment, int SetbackTiles)
{
    /// <summary>A Ruleset whose land cannot be subdivided at all.</summary>
    public static LotRuleset None => default;

    /// <summary>Whether the subdivider runs.</summary>
    public bool Runs => LotsPerSegment > 0;

    /// <summary>
    /// <b>The footprint one parcel carries</b> — the parcel inset by four independently drawn
    /// setbacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Each side is drawn on <c>[0, SetbackTiles]</c>, inclusive at both ends.</b> Zero is a
    /// building on the pavement, which a terrace is; the ceiling is a full garden. ⚠ <b>The draw is on
    /// the PARCEL'S CORNER and not on the Lot</b>, so the same patch of ground puts a Building back
    /// where the last one stood.
    /// </para>
    /// <para>
    /// ⚠ <b>A parcel too small to inset keeps its whole self.</b> The setbacks are clamped so the
    /// footprint is at least one Tile each way — ***a Building covering nothing is not a garden***, and
    /// at <c>block_tiles = 4</c> a parcel is one Tile across.
    /// </para>
    /// </remarks>
    public (Quantities.Tiles East, Quantities.Tiles North, Quantities.Tiles Wide, Quantities.Tiles Deep)
        Footprint(WorldKey key, Quantities.Tiles east, Quantities.Tiles north,
            Quantities.Tiles wide, Quantities.Tiles deep)
    {
        if (wide.Raw < 1 || deep.Raw < 1)
        {
            return (Quantities.Tiles.Zero, Quantities.Tiles.Zero, Quantities.Tiles.Zero, Quantities.Tiles.Zero);
        }

        if (SetbackTiles < 1)
        {
            return (east, north, wide, deep);
        }

        // The patch's own corner, packed. Two Tiles coordinates fit a ulong with room to spare, and
        // the map is 16,384 a side.
        ulong patch = ((ulong)(uint)east.Raw << 32) | (uint)north.Raw;
        ulong draw = Determinism.Randomness.Draw(
            key, patch, Quantities.Ticks.Zero, Determinism.PurposeTag.BuildingFootprint);

        int span = SetbackTiles + 1;

        // Four independent bytes of one draw rather than four draws: the stream is counter-based and
        // one call is one mix, and the bytes of a mixed word are independent of each other.
        int west = (int)((draw & 0xFF) % (ulong)span);
        int easterly = (int)(((draw >> 8) & 0xFF) % (ulong)span);
        int southerly = (int)(((draw >> 16) & 0xFF) % (ulong)span);
        int northerly = (int)(((draw >> 24) & 0xFF) % (ulong)span);

        Trim(wide.Raw, ref west, ref easterly);
        Trim(deep.Raw, ref southerly, ref northerly);

        return (new Quantities.Tiles(east.Raw + west),
            new Quantities.Tiles(north.Raw + southerly),
            new Quantities.Tiles(wide.Raw - west - easterly),
            new Quantities.Tiles(deep.Raw - southerly - northerly));
    }

    /// <summary>
    /// <b>How many storeys a Building on this parcel stands</b> — the pattern's height, plus a draw
    /// of one.
    /// </summary>
    /// <remarks>
    /// <b>One storey of variation and no more</b>, drawn on the parcel's corner like the setbacks.
    /// ⚠ <b>It is what stops a block reading as a wall</b>, and it is deliberately small: two
    /// storeys of jitter would blur the ladder, which is the thing the height is supposed to say.
    /// The tag is <see cref="Determinism.PurposeTag.BuildingFootprint"/>'s and the bit range is its
    /// own, so a tall Building is not also a deep one.
    /// </remarks>
    public static byte StoreysOn(
        WorldKey key, Quantities.Tiles east, Quantities.Tiles north, int patternStoreys)
    {
        ulong patch = ((ulong)(uint)east.Raw << 32) | (uint)north.Raw;
        ulong draw = Determinism.Randomness.Draw(
            key, patch, Quantities.Ticks.Zero, Determinism.PurposeTag.BuildingFootprint);

        int storeys = patternStoreys + (int)((draw >> 32) & 1);

        return (byte)(storeys < 1 ? 1 : storeys > 255 ? 255 : storeys);
    }

    /// <summary>Shrinks two setbacks until they leave at least one Tile between them.</summary>
    private static void Trim(int span, ref int low, ref int high)
    {
        while (low + high >= span)
        {
            if (high > low)
            {
                high--;
            }
            else
            {
                low--;
            }
        }
    }
}

/// <summary>
/// One <c>[[band]]</c> — a <b>density band</b>, which is <c>adr/0025</c>'s cap.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0025</c>: <em>"The player sets a ceiling, never a floor."</em></b> A band is permission
/// and never instruction — <em>"a high band on land nothing wants to build on grows nothing, and that
/// is information rather than a bug"</em> — and it is <b>capacity rather than quality</b>: a
/// high-density slum and a high-density tower are the same band.
/// </para>
/// <para>
/// <b>It is a permission set over kinds, and that is the ADR's own mechanism rather than a second
/// one.</b> <c>adr/0025</c> is explicit, in a pointer it added because the sentence had twice been
/// recorded as absent: <em>"a band expresses itself as which kinds a Lot permits (<c>adr/0055</c>),
/// and a kind declares its occupancy — so how many Occupants a Lot may carry is discharged through
/// the permission set rather than by a second mechanism."</em> So this carries the same sixteen bits
/// <see cref="Entities.LotTable.Zone"/> carries, meaning the same thing.
/// </para>
/// <para>
/// 🔴 <b>THE ZONE AND THE BAND ARE INTERSECTED AND NEVER UNIONED</b>, which is the whole of *cap*.
/// A Zone says what <em>uses</em> are allowed here; a band says at what <em>intensity</em>. Taking the
/// union would let a band <em>grant</em> a permission the player never painted, which is a floor
/// wearing a ceiling's name.
/// </para>
/// <para>
/// <b>A band's identity is its position in the file, counted from 1</b>, which is the convention every
/// <c>[[building]]</c> kind already follows. <b>Zero means <em>no band</em></b> — the state of every
/// block in every world written before bands existed — and a world with no <c>[[band]]</c> at all
/// behaves exactly as it did, which is what keeps this from being a change to twelve shipped
/// Rulesets.
/// </para>
/// <para>
/// ⚠ <b>DECLARATION ORDER IS INTENSITY ORDER, lowest first</b>, and that is authored rather than
/// derived — nothing in the file states a density. It is what the generator reads to lay bands out,
/// and reordering a file's <c>[[band]]</c> tables is therefore a change to the city rather than a
/// tidy-up. ⚠ <b>Nothing checks it</b>, because <em>intensity</em> is not a quantity this type
/// carries; a band admitting more occupants than the one below it is a property of the kinds it
/// names, and the loader cannot see an ordering the author did not state.
/// </para>
/// </remarks>
public readonly record struct BandDefinition
{
    /// <summary>Which kinds this band admits — <b>one bit per kind</b>, as a Zone is.</summary>
    public ushort Admits { get; init; }
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
    /// <see cref="KindDefinition.CondemnAfterTicks"/>'s precedent</b>, and for the same reason:
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
    /// When a concentration of Buildings is a centre of its own. <b>Absent means no Districts.</b>
    /// </summary>
    public DistrictRuleset Districts { get; init; } = DistrictRuleset.None;

    /// <summary>
    /// The <c>[lots]</c> table in force — how zoned land is carved into parcels (<c>adr/0078</c>).
    /// </summary>
    public LotRuleset Lots { get; init; } = LotRuleset.None;

    /// <summary>The <c>[capacity]</c> table — how much floor one tenancy, job or car takes.</summary>
    public CapacityRuleset Capacity { get; init; } = CapacityRuleset.None;

    /// <summary>
    /// The <c>[[band]]</c> tables in declaration order — <b>the density bands</b> (<c>adr/0025</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An init property defaulting to empty</b>, on <see cref="Layers"/>' rule: a file with no
    /// <c>[[band]]</c> is a complete Ruleset and behaves exactly as it did before bands existed. That
    /// is what keeps <c>plans/0053</c> step 2 from being an edit to every shipped file.
    /// </para>
    /// <para>
    /// ⚠ <b>Read a band through <see cref="Band"/> rather than indexing this</b>, because the index a
    /// block carries is <b>one-based</b> — zero is <em>no band</em> and is the value every block in a
    /// bandless world holds.
    /// </para>
    /// </remarks>
    public BandDefinition[] Bands { get; init; } = [];

    /// <summary>Whether this Ruleset declares any density band at all.</summary>
    public bool HasBands => Bands.Length > 0;

    /// <summary>
    /// The band an index names, or one admitting <b>everything</b> where there is none.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>An absent band admits EVERYTHING and does not refuse.</b> A band is a <em>cap</em>, and
    /// the intersection that applies it has to be the identity when no cap was painted — otherwise a
    /// world with no <c>[[band]]</c> would have every Lot admitting nothing and would build no city at
    /// all. ***The permissive answer is the correct one here precisely because the mechanism is
    /// subtractive***, which is the opposite of how most absences in this Ruleset read.
    /// </remarks>
    /// <param name="index">A one-based band index; <c>0</c> means the block carries no band.</param>
    public BandDefinition Band(int index) =>
        index >= 1 && index <= Bands.Length
            ? Bands[index - 1]
            : new BandDefinition { Admits = ushort.MaxValue };

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
    /// <see cref="Jobs"/>' reason. ⚠ <b>It is independent of <see cref="Capacity"/>'s
    /// <c>floor_tiles_per_parking_space</c></b>, which is the <em>supply</em>: a file may state a rate
    /// and no radius, which is a city whose parking exists and cannot be reached, and the two are
    /// separate keys because a designer retunes them for separate reasons.
    /// </remarks>
    public ParkingRuleset Parking { get; init; } = ParkingRuleset.None;

    /// <summary>
    /// <b>What each sort of ground is worth before anything is built on it</b> — the
    /// <c>[[terrain]]</c> tables (milestone 24 task 2).
    /// </summary>
    /// <remarks>
    /// <see cref="TerrainRuleset.None"/> when the file states no <c>[[terrain]]</c>. ⚠ <b>That is a
    /// Ruleset declining to price its ground and never a world without terrain in it</b> — the
    /// polarity is unlike <see cref="Parking"/>'s, and deliberately: every world has ground
    /// (<c>adr/0021</c>), so the per-Cell type column is written from the <c>WorldKey</c> whatever
    /// this says. What absence removes is the <em>lookup</em>, and
    /// <see cref="TerrainRuleset.BaseFertility"/> throws rather than returning zero for it.
    /// </remarks>
    public TerrainRuleset Terrain { get; init; } = TerrainRuleset.None;

    /// <summary>
    /// Where the sea stands — <c>[water]</c>, or <see cref="WaterRuleset.None"/> when unstated.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Absence means the world genuinely has no water in it</b>, which is the opposite polarity
    /// to <see cref="Terrain"/> and the same as <see cref="Parking"/>'s. Every world has ground; not
    /// every world has a coast. <c>adr/0160</c>.
    /// </remarks>
    public WaterRuleset Water { get; init; } = WaterRuleset.None;

    /// <summary>
    /// How often the ground floods — <c>[disasters]</c>, or <see cref="DisasterRuleset.None"/>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Separable from <see cref="Water"/> in one direction only.</b> A world may have a
    /// floodplain and never flood — <c>coastal.toml</c> is one, and a hazard overlay with nothing
    /// behind it is exactly what <c>01 §5.3</c>'s posted price looks like before the first Act of
    /// God. A world that floods with no floodplain is refused at the parse site, because the Hazard
    /// Region is generated from <c>[water] flood_level_percent</c> and there would be nowhere for it
    /// to happen.
    /// </remarks>
    public DisasterRuleset Disasters { get; init; } = DisasterRuleset.None;

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
    /// <b>What each Hinterland charges for each Good</b> — every <c>[[hinterland]]</c> table's
    /// <c>prices</c> array, flattened (<c>adr/0135</c>, milestone 12 task 6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Hinterland-major, one <see cref="Ruleset.ResourceCount"/> stride apiece</b>, so the price
    /// the Hinterland at index <c>h</c> charges for <c>resource</c> is at
    /// <c>h * ResourceCount + resource.Raw - 1</c>. Empty when no <c>[[hinterland]]</c> is declared,
    /// and otherwise exactly <c>Hinterlands.Length * ResourceCount</c> long — the loader fills a
    /// non-Good's slot with zero rather than leaving the array ragged.
    /// </para>
    /// <para>
    /// ⚠ <b>This is where the price lives and <see cref="HinterlandDefinition"/> is not</b>, which is
    /// the one structural decision in the key. A <c>Money[]</c> inside that record struct would give
    /// it reference equality and an array's default hash code, and the struct is the type this file
    /// hands out by value everywhere. ***A per-item array belongs beside the collection and not
    /// inside the item*** when the item is a struct.
    /// </para>
    /// <para>
    /// <b>Nothing reads a per-edge price yet and the array still holds them.</b> What the market
    /// consumes is <see cref="ImportCeiling"/>, the minimum across every declared Hinterland, because
    /// <c>adr/0135</c> ships <b>no haulage term at 12</b> — with carriage free every gate is
    /// equidistant and a city buys at the cheapest, so there is nothing to choose between edges. ⚠
    /// <b>The per-edge figures are authored content and are kept for the milestone that stops
    /// carriage being free</b>: when <c>adr/0133</c>'s charge ships, the ceiling becomes a
    /// per-District <c>min(price + haul)</c> and this array is what it is a minimum <em>over</em>.
    /// ***Collapsing the four to their minimum at load would have thrown away the content that makes
    /// the Outside legible*** — <c>CONTEXT.md</c> → Hinterland's *four comparable markets are each
    /// other's referent.*
    /// </para>
    /// </remarks>
    public Money[] HinterlandPrices { get; init; } = [];

    /// <summary>
    /// The <c>[market]</c> table — <b>how a Pool price moves</b>. <see cref="MarketRuleset.None"/>
    /// when the file states none, which is a city whose prices never leave the ceiling.
    /// </summary>
    public MarketRuleset Market { get; init; } = MarketRuleset.None;

    /// <summary>How far each Need moves, and how deep it may go. <c>[needs]</c>.</summary>
    /// <remarks>
    /// <b>Absent means no Household in this city has a Need</b>, reached by omitting the table rather
    /// than by a defaulted key — <see cref="Market"/>'s shape. Every Ruleset shipped before milestone
    /// 28 is such a city.
    /// </remarks>
    public NeedRuleset Needs { get; init; } = NeedRuleset.None;

    /// <summary>Which Need each Resource feeds, indexed by <c>resource - 1</c>.</summary>
    /// <remarks>
    /// <b>On the Resource rather than on the Rule, because it is a property of the thing consumed.</b>
    /// <c>04 §1</c> pairs Food with Sustenance and Consumer Goods with Satisfaction — the Good decides
    /// the Need, and any Rule drawing that Good on a Household's behalf is a shopping occasion for it.
    /// ⚠ <b>Empty means no Resource feeds a Need</b>, which is every Ruleset that omits
    /// <see cref="Needs"/>.
    /// </remarks>
    public Need[] ResourceNeeds { get; init; } = [];

    /// <summary>Which Need this Resource feeds, or <see cref="Need.None"/>.</summary>
    public Need NeedOf(ResourceId resource) =>
        resource.Raw >= 1 && resource.Raw <= ResourceNeeds.Length
            ? ResourceNeeds[resource.Raw - 1]
            : Need.None;

    /// <summary>
    /// The <c>[founding]</c> table, or <see cref="FoundingRuleset.None"/> when the file states none —
    /// which is a city in which no Household ever founds a shop.
    /// </summary>
    /// <remarks>
    /// <b>Absent is the ten shipped files and it is a real city rather than a broken one</b>
    /// (<c>adr/0145</c>): the founding channel is one of two ways a Business enters, so a file with no
    /// <c>[founding]</c> table still gets shops if it declares a gate. ⚠ <b>A file with NEITHER gets
    /// no Businesses at all</b>, which is every world that existed before milestone 27 task 8.
    /// </remarks>
    public FoundingRuleset Founding { get; init; } = FoundingRuleset.None;

    /// <summary>
    /// What the Hinterland at <paramref name="hinterland"/> charges for <paramref name="resource"/>.
    /// </summary>
    /// <remarks>
    /// <b>An index into <see cref="Hinterlands"/>, not a <see cref="MapEdge"/></b>, because this is
    /// the raw authored figure and the edge lookup is <see cref="TryHinterland"/>'s job. Zero for a
    /// Resource no <c>prices</c> entry names, which the loader permits only outside the
    /// <c>good</c> family.
    /// </remarks>
    /// <param name="hinterland">A position in <see cref="Hinterlands"/>.</param>
    /// <param name="resource">The Resource being priced.</param>
    public Money ImportPrice(int hinterland, ResourceId resource)
    {
        int index = (hinterland * ResourceCount) + resource.Raw - 1;
        return (uint)index < (uint)HinterlandPrices.Length ? HinterlandPrices[index] : default;
    }

    /// <summary>
    /// <b>The ceiling on what <paramref name="resource"/> can cost inside the city</b> — the lowest
    /// price any declared Hinterland charges for it (<c>adr/0050</c>, <c>adr/0135</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The minimum, and it is derived rather than chosen.</b> <c>adr/0135</c> ships no haulage
    /// term at 12, so importing from the far edge costs exactly what importing from the near one
    /// does and a rational city buys at the cheapest. ***With carriage free there is nothing to
    /// choose between four gates***, so <c>min</c> is the only answer that is not an invention.
    /// ⚠ <b>Its own revisit trigger is <c>adr/0133</c>'s charge shipping</b>, at which point the
    /// ceiling stops being a property of the Ruleset and becomes a property of a District — the
    /// signature moves, not just the body.
    /// </para>
    /// <para>
    /// <b>Zero when no Hinterland is declared</b>, and that is the honest answer rather than a
    /// missing one: a world with no door in it can import nothing, so no import bounds its prices.
    /// ⚠ <b>A zero ceiling pins every Pool price to zero</b>, which is why the loader refuses a file
    /// that states <c>[districts]</c> and leaves a Good unpriced — see <c>RulesetLoader</c>. ***An
    /// anchor that is absent does not fail loudly on its own***; it produces a market in which
    /// everything is free, and <c>adr/0050</c>'s runaway arrives from the other direction.
    /// </para>
    /// </remarks>
    /// <param name="resource">The Resource being priced.</param>
    public Money ImportCeiling(ResourceId resource)
    {
        Money lowest = default;
        bool found = false;
        for (int hinterland = 0; hinterland < Hinterlands.Length; hinterland++)
        {
            Money price = ImportPrice(hinterland, resource);
            if (!found || price < lowest)
            {
                lowest = price;
                found = true;
            }
        }

        return lowest;
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

    /// <inheritdoc cref="ResourceKeys"/>
    /// <remarks>
    /// <inheritdoc cref="ResourceKeys" path="/remarks"/>
    /// <para>
    /// ⚠ <b>A separate key family from <see cref="KindKeys"/>, and it has to be.</b> The two kind
    /// namespaces are independent (<c>adr/0141</c>), so a file may name a <c>[[building]]</c> and a
    /// <c>[[business]]</c> the same word. Hashing them into one family would make a bakery <em>the
    /// premises</em> on reload.
    /// </para>
    /// </remarks>
    public ulong[] BusinessKindKeys { get; init; } = [];

    /// <summary>What the Business kind with this id is, as a key comparable across Rulesets.</summary>
    public ulong BusinessKindKey(byte kind) =>
        BusinessKindKeys.Length == 0 ? kind : BusinessKindKeys[kind - 1];

    /// <summary>What each Business kind declares, indexed by <c>kind - 1</c>.</summary>
    /// <remarks>
    /// <b>An <c>init</c> property rather than a constructor argument, on
    /// <see cref="BusinessKindKeys"/>' precedent and for its reason.</b> The nine positional arrays
    /// are the structure a Ruleset is built from; the second kind namespace arrived after them and
    /// grows the same way it did. ⚠ <b>Empty is the ordinary case</b> — twelve of the fourteen shipped
    /// files declare no trade at all — and an empty array here means exactly what
    /// <see cref="BusinessKindCount"/> zero means.
    /// </remarks>
    public BusinessKindDefinition[] BusinessKinds { get; init; } = [];

    /// <summary>What the Business kind with this id declares.</summary>
    /// <remarks>
    /// ⚠ <b>Throws where <see cref="BusinessKindKey"/> defaults, and the difference is deliberate.</b>
    /// A key is asked for by migration code walking two Rulesets that may disagree about how many
    /// kinds exist, so it answers for an id it does not hold. <b>A definition is asked for by a caller
    /// that has already resolved a live Business's kind column</b>, where an out-of-range id is a
    /// corrupt row rather than a question — <see cref="Kind"/>'s shape exactly.
    /// </remarks>
    public BusinessKindDefinition BusinessKind(byte kind)
    {
        if (kind == 0 || kind > BusinessKinds.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                $"no Business kind carries id {kind}; this Ruleset declares {BusinessKinds.Length}.");
        }

        return BusinessKinds[kind - 1];
    }

    /// <summary>What each Life Stage declares, indexed by <c>stage - 1</c>.</summary>
    /// <remarks>
    /// <b>Empty is the ordinary case and means the city has no demographics at all.</b> Every shipped
    /// file declares none, on <c>[[hinterland]]</c>'s precedent: a mechanism arrives as a table a
    /// Ruleset may state rather than as a default every world inherits. ⚠ <b>A world declaring no
    /// stage never advances one</b>, which is what keeps <c>plans/0046</c> stage 1 off thirteen
    /// standing baselines.
    /// </remarks>
    public LifeStageDefinition[] LifeStages { get; init; } = [];

    /// <summary>How many Life Stages are declared. Ids run <c>1..LifeStageCount</c>.</summary>
    public int LifeStageCount { get; init; }

    /// <summary>
    /// The band a Citizen's age is drawn from when it becomes an adult, or <c>false</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One band for the world, found rather than authored as one.</b> It is stated on the stage a
    /// <c>children_become</c> names, because that is the stage adults are formed in — and in practice
    /// exactly one stage is ever named, so the scan finds one band. ⚠ <b>The FIRST is taken if a file
    /// states several</b>, which is a Ruleset saying that adults formed in different stages carry
    /// different ages; nothing in the design says that, and nothing refuses it either.
    /// </para>
    /// <para>
    /// <b>Read by the populator as well as by the spawn</b>, and that is the point: a founding city
    /// whose Citizens all carried age zero would be a city of children under
    /// <c>plans/0046</c> stage 4's gate. ***Zero is the marker of childhood, so every adult needs a
    /// real one.***
    /// </para>
    /// </remarks>
    public bool TryAdultAge(out int min, out int max)
    {
        for (int i = 0; i < LifeStageCount; i++)
        {
            if (LifeStages[i].AdultAgeMaxDays > 0)
            {
                min = LifeStages[i].AdultAgeMinDays;
                max = LifeStages[i].AdultAgeMaxDays;

                return true;
            }
        }

        min = 0;
        max = 0;

        return false;
    }

    /// <summary>The authored name of each Life Stage, indexed by <c>stage - 1</c>.</summary>
    /// <remarks>
    /// <b>Held for the same reason <see cref="BusinessKindKeys"/> is</b>: a reload compares two
    /// Rulesets that may disagree about how many stages exist and about their order, and a name is the
    /// only thing that survives a renumbering. <c>Core</c> returns ids and never strings, so nothing
    /// in the simulation reads these — the shell and the migration do.
    /// </remarks>
    public ulong[] LifeStageKeys { get; init; } = [];

    /// <summary>The authored name of each Policy, indexed by its position in declaration order.</summary>
    /// <remarks>
    /// <para>
    /// <b>Held because <c>CommandKind.Govern</c> made a Policy the first one anything points
    /// at.</b> Until the verb shipped, this type's own remark was true — <em>nothing in a Ruleset
    /// refers to a Policy</em> — so declaration order was identity enough and the loader read the name
    /// for refusal text and threw it away. ***A governed amount is saved state naming a Policy across
    /// a reload***, and an index is not a name: inserting a <c>[[policy]]</c> above another shifts
    /// every index below it, and <see cref="RulesetShape"/> reports that as
    /// <c>RulesetChange.PolicyCount</c> without anything acting on it.
    /// </para>
    /// <para>
    /// ⚠ <b>Zero means unnameable rather than unnamed-and-therefore-first.</b> <c>name</c> stays
    /// non-required at the loader — no shipped file omits it, and making it required would be a
    /// refusal bought for a case nobody writes — so a Policy without one keys to zero and
    /// <c>CommandKind.Govern</c> refuses to address it by name. ***The constraint lands on the
    /// verb that needs identity, not on every file that declares a sweep.***
    /// </para>
    /// </remarks>
    public ulong[] PolicyKeys { get; init; } = [];

    /// <summary>What the Policy at this index is, as a key comparable across Rulesets.</summary>
    /// <remarks>
    /// <b>Zero for a Policy this Ruleset cannot name</b> — an unnamed table, or one from a Ruleset
    /// built in code, where <see cref="ResourceKeys"/>' <em>empty means positional</em> cannot apply
    /// because a governed row has to survive a renumbering and a position does not.
    /// </remarks>
    public ulong PolicyKey(int policy) =>
        policy >= 0 && policy < PolicyKeys.Length ? PolicyKeys[policy] : 0;

    /// <summary>Whether this Ruleset has demographics at all.</summary>
    public bool DeclaresLifeStages => LifeStageCount > 0;

    /// <summary>What the Life Stage with this id declares.</summary>
    /// <remarks>
    /// <b>Throws for <see cref="BusinessKind"/>'s reason.</b> A caller here has already resolved a
    /// live Household's <c>life_stage</c> column, so an out-of-range id is a corrupt row rather than
    /// a question. ⚠ <b>Zero is out of range on purpose</b> — it is the terminal marker on
    /// <see cref="LifeStageDefinition.NextStage"/> and the value a Household carries in a world with
    /// no stage table, and both are conditions a caller tests rather than looks up.
    /// </remarks>
    public LifeStageDefinition LifeStage(byte stage)
    {
        if (stage == 0 || stage > LifeStages.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stage),
                stage,
                $"no Life Stage carries id {stage}; this Ruleset declares {LifeStages.Length}.");
        }

        return LifeStages[stage - 1];
    }

    /// <summary>
    /// The taste of a Household that has no opinion — <b>the exact middle of the axis</b>, and the
    /// value every Household carries in a world that authors none.
    /// </summary>
    /// <remarks>
    /// <b>It is load-bearing that this is a real position on the axis and not a sentinel.</b> A
    /// Household here scores every candidate dwelling identically, so placement falls through to the
    /// first-with-room accept it made before <c>adr/0027</c> shipped. ***The neutral taste is what
    /// makes the mechanism a widening of the old behaviour rather than a replacement for it***, and
    /// it is why 27 of the 30 shipped Rulesets produce the same city, State Hash for State Hash,
    /// with this code in the build.
    /// </remarks>
    public const int CentralityNeutral = 1 << (Fixed.FractionalBits - 1);

    /// <summary>The percent that <see cref="CentralityNeutral"/> is written as in a Ruleset.</summary>
    public const int CentralityNeutralPercent = 50;

    /// <summary>
    /// Whether any Life Stage in this Ruleset states an opinion about centrality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The gate that keeps this mechanism out of every world that did not ask for it.</b>
    /// Placement checks it once and takes the old path when it is false — ⚠ <b>and the reason is
    /// the draw count rather than the score</b>. A scored accept looks at every candidate in the
    /// budget; the old accept stops at the first with room. Running the scored loop over a city of
    /// neutral Households would produce the same <em>choice</em> and a different <em>stream
    /// position</em>, and a mechanism nobody authored would move every State Hash in the corpus.
    /// </para>
    /// <para>
    /// ⚠ <b>A base of exactly <see cref="CentralityNeutralPercent"/> with a width of zero reads as
    /// silence on purpose.</b> An author who states the neutral value explicitly gets the neutral
    /// city, which is the same city as saying nothing — there is no third state where the key is
    /// present and inert in some other way.
    /// </para>
    /// </remarks>
    public bool CentralityVaries
    {
        get
        {
            for (int stage = 0; stage < LifeStages.Length; stage++)
            {
                if (LifeStages[stage].CentralitySpreadPercent != 0
                    || LifeStages[stage].CentralityBasePercent != CentralityNeutralPercent)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Where this Household sits on the space-against-centrality axis, from <c>0</c> (wants room) to
    /// <see cref="Fixed.One"/> (wants the middle), <b>fixed for the Household's whole life</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0027</c> in one expression: the stage supplies a base and a width, the Household
    /// supplies its position within them, and only the first of those three moves.</b> The position
    /// comes from a <see cref="PurposeTag.CentralityTaste"/> draw at
    /// <see cref="Quantities.Ticks.Zero"/> against the Household's monotonic id, so re-asking on any
    /// later Tick, in any stage, after any save and reload, returns the same fraction of the same
    /// range. ***Continuity of character costs no storage and cannot be forgotten by a writer.***
    /// </para>
    /// <para>
    /// ⚠ <b>Stage <c>0</c> answers <see cref="CentralityNeutral"/> rather than throwing</b>, unlike
    /// <see cref="LifeStage(byte)"/> beside it, and the difference is deliberate. A world declaring
    /// no <c>[[life_stage]]</c> leaves every Household at stage zero for ever
    /// (<c>HouseholdTable.LifeStage</c>), and asking such a Household what it wants is an ordinary
    /// question with an ordinary answer — <em>nothing in particular</em>. Throwing would make this
    /// method unaskable in 27 of the 30 shipped worlds.
    /// </para>
    /// <para>
    /// ⚠ <b>The result is a POSITION and not a weight, and the sign convention is only in the
    /// consumer.</b> Nothing here knows that placement turns it into <c>2T − Fixed.One</c>; this
    /// answers what the Household wants and never how strongly a scorer should act on it.
    /// </para>
    /// </remarks>
    /// <param name="key">The world seed.</param>
    /// <param name="entityId">The Household's monotonic id — never its slot, which is recycled.</param>
    /// <param name="stage">The Household's current Life Stage; <c>0</c> when the world has none.</param>
    public int CentralityTaste(WorldKey key, ulong entityId, byte stage)
    {
        if (stage == 0 || stage > LifeStages.Length)
        {
            return CentralityNeutral;
        }

        LifeStageDefinition definition = LifeStages[stage - 1];

        // The position within the stage's range, on [0, Fixed.One). Drawn at Tick zero so it is a
        // property of the Household rather than of the moment it was asked about.
        ulong position = Randomness.Draw(key, entityId, Ticks.Zero, PurposeTag.CentralityTaste)
            % (ulong)Fixed.One;

        // (base + spread * position) as a percent in Q16.16, then percent to fraction. The loader
        // refuses base + spread above 100, so this cannot exceed Fixed.One.
        long scaled = ((long)definition.CentralityBasePercent * Fixed.One)
            + ((long)definition.CentralitySpreadPercent * (long)position);

        return (int)IntegerMath.FloorDiv(scaled, 100);
    }

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
            Districts = Districts,
            Lots = Lots,
            Capacity = Capacity,
            Bands = Bands,
            Trips = Trips,
            Jobs = Jobs,
            Households = Households,
            Traffic = Traffic,
            Parking = Parking,
            Terrain = Terrain,
            Water = Water,
            Disasters = Disasters,
            Policies = Policies,
            Hinterlands = Hinterlands,
            HinterlandPrices = HinterlandPrices,
            Market = Market,
            Founding = Founding,
            ResourceKeys = ResourceKeys,
            KindKeys = KindKeys,
            BusinessKindCount = BusinessKindCount,
            BusinessKindKeys = BusinessKindKeys,
            BusinessKinds = BusinessKinds,
            LifeStageCount = LifeStageCount,
            LifeStageKeys = LifeStageKeys,
            PolicyKeys = PolicyKeys,
            Needs = Needs,
            ResourceNeeds = ResourceNeeds,
            LifeStages = LifeStages,
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

    /// <summary>How many Business kinds are declared. Ids run <c>1..BusinessKindCount</c>.</summary>
    /// <remarks>
    /// <para>
    /// <b>A count rather than a definition array, because a Business kind declares nothing yet.</b>
    /// <c>adr/0141</c> gives it <c>jobs</c>, shift hours and the wage — and all three belong to
    /// milestone 27's <em>task 7</em>, where the Workplace stops being a Building. Until one of them
    /// lands there is no field to put in a <c>BusinessKindDefinition</c>, and inventing the type empty
    /// would be a shape with nothing in it.
    /// </para>
    /// <para>
    /// ⚠ <b>What the kind buys before it declares anything is IDENTITY.</b> A Business row can name its
    /// trade, the name survives a reload through <see cref="BusinessKindKeys"/>, and
    /// <c>World.CreateBusiness</c> has a kind to create <em>from</em> — which is what milestone 27's
    /// task 8 needs and what its risk says is missing.
    /// </para>
    /// <para>
    /// ⚠ <b>It is an init property rather than a constructor parameter</b>, on
    /// <see cref="KindKeys"/>'s precedent: the constructor's nine positional parameters are named by
    /// every hand-built Ruleset in the test suite, and a tenth would edit all of them to say
    /// <em>nothing here</em>.
    /// </para>
    /// </remarks>
    public int BusinessKindCount { get; init; }

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

    /// <summary>Whether this Ruleset declares a Business kind with this id.</summary>
    /// <remarks>
    /// <b>The Business half of <see cref="Declares"/>, and derelict means the same thing here.</b> A
    /// Business whose trade a reloaded Ruleset no longer names keeps its row and its balance — the
    /// money is conserved either way (<c>adr/0024</c>) — and reads as a trade nobody declared, exactly
    /// as <c>02 §4.3</c> has a Building do.
    /// </remarks>
    public bool DeclaresBusiness(byte kind) => kind != 0 && kind <= BusinessKindCount;

    /// <summary>Which Need a Building of this kind is attended for; <c>None</c> if it is not one.</summary>
    /// <remarks>
    /// <b>Total over undeclared kinds rather than throwing</b>, which is <see cref="NeedOf"/>'s
    /// polarity and <c>02 §4.3</c>'s: a Building whose kind a reloaded Ruleset no longer names is
    /// derelict, and a derelict school is a Building nobody can attend rather than a load failure.
    /// </remarks>
    public Need ServedBy(byte kind) => Declares(kind) ? Kind(kind).Serves : Need.None;

    /// <summary>Whether any declared kind is attended for this Need.</summary>
    /// <remarks>
    /// <b>Asked once at a sweep's front door and not per Household.</b> A city whose Ruleset declares
    /// no school cannot fail a school run — there is no occasion to fail — so
    /// <c>ServiceEngine</c> must be silent rather than degrading everybody, which is the
    /// difference between <em>the city has no schools</em> and <em>the city's schools are
    /// unreachable</em>. ⚠ <b>It walks the kinds and is not cached</b>: the array is short, this runs
    /// once a Day, and a cached flag would be derived state with a rebuild nobody wrote.
    /// </remarks>
    public bool ServesAny(Need need)
    {
        if (need == Need.None)
        {
            return false;
        }

        for (int i = 0; i < _kinds.Length; i++)
        {
            if (_kinds[i].Serves == need)
            {
                return true;
            }
        }

        return false;
    }

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
