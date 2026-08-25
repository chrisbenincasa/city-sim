using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Space;

/// <summary>
/// What the <c>w₅</c> shoreline term is actually WORTH, in the units the other two terms are in.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>An instrument. It prints figures for a document to quote and asserts almost nothing.</b>
/// <c>DesirabilityWeights.Default</c>'s shoreline intensity of <b>6.0</b> is chosen on this output.
/// Measured 2026-08-24 over three keys: the mean level over 64 shore samples is <b>2.09</b> one Tile
/// inland and <b>0.33</b> at the far end of the range, which puts the near shore alongside noise
/// beside a capacity Street (about 3) and an order below a strong plume (about 12).
/// ⚠ <b>It leaves <see cref="Transcendental.Log1P"/>'s logarithmic stretch around mid-range</b>, so
/// the outer half superposes linearly — a weaker property than the noise 4.0 has, recorded rather
/// than glossed. If this output stops saying that, <c>DesirabilityWeights.Default</c>'s remark is
/// what needs re-writing.
/// </para>
/// <para>
/// ⚠ <b>The intensity numbers are not comparable across terms and the printed LEVELS are.</b>
/// Shoreline's multiplicand is a fill fraction bounded in <c>[0, 1]</c>; noise's is an unbounded flow.
/// That is why this measures the composed magnitude rather than reasoning from the coefficients.
/// </para>
/// </remarks>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ShorelineMeasurementTests(ITestOutputHelper output)
{
    private const int Citizens = 4_000;

    [Fact]
    public void What_a_fouled_coastline_is_worth_against_pollution_and_noise()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "coastal.toml"));
        Ruleset rules = loaded.Ruleset ?? throw new InvalidOperationException(loaded.Describe());

        foreach (ulong seed in new ulong[] { 1, 24_007, 770_413 })
        {
            WorldKey key = WorldKey.FromSeed(seed);
            World world = new(Citizens, rules, key);
            SyntheticCity.PopulateInto(world, key, Ticks.Zero);

            // Every body completely fouled: the ceiling of the term, which is what a range check needs.
            for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
            {
                if (!world.Water.Rows.IsLive(slot)
                    || !world.Bins.Rows.TryResolve(world.Water.Bin[slot], out int bin))
                {
                    continue;
                }

                long space = world.Bins.SpaceAt(bin);

                if (space > 0)
                {
                    world.Deposit(world.Water.Bin[slot], space, Ticks.Zero);
                }
            }

            Shoreline shore = world.Shore!;
            ShorelineSource source = DesirabilityWeights.Default.ShorelineSource;

            int at1 = 0, at25 = 0, at50 = 0, at75 = 0, atRange = 0, samples = 0;

            for (int cell = 0; cell < world.WaterCells.Rows.SlotCount && samples < 64; cell++)
            {
                if (!world.WaterCells.Rows.IsLive(cell)) { continue; }

                Cells east = world.WaterCells.East[cell];
                Cells north = world.WaterCells.North[cell];

                if (east.Raw <= 2 || world.WaterInCells.IsWet(new Cells(east.Raw - 1), north))
                {
                    continue;
                }

                Tiles up = CellGrid.ToTiles(north) + new Tiles(CellGrid.TilesPerCell / 2);
                int edge = CellGrid.ToTiles(east).Raw;
                int range = source.Range.Raw;

                at1 += shore.Fouling(source, new Tiles(edge - 1), up);
                at25 += shore.Fouling(source, new Tiles(edge - IntegerMath.FloorDiv(range, 4)), up);
                at50 += shore.Fouling(source, new Tiles(edge - IntegerMath.FloorDiv(range, 2)), up);
                at75 += shore.Fouling(
                    source, new Tiles(edge - IntegerMath.FloorDiv(range * 3, 4)), up);
                atRange += shore.Fouling(source, new Tiles(edge - range), up);
                samples++;
            }

            Assert.True(samples > 0, $"seed {seed} produced no shore Cell with land to its west");

            output.WriteLine(
                $"seed {seed}: {samples} shore samples, mean level at 1 Tile "
                + $"{Mean(at1, samples)}, at 25% of range {Mean(at25, samples)}, at 50% "
                + $"{Mean(at50, samples)}, at 75% {Mean(at75, samples)}, at the range "
                + $"{Mean(atRange, samples)} (Q16.16; {Fixed.One} is 1.0)");
        }
    }

    private static int Mean(long total, int samples) => (int)IntegerMath.RoundDiv(total, samples);
}
