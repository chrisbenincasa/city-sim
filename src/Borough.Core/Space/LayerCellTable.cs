using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// One Cell's worth of Map Layer storage. Empty for the reason <c>Entities.Citizen</c> is empty.
/// </summary>
public readonly struct LayerCell;

/// <summary>
/// The Map Layers, stored one <c>i32</c> per Cell per Layer, sparsely, and double-buffered.
/// </summary>
/// <remarks>
/// <para>
/// <b>Sparse, because an undeveloped region is a null rather than an array</b> (<c>02 §2.1</c>, and
/// <c>plans/0009</c> task 1 extending its reasoning from Chunks to Layer storage). A row exists for a
/// Cell that something has emitted into or that a plume reaches; map extent costs nothing until it is
/// built on. <b>The saving is in the save and in the reach of a Tick, not in the byte count</b> — at
/// 128² Cells a dense grid of all three Layers is under 200 KB, so anyone reading this for a memory
/// argument will not find one. It also degrades honestly: a city that covers the map <em>is</em>
/// dense, and the residency index is a flat array either way.
/// </para>
/// <para>
/// <b><see cref="Buffering.TwoCopies"/>, and it is the first table in the project to declare it</b>
/// (<c>adr/0037</c>, <c>05 §3</c>). Phase 5 is permitted parallel and both reads and writes these
/// Cells — land value moves <em>toward</em> its target rather than to it, so it reads its own previous
/// value — which is exactly the rule's antecedent. It is declared through the per-table property
/// rather than as a special case, so nothing here knows it is special.
/// </para>
/// <para>
/// <b>A row is one Cell, and a Chunk is provisionally one Cell too</b> (<c>ChunkGrid</c>). When the
/// Chunk grows to a real multiple, the sparse unit becomes the Chunk and a row holds a block; nothing
/// above this type changes, because every conversion between the two grids is a shift.
/// </para>
/// </remarks>
[Table]
public sealed class LayerCellTable
{
    private readonly Rows<LayerCell> _rows;

    /// <param name="capacity">Initial row count. Rows are added as the developed area grows.</param>
    public LayerCellTable(int capacity)
    {
        _rows = new Rows<LayerCell>("layer_cell", capacity, Buffering.TwoCopies);

        East = _rows.Saved<Cells>("east");
        North = _rows.Saved<Cells>("north");

        PollutionSource = _rows.Saved<int>("pollution_source");
        Pollution = _rows.Saved<int>("pollution");
        PollutionPass = _rows.Scratch<int>("pollution_pass");

        LandValue = _rows.Saved<int>("land_value");
        LandValueTarget = _rows.Saved<int>("land_value_target");

        Sealing = _rows.Saved<int>("sealing");

        _rows.Seal();
    }

    /// <summary>The slot allocator, the generation counters and the column list.</summary>
    public Rows<LayerCell> Rows => _rows;

    /// <summary>The Cell's east coordinate. Saved, because residency is rebuilt from it.</summary>
    public Column<Cells> East { get; }

    /// <summary>The Cell's north coordinate.</summary>
    public Column<Cells> North { get; }

    /// <summary>
    /// What industry emits into this Cell, before diffusion. The convolution's input.
    /// </summary>
    /// <remarks>
    /// <b>Stored separately from the field it produces, which is what makes the scheme a convolution
    /// rather than a relaxation</b> (<c>adr/0034 §3</c>). A relaxation has one array and iterates it
    /// toward a steady state, so the field <em>is</em> its own history and a changed source perturbs
    /// all of it. Keeping the sources means the field is a pure function of them, recomputable at any
    /// time, and identical whether it was computed once or a thousand times.
    /// </remarks>
    public Column<int> PollutionSource { get; }

    /// <summary>
    /// Industrial pollution, diffused. <b>In kernel units — divide by
    /// <see cref="SeparableKernel.Scale"/> to read it.</b>
    /// </summary>
    /// <remarks>
    /// Pre-normalised storage is what buys exact superposition; see
    /// <see cref="SeparableKernel.Normalise"/>, which is the only rounding in the scheme and states
    /// why it cannot live anywhere earlier.
    /// </remarks>
    public Column<int> Pollution { get; }

    /// <summary>
    /// The horizontal pass's intermediate, scaled by <see cref="SeparableKernel.Gain"/> once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Disposition.Scratch"/>, and its content between two diffusions is meaningless by
    /// declaration.</b> It is written and read entirely within one call to
    /// <see cref="LayerDiffusion"/>; it survives to the next only because a column is storage rather
    /// than a local. It is a column rather than a bare array so that it is declared once like
    /// everything else — <c>BOR0901</c> would reject the array, and it is right to: scratch that
    /// escapes the declaration is scratch nobody audits.
    /// </para>
    /// <para>
    /// <b>It was <see cref="Disposition.Derived"/> until milestone 8 task 1, and that was the wrong
    /// declaration rather than a loose one.</b> <see cref="Disposition.Derived"/> claims the field is
    /// a pure function of saved state; nothing rebuilds this one and nothing should, because there is
    /// nothing to recover. The rebuild audit that made the difference matter is what created the
    /// third value — this column is the reason it exists, and the obligation it now carries (no read
    /// outlives the phase that wrote it) is asserted rather than assumed.
    /// </para>
    /// </remarks>
    public Column<int> PollutionPass { get; }

    /// <summary>
    /// Land value. <b>The one composite that is stored, and it is stored because it has momentum.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>02 §2.4</c>'s composition rule is <em>compose at the point of use, do not bake composites
    /// into stored layers</em>, and land value is the stated exception. It moves slowly toward the
    /// current desirability rather than tracking it, which is both realistic and a stabiliser against
    /// oscillation — and a quantity with memory cannot be recomputed from the present, so it has to be
    /// state. That is <c>02 §2.5</c> question 5 answering <em>yes</em>.
    /// </para>
    /// <para>
    /// <b>It is not diffused, and that is the second thing that distinguishes it from pollution.</b>
    /// There is no kernel and no source field: it is an ordinary stored number that an operator moves.
    /// The double buffer earns its place here rather than there — this is the column that genuinely
    /// reads its own previous value, which is <c>adr/0037</c>'s antecedent.
    /// </para>
    /// </remarks>
    public Column<int> LandValue { get; }

    /// <summary>
    /// What land value is moving toward. <b>Nothing writes it, and that is the named hole.</b>
    /// </summary>
    /// <remarks>
    /// The target is desirability, which composes noise and amenity — both queries on a Road Graph
    /// that does not exist in Phase 1. See <see cref="MapLayers.Desirability"/>, which refuses rather
    /// than returning zero. The column is declared now because <em>where the number lands</em> is this
    /// slice's decision and <em>what the number is</em> is not; declaring it later would mean a schema
    /// change applied to a table that already has rows.
    /// </remarks>
    public Column<int> LandValueTarget { get; }

    /// <summary>
    /// The count of Tiles in this Cell ever built on. <b>Stored per Cell and not diffused.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A count, not a field</b> (<c>02 §2.4</c>, <c>CONTEXT.md</c> → Sealing). It has no kernel and
    /// no cadence — it changes on build. One house seals 1/1024 of its Cell, which is
    /// <see cref="CellGrid.TilesInCell"/> and is why <c>adr/0034</c> says Sealing's denominator became
    /// a property of the Cell.
    /// </para>
    /// <para>
    /// <b>Bounded by construction, which is how it satisfies <c>adr/0006</c> without an invariant
    /// doing the work.</b> A Cell cannot have more Tiles built on it than it has Tiles, so
    /// <see cref="MapLayers.Seal"/> clamps rather than trusting its caller. The end-of-run tier checks
    /// it anyway — a bound maintained at one write site is a bound that stops holding when somebody
    /// adds a second.
    /// </para>
    /// </remarks>
    public Column<int> Sealing { get; }

    /// <summary>Allocates the row for a Cell. The caller owns residency; see <see cref="CellResidency"/>.</summary>
    public Handle<LayerCell> Create(Cells east, Cells north)
    {
        Handle<LayerCell> handle = _rows.Allocate();
        int slot = _rows.Resolve(handle);

        East[slot] = east;
        North[slot] = north;

        return handle;
    }
}
