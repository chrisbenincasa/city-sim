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
    public static LayerRates Default => From(
        landValueTau: 8,
        sealingDecayTau: 0,
        pollutionDecayTicks: DefaultPollutionDecayTicks,
        pollutionPeriod: LayerSchedule.Default.IndustrialPollution.Period);

    /// <summary>
    /// One Day, which is what a shut-down factory's plume is meant to fade over.
    /// </summary>
    /// <remarks>
    /// <b>The duration is the stated number and the tau is derived from it</b>, which is the whole of
    /// <c>plans/0015</c>'s decision owed 2. Stating <c>128</c> instead would silently start meaning
    /// <em>two Days</em> the moment a designer changed the pollution period from 64 to 128, and slice 8
    /// is the slice that makes that a thing a designer can do while the city runs.
    /// </remarks>
    public const int DefaultPollutionDecayTicks = Ticks.PerDay;

    /// <summary>
    /// The rates a Ruleset declares, with pollution's decay authored as a <b>duration</b> and
    /// converted here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is decision owed 2 of <c>plans/0015</c>, and the conversion is the whole of it.</b>
    /// <see cref="PollutionTau"/> is a count of <em>scheduled updates</em>, so its value depends on
    /// the cadence — and slice 8 makes the cadence hot-reloadable. A file listing <c>128</c> as a
    /// literal would silently start meaning <em>two Days</em> the moment a designer changed the period
    /// from 64 to 128, with nothing to indicate it. So the file states the duration it actually means
    /// and the cadence it will be run at, and the tau is derived from both.
    /// </para>
    /// <para>
    /// <b>Ticks rather than Days, which is the one place this departs from the plan's
    /// recommendation.</b> Days is closer to the designer sentence — <em>a shut-down factory's plume
    /// fades over about a Day</em> — but it is a unit nothing else in a Ruleset uses, and any value
    /// under a Day would need the quoted-decimal machinery to express. Ticks is what every rate and
    /// interval in the file is already written in, and <c>8192</c> carries the comment <c>one Day</c>
    /// perfectly well.
    /// </para>
    /// <para>
    /// <b>Rounded rather than refused when the division is inexact.</b> A decay of 8,192 Ticks at a
    /// period of 100 is 81.92 updates, and a designer who writes that has said something meaningful;
    /// refusing it would make the two numbers secretly coupled. <see cref="IntegerMath.RoundDiv"/>
    /// states the rounding, which is the project's standing answer to this shape.
    /// </para>
    /// </remarks>
    /// <param name="landValueTau">Land value's time constant, in scheduled updates.</param>
    /// <param name="sealingDecayTau">Sealing's, in scheduled updates. Zero means never.</param>
    /// <param name="pollutionDecayTicks">
    /// How long a Cell's pollution source takes to be absorbed, <b>in Ticks</b>. Zero means never.
    /// </param>
    /// <param name="pollutionPeriod">The pollution cadence the decay will run at.</param>
    public static LayerRates From(
        int landValueTau, int sealingDecayTau, int pollutionDecayTicks, int pollutionPeriod) =>
        new(
            landValueTau,
            sealingDecayTau,
            pollutionDecayTicks <= 0 || pollutionPeriod <= 0
                ? 0
                : IntegerMath.RoundDiv(pollutionDecayTicks, pollutionPeriod));
}

/// <summary>
/// The Map Layer numbers that are <b>world-creation</b> rather than tuning: fixed when a world is
/// created, baked into the save, and a reload that changes one is refused.
/// </summary>
/// <remarks>
/// <para>
/// <b>A separate type so that the two categories are visible in the code and not only in a
/// document.</b> <c>adr/0015</c> splits Ruleset data into hot-reloadable tuning and world-creation
/// constants, and <see cref="LayerRuleset"/> carries both — the cadence is tuning, the kernel is not.
/// <c>adr/0044</c>'s withdrawn second half is the standing warning about what happens when that line
/// is drawn by argument instead of by test: it filed the cadence as world-creation-fixed while citing
/// <c>adr/0015</c> without running the membership test <c>adr/0015</c> states. A type boundary is
/// cheaper to keep straight than a paragraph.
/// </para>
/// <para>
/// <b>The membership test, run per number.</b> <em>Was existing simulation state recorded in units of
/// this constant?</em> A diffused Cell holds a convolution of its sources <em>through this kernel</em>,
/// so every stored pollution value is in units of it — changing the radius mid-run reinterprets every
/// Cell on the map. The cadence fails the same test, because the dirty set holds Cells rather than
/// Ticks and the next pass produces the field it would have produced anyway.
/// </para>
/// </remarks>
/// <param name="IndustrialPollutionMetres">The industrial pollution kernel's reach, in metres.</param>
public readonly record struct LayerConstants(int IndustrialPollutionMetres)
{
    /// <summary>
    /// <b>1,024 m, which is 8 Cells, and it is the bottom of a band rather than a derived figure.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.4</c> grounds the range in reality as <em>real plumes run 1–10 km</em> and states no
    /// kernel. This takes the low end, because it is the end a Cell grid can represent: at 10 km the
    /// radius is 79 Cells and the kernel touches 159 Cells per axis, which is most of a 128-Cell map.
    /// </para>
    /// <para>
    /// <b>The band fails the corpus's own guard rule and that is worth recording rather than
    /// resolving.</b> <c>02 §2.5</c> guard rule 1 is <em>two ranges more than ~5× apart means two
    /// fields wearing one name</em> — and 1–10 km is 10× apart. Either industrial pollution is two
    /// fields (a near plume and a regional haze), or the band describes the spread <em>across</em>
    /// industries rather than the reach of one. Nothing here can tell those apart, and neither can an
    /// argument; it wants a source. <b>UNRATIFIED</b>, and moving it out of a <c>const</c> and into a
    /// file did not ratify it — the <c>plans/0002</c> §D row stays open.
    /// </para>
    /// </remarks>
    public static LayerConstants Default => new(IndustrialPollutionMetres: 1_024);
}

/// <summary>
/// Everything about the Map Layers that comes from the Ruleset: the cadence, the rates, and the
/// world-creation constants.
/// </summary>
/// <remarks>
/// <para>
/// <b>One type, because these numbers are read together and reloaded together</b> (<c>adr/0015</c>).
/// The Ruleset is swapped at a phase boundary and recorded in the Input Log, so a Layer subsystem that
/// took its cadence from one place and its rates from another would have two reload paths and one of
/// them would eventually be forgotten.
/// </para>
/// <para>
/// <b>Two categories in one type, and the split is <see cref="Constants"/>.</b>
/// <see cref="Schedule"/> and <see cref="Rates"/> are hot-reloadable tuning; <see cref="Constants"/>
/// is fixed when a world is created and a reload that changes it is <em>refused</em>. Both live here
/// because they are one file's worth of Layer data and a designer edits them in one sitting — and
/// keeping them apart in the type is what lets the reload path apply one while refusing the other
/// without either being a special case somebody has to remember.
/// </para>
/// <para>
/// <b>It is a constructor argument of <c>World</c> as well as a member of <c>Ruleset</c>, and that is
/// not redundancy.</b> Slice 8 made a Ruleset carry it, which is what lets a reload change a cadence
/// at all. The explicit argument stays because it is what let <c>adr/0044</c> build two worlds
/// differing <em>only</em> in cadence and compare their hash traces — a measurement no Ruleset-shaped
/// door makes convenient, and the one that settled whether the cadence was hash-bearing.
/// </para>
/// </remarks>
public readonly struct LayerRuleset
{
    /// <param name="schedule">When each Layer is recomputed.</param>
    /// <param name="rates">How fast each Layer moves when it is.</param>
    public LayerRuleset(LayerSchedule schedule, LayerRates rates)
        : this(schedule, rates, LayerConstants.Default)
    {
    }

    /// <inheritdoc cref="LayerRuleset(LayerSchedule, LayerRates)"/>
    /// <param name="schedule">When each Layer is recomputed.</param>
    /// <param name="rates">How fast each Layer moves when it is.</param>
    /// <param name="constants">The world-creation numbers, frozen for this world's life.</param>
    public LayerRuleset(LayerSchedule schedule, LayerRates rates, LayerConstants constants)
        : this(schedule, rates, constants, DesirabilityWeights.Default)
    {
    }

    /// <inheritdoc cref="LayerRuleset(LayerSchedule, LayerRates)"/>
    /// <param name="schedule">When each Layer is recomputed.</param>
    /// <param name="rates">How fast each Layer moves when it is.</param>
    /// <param name="constants">What is baked into the world rather than tuned.</param>
    /// <param name="desirability">The weights and noise parameters the composition reads.</param>
    public LayerRuleset(
        LayerSchedule schedule, LayerRates rates, LayerConstants constants, DesirabilityWeights desirability)
    {
        Schedule = schedule;
        Rates = rates;
        Constants = constants;
        Desirability = desirability;
    }

    /// <summary>What <see cref="MapLayers.Desirability"/> composes with. <b>Tuning</b>, and all of it
    /// unratified.</summary>
    public DesirabilityWeights Desirability { get; }

    /// <summary>The stated defaults of <c>02 §2.4</c>.</summary>
    public static LayerRuleset Default { get; } =
        new(LayerSchedule.Default, LayerRates.Default, LayerConstants.Default);

    /// <summary>When each Layer is recomputed. <b>Tuning</b> — hot-reloadable.</summary>
    public LayerSchedule Schedule { get; }

    /// <summary>How fast each Layer moves when it is. <b>Tuning</b> — hot-reloadable.</summary>
    public LayerRates Rates { get; }

    /// <summary><b>World-creation.</b> Frozen at world creation; a reload that changes it is refused.</summary>
    public LayerConstants Constants { get; }
}
