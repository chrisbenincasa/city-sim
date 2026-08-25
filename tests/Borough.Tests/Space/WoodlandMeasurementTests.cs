using Borough.Core.Determinism;
using Borough.Core.Space;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What <see cref="WoodlandGenerator"/> actually produces, per Cell and per world.
/// </summary>
/// <remarks>
/// <b>Written because task 8a's assertion tier turned up a range nobody had predicted.</b> Scaling
/// against <see cref="ValueNoise.Ceiling"/> is derived and authors no number, but what it derives had
/// never been looked at: a sum of uniforms concentrates, so the realised per-Cell range is a band in
/// the middle of the Cell rather than the whole of it. <c>adr/0043</c> — the shape of the output is a
/// claim a measurement settles, so this is the measurement.
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class WoodlandMeasurementTests(ITestOutputHelper output)
{
    private static int[] Forest(WorldKey key)
    {
        WoodlandCellTable woodland = new();
        WoodlandGenerator.LayInto(woodland, key);

        var map = new int[CellGrid.WorldCellCount];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                map[CellGrid.Index(x, y)] = woodland.At(x, y);
            }
        }

        return map;
    }

    [Fact]
    public void WhatTheGeneratorPlants()
    {
        output.WriteLine($"Ceiling {ValueNoise.Ceiling}, octaves {ValueNoise.Octaves}, "
            + $"{CellGrid.WorldCellCount} Cells of {CellGrid.TilesInCell} Tiles");
        output.WriteLine("");
        output.WriteLine("key        min   max   mean   cover%   bare   full");

        foreach (uint seed in new uint[] { 0x0001U, 0x0002U, 0xBEEFU, 0xF0E5U, 0x5EA1U })
        {
            int[] forest = Forest(WorldKey.FromSeed(seed));

            int low = int.MaxValue;
            int high = int.MinValue;
            long total = 0;
            int bare = 0;
            int full = 0;

            foreach (int tiles in forest)
            {
                low = Math.Min(low, tiles);
                high = Math.Max(high, tiles);
                total += tiles;

                if (tiles == 0) { bare++; }
                if (tiles == CellGrid.TilesInCell) { full++; }
            }

            long mean = total / forest.Length;
            long cover = total * 100 / ((long)forest.Length * CellGrid.TilesInCell);

            output.WriteLine(
                $"0x{seed:X4}   {low,4}  {high,4}   {mean,4}   {cover,5}%   {bare,4}   {full,4}");
        }
    }
}
