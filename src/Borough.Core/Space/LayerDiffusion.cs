using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// The convolution: two 1-D integer passes over the Cell grid, exact and order-independent.
/// </summary>
/// <remarks>
/// <para>
/// <b>A Map Layer is a source field convolved with a bounded kernel, never an iterative
/// relaxation</b> (<c>adr/0034 §3</c>, <c>02 §2.4</c>). Two properties follow and everything else in
/// this slice rests on them. Convolution is <b>linear</b>, so twenty factories superpose exactly —
/// there is no interaction to model and no ordering to get wrong. And its support is <b>bounded</b>,
/// so a changed source can only move output within one kernel radius, which makes
/// <see cref="Run(LayerCellTable,CellResidency,SeparableKernel,Column{int},Column{int},Column{int},CellRect)"/>
/// exact rather than approximate. Under relaxation-to-steady-state neither holds, one changed source
/// perturbs the whole field, and saves diverge for reasons nobody could find.
/// </para>
/// <para>
/// <b>The source and the result are different columns, and the result is written to the write half.</b>
/// Both matter, and they are two different guarantees rather than one stated twice. Reading a
/// different column from the one being written is what makes the operator a convolution at all; writing
/// the write half is <c>adr/0037</c>'s requirement for a phase that is permitted to run in parallel.
/// An implementation that read and wrote one array would be order-dependent, which <c>02 §2.4</c> names
/// as simultaneously a determinism hazard and a visible directional smear — a bug that looks like an
/// art decision.
/// </para>
/// <para>
/// <b>There is no rounding here.</b> The passes accumulate exactly and the result is stored in kernel
/// units; <see cref="SeparableKernel.Normalise"/> owns the single stated division and explains why it
/// cannot be moved earlier without destroying superposition.
/// </para>
/// </remarks>
public static class LayerDiffusion
{
    /// <summary>Recomputes a Layer over the whole map.</summary>
    public static void Run(
        LayerCellTable cells,
        CellResidency residency,
        SeparableKernel kernel,
        Column<int> source,
        Column<int> pass,
        Column<int> value) =>
        Run(cells, residency, kernel, source, pass, value, CellRect.World);

    /// <summary>
    /// Recomputes a Layer over <paramref name="output"/> only, leaving every Cell outside it alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The caller owns the halo, and the type system cannot check that it got it right.</b> This
    /// recomputes exactly what it is asked to and nothing more; it is <see cref="LayerSources"/> that
    /// dilates a dirty region by <see cref="SeparableKernel.Radius"/> before calling. Splitting it that
    /// way is what makes the bit-identity claim testable — a full recompute is this method over
    /// <see cref="CellRect.World"/>, so the incremental test compares one code path against itself with
    /// two rectangles rather than comparing two implementations.
    /// </para>
    /// <para>
    /// <b>The intermediate band is dilated along north only, and the asymmetry is not a typo.</b> The
    /// vertical pass at a Cell reads the horizontal pass's result up to <em>r</em> Cells north and
    /// south of it, and nowhere east or west — the east reach was already spent, inside the horizontal
    /// pass, reading sources. Dilating both axes would be harmless and slower; dilating neither leaves
    /// the vertical pass reading stale intermediates at the edge of the window, which is a halo one
    /// Cell short wearing a plausible field's clothes.
    /// </para>
    /// </remarks>
    public static void Run(
        LayerCellTable cells,
        CellResidency residency,
        SeparableKernel kernel,
        Column<int> source,
        Column<int> pass,
        Column<int> value,
        CellRect output)
    {
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(residency);
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pass);
        ArgumentNullException.ThrowIfNull(value);

        CellRect target = output.Clamp();

        if (target.IsEmpty)
        {
            return;
        }

        int radius = kernel.Radius.Raw;

        CellRect band = new CellRect(
            target.East,
            target.North - kernel.Radius,
            target.Width,
            target.Height + (kernel.Radius * 2)).Clamp();

        Horizontal(residency, kernel, source, pass, band, radius);
        Vertical(residency, kernel, pass, value, target, radius);
    }

    /// <summary>Pass one: each Cell gathers its east-west neighbourhood of sources.</summary>
    private static void Horizontal(
        CellResidency residency,
        SeparableKernel kernel,
        Column<int> source,
        Column<int> pass,
        CellRect band,
        int radius)
    {
        for (int north = band.North.Raw; north < band.NorthEnd.Raw; north++)
        {
            for (int east = band.East.Raw; east < band.EastEnd.Raw; east++)
            {
                int slot = residency.Slot(new Cells(east), new Cells(north));

                if (slot == CellResidency.NotResident)
                {
                    continue;
                }

                long accumulated = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    int neighbour = residency.Slot(new Cells(east + offset), new Cells(north));

                    if (neighbour != CellResidency.NotResident)
                    {
                        accumulated += (long)kernel.Weight(offset) * source[neighbour];
                    }
                }

                pass[slot] = Narrow(accumulated, east, north);
            }
        }
    }

    /// <summary>Pass two: each Cell gathers its north-south neighbourhood of pass-one results.</summary>
    private static void Vertical(
        CellResidency residency,
        SeparableKernel kernel,
        Column<int> pass,
        Column<int> value,
        CellRect target,
        int radius)
    {
        for (int north = target.North.Raw; north < target.NorthEnd.Raw; north++)
        {
            for (int east = target.East.Raw; east < target.EastEnd.Raw; east++)
            {
                int slot = residency.Slot(new Cells(east), new Cells(north));

                if (slot == CellResidency.NotResident)
                {
                    continue;
                }

                long accumulated = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    int neighbour = residency.Slot(new Cells(east), new Cells(north + offset));

                    if (neighbour != CellResidency.NotResident)
                    {
                        accumulated += (long)kernel.Weight(offset) * pass[neighbour];
                    }
                }

                value.AtBack(slot) = Narrow(accumulated, east, north);
            }
        }
    }

    /// <summary>
    /// Narrows a pass accumulator to the <c>i32</c> a Layer Cell is, or says so loudly.
    /// </summary>
    /// <remarks>
    /// <b>Throwing rather than saturating, on <see cref="Arithmetic.IntegerMath.ShiftLeft"/>'s
    /// precedent: there is no correct answer, so the loud wrong answer beats the quiet one.</b> The
    /// bound this states is a real constraint on the world and not a defensive check — a two-pass tent
    /// of radius 8 multiplies by 6,561, so a source Cell above roughly 327,000 cannot be represented
    /// diffused. That is the same claim <c>adr/0003</c>'s extension of <c>adr/0006</c> makes from the
    /// other side, where an unbounded magnitude is the defect rather than the symptom; the end-of-run
    /// tier is where it is caught before it gets here.
    /// </remarks>
    private static int Narrow(long accumulated, int east, int north)
    {
        if (accumulated is > int.MaxValue or < int.MinValue)
        {
            throw new OverflowException(
                $"diffused Layer value {accumulated} at Cell ({east}, {north}) does not fit an i32. "
                + "adr/0003: no quantity accumulates without bound.");
        }

        return (int)accumulated;
    }
}
