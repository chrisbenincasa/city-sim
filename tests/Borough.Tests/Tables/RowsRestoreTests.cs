using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Persistence;
using Borough.Core.Tables;
using Borough.Tests.Golden;

namespace Borough.Tests.Tables;

/// <summary>
/// Milestone 8 task 3 — the allocator restore path, slot-exact.
/// </summary>
/// <remarks>
/// <para>
/// <b>The largest item in the milestone and the one with no precedent in the tree.</b> Before this,
/// <c>Rows</c> had no restore path at all: the four allocator scalars were private with no accessors
/// for two of them and no setters for any, and <c>AllocateSlot</c>/<c>FreeSlot</c> are
/// <c>private protected</c>, so nothing outside the class hierarchy could place a row in a chosen
/// slot. Writing bytes out was a new member on <c>Column</c>; getting them back in was a capability
/// the table layer did not have.
/// </para>
/// <para>
/// <b>Slot-exactness is the whole constraint</b> (<c>adr/0086</c>): the free list and the id counter
/// are <em>saved state</em> rather than bookkeeping to recompute, so a loader that rebuilt the free
/// list by scanning for dead rows would hand the next row a different slot.
/// </para>
/// <para>
/// <b>⚠ And the State Hash already catches that, which is worth stating because it is easy to write
/// the opposite.</b> <c>Rows.Fold</c> folds <c>_freeHead</c> as one of its four scalars <em>and</em>
/// folds <c>free_next</c>, which is a <see cref="Disposition.Saved"/> column — so the whole free list
/// is inside the hash, head and chain, and a recomputed one diverges <b>at the load</b> rather than
/// downstream. The first draft of this file claimed the reverse. What
/// <see cref="The_next_allocation_lands_where_the_save_says_it_will"/> adds is not detection but
/// <em>legibility</em>: it fails saying <em>the next Household went to a different slot</em> where the
/// hash fails saying two 64-bit numbers differ. ***A second instrument for something already covered
/// earns its place by naming the consequence, not by catching more.***
/// </para>
/// </remarks>
public sealed class RowsRestoreTests
{
    /// <summary>One table's saved state: the four scalars and its saved columns' bytes.</summary>
    private sealed record Snapshot(
        int SlotCount, int LiveCount, int FreeHead, ulong NextId, byte[] Bytes);

    /// <summary>
    /// A world restored from bytes hashes as it did when it was saved, after running on.
    /// </summary>
    /// <remarks>
    /// This is the whole of tasks 2 and 3 composed, and it is as close to <c>05 §4</c> invariant 6 as
    /// the milestone gets before there is a file. What it does not cover is the header and the Ruleset
    /// — tasks 4 and 5 — and the <em>run M more Ticks</em> half, which is task 7.
    /// </remarks>
    [Fact]
    public void A_world_restores_to_the_hash_it_was_saved_at()
    {
        (World world, Simulation simulation) = Stepped(512);

        ulong saved = world.HashState();
        Snapshot[] snapshot = Save(world);

        for (int tick = 0; tick < 128; tick++)
        {
            simulation.Step(default);
        }

        Assert.NotEqual(saved, world.HashState());

        Restore(world, snapshot);

        Assert.Equal(saved, world.HashState());

        // And the derived half comes back from the saved half, which is task 1's claim reached
        // through a load rather than through a rebuild on a running world.
        world.RebuildDerived();

        Assert.Equal(saved, world.HashState());
    }

    /// <summary>
    /// The next row allocated after a restore lands in the slot it would have landed in without one.
    /// </summary>
    /// <remarks>
    /// <b>What slot-exactness means, said in the currency it is about.</b> The hash covers this
    /// already — see this class's remarks, the free list is folded head and chain — so the value here
    /// is the failure message rather than the coverage: a broken restore reports <em>the next
    /// Household went to a different slot</em> instead of two 64-bit numbers disagreeing. It is also
    /// the assertion that survives if the free list ever leaves the hash, which is the only reason to
    /// prefer a behavioural test to a cheaper one.
    /// </remarks>
    [Fact]
    public void The_next_allocation_lands_where_the_save_says_it_will()
    {
        (World world, _) = SteppedWithAGap(512);

        Rows<Household> households = world.Households.Rows;

        // The fixture must actually have a free list, or this test is asserting something about an
        // append-only table and would pass for the wrong reason. SteppedWithAGap says why one has to
        // be made rather than found.
        Assert.NotEqual(Rows.NoSlot, households.FreeHead);

        Snapshot[] snapshot = Save(world);

        Handle<Household> first = households.Allocate();
        int slotWithoutRestore = households.Resolve(first);
        ulong idWithoutRestore = households.IdAt(slotWithoutRestore);

        Restore(world, snapshot);

        Handle<Household> second = households.Allocate();
        int slotAfterRestore = households.Resolve(second);

        Assert.Equal(slotWithoutRestore, slotAfterRestore);
        Assert.Equal(idWithoutRestore, households.IdAt(slotAfterRestore));
        Assert.Equal(first, second);
    }

    /// <summary>
    /// A freed slot holds zeroes, which is what makes a byte-exact round trip achievable at all.
    /// </summary>
    /// <remarks>
    /// <c>FreeSlot</c> clears every column at the slot, so the residue between live rows is
    /// <em>reproducible</em> rather than arbitrary — two runs that free the same slots hold the same
    /// bytes there. A table that left a freed row's data in place would still hash consistently, but
    /// the file would carry the ghosts of demolished Buildings and a compaction would become
    /// observable. Asserted here because the restore's own verification relies on it.
    /// </remarks>
    [Fact]
    public void A_freed_slot_holds_zeroes()
    {
        (World world, _) = SteppedWithAGap(512);

        Rows<Household> households = world.Households.Rows;
        int free = households.FreeHead;

        Assert.NotEqual(Rows.NoSlot, free);
        Assert.False(households.IsLive(free));
        Assert.Equal(0UL, households.IdAt(free));
        Assert.Equal(new Money(0), world.Households.Money[free]);
        Assert.Equal(new Money(0), world.Households.Savings[free]);
    }

    /// <summary>A header claiming more live rows than there are slots is refused.</summary>
    [Fact]
    public void A_live_count_above_the_slot_count_is_refused()
    {
        (World world, _) = Stepped(64);
        Snapshot[] snapshot = Save(world);

        Rows table = world.Households.Rows;
        Snapshot bad = Corrupt(world, snapshot, table) with { LiveCount = table.SlotCount + 1 };

        Assert.Contains(
            "live count above the slot count",
            Assert.Throws<InvalidOperationException>(() => Apply(table, bad)).Message,
            StringComparison.Ordinal);
    }

    /// <summary>A free head pointing outside the table is refused.</summary>
    [Fact]
    public void A_free_head_outside_the_table_is_refused()
    {
        (World world, _) = Stepped(64);
        Snapshot[] snapshot = Save(world);

        Rows table = world.Households.Rows;
        Snapshot bad = Corrupt(world, snapshot, table) with { FreeHead = table.SlotCount + 7 };

        Assert.Contains(
            "free head",
            Assert.Throws<InvalidOperationException>(() => Apply(table, bad)).Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A free list that points at itself is refused rather than walked, because the alternative is a
    /// load that hangs.
    /// </summary>
    [Fact]
    public void A_cyclic_free_list_is_refused_rather_than_walked()
    {
        (World world, _) = SteppedWithAGap(512);
        Snapshot[] snapshot = Save(world);

        Rows table = world.Households.Rows;
        Snapshot original = Corrupt(world, snapshot, table);

        Assert.NotEqual(Rows.NoSlot, original.FreeHead);

        byte[] bytes = [.. original.Bytes];
        int offset = ColumnOffset(table, "free_next") + (original.FreeHead * sizeof(int));
        BitConverter.TryWriteBytes(bytes.AsSpan(offset), original.FreeHead);

        Assert.Contains(
            "cycle",
            Assert.Throws<InvalidOperationException>(
                () => Apply(table, original with { Bytes = bytes })).Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A dead slot carrying a non-zero id is refused, because this allocator cannot produce one.
    /// </summary>
    /// <remarks>
    /// The check that says the residue in the file was written by <em>this</em> allocator rather than
    /// by something that compacted, reordered or hand-edited it. It is the cheapest available test of
    /// the *"do not compact"* rule the milestone states and cannot otherwise enforce.
    /// </remarks>
    [Fact]
    public void A_dead_slot_carrying_an_id_is_refused()
    {
        (World world, _) = SteppedWithAGap(512);
        Snapshot[] snapshot = Save(world);

        Rows table = world.Households.Rows;
        Snapshot original = Corrupt(world, snapshot, table);

        Assert.NotEqual(Rows.NoSlot, original.FreeHead);

        byte[] bytes = [.. original.Bytes];
        int offset = ColumnOffset(table, "id") + (original.FreeHead * sizeof(ulong));
        BitConverter.TryWriteBytes(bytes.AsSpan(offset), 4_242UL);

        Assert.Contains(
            "rather than 0",
            Assert.Throws<InvalidOperationException>(
                () => Apply(table, original with { Bytes = bytes })).Message,
            StringComparison.Ordinal);
    }

    /// <summary>A byte count that does not match the declaration is refused.</summary>
    [Fact]
    public void A_short_buffer_is_refused()
    {
        (World world, _) = Stepped(64);
        Snapshot[] snapshot = Save(world);

        Rows table = world.Households.Rows;
        Snapshot original = Corrupt(world, snapshot, table);

        Snapshot bad = original with { Bytes = original.Bytes[..^1] };

        Assert.Throws<InvalidOperationException>(() => Apply(table, bad));
    }

    /// <summary>The snapshot belonging to a given table.</summary>
    private static Snapshot Corrupt(World world, Snapshot[] snapshot, Rows table)
    {
        int index = 0;

        foreach (Rows candidate in world.Tables)
        {
            if (ReferenceEquals(candidate, table))
            {
                return snapshot[index];
            }

            index++;
        }

        throw new InvalidOperationException($"table '{table.Name}' is not in this world.");
    }

    private static void Apply(Rows table, Snapshot snapshot)
    {
        var source = new BlobSource(snapshot.Bytes);

        table.Restore(
            snapshot.SlotCount, snapshot.LiveCount, snapshot.FreeHead, snapshot.NextId, source);

        if (source.Unread != 0)
        {
            throw new InvalidOperationException(
                $"table '{table.Name}' left {source.Unread} of {snapshot.Bytes.Length} bytes unread. A "
                + "column set that is the right shape and the wrong length is a format version the "
                + "header should have refused.");
        }
    }

    /// <summary>
    /// One table's saved columns, concatenated, as a source. ⚠ <b>A save is streamed rather than
    /// blobbed as of task 5</b> — this exists so these tests can still corrupt a byte at a known offset,
    /// which is the whole point of them; <c>SaveFileTests</c> is where the real one-pass source is
    /// exercised.
    /// </summary>
    private sealed class BlobSource(byte[] bytes) : ISaveSource
    {
        private int _read;

        public int Unread => bytes.Length - _read;

        public void Read(Span<byte> into)
        {
            if (_read + into.Length > bytes.Length)
            {
                throw new InvalidOperationException(
                    $"this blob is {bytes.Length} bytes and a reader asked for {into.Length} more at "
                    + $"offset {_read}.");
            }

            bytes.AsSpan(_read, into.Length).CopyTo(into);
            _read += into.Length;
        }
    }

    /// <summary>Where a named saved column's bytes begin inside a table's blob.</summary>
    private static int ColumnOffset(Rows table, string name)
    {
        int offset = 0;

        foreach (Column column in table.SavedColumns)
        {
            if (column.Name == name)
            {
                return offset;
            }

            offset += column.BytesPerRow * table.SlotCount;
        }

        throw new InvalidOperationException($"table '{table.Name}' declares no saved column '{name}'.");
    }

    private static Snapshot[] Save(World world)
    {
        List<Snapshot> snapshots = [];

        foreach (Rows table in world.Tables)
        {
            int total = 0;

            foreach (Column column in table.SavedColumns)
            {
                total += column.BytesPerRow * table.SlotCount;
            }

            byte[] bytes = new byte[total];
            int offset = 0;

            foreach (Column column in table.SavedColumns)
            {
                Span<byte> storage = column.StorageBytes(table.SlotCount);
                storage.CopyTo(bytes.AsSpan(offset, storage.Length));
                offset += storage.Length;
            }

            snapshots.Add(new Snapshot(
                table.SlotCount, table.LiveCount, table.FreeHead, table.NextId, bytes));
        }

        return [.. snapshots];
    }

    private static void Restore(World world, Snapshot[] snapshots)
    {
        int index = 0;

        foreach (Rows table in world.Tables)
        {
            Apply(table, snapshots[index++]);
        }

        Assert.Equal(snapshots.Length, index);
    }

    /// <summary>
    /// A stepped world with a hole in its Household table, because a generated city does not make one.
    /// </summary>
    /// <remarks>
    /// <b>⚠ 512 Ticks of the shipped Ruleset frees no Household at all, and that is correct rather
    /// than surprising.</b> <c>adr/0054</c> sends a demolished Building's Households to the Unplaced
    /// Pool with their money intact, so demolition — the only thing in a generated city that destroys
    /// anything — never retires a Household row. The free list this test needs therefore has to be
    /// made on purpose, which is the same reason <c>GoldenFixtures.Build</c> retires a Household and a
    /// Citizen by hand: <em>"so the free list and the never-reused id counter are both off their
    /// initial values"</em>. ***A restore path tested only against append-only tables is a restore
    /// path whose free list has never been read.***
    /// </remarks>
    private static (World World, Simulation Simulation) SteppedWithAGap(int ticks)
    {
        (World world, Simulation simulation) = Stepped(ticks);

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot))
            {
                world.DestroyHousehold(world.Households.Rows.At(slot));
                break;
            }
        }

        Assert.NotEqual(Rows.NoSlot, world.Households.Rows.FreeHead);

        return (world, simulation);
    }

    private static (World World, Simulation Simulation) Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules());

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }
}
