using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One pond, lake, river, bay or stretch of coast. Empty for <c>Entities.Citizen</c>'s reason.
/// </summary>
public readonly struct WaterBody;

/// <summary>
/// The Water Bodies and which one each drains into — <b>the water graph, as rows and one edge.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0034</c>, <c>adr/0157</c>, milestone 24 task 6a. <see cref="WaterGenerator.LayInto"/> fills
/// it at world creation and <b>nothing writes it afterwards</b>: <c>adr/0021</c> makes water generated
/// once and immutable, so the graph and its flow directions are generator output and are never read
/// inside a Tick.
/// </para>
/// <para>
/// <b>There is no taxonomy of water types here, and that is <c>adr/0034</c> working rather than an
/// omission.</b> That ADR's claim is that a capacity and an outflow rate <em>"produce ponds, rivers
/// and seas without a taxonomy of water types"</em>. A pond is a small body draining slowly, a river
/// is a chain of bodies draining fast, a sea is the body every chain terminates in. ***Nothing in this
/// table says which is which, because nothing needs to.***
/// </para>
/// <para>
/// 🔴 <b>The capacity and the outflow rate are NOT here, and their absence is the task split rather
/// than an oversight.</b> Both are parameters of a Bin, and a Water Body's Bin is milestone 24 task
/// 6b — blocked on what family Waste is, which <c>CONTEXT.md</c> answers two ways two entries apart
/// and <c>docs/references.md</c> §10 surveys. <b>So this table is the graph without the flow</b>: it
/// says where the water is and which way it goes, and says nothing yet about how much moves.
/// ⚠ <b>Until 6b there is no level, and the <c>− w₅·shoreline</c> term therefore stays ABSENT from
/// desirability rather than present and zero</b> — <c>adr/0123</c>, whose whole subject is that a
/// working mechanism saying something false is worse than a named hole.
/// </para>
/// <para>
/// <b><c>(saved AND hashed)</c>.</b> A generated-once table still saves, because a save does not carry
/// the <c>WorldKey</c> back into the generator — <see cref="TerrainCellTable"/>'s reasoning, and
/// <c>adr/0111</c>'s.
/// </para>
/// </remarks>
[Table]
public sealed class WaterBodyTable
{
    private readonly Rows<WaterBody> _rows;

    /// <param name="capacity">Initial row count. One row per Water Body.</param>
    /// <param name="bins">The table <see cref="Bin"/> handles are resolved against.</param>
    public WaterBodyTable(int capacity, Rules.BinTable bins)
    {
        ArgumentNullException.ThrowIfNull(bins);

        _rows = new Rows<WaterBody>("water_body", capacity, Buffering.OneCopy);

        Downstream = _rows.SavedHandle("downstream", _rows);
        CellCount = _rows.Saved<int>("cell_count");
        Exits = _rows.Saved<int>("exits");
        Bin = _rows.SavedHandle("bin", bins.Rows);

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<WaterBody> Rows => _rows;

    /// <summary>
    /// The body this one drains into, or <c>default</c> — which means <b>off the map</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An unset handle is a real answer here and not a missing one.</b> <c>CONTEXT.md</c> → Water
    /// Body ends the chain <em>"in a Hinterland off the map edge"</em>, and a Hinterland is not a row
    /// in this table — it is what lies beyond the boundary. So a body that reaches the map's edge
    /// drains out of the world, and spells that as <c>default</c>. ⚠ <b>This is the one place in the
    /// build where <c>Handle.IsNone</c> carries meaning rather than reporting a hole</b>, which is why
    /// the sentence is here rather than in a comment at the one call site.
    /// </para>
    /// <para>
    /// <b>A handle into this same table, which is what makes it a graph.</b> The column folds the
    /// target row's monotonic id, so the State Hash sees which body drains into which and never a
    /// recycled slot index.
    /// </para>
    /// </remarks>
    public HandleColumn<WaterBody> Downstream { get; }

    /// <summary>How many Cells this body covers. <b>Its size, and therefore its capacity.</b></summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Water Body: <b>capacity decides whether pollution behaves as a debt or a
    /// rent</b> — a small body accumulates permanently, a large one tracks throughput and recovers —
    /// <em>"a gradient rather than two categories, which is the point."</em> Size is what produces
    /// that gradient with no taxonomy of water types, so the Bin's capacity is this count times
    /// <c>[water] capacity_per_cell</c>. <c>milestone 24 task 6b</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>Saved, though it is countable from <see cref="WaterCellTable"/>'s rows.</b> That would
    /// normally make it derived — but <c>adr/0021</c> makes water <b>immutable</b>, so this is
    /// generator output about a world that cannot change, on exactly the grounds the rest of the water
    /// graph is saved: a load does not re-run the generator, and there is nothing for a stale copy to
    /// drift against.
    /// </para>
    /// </remarks>
    public Column<int> CellCount { get; }

    /// <summary>
    /// How many places this body's water leaves through — <b>0 for an endorheic body.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The outflow rate is this count times <c>[water] outflow_per_exit_per_day</c></b>, and the
    /// three cases <c>CONTEXT.md</c> → Water Body describes fall out of it with no taxonomy:
    /// <em>a pond has no outflow and fills</em> — 0 exits; a landlocked lake spilling over its rim has
    /// **one**; and the sea touches the map's edge along many Cells, so it drains a lot in absolute
    /// terms and <em>slowly relative to itself</em>, because its capacity grows with its area while
    /// its exits grow with its perimeter.
    /// </para>
    /// <para>
    /// 🔴 <b>It is also what disambiguates <see cref="Downstream"/>, whose <c>default</c> means two
    /// different things.</b> That column's own remark says an unset handle is <em>off the map</em> —
    /// true of a body touching the boundary, and <b>equally true of an endorheic one that drains
    /// nowhere at all</b>. Measured on <c>coastal.toml</c>, endorheic bodies are **9 to 38 of 14 to
    /// 64**, so the case the sentence omits is the majority one. ***With this column the two are
    /// distinguishable: zero exits is endorheic, and non-zero with no downstream leaves the world.***
    /// </para>
    /// </remarks>
    public Column<int> Exits { get; }

    /// <summary>
    /// This body's Bin — <b>the one Utility-family Resource it holds</b>, or unset.
    /// </summary>
    /// <remarks>
    /// <c>adr/0161</c>: exactly one Resource, and its family is <c>Utility</c>, because a Water Body
    /// moves its contents along an edge of the water graph and a Good doing that would move with no
    /// Vehicle. ⚠ <b>Unset when the Ruleset's <c>[water]</c> states no Bin</b>, which is water with no
    /// level rather than a level that is permanently zero (<c>adr/0123</c>). The handle lives here
    /// rather than on the Bin for <see cref="Rules.BinOwnerKind.District"/>'s reason: the Bin's owner
    /// column is bound to the Building table.
    /// </remarks>
    public HandleColumn<Rules.Bin> Bin { get; }

    /// <summary>Opens a Water Body that drains off the map.</summary>
    /// <remarks>
    /// <b>Draining off-map is the state a body is born in</b>, because the generator finds bodies
    /// before it knows the drainage: an unset handle is what <c>default</c> already means, so a body
    /// whose downstream is never resolved is not an uninitialised row.
    /// </remarks>
    public Handle<WaterBody> Create() => _rows.Allocate();

    /// <summary>Points one body's outflow at another.</summary>
    /// <exception cref="ArgumentException"><paramref name="body"/> drains into itself.</exception>
    public void DrainsInto(Handle<WaterBody> body, Handle<WaterBody> downstream)
    {
        if (body.Equals(downstream))
        {
            throw new ArgumentException(
                "a Water Body cannot drain into itself: an outflow that returns to its own body is a "
                + "cycle of length one, and the graph is what stops the level being circular.",
                nameof(downstream));
        }

        Downstream[_rows.Resolve(body)] = downstream;
    }
}
