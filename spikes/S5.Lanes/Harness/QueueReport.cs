using System.Text;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L1 — car-following down a sorted queue, and the two claims `adr/0016` makes about it.
/// </summary>
internal static class QueueReport
{
    /// <summary>
    /// Vehicles held constant across the queue-length sweep, so the rung varies the queue length
    /// and not the working set with it. This is the axis S0b's findings 42–43 found dominating a
    /// unit cost that had been measured on a fixture, and repeating that mistake here would be
    /// unforced.
    /// </summary>
    internal const int TotalVehicles = 294_912;

    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L1 — the queue pass");
        text.AppendLine();
        text.AppendLine(
            "One Tick of IDM car-following down a sorted one-dimensional queue, no Overlaps, no "
            + "spatial index, no indirection. Every Lane is a ring, so every Vehicle has a leader "
            + "and the kernel runs in the congested regime the Microscopic tier exists for rather "
            + "than in free flow.");
        text.AppendLine();

        text.AppendLine("### Queue length, at a fixed working set");
        text.AppendLine();
        text.AppendLine(
            $"{Format.Count(TotalVehicles)} Vehicles at every rung, redistributed across more or "
            + "fewer Lanes. A rung that swept the queue length *and* the working set together "
            + "would report their product and call it the queue length.");
        text.AppendLine();
        text.AppendLine("| Vehicles/Lane | Lanes | ns/Vehicle | ns/Vehicle (median) | vs L0 |");
        text.AppendLine("|---:|---:|---:|---:|---:|");

        long atSegmentRung = 0;
        foreach (int perLane in new[] { 4, 8, Units.VehiclesPerLaneAtJam, 32, 128, 512, 4096 })
        {
            int lanes = TotalVehicles / perLane;
            var network = LaneNetwork.Build(lanes, perLane, 70, 0, 0x5EEDUL);
            var sample = Timing.Measure(() => Idm.StepQueues(network), network.Vehicles, 250, 9);

            text.AppendLine(
                $"| {perLane} | {Format.Count(lanes)} "
                + $"| {Format.Nanoseconds(sample.PicosecondsPerUnit)} "
                + $"| {Format.Nanoseconds(sample.MedianPicosecondsPerUnit)} "
                + $"| {Format.Ratio(sample.PicosecondsPerUnit, Findings.DenominatorPicoseconds)} |");

            if (perLane == Units.VehiclesPerLaneAtJam)
            {
                atSegmentRung = sample.PicosecondsPerUnit;
            }
        }

        Findings.QueuePicoseconds = atSegmentRung;
        Findings.QueueLength = Units.VehiclesPerLaneAtJam;

        text.AppendLine();
        text.AppendLine(
            $"**The rung that counts is {Units.VehiclesPerLaneAtJam}** — what one Lane of a "
            + "128 m Segment holds at a standstill. Every longer rung is a queue no Segment in "
            + "this design has, and quoting one would be S0b's finding again: a unit cost taken "
            + "on a fixture the world does not produce.");
        text.AppendLine();

        text.AppendLine("### Regime");
        text.AppendLine();
        text.AppendLine(
            "The kernel has three data-dependent branches — the interaction term going negative, "
            + "the gap ratio hitting its cap, and the velocity flooring at zero — so its cost is "
            + "not obviously flat across traffic states. Occupancy is Vehicles as a percentage of "
            + $"what the Lane holds at a standstill, at {Units.VehiclesPerLaneAtJam} per Lane.");
        text.AppendLine();
        int[] occupancies = [25, 50, 70, 90, 100];
        var measured = new long[occupancies.Length];
        long reference = 0;

        for (int i = 0; i < occupancies.Length; i++)
        {
            int lanes = TotalVehicles / Units.VehiclesPerLaneAtJam;
            var network = LaneNetwork.Build(
                lanes, Units.VehiclesPerLaneAtJam, occupancies[i], 0, 0x5EEDUL);
            var sample = Timing.Measure(() => Idm.StepQueues(network), network.Vehicles, 250, 9);
            measured[i] = sample.PicosecondsPerUnit;

            if (occupancies[i] == 70)
            {
                reference = measured[i];
            }
        }

        text.AppendLine("| Occupancy | ns/Vehicle | vs 70% |");
        text.AppendLine("|---:|---:|---:|");

        for (int i = 0; i < occupancies.Length; i++)
        {
            text.AppendLine(
                $"| {occupancies[i]}% | {Format.Nanoseconds(measured[i])} "
                + $"| {Format.Ratio(measured[i], reference)} |");
        }

        text.AppendLine();

        text.AppendLine("### Where the time goes");
        text.AppendLine();
        text.AppendLine(
            "The IDM as written divides **three times per Vehicle per Tick** and two of those "
            + "denominators never vary — `2√(ab)` is a constant of the Ruleset and `v0` is a "
            + "constant of the driver. A 64-bit integer division is tens of cycles and does not "
            + "pipeline; a floating-point division is a handful and does. That is exactly where a "
            + "transplant from a float engine should be expected to cost, so the two forms are "
            + "measured rather than argued about. The third division, `s*/s`, has the gap to the "
            + "vehicle in front as its denominator and no reciprocal exists for it — **this is the "
            + "floor of what the substitution can buy, not an alternative implementation.**");
        text.AppendLine();

        int reciprocalLanes = TotalVehicles / Units.VehiclesPerLaneAtJam;
        var dividing = LaneNetwork.Build(
            reciprocalLanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
        var reciprocal = LaneNetwork.Build(
            reciprocalLanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);

        var dividingSample = Timing.Measure(
            () => Idm.StepQueues(dividing), dividing.Vehicles, 250, 9);
        var reciprocalSample = Timing.Measure(
            () => Idm.StepQueuesReciprocal(reciprocal), reciprocal.Vehicles, 250, 9);

        Findings.ReciprocalPicoseconds = reciprocalSample.PicosecondsPerUnit;

        text.AppendLine("| Form | Divisions/Vehicle | Row | ns/Vehicle | vs L0 | Vehicles in 15.6 ms |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|");
        text.AppendLine(
            $"| As written | 3 | 16 B | {Format.Nanoseconds(dividingSample.PicosecondsPerUnit)} "
            + $"| {Format.Ratio(dividingSample.PicosecondsPerUnit, Findings.DenominatorPicoseconds)} "
            + $"| {Format.Count(dividingSample.UnitsPerTickBudget)} |");
        text.AppendLine(
            $"| Reciprocal | 1 | 20 B | {Format.Nanoseconds(reciprocalSample.PicosecondsPerUnit)} "
            + $"| {Format.Ratio(reciprocalSample.PicosecondsPerUnit, Findings.DenominatorPicoseconds)} "
            + $"| {Format.Count(reciprocalSample.UnitsPerTickBudget)} |");
        text.AppendLine();
        text.AppendLine(
            "Removing two of the three divisions moves the pass by "
            + $"{Format.Ratio(dividingSample.PicosecondsPerUnit, reciprocalSample.PicosecondsPerUnit)}. "
            + "**Read this as an attribution and not as a recommendation.** A reciprocal changes "
            + "the arithmetic, so it changes the State Hash, so under `CLAUDE.md`'s own test it is "
            + "*a design change however it was motivated* — and the fifth column is state a "
            + "promotion has to materialise.");
        text.AppendLine();

        return text.ToString();
    }
}
