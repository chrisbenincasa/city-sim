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
    /// <para>
    /// A count, not a field: <b>it has no kernel</b>, because nothing about one Cell's Sealing reaches
    /// its neighbours. Slice 6 task 6.
    /// </para>
    /// <para>
    /// ⚠ <b>It DOES have a cadence as of milestone 24 task 4, and this comment said it never would.</b>
    /// It said <em>"no kernel and no cadence, because it changes on build"</em> — true of the write and
    /// false of the read. Sealing goes up on build and comes back down on a schedule, because ground
    /// recovering is a thing that happens over time whether or not anything is built that Tick
    /// (<c>02 §2.4</c>, <c>CONTEXT.md</c> → Sealing). ***A quantity that only an event can raise still
    /// needs a clock to lower it.***
    /// </para>
    /// </remarks>
    Sealing = 2,

    /// <summary>
    /// Forest coming back on ground nothing is standing on. <b>Not a field, and not diffused.</b>
    /// </summary>
    /// <remarks>
    /// <b>Here for the same reason <see cref="Sealing"/> is, one milestone-task later.</b> It is a
    /// count per Cell rather than a field, it has no kernel and no range, and what puts it on this
    /// enum is that it happens <em>on a clock</em>: <c>adr/0022</c> — <em>"forest regrows on unsealed,
    /// unoccupied land — slowly"</em> — is a statement about elapsed time and about nothing else.
    /// ⚠ <b>Its cadence is a Day, like Sealing's, and its offset is not</b>, because a Tick carrying
    /// both would make the two halves of one loop fire together and there would be no reading of the
    /// map in between.
    /// </remarks>
    Woodland = 3,
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
    /// <summary>A Layer with no schedule, recomputed by an event instead.</summary>
    /// <remarks>
    /// ⚠ <b>Nothing uses it as of milestone 24 task 4.</b> This said <em>"Sealing's"</em> and Sealing
    /// now has a cadence, so the value is kept as a reachable state rather than deleted — a Ruleset
    /// cannot author it (a period below 1 is refused), which is what makes it a shape the code can
    /// express and a file cannot.
    /// </remarks>
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
    /// <param name="sealing">Sealing's decay cadence. Milestone 24 task 4.</param>
    public LayerSchedule(
        LayerCadence industrialPollution,
        LayerCadence landValue,
        LayerCadence sealing,
        LayerCadence woodland)
    {
        IndustrialPollution = industrialPollution;
        LandValue = landValue;
        Sealing = sealing;
        Woodland = woodland;
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
        landValue: new LayerCadence(Period: 256, Offset: 16),
        sealing: new LayerCadence(Period: Ticks.PerDay, Offset: 48),
        woodland: new LayerCadence(Period: Ticks.PerDay, Offset: 80));

    /// <summary>Industrial pollution's cadence.</summary>
    public LayerCadence IndustrialPollution { get; }

    /// <summary>Land value's cadence.</summary>
    public LayerCadence LandValue { get; }

    /// <summary>
    /// Sealing's decay cadence. <b>A Day, and the unit is the argument.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0044</c> makes a Layer cadence <b>the designer's number and not the profiler's</b>, and
    /// this one is the least arbitrary of the three: <c>CONTEXT.md</c> → Sealing states the intent in
    /// <b>Days</b> — <em>"floodplain may recover over hundreds of Days"</em> — so the pass that
    /// delivers it ticks in Days. ***A period in the unit the design states its intent in is a period
    /// nobody has to convert to check.*** ⚠ <b>Offset 48</b>, which is congruent to neither 0 mod 64
    /// nor 16 mod 256, so it lands on a Tick neither other Layer uses. That is the stagger and its
    /// only required property.
    /// </remarks>
    public LayerCadence Sealing { get; }

    /// <summary>How often forest grows back. <b>A Day, and offset 80.</b></summary>
    /// <remarks>
    /// <b>A Day for Sealing's reason and offset 80 for the opposite one.</b> The period is a Day
    /// because <c>adr/0022</c> states the intent in elapsed time and the Ruleset authors a duration in
    /// Days, so nothing converts. ⚠ <b>The offset must not be 48</b>: Sealing's decay opens the room
    /// that regrowth fills, so firing both on one Tick would run the two halves of that loop with no
    /// Tick in between for anything to read the map — and it would put two whole-map sweeps in one
    /// Tick's budget, which is the collision the stagger exists to prevent (<c>05 §9</c>). **80** is
    /// congruent to neither 0 mod 64 nor 16 mod 256 and is not 48, which is all four Layers require.
    /// </remarks>
    public LayerCadence Woodland { get; }

    /// <summary>The cadence of one Layer.</summary>
    public LayerCadence For(Layer layer) => layer switch
    {
        Layer.IndustrialPollution => IndustrialPollution,
        Layer.LandValue => LandValue,
        Layer.Sealing => Sealing,
        Layer.Woodland => Woodland,
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
/// <param name="PollutionTau">
/// How many scheduled updates the environment takes to absorb a Cell's pollution source
/// (<c>adr/0051</c>). <b>Zero means never</b>, which is the pre-<c>adr/0051</c> behaviour and is kept
/// reachable only so the accumulating case can be written in a test and watched to fail.
/// </param>
/// <param name="WoodlandRegrowthDays">
/// How many Days a <b>fully wooded</b> Cell, cleared to bare ground, takes to return to what the seed
/// laid. <b>Zero means never</b>, which is what every world before milestone 24 task 8b had.
/// ⚠ <b>The duration is denominated against a FULL Cell and a thinly wooded one returns sooner</b> —
/// see <see cref="WoodlandTilesPerPass"/>, which is where that caveat is argued.
/// </param>
public readonly record struct LayerRates(
    int LandValueTau, int PollutionTau, int WoodlandRegrowthDays)
{
    /// <summary>
    /// How many Tiles one scheduled pass puts back. <b>Linear, and floored at one Tile.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Linear rather than an exponential approach, and task 4 is why.</b> The two rates in this
    /// type are time constants, and a time constant is <em>not</em> the duration a designer means —
    /// milestone 24 task 4 shipped a caveat about the multiplier between them, got the multiplier
    /// wrong, and had it refuted by the first instrument that measured it
    /// (<c>plans/0042</c> <b>F12</b>). ***A linear rate makes the authored number and the felt number
    /// the same number***, so there is no multiplier to mis-quote and no caveat to travel wrong.
    /// </para>
    /// <para>
    /// <b>The Ruleset states the DURATION and this derives the step</b>, which is
    /// <c>adr/0059</c>'s rule and <c>pollution_decay_ticks</c>' precedent: authoring <em>2 Tiles a
    /// pass</em> would silently mean half the recovery the day somebody changed the cadence.
    /// </para>
    /// <para>
    /// ⚠ 🔴 <b>The step is ABSOLUTE, so the authored duration is the recovery time of a FULL Cell and
    /// not of every Cell.</b> A Cell whose <see cref="WoodlandCellTable.Potential"/> is a quarter of
    /// the Cell comes back in a quarter of the stated Days, because forest advances at so many Tiles a
    /// pass wherever it advances at all. ***Found by measuring rather than by reading***: the cost
    /// instrument put <b>26.6%</b> of the map's forest back in 65 passes of a 512-Day rate, where the
    /// duration alone predicts 12.7%. The alternative — scaling the step by each Cell's ceiling so
    /// every Cell takes the same <em>fraction</em> of the duration — is refused because it reintroduces
    /// a per-Cell division whose result rounds to zero on exactly the thinly wooded Cells it exists to
    /// slow down, which is the defect below arriving through the fix for it. ⚠ <b>So the key names a
    /// full Cell, and any figure quoted from it carries that clause</b> (<c>plans/0012</c> Cause 5).
    /// </para>
    /// <para>
    /// ⚠ <b>Floored at one Tile, and the floor is the same defect task 4 found wearing the other
    /// sign.</b> <c>RoundDiv(1024, days)</c> is zero for any duration past
    /// <see cref="CellGrid.TilesInCell"/>, so an authored 2,000 Days would put back <em>nothing, for
    /// ever</em> while reading as a very slow rate. The loader refuses above the Cell for that reason,
    /// and the floor is here so the refusal is a second line of defence rather than the only one.
    /// </para>
    /// </remarks>
    public int WoodlandTilesPerPass => WoodlandRegrowthDays <= 0
        ? 0
        : Max(1, IntegerMath.RoundDiv(CellGrid.TilesInCell, WoodlandRegrowthDays));

    private static int Max(int a, int b) => a > b ? a : b;

    /// <summary>
    /// <b>Pollution's tau is derived rather than picked, and the derivation is the whole argument for
    /// it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ✅ <b>Sealing's rate left this type at milestone 24 task 4, and where it went is the point.</b>
    /// This paragraph used to explain why it was a single global pinned at zero: <c>02 §2.4</c> keys
    /// the rate <b>by terrain type</b>, there was no terrain in Phase 1, so there was no key to look
    /// one up with. **There is terrain now**, so the rate lives on <c>[[terrain]]</c> beside the type
    /// it is keyed by — <see cref="Rules.TerrainRuleset.SealingDecayTau"/> — and
    /// <c>[layers] sealing_decay_tau</c> is <b>gone rather than defaulted</b>, refused at load with a
    /// message naming where it moved. ***A stated absence is discharged by building the thing, not by
    /// keeping the placeholder and changing its value.***
    /// </para>
    /// <para>
    /// It is <b>32</b>, which is <c>TICKS_PER_DAY ÷ the pollution cadence</c> — 2048 ÷ 64 — so it is
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
        pollutionDecayTicks: DefaultPollutionDecayTicks,
        pollutionPeriod: LayerSchedule.Default.IndustrialPollution.Period,
        woodlandRegrowthDays: 0);

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
    /// interval in the file is already written in, and <c>2048</c> carries the comment <c>one Day</c>
    /// perfectly well.
    /// </para>
    /// <para>
    /// ⚠ <b>The two figures above said <c>128</c> and <c>8192</c> until 2026-08-24</b>, which is what
    /// a Day was before <c>adr/0094</c> made it 2,048 Ticks. The derivation was right and survived the
    /// move; the digits beside it did not, because a doc-comment is invisible to every mechanical check
    /// in <c>tests/Borough.Tests/Corpus/</c> — those are document-to-document. <c>plans/0012</c>
    /// <b>Cause 1</b>, found while task 4 was reading this type for a different reason.
    /// </para>
    /// <para>
    /// <b>Rounded rather than refused when the division is inexact.</b> A decay of 2,048 Ticks at a
    /// period of 100 is 81.92 updates, and a designer who writes that has said something meaningful;
    /// refusing it would make the two numbers secretly coupled. <see cref="IntegerMath.RoundDiv"/>
    /// states the rounding, which is the project's standing answer to this shape.
    /// </para>
    /// </remarks>
    /// <param name="landValueTau">Land value's time constant, in scheduled updates.</param>
    /// <param name="pollutionDecayTicks">
    /// How long a Cell's pollution source takes to be absorbed, <b>in Ticks</b>. Zero means never.
    /// </param>
    /// <param name="pollutionPeriod">The pollution cadence the decay will run at.</param>
    public static LayerRates From(
        int landValueTau,
        int pollutionDecayTicks,
        int pollutionPeriod,
        int woodlandRegrowthDays) =>
        new(
            landValueTau,
            pollutionDecayTicks <= 0 || pollutionPeriod <= 0
                ? 0
                : IntegerMath.RoundDiv(pollutionDecayTicks, pollutionPeriod),
            woodlandRegrowthDays);
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
        : this(schedule, rates, constants, desirability, FertilityWeights.Default)
    {
    }

    /// <inheritdoc cref="LayerRuleset(LayerSchedule, LayerRates)"/>
    /// <param name="schedule">When each Layer is recomputed.</param>
    /// <param name="rates">How fast each Layer moves when it is.</param>
    /// <param name="constants">What is baked into the world rather than tuned.</param>
    /// <param name="desirability">The weights and noise parameters that composition reads.</param>
    /// <param name="fertility">The one weight <see cref="MapLayers.Fertility"/> composes with.</param>
    public LayerRuleset(
        LayerSchedule schedule,
        LayerRates rates,
        LayerConstants constants,
        DesirabilityWeights desirability,
        FertilityWeights fertility)
    {
        Schedule = schedule;
        Rates = rates;
        Constants = constants;
        Desirability = desirability;
        Fertility = fertility;
    }

    /// <summary>What <see cref="MapLayers.Desirability"/> composes with. <b>Tuning</b>, and all of it
    /// unratified.</summary>
    public DesirabilityWeights Desirability { get; }

    /// <summary>
    /// What <see cref="MapLayers.Fertility"/> composes with. <b>Tuning</b>, and unratified.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>One weight and not two</b> — the Sealing coefficient is derived from an endpoint and has
    /// no key at all (<c>adr/0155</c>). See <see cref="FertilityWeights"/>.
    /// </remarks>
    public FertilityWeights Fertility { get; }

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
