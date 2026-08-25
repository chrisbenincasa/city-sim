using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Quantities;

namespace Borough.Core.Space;

/// <summary>
/// Summed value noise over the Cell grid, from a <see cref="WorldKey"/> and a
/// <see cref="PurposeTag"/> alone. <b>The one field-shaping routine the generators share.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>Extracted from <see cref="TerrainGenerator"/> when Woodland became its second caller</b>
/// (<c>adr/0159</c>, milestone 24 task 8a). It was a private local of that class while it had one
/// consumer, which was correct then; two consumers wanting the identical sixty lines is where the
/// duplication stops paying. ⚠ <b>The extraction moved no State Hash</b> — the arithmetic is
/// unchanged and the purpose tag it was hard-coding is now the parameter.
/// </para>
/// <para>
/// <b>Coarse octaves loudest.</b> Octave <c>k</c> draws a lattice every <c>1 &lt;&lt; k</c> Cells and
/// contributes at amplitude <c>1 &lt;&lt; k</c>, so a field has regional shape with fine detail on it
/// rather than either alone. <b>The amplitude ladder is the octave ladder</b> — it is not a chosen
/// falloff, and that is what keeps this routine free of authored numbers
/// (<c>adr/0015</c>, <c>adr/0052</c>).
/// </para>
/// <para>
/// <b>What a caller does with the range is the caller's decision and the two shipped ones differ.</b>
/// <see cref="TerrainGenerator"/> bands against the range the key <em>realised</em>, so that every
/// terrain type exists on every world; <see cref="WoodlandGenerator"/> scales against
/// <see cref="Ceiling"/>, the range the sum <em>could</em> produce, so that how much forest a world
/// has is a property of its seed. ***Self-normalising is not a property of the noise; it is a
/// property of the reading.***
/// </para>
/// </remarks>
internal static class ValueNoise
{
    /// <summary>
    /// How many octaves a field sums. <b>Derived: the map is a power of two Cells across.</b>
    /// </summary>
    /// <remarks>
    /// Every scale from one Cell up to a quarter of the map, and no scale is preferred — a
    /// <em>feature size</em> would be a chosen number, and this is the whole ladder instead.
    /// </remarks>
    internal static int Octaves
    {
        get
        {
            int octaves = 0;

            for (int cells = CellGrid.WorldCells; cells > 2; cells >>= 1)
            {
                octaves++;
            }

            return octaves;
        }
    }

    /// <summary>
    /// The largest value <see cref="Field"/> can return. <b>Derived from the ladder, not measured.</b>
    /// </summary>
    /// <remarks>
    /// A draw is a byte, so an octave's blend peaks at <c>255 × spacing²</c> and its contribution at
    /// <c>255 × spacing</c> once the interpolation weights are undone. Summing the ladder gives
    /// <c>255 × (2^Octaves − 1)</c>. ⚠ <b>It is a ceiling and not a maximum</b>: reaching it needs
    /// every lattice corner of every octave to draw 255, which no key does. A caller scaling against
    /// it is choosing a fixed denominator on purpose.
    /// </remarks>
    internal static int Ceiling => byte.MaxValue * (IntegerMath.ShiftLeft(1, Octaves) - 1);

    /// <summary>One value per Cell, in <see cref="CellGrid.Index"/> order.</summary>
    /// <param name="key">The world's key. The only source of variation.</param>
    /// <param name="purpose">
    /// What is being decided. <b>Two fields drawn with one tag are one field wearing two names</b>,
    /// which is the correlation <see cref="PurposeTag"/> exists to prevent.
    /// </param>
    internal static int[] Field(WorldKey key, PurposeTag purpose)
    {
        var field = new int[CellGrid.WorldCellCount];

        for (int octave = 0; octave < Octaves; octave++)
        {
            // One past the last lattice point on each axis, because interpolating the final Cell of a
            // span reads the corner beyond it.
            int width = IntegerMath.ShiftRight(CellGrid.WorldCells, octave) + 2;
            var lattice = new int[width * width];

            for (int point = 0; point < lattice.Length; point++)
            {
                // The octave is in the id and not in the purpose tag. A tag names a DECISION and this
                // is one decision -- what the field is shaped like -- sampled at nine scales; a tag
                // per octave would claim nine. Distinct ids are what make the draws independent.
                ulong id = (ulong)IntegerMath.ShiftLeft((long)octave, 32) | (uint)point;

                lattice[point] = (int)(Randomness.Draw(key, id, Ticks.Zero, purpose) & 0xFF);
            }

            Accumulate(field, lattice, width, octave);
        }

        return field;
    }

    /// <summary>Adds one octave's interpolated lattice into the running field.</summary>
    private static void Accumulate(int[] field, int[] lattice, int width, int octave)
    {
        int spacing = IntegerMath.ShiftLeft(1, octave);
        int mask = spacing - 1;

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            int latticeNorth = IntegerMath.ShiftRight(north, octave);
            int alongNorth = north & mask;
            int backNorth = spacing - alongNorth;

            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                int latticeEast = IntegerMath.ShiftRight(east, octave);
                int alongEast = east & mask;
                int backEast = spacing - alongEast;

                int corner = (latticeNorth * width) + latticeEast;

                int blend =
                    (lattice[corner] * backEast * backNorth)
                    + (lattice[corner + 1] * alongEast * backNorth)
                    + (lattice[corner + width] * backEast * alongNorth)
                    + (lattice[corner + width + 1] * alongEast * alongNorth);

                // Divided by spacing squared to undo the weights, then scaled by the octave's own
                // amplitude -- which is `spacing`. The two shifts cancel to one, and the arithmetic
                // is exact because both are powers of two.
                field[(north * CellGrid.WorldCells) + east] += IntegerMath.ShiftRight(blend, octave);
            }
        }
    }
}
