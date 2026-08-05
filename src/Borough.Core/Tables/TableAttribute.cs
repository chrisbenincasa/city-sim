namespace Borough.Core.Tables;

/// <summary>
/// Marks a hand-written entity table, and is the hook <c>BOR0901</c> uses to find one.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a marker, not a framework.</b> Nothing reads it at runtime — no registry, no component
/// API, no query planner. adr/0004 rejects a general table framework of our own in the same breath as
/// it rejects DOTS, and the line it defends is <em>generality nobody asked for</em>. An attribute the
/// compiler reads and the simulation never does is on the safe side of that line.
/// </para>
/// <para>
/// <b>What it buys is the one thing adr/0003's field rule cannot buy on its own.</b> Declaring a
/// column through <see cref="Rows{T}"/> is what allocates it, so a column that was never declared has
/// no storage and cannot be written to. That closes the declaration hole from the inside. The hole it
/// does not close is somebody adding a bare <c>int[]</c> beside the columns — storage that is neither
/// saved nor hashed, which is precisely the invisible-divergence defect the single declaration exists
/// to prevent. <c>BOR0901</c> closes it, and this attribute is how the analyser knows where to look.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TableAttribute : Attribute;
