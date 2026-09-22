using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace LabourCost;

internal static class Program
{
    internal static void Emit(object value) { Console.WriteLine(JsonSerializer.Serialize(value)); Console.Out.Flush(); }

    private static void Main(string[] args)
    {
        var source = RulesetSource.Load("rulesets/stress-shopping.toml");
        var rules = source.Ruleset ?? throw new InvalidOperationException("Ruleset failed");
        var key = WorldKey.FromSeed(0);
        int population = int.Parse(args[1]);
        Emit(new { type = "conditions", mode = args[0], population, seed = 0, runtime = RuntimeInformation.FrameworkDescription,
            cpuCount = Environment.ProcessorCount, serverGc = System.Runtime.GCSettings.IsServerGC,
            configuration = "Release", threads = 1, affinity = Process.GetCurrentProcess().ProcessorAffinity.ToInt64(),
            coreSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(World).Assembly.Location))),
            rulesetSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes("rulesets/stress-shopping.toml"))) });
        World world;
        if (args[0] == "load")
        {
            using var file = File.OpenRead(args[2]);
            world = SaveFile.Read(new SaveSource(file), rules, out _);
        }
        else
        {
            world = new World(population, rules, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        }
        if (args[0] == "age")
        {
            var sim = new Simulation(world, key) { VerifyDecideWritesNothing = false, RouteWorkerCount = 1 };
            ulong until = ulong.Parse(args[2]);
            while (world.Tick.Raw < until)
            {
                sim.Step(default);
                if (world.Tick.Raw % Ticks.PerDay == 0) Emit(new { type = "age", tick = world.Tick.Raw, citizens = world.Citizens.Rows.LiveCount });
            }
            sim.Rules.Drain(); sim.Trips.Drain(); sim.CheckEndOfRun();
            Census(world, "aged");
            using var file = File.Create(args[3]);
            SaveFile.Write(world, source.Capture!.ContentHash, new SaveSink(file));
            Emit(new { type = "validated", hash = world.HashState().ToString("X16"), invariants = "passed" });
            return;
        }
        Census(world, args[0] == "load" ? "aged" : "generated");
        Probe.Setup(world);
        if (args[0] == "load") Measure(world, "observed", world.Tick);
        // Controlled roster sweep: preserve table layout and employer distribution, force a
        // known fraction to AtWork, retaining the real OnDuty/illness checks in both passes.
        var jobs = Enumerable.Range(0, world.Businesses.Rows.SlotCount)
            .Where(b => world.Businesses.Rows.IsLive(b) && world.Rules.DeclaresBusiness(world.Businesses.Kind[b])).ToArray();
        if (jobs.Length == 0) throw new InvalidOperationException("No employers");
        foreach (int percent in new[] { 0, 25, 50, 100 })
        {
            for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            {
                if (!world.Citizens.Rows.IsLive(c)) continue;
                // Coprime stride scatters workers over employers instead of a contiguous hot Bin.
                int job = jobs[(int)((long)c * 7919 % jobs.Length)];
                world.Citizens.Workplace[c] = world.Businesses.Rows.At(job);
                world.Citizens.Activity[c] = (byte)(c % 100 < percent ? CitizenActivity.AtWork : CitizenActivity.AtHome);
            }
            Measure(world, $"controlled-{percent}", new Ticks(12 * Ticks.PerDay / 24));
        }
    }

    private static int Width(Rows rows) => rows.Columns.ToArray().Sum(c => c.BytesPerRow);

    private static void Census(World world, string label)
    {
        var bins = world.Bins.Rows;
        long total = world.Tables.ToArray().Sum(t => (long)t.Capacity * Width(t) * (t.Buffering == Buffering.TwoCopies ? 2 : 1));
        int businessBins = 0, goodsBins = 0;
        for (int b = 0; b < bins.SlotCount; b++)
        {
            if (!bins.IsLive(b)) continue;
            if (world.Bins.OwnerKind[b] == BinOwnerKind.Business) businessBins++;
            if (world.Rules.Family(world.Bins.Resource[b]) == ResourceFamily.Good) goodsBins++;
        }
        Emit(new { type = "census", label, tick = world.Tick.Raw, citizens = world.Citizens.Rows.LiveCount,
            citizenCapacity = world.Citizens.Rows.Capacity, businesses = world.Businesses.Rows.LiveCount,
            liveBins = bins.LiveCount, binSlots = bins.SlotCount, binCapacity = bins.Capacity, businessBins, goodsBins,
            binWidth = Width(bins), savedBinWidth = bins.SavedBytesPerRow, tablePayloadBytes = total,
            columns = bins.Columns.ToArray().Select(c => new { c.Name, c.BytesPerRow, disposition = c.Disposition.ToString() }) });
        foreach (int n in new[] { 2, 4, 8, 16 })
        {
            long start = GC.GetAllocatedBytesForCurrentThread();
            object buckets = n switch { 2 => new Bucket2[bins.Capacity], 4 => new Bucket4[bins.Capacity],
                8 => new Bucket8[bins.Capacity], _ => new Bucket16[bins.Capacity] };
            var clock = new ulong[bins.Capacity];
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            GC.KeepAlive(buckets); GC.KeepAlive(clock);
            int addedWidth = (n + 1) * sizeof(long);
            Emit(new { type = "memory", label, buckets = n, addedWidth,
                payloadBytes = (long)addedWidth * bins.Capacity, measuredArrayAllocationBytes = allocated,
                savedSlotBytes = (long)addedWidth * bins.SlotCount,
                remainderBytes = (long)sizeof(long) * world.Citizens.Rows.Capacity,
                addedBinsForLabour = world.Businesses.Rows.LiveCount,
                addedLabourBinPayload = (long)(Width(bins) + addedWidth) * world.Businesses.Rows.LiveCount });
        }
    }

    private static void Measure(World world, string roster, Ticks tick)
    {
        int onDuty = 0;
        for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            if (world.Citizens.Rows.IsLive(c) && (CitizenActivity)world.Citizens.Activity[c] == CitizenActivity.AtWork
                && !CivicEngine.TooIllToWork(world, c) && WorkSchedule.OnDuty(world, c, tick)) onDuty++;
        int count = world.Citizens.Rows.SlotCount;
        long[] earned = new long[count], remainder = new long[count];
        for (int c = 0; c < count; c++) { earned[c] = world.Citizens.EarnedWage[c]; remainder[c] = world.Citizens.WageRemainder[c]; }
        void Restore()
        {
            for (int c = 0; c < count; c++) { world.Citizens.EarnedWage[c] = earned[c]; world.Citizens.WageRemainder[c] = remainder[c]; }
            Probe.Reset(world);
        }
        // Instrument correctness: inserting the deposit must leave every wage and remainder equal.
        Restore(); WorkSchedule.Accrue(world, tick);
        long[] expected = new long[count * 2];
        for (int c = 0; c < count; c++) { expected[c * 2] = world.Citizens.EarnedWage[c]; expected[c * 2 + 1] = world.Citizens.WageRemainder[c]; }
        Restore(); Probe.Mode = 2; WorkScheduleLabour.Accrue(world, tick);
        for (int c = 0; c < count; c++)
            if (expected[c * 2] != world.Citizens.EarnedWage[c] || expected[c * 2 + 1] != world.Citizens.WageRemainder[c])
                throw new InvalidOperationException("Prototype changed wage accrual");
        Probe.Verify(world, onDuty);
        int iterations = Math.Clamp(4_000_000 / Math.Max(1, count), 64, 200);
        Emit(new { type = "workload", roster, citizens = world.Citizens.Rows.LiveCount, onDuty, tick = tick.Raw, iterations,
            verification = "wages match; fractional deposit and bucket totals checked", prototypeBuckets = 4, cycleTicks = 32 });
        // Warm every method before paired, alternating-order batches. Frozen world, fixed schedule
        // Tick; expiry's independent clock advances once per pass, outside the timed interval.
        for (int mode = 0; mode <= 3; mode++) Run(mode, Math.Max(16, iterations));
        for (int pair = 0; pair < 9; pair++)
        {
            int[] order = pair % 2 == 0 ? [0, 1, 2, 3] : [3, 2, 1, 0];
            foreach (int mode in order)
            {
                Restore();
                if (mode == 3) Probe.PrimeExpiry(world);
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                int gc0 = GC.CollectionCount(0), gc1 = GC.CollectionCount(1), gc2 = GC.CollectionCount(2);
                double ms = Run(mode, iterations);
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                Emit(new { type = "timing", roster, pair, mode = new[] { "baseline", "clone-control", "deposit", "deposit-expiry" }[mode],
                    msPerPass = ms / iterations, allocated,
                    collections = new[] { GC.CollectionCount(0) - gc0, GC.CollectionCount(1) - gc1, GC.CollectionCount(2) - gc2 } });
            }
        }
        Restore();
        double Run(int mode, int iterations)
        {
            Probe.Mode = mode;
            long elapsed = 0;
            for (int i = 0; i < iterations; i++)
            {
                Probe.Clock++;
                long start = Stopwatch.GetTimestamp();
                if (mode == 0) WorkSchedule.Accrue(world, tick); else WorkScheduleLabour.Accrue(world, tick);
                elapsed += Stopwatch.GetTimestamp() - start;
            }
            return elapsed * 1000.0 / Stopwatch.Frequency;
        }
    }
}

[InlineArray(2)] internal struct Bucket2 { private long _element; }
[InlineArray(4)] internal struct Bucket4 { private long _element; }
[InlineArray(8)] internal struct Bucket8 { private long _element; }
[InlineArray(16)] internal struct Bucket16 { private long _element; }

internal static class Probe
{
    internal static int Mode;
    internal static ulong Clock;
    private static Handle<Bin>[] _bins = [];
    private static long[] _remainder = [];
    private static Bucket4[] _buckets = [];
    private static ulong[] _cycle = [];
    private static long[] _waste = [];
    private const long PerDay = 4097;

    internal static void Setup(World world)
    {
        _bins = new Handle<Bin>[world.Businesses.Rows.Capacity];
        _remainder = new long[world.Citizens.Rows.Capacity];
        for (int b = 0; b < world.Businesses.Rows.SlotCount; b++)
        {
            if (!world.Businesses.Rows.IsLive(b)) continue;
            // Isolated, uncapped proxy Bin. Money provides the existing non-market deposit path;
            // no simulation Steps/invariants run after adding these unlinked measurement Bins.
            var h = world.Bins.Rows.Allocate(); int slot = world.Bins.Rows.Resolve(h);
            world.Bins.Capacity[slot] = long.MaxValue;
            world.Bins.OwnerKind[slot] = BinOwnerKind.Business;
            world.Bins.Resource[slot] = world.Bins.Resource[world.Bins.Rows.Resolve(world.Businesses.Balance[b])];
            world.Bins.SupplyHead[slot] = world.Bins.SpaceHead[slot] = 0;
            world.Bins.SupplyTail[slot] = world.Bins.SpaceTail[slot] = 0;
            _bins[b] = h;
        }
        _buckets = new Bucket4[world.Bins.Rows.Capacity];
        _cycle = new ulong[world.Bins.Rows.Capacity];
        _waste = new long[world.Businesses.Rows.Capacity];
    }

    internal static void Reset(World world)
    {
        for (int b = 0; b < _bins.Length; b++)
            if (!_bins[b].IsNone) world.Withdraw(_bins[b], world.Bins.LevelAt(world.Bins.Rows.Resolve(_bins[b])), Ticks.Zero);
        Array.Clear(_remainder); Array.Clear(_buckets); Array.Clear(_cycle); Array.Clear(_waste); Clock = 0;
    }

    internal static void PrimeExpiry(World world)
    {
        foreach (var bin in _bins)
        {
            if (bin.IsNone) continue;
            int slot = world.Bins.Rows.Resolve(bin);
            for (int i = 0; i < 4; i++) _buckets[slot][i] = 8;
            world.Deposit(bin, 32, Ticks.Zero);
        }
    }

    internal static void Deposit(World world, int citizen, int job, Ticks tick)
    {
        if (Mode == 1) return;
        // Independent productivity grading; intentionally non-divisible so the remainder matters.
        long tier = 100 + world.Citizens.SkillTier[citizen] * 25;
        long premium = Math.Min(20, world.Citizens.Experience[citizen] / 100);
        long rate = IntegerMath.FloorDiv(IntegerMath.FloorDiv(PerDay * tier, 100) * (100 + premium), 100);
        long scaled = _remainder[citizen] + rate;
        long whole = IntegerMath.ShiftRight(scaled, 11);
        _remainder[citizen] = scaled % Ticks.PerDay;
        if (whole == 0) return;
        var bin = _bins[job];
        if (Mode == 3)
        {
            int slot = world.Bins.Rows.Resolve(bin);
            ulong cycle = Clock / 32;
            int elapsed = (int)Math.Min(4UL, cycle - _cycle[slot]);
            ref Bucket4 buckets = ref _buckets[slot];
            long expired = 0;
            for (int i = 0; i < elapsed; i++) expired += buckets[i];
            if (elapsed != 0)
            {
                for (int i = 0; i < 4 - elapsed; i++) buckets[i] = buckets[i + elapsed];
                for (int i = 4 - elapsed; i < 4; i++) buckets[i] = 0;
                _cycle[slot] = cycle;
                if (expired > 0) { world.Withdraw(bin, expired, tick); _waste[job] += expired; }
            }
            buckets[3] += whole;
        }
        world.Deposit(bin, whole, tick);
    }

    internal static void Verify(World world, int onDuty)
    {
        long units = 0;
        foreach (var bin in _bins) if (!bin.IsNone) units += world.Bins.LevelAt(world.Bins.Rows.Resolve(bin));
        if (onDuty > 0 && units < onDuty * 2L) throw new InvalidOperationException("No labour deposited");
        if (_remainder.Any(r => r < 0 || r >= Ticks.PerDay)) throw new InvalidOperationException("Invalid remainder");
        Reset(world); Mode = 3;
        // One known eligible worker: preserve fractions across 257 Ticks and expire across buckets.
        int c = Enumerable.Range(0, world.Citizens.Rows.SlotCount).FirstOrDefault(c => world.Citizens.Rows.IsLive(c)
            && world.Businesses.Rows.TryResolve(world.Citizens.Workplace[c], out _), -1);
        if (c < 0) return;
        int job = world.Businesses.Rows.Resolve(world.Citizens.Workplace[c]);
        long rate = IntegerMath.FloorDiv(IntegerMath.FloorDiv(PerDay * (100 + world.Citizens.SkillTier[c] * 25), 100)
            * (100 + Math.Min(20, world.Citizens.Experience[c] / 100)), 100);
        for (int i = 1; i <= 257; i++) { Clock = (ulong)i; Deposit(world, c, job, Ticks.Zero); }
        int slot = world.Bins.Rows.Resolve(_bins[job]);
        long sum = 0; for (int i = 0; i < 4; i++) sum += _buckets[slot][i];
        if (sum != world.Bins.LevelAt(slot) || sum + _waste[job] != rate * 257 / Ticks.PerDay
            || _remainder[c] != rate * 257 % Ticks.PerDay || _waste[job] <= 0)
            throw new InvalidOperationException("Expiry/remainder prototype failed conservation check");
        Reset(world);
    }
}

internal sealed class SaveSink(Stream stream) : ISaveSink
{ public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes); }
internal sealed class SaveSource(Stream stream) : ISaveSource
{ public void Read(Span<byte> bytes) => stream.ReadExactly(bytes); }
