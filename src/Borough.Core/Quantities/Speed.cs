using Borough.Core.Arithmetic;

namespace Borough.Core.Quantities;

/// <summary>
/// A speed, in Q16.16 Tiles per Tick. <c>02 §2</c>: <i>"Vehicle speed is stored as Tiles per
/// Tick."</i>
/// </summary>
/// <remarks>
/// <para>
/// <b>The second of the three quantities <c>adr/0071</c> carries at the Q16.16 scale</b>, and the one
/// that was already being stored at it before the ADR existed:
/// <c>spikes/S2.Routing/Graph/Units.cs</c> held free-flow speed here while <c>05 §3</c> forbade the
/// representation, which is the contradiction that ADR resolved. Whole Tiles/Tick would round a
/// walking pace of 3.66 to 3 — a 20% error on the mode the whole pedestrian layer is made of.
/// </para>
/// <para>
/// <b>The exchange rate from km/h runs outside a Tick, but the assumption behind it does not, and
/// those are different claims.</b> <see cref="FromKilometresPerHour"/> exists for the loader, where a
/// human authors a number; the conversion runs once at load and its output then runs in every Tick.
/// So <c>02 §2</c>'s <i>"there are no seconds in the library and no metres"</i> is true of this
/// file's <em>arithmetic</em> and false of its <em>premise</em> — a fact this comment used to elide by
/// quoting the rule it depends on breaking, on the same screen.
/// </para>
/// <para>
/// <b>A Tick's duration in seconds is DERIVED, not free</b> (<c>adr/0082</c>). The Ruleset authors
/// speeds in km/h, <c>05 §26</c> fixes a Tile at ~4 m, and <c>02 §2</c> mandates Tiles/Tick — three
/// quantities that determine the fourth. A Tick is <b>42.1875 s of in-world time</b>, because a Day
/// is 24 in-world hours over <see cref="Ticks.PerDay"/>. What remains genuinely free is the
/// <em>other</em> Ticks→seconds rate, the host's wall-clock one, which is the speed ladder and which
/// nothing here reads. <c>adr/0019</c> had one table row where there are two rates, which is how a
/// Tick came to have three different durations across the corpus.
/// </para>
/// <para>
/// <b>A Tick is a behavioural unit and car-following does not use it.</b> At 42.1875 s a car clears
/// a 128 m Segment in <b>0.22</b> Ticks, which is far too coarse for Lane queues to form. ⚠ This read
/// <em>10.546875 s</em> and <em>0.9 Ticks</em> until 2026-08-19 — the 8192-Tick Day <c>adr/0094</c>
/// retired — and the correction makes the argument <b>stronger</b> rather than weaker, which is why
/// nothing downstream of it moves. <c>03 §3</c> already carries the same figure the other way up: the
/// crossing rate is <b>~4.6</b> Segments a Tick, not <c>adr/0041</c>'s <em>about one</em>. The Lane kernel
/// therefore takes an integer <b>sub-step ratio</b> inside Tick phase 4 (<c>adr/0082</c>); making that
/// resolution global costs 108× the measured Tick budget. Walking needs nothing from this — a walk Leg
/// is a departure, an arrival and a cost (<c>03 §3.8</c>).
/// </para>
/// <para>
/// <b>It does not multiply by its own kind.</b> A speed times a speed is not a quantity this design
/// has, so the operator is absent rather than present-and-discouraged — <see cref="Ratio"/> is where
/// <c>fixed × fixed</c> is legal, and scaling a speed by one is offered below.
/// </para>
/// </remarks>
public readonly record struct Speed(int Raw) : IComparable<Speed>
{
    /// <summary>
    /// Q16.16 Tiles/Tick per km/h, exactly — <b>192,000 at <see cref="Ticks.PerDay"/> = 2048</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Tile is <see cref="Tiles.Metres"/> and a Day is <see cref="Ticks.SecondsPerDay"/> over
    /// <see cref="Ticks.PerDay"/> Ticks, so a Tick is 42.1875 s. Then
    /// <c>Tiles/Tick = (km/h) × (1000/3600) × 42.1875 ÷ 4 = (km/h) × 2.9296875</c>, and
    /// <c>× 65536</c> gives 192,000 exactly. <c>SpeedTests</c> asserts the exactness rather than
    /// trusting this paragraph.
    /// </para>
    /// <para>
    /// ⚠ <b>This was the literal <c>48,000</c> until 2026-08-13, and it is the one thing
    /// <c>adr/0094</c> got wrong about its own change.</b> That ADR says the Goods quantities are
    /// <em>"the only rescaling in the change"</em> and classifies every number by what it is
    /// denominated in — but a <em>conversion factor</em> is denominated in both units at once, so it
    /// belongs to no class in that table and was not enumerated. Left at 48,000 with a Day of 2048,
    /// an authored <c>walk_speed_kph = 5</c> would have walked the city at <b>1.25 km/h</b>: every
    /// commute four times longer in clock minutes, and a Commute Budget refusing almost every job.
    /// <b>Nothing would have failed to compile and no Ruleset text would have been wrong.</b>
    /// </para>
    /// <para>
    /// <b>It is derived now, which is the actual fix.</b> <see cref="TravelTime"/> runs the same
    /// derivation and had always written it as one — its own remark says <em>"the same derivation
    /// <see cref="Speed"/> runs, and if one moves both move"</em>. <b>One fact, stated in two files,
    /// spelled as an expression in one and as a value in the other; the value is the copy that
    /// drifted.</b> <c>plans/0012</c> Cause 1, in code rather than in prose.
    /// </para>
    /// <para>
    /// <b>The factor is ratified by observation</b> (<c>adr/0082</c>): at <c>walk_speed_kph = 5</c>
    /// <c>--trips</c> over <c>minimal.toml</c> reports a median walk of 1.4 min at 128 m and 18.3 min
    /// at 2,048 m — 5 km/h to the digit. When this file and a design document disagreed about how long
    /// a Tick is, <b>this file was the one that was right</b>, which is the reverse of the corpus's
    /// usual direction.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// <b><c>static readonly</c> rather than <c>const</c>, and the analyser is why.</b> <c>BOR0203</c>
    /// refuses a raw <c>/</c> anywhere outside <c>Borough.Core.Arithmetic</c>, including inside a
    /// constant initialiser — and <see cref="IntegerMath.FloorDiv"/> is a method call, which a
    /// <c>const</c> cannot hold. The division here is exact, so the floor takes nothing; stating it is
    /// the point. The JIT folds a <c>static readonly</c> of this shape, so the spelling costs nothing
    /// at the call site.
    /// </remarks>
    private static readonly int PerKilometrePerHour = (int)IntegerMath.FloorDiv(
        Fixed.One * 1_000L * Ticks.SecondsPerDay, 3_600L * Ticks.PerDay * Tiles.Metres);

    /// <summary>Stationary. Never a road's free-flow speed — a Segment nobody may traverse is a mask.</summary>
    public static Speed Zero => new(0);

    /// <summary>
    /// Converts an authored km/h to Tiles/Tick. <b>For the Ruleset loader, not for a Tick.</b>
    /// </summary>
    /// <remarks>
    /// <b>The guard here is the format's, not the designer's</b>, and the two were confused in this
    /// comment until a test read the prose and then read the code. Q16.16 holds a speed up to
    /// <b>44,739 km/h</b> — <c>Fixed.MaxValue ÷ 48,000</c> — so this refuses nothing a city would
    /// ever author, and a Ruleset asking for a 40,000 km/h Street gets one. What refuses an
    /// <em>implausible</em> road is <c>RulesetLoader</c>'s own ceiling of 682 km/h, which is where a
    /// sanity bound belongs: it can name the file and the line, and it can be argued about by a
    /// designer without touching arithmetic. <i>682 is what this comment used to claim was the format
    /// limit; it is 32,767 ÷ 48, which is the format's whole part divided by the factor with its
    /// thousand dropped — a plausible-looking number arrived at by an arithmetic slip.</i>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The speed is negative, or exceeds what Q16.16 can hold — 44,739 km/h.
    /// </exception>
    public static Speed FromKilometresPerHour(int kilometresPerHour)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(kilometresPerHour);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            kilometresPerHour, IntegerMath.FloorDiv(Fixed.MaxValue, PerKilometrePerHour));

        return new Speed(kilometresPerHour * PerKilometrePerHour);
    }

    /// <summary>The whole Tiles covered in one Tick, rounding toward negative infinity.</summary>
    public Tiles ToTilesPerTickFloor() => new(Fixed.ToIntFloor(Raw));

    /// <summary>
    /// The slower of two speeds. <b>Named because the choice is load-bearing rather than a helper.</b>
    /// </summary>
    /// <remarks>
    /// A Segment has one free-flow speed and two modes traverse it at different rates, which one
    /// column cannot express — and a second speed column would contradict either
    /// <c>CONTEXT.md</c> → Segment, which lists free-flow speed among a Segment's attributes in the
    /// singular, or <c>CONTEXT.md</c> → Road Graph's <i>one graph with mode masks</i>. The resolution
    /// is <c>min(the mode's own ceiling, the road's free-flow speed)</c>: a pedestrian walks at
    /// walking pace on a boulevard and in a lane alike, and a car is held to the road.
    /// </remarks>
    public static Speed SlowerOf(Speed left, Speed right) => left.Raw < right.Raw ? left : right;

    /// <inheritdoc/>
    public int CompareTo(Speed other) => Raw.CompareTo(other.Raw);

    /// <summary>Scales by a dimensionless ratio — the congestion case. The product is still a speed.</summary>
    public static Speed operator *(Speed value, Ratio ratio) => new(Fixed.Mul(value.Raw, ratio.Raw));

    /// <inheritdoc cref="op_Multiply(Speed,Ratio)"/>
    public static Speed operator *(Ratio ratio, Speed value) => value * ratio;

    public static bool operator <(Speed left, Speed right) => left.Raw < right.Raw;

    public static bool operator >(Speed left, Speed right) => left.Raw > right.Raw;

    public static bool operator <=(Speed left, Speed right) => left.Raw <= right.Raw;

    public static bool operator >=(Speed left, Speed right) => left.Raw >= right.Raw;
}
