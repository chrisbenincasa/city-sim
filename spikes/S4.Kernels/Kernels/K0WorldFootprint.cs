using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using S4.Kernels.Baseline;

namespace S4.Kernels.Kernels;

/// <summary>
/// K0 — the world's actual footprint. plans/0004 task 3: "Allocate the whole world at 1M and report
/// the real footprint, per table, hot and cold separately."
///
/// *Decides:* the size of everything downstream — the async save's copy, the transform history's
/// budget, and whether the recomputed Citizen row is closer to 40 bytes or to 80.
///
/// It allocates for real rather than computing a sum, and reports both. A computed figure cannot see
/// what the allocator and the operating system actually charge, and the gap between the two is the
/// only part of this kernel that can surprise.
///
/// **1M is a floor, not a cap**, so the headline is not "does it fit" — it is how many multiples of a
/// million this machine could hold.
/// </summary>
internal static unsafe class K0WorldFootprint
{
    public static Report Run(long citizens, int[] microscopicCaps)
    {
        var allocations = new List<nint>();
        var tables = new List<TableFootprint>();

        var rssBefore = WorkingSetBytes();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            foreach (var table in WorldSchema.All)
            {
                var rows = table.Rows(citizens);
                long committed = 0;

                // Structure-of-arrays: one contiguous allocation per column, not per row. First-touch
                // every page, or the operating system never charges us for memory we have not used
                // and the "real footprint" is a promise rather than a measurement.
                foreach (var column in table.Columns)
                {
                    var bytes = (nuint)(rows * column.Bytes);
                    if (bytes == 0)
                    {
                        continue;
                    }

                    var p = (byte*)NativeMemory.Alloc(bytes);
                    allocations.Add((nint)p);
                    Touch(p, bytes);
                    committed += (long)bytes;
                }

                tables.Add(new TableFootprint(
                    table.Name,
                    rows,
                    rows * table.BytesPerRow(WorldSchema.Tier.PerTick),
                    rows * table.BytesPerRow(WorldSchema.Tier.Wake),
                    rows * table.BytesPerRow(WorldSchema.Tier.Cold),
                    committed));
            }

            stopwatch.Stop();
            var rssAfter = WorkingSetBytes();

            var caps = new List<CapFootprint>();
            foreach (var cap in microscopicCaps)
            {
                caps.Add(CapCost(cap));
            }

            return new Report(citizens, tables, caps, rssAfter - rssBefore, stopwatch.Elapsed.TotalSeconds, TotalRamBytes());
        }
        finally
        {
            foreach (var p in allocations)
            {
                NativeMemory.Free((void*)p);
            }
        }
    }

    /// <summary>
    /// Lanes and Vehicles as a function of the Microscopic Cap, which is a fixed world constant with
    /// no value. Computed rather than allocated — the point is the shape of the curve, and it lets
    /// K0 say what the Cap costs before anybody has to choose one.
    /// </summary>
    private static CapFootprint CapCost(int microscopicSegments)
    {
        var lanes = (long)microscopicSegments * WorldSchema.LanesPerMicroscopicSegment;
        var vehicles = lanes * WorldSchema.VehiclesPerLaneAtJam;

        var laneBytes = lanes * WorldSchema.LaneColumns.Sum(c => c.Bytes);
        var vehicleBytes = vehicles * WorldSchema.VehicleColumns.Sum(c => c.Bytes);

        return new CapFootprint(microscopicSegments, lanes, vehicles, laneBytes, vehicleBytes);
    }

    private static void Touch(byte* p, nuint bytes)
    {
        for (nuint offset = 0; offset < bytes; offset += 4096)
        {
            p[offset] = 1;
        }
    }

    private static long WorkingSetBytes()
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return process.WorkingSet64;
    }

    private static long TotalRamBytes()
    {
        var info = GC.GetGCMemoryInfo();
        return info.TotalAvailableMemoryBytes;
    }

    internal sealed record TableFootprint(
        string Name, long Rows, long PerTickBytes, long WakeBytes, long ColdBytes, long CommittedBytes);

    internal sealed record CapFootprint(
        int MicroscopicSegments, long Lanes, long Vehicles, long LaneBytes, long VehicleBytes)
    {
        public long TotalBytes => LaneBytes + VehicleBytes;
    }

    internal sealed record Report(
        long Citizens,
        IReadOnlyList<TableFootprint> Tables,
        IReadOnlyList<CapFootprint> Caps,
        long WorkingSetDeltaBytes,
        double AllocateSeconds,
        long TotalRamBytes)
    {
        public long PerTick => Tables.Sum(t => t.PerTickBytes);

        public long Wake => Tables.Sum(t => t.WakeBytes);

        public long Cold => Tables.Sum(t => t.ColdBytes);

        public long Committed => Tables.Sum(t => t.CommittedBytes);

        public string ToMarkdown()
        {
            var sb = new StringBuilder();
            var c = CultureInfo.InvariantCulture;

            sb.AppendLine(c, $"### K0 — the world at {Citizens / 1000:N0}k Citizens");
            sb.AppendLine();
            sb.AppendLine("Allocated for real and first-touched, not computed. Per-Tick is what an ordinary Tick");
            sb.AppendLine("touches; wake is what a woken entity costs; cold is transactions and inspection.");
            sb.AppendLine();
            sb.AppendLine("| Table | Rows | Per-Tick | Wake | Cold | Total |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var t in Tables)
            {
                sb.Append(c, $"| {t.Name} | {t.Rows:N0} | {Mb(t.PerTickBytes)} | {Mb(t.WakeBytes)} | ");
                sb.AppendLine(c, $"{Mb(t.ColdBytes)} | {Mb(t.CommittedBytes)} |");
            }

            sb.Append(c, $"| **Total** | | **{Mb(PerTick)}** | **{Mb(Wake)}** | ");
            sb.AppendLine(c, $"**{Mb(Cold)}** | **{Mb(Committed)}** |");
            sb.AppendLine();

            sb.Append(c, $"Process working set grew by **{Mb(WorkingSetDeltaBytes)}** against {Mb(Committed)} requested — ");
            sb.Append(c, $"an overhead of {(WorkingSetDeltaBytes - Committed) / (double)Committed * 100:F1}%. ");
            sb.AppendLine(c, $"Allocated and touched in {AllocateSeconds * 1000:F0} ms.");
            sb.AppendLine();

            sb.AppendLine("### What the Microscopic Cap costs");
            sb.AppendLine();
            sb.AppendLine("Lanes and Vehicles are sized by the Cap, not by population. A Statistical Segment has no");
            sb.AppendLine("Lanes at all, so this is the whole of what the microscopic tier charges in memory.");
            sb.AppendLine();
            sb.AppendLine("| Microscopic Segments | Lanes | Vehicles at jam | Footprint |");
            sb.AppendLine("|---:|---:|---:|---:|");
            foreach (var cap in Caps)
            {
                sb.Append(c, $"| {cap.MicroscopicSegments:N0} | {cap.Lanes:N0} | {cap.Vehicles:N0} | ");
                sb.AppendLine(c, $"{Mb(cap.TotalBytes)} |");
            }

            sb.AppendLine();
            var headroom = TotalRamBytes / (double)Committed;
            sb.Append(c, $"**Headroom: {headroom:F0}x.** This machine reports {Mb(TotalRamBytes)} available, so it could ");
            sb.Append(c, $"hold roughly {headroom * Citizens / 1e6:F0} million Citizens' worth of world. ");
            sb.AppendLine("1M is a floor rather than a cap, so that multiple is the result and the absolute figure is only how it was reached.");

            return sb.ToString();
        }

        private static string Mb(long bytes) =>
            bytes >= 1024 * 1024
                ? string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024.0 / 1024.0:F1} MiB")
                : string.Create(CultureInfo.InvariantCulture, $"{bytes / 1024.0:F0} KiB");
    }
}
