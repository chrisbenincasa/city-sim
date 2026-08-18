using System.Buffers.Binary;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 4 — the header, and what it refuses.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every test here is a refusal except the first two.</b> That is the type's job: the body of a save
/// is the field declaration read out with no self-description in it (<c>adr/0086</c>), so a build whose
/// declaration differs reads the same bytes and gets a different city, with nothing anywhere raising a
/// hand. The header exists to make each of those differences loud, and a refusal that is not tested is a
/// refusal nobody has seen fire.
/// </para>
/// <para>
/// ⚠ <b>The world-creation constants cannot be tested by changing them</b> — they are <c>const</c>, so
/// the disagreeing build does not exist at test time. The tests below forge a header instead, which
/// tests the check and not the constant, and that difference is worth stating: what is asserted is that
/// a save written under a different <c>TICKS_PER_DAY</c> is refused, not that <c>TICKS_PER_DAY</c> is
/// 2048.
/// </para>
/// </remarks>
public sealed class SaveHeaderTests
{
    private const ulong Ruleset = 0xFEED_FACE_CAFE_BABEUL;

    [Fact]
    public void A_header_round_trips()
    {
        var world = new World(1000, Core.Rules.Ruleset.Empty, WorldKey.FromSeed(0x8000_0001UL));
        SaveHeader written = SaveHeader.Of(world, Ruleset);

        Span<byte> bytes = stackalloc byte[SaveHeader.Bytes];
        written.Write(bytes);

        Assert.Equal(written, SaveHeader.Read(bytes));
    }

    /// <summary>
    /// The header carries the world key, and it is the reason the header exists at all: nothing else in
    /// a save carries it, and <c>World.RebuildDerived</c> cannot run without it.
    /// </summary>
    [Fact]
    public void The_world_key_survives_and_it_is_not_a_column()
    {
        var key = WorldKey.FromSeed(0xB0A0_0C6E_A7E0_0001UL);
        var world = new World(1000, Core.Rules.Ruleset.Empty, key);

        Span<byte> bytes = stackalloc byte[SaveHeader.Bytes];
        SaveHeader.Of(world, Ruleset).Write(bytes);

        SaveHeader read = SaveHeader.Read(bytes);

        Assert.Equal(key, read.Key);
        Assert.Equal(Ruleset, read.RulesetInForce);
        Assert.Equal(SaveHeader.Current, read.FormatVersion);
    }

    /// <summary>
    /// The four world-creation constants are written as this build has them, individually rather than
    /// folded together — <c>adr/0086</c>'s <em>do not compact</em>, so that a mismatch names which one.
    /// </summary>
    [Fact]
    public void The_world_creation_constants_are_written_individually()
    {
        SaveHeader header = SaveHeader.Read(HeaderOfThisBuild());

        Assert.Equal(Ticks.PerDay, header.TicksPerDay);
        Assert.Equal(EventWheel.Size, header.WheelSize);
        Assert.Equal(CellGrid.WorldCells, header.WorldCells);
        Assert.Equal(CellGrid.TilesPerCell, header.TilesPerCell);
    }

    [Fact]
    public void A_file_that_is_not_a_save_is_refused()
    {
        byte[] bytes = HeaderOfThisBuild();
        bytes[0] = (byte)'B';

        Assert.Contains("not a borough save", Refusal(bytes));
    }

    [Fact]
    public void A_file_too_short_to_hold_a_header_is_refused()
    {
        Assert.Contains("52 bytes and this file is 51", Refusal(HeaderOfThisBuild().AsSpan(0, 51)));
    }

    /// <summary>
    /// A future format version is refused rather than read optimistically — there is no migration chain
    /// because there has never been a second version, and reading version 2 as version 1 is exactly the
    /// silent misinterpretation the header exists to stop.
    /// </summary>
    [Fact]
    public void A_format_version_this_build_does_not_write_is_refused()
    {
        byte[] bytes = HeaderOfThisBuild();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), SaveHeader.Current + 1);

        string refusal = Refusal(bytes);

        Assert.Contains($"format version {SaveHeader.Current + 1}", refusal);
        Assert.Contains("adr/0086", refusal);
    }

    /// <summary>
    /// The byte-order sentinel. It is the one field written in the machine's order, and reversing it is
    /// what a big-endian writer's file would look like from here.
    /// </summary>
    [Fact]
    public void A_save_from_a_host_with_a_different_byte_order_is_refused()
    {
        byte[] bytes = HeaderOfThisBuild();
        bytes.AsSpan(12, 8).Reverse();

        Assert.Contains("different byte order", Refusal(bytes));
    }

    /// <summary>
    /// A world-creation constant that moved. The refusal names the constant, because <em>this save is
    /// unreadable</em> and <em>this save was written by a build with a different Day</em> send a reader
    /// to different places.
    /// </summary>
    [Theory]
    [InlineData(36, "TICKS_PER_DAY")]
    [InlineData(40, "WHEEL_SIZE")]
    [InlineData(44, "CellGrid.WorldCells")]
    [InlineData(48, "CellGrid.TilesPerCell")]
    public void A_world_creation_constant_that_moved_is_refused_by_name(int offset, string named)
    {
        byte[] bytes = HeaderOfThisBuild();
        int here = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset));
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), here / 2);

        string refusal = Refusal(bytes);

        Assert.Contains(named, refusal);
        Assert.Contains("There is no migration", refusal);
    }

    /// <summary>
    /// ⚠ <b>The negative assertion, written so the absence cannot rot into an oversight.</b> There is no
    /// generator version and no world seed in a version-1 header (<c>adr/0111</c>), and the header is
    /// exactly wide enough for what is in it — so a field added later cannot be added in silence.
    /// </summary>
    [Fact]
    public void The_header_is_exactly_as_wide_as_its_fields()
    {
        Assert.Equal(52, SaveHeader.Bytes);

        // magic 8, format version 4, sentinel 8, key 8, Ruleset 8, four constants at 4 each.
        Assert.Equal(SaveHeader.Bytes, 8 + 4 + 8 + 8 + 8 + (4 * 4));
    }

    /// <summary>
    /// A destination too narrow is a caller's error rather than a corrupt file, and it is the one
    /// refusal here that is not an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void A_destination_too_narrow_to_write_into_is_refused()
    {
        var world = new World(1000, Core.Rules.Ruleset.Empty, WorldKey.FromSeed(1));
        SaveHeader header = SaveHeader.Of(world, Ruleset);
        byte[] narrow = new byte[SaveHeader.Bytes - 1];

        Assert.Throws<ArgumentException>(() => header.Write(narrow));
    }

    private static byte[] HeaderOfThisBuild()
    {
        var world = new World(1000, Core.Rules.Ruleset.Empty, WorldKey.FromSeed(0x8000_0001UL));
        byte[] bytes = new byte[SaveHeader.Bytes];

        SaveHeader.Of(world, Ruleset).Write(bytes);

        return bytes;
    }

    private static string Refusal(ReadOnlySpan<byte> bytes)
    {
        byte[] copy = bytes.ToArray();

        return Assert.Throws<InvalidOperationException>(() => SaveHeader.Read(copy)).Message;
    }
}
