using System.Text;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L4 — the derived product, and the tripwire table evaluated against it.
/// </summary>
/// <remarks>
/// The thresholds below are transcribed from <c>plans/0019 §The tripwire</c>, which was written
/// before anything ran. They are constants in the source so that the verdict is computed rather
/// than asserted, and so that moving one is a diff.
/// </remarks>
internal static class ProductReport
{
    /// <summary>T1. <c>adr/0016</c>'s transplanted headline, in our unit.</summary>
    private const long CitybounFigure = 400_000;

    /// <summary>
    /// T2. S2 R2's Segment count over an 80% threshold on the uniform O-D rung, times a Segment's
    /// Vehicles at a standstill. Not a stressed-Segment count and not a Cap.
    /// </summary>
    private const long StressedSegments = 2_592;

    /// <summary>T3. S4's own tolerance: worse than 3–4× off the hand-computed ideal.</summary>
    private const long MemcpyConstantMultiple = 4;

    /// <summary>
    /// T4. Ticks a Vehicle takes to cross a 128 m Segment at free flow: 32 Tiles ÷ 1.05.
    /// </summary>
    private const long SegmentTraversalTicks = 30;

    /// <summary>T5. The scatter multiple this corpus has now sighted three times.</summary>
    private const long ScatterMultipleHundredths = 150;

    public static string Run()
    {
        var text = new StringBuilder();

        long segmentVehicles = Units.LanesPerSegment * Units.VehiclesPerLaneAtJam;
        long budgetPicoseconds = Units.TickBudgetNanoseconds * 1000L;

        long bare = Findings.NetworkPicoseconds == 0
            ? 0
            : budgetPicoseconds / Findings.NetworkPicoseconds;
        long withOverlaps = Findings.OverlapPicoseconds == 0
            ? 0
            : budgetPicoseconds / Findings.OverlapPicoseconds;

        long bareSegments = bare / segmentVehicles;
        long overlapSegments = withOverlaps / segmentVehicles;

        long residency = Findings.NetworkPicoseconds == 0
            ? 0
            : (Findings.PromotePicoseconds + Findings.DemotePicoseconds) / Findings.NetworkPicoseconds;

        text.AppendLine("## L4 — the derived product");
        text.AppendLine();
        text.AppendLine(
            "One core, one 15.6 ms Tick at 4× speed, `adr/0016`'s structure in `adr/0003`'s "
            + "arithmetic. **S5 supplies one side of a ratio and does not set the Microscopic Cap** "
            + "— the other side is how many Vehicles a real city stresses at once, which is "
            + "milestone 5b's and does not exist.");
        text.AppendLine();
        text.AppendLine("| Quantity | Figure |");
        text.AppendLine("|---|---:|");
        text.AppendLine(
            $"| ns per Vehicle per Tick, no Overlaps | {Format.Nanoseconds(Findings.NetworkPicoseconds)} |");
        text.AppendLine(
            $"| ns per Vehicle per Tick, {Findings.OverlapsPerLane} Overlaps per Lane by cursor "
            + $"| {Format.Nanoseconds(Findings.OverlapPicoseconds)} |");
        text.AppendLine($"| **Vehicles per Tick per core, no Overlaps** | **{Format.Count(bare)}** |");
        text.AppendLine($"| **Vehicles per Tick per core, with Overlaps** | **{Format.Count(withOverlaps)}** |");
        text.AppendLine(
            $"| Vehicles in a Segment at a standstill | {segmentVehicles} |");
        text.AppendLine($"| **Microscopic Segments in 15.6 ms, no Overlaps** | **{Format.Count(bareSegments)}** |");
        text.AppendLine($"| **Microscopic Segments in 15.6 ms, with Overlaps** | **{Format.Count(overlapSegments)}** |");
        text.AppendLine(
            $"| Promotion + demotion, ns per Vehicle "
            + $"| {Format.Nanoseconds(Findings.PromotePicoseconds + Findings.DemotePicoseconds)} |");
        text.AppendLine($"| **Break-even residency** | **{Format.Count(residency)} Ticks** |");
        text.AppendLine();

        text.AppendLine("### The tripwire, evaluated");
        text.AppendLine();
        text.AppendLine(
            "Transcribed from `plans/0019`, which stated every threshold before anything ran.");
        text.AppendLine();
        text.AppendLine("| # | Condition | Reading | Fired? |");
        text.AppendLine("|---|---|---:|---|");

        bool t1 = withOverlaps < CitybounFigure;
        text.AppendLine(
            $"| T1 | Vehicles/Tick/core below {Format.Count(CitybounFigure)} — `adr/0016`'s "
            + "transplanted headline does not survive our arithmetic and our Tick "
            + $"| {Format.Count(withOverlaps)} | {Fired(t1)} |");

        long stressedVehicles = StressedSegments * segmentVehicles;
        bool t2 = withOverlaps < stressedVehicles;
        text.AppendLine(
            $"| T2 | Vehicles/Tick/core below {Format.Count(stressedVehicles)} "
            + $"({Format.Count(StressedSegments)} Segments × {segmentVehicles}) — `adr/0007`'s "
            + "*bounded by network stress, not by population* fails at the only adjacent count the "
            + $"corpus holds | {Format.Count(withOverlaps)} | {Fired(t2)} |");

        long multiple = Findings.DenominatorPicoseconds == 0
            ? 0
            : (Findings.NetworkPicoseconds * 100L) / Findings.DenominatorPicoseconds;
        bool t3 = multiple > MemcpyConstantMultiple * 100L;
        text.AppendLine(
            $"| T3 | Queue pass worse than {MemcpyConstantMultiple}× the bare walk — `adr/0016`'s "
            + "*with the constant of a `memcpy`* is false in the letter "
            + $"| {Format.Ratio(Findings.NetworkPicoseconds, Findings.DenominatorPicoseconds)} "
            + $"| {Fired(t3)} |");

        bool t4 = residency > SegmentTraversalTicks;
        text.AppendLine(
            $"| T4 | Break-even residency above {SegmentTraversalTicks} Ticks — one Segment "
            + "traversal at free flow — so `adr/0016`'s own revisit trigger *promotion cost "
            + $"dominating the traffic budget* fires | {Format.Count(residency)} Ticks "
            + $"| {Fired(t4)} |");

        long scatter = Findings.QueuePicoseconds == 0
            ? 0
            : (Findings.NetworkPicoseconds * 100L) / Findings.QueuePicoseconds;
        bool t5 = scatter > ScatterMultipleHundredths;
        text.AppendLine(
            $"| T5 | Network rung more than {ScatterMultipleHundredths / 100}."
            + $"{ScatterMultipleHundredths % 100:00}× the fixed-working-set queue rung — the "
            + "single-queue figure is a fixture number and may not be published as the unit cost "
            + $"| {Format.Ratio(Findings.NetworkPicoseconds, Findings.QueuePicoseconds)} "
            + $"| {Fired(t5)} |");

        text.AppendLine();
        text.AppendLine(
            "**What a fired tripwire does and does not mean.** T1 firing is a statement about a "
            + "sentence in `adr/0016`, not about the design: the ADR's structural argument — no "
            + "spatial index, predecessor is the previous array element, scheduling granularity is "
            + "the Lane — is untouched by the constant. T2 firing is the one that reaches a "
            + "decision, and even then it reaches `adr/0007`'s scaling clause rather than the Cap's "
            + "value, which S5 must not set.");
        text.AppendLine();

        return text.ToString();
    }

    private static string Fired(bool fired) => fired ? "**FIRED**" : "no";
}
