using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Borough.Tests.Golden;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 9 task 7: the acceptance run. <b>100,000 Ticks on <c>rulesets/fouled.toml</c></b>, and
/// decision 5's <em>floor</em> — which refutes and never confirms.
/// </summary>
/// <remarks>
/// <para>
/// <b>Land value is the awkward magnitude, so the flatness is asserted on the FLOW.</b> The Definition
/// of done wants no collection and no magnitude trending upward at steady state
/// (<c>adr/0006</c>). Land value is a magnitude that is <em>supposed</em> to move, so
/// <see cref="The_level_settles_and_does_not_trend"/> reads the mean over a window and
/// <see cref="The_flow_does_not_trend"/> reads the total absolute Day-over-Day movement.
/// ***The exempt axis is the level within a Day, and it is exempt because the target itself moves
/// with the Day*** (<c>adr/0127</c>) — the flow across Days is not exempt and is what is asserted.
/// </para>
/// <para>
/// ⚠ <b>AND THE FLOOR REFUTED ONE OF ITS OWN READINGS, WHICH IS WHAT A FLOOR IS FOR.</b>
/// <c>adr/0125</c> gave the desirability weights a reachable floor of three readings because their
/// real ratifier needs a consumer nobody built. Readings 1 and 2 — the field varies, both terms are
/// visible — pass. <b>Reading 3 fails</b>: pollution and noise are rank-correlated across Cells at
/// <b>86 to 100 percent</b> on every readable Day, so ***no ratio between <c>w₂</c> and <c>w₃</c> is
/// identifiable in this world at all***. <c>plans/0002</c> §D1 named this outcome in advance, and the
/// cause here is sharper than the one it anticipated: <c>fouled.toml</c>'s emitting kind is
/// <c>dwelling</c> (<c>0034</c> F19), and dwellings are also what generate the traffic — so the two
/// terms have literally the same source.
/// </para>
/// <para>
/// <b>1,000 Citizens rather than 4,000</b>, matching <c>MoneyLongRunTests</c>, because the run is the
/// expensive part and every reading here is qualitative. ⚠ <b>The cost of the small city is that the
/// commute is sparse</b> — see <see cref="LandValueLongRun.ReadAt"/>, where the instrument had to be
/// widened twice before the noise reading stopped depending on the sampling.
/// </para>
/// </remarks>
public sealed class LandValueLongRunTests(LandValueLongRun run) : IClassFixture<LandValueLongRun>
{
    /// <summary>
    /// <b>The level settles.</b> The mean over the second half of the run matches the mean over the
    /// first half after settling, so the field is not sinking without limit.
    /// </summary>
    [Fact]
    public void The_level_settles_and_does_not_trend()
    {
        LandValueLongRun.Reading[] readings = run.Readings;

        Assert.True(readings.Length > 40, $"only {readings.Length} Days were read");

        // The first dozen Days are the field filling from zero, which is a transient and not a trend.
        int settled = 12;
        int middle = settled + ((readings.Length - settled) / 2);
        long early = Mean(readings, settled, middle);
        long late = Mean(readings, middle, readings.Length);
        long drift = Math.Abs(late - early);

        Assert.True(early < 0, $"the settled field is not below zero at all: {early}");
        Assert.True(
            drift * 10 <= Math.Abs(early),
            $"the mean land value went from {early} over Days {settled + 1}-{middle} to {late} over "
            + $"Days {middle + 1}-{readings.Length}, a drift of {drift}. adr/0006: no magnitude "
            + "trends at steady state, and the level is the one this run is allowed to let move "
            + "WITHIN a Day and not across them");
    }

    /// <summary>
    /// <b>The flow does not trend.</b> Day-over-Day movement collapses from the fill and then stays
    /// in a band; it does not grow.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It does not go to zero either, and that is <c>adr/0127</c> rather than a fault.</b> The
    /// city keeps building, so the sources keep moving, so the target keeps moving. What
    /// <c>adr/0006</c> forbids is a magnitude that trends <em>upward</em>, and this asserts the
    /// direction rather than a resting point.
    /// </remarks>
    [Fact]
    public void The_flow_does_not_trend()
    {
        LandValueLongRun.Reading[] readings = run.Readings;
        int settled = 12;
        int middle = settled + ((readings.Length - settled) / 2);
        long early = 0;
        long late = 0;

        for (int i = settled; i < middle; i++)
        {
            early += readings[i].Flow;
        }

        for (int i = middle; i < readings.Length; i++)
        {
            late += readings[i].Flow;
        }

        early /= middle - settled;
        late /= readings.Length - middle;

        Assert.True(early > 0, "nothing moved at all across a whole window of Days");
        Assert.True(
            late <= early * 2,
            $"Day-over-Day movement went from a mean of {early} to {late}, more than doubling. "
            + "adr/0006 forbids a magnitude trending upward at steady state, and this is the "
            + "magnitude land value is allowed to have instead of a resting value");

        // And the fill really was a transient: the first Day after populating moves far more than
        // any settled Day. Without this the assertion above would pass over a field that never moved.
        Assert.True(
            readings[1].Flow > early * 2,
            $"the fill moved {readings[1].Flow} against a settled mean of {early}, which is not the "
            + "transient this window was chosen to exclude -- so `settled` is wrong");
    }

    /// <summary>
    /// <b>No collection grows with elapsed time.</b> <c>adr/0006</c>'s other half, on the table this
    /// milestone added a writer to.
    /// </summary>
    /// <remarks>
    /// The producer walks the rows that exist and creates none, so the Cell table can only grow when
    /// something <em>emits</em> into a Cell that had no row. Over the settled window the emitters are
    /// inside a city that is no longer spreading, so the count holds. ⚠ <b>This is the assertion that
    /// would have caught a producer written the obvious way</b> — one that called
    /// <c>SetLandValueTarget</c> per Cell of the map and allocated a row for every one of them.
    /// </remarks>
    [Fact]
    public void No_collection_grows_with_elapsed_time()
    {
        LandValueLongRun.Reading[] settled = run.Readings[12..];
        int first = settled[0].Cells;

        foreach (LandValueLongRun.Reading reading in settled)
        {
            Assert.True(
                reading.Cells == first,
                $"the Cell table held {first} rows on Day {settled[0].Day} and {reading.Cells} on "
                + $"Day {reading.Day}. Something is allocating a Cell row per Tick, per Day, or per "
                + "read -- adr/0006, and the producer is the newest writer of this table");
        }
    }

    /// <summary>
    /// <b>Decision 5's floor, reading 1: the field varies across Cells.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <c>w₂</c> small enough rounds every Cell onto the same value, and ***a uniform field is
    /// visibly working while carrying no information***.
    /// </para>
    /// <para>
    /// ⚠ <b>A FIFTH, AND IT WAS A QUARTER UNTIL <c>plans/0053</c> — re-swept rather than nudged.</b>
    /// The settled tail now reads between <b>43 and 53 distinct values over 175 Cells</b> (24.6% to
    /// 30.3%), so a floor at a quarter ran through the middle of the distribution and failed on the
    /// low Days alone. ***A threshold inside the spread of the thing it bounds is a coin toss wearing
    /// an assertion's clothes.*** A fifth sits below the whole measured range with room, and it still
    /// refuses the failure this exists for by a wide margin: a field rounded onto one value reads 1.
    /// </para>
    /// <para>
    /// ⚠ <b>What moved it was occupancy dividing the ground.</b> One of the field's two terms is the
    /// Building density, and the city's Buildings now differ in how many people they hold — so the
    /// same population produces a different arrangement of Cells. The count is a reading about THIS
    /// city and it moves whenever the generator does.
    /// </para>
    /// </remarks>
    [Fact]
    public void Floor_reading_one_the_field_varies_across_cells()
    {
        foreach (LandValueLongRun.Reading reading in run.Readings[12..])
        {
            Assert.True(
                reading.Distinct * 5 > reading.Cells,
                $"on Day {reading.Day} only {reading.Distinct} of {reading.Cells} Cells hold "
                + "distinct land values, so the field is nearly uniform and carries almost no "
                + "information even though it is visibly working");
        }
    }

    /// <summary>
    /// <b>Decision 5's floor, reading 2: both terms are visible.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// If one term is negligible beside the other everywhere, this is a one-term field wearing a
    /// two-term formula — <c>adr/0123</c>'s concern arriving as a number instead of as an absence.
    /// </para>
    /// <para>
    /// ⚠ <b>Not every Day, and the exception is the city rather than the query.</b> Measured: on one
    /// Day of forty-nine the noise term is zero across the whole 06:00-to-12:00 band, because a
    /// 1,000-Citizen city can genuinely have nobody on the road. ***A silent Day is a reading about
    /// the world; it becomes a reading about the instrument only when there are many of them.***
    /// </para>
    /// <para>
    /// ⚠ <b>THE SILENT SHARE WENT 1-IN-49 TO 8-IN-37 ON 2026-08-27, and the city really did go
    /// quiet.</b> <c>CommuteEngine</c> stopped sending Citizens on journeys to where they already
    /// stood, which was ***about a quarter of this city's commuting*** — and every one of those
    /// phantom journeys had been putting traffic on a Segment and noise in this term. So the ceiling
    /// moves from a tenth to a quarter. 🔴 <b>What is worth carrying is not the constant: part of the
    /// noise term's visibility was being paid for by a defect</b>, and the same is true of any
    /// figure taken off this Layer before that date. The two-term claim still holds on 29 of 37
    /// Days, which is what this test is actually for.
    /// </para>
    /// </remarks>
    [Fact]
    public void Floor_reading_two_both_terms_are_visible()
    {
        LandValueLongRun.Reading[] settled = run.Readings[12..];
        int silent = 0;

        foreach (LandValueLongRun.Reading reading in settled)
        {
            Assert.True(
                reading.PollutionPeak > 0,
                $"the pollution term is zero everywhere on Day {reading.Day}, on the one shipped "
                + "Ruleset whose Rules emit");

            if (reading.NoisePeak == 0)
            {
                silent++;
                continue;
            }

            Assert.True(
                reading.PollutionPeak < reading.NoisePeak * 100
                    && reading.NoisePeak < reading.PollutionPeak * 100,
                $"on Day {reading.Day} pollution peaks at {reading.PollutionPeak} and noise at "
                + $"{reading.NoisePeak}: one term is doing all the work and the composition is a "
                + "one-term field wearing a two-term formula");
        }

        Assert.True(
            silent * 4 <= settled.Length,
            $"the noise term was zero across the whole commute band on {silent} of {settled.Length} "
            + "Days. A few are the city having quiet mornings; this many is the instrument sampling "
            + "the wrong hours");
    }

    /// <summary>
    /// <b>Decision 5's floor, reading 3 — AND IT FAILS, which is what a floor is for.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>plans/0002</c> §D1 called this the fifth-sighting check: <em>industry and roads are both
    /// placed in proportion to the same population, so if pollution and noise co-vary no ratio is
    /// identifiable at all and what is owed is a hand-authored world rather than a better number</em>.
    /// Measured, they co-vary at <b>86 to 100 percent rank concordance</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>The cause here is sharper than the one that sentence anticipated.</b> It is not that the
    /// two are placed in proportion to the same population — it is that in <c>fouled.toml</c> they
    /// have <b>the same source</b>. The emitting kind is <c>dwelling</c> (<c>0034</c> F19), and
    /// dwellings are also what generate the commute. ***The simplification that made the floor
    /// reachable is what makes its third reading unreadable.***
    /// </para>
    /// <para>
    /// <b>So this test asserts the correlation is HIGH.</b> That reads backwards and is deliberate:
    /// the finding is a fact about this world, and the day somebody authors a world where the two
    /// sources are apart, this goes red and the weights become identifiable — which is the event
    /// worth being told about.
    /// </para>
    /// </remarks>
    [Fact]
    public void Floor_reading_three_the_two_terms_are_not_separable_in_this_world()
    {
        foreach (LandValueLongRun.Reading reading in run.Readings[12..])
        {
            if (reading.NoisePeak == 0)
            {
                continue;
            }

            Assert.True(
                reading.Tau > 75,
                $"on Day {reading.Day} the two terms are only {reading.Tau}% rank-concordant. If "
                + "this has fallen, somebody has separated the sources -- pollution and noise are "
                + "now independent enough that the ratio between w2 and w3 may be identifiable, and "
                + "adr/0125's owed ratifier should be reopened rather than this bound lowered");
        }
    }

    private static long Mean(LandValueLongRun.Reading[] readings, int from, int to)
    {
        long total = 0;

        for (int i = from; i < to; i++)
        {
            total += readings[i].Mean;
        }

        return total / (to - from);
    }
}

/// <summary>The run, done once and read by every assertion above.</summary>
public sealed class LandValueLongRun
{
    private const int Ticks = 100_000;
    private const int Population = 1_000;

    /// <summary>
    /// The Ticks within a Day a reading is attempted at — <b>07:00 to 10:00, and not midnight.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Not a round number of Days, and the roundness is the trap.</b> A Day is
    /// <see cref="Ticks.PerDay"/> = 2,048, so a reading every 2,048 Ticks lands at the same hour every
    /// time — and the obvious phase, <c>(tick + 1) % 2048 == 0</c>, lands at <b>23:59</b>, where every
    /// Segment is empty and desirability's noise term is zero in every Cell. ***A periodic reading of
    /// an instantaneous quantity samples ONE HOUR of the day, and which hour is a choice somebody has
    /// to make on purpose.***
    /// </para>
    /// <para>
    /// ⚠ <b>A band rather than an instant, because one instant is not a measurement at this
    /// population.</b> Measured, twice: reading at 08:00 <em>alone</em>, at 1,000 Citizens, found the
    /// noise term <b>zero on ten of forty-nine Days</b> — Shift starts are spread over 6 to 10
    /// (<c>adr/0101</c>) and a small city simply has nobody on the road at that exact Tick on some
    /// Days. Four attempts cut it to two Days; this band — 06:00 to 12:00 every 32 Ticks, which is the
    /// whole spread of Shift starts plus the journeys they generate — is what makes the reading a
    /// sample of the commute rather than of a moment in it. ***The instrument had to be fixed twice
    /// before the quantity it measures stopped depending on the sampling.***
    /// </para>
    /// </remarks>
    internal static ReadOnlySpan<int> ReadAt =>
        [512, 544, 576, 608, 640, 672, 704, 736, 768, 800, 832, 864, 896, 928, 960, 992, 1024];

    public Reading[] Readings { get; } = Run();

    public readonly record struct Reading(
        int Day,
        int Cells,
        int Moved,
        long Flow,
        long Mean,
        int Lowest,
        int Distinct,
        int PollutionPeak,
        int NoisePeak,
        int Tau);

    private static Reading[] Run()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "fouled.toml"));

        Assert.True(result.Ok, $"rulesets/fouled.toml was refused:\n  {result.Describe()}");

        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(Population, result.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        Assert.True(
            world.Households.Rows.LiveCount > 0,
            "the populator built no Households, so every reading below is over an empty city.");

        DesirabilityWeights weights = world.Rules.Layers.Desirability;
        List<Reading> readings = [];
        int[] previous = [];
        bool[] seen = [];

        for (int tick = 0; tick < Ticks; tick++)
        {
            simulation.Step(default);

            int intoDay = tick % (int)Core.Quantities.Ticks.PerDay;

            if (!ReadAt.Contains(intoDay))
            {
                continue;
            }

            Reading candidate = Read(
                world, weights, tick, intoDay == ReadAt[0], ref previous, ref seen);

            // The loudest of the Day's four attempts. The first one carries the land value figures,
            // which are a stored column and do not move between them; the later ones can only
            // improve the two term peaks and the concordance, which are the fragile half.
            if (intoDay == ReadAt[0])
            {
                readings.Add(candidate);
            }
            else if (candidate.NoisePeak > readings[^1].NoisePeak)
            {
                readings[^1] = readings[^1] with
                {
                    PollutionPeak = candidate.PollutionPeak,
                    NoisePeak = candidate.NoisePeak,
                    Tau = candidate.Tau,
                };
            }
        }

        return [.. readings];
    }

    private static Reading Read(
        World world,
        DesirabilityWeights weights,
        int tick,
        bool record,
        ref int[] previous,
        ref bool[] seen)
    {
        LayerCellTable cells = world.Layers.Cells;
        int slots = cells.Rows.SlotCount;

        if (previous.Length < slots)
        {
            Array.Resize(ref previous, slots);
            Array.Resize(ref seen, slots);
        }

        List<(int Pollution, int Noise)> terms = [];
        HashSet<int> distinct = [];
        int live = 0;
        int moved = 0;
        long flow = 0;
        long total = 0;
        int lowest = 0;
        int pollutionPeak = 0;
        int noisePeak = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = cells.LandValue[slot];

            live++;
            total += value;
            lowest = Math.Min(lowest, value);
            distinct.Add(value);

            // ⚠ THE FLOW IS BOOKED ON THE DAY'S FIRST ATTEMPT ONLY. Four attempts a Day sharing one
            // previous-value array would report three of them as motionless and destroy the very
            // quantity adr/0006 is asserted on.
            if (record)
            {
                if (seen[slot])
                {
                    long step = Math.Abs((long)value - previous[slot]);

                    flow += step;

                    if (step != 0)
                    {
                        moved++;
                    }
                }

                previous[slot] = value;
                seen[slot] = true;
            }

            Cells east = cells.East[slot];
            Cells north = cells.North[slot];
            int pollution = -world.Layers.CellDesirability(
                world.Roads, weights with { Noise = 0 }, east, north);
            int noise = -world.Layers.CellDesirability(
                world.Roads, weights with { Pollution = 0 }, east, north);

            terms.Add((pollution, noise));
            pollutionPeak = Math.Max(pollutionPeak, pollution);
            noisePeak = Math.Max(noisePeak, noise);
        }

        return new Reading(
            IntegerMath.FloorDiv(tick, (int)Core.Quantities.Ticks.PerDay) + 1,
            live,
            moved,
            flow,
            live == 0 ? 0 : total / live,
            lowest,
            distinct.Count,
            pollutionPeak,
            noisePeak,
            Concordance(terms));
    }

    /// <summary>
    /// Kendall's tau between the two terms across Cells, as a percentage.
    /// </summary>
    /// <remarks>
    /// <b>Rank concordance rather than Pearson, because it needs no square root and no float.</b>
    /// Over every pair of Cells, the pair is <em>concordant</em> when the Cell with more pollution
    /// also has more noise. 100 means the two terms carry the same information and no ratio between
    /// their weights is identifiable; 0 means they vary independently and a ratio is readable.
    /// </remarks>
    private static int Concordance(List<(int Pollution, int Noise)> terms)
    {
        long concordant = 0;
        long discordant = 0;

        for (int a = 0; a < terms.Count; a++)
        {
            for (int b = a + 1; b < terms.Count; b++)
            {
                int pollution = terms[a].Pollution.CompareTo(terms[b].Pollution);
                int noise = terms[a].Noise.CompareTo(terms[b].Noise);

                if (pollution == 0 || noise == 0)
                {
                    continue;
                }

                if (pollution == noise)
                {
                    concordant++;
                }
                else
                {
                    discordant++;
                }
            }
        }

        long pairs = concordant + discordant;

        return pairs == 0 ? 0 : (int)IntegerMath.RoundDiv((concordant - discordant) * 100, pairs);
    }
}
