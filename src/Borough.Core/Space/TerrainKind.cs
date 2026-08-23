namespace Borough.Core.Space;

/// <summary>
/// What sort of ground a Cell is. <b>The one part of terrain the simulation stores.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0157</c>. Five types, and the set was named nowhere until 2026-08-23: <c>adr/0154</c> keyed
/// <b>two</b> Ruleset values off a terrain type while presuming only <em>"a small enumeration"</em>,
/// and <c>CONTEXT.md</c> and <c>adr/0022</c> name <see cref="Rock"/> and <see cref="Floodplain"/> only
/// as examples inside sentences about recovery. <b>A key was specified before the thing it keys on.</b>
/// </para>
/// <para>
/// <b>The two values keyed by this are Base Fertility and the Sealing decay rate</b>, both
/// <c>[terrain]</c> Ruleset data and neither stored per Cell — <c>adr/0022</c>'s rule that storing a
/// rate would freeze it into every save, and <c>adr/0154</c>'s that Base Fertility is not a field.
/// </para>
/// <para>
/// ⚠ <b>The ordinals are hash-bearing.</b> The column is <c>(saved AND hashed)</c>, so <b>appending a
/// sixth type is free and renumbering these five is a re-baseline</b>. Backed by <c>byte</c> because
/// the column is one per Cell and there are <see cref="CellGrid.WorldCellCount"/> of them.
/// </para>
/// <para>
/// ⚠ <b>Terrain height is not here and is not anywhere</b> (<c>adr/0156</c>): the generator computes
/// height, reads it while placing these, and keeps only the outputs. There is no height column at any
/// resolution.
/// </para>
/// </remarks>
public enum TerrainKind : byte
{
    /// <summary>Ordinary ground. <b>The default, and most of the map.</b></summary>
    /// <remarks>
    /// Base Fertility <c>1.0</c> — fully fertile, which is the scale's own top rather than a chosen
    /// number (<c>adr/0155</c>: a fraction with <c>1.0</c> meaning fully fertile).
    /// </remarks>
    Ordinary = 0,

    /// <summary>Rock. <b>Farms badly and never recovers from being built on.</b></summary>
    /// <remarks>
    /// Base Fertility <c>0.2</c>. ⚠ <b>Not zero, and the difference is argued</b>: <c>adr/0022</c>'s
    /// <em>scarcity is a gradient, never a wall</em> is a section of that document, and a zero would
    /// make this a wall. A rock Cell farms badly rather than refusing to farm.
    /// </remarks>
    Rock = 1,

    /// <summary>Alluvial floodplain. <b>As fertile as ordinary ground, and it recovers fastest.</b></summary>
    /// <remarks>
    /// Base Fertility <c>1.0</c>. <c>adr/0022</c>'s own pairing — <em>"rock and clay may never
    /// recover, alluvial floodplain may recover over hundreds of Days"</em> — and the two endpoints of
    /// the Sealing decay rate that task 4 keys off this enum.
    /// </remarks>
    Floodplain = 2,

    /// <summary>Marsh. <b>Wet ground: poor for farming, and slow rather than fast to recover.</b></summary>
    /// <remarks>Base Fertility <c>0.5</c>.</remarks>
    Marsh = 3,

    /// <summary>Thin soil. <b>Ordinary ground with little of it — the middle of the range.</b></summary>
    /// <remarks>Base Fertility <c>0.6</c>.</remarks>
    ThinSoil = 4,
}
