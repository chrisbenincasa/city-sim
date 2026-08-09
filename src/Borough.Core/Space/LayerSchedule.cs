using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// The Map Layers that exist. Only what is actually a Layer under <c>adr/0034</c>'s classification.
/// </summary>
/// <remarks>
/// <b>Noise and near-road pollution are deliberately absent, and this is the enum somebody would add
/// them to by reflex.</b> They are <em>line sources</em>: short-ranged, logarithmic, 50–300 m, with
/// the whole gradient inside one Cell — so a Cell-resolution field degrades into <em>is there a road
/// here</em>. A line source is a distance query, exact at Tile resolution, and quantising it to any
/// grid is worse than not quantising it. Finer Cells were considered and rejected. See
/// <c>02 §2.5</c>, whose procedure exists because <em>"add a Map Layer" was the reflex answer four
/// times running and was the right answer once</em>.
/// </remarks>
public enum Layer : byte
{
    /// <summary>Point sources, plumes ≫ a Cell, superposing and isotropic. Diffused.</summary>
    IndustrialPollution = 0,

    /// <summary>
    /// Stored because it has <em>momentum</em> — the one field that is not composed at the point of use.
    /// </summary>
    /// <remarks>
    /// It moves toward current desirability rather than tracking it, which is both realistic and a
    /// stabiliser against oscillation (<c>02 §2.4</c>). Slice 6 task 6.
    /// </remarks>
    LandValue = 1,

    /// <summary>
    /// The count of Tiles in a Cell ever built on. Stored per Cell and <b>not diffused</b>.
    /// </summary>
    /// <remarks>
    /// A count, not a field: it has no kernel and no cadence, because it changes on build.
    /// <see cref="LayerSchedule.IsDue"/> answers false for it forever, which is the honest answer
    /// rather than a period nobody reads. Slice 6 task 6.
    /// </remarks>
    Sealing = 2,
}

/// <summary>
/// How often one Layer is recomputed, and on which Tick of its cycle.
/// </summary>
/// <remarks>
/// <b>The offset is what stops a single Tick carrying every Layer at once</b> (<c>05 §9</c>): a spike
/// every 64 Ticks is a visible stutter and the same work spread across those 64 Ticks is not. It is a
/// stagger rather than a jitter — a function of the Tick and nothing else — so two runs of one log
/// recompute the same Layers on the same Ticks.
/// </remarks>
/// <param name="Period">Ticks between recomputations. Zero or negative never fires.</param>
/// <param name="Offset">Which Tick of the cycle it fires on.</param>
public readonly record struct LayerCadence(int Period, int Offset)
{
    /// <summary>A Layer with no schedule, recomputed by an event instead. Sealing's.</summary>
    public static LayerCadence Never => default;

    /// <summary>Whether this Layer is recomputed on the given Tick.</summary>
    public bool IsDue(Ticks tick) =>
        Period > 0 && tick.Raw % (ulong)Period == (ulong)(uint)Offset;
}

/// <summary>
/// The staggered schedule: <c>05 §9</c>'s slot, as a table rather than a scatter of magic numbers.
/// </summary>
/// <remarks>
/// <para>
/// <b>A table, because a cadence spread through Phase 5 is a cadence nobody can audit.</b> The
/// numbers here decide when a source's contribution becomes visible to a Rule that reads the Cell, and
/// the whole point of the <em>Decisions owed</em> section below is that somebody has to be able to run
/// <c>05 §4</c>'s State Hash test against each of them <b>by name</b>. A number embedded in an
/// <c>if</c> has no name.
/// </para>
/// <para>
/// <b>Measured, not argued, and the measurement said the numbers are hash-bearing</b>
/// (<c>adr/0044</c>). Two worlds identical but for the diffusion period produce two hash traces, so
/// under <c>05 §4</c> a change here is a design change: <b>the designer's number, not the
/// profiler's</b>. <c>05 §9</c> used to offer it as one of three performance multipliers, which is
/// the same welding failure <c>adr/0034</c> found in Chunk size, one section earlier in the same
/// document.
/// </para>
/// <para>
/// <b>Hash-bearing does not mean frozen, and the first draft of <c>adr/0044</c> got that wrong.</b>
/// These stay ordinary hot-reloadable Ruleset data. <c>adr/0015</c>'s world-creation category has a
/// stated membership test — <em>was existing simulation state recorded in units of the constant?</em>
/// — and a cadence fails it: a Cell holds a convolution of its sources, and the dirty set holds Cells,
/// not Ticks. So a period changed mid-run reinterprets nothing, and the next pass produces the field
/// it would have produced anyway. <b>The dirty set is what makes that true</b>, which is why
/// <c>adr/0034 §3</c>'s refusal of iterative relaxation and this classification stand or fall
/// together.
/// </para>
/// </remarks>
public readonly struct LayerSchedule
{
    /// <param name="industrialPollution">Pollution's cadence.</param>
    /// <param name="landValue">Land value's cadence.</param>
    public LayerSchedule(LayerCadence industrialPollution, LayerCadence landValue)
    {
        IndustrialPollution = industrialPollution;
        LandValue = landValue;
    }

    /// <summary>
    /// <c>02 §2.4</c>'s figures: pollution every 64 Ticks, land value every 256, staggered apart.
    /// </summary>
    /// <remarks>
    /// <b>Land value's offset is 16 rather than 0, and that is the whole of the stagger.</b> At offset
    /// 0 the two would coincide on every Tick divisible by 256 — four times a Day — putting both
    /// convolutions in one Tick's budget for no reason. 16 is coprime to nothing in particular; what
    /// it is, is not congruent to 0 modulo 64, which is the only property required.
    /// </remarks>
    public static LayerSchedule Default { get; } = new(
        industrialPollution: new LayerCadence(Period: 64, Offset: 0),
        landValue: new LayerCadence(Period: 256, Offset: 16));

    /// <summary>Industrial pollution's cadence.</summary>
    public LayerCadence IndustrialPollution { get; }

    /// <summary>Land value's cadence.</summary>
    public LayerCadence LandValue { get; }

    /// <summary>The cadence of one Layer. Sealing has none; it changes on build.</summary>
    public LayerCadence For(Layer layer) => layer switch
    {
        Layer.IndustrialPollution => IndustrialPollution,
        Layer.LandValue => LandValue,
        Layer.Sealing => LayerCadence.Never,
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>Whether a Layer is recomputed on the given Tick.</summary>
    public bool IsDue(Layer layer, Ticks tick) => For(layer).IsDue(tick);
}

/// <summary>
/// The rates the Layers move at, as whole-number time constants. Ruleset data.
/// </summary>
/// <remarks>
/// <b>Integer fractions with stated rounding, not <see cref="Ratio"/>.</b> A Q16.16 ratio is the right
/// type for a dimensionless scale factor and the wrong one here: a diffused Layer is stored in kernel
/// units and reaches six figures, so <c>Fixed.FromInt</c> on one would overflow the format's ±32,768
/// whole range. <c>05 §3</c>'s <em>Q16.16 is for sub-Tile positions and nothing else</em> is pointing
/// at the same fact from the other side. A time constant divides, and
/// <see cref="IntegerMath.RoundDiv"/> states how.
/// </remarks>
/// <param name="LandValueTau">
/// How many scheduled updates land value takes to close the gap to its target. Larger is slower.
/// </param>
/// <param name="SealingDecayTau">
/// How many updates Sealing takes to decay away. <b>Zero means never</b>, which is Phase 1's value.
/// </param>
/// <param name="PollutionTau">
/// How many scheduled updates the environment takes to absorb a Cell's pollution source
/// (<c>adr/0051</c>). <b>Zero means never</b>, which is the pre-<c>adr/0051</c> behaviour and is kept
/// reachable only so the accumulating case can be written in a test and watched to fail.
/// </param>
public readonly record struct LayerRates(int LandValueTau, int SealingDecayTau, int PollutionTau)
{
    /// <summary>
    /// Phase 1's rates. <b>Sealing does not decay, and that is a stated absence rather than a guess.</b>
    /// </summary>
    /// <remarks>
    /// <c>02 §2.4</c> says Sealing decays at a Ruleset rate <b>keyed by terrain type</b> — rock may
    /// never recover, floodplain may recover over hundreds of Days. There is no terrain in Phase 1, so
    /// there is no key, so there is no rate to look one up with. <b>Zero is the conservative answer
    /// rather than a placeholder</b>: it is the case where Sealing only accumulates, which is the one
    /// <c>adr/0006</c> would object to if the bound were not structural — and it is, because a Cell
    /// cannot have more Tiles built on it than it has Tiles.
    /// </remarks>
    /// <summary>
    /// <b>Pollution's tau is derived rather than picked, and the derivation is the whole argument for
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is <b>128</b>, which is <c>TICKS_PER_DAY ÷ the pollution cadence</c> — 8192 ÷ 64 — so it is
    /// <em>one Day, counted in the units the decay actually runs in</em>. That makes the designer-facing
    /// sentence "a shut-down factory's plume fades over about a Day", and it makes the number move
    /// correctly on its own if either constant it is built from ever changes. <c>adr/0044</c> is the
    /// standing warning about the alternative: the Layer cadence was two numbers picked to look
    /// reasonable, cited as settled by three documents, and had to be measured back out.
    /// </para>
    /// <para>
    /// <b>It is still unratified and it is still hash-bearing</b>, because a derivation is not a
    /// ratification — what is derived is the <em>time constant</em>, and what nobody has checked is
    /// whether a Day is the right period for a plume to fade over. Filed in <c>plans/0002</c> §D with
    /// its ratifier.
    /// </para>
    /// </remarks>
    public static LayerRates Default => new(LandValueTau: 8, SealingDecayTau: 0, PollutionTau: 128);
}

/// <summary>
/// Everything about the Map Layers that comes from the Ruleset: the cadence and the rates.
/// </summary>
/// <remarks>
/// <para>
/// <b>One type, because these numbers are read together and reloaded together</b> (<c>adr/0015</c>).
/// The Ruleset is swapped at a phase boundary and recorded in the Input Log, so a Layer subsystem that
/// took its cadence from one place and its rates from another would have two reload paths and one of
/// them would eventually be forgotten.
/// </para>
/// <para>
/// <b>It is a constructor argument of <c>World</c> rather than a constant, and that is what the
/// measurement needed.</b> Slice 8 will read it from the Ruleset; until then a caller supplies it, and
/// the ability to supply two of them is what let <c>adr/0044</c> compare two cadences' hash traces
/// instead of arguing about them.
/// </para>
/// </remarks>
public readonly struct LayerRuleset
{
    /// <param name="schedule">When each Layer is recomputed.</param>
    /// <param name="rates">How fast each Layer moves when it is.</param>
    public LayerRuleset(LayerSchedule schedule, LayerRates rates)
    {
        Schedule = schedule;
        Rates = rates;
    }

    /// <summary>The stated defaults of <c>02 §2.4</c>.</summary>
    public static LayerRuleset Default { get; } =
        new(LayerSchedule.Default, LayerRates.Default);

    /// <summary>When each Layer is recomputed.</summary>
    public LayerSchedule Schedule { get; }

    /// <summary>How fast each Layer moves when it is.</summary>
    public LayerRates Rates { get; }
}
