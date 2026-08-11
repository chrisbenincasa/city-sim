using System.Text;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>
/// L2 — the whole network in one Tick, and the Overlap exchange that makes it two-dimensional.
/// </summary>
internal static class NetworkReport
{
    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L2 — the network, and the Overlap exchange");
        text.AppendLine();
        text.AppendLine(
            $"Every Lane holds {Units.VehiclesPerLaneAtJam} Vehicles — one Lane of a 128 m Segment "
            + "at a standstill — and the rung is the number of Lanes. This is the sweep that "
            + "answers the question, because the Microscopic tier's cost is a whole-network cost "
            + "and a per-queue figure is a laboratory number until a network has produced one.");
        text.AppendLine();
        text.AppendLine(
            "| Lanes | Vehicles | Segments | Vehicle rows | ns/Vehicle | vs L1 | Vehicles in 15.6 ms |");
        text.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

        int[] rungs = [16, 64, 256, 1_024, 4_096, 16_384, 65_536, 262_144];
        var perVehicle = new long[rungs.Length];
        var vehicles = new long[rungs.Length];
        var fits = new long[rungs.Length];

        for (int i = 0; i < rungs.Length; i++)
        {
            var network = LaneNetwork.Build(rungs[i], Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
            var sample = Timing.Measure(() => Idm.StepQueues(network), network.Vehicles, 250, 9);
            perVehicle[i] = sample.PicosecondsPerUnit;
            vehicles[i] = network.Vehicles;
            fits[i] = sample.UnitsPerTickBudget;

            long bytes = network.VehicleBytes;

            text.AppendLine(
                $"| {Format.Count(rungs[i])} | {Format.Count(network.Vehicles)} "
                + $"| {Format.Count(rungs[i] / Units.LanesPerSegment)} "
                + $"| {Format.Count(bytes / 1024)} KiB "
                + $"| {Format.Nanoseconds(perVehicle[i])} "
                + $"| {Format.Ratio(perVehicle[i], Findings.QueuePicoseconds)} "
                + $"| {Format.Count(fits[i])} |");
        }

        // The self-consistent rung: the one whose Vehicle count is closest to the number of
        // Vehicles it says fit in a Tick. Any other rung reports a cost for a working set the
        // answer does not have, which is how a per-Vehicle figure quietly becomes a claim about a
        // city that could not exist.
        int chosen = 0;
        long bestGap = long.MaxValue;
        for (int i = 0; i < rungs.Length; i++)
        {
            long gap = vehicles[i] - fits[i];
            if (gap < 0)
            {
                gap = -gap;
            }

            if (gap < bestGap)
            {
                bestGap = gap;
                chosen = i;
            }
        }

        Findings.NetworkPicoseconds = perVehicle[chosen];
        Findings.NetworkVehicles = vehicles[chosen];

        text.AppendLine();
        text.AppendLine(
            "**The self-consistent rung is the one whose Vehicle count is closest to the Vehicle "
            + "count it says fits in a Tick** — here "
            + $"{Format.Count(vehicles[chosen])} Vehicles at "
            + $"{Format.Nanoseconds(perVehicle[chosen])} ns each. Reading any other row as the "
            + "answer states a cost for a working set the answer does not have.");
        text.AppendLine();

        text.AppendLine("### The Overlap exchange");
        text.AppendLine();
        text.AppendLine(
            "`adr/0016` states that Overlapping Lanes exchange their Vehicles' projected positions "
            + "once per Tick and states no cost for it, because the cost depends on how a Lane "
            + "finds the Vehicle near the conflict point — a data-structure decision nobody has "
            + "taken. Both plausible answers are measured. **Scan** walks the partner's queue from "
            + "its head, which is what a first implementation writes. **Cursor** keeps the queue "
            + "index found last Tick, which is O(1) amortised and is state promotion must "
            + "materialise and demotion must discard.");
        text.AppendLine();

        const int overlapLanes = 16_384;
        text.AppendLine(
            $"At {Format.Count(overlapLanes)} Lanes / "
            + $"{Format.Count(overlapLanes * Units.VehiclesPerLaneAtJam)} Vehicles, "
            + "exchange plus queue pass, against the same rung with no Overlaps at all.");
        text.AppendLine();
        text.AppendLine("| Overlaps/Lane | Exchange | ns/Vehicle | vs no Overlaps | Vehicles in 15.6 ms |");
        text.AppendLine("|---:|---|---:|---:|---:|");

        long noOverlaps = 0;
        {
            var network = LaneNetwork.Build(overlapLanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
            var sample = Timing.Measure(() => Idm.StepQueues(network), network.Vehicles, 250, 9);
            noOverlaps = sample.PicosecondsPerUnit;
            text.AppendLine(
                $"| 0 | — | {Format.Nanoseconds(noOverlaps)} | 1.00× "
                + $"| {Format.Count(sample.UnitsPerTickBudget)} |");
        }

        foreach (int per in new[] { 1, 2, 4 })
        {
            foreach (bool cursor in new[] { true, false })
            {
                var network = LaneNetwork.Build(
                    overlapLanes, Units.VehiclesPerLaneAtJam, 70, per, 0x5EEDUL);

                var sample = Timing.Measure(
                    () =>
                    {
                        if (cursor)
                        {
                            Overlaps.ExchangeByCursor(network);
                        }
                        else
                        {
                            Overlaps.ExchangeByScan(network);
                        }

                        Idm.StepQueuesWithOverlaps(network);
                    },
                    network.Vehicles,
                    250,
                    9);

                text.AppendLine(
                    $"| {per} | {(cursor ? "cursor" : "scan")} "
                    + $"| {Format.Nanoseconds(sample.PicosecondsPerUnit)} "
                    + $"| {Format.Ratio(sample.PicosecondsPerUnit, noOverlaps)} "
                    + $"| {Format.Count(sample.UnitsPerTickBudget)} |");

                if (cursor && per == 2)
                {
                    Findings.OverlapPicoseconds = sample.PicosecondsPerUnit;
                    Findings.OverlapsPerLane = per;
                }
            }
        }

        text.AppendLine();
        text.AppendLine(
            "**Two Overlaps per Lane is the row L4 carries.** A Lane on a four-Lane Segment has a "
            + "Switch Lane on each side that it is not on the edge of, plus whatever crosses it at "
            + "the node — so one is optimistic and four is a busy intersection. Nothing in the "
            + "corpus states this number, and it is not S5's to choose: it is a property of the "
            + "Road Graph the geometry pass produces, which does not exist.");
        text.AppendLine();

        return text.ToString();
    }
}
