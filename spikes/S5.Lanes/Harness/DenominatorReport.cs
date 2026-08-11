using System.Text;
using Borough.Core.Arithmetic;
using S5.Lanes.Lanes;

namespace S5.Lanes.Harness;

/// <summary>L0 — the fixture, the row schema, and the denominator.</summary>
internal static class DenominatorReport
{
    public static string Run()
    {
        var text = new StringBuilder();

        text.AppendLine("## L0 — the fixture, the row, and the denominator");
        text.AppendLine();
        text.AppendLine(
            "Every figure in this capture is Q16.16 Tiles and Tiles per Tick, through "
            + "`Borough.Core.Arithmetic.Fixed` including its `checked` narrowing. Nothing here is a "
            + "`double`, which is the difference between this measurement and the one `adr/0016` "
            + "quotes.");
        text.AppendLine();

        text.AppendLine("### The units, and where each comes from");
        text.AppendLine();
        text.AppendLine("| Quantity | Value | Source |");
        text.AppendLine("|---|---:|---|");
        text.AppendLine($"| Tile | {Units.MetresPerTile} m | Cell = 32×32 Tiles ≈ 128 m |");
        text.AppendLine(
            $"| Segment | {Units.SegmentLengthTiles} Tiles = "
            + $"{Units.SegmentLengthTiles * Units.MetresPerTile} m | S2 R0; `CONTEXT` → Segment |");
        text.AppendLine($"| Lanes per Segment | {Units.LanesPerSegment} | `CONTEXT` → Segment |");
        text.AppendLine(
            $"| Free-flow speed | {Whole(Units.FreeFlowSpeed)} Tiles/Tick | `adr/0019`: 4.2 m per Tick at 8192 Ticks/Day |");
        text.AppendLine(
            $"| Vehicle length | {Whole(Units.VehicleLength)} Tiles | 5 m |");
        text.AppendLine(
            $"| `s0` minimum gap | {Whole(Units.MinimumGap)} Tiles | Treiber, 2 m |");
        text.AppendLine(
            $"| `T` desired headway | {Whole(Units.DesiredHeadwayTicks)} Ticks | Treiber 1.5 s ÷ 0.2326 s/Tick |");
        text.AppendLine(
            $"| `a` | {Whole(Units.MaxAcceleration)} Tiles/Tick² | Treiber 1.4 m/s² |");
        text.AppendLine(
            $"| `b` | {Whole(Units.ComfortableBraking)} Tiles/Tick² | Treiber 2.0 m/s² |");
        text.AppendLine(
            $"| Jam spacing | {Whole(Units.JamSpacing)} Tiles | derived, `s0` + length |");
        text.AppendLine(
            $"| **Vehicles per Lane at a standstill** | **{Units.VehiclesPerLaneAtJam}** | derived, Segment ÷ jam spacing |");
        text.AppendLine();
        text.AppendLine(
            "**One IDM step per Tick, and no substepping to price.** `adr/0019` derives "
            + "`TICKS_PER_DAY` *from* car-following resolution and states the row this table uses — "
            + "4.2 m per Tick is 12% of the ~36 m safe following distance, against Treiber's "
            + "Δt ≤ 0.5 s. The Tick *is* the integration step.");
        text.AppendLine();

        text.AppendLine("### The row");
        text.AppendLine();
        text.AppendLine(
            $"Four `int` columns — position, velocity, desired speed, Traveller id — so "
            + $"**{LaneNetwork.BytesPerVehicleRow} bytes per Vehicle**, struct of arrays, one arena. "
            + "A Segment at a standstill is "
            + $"{Units.LanesPerSegment * Units.VehiclesPerLaneAtJam} Vehicles and therefore "
            + $"{Units.LanesPerSegment * Units.VehiclesPerLaneAtJam * LaneNetwork.BytesPerVehicleRow} "
            + "bytes of queue.");
        text.AppendLine();

        text.AppendLine("### The denominator");
        text.AppendLine();
        text.AppendLine(
            "The same walk over the same arrays with the arithmetic removed: three loads, two "
            + "stores, the ring bookkeeping. Measured here rather than divided against S4's "
            + "recorded bandwidth, because S4's figure was taken under a different governor on a "
            + "different day and dividing across that is a ratio between two machines.");
        text.AppendLine();
        text.AppendLine("| Lanes | Vehicles | ns/Vehicle | GB/s of Vehicle row |");
        text.AppendLine("|---:|---:|---:|---:|");

        long headline = 0;
        foreach (int lanes in new[] { 64, 1024, 16_384, 262_144 })
        {
            var network = LaneNetwork.Build(lanes, Units.VehiclesPerLaneAtJam, 70, 0, 0x5EEDUL);
            long checksum = 0;
            var sample = Timing.Measure(
                () => checksum += Denominator.Touch(network), network.Vehicles, 250, 9);
            Sink += checksum;

            long bytesPerSecond = sample.PicosecondsPerUnit == 0
                ? 0
                : (LaneNetwork.BytesPerVehicleRow * 1_000_000_000_000L) / sample.PicosecondsPerUnit;

            text.AppendLine(
                $"| {Format.Count(lanes)} | {Format.Count(network.Vehicles)} "
                + $"| {Format.Nanoseconds(sample.PicosecondsPerUnit)} "
                + $"| {Format.Nanoseconds(bytesPerSecond / 1_000_000L)} |");

            if (lanes == 16_384)
            {
                headline = sample.PicosecondsPerUnit;
            }
        }

        Findings.DenominatorPicoseconds = headline;

        text.AppendLine();
        text.AppendLine(
            "The 16,384-Lane row is the one L1 and L2 divide against: "
            + $"**{Format.Nanoseconds(headline)} ns per Vehicle**, at a working set "
            + $"({Format.Count(16_384L * Units.VehiclesPerLaneAtJam * LaneNetwork.BytesPerVehicleRow / 1024)} KiB) "
            + "past this machine's L2 and inside its L3.");
        text.AppendLine();

        return text.ToString();
    }

    internal static long Sink { get; set; }

    private static string Whole(int fixedValue)
    {
        long thousandths = ((long)fixedValue * 1000L) >> 16;
        return $"{thousandths / 1000}.{Positive(thousandths % 1000):000}";

        static long Positive(long value) => value < 0 ? -value : value;
    }
}
