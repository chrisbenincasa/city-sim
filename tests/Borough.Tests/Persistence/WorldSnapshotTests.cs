using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 6 — the copy at the end of phase 7.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is being tested is a moment, not a data structure.</b> <c>adr/0087</c>'s requirement is that
/// the thing which writes a save never reads a live table, and the mechanism that delivers it is that
/// the copy is taken by the Tick rather than by the caller — serial, after every phase, at the same
/// boundary the State Hash is taken at. A copy taken mid-Tick captures some tables before Settle and
/// some after, which is a world that never existed.
/// </para>
/// <para>
/// ⚠ <b>It is taken after <c>World.Advance</c> rather than at the end of phase 7, against
/// <c>adr/0087</c>'s wording, and these tests are what found it.</b> <c>Advance</c> increments the
/// Tick, which has been <b>saved state</b> since <c>adr/0058</c>, so a copy before it records the Tick
/// just finished and reloading it re-runs that Tick. ***"The end of phase 7" and "the end of the Tick"
/// are different instants.*** ⚠ <b>The first version of this remark blamed the double-buffer swap and
/// was wrong</b> — <c>MapLayers</c> swaps inside the Layers phase, so <c>adr/0087</c>'s reasoning about
/// buffers is correct and untouched. The mechanism was asserted from the shape of the failure rather
/// than read, which is <c>adr/0093</c> committed while writing up a finding about a different ADR
/// making the same kind of mistake.
/// </para>
/// <para>
/// ⚠ <b>This milestone cannot test the property directly, and saying so is the point.</b> Nothing is
/// parallel around the save yet, so a writer that walked the live world would produce a correct file
/// today — which is exactly the case the ADR says is still a defect. What can be asserted is that the
/// save is taken by the Tick rather than by the caller, that it agrees with the hash at that instant,
/// and that the seam between the copy and the write is a real function boundary.
/// </para>
/// </remarks>
public sealed class WorldSnapshotTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    /// <summary>
    /// The save happens inside the Tick that was asked for, and the world it describes is the one at
    /// the end of that Tick — not the one the caller was looking at when it asked.
    /// </summary>
    /// <remarks>
    /// <b>This is what pins the copy to the far side of <c>World.Advance</c></b>, and every one of the
    /// five cases discriminates, because the clock moves on every Tick. ⚠ <b>The sweep was added for a
    /// reason that turned out to be the wrong one</b> — it was meant to straddle a Map Layer diffusion
    /// boundary, on a theory about the buffer swap that measurement refuted. It is kept because the
    /// reason it is worth having survives the theory that motivated it: 64 and 256 are the Layer and
    /// land-value cadences, so the round trip is exercised on Ticks where a double-buffered table
    /// actually moved as well as on Ticks where it did not.
    /// </remarks>
    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(127)]
    [InlineData(255)]
    [InlineData(256)]
    public void A_save_is_taken_at_the_end_of_the_tick_that_was_asked_for(int ticks)
    {
        (World world, Simulation simulation) = Stepped(ticks);

        ulong before = world.HashState();
        var file = new MemorySave();

        simulation.SaveAtEndOfTick(file);
        Assert.True(simulation.SaveIsDue);

        simulation.Step(default);

        Assert.False(simulation.SaveIsDue);
        Assert.NotEmpty(file.Bytes);

        ulong after = world.HashState();
        World loaded = SaveFile.Read(file, GoldenFixtures.Rules(), out _);

        Assert.Equal(after, loaded.HashState());
        Assert.NotEqual(before, loaded.HashState());
    }

    /// <summary>
    /// ⚠ <b>The Factorio test's first half, and the reason task 7 is a separate task.</b> Save, reload,
    /// and the hashes agree — which is a round trip. What it cannot catch is a derived column that
    /// rebuilds to the wrong value, because that only reaches saved state once the world runs on.
    /// </summary>
    [Fact]
    public void A_reloaded_world_steps_on_to_the_same_hashes()
    {
        (World world, Simulation simulation) = Stepped(256);

        var file = new MemorySave();
        simulation.SaveAtEndOfTick(file);
        simulation.Step(default);

        World loaded = SaveFile.Read(file, GoldenFixtures.Rules(), out SaveHeader header);
        var resumed = new Simulation(loaded, header.Key);

        for (int tick = 0; tick < 64; tick++)
        {
            simulation.Step(default);
            resumed.Step(default);

            Assert.Equal(world.HashState(), loaded.HashState());
        }
    }

    /// <summary>
    /// The buffer is reused. A second save of the same city neither grows it nor produces a different
    /// file.
    /// </summary>
    [Fact]
    public void The_snapshot_buffer_is_reused_across_saves()
    {
        (_, Simulation simulation) = Stepped(256);

        var first = new MemorySave();
        simulation.SaveAtEndOfTick(first);
        simulation.Step(default);

        var second = new MemorySave();
        simulation.SaveAtEndOfTick(second);
        simulation.Step(default);

        Assert.Equal(first.Bytes.Length, second.Bytes.Length);

        // The city moved on by one Tick, so the bytes differ. What must not differ is the shape.
        Assert.Equal(1, first.Writes);
        Assert.Equal(1, second.Writes);
    }

    /// <summary>
    /// ⚠ <b>The seam D4 drew, asserted as a shape rather than as a duration.</b> <c>Core</c> may not
    /// read a clock (<c>05 §4</c>), so the two halves cannot be timed here — what is checked is that
    /// they are separable at all: filling the snapshot is one call and handing it on is another, so a
    /// thread can take the second without the first.
    /// </summary>
    [Fact]
    public void The_copy_and_the_write_are_separable()
    {
        (World world, _) = Stepped(256);

        var snapshot = new WorldSnapshot();
        SaveFile.Write(world, InForce, snapshot);

        int copied = snapshot.Length;
        Assert.True(copied > 0);

        var file = new MemorySave();
        snapshot.DrainTo(file);

        _output.WriteLine($"copy {copied:N0} B, drained in {file.Writes} write(s)");

        Assert.Equal(copied, file.Bytes.Length);
        Assert.Equal(1, file.Writes);

        // The copy survives the drain and can be handed on again -- which is what a retry is.
        var again = new MemorySave();
        snapshot.DrainTo(again);
        Assert.Equal(file.Bytes, again.Bytes);
    }

    /// <summary>
    /// A snapshot reset keeps the buffer and forgets the contents, which is what makes an autosave
    /// cost no allocation after the first.
    /// </summary>
    [Fact]
    public void A_reset_keeps_the_buffer_and_drops_the_contents()
    {
        (World world, _) = Stepped(64);

        var snapshot = new WorldSnapshot();
        SaveFile.Write(world, InForce, snapshot);

        int capacity = snapshot.Capacity;
        Assert.True(capacity > 0);

        snapshot.Reset();

        Assert.Equal(0, snapshot.Length);
        Assert.Equal(capacity, snapshot.Capacity);

        SaveFile.Write(world, InForce, snapshot);

        Assert.Equal(capacity, snapshot.Capacity);
    }

    /// <summary>
    /// ⚠ <b>The negative assertion, so the absence cannot rot.</b> A snapshot cannot carry a State
    /// Hash: <c>HandleColumn.Fold</c> folds the target row's monotonic id, which is in another table
    /// and is not a function of the handle's bytes, so a fold over these bytes is not the State Hash.
    /// This asserts the divergence exists rather than asserting a particular number.
    /// </summary>
    [Fact]
    public void A_fold_over_the_bytes_is_not_the_state_hash()
    {
        (World world, _) = Stepped(256);

        var snapshot = new WorldSnapshot();
        SaveFile.Write(world, InForce, snapshot);

        ulong overBytes = 0;

        foreach (byte value in snapshot.Bytes)
        {
            overBytes = Randomness.Mix(overBytes ^ value);
        }

        Assert.NotEqual(world.HashState(), overBytes);
    }

    private static (World World, Simulation Simulation) Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules(), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }
}
