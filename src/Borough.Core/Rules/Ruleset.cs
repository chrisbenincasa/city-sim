namespace Borough.Core.Rules;

using Borough.Core.Arithmetic;
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
public readonly record struct MapEmission(Layer Layer, int Amount);

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
    /// <b>What <c>with</c> would give a record, spelled by hand because this is a class.</b> It exists
    /// for the two callers that legitimately hold Rules and Layer data separately: a world constructed
    /// from a stated cadence and no Rules — <c>adr/0044</c>'s measurement door — and a test that
    /// reloads a Ruleset differing only in cadence. The arrays are shared rather than copied because a
    /// loaded Ruleset is immutable.
    /// </remarks>
    public Ruleset WithLayers(LayerRuleset layers) =>
        new(_resources, _rules, _kinds, _inputs, _outputs, _emissions, _bins, _kindRules, _zoneRules)
        {
            Layers = layers,
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

    /// <summary>One Building kind, or a throw.</summary>
    /// <exception cref="ArgumentOutOfRangeException">No kind carries that id.</exception>
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
