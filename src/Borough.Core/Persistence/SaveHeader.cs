namespace Borough.Core.Persistence;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

/// <summary>
/// The fixed-size block in front of a save. Everything a loader must agree with the writer about
/// <em>before</em> it may read a single column.
/// </summary>
/// <remarks>
/// <para>
/// <b>The body has no schema and this does</b> (<c>adr/0086</c>). The save proper is the per-field
/// declaration read out in the order <c>World.HashState</c> already folds it, which means it is only
/// interpretable by a build whose declaration matches — and a mismatch there is silent, because one
/// column's bytes are as good as another's. So the header's whole job is to turn every silent mismatch
/// into a refusal, and its contents are chosen by one test: <em>what could differ between the writing
/// build and the reading build such that the body would parse and mean something else?</em>
/// </para>
/// <para>
/// <b>Three answers, and the third is a class rather than a field.</b> The declaration set, which is
/// <see cref="FormatVersion"/>. The Rules the numbers refer to, which is <see cref="RulesetInForce"/>.
/// And the <b>world-creation constants that live in the binary rather than in a table</b> — a saved
/// column carries its own value and cannot disagree, but a <c>const</c> is supplied by the reader, so a
/// save written under one and read under another is a body that parses perfectly and means something
/// else. Each of the four below says <em>baked into the save</em> in its own file, and none of them had
/// anywhere to be baked into until this type existed.
/// </para>
/// <para>
/// ⚠ <b>There is no generator version and no world seed, against <c>adr/0086</c>'s table of three.</b>
/// Both are <c>adr/0111</c>, and they are one requirement rather than two: a seed is needed only by
/// something that regenerates from it, and nothing does. Writing a placeholder generator version would
/// be worse than omitting it — <c>adr/0021</c> <em>pins</em> that number, so a build that grows a
/// generator must refuse every save written before it, and a save carrying <c>generator_version = 1</c>
/// would be <em>accepted</em> by that build and land its city on a landscape that was not there when it
/// was saved. <b>An absent version refuses; a placeholder agrees.</b>
/// </para>
/// <para>
/// <b>The fixed fields are little-endian and one sentinel is not.</b> The body is written through
/// <c>MemoryMarshal.AsBytes</c>, so a multi-byte field inside a row sits in the <em>machine's</em> order
/// and a save is not portable across byte orders — <c>plans/0012</c> item 5, which found the same thing
/// under the State Hash. This type cannot fix that and can refuse it: the header parses on any host
/// because its own fields are written explicitly, and the sentinel is written in native order so that a
/// reader getting it back changed knows the body will be garbage rather than discovering it a hundred
/// megabytes later.
/// </para>
/// </remarks>
public readonly struct SaveHeader : IEquatable<SaveHeader>
{
    /// <summary>The header's width. Fixed for every format version this build can read.</summary>
    public const int Bytes = 60;

    /// <summary>
    /// The format version this build writes. Versions the <b>declaration set</b> — which tables exist,
    /// which columns, and which disposition each was declared with.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is not <c>World.HashSeed</c>'s version byte and the two must not be conflated.</b> That
    /// byte signs a deliberate re-baseline of the <em>hash</em> — the same city hashing differently —
    /// so a reviewer can tell a signed change from a regression. This versions the <em>file</em>, and it
    /// moves when the set of columns in it moves, which is a case that byte explicitly does not cover:
    /// its own remark records that appending a table does <b>not</b> bump it. A save written before a
    /// new table exists is short by a table and needs a migration; the hash it was written at was never
    /// wrong.
    /// </remarks>
    public const int Current = 1;

    private const ulong ByteOrderSentinel = 0x0102_0304_0506_0708UL;

    private static ReadOnlySpan<byte> Magic => "borosave"u8;

    private SaveHeader(
        int formatVersion,
        WorldKey key,
        ulong rulesetInForce,
        int ticksPerDay,
        int wheelSize,
        int worldCells,
        int tilesPerCell,
        ulong stateHash)
    {
        FormatVersion = formatVersion;
        Key = key;
        RulesetInForce = rulesetInForce;
        TicksPerDay = ticksPerDay;
        WheelSize = wheelSize;
        WorldCells = worldCells;
        TilesPerCell = tilesPerCell;
        StateHash = stateHash;
    }

    /// <summary>The format version the file was written under.</summary>
    public int FormatVersion { get; }

    /// <summary>
    /// The world key — <c>Randomness.Draw</c>'s first coordinate, and the one piece of world state that
    /// is not a column.
    /// </summary>
    /// <remarks>
    /// <b>It is here because <c>World.RebuildDerived</c> cannot run without it.</b> The commute roster
    /// is a pure function of the Ruleset in force and of each Citizen's id hashed against this key, and
    /// that method <em>"takes no arguments and must not start taking them"</em>. So a save that omits
    /// the key restores every column correctly and then cannot rebuild a derived structure — the
    /// milestone's named risk arriving through the header rather than through a column.
    /// </remarks>
    public WorldKey Key { get; }

    /// <summary>The content hash of the Ruleset in force when the save was taken.</summary>
    /// <remarks>
    /// <b>This is <c>Simulation._inForce</c>, and writing it here is what dissolves that field.</b>
    /// Saving it as world state as well would be a second copy of a header entry, which is the failure
    /// <c>plans/0012</c> <em>Cause 1</em> names. <c>05 §7</c>'s two cross-Ruleset load policies —
    /// lenient in play, refused on an unaccounted mismatch in replay — are already written and are what
    /// a mismatch here means; neither is decided by this type.
    /// </remarks>
    public ulong RulesetInForce { get; }

    /// <summary><c>TICKS_PER_DAY</c> as the writing build had it.</summary>
    public int TicksPerDay { get; }

    /// <summary><c>WHEEL_SIZE</c> as the writing build had it.</summary>
    public int WheelSize { get; }

    /// <summary><c>CellGrid.WorldCells</c> as the writing build had it.</summary>
    public int WorldCells { get; }

    /// <summary><c>CellGrid.TilesPerCell</c> as the writing build had it.</summary>
    public int TilesPerCell { get; }

    /// <summary>
    /// <c>World.HashState</c> at the instant the body below was copied — the ninth field, and the one
    /// that makes a load check itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is a verification and not a schema check, which is why it is last</b> (<c>adr/0112</c>).
    /// Every other field says <em>this body will parse and mean what it says</em>; this one says
    /// <em>and here is what it meant</em>. So a load reads the columns, runs <c>RebuildDerived</c>, and
    /// recomputes — which makes <c>05 §4</c> invariant 6 a property of every load rather than of the
    /// seven cases in the test suite.
    /// </para>
    /// <para>
    /// <b>⚠ It costs the simulation thread nothing, and that is the reason it exists at all.</b>
    /// <c>adr/0087</c> forbids computing it there, and milestone 8 shipped without a hash because the
    /// only source anybody had found was the live world. <see cref="SaveHash"/> takes it from the copy
    /// instead. ***The clause was never the obstacle; the missing mechanism was.***
    /// </para>
    /// <para>
    /// <b>⚠ Zero is not a sentinel and there is no unverified save.</b> A hash of zero is a legitimate
    /// value that a world could genuinely have, so a reader cannot use it to mean <em>this file
    /// carries no hash</em> — which is why the field was added while the format was unreleased rather
    /// than being made optional later. Every version-1 save carries one.
    /// </para>
    /// </remarks>
    public ulong StateHash { get; }

    /// <summary>The header this build would write for a world.</summary>
    /// <param name="world">The world being saved. Supplies the key and nothing else.</param>
    /// <param name="rulesetInForce">
    /// The content hash of the Ruleset in force — <c>Simulation.RulesetInForce</c>.
    /// </param>
    /// <param name="stateHash">
    /// <c>World.HashState</c> at the instant the body was copied. <see cref="SaveHash.Of"/> is where a
    /// save takes it from, so that the simulation thread pays nothing for it.
    /// </param>
    public static SaveHeader Of(World world, ulong rulesetInForce, ulong stateHash)
    {
        ArgumentNullException.ThrowIfNull(world);

        return new SaveHeader(
            Current,
            world.Key,
            rulesetInForce,
            Ticks.PerDay,
            EventWheel.Size,
            CellGrid.WorldCells,
            CellGrid.TilesPerCell,
            stateHash);
    }

    /// <summary>Writes the header. The destination must be at least <see cref="Bytes"/> wide.</summary>
    public void Write(Span<byte> destination)
    {
        if (destination.Length < Bytes)
        {
            throw new ArgumentException(
                $"a save header is {Bytes} bytes and this destination is {destination.Length}.",
                nameof(destination));
        }

        ulong sentinel = ByteOrderSentinel;

        Magic.CopyTo(destination);
        BinaryPrimitives.WriteInt32LittleEndian(destination[8..], FormatVersion);

        // Native order, deliberately, and the only field here that is. See the remark on the type.
        MemoryMarshal.Write(destination[12..], in sentinel);

        BinaryPrimitives.WriteUInt64LittleEndian(destination[20..], Key.Raw);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[28..], RulesetInForce);
        BinaryPrimitives.WriteInt32LittleEndian(destination[36..], TicksPerDay);
        BinaryPrimitives.WriteInt32LittleEndian(destination[40..], WheelSize);
        BinaryPrimitives.WriteInt32LittleEndian(destination[44..], WorldCells);
        BinaryPrimitives.WriteInt32LittleEndian(destination[48..], TilesPerCell);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[52..], StateHash);
    }

    /// <summary>Reads a header, refusing anything this build must not go on to read the body of.</summary>
    /// <remarks>
    /// <b>The order of the checks is the point.</b> Each is a precondition of the next being meaningful:
    /// the magic says this is a save at all, the format version says the rest of the header is laid out
    /// the way this build expects, the sentinel says multi-byte fields will survive, and the constants
    /// say the columns will mean what they say. A reader that checked them in any other order would
    /// report the wrong cause for the right refusal.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The file is one this build must not read.</exception>
    public static SaveHeader Read(ReadOnlySpan<byte> source)
    {
        if (source.Length < Bytes)
        {
            throw new InvalidOperationException(
                $"a save header is {Bytes} bytes and this file is {source.Length}.");
        }

        if (!source[..8].SequenceEqual(Magic))
        {
            throw new InvalidOperationException("this is not a borough save.");
        }

        int formatVersion = BinaryPrimitives.ReadInt32LittleEndian(source[8..]);

        if (formatVersion != Current)
        {
            throw new InvalidOperationException(
                $"this save is format version {formatVersion} and this build writes version {Current}. "
                + "A save is the field declaration read out (adr/0086), so a version it does not "
                + "recognise names a set of columns it cannot lay out, and there is no migration chain "
                + "yet because there has never been a second version to migrate from.");
        }

        if (MemoryMarshal.Read<ulong>(source[12..]) != ByteOrderSentinel)
        {
            throw new InvalidOperationException(
                "this save was written on a host with a different byte order. The header parsed and the "
                + "body would not: a column is written through MemoryMarshal.AsBytes, so a multi-byte "
                + "field sits in the machine's order (plans/0012 item 5).");
        }

        var header = new SaveHeader(
            formatVersion,
            WorldKey.Restore(BinaryPrimitives.ReadUInt64LittleEndian(source[20..])),
            BinaryPrimitives.ReadUInt64LittleEndian(source[28..]),
            BinaryPrimitives.ReadInt32LittleEndian(source[36..]),
            BinaryPrimitives.ReadInt32LittleEndian(source[40..]),
            BinaryPrimitives.ReadInt32LittleEndian(source[44..]),
            BinaryPrimitives.ReadInt32LittleEndian(source[48..]),
            BinaryPrimitives.ReadUInt64LittleEndian(source[52..]));

        Agree("TICKS_PER_DAY", header.TicksPerDay, Ticks.PerDay);
        Agree("WHEEL_SIZE", header.WheelSize, EventWheel.Size);
        Agree("CellGrid.WorldCells", header.WorldCells, CellGrid.WorldCells);
        Agree("CellGrid.TilesPerCell", header.TilesPerCell, CellGrid.TilesPerCell);

        return header;
    }

    /// <inheritdoc/>
    public bool Equals(SaveHeader other) =>
        FormatVersion == other.FormatVersion
        && Key == other.Key
        && RulesetInForce == other.RulesetInForce
        && TicksPerDay == other.TicksPerDay
        && WheelSize == other.WheelSize
        && WorldCells == other.WorldCells
        && TilesPerCell == other.TilesPerCell
        && StateHash == other.StateHash;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SaveHeader other && Equals(other);

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Written out rather than composed, because <c>System.HashCode</c> is seeded from process
    /// entropy</b> and <c>BOR0206</c> is a build error on it. Nothing in the simulation looks a header
    /// up, so this exists to satisfy the equality contract rather than to be fast.
    /// </remarks>
    public override int GetHashCode()
    {
        ulong hash = Randomness.Mix((ulong)(uint)FormatVersion);

        hash = Randomness.Mix(hash ^ Key.Raw);
        hash = Randomness.Mix(hash ^ RulesetInForce);
        hash = Randomness.Mix(hash ^ (ulong)(uint)TicksPerDay);
        hash = Randomness.Mix(hash ^ (ulong)(uint)WheelSize);
        hash = Randomness.Mix(hash ^ (ulong)(uint)WorldCells);
        hash = Randomness.Mix(hash ^ (ulong)(uint)TilesPerCell);
        hash = Randomness.Mix(hash ^ StateHash);

        return (int)hash;
    }

    public static bool operator ==(SaveHeader left, SaveHeader right) => left.Equals(right);

    public static bool operator !=(SaveHeader left, SaveHeader right) => !left.Equals(right);

    private static void Agree(string constant, int saved, int here)
    {
        if (saved == here)
        {
            return;
        }

        throw new InvalidOperationException(
            $"this save was written with {constant} = {saved} and this build has {here}. It is a "
            + "world-creation constant, so every column that counts in it means something else here. "
            + "There is no migration: the file is correct and this binary is the wrong one to read it.");
    }
}
