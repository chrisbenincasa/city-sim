using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Disaster in progress. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct Disaster;

/// <summary>
/// What is happening to the ground right now — <b>the Disasters that have begun and not finished.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>CONTEXT.md</c> → Disaster, <c>01 §5.2</c>, <c>01 §5.3</c>. <b>World-scheduled: timing and
/// place are a function of seed and Tick over the precomputed Hazard Region, with no reference to
/// what is standing there.</b> Nothing in this table is read from the city, and nothing about the
/// city can move a row in it — which is what makes the hazard overlay a posted price rather than a
/// trap (<c>01 §5.3</c>).
/// </para>
/// <para>
/// <b>A table rather than a single row, though only one Disaster has ever been observed alive at
/// once.</b> The interval is a Ruleset duration and the rise and recession are two more, so an
/// author who sets the interval below the lifetime gets overlapping floods — a world, and not a
/// misconfiguration. Special-casing one at a time would make that world unreachable and would put
/// the arithmetic saying so in a loader.
/// </para>
/// <para>
/// ⚠ <b><see cref="SeedDepth"/> is the whole of a flood's severity and there is no severity
/// key.</b> <c>01 §5.2</c>: <em>"No severity constant is authored anywhere."</em> The surge starts
/// at the seed's own ground and rises to the flood level, so a flood seeded on deep ground reaches
/// nearly everything below it and one seeded high reaches almost nothing. ***Where the world put
/// it is how bad it is.***
/// </para>
/// </remarks>
[Table]
public sealed class DisasterTable
{
    /// <summary>The only kind that ships. <c>01 §5.2</c> tabulates four; three are unbuilt.</summary>
    public const byte Flood = 1;

    private readonly Rows<Disaster> _rows;

    /// <param name="capacity">Initial row count. One row per Disaster in progress.</param>
    public DisasterTable(int capacity)
    {
        _rows = new Rows<Disaster>("disaster", capacity, Buffering.OneCopy);

        Kind = _rows.Saved<byte>("kind");
        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");
        SeedDepth = _rows.Saved<int>("seed_depth");
        Began = _rows.Saved<Ticks>("began");
        Ruined = _rows.Saved<int>("ruined");
        Swept = _rows.Saved<int>("swept");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<Disaster> Rows => _rows;

    /// <summary>Which Disaster this is. <see cref="Flood"/> is the only one built.</summary>
    public Column<byte> Kind { get; }

    /// <summary>The east coordinate of the Cell the world seeded it on.</summary>
    public Column<Cells> East { get; }

    /// <summary>The north coordinate of the Cell the world seeded it on.</summary>
    public Column<Cells> North { get; }

    /// <summary>
    /// The seed Cell's own floodplain depth — <b>the flood's scale, and its severity.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The surge is measured against this and against nothing authored.</b> A Hazard Region row's
    /// depth is <em>the flood level minus its ground</em>, so a large depth is low ground. The surge
    /// begins with the water surface at the seed's own ground — everything connected to it and
    /// <em>lower</em> is under water at once — and rises from there to the flood level itself.
    /// </para>
    /// <para>
    /// ⚠ <b>It is also the mark that decides which verb a Building meets.</b> Ground below the
    /// seed's is swept away; ground above it is ruined and stands. Both are existing verbs
    /// (<c>01 §5.2</c>: <em>"Every effect is an existing verb"</em>), and which one fires is a
    /// comparison between two depths rather than a number anybody chose.
    /// </para>
    /// </remarks>
    public Column<int> SeedDepth { get; }

    /// <summary>The Tick the world scheduled it on.</summary>
    public Column<Ticks> Began { get; }

    /// <summary>How many Buildings this Disaster has left standing as ruins.</summary>
    /// <remarks>
    /// <b>Saved and hashed rather than a readout's tally</b>, because <c>01 §5.3</c> requires the
    /// uninteresting flood to be reported too — <em>"Riverside floodplain inundated — 0 Buildings
    /// affected"</em> is the game telling a player that a zoning decision forty Days ago was right,
    /// and a counter living outside the world could not say it after a load.
    /// </remarks>
    public Column<int> Ruined { get; }

    /// <summary>How many Buildings this Disaster has destroyed outright.</summary>
    public Column<int> Swept { get; }

    /// <summary>Records that a Disaster has begun.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seedDepth"/> is not positive.</exception>
    public Handle<Disaster> Create(byte kind, Cells east, Cells north, int seedDepth, Ticks began)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seedDepth);

        Handle<Disaster> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        Kind[slot] = kind;
        East[slot] = east;
        North[slot] = north;
        SeedDepth[slot] = seedDepth;
        Began[slot] = began;
        Ruined[slot] = 0;
        Swept[slot] = 0;

        return handle;
    }

    /// <summary>This table's contribution to a State Hash, folded alone.</summary>
    public ulong Fingerprint()
    {
        ulong hash = 0;
        _rows.FoldAll(ref hash);

        return hash;
    }
}
