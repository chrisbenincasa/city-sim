namespace Borough.Headless;

using System.Globalization;
using System.Text;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

/// <summary>
/// <c>--land-value</c>: the target, the lag, and the gap between them, on a running city.
/// </summary>
/// <remarks>
/// <para>
/// <b>Milestone 9 task 6, and the picture worth printing is three grids rather than one.</b> Land
/// value alone is a picture of a field; what this milestone built is a field <em>chasing</em> another
/// field, and the claim — <c>02 §2.4</c>'s <em>it moves slowly toward the current desirability rather
/// than tracking it</em> — is only visible when the two are printed together with their difference.
/// ***A lag is not a property of a value; it is a property of a pair.***
/// </para>
/// <para>
/// <b>It is not <c>--layer</c> grown, and that is a decision.</b> <c>--layer</c> builds its own world
/// and hand-places sources, because — in its own words — <em>no session can place a source until
/// Rules exist</em>. ⚠ <b>That sentence was true when it was written and is now stale</b>: Rules
/// exist, and <c>rulesets/fouled.toml</c> is a Ruleset whose Rules place sources. But a land value
/// picture needs a city that has been <em>running</em>, because the quantity is a history, so it
/// belongs with <c>--zones</c> and <c>--parking</c> in the family of modes that step a world rather
/// than with the mode that draws a convolution.
/// </para>
/// <para>
/// ⚠ <b>THE HOUR IS PRINTED IN THE HEADER AND IT IS NOT DECORATION.</b> Desirability's noise term
/// reads a Segment's volume <em>at the instant it is asked</em>
/// (<c>adr/0127</c>), so a dump taken at midnight in a city where everybody drives shows a target
/// with no noise in it at all. A reader who does not know the hour cannot tell a quiet neighbourhood
/// from a quiet time of day.
/// </para>
/// <para>
/// <b>It refuses a Ruleset that emits nothing.</b> <c>--parking</c>'s polarity: the only thing in the
/// build that creates a Cell row is a pollution emission, so on a Ruleset with no map term every
/// panel here is blank — and a blank picture reads as a broken instrument rather than as a file with
/// no industry in it. ⚠ <b>Eight of the nine shipped Rulesets are such files</b>, and say so in their
/// own headers.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// Cell coordinates and integers; the ramp, the headings and the units are this file's.
/// </para>
/// </remarks>
internal static class LandValueDump
{
    /// <summary>Darkest last, and <c>--layer</c>'s ramp deliberately, so the two read alike.</summary>
    private const string Ramp = " .:-=+*#%";

    /// <summary>How many cadence samples the closing table reports over.</summary>
    /// <remarks>
    /// <b>Sixteen, which is two Days at the shipped cadence</b> — <c>land_value_period</c> is 256 and
    /// a Day is <see cref="Ticks.PerDay"/> = 2,048, so eight samples land in a Day. Two Days rather
    /// than one because a single Day cannot distinguish a daily orbit from a drift.
    /// </remarks>
    private const int Samples = 16;

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        if (Refuse(rules, output) is int refusal)
        {
            return refusal;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        int period = rules.Layers.Schedule.For(Layer.LandValue).Period;
        int offset = rules.Layers.Schedule.For(Layer.LandValue).Offset;
        List<int> movers = [];
        int[] previous = [];
        bool[] seen = [];

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            if (period <= 0 || (int)(tick % (ulong)period) != offset)
            {
                continue;
            }

            movers.Add(Moved(world, ref previous, ref seen));
        }

        CellRect window = Occupied(world);

        Header(output, options, rules, world, window);

        Grid(output, world, window, "## The TARGET — desirability, composed now, at the hour above",
            "What a Cell would be worth if land value tracked its surroundings instantly. It does not.",
            (east, north) => -world.Layers.CellDesirability(
                world.Roads, rules.Layers.Desirability, east, north));

        output.WriteLine();

        Grid(output, world, window, "## The LAG — land value, the stored column",
            "The same field with memory. Where this is lighter than the panel above, the Cell has not"
            + " finished falling.",
            (east, north) => -world.Layers.LandValue(east, north));

        output.WriteLine();

        Grid(output, world, window, "## The GAP — target minus value, magnitude",
            "Where the city is still moving. A dark Cell is one whose surroundings changed recently"
            + " and whose value has not caught up.",
            (east, north) => IntegerMath.Abs(
                -world.Layers.CellDesirability(world.Roads, rules.Layers.Desirability, east, north)
                - -world.Layers.LandValue(east, north)));

        output.WriteLine();

        Motion(output, movers, period);

        return 0;
    }

    /// <summary>Counts the Cells whose land value changed since the previous cadence sample.</summary>
    /// <remarks>
    /// <b>Indexed by slot, with a seen flag beside the value.</b> A slot sampled for the first time
    /// has not <em>moved</em>, it has appeared — the city is still building, so counting a new Cell
    /// as motion would report a growing city as a churning one. The producer creates no rows
    /// (<c>MapLayers.SetLandValueTargets</c>), so a slot that is live stays live and the arrays only
    /// ever grow.
    /// </remarks>
    private static int Moved(World world, ref int[] previous, ref bool[] seen)
    {
        int slots = world.Layers.Cells.Rows.SlotCount;

        if (previous.Length < slots)
        {
            Array.Resize(ref previous, slots);
            Array.Resize(ref seen, slots);
        }

        int moved = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            int value = world.Layers.Cells.LandValue[slot];

            if (seen[slot] && previous[slot] != value)
            {
                moved++;
            }

            previous[slot] = value;
            seen[slot] = true;
        }

        return moved;
    }

    /// <summary>The bounding box of the Cells that have rows, clamped to something a terminal fits.</summary>
    /// <remarks>
    /// <b>Derived from the world rather than declared</b>, unlike <c>--layer</c>'s fixed window, and
    /// the reason is that this world is generated: where a city lands is the generator's business and
    /// a hard-coded box would draw a blank grid the first time it moved.
    /// </remarks>
    private static CellRect Occupied(World world)
    {
        int minEast = int.MaxValue;
        int minNorth = int.MaxValue;
        int maxEast = int.MinValue;
        int maxNorth = int.MinValue;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (!world.Layers.Cells.Rows.IsLive(slot))
            {
                continue;
            }

            minEast = Math.Min(minEast, world.Layers.Cells.East[slot].Raw);
            minNorth = Math.Min(minNorth, world.Layers.Cells.North[slot].Raw);
            maxEast = Math.Max(maxEast, world.Layers.Cells.East[slot].Raw);
            maxNorth = Math.Max(maxNorth, world.Layers.Cells.North[slot].Raw);
        }

        if (minEast > maxEast)
        {
            return new CellRect(new Cells(0), new Cells(0), new Cells(1), new Cells(1));
        }

        int width = Math.Min(maxEast - minEast + 1, 78);
        int height = Math.Min(maxNorth - minNorth + 1, 32);

        return new CellRect(
            new Cells(minEast), new Cells(minNorth), new Cells(width), new Cells(height));
    }

    private static void Header(
        TextWriter output, Options options, Ruleset rules, World world, CellRect window)
    {
        ulong tick = options.Ticks;
        ulong intoDay = tick % Ticks.PerDay;
        int minutes = (int)IntegerMath.FloorDiv((long)intoDay * 24 * 60, (long)Ticks.PerDay);

        output.WriteLine("# Borough land value dump — the target, the lag, and the gap");
        output.WriteLine(
            $"# {options.Citizens} Citizens, {tick} Ticks, "
            + $"{world.Buildings.Rows.LiveCount} Buildings, "
            + $"{world.Layers.Cells.Rows.LiveCount} Cells with a row.");
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"# THE HOUR IS {IntegerMath.FloorDiv(minutes, 60):00}:{minutes % 60:00} of day "
            + $"{(tick / Ticks.PerDay) + 1}, and it matters: desirability's noise term reads a"));
        output.WriteLine(
            "# Segment's volume AT THIS INSTANT (adr/0127), so a dump taken at midnight shows a "
            + "target with no");
        output.WriteLine(
            "# traffic in it at all. A quiet Cell here may be a quiet place or a quiet hour.");
        output.WriteLine(
            $"# Weights: pollution {Percent(rules.Layers.Desirability.Pollution)}%, "
            + $"noise {Percent(rules.Layers.Desirability.Noise)}%, "
            // Tiles straight to metres. Routing it through Cells would quantise a 300 m range
            // to 256, which is a conversion doing damage rather than work.
            + $"noise range {rules.Layers.Desirability.NoiseSource.Range.Raw * Tiles.Metres} m. "
            + "ALL UNRATIFIED (plans/0002 §D1).");
        output.WriteLine(
            $"# Lag tau {rules.Layers.Rates.LandValueTau}, cadence every "
            + $"{rules.Layers.Schedule.For(Layer.LandValue).Period} Ticks. "
            + $"Window {window.Width.Raw}x{window.Height.Raw} Cells from "
            + $"({window.East.Raw}, {window.North.Raw}).");
        output.WriteLine(
            "# Every panel prints MAGNITUDE. Desirability has no positive term until amenity exists "
            + "(adr/0123),");
        output.WriteLine(
            "# so the whole field is at or below zero and a darker Cell is a worse one.");
        output.WriteLine();
    }

    private static void Grid(
        TextWriter output, World world, CellRect window, string heading, string caption,
        Func<Cells, Cells, int> read)
    {
        int peak = 0;

        for (int row = 0; row < window.Height.Raw; row++)
        {
            for (int column = 0; column < window.Width.Raw; column++)
            {
                peak = Math.Max(
                    peak,
                    read(new Cells(window.East.Raw + column), new Cells(window.North.Raw + row)));
            }
        }

        output.WriteLine(heading);
        output.WriteLine(caption);
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"peak {Whole(peak)} (Q16.16 {peak}), ramp \"{Ramp}\" low to high"));

        StringBuilder line = new(window.Width.Raw);

        for (int row = 0; row < window.Height.Raw; row++)
        {
            line.Clear();

            for (int column = 0; column < window.Width.Raw; column++)
            {
                line.Append(Step(
                    read(new Cells(window.East.Raw + column), new Cells(window.North.Raw + row)),
                    peak));
            }

            output.WriteLine(line.ToString());
        }
    }

    /// <summary>What the field did over the run, which is the half a grid cannot show.</summary>
    private static void Motion(TextWriter output, List<int> movers, int period)
    {
        output.WriteLine("## What the field DID — Cells moving per cadence sample");

        if (movers.Count == 0)
        {
            output.WriteLine(
                $"No cadence landed in this run. The land value cadence is every {period} Ticks; "
                + "ask for more --ticks.");
            return;
        }

        int from = Math.Max(0, movers.Count - Samples);
        int taken = movers.Count - from;
        int total = 0;

        for (int i = from; i < movers.Count; i++)
        {
            total += movers[i];
        }

        output.WriteLine(
            $"Last {taken} samples, oldest first: "
            + string.Join(" ", movers.GetRange(from, taken)));
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Mean {IntegerMath.FloorDiv(total, taken)} Cells moving per sample."));
        output.WriteLine(
            "A settled field would report zero. This one does not settle, and that is the finding "
            + "rather than a fault:");
        output.WriteLine(
            "the target moves with the Day because the noise term is instantaneous, so land value "
            + "orbits rather than resting");
        output.WriteLine(
            "(adr/0127). The Cells that DO report zero are the clean, quiet ones — target zero, gap "
            + "zero, operator stopped.");
    }

    /// <summary>Refuses a Ruleset on which every panel would be blank.</summary>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        for (int id = 1; id <= rules.RuleCount; id++)
        {
            foreach (MapEmission emission in rules.Emissions(new RuleId((ushort)id)))
            {
                if (emission.Layer == Layer.IndustrialPollution && emission.Amount > 0)
                {
                    return null;
                }
            }
        }

        output.WriteLine(
            "This Ruleset declares no Rule that emits into a Map Layer, so no Cell will ever get a "
            + "row and every panel would be blank.");
        output.WriteLine(
            "The only thing in the build that creates a Cell row is a pollution emission. Eight of "
            + "the nine shipped Rulesets emit nothing —");
        output.WriteLine(
            "a dwelling is not industry, and they each say so — which is why land value was zero in "
            + "every world until rulesets/fouled.toml.");
        output.WriteLine();
        output.WriteLine("  dotnet run --project src/Borough.Headless -- --land-value \\");
        output.WriteLine("    --ruleset rulesets/fouled.toml --citizens 4000 --ticks 21163");
        output.WriteLine();
        output.WriteLine(
            "21163 rather than a round number on purpose: it is ten Days and about eight hours, so "
            + "the dump lands in the morning");
        output.WriteLine(
            "commute rather than at midnight. A round multiple of 2048 always lands at midnight, "
            + "where the noise term is zero.");

        return 2;
    }

    /// <summary>Maps a magnitude onto the ramp. Zero is always blank, so the field's edge shows.</summary>
    private static char Step(int value, int peak)
    {
        if (value <= 0 || peak <= 0)
        {
            return Ramp[0];
        }

        int step = IntegerMath.CeilDiv(value * (Ramp.Length - 1), peak);

        return Ramp[step >= Ramp.Length ? Ramp.Length - 1 : step];
    }

    /// <summary>A Q16.16 value as whole units and hundredths, which is what a reader compares.</summary>
    private static string Whole(int fixedValue)
    {
        int whole = fixedValue >> 16;
        int hundredths = IntegerMath.FloorDiv((fixedValue & 0xFFFF) * 100, 1 << 16);

        return string.Create(CultureInfo.InvariantCulture, $"{whole}.{hundredths:00}");
    }

    private static int Percent(int weight) => IntegerMath.RoundDiv(weight * 100, 1 << 16);
}
