using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Golden;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

public sealed class LandPermissionTests(ITestOutputHelper output)
{
    private static readonly GroundPermissions Housing = new(1, 2);
    private static readonly GroundPermissions Trade = new(2, 3, true, 4);

    [Fact]
    public void Cross_page_paint_splits_coalesces_and_preserves_unaffected_ground()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        Assert.Equal(PermissionRefusal.None, land.Paint(new(30, 30, 5, 5), Housing, 128));
        Assert.Equal(4, table.Rows.LiveCount);
        Assert.Equal(PermissionRefusal.None, land.Paint(new(31, 31, 2, 2), Trade, 128));
        for (int y = 29; y < 36; y++)
        {
            for (int x = 29; x < 36; x++)
            {
                GroundPermissions expected = x is >= 31 and < 33 && y is >= 31 and < 33 ? Trade
                    : x is >= 30 and < 35 && y is >= 30 and < 35 ? Housing : default;
                Assert.Equal(expected, land.At(x, y));
            }
        }
        land.Paint(new(30, 30, 5, 5), Housing, 128);
        Assert.Equal(4, table.Rows.LiveCount);
        land.Paint(new(30, 30, 5, 5), default, 128);
        Assert.Equal(0, table.Rows.LiveCount);
    }

    [Fact]
    public void Form_paint_preserves_use_and_band_and_empty_is_not_unrestricted()
    {
        var land = new LandPermissions(new LandPermissionTable());
        land.Paint(new(0, 0, 32, 32), Housing, 128);
        land.Paint(new(32, 0, 32, 32), Trade, 128);
        land.PaintForms(new(30, 0, 6, 32), true, 0, 128);
        Assert.Equal(Housing with { RestrictsForms = true }, land.At(31, 2));
        Assert.Equal(Trade with { Forms = 0 }, land.At(33, 2));
        Assert.Equal(PermissionRefusal.Form, land.Check(new(30, 0, 2, 32), 1, 4, out _));
        land.PaintForms(new(30, 0, 6, 32), false, 65535, 128);
        Assert.Equal(PermissionRefusal.None, land.Check(new(30, 0, 2, 32), 1, 4, out byte band));
        Assert.Equal(2, band);
        Assert.Equal(PermissionRefusal.Use, land.Check(new(30, 0, 6, 32), 1, 4, out _));
        Assert.Equal(PermissionRefusal.MixedIntensity, land.Check(new(30, 0, 6, 32), 3, 4, out _));
        Assert.Equal(PermissionRefusal.Unzoned, land.Check(new(63, 0, 2, 1), 3, 4, out _));
        land.PaintForms(new(100, 100, 1, 1), true, 4, 128);
        Assert.Equal(new GroundPermissions(0, 0, true, 4), land.At(100, 100));
        Assert.Equal(PermissionRefusal.Unzoned, land.Check(new(100, 100, 1, 1), 1, 4, out _));
    }

    [Fact]
    public void Noop_invalid_bounds_and_capacity_refusal_preserve_allocator_and_hash()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        land.Paint(new(0, 0, 256, 32), Housing, 8);
        Assert.Equal(8, table.Rows.LiveCount);
        ulong hash = Hash(table);
        ulong id = table.Rows.NextId;
        int capacity = table.Rows.Capacity;
        Assert.Equal(PermissionRefusal.None, land.Paint(new(0, 0, 256, 32), Housing, 8));
        Assert.Equal(PermissionRefusal.RecordLimit, land.Paint(new(31, 0, 2, 1), Trade, 8));
        Assert.Equal(PermissionRefusal.InvalidBounds, land.Paint(new(int.MaxValue, 0, 1, 1), Trade, 8));
        Assert.Equal(PermissionRefusal.InvalidBounds, land.Paint(new(-1, 0, 1, 1), Trade, 8));
        Assert.Equal(hash, Hash(table));
        Assert.Equal(id, table.Rows.NextId);
        Assert.Equal(capacity, table.Rows.Capacity);
        Assert.Equal(Housing, land.At(31, 0));
    }

    [Fact]
    public void Whole_operation_count_allows_later_compaction_to_pay_for_earlier_growth()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        land.Paint(new(0, 0, 32, 32), Housing, 8);
        for (int x = 32; x < 39; x++) { land.Paint(new(x, 0, 1, 32), x % 2 == 0 ? Housing : Trade, 8); }
        Assert.Equal(8, table.Rows.LiveCount);
        // First page splits from 1 to 2; the second shrinks from 7 to 1.
        Assert.Equal(PermissionRefusal.None, land.Paint(new(16, 0, 48, 32), Trade, 8));
        Assert.Equal(3, table.Rows.LiveCount);
        Assert.Equal(8, table.Rows.SlotCount);
        Assert.Equal(8, table.Rows.Capacity);
    }

    [Fact]
    public void Repeated_fragmentation_and_clear_reuse_bounded_slots()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    Assert.Equal(PermissionRefusal.None, land.Paint(new(x, y, 1, 1), (x + y) % 2 == 0 ? Housing : Trade, 1024));
                }
            }
            Assert.Equal(1024, table.Rows.LiveCount);
            Assert.Equal(1024, table.Rows.Capacity);
            land.Paint(new(0, 0, 32, 32), Housing, 1024);
            Assert.Equal(1, table.Rows.LiveCount);
            land.Paint(new(0, 0, 32, 32), default, 1024);
            Assert.Equal(0, table.Rows.LiveCount);
            Assert.Equal(1024, table.Rows.SlotCount);
        }
    }

    [Fact]
    public void Deterministic_random_paint_matches_dense_oracle_and_rebuild()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        var oracle = new GroundPermissions[64 * 64];
        var random = new Random(620018);
        for (int i = 0; i < 300; i++)
        {
            int x = random.Next(64), y = random.Next(64);
            int width = random.Next(1, 65 - x), height = random.Next(1, 65 - y);
            GroundPermissions permission = i % 3 == 0 ? default : i % 3 == 1 ? Housing : Trade;
            bool formsOnly = i % 7 == 0;
            var bounds = new LandRectangle(x, y, width, height);
            Assert.Equal(PermissionRefusal.None, formsOnly
                ? land.PaintForms(bounds, permission.RestrictsForms, permission.Forms, 4096)
                : land.Paint(bounds, permission, 4096));
            for (int dy = y; dy < y + height; dy++)
            {
                for (int dx = x; dx < x + width; dx++)
                {
                    oracle[dy * 64 + dx] = formsOnly
                        ? oracle[dy * 64 + dx] with { RestrictsForms = permission.RestrictsForms, Forms = permission.Forms }
                        : permission;
                }
            }
            if (i % 10 == 0) { land.Rebuild(); }
            for (int dy = 0; dy < 64; dy++)
            {
                for (int dx = 0; dx < 64; dx++) { Assert.Equal(oracle[dy * 64 + dx], land.At(dx, dy)); }
            }
        }
    }

    [Fact]
    public void World_hash_save_load_and_continued_allocator_identity_agree()
    {
        var rules = GoldenFixtures.Rules();
        var world = new World(0, rules);
        ulong empty = world.HashState();
        world.PaintPermissions(new(30, 30, 5, 5), Housing);
        world.PaintFormPermissions(new(31, 31, 2, 2), true, 0);
        Assert.NotEqual(empty, world.HashState());
        world.PaintPermissions(new(30, 30, 1, 1), default);
        var file = new MemorySave();
        SaveFile.Write(world, 123, file);
        World loaded = SaveFile.Read(file, rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        Assert.Equal(world.LandPermissions.At(31, 31), loaded.LandPermissions.At(31, 31));
        foreach (World city in new[] { world, loaded })
        {
            city.PaintPermissions(new(0, 0, 64, 64), Trade);
            city.PaintFormPermissions(new(31, 0, 2, 64), false, 0);
        }
        Assert.Equal(world.HashState(), loaded.HashState());
        Assert.Equal(world.PermissionRectangles.Rows.NextId, loaded.PermissionRectangles.Rows.NextId);
    }

    [Fact]
    public void Ruleset_limit_refuses_load_and_reload_below_saved_high_water()
    {
        var rules = GoldenFixtures.Rules();
        var world = new World(0, rules);
        world.PaintPermissions(new(0, 0, 320, 32), Housing);
        world.PaintPermissions(new(0, 0, 320, 32), default);
        Assert.Equal(10, world.PermissionRectangles.Rows.SlotCount);
        // Loader exercises the authored limit without a test-only mutation door.
        string text = File.ReadAllText(GoldenFixtures.RulesetPath) + "\n[land_permissions]\nmax_records = 8\n";
        var result = RulesetLoader.Parse(text, "test.toml");
        Assert.True(result.Ok);
        var small = result.Ruleset!;
        ulong hash = world.HashState();
        Assert.Throws<InvalidOperationException>(() => world.Adopt(small, 1, new Ticks(0), default));
        Assert.Equal(hash, world.HashState());
        var file = new MemorySave();
        SaveFile.Write(world, 123, file);
        Assert.Throws<InvalidOperationException>(() => SaveFile.Read(file, small, out _));
    }

    [Fact]
    public void Csharp_column_budget_includes_allocator_and_derived_links()
    {
        var table = new LandPermissionTable();
        int bytes = table.Rows.Columns.ToArray().Sum(column => column.BytesPerRow);
        Assert.Equal(44, bytes);
        Assert.Equal(40, table.Rows.SavedBytesPerRow);
        output.WriteLine($"Column payload at 1048576 slots: {bytes * 1048576L} bytes; directory: {CellGrid.WorldCellCount * sizeof(int)} bytes.");
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Measure_actual_full_budget_managed_allocation()
    {
        // Warm construction/JIT before measuring allocations on this thread.
        _ = new LandPermissions(new LandPermissionTable());
        long before = GC.GetAllocatedBytesForCurrentThread();
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        table.Rows.PrepareCapacity(1_048_576, 1_048_576);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        output.WriteLine($"Allocated managed bytes (table, directory, column growth, headers/bookkeeping): {allocated}");
        Assert.InRange(allocated, 45L * 1024 * 1024, 46L * 1024 * 1024);
        GC.KeepAlive(land);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("268435457")]
    [InlineData("1.5")]
    [InlineData("\"many\"")]
    public void Invalid_authored_limits_are_refused(string value)
    {
        var result = RulesetLoader.Parse("[[resource]]\nname = \"flour\"\nfamily = \"good\"\n"
            + "[land_permissions]\nmax_records = " + value, "test.toml");
        Assert.False(result.Ok);
    }

    [Fact]
    public void Non_power_of_two_limit_caps_growth_and_ruleset_copy_retains_it()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        Assert.Equal(PermissionRefusal.None, land.Paint(new(0, 0, 288, 32), Housing, 9));
        Assert.Equal(9, table.Rows.Capacity);
        Assert.Equal(PermissionRefusal.RecordLimit, land.Paint(new(288, 0, 32, 32), Housing, 9));
        var result = RulesetLoader.Parse("[[resource]]\nname = \"flour\"\nfamily = \"good\"\n"
            + "[land_permissions]\nmax_records = 9", "test.toml");
        Assert.True(result.Ok, result.Describe());
        Assert.Equal(9, result.Ruleset!.WithLayers(result.Ruleset.Layers).PermissionRecordLimit);
    }

    [Fact]
    public void Invariant_detects_overlap_and_stale_index_without_mutating_saved_state()
    {
        var world = new World(0, GoldenFixtures.Rules());
        world.PaintPermissions(new(0, 0, 1, 1), Housing);
        world.PaintPermissions(new(1, 0, 1, 1), Trade);
        Assert.True(world.LandPermissions.IsValid(out _));
        world.PermissionRectangles.X[1] = 0;
        ulong hash = world.HashState();
        Assert.False(world.LandPermissions.IsValid(out _));
        Assert.Equal(hash, world.HashState());
        Assert.Throws<Borough.Core.Invariants.InvariantViolationException>(() =>
            Borough.Core.Invariants.WorldInvariants.LandPermissionsAreWellFormed(world, world.Invariants));
    }

    [Fact]
    public void Preparing_capacity_changes_no_saved_state_and_unchanged_pages_keep_identity()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        land.Paint(new(0, 0, 64, 32), Housing, 128);
        ulong hash = Hash(table);
        table.Rows.PrepareCapacity(100, 100);
        Assert.Equal(hash, Hash(table));
        var untouched = table.Rows.At(1);
        land.Paint(new(0, 0, 32, 32), Trade, 128);
        Assert.True(table.Rows.IsValid(untouched));
        Assert.Equal(PermissionRefusal.InvalidForm, land.Check(new(32, 0, 32, 32), 1, 3, out _));
        Assert.Equal(PermissionRefusal.InvalidForm, land.Check(new(32, 0, 32, 32), 1, 0, out _));
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Oversized_world_paint_counts_before_allocating_staged_rectangles()
    {
        var table = new LandPermissionTable();
        var land = new LandPermissions(table);
        land.Paint(new(0, 0, 1, 1), Housing, 8); // warm the path
        ulong hash = Hash(table);
        long before = GC.GetAllocatedBytesForCurrentThread();
        var refusal = land.Paint(new(0, 0, CellGrid.WorldTiles, CellGrid.WorldTiles), Trade, 8);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(PermissionRefusal.RecordLimit, refusal);
        Assert.Equal(hash, Hash(table));
        Assert.Equal(8, table.Rows.Capacity);
        Assert.InRange(allocated, 1_048_576, 1_050_000);
        output.WriteLine($"Refused whole-world paint: {allocated} managed bytes; no staged rectangles or column growth.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Save_load_refuses_invalid_geographic_state_even_with_a_matching_saved_hash(int corruption)
    {
        var rules = GoldenFixtures.Rules();
        var world = new World(0, rules);
        world.PaintPermissions(new(0, 0, 1, 1), Housing);
        if (corruption == 0) { world.PermissionRectangles.Width[0] = 33; }
        if (corruption == 1) { world.PermissionRectangles.Permission[0] = 1UL << 63; }
        if (corruption == 2) { world.PermissionRectangles.Height[0] = 0; }
        var file = new MemorySave();
        SaveFile.Write(world, 1, file);
        Assert.Throws<InvalidOperationException>(() => SaveFile.Read(file, rules, out _));
    }

    [Fact]
    public void Authored_limit_defaults_and_duplicate_table_refusal_are_explicit()
    {
        const string resource = "[[resource]]\nname = \"flour\"\nfamily = \"good\"\n";
        var missing = RulesetLoader.Parse(resource, "test.toml");
        Assert.True(missing.Ok, missing.Describe());
        Assert.Equal(Borough.Core.Rules.Ruleset.DefaultPermissionRecordLimit, missing.Ruleset!.PermissionRecordLimit);
        var duplicate = RulesetLoader.Parse(resource
            + "[land_permissions]\nmax_records = 8\n[land_permissions]\nmax_records = 9", "test.toml");
        Assert.False(duplicate.Ok);
    }

    private static ulong Hash(LandPermissionTable table)
    {
        ulong hash = 0;
        table.Rows.Fold(ref hash);
        return hash;
    }
}
