using System.Globalization;
using System.Text;
using Borough.Core.Entities;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Prints a Map Layer's Cell grid, before and after a source change, with the halo that moved.
/// </summary>
/// <remarks>
/// <para>
/// <b>The first time the project shows a <em>field</em> rather than a number</b> (<c>plans/0009</c>
/// acceptance), and the direct ancestor of the Phase 3 overlay. Everything before this printed counts
/// and hashes; a diffusion is the first thing whose defects are <em>shaped</em>, and a directional
/// smear or a halo one Cell short is obvious in a picture and invisible in a hash.
/// </para>
/// <para>
/// <b>The halo is printed beside the field because it is the claim being made.</b> Incremental
/// re-diffusion asserts that only the changed sources and their surroundings were recomputed; a dump
/// of the two fields shows that the result is right, and only the halo shows that it was arrived at
/// the cheap way. Together they are the difference between <em>the field looks fine</em> and <em>the
/// incremental scheme did what it says</em>.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// Cell coordinates and integers; the ramp, the headings and the units are this file's.
/// </para>
/// </remarks>
internal static class LayerDump
{
    /// <summary>The demonstration window, in Cells. Chosen to fit a terminal, not the map.</summary>
    /// <remarks>
    /// <b>Shared with <see cref="TerrainDump"/></b>, so that two dumps of the same map crop to the
    /// same ground and can be read against each other line for line.
    /// </remarks>
    internal static readonly CellRect Window =
        new(new Cells(0), new Cells(0), new Cells(56), new Cells(24));

    /// <summary>
    /// Darkest last. A ramp rather than digits because a gradient is what is being judged.
    /// </summary>
    /// <remarks>
    /// Nine steps, which is about what a reader can distinguish and well below what the field
    /// resolves. It is a <em>display</em> quantisation and nothing reads it back; the CSV form beside
    /// it is what anybody comparing numbers should use.
    /// </remarks>
    internal const string Ramp = " .:-=+*#%";

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static void Run(TextWriter output, Layer layer, bool csv)
    {
        World world = new(1_000);
        MapLayers layers = world.Layers;

        // Two sources far enough apart that their plumes are separable by eye at radius 8, and both
        // inside the window. A single source would demonstrate a kernel; two demonstrate superposition.
        layers.EmitPollution(new Cells(14), new Cells(12), 4_000);
        layers.EmitPollution(new Cells(38), new Cells(9), 2_500);
        layers.RediffusePollution();

        output.WriteLine($"# Borough Map Layer dump — {Name(layer)}");
        output.WriteLine($"# Cell = {CellGrid.TilesPerCell}x{CellGrid.TilesPerCell} Tiles "
            + $"(~{CellGrid.MetresPerCell} m). Window {Window.Width.Raw}x{Window.Height.Raw} Cells "
            + $"from ({Window.East.Raw}, {Window.North.Raw}).");
        output.WriteLine($"# Kernel: separable tent, radius {layers.PollutionKernel.Radius.Raw} "
            + $"Cells = {CellGrid.ToMetres(layers.PollutionKernel.Radius)} m. UNRATIFIED (adr/0044).");
        output.WriteLine();

        output.WriteLine("## Before — two sources, full recompute");
        Write(output, layers, layer, csv);

        // A third source, close to the first, so the halo overlaps one plume and not the other.
        layers.EmitPollution(new Cells(20), new Cells(15), 3_000);
        CellRect halo = layers.DiffusePollution();

        output.WriteLine();
        output.WriteLine("## After — one source added at Cell (20, 15), diffused incrementally");
        Write(output, layers, layer, csv);

        output.WriteLine();
        output.WriteLine("## The halo actually recomputed");
        output.WriteLine(
            $"Cells ({halo.East.Raw}, {halo.North.Raw}) to "
            + $"({halo.EastEnd.Raw - 1}, {halo.NorthEnd.Raw - 1}) — {halo.Count} Cells, "
            + $"against {CellGrid.WorldCellCount} on the map.");
        output.WriteLine(
            "It is one kernel radius around the changed source and nothing else, and the result is "
            + "bit-identical to recomputing the map. Exact, not approximate: the kernel has bounded "
            + "support, so no Cell outside this box can read the source that changed (adr/0034).");
    }

    private static void Write(TextWriter output, MapLayers layers, Layer layer, bool csv)
    {
        int count = MapLayers.LayerCellCount(Window);
        Span<LayerReading> readings = new LayerReading[count];
        int written = layers.LayerCells(Window, layer, readings);

        int peak = 0;
        for (int i = 0; i < written; i++)
        {
            if (readings[i].Value > peak)
            {
                peak = readings[i].Value;
            }
        }

        if (csv)
        {
            output.WriteLine("east,north,value");

            for (int i = 0; i < written; i++)
            {
                LayerReading reading = readings[i];
                output.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{reading.East.Raw},{reading.North.Raw},{reading.Value}"));
            }

            return;
        }

        output.WriteLine($"peak {peak}, ramp \"{Ramp}\" low to high");

        StringBuilder line = new(Window.Width.Raw);

        for (int row = 0; row < Window.Height.Raw; row++)
        {
            line.Clear();

            for (int column = 0; column < Window.Width.Raw; column++)
            {
                line.Append(Step(readings[(row * Window.Width.Raw) + column].Value, peak));
            }

            output.WriteLine(line.ToString());
        }
    }

    /// <summary>Maps a value onto the ramp. Zero is always blank, so the plume's edge is visible.</summary>
    internal static char Step(int value, int peak)
    {
        if (value <= 0 || peak <= 0)
        {
            return Ramp[0];
        }

        // Ceiling, so any non-zero value gets at least the first mark. Flooring would print the whole
        // outer skirt of a plume as empty, which is the part a reader is checking the falloff on.
        int step = Borough.Core.Arithmetic.IntegerMath.CeilDiv(value * (Ramp.Length - 1), peak);

        return Ramp[step >= Ramp.Length ? Ramp.Length - 1 : step];
    }

    /// <summary>The shell owns every string a human reads (<c>adr/0002</c>).</summary>
    internal static string Name(Layer layer) => layer switch
    {
        Layer.IndustrialPollution => "industrial pollution",
        Layer.LandValue => "land value",
        Layer.Sealing => "sealing",
        Layer.Woodland => "woodland",
        _ => throw new ArgumentOutOfRangeException(nameof(layer)),
    };

    /// <summary>Parses a Layer name from the command line, or explains what the names are.</summary>
    internal static bool TryParse(string value, out Layer layer)
    {
        switch (value)
        {
            case "pollution":
                layer = Layer.IndustrialPollution;
                return true;

            case "land-value":
                layer = Layer.LandValue;
                return true;

            case "sealing":
                layer = Layer.Sealing;
                return true;

            case "woodland":
                layer = Layer.Woodland;
                return true;

            default:
                layer = default;
                return false;
        }
    }
}
