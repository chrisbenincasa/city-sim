using System.Globalization;
using System.Text;
using S2.Routing.Graph;

namespace S2.Routing.Harness;

/// <summary>
/// R0's first deliverable: the graph's footprint as a curve against Segment count, and the road
/// density each rung implies.
/// </summary>
/// <remarks>
/// <para>
/// <b>The point of the curve is the density, not the bytes.</b> <c>CONTEXT.md</c> → Segment says the
/// ~30,000 figure <i>"rests on a road-density assumption nothing in this corpus has yet argued, and
/// it is spike S2's to replace."</i> So the column that matters most here is not the megabyte total —
/// it is <b>km of road per km²</b>, which is the first quantity in this project that can be checked
/// against a real city.
/// </para>
/// <para>
/// <b>Reported both computed and measured, following K0.</b> K0's stated reason was that <i>"a
/// computed figure cannot see what the allocator and the operating system actually charge, and the
/// gap between the two is the only part of this kernel that can surprise."</i> The gap is smaller
/// here — these are managed arrays rather than first-touched pages — but the discipline of printing
/// both is what would surface an arrangement whose real cost is not its column widths.
/// </para>
/// </remarks>
internal static class FootprintReport
{
    /// <summary>
    /// Block sizes, in Tiles. A Cell is 32, so the middle rung is one Street on every Cell boundary
    /// and the sweep brackets it by 4× in each direction.
    /// </summary>
    private static readonly int[] BlockRungs = [128, 96, 64, 48, 32, 24, 16];

    /// <summary>Arterial counts for the severance table. Zero is a rung, because it is the control.</summary>
    private static readonly int[] ArterialRungs = [0, 2, 4, 8, 16, 32];

    public static string Run()
    {
        var report = new StringBuilder();

        report.AppendLine("## S2 R0 — the synthetic Road Graph");
        report.AppendLine();
        report.AppendLine(Capture.Stamp());
        report.AppendLine();
        report.AppendLine("The graph is directed, one arc per permitted direction, mode masks on the arc.");
        report.AppendLine("Cost is time in Q16.16 Ticks; see `Graph/Units.cs` for why it is not whole Ticks.");
        report.AppendLine();

        AppendDensityCurve(report);
        AppendFootprintCurve(report);
        AppendVolumeScope(report);
        AppendColumns(report);
        AppendSeverance(report);

        return report.ToString();
    }

    // --- The curve the corpus is actually owed --------------------------------------------------

    private static void AppendDensityCurve(StringBuilder report)
    {
        report.AppendLine("### Road density against block size");
        report.AppendLine();
        report.AppendLine("The `~30,000 Segments` placeholder is a road-density assumption nobody has argued.");
        report.AppendLine("This is what each density implies on a 4096² map.");
        report.AppendLine();
        report.AppendLine("| Block | Segments | Nodes | Arcs | Segments/km² | km road/km² | Mean Segment | Foot-admitting |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int block in BlockRungs)
        {
            var graph = GraphGenerator.Build(GraphParameters.Working with { BlockTiles = block });

            long roadTiles = graph.RoadLengthTiles();
            int foot = graph.SegmentsAdmitting(Modes.Foot);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {block} Tiles | {graph.Segments:N0} | {graph.Nodes:N0} | {graph.Arcs:N0} " +
                $"| {SegmentsPerSquareKilometre(graph):N0} " +
                $"| {Hundredths(RoadKilometresPerSquareKilometre(graph))} " +
                $"| {MeanSegmentTiles(graph, roadTiles)} Tiles " +
                $"| {Percent(foot, graph.Segments)} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**A Cell is 32 Tiles.** The 32-Tile rung is one Street on every Cell boundary, and it is " +
            "the rung that reproduces the corpus's placeholder — which is worth stating, because it " +
            "means the placeholder was never arbitrary. Whether it is *right* is the km/km² column, " +
            "against a real city.");
        report.AppendLine();
    }

    // --- Footprint ------------------------------------------------------------------------------

    private static void AppendFootprintCurve(StringBuilder report)
    {
        report.AppendLine("### Footprint against Segment count");
        report.AppendLine();
        report.AppendLine("`(saved AND hashed)` and `(derived AND rebuilt)` separated, because the second half is");
        report.AppendLine("what `adr/0040` makes free to change forever and what a later optimisation may delete.");
        report.AppendLine();
        report.AppendLine("| Block | Segments | Saved | Derived | Total | Bytes/Segment | Managed heap Δ |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int block in BlockRungs)
        {
            var parameters = GraphParameters.Working with { BlockTiles = block };

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetTotalMemory(forceFullCollection: true);

            var graph = GraphGenerator.Build(parameters);

            long after = GC.GetTotalMemory(forceFullCollection: true);

            long saved = 0;
            long derived = 0;
            foreach (var column in graph.Columns())
            {
                if (column.Derived)
                {
                    derived += column.Bytes;
                }
                else
                {
                    saved += column.Bytes;
                }
            }

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {block} Tiles | {graph.Segments:N0} | {Kib(saved)} | {Kib(derived)} | {Kib(saved + derived)} " +
                $"| {(saved + derived) / graph.Segments} | {Kib(after - before)} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "The managed-heap column is the K0 discipline — a computed figure cannot see what the " +
            "allocator actually charges — and here it also carries the generator's transient `List<T>` " +
            "scaffolding, so it is an upper bound on the graph rather than a measurement of it.");
        report.AppendLine();
    }

    private static void AppendVolumeScope(StringBuilder report)
    {
        report.AppendLine("### What per-direction `volume / capacity` costs");
        report.AppendLine();
        report.AppendLine("`plans/0010` forbids R0 from settling this and requires it parameterised. This is the price;");
        report.AppendLine("what it buys is not visible until R2 has volume to attribute.");
        report.AppendLine();
        report.AppendLine("| Block | Segments | Per Segment | Per direction | Δ | Δ as share of total |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|");

        foreach (int block in BlockRungs)
        {
            var perSegment = GraphGenerator.Build(GraphParameters.Working with
            {
                BlockTiles = block,
                VolumeScope = VolumeScope.PerSegment,
            });

            var perDirection = GraphGenerator.Build(GraphParameters.Working with
            {
                BlockTiles = block,
                VolumeScope = VolumeScope.PerDirection,
            });

            long a = Total(perSegment);
            long b = Total(perDirection);

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {block} Tiles | {perSegment.Segments:N0} | {Kib(a)} | {Kib(b)} | {Kib(b - a)} " +
                $"| {Percent(b - a, b)} |"));
        }

        report.AppendLine();
    }

    private static void AppendColumns(StringBuilder report)
    {
        var graph = GraphGenerator.Build(GraphParameters.Working);

        report.AppendLine("### Per column, at the working rung");
        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"Block {GraphParameters.Working.BlockTiles} Tiles, {GraphParameters.Working.ArterialCount} Arterials, " +
            $"{graph.Segments:N0} Segments, {graph.Nodes:N0} nodes, {graph.Arcs:N0} arcs."));
        report.AppendLine();
        report.AppendLine("| Column | Group | Count | Bytes each | Bytes | Declaration |");
        report.AppendLine("|---|---|---:|---:|---:|---|");

        foreach (var column in graph.Columns())
        {
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| `{column.Name}` | {column.Group} | {column.Count:N0} | {column.BytesEach} | {Kib(column.Bytes)} " +
                $"| {(column.Derived ? "`(derived AND rebuilt)`" : "`(saved AND hashed)`")} |"));
        }

        report.AppendLine();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"**Total {Kib(Total(graph))}**, against S4's K0 figure of **172.3 MiB** for the whole world at 1M. " +
            $"The Road Graph is not the memory problem, which is the same thing K0 found about the " +
            $"Microscopic Cap and is worth stating before any router is measured on top of it."));
        report.AppendLine();
    }

    // --- Severance ------------------------------------------------------------------------------

    private static void AppendSeverance(StringBuilder report)
    {
        report.AppendLine("### What the Arterials do to the grid");
        report.AppendLine();
        report.AppendLine("An Arterial occupies the ground it crosses, so every Street it crosses is deleted or kept");
        report.AppendLine("as a foot crossing. This is the detour the router will be asked about, and it is the only");
        report.AppendLine("way `CONTEXT.md` → Severance is observable at all.");
        report.AppendLine();
        report.AppendLine("| Arterials | Runs | Ramps | Mean run | Segments | Severed | Foot crossings | Car-admitting | Foot-admitting |");
        report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|");

        foreach (int arterials in ArterialRungs)
        {
            var graph = GraphGenerator.Build(GraphParameters.Working with { ArterialCount = arterials });

            string meanRun = graph.ArterialRuns == 0
                ? "—"
                : string.Create(CultureInfo.InvariantCulture,
                    $"{graph.ArterialRunTiles / graph.ArterialRuns} Tiles");

            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"| {arterials} | {graph.ArterialRuns:N0} | {graph.ArterialRamps:N0} | {meanRun} " +
                $"| {graph.Segments:N0} | {graph.SeveredStreets:N0} | {graph.FootCrossings:N0} " +
                $"| {graph.SegmentsAdmitting(Modes.Car):N0} " +
                $"| {graph.SegmentsAdmitting(Modes.Foot):N0} |"));
        }

        report.AppendLine();
        report.AppendLine(
            "**Severed count is a road-network fact, not yet a Severance measurement.** Whether a " +
            "neighbourhood is actually cut off on foot is a reachability question over the foot " +
            "subgraph, and answering it needs a search — which is R0's second half. What this table " +
            "establishes is that there is something to find: the graph has genuine barriers in it " +
            "rather than a grid with decorative diagonals drawn over the top.");
        report.AppendLine();
    }

    // --- Arithmetic. BOR0203 is off under Harness/; see .editorconfig for why. -------------------

    private static long Total(RoadGraph graph)
    {
        long total = 0;
        foreach (var column in graph.Columns())
        {
            total += column.Bytes;
        }

        return total;
    }

    /// <summary>Map area in square metres. A Tile is ~4 m — `05 §26`, "268 km² (4096² Tiles @ ~4 m)".</summary>
    private static long AreaSquareMetres(RoadGraph graph) =>
        (long)graph.Parameters.MapTiles * graph.Parameters.MapTiles * 16;

    private static long SegmentsPerSquareKilometre(RoadGraph graph) =>
        graph.Segments * 1_000_000L / AreaSquareMetres(graph);

    /// <summary>Hundredths of a kilometre of road per square kilometre of city.</summary>
    private static long RoadKilometresPerSquareKilometre(RoadGraph graph) =>
        graph.RoadLengthTiles() * 4 * 100_000L / AreaSquareMetres(graph);

    private static string MeanSegmentTiles(RoadGraph graph, long roadTiles) =>
        (roadTiles / graph.Segments).ToString(CultureInfo.InvariantCulture);

    private static string Hundredths(long value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value / 100}.{value % 100:D2}");

    private static string Percent(long part, long whole) =>
        whole == 0
            ? "—"
            : string.Create(CultureInfo.InvariantCulture, $"{part * 100 / whole}%");

    private static string Kib(long bytes) =>
        bytes >= 1024 * 1024
            ? string.Create(CultureInfo.InvariantCulture, $"{bytes * 10 / (1024 * 1024) / 10}.{bytes * 10 / (1024 * 1024) % 10} MiB")
            : string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024:N0} KiB");
}
