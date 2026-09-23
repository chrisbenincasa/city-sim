using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

internal static class Followup
{
    private static readonly string[] Names = ["baseline", "direct-dense", "combined-dense", "direct-sparse", "combined-sparse"];
    private static Handle<Bin>[] _bins = [];
    private static long[] _remainders = [], _pending = [], _waste = [];
    private static int[] _touched = [];
    private static Bucket4[] _dense = [];
    private static ulong[] _denseClocks = [];
    private static ExpiryStore _sparse = null!;
    private static int _touchedCount, _mode;
    private static long _rate;
    private static ulong _clock;

    internal static void Run(string[] args)
    {
        int population = int.Parse(args[1]), seed = int.Parse(args[2]), percent = int.Parse(args[4]);
        _rate = long.Parse(args[3]);
        CheckWakeSemantics();
        var source = RulesetSource.Load("rulesets/stress-shopping.toml");
        var rules = source.Ruleset ?? throw new InvalidOperationException("Ruleset failed");
        var key = WorldKey.FromSeed((ulong)seed);
        World world;
        if (args.Length > 5)
        {
            using var stream = File.OpenRead(args[5]);
            world = SaveFile.Read(new SaveSource(stream), rules, out _);
        }
        else
        {
            world = new World(population, rules, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        }
        Program.Emit(new { type = "conditions", population, seed, rate = _rate, atWorkPercent = percent,
            checkpoint = args.Length > 5 ? args[5] : null, runtime = RuntimeInformation.FrameworkDescription,
            threads = 1, affinity = Process.GetCurrentProcess().ProcessorAffinity.ToInt64(),
            coreSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(World).Assembly.Location))),
            harnessSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Followup).Assembly.Location))) });
        Ticks tick = percent < 0 ? world.Tick : new Ticks(1024);
        var jobs = Enumerable.Range(0, world.Businesses.Rows.SlotCount)
            .Where(b => world.Businesses.Rows.IsLive(b) && world.Rules.DeclaresBusiness(world.Businesses.Kind[b])).ToArray();
        if (percent >= 0)
            for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            {
                if (!world.Citizens.Rows.IsLive(c)) continue;
                world.Citizens.Workplace[c] = world.Businesses.Rows.At(jobs[(int)((long)c * 7919 % jobs.Length)]);
                world.Citizens.Activity[c] = (byte)(c % 100 < percent ? CitizenActivity.AtWork : CitizenActivity.AtHome);
            }
        var perJob = new int[world.Businesses.Rows.Capacity];
        var active = new List<int>();
        for (int c = 0; c < world.Citizens.Rows.SlotCount; c++)
            if (world.Citizens.Rows.IsLive(c) && (CitizenActivity)world.Citizens.Activity[c] == CitizenActivity.AtWork
                && !CivicEngine.TooIllToWork(world, c) && WorkSchedule.OnDuty(world, c, tick))
            { perJob[world.Businesses.Rows.Resolve(world.Citizens.Workplace[c])]++; active.Add(c); }
        int goods = 0;
        for (int b = 0; b < world.Bins.Rows.SlotCount; b++)
            if (world.Bins.Rows.IsLive(b) && world.Rules.Family(world.Bins.Resource[b]) == ResourceFamily.Good) goods++;
        int originalCapacity = world.Bins.Rows.Capacity, originalSlots = world.Bins.Rows.SlotCount;
        Setup(world);
        Program.Emit(new { type = "workload", citizens = world.Citizens.Rows.LiveCount, onDuty = perJob.Sum(),
            workingBusinesses = perJob.Count(n => n > 0), maxWorkersPerBusiness = perJob.Max(),
            businesses = world.Businesses.Rows.LiveCount, goodsBins = goods, originalCapacity, originalSlots,
            binCapacity = world.Bins.Rows.Capacity, binSlots = world.Bins.Rows.SlotCount, clock = tick.Raw });
        ReportMemory(world, goods);
        long directCalls = 0, combinedCalls = 0;
        var seen = new int[world.Businesses.Rows.Capacity];
        for (int pass = 1; pass <= 32; pass++)
            foreach (int c in active)
            {
                long experience = world.Citizens.Experience[c] / 100;
                long rate = IntegerMath.FloorDiv(IntegerMath.FloorDiv(_rate * (100 + world.Citizens.SkillTier[c] * 25), 100)
                    * (100 + (experience > 20 ? 20 : experience)), 100);
                long before = (long)c * 37 % Ticks.PerDay + rate * (pass - 1);
                if ((before + rate) / Ticks.PerDay == before / Ticks.PerDay) continue;
                directCalls++;
                int job = world.Businesses.Rows.Resolve(world.Citizens.Workplace[c]);
                if (seen[job] != pass) { seen[job] = pass; combinedCalls++; }
            }
        Program.Emit(new { type = "deposit-counts", passes = 32, directCalls, combinedCalls,
            scope = "derived outside timing from the same initial remainders and grading; timed proxy queues are empty" });
        var earned = world.Citizens.EarnedWage.Span.ToArray();
        var wages = world.Citizens.WageRemainder.Span.ToArray();
        void Restore()
        {
            earned.CopyTo(world.Citizens.EarnedWage.Span); wages.CopyTo(world.Citizens.WageRemainder.Span);
            for (int j = 0; j < _bins.Length; j++)
                if (!_bins[j].IsNone)
                {
                    int bin = world.Bins.Rows.Resolve(_bins[j]);
                    world.Withdraw(_bins[j], world.Bins.LevelAt(bin), tick);
                    world.Deposit(_bins[j], 32, tick);
                    for (int k = 0; k < 4; k++)
                    { _dense[bin][k] = 8; _sparse.Buckets[_sparse.Index[bin] - 1][k] = 8; }
                }
            for (int c = 0; c < _remainders.Length; c++) _remainders[c] = (long)c * 37 % Ticks.PerDay;
            Array.Clear(_pending); Array.Clear(_waste); Array.Clear(_denseClocks); _sparse.Clocks.Span.Clear();
            _touchedCount = 0; _clock = 0;
        }
        // Compare complete Core hashes and every prototype value, including through an untouched
        // interval longer than the shelf life. The sidecar itself is not registered in World.
        (ulong World, long[] State)? expected = null;
        foreach (int mode in new[] { 1, 2, 3, 4 })
        {
            Restore(); _mode = mode;
            foreach (ulong clock in new ulong[] { 1, 2, 31, 32, 33, 127, 128, 129, 257, 10000, 10001 })
            { _clock = clock; WorkScheduleCombined.Accrue(world, tick); Flush(world, tick); }
            var state = State(world);
            ulong hash = world.HashState();
            if (expected is { } e && (e.World != hash || !e.State.AsSpan().SequenceEqual(state)))
                throw new InvalidOperationException($"Combined/storage result differs: {Names[mode]}");
            expected ??= (hash, state);
        }
        var prototypeEarned = world.Citizens.EarnedWage.Span.ToArray();
        var prototypeWages = world.Citizens.WageRemainder.Span.ToArray();
        Restore(); for (int i = 0; i < 11; i++) WorkSchedule.Accrue(world, tick);
        if (!prototypeEarned.AsSpan().SequenceEqual(world.Citizens.EarnedWage.Span)
            || !prototypeWages.AsSpan().SequenceEqual(world.Citizens.WageRemainder.Span))
            throw new InvalidOperationException("Prototype changed wages");
        CheckIndexRebuild(world);
        Program.Emit(new { type = "verification", results = "all four variants match Core hash, wages, worker fractions, levels, expiry clocks, buckets and waste" });
        const int iterations = 32, pairs = 7;
        for (int mode = 0; mode < Names.Length; mode++) { Restore(); RunPasses(mode, iterations); }
        for (int pair = 0; pair < pairs; pair++)
        {
            foreach (int mode in pair % 2 == 0 ? new[] { 0, 1, 2, 3, 4 } : new[] { 4, 3, 2, 1, 0 })
            {
                Restore();
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
                double ms = RunPasses(mode, iterations);
                allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
                Program.Emit(new { type = "timing", pair, mode = Names[mode], iterations, msPerPass = ms / iterations,
                    allocated, collections = new[] { GC.CollectionCount(0) - g0, GC.CollectionCount(1) - g1, GC.CollectionCount(2) - g2 } });
            }
        }
        double RunPasses(int mode, int count)
        {
            _mode = mode; long elapsed = 0;
            for (int i = 0; i < count; i++)
            {
                _clock++;
                long start = Stopwatch.GetTimestamp();
                if (mode == 0) WorkSchedule.Accrue(world, tick);
                else { WorkScheduleCombined.Accrue(world, tick); Flush(world, tick); }
                elapsed += Stopwatch.GetTimestamp() - start;
            }
            return elapsed * 1000.0 / Stopwatch.Frequency;
        }
    }

    private static void Setup(World world)
    {
        _bins = new Handle<Bin>[world.Businesses.Rows.Capacity];
        _remainders = new long[world.Citizens.Rows.Capacity];
        _pending = new long[_bins.Length]; _touched = new int[_bins.Length]; _waste = new long[_bins.Length];
        for (int j = 0; j < world.Businesses.Rows.SlotCount; j++)
        {
            if (!world.Businesses.Rows.IsLive(j)) continue;
            var handle = world.Bins.Rows.Allocate(); int bin = world.Bins.Rows.Resolve(handle);
            world.Bins.Capacity[bin] = long.MaxValue; world.Bins.OwnerKind[bin] = BinOwnerKind.Business;
            world.Bins.Resource[bin] = world.Bins.Resource[world.Bins.Rows.Resolve(world.Businesses.Balance[j])];
            _bins[j] = handle;
        }
        _dense = new Bucket4[world.Bins.Rows.Capacity]; _denseClocks = new ulong[_dense.Length];
        _sparse = new ExpiryStore(world.Bins.Rows);
        foreach (var bin in _bins) if (!bin.IsNone) _sparse.Add(bin);
    }

    internal static bool CanCombine(World world, Handle<Bin> bin)
    {
        int slot = world.Bins.Rows.Resolve(bin);
        return world.Bins.SupplyHead[slot] == 0 && world.Bins.SpaceHead[slot] == 0;
    }

    internal static void Deposit(World world, int citizen, int job, Ticks tick)
    {
        long tier = 100 + world.Citizens.SkillTier[citizen] * 25;
        long experience = world.Citizens.Experience[citizen] / 100;
        long premium = experience > 20 ? 20 : experience;
        long rate = IntegerMath.FloorDiv(IntegerMath.FloorDiv(_rate * tier, 100) * (100 + premium), 100);
        long scaled = _remainders[citizen] + rate;
        long whole = IntegerMath.ShiftRight(scaled, 11);
        _remainders[citizen] = scaled % Ticks.PerDay;
        if (whole == 0) return;
        var bin = _bins[job];
        if ((_mode == 2 || _mode == 4) && CanCombine(world, bin))
        {
            if (_pending[job] == 0) _touched[_touchedCount++] = job;
            _pending[job] += whole;
        }
        else Apply(world, job, whole, tick);
    }

    private static void Flush(World world, Ticks tick)
    {
        for (int i = 0; i < _touchedCount; i++)
        {
            int job = _touched[i]; Apply(world, job, _pending[job], tick); _pending[job] = 0;
        }
        _touchedCount = 0;
    }

    private static void Apply(World world, int job, long whole, Ticks tick)
    {
        var handle = _bins[job]; int bin = world.Bins.Rows.Resolve(handle);
        bool sparse = _mode >= 3;
        int index = sparse ? _sparse.Index[bin] - 1 : bin;
        Span<Bucket4> storage = sparse ? _sparse.Buckets.Span : _dense;
        Span<ulong> clocks = sparse ? _sparse.Clocks.Span : _denseClocks;
        ulong cycle = _clock / 32, delta = cycle - clocks[index];
        int elapsed = delta > 4 ? 4 : (int)delta;
        ref Bucket4 buckets = ref storage[index];
        long expired = 0;
        for (int i = 0; i < elapsed; i++) expired += buckets[i];
        if (elapsed != 0)
        {
            for (int i = 0; i < 4 - elapsed; i++) buckets[i] = buckets[i + elapsed];
            for (int i = 4 - elapsed; i < 4; i++) buckets[i] = 0;
            clocks[index] = cycle;
            if (expired > 0) { world.Withdraw(handle, expired, tick); _waste[job] += expired; }
        }
        buckets[3] += whole; world.Deposit(handle, whole, tick);
    }

    private static long[] State(World world)
    {
        var state = new List<long>(_remainders.Length + _bins.Length * 7);
        state.AddRange(_remainders);
        for (int job = 0; job < _bins.Length; job++)
        {
            if (_bins[job].IsNone) continue;
            int bin = world.Bins.Rows.Resolve(_bins[job]), s = _sparse.Index[bin] - 1;
            state.Add(world.Bins.LevelAt(bin)); state.Add(_waste[job]);
            state.Add((long)(_mode >= 3 ? _sparse.Clocks[s] : _denseClocks[bin]));
            for (int k = 0; k < 4; k++) state.Add(_mode >= 3 ? _sparse.Buckets[s][k] : _dense[bin][k]);
        }
        return state.ToArray();
    }

    private static void ReportMemory(World world, int goods)
    {
        long sparseWidth = _sparse.Rows.Columns.ToArray().Sum(c => c.BytesPerRow);
        long indexBytes = (long)_sparse.Index.Length * sizeof(int);
        Program.Emit(new { type = "memory", denseBytes = (long)_dense.Length * 40,
            sparseRowWidth = sparseWidth, sparseLive = _sparse.Rows.LiveCount, sparseCapacity = _sparse.Rows.Capacity,
            sparseIndexBytes = indexBytes, sparseBytes = sparseWidth * _sparse.Rows.Capacity + indexBytes,
            combinedScratchBytes = (long)_pending.Length * sizeof(long) + (long)_touched.Length * sizeof(int),
            sharedBinHandleBytes = (long)_bins.Length * 8, sharedWasteBytes = (long)_waste.Length * 8,
            sharedRemainderBytes = (long)_remainders.Length * 8,
            representation = "sparse row: 16 allocator + 8 Bin owner handle + 32 buckets + 8 clock; derived int Bin-to-row index" });
        foreach (int percent in new[] { 0, 25, 50, 100 })
        {
            int count = _sparse.Rows.LiveCount + goods * percent / 100;
            int capacity = 8; while (capacity < count) capacity *= 2;
            Program.Emit(new { type = "storage-sensitivity", goodsExpiringPercent = percent, expiringBins = count,
                sparseCapacity = capacity, sparseBytes = sparseWidth * capacity + indexBytes,
                denseBytes = (long)_dense.Length * 40, provenance = "calculated from measured widths and counts; not a perishable-content observation" });
        }
    }

    private static void CheckIndexRebuild(World world)
    {
        var before = _sparse.Index.ToArray(); _sparse.Rebuild();
        if (!before.AsSpan().SequenceEqual(_sparse.Index)) throw new InvalidOperationException("Index rebuild changed mapping");
        var store = new ExpiryStore(world.Bins.Rows);
        var handles = _bins.Where(b => !b.IsNone).Take(2).ToArray();
        int slot = store.Add(handles[0]); store.Remove(handles[0]); int reused = store.Add(handles[1]); store.Rebuild();
        if (slot != reused || store.Index[world.Bins.Rows.Resolve(handles[0])] != 0
            || store.Index[world.Bins.Rows.Resolve(handles[1])] != reused + 1)
            throw new InvalidOperationException("Expiry row reuse left a stale index");
    }

    private static void CheckWakeSemantics()
    {
        (ulong Hash, int Awake) Run(int mode, bool space)
        {
            var resource = new ResourceId(1); var rule = new RuleId(1);
            var rules = new Ruleset(resources: [ResourceFamily.Good],
                rules: [new RuleDefinition(1, 8, ApplyCount.Band(6, 6), RuleId.None, false, default, ConditionId.None, 0, space ? 0 : 1, 0, space ? 1 : 0, 0, 0)],
                kinds: [new KindDefinition(0, 1, 0, 1)], inputs: space ? [] : [new Term(new BinRef(Scope.Local, resource), 1)],
                outputs: space ? [new Term(new BinRef(Scope.Local, resource), 1)] : [], emissions: [], bins: [new BinDeclaration(resource, BinCapacity.Of(100))], kindRules: [rule], zoneRules: []);
            var world = new World(1000, rules);
            var lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
            var building = world.Buildings.Create(world.Lots, lot, 1); var bin = world.CreateBin(building, resource);
            if (space) world.Deposit(bin, 100, Ticks.Zero);
            for (int i = 0; i < 2; i++)
            {
                var sleeper = world.CreateRuleInstance(building, rule, Ticks.Zero, delay: 1);
                world.Wheel.PopDue(new Ticks(1)); world.Subscribe(sleeper, bin, space ? Blocking.Space : Blocking.Supply);
            }
            long pending = 0;
            foreach (long quantity in new long[] { 6, 1 })
            {
                if (mode == 1 || (mode == 2 && CanCombine(world, bin))) pending += quantity;
                else Move(quantity);
            }
            if (pending > 0) Move(pending);
            void Move(long quantity)
            {
                if (space) world.Withdraw(bin, quantity, new Ticks(1));
                else world.Deposit(bin, quantity, new Ticks(1));
            }
            int awake = Enumerable.Range(0, world.RuleInstances.Rows.SlotCount).Count(i => !world.RuleInstances.IsWaiting(i));
            return (world.HashState(), awake);
        }
        foreach (bool space in new[] { false, true })
        {
            var direct = Run(0, space); var unguarded = Run(1, space); var guarded = Run(2, space);
            if (direct.Awake != 2 || unguarded.Awake != 1 || guarded != direct)
                throw new InvalidOperationException("Wake semantics probe did not establish guarded equivalence");
            Program.Emit(new { type = "wake-probe", blocking = space ? "space" : "supply",
                directAwake = direct.Awake, unguardedAwake = unguarded.Awake,
                guardedAwake = guarded.Awake, guardedHashMatches = guarded.Hash == direct.Hash,
                consequence = "unconditional combining changes wake admission; guarded path preserves individual writes when either queue is nonempty" });
        }
    }
}

internal readonly struct Expiry;
internal sealed class ExpiryStore
{
    internal readonly Rows<Expiry> Rows;
    internal readonly HandleColumn<Bin> Owner;
    internal readonly Column<Bucket4> Buckets;
    internal readonly Column<ulong> Clocks;
    internal readonly int[] Index;
    private readonly Rows<Bin> _bins;
    internal ExpiryStore(Rows<Bin> bins)
    {
        _bins = bins; Rows = new Rows<Expiry>("expiry_probe", 8, Buffering.OneCopy);
        Owner = Rows.SavedHandle("bin", bins); Buckets = Rows.Saved<Bucket4>("buckets"); Clocks = Rows.Saved<ulong>("clock");
        Rows.Seal(); Index = new int[bins.Capacity];
    }
    internal int Add(Handle<Bin> bin)
    {
        int slot = Rows.Resolve(Rows.Allocate()); Owner[slot] = bin; Buckets[slot] = default; Clocks[slot] = 0;
        Index[_bins.Resolve(bin)] = slot + 1; return slot;
    }
    internal void Remove(Handle<Bin> bin)
    {
        int b = _bins.Resolve(bin); int row = Index[b] - 1; Index[b] = 0; Rows.Free(Rows.At(row));
    }
    internal void Rebuild()
    {
        Array.Clear(Index);
        for (int row = 0; row < Rows.SlotCount; row++) if (Rows.IsLive(row)) Index[_bins.Resolve(Owner[row])] = row + 1;
    }
}
