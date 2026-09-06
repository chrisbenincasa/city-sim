using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.Entities;

public sealed class StreetOverlapProbe(ITestOutputHelper output)
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset ?? throw new InvalidOperationException(result.Describe());
    }

    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void How_much_footprint_stands_in_the_street()
    {
        var world = new World(4_000, Shipped("minimal.toml"));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        BlockLattice lattice = world.Roads.Streets.Lattice;

        bool Line(int tile) => tile >= 0 && lattice.EdgeOf(lattice.LineAt(tile)) == tile;

        // The Street is 8 m -- two Tiles -- centred on the line, so the right of way is the line's
        // own Tile and the one before it.
        bool Paved(int tile) => Line(tile) || Line(tile + 1);

        int lots = 0, touching = 0, tiles = 0, wholeFront = 0, fronting = 0, crossing = 0;
        var histogram = new int[8];

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot))
            {
                continue;
            }

            int east = world.Lots.FootprintEast[slot].Raw;
            int north = world.Lots.FootprintNorth[slot].Raw;
            int wide = world.Lots.FootprintWide[slot].Raw;
            int deep = world.Lots.FootprintDeep[slot].Raw;

            if (wide <= 0 || deep <= 0)
            {
                continue;
            }

            lots++;

            int over = 0;

            for (int x = east; x < east + wide; x++)
            {
                for (int y = north; y < north + deep; y++)
                {
                    if (Paved(x) || Paved(y))
                    {
                        over++;
                    }
                }
            }

            var side = (StreetSide)world.Lots.Side[slot];
            bool horizontal = lattice.Nominal > 0
                && lattice.EdgeOf(lattice.LineAt(world.Lots.North[slot].Raw))
                    == world.Lots.North[slot].Raw;

            bool onFront = false, onCross = false;

            for (int x = east; x < east + wide; x++)
            {
                if (Paved(x))
                {
                    if (horizontal) { onCross = true; } else { onFront = true; }
                }
            }

            for (int y = north; y < north + deep; y++)
            {
                if (Paved(y))
                {
                    if (horizontal) { onFront = true; } else { onCross = true; }
                }
            }

            if (onFront) { fronting++; }
            if (onCross) { crossing++; }

            if (over > 0)
            {
                touching++;
                tiles += over;
                histogram[Math.Min(7, over)]++;
            }

            if (over >= wide * deep)
            {
                wholeFront++;
            }
        }

        output.WriteLine($"lots with a footprint      {lots}");
        output.WriteLine($"footprints on the street   {touching}  ({100.0 * touching / lots:N1}%)");
        output.WriteLine($"street Tiles built on      {tiles}  ({tiles * 16:N0} m2)");
        output.WriteLine($"entirely on the street     {wholeFront}");
        output.WriteLine($"into its OWN Street        {fronting}");
        output.WriteLine($"into a CROSS Street        {crossing}");

        for (int i = 1; i < histogram.Length; i++)
        {
            output.WriteLine($"  {i}{(i == 7 ? "+" : " ")} Tiles: {histogram[i]}");
        }
    }
}
