using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 10 — the State Hash, folded from a copy instead of from the world.
/// </summary>
/// <remarks>
/// <para>
/// <b>One assertion carries this file and every other test here is a way of stressing it:</b>
/// <c>SaveHash.Of(world, body)</c> equals <c>world.HashState()</c> at the instant the body was taken.
/// That equality is what lets a save carry a verified hash for <b>no cost on the simulation thread</b>
/// (<c>adr/0112</c>), and it is what holds the one fold honest against its two sources — the live
/// columns and a buffer of bytes.
/// </para>
/// <para>
/// ⚠ <b>The claim it overturns was that this is not buildable.</b> Milestone 8 task 6 recorded that a
/// save could not carry a hash, because <c>HandleColumn.Fold</c> folds the target row's monotonic id
/// and a handle's bytes do not contain one. True — and it does not follow, because <c>Rows</c> declares
/// <c>id</c> and <c>generation</c> as saved columns, so the id is in the <em>copy</em> even though it is
/// not in the handle. ***A value absent from a column's own bytes can still be present in the file.***
/// <c>WorldSnapshotTests.A_fold_over_the_bytes_is_not_the_state_hash</c> is the other half of this pair
/// and stays true: a naive fold over the buffer is still not the hash, and it never was the question.
/// </para>
/// <para>
/// <b>Every case steps the world first, because the interesting content is the handles.</b> A world at
/// Tick 0 has few and a world that has run has dangling ones, severed ones and reallocated slots — the
/// three cases where folding the id differs from folding the handle.
/// </para>
/// </remarks>
public sealed class SaveHashTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    /// <summary>
    /// The load-bearing one. The fold over a copy is the State Hash, at every instant tried.
    /// </summary>
    /// <remarks>
    /// <b>The sweep is over Ticks rather than over worlds</b>, because what varies with elapsed time is
    /// the thing this is about: demolition frees rows, the free list recycles slots, and handles into
    /// recycled slots are exactly where folding an id and folding a handle come apart. 0 is included so
    /// that a world with almost no handles is covered too, and 256 is past the Zone Rules' first
    /// condemnations.
    /// </remarks>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(256)]
    [InlineData(1024)]
    public void A_fold_over_the_copy_is_the_state_hash(int ticks)
    {
        World world = Stepped(ticks);

        var body = new MemorySave();
        SaveFile.WriteBody(world, body);

        ulong live = world.HashState();
        ulong copied = SaveHash.Of(world, body.Bytes);

        _output.WriteLine($"tick {ticks}: live 0x{live:X16}, copy 0x{copied:X16}, {body.Bytes.Length} bytes");

        Assert.Equal(live, copied);
    }

    /// <summary>
    /// The equality is not an accident of a hash that ignores its input: a world one Tick further on
    /// folds to a different number down both paths, and the two still agree.
    /// </summary>
    /// <remarks>
    /// <b>Without this the test above passes for a fold that returns a constant.</b> It is the cheapest
    /// discriminator between <em>the two paths agree</em> and <em>the two paths agree because neither
    /// reads anything</em>.
    /// </remarks>
    [Fact]
    public void The_copied_hash_moves_when_the_world_does()
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules(), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        ulong previous = 0;

        for (int tick = 0; tick < 8; tick++)
        {
            simulation.Step(default);

            var body = new MemorySave();
            SaveFile.WriteBody(world, body);

            ulong copied = SaveHash.Of(world, body.Bytes);

            Assert.Equal(world.HashState(), copied);
            Assert.NotEqual(previous, copied);

            previous = copied;
        }
    }

    /// <summary>
    /// A save carries the hash and a load checks it, so <c>05 §4</c> invariant 6 is a property of every
    /// load rather than of the cases in this suite.
    /// </summary>
    [Fact]
    public void A_save_carries_the_hash_of_the_world_it_was_taken_from()
    {
        World world = Stepped(256);

        var file = new MemorySave();
        SaveFile.Write(world, InForce, new WorldSnapshot(), file);

        Span<byte> headerBytes = stackalloc byte[SaveHeader.Bytes];
        file.Read(headerBytes);

        Assert.Equal(world.HashState(), SaveHeader.Read(headerBytes).StateHash);
    }

    /// <summary>
    /// ⚠ <b>A corrupt save is refused by the load rather than becoming a city.</b> One flipped byte in a
    /// data column survives the allocator's consistency walk — the generations and the free list are
    /// intact and the file is the right length — so before task 10 it loaded into a world that was
    /// quietly not the one that was saved.
    /// </summary>
    /// <remarks>
    /// <b>The byte is flipped near the end of the file on purpose.</b> The first columns of the first
    /// table are <c>id</c>, <c>generation</c> and <c>free_next</c>, and corrupting those trips
    /// <c>Rows.Restore</c>'s own walk — which is a different mechanism refusing for a different reason.
    /// What is being tested is the case that walk cannot see.
    /// </remarks>
    [Fact]
    public void A_flipped_byte_in_the_body_is_refused_by_the_load()
    {
        World world = Stepped(256);

        var file = new MemorySave();
        SaveFile.Write(world, InForce, new WorldSnapshot(), file);

        byte[] bytes = file.Bytes;
        bytes[^9] ^= 0xFF;

        var corrupt = new MemorySave();
        corrupt.Write(bytes);

        string refusal = Assert.Throws<InvalidOperationException>(
            () => SaveFile.Read(corrupt, GoldenFixtures.Rules(), out _)).Message;

        _output.WriteLine(refusal);

        Assert.Contains("hashes to", refusal);
        Assert.Contains("05 §4 invariant 6", refusal);
    }

    /// <summary>
    /// A body written by a build with a different declaration is refused where it is noticed — on the
    /// length — rather than folded into a number that means nothing.
    /// </summary>
    [Fact]
    public void A_body_that_is_not_this_builds_shape_is_refused()
    {
        World world = Stepped(64);

        var body = new MemorySave();
        SaveFile.WriteBody(world, body);

        byte[] truncated = body.Bytes[..^64];

        string refusal = Assert.Throws<InvalidOperationException>(
            () => SaveHash.Of(world, truncated)).Message;

        _output.WriteLine(refusal);

        Assert.Contains("save body", refusal);
    }

    private static World Stepped(int ticks)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, GoldenFixtures.Rules(), key);
        var simulation = new Simulation(world, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return world;
    }
}
