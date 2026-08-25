using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 7: <b>desirability's <c>w₅</c> shoreline term</b> — the last of
/// <c>02 §2.4</c>'s four, and the one that could not ship until a Water Body's Bin had an inflow.
/// </summary>
/// <remarks>
/// <para>
/// The claims: <b>clean water costs nothing and the zero is exact</b>; <b>a fouled body degrades the
/// land beside it</b>; <b>the term falls off with distance and stops at the range</b>; <b>the
/// intensity is a fill FRACTION rather than an absolute level</b>, so a teaspoon in the sea is not a
/// teaspoon in a pond; and <b>only the water's edge contributes</b>, because an area's influence on
/// land is its perimeter.
/// </para>
/// <para>
/// ⚠ <b>A world with no water passes <c>null</c> and the term is ABSENT, not zero</b> — see
/// <c>adr/0123</c>. The two look alike from outside the composition and are different facts, and
/// <see cref="A_world_with_no_water_has_no_term_at_all"/> is the only test that can tell them apart:
/// it asserts on the <em>argument</em> rather than on the answer.
/// </para>
/// </remarks>
public sealed class ShorelineTests
{
    private const int Citizens = 4_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(24_007);

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static World Generated(WorldKey key, string file = "coastal.toml")
    {
        World world = new(Citizens, Load(file), key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return world;
    }

    /// <summary>The first shore Cell of the largest body, and a Tile one Cell inland of it.</summary>
    private static (int Body, Cells East, Cells North) Largest(World world)
    {
        int best = Rows.NoSlot;
        int most = 0;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (world.Water.Rows.IsLive(slot) && world.Water.CellCount[slot] > most)
            {
                most = world.Water.CellCount[slot];
                best = slot;
            }
        }

        Assert.NotEqual(Rows.NoSlot, best);

        for (int cell = 0; cell < world.WaterCells.Rows.SlotCount; cell++)
        {
            if (!world.WaterCells.Rows.IsLive(cell)
                || !world.Water.Rows.TryResolve(world.WaterCells.Body[cell], out int at)
                || at != best)
            {
                continue;
            }

            Cells east = world.WaterCells.East[cell];
            Cells north = world.WaterCells.North[cell];

            // Inland to the west, and on the map, so the probe Tile below is real ground.
            if (east.Raw > 1 && !world.WaterInCells.IsWet(new Cells(east.Raw - 1), north))
            {
                return (best, east, north);
            }
        }

        throw new InvalidOperationException("the largest body has no shore Cell with land to its west.");
    }

    private static long Fill(World world, int body, long want)
    {
        int bin = world.Bins.Rows.Resolve(world.Water.Bin[body]);
        long space = world.Bins.SpaceAt(bin);
        long amount = want < space ? want : space;

        if (amount > 0)
        {
            world.Deposit(world.Water.Bin[body], amount, Ticks.Zero);
        }

        return amount;
    }

    /// <summary>Fouling at a Tile <paramref name="west"/> Tiles inland of a shore Cell's west edge.</summary>
    private static int FoulingInland(World world, Cells shoreEast, Cells shoreNorth, int west)
    {
        Shoreline shore = world.Shore
            ?? throw new InvalidOperationException("coastal.toml states a [water] Bin; Shore is null.");

        Tiles east = new(CellGrid.ToTiles(shoreEast).Raw - west);
        Tiles north = CellGrid.ToTiles(shoreNorth) + new Tiles(CellGrid.TilesPerCell / 2);

        return shore.Fouling(DesirabilityWeights.Default.ShorelineSource, east, north);
    }

    /// <summary>
    /// 🔴 <b>Clean water costs the land beside it nothing, and the zero is exact rather than small.</b>
    /// </summary>
    /// <remarks>
    /// This is what makes the term admissible under <c>adr/0123</c>. A term that is zero <em>on this
    /// world</em> is a fact about the world; a term that is zero on every world is the
    /// present-and-permanently-zero mechanism that ADR refused. The next test is the other half.
    /// </remarks>
    [Fact]
    public void Clean_water_costs_the_land_beside_it_nothing()
    {
        World world = Generated(Key);
        (_, Cells east, Cells north) = Largest(world);

        Assert.Equal(0, FoulingInland(world, east, north, 1));
    }

    /// <summary>🔴 <b>A fouled body degrades the land beside it.</b> The term's whole point.</summary>
    [Fact]
    public void A_fouled_body_degrades_the_land_beside_it()
    {
        World world = Generated(Key);
        (int body, Cells east, Cells north) = Largest(world);

        Assert.True(Fill(world, body, long.MaxValue) > 0, "the body took nothing");
        Assert.True(
            FoulingInland(world, east, north, 1) > 0,
            "a completely fouled body put nothing on the Tile one step inland of its own shore");
    }

    /// <summary>The land value composition actually reads it, and it subtracts.</summary>
    [Fact]
    public void The_composition_subtracts_it()
    {
        World world = Generated(Key);
        (int body, Cells east, Cells north) = Largest(world);

        Tiles at = new(CellGrid.ToTiles(east).Raw - 1);
        Tiles up = CellGrid.ToTiles(north) + new Tiles(CellGrid.TilesPerCell / 2);

        int clean = world.Layers.Desirability(
            world.Roads, DesirabilityWeights.Default, at, up, null, world.Shore);

        Fill(world, body, long.MaxValue);

        int fouled = world.Layers.Desirability(
            world.Roads, DesirabilityWeights.Default, at, up, null, world.Shore);

        Assert.True(fouled < clean, $"fouling the water moved desirability {clean} -> {fouled}");
    }

    /// <summary>It falls off with distance, and it stops at the range.</summary>
    [Fact]
    public void It_falls_off_with_distance_and_stops_at_the_range()
    {
        World world = Generated(Key);
        (int body, Cells east, Cells north) = Largest(world);

        Fill(world, body, long.MaxValue);

        int near = FoulingInland(world, east, north, 1);
        int far = FoulingInland(world, east, north, 32);

        Assert.True(near > far, $"near {near} was not above far {far}");
        Assert.True(far > 0, "32 Tiles is inside the 400 m range and read zero");

        int range = DesirabilityWeights.Default.ShorelineSource.Range.Raw;

        Assert.Equal(0, FoulingInland(world, east, north, range + CellGrid.TilesPerCell + 1));
    }

    /// <summary>
    /// 🔴 <b>The intensity is a fill FRACTION, so the same tonnage does not foul a sea and a pond
    /// alike.</b>
    /// </summary>
    /// <remarks>
    /// The claim <see cref="Shoreline"/>'s remarks make and the one place it is checked. Two bodies of
    /// very different size are given the <em>same absolute amount</em>; the smaller one must read
    /// worse. Under the reading this class rejects — intensity as the raw level — they would read the
    /// same, and <c>adr/0034</c>'s <em>"the body's level"</em> is exactly that reading if its units are
    /// not asked about.
    /// </remarks>
    [Fact]
    public void The_same_tonnage_fouls_a_small_body_worse_than_a_large_one()
    {
        World world = Generated(Key);

        int big = Rows.NoSlot, small = Rows.NoSlot;
        int most = 0, fewest = int.MaxValue;

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            if (!world.Water.Rows.IsLive(slot)) { continue; }

            int cells = world.Water.CellCount[slot];

            if (cells > most) { most = cells; big = slot; }
            if (cells > 0 && cells < fewest) { fewest = cells; small = slot; }
        }

        Assert.NotEqual(Rows.NoSlot, big);
        Assert.NotEqual(Rows.NoSlot, small);
        Assert.True(most > fewest, $"every body is the same size ({most} Cells); nothing to compare");

        // Sized to the SMALL body's capacity so both can take it, which is what makes the two
        // fill fractions differ by exactly the size ratio.
        long tonnage = (long)fewest * world.Rules.Water.CapacityPerCell;

        Assert.Equal(tonnage, Fill(world, small, tonnage));
        Assert.Equal(tonnage, Fill(world, big, tonnage));

        int bigBin = world.Bins.Rows.Resolve(world.Water.Bin[big]);
        int smallBin = world.Bins.Rows.Resolve(world.Water.Bin[small]);

        Assert.Equal(world.Bins.LevelAt(bigBin), world.Bins.LevelAt(smallBin));

        Shoreline shore = world.Shore!;
        ShorelineSource source = DesirabilityWeights.Default.ShorelineSource;

        Assert.True(
            Foul(world, shore, source, small) > Foul(world, shore, source, big),
            "the same tonnage read no worse in the smaller body — the intensity is being taken as an "
            + "absolute level rather than as a fill fraction. See Shoreline's remarks.");
    }

    /// <summary>The fouling read from the first shore Cell of one body, one Tile out.</summary>
    private static int Foul(World world, Shoreline shore, ShorelineSource source, int body)
    {
        for (int cell = 0; cell < world.WaterCells.Rows.SlotCount; cell++)
        {
            if (!world.WaterCells.Rows.IsLive(cell)
                || !world.Water.Rows.TryResolve(world.WaterCells.Body[cell], out int at)
                || at != body)
            {
                continue;
            }

            Cells east = world.WaterCells.East[cell];
            Cells north = world.WaterCells.North[cell];

            if (east.Raw > 1 && !world.WaterInCells.IsWet(new Cells(east.Raw - 1), north))
            {
                return shore.Fouling(
                    source,
                    new Tiles(CellGrid.ToTiles(east).Raw - 1),
                    CellGrid.ToTiles(north) + new Tiles(CellGrid.TilesPerCell / 2));
            }
        }

        throw new InvalidOperationException($"body {body} has no shore Cell with land to its west.");
    }

    /// <summary>
    /// 🔴 <b>A world with no water has no term at all</b>, and this asserts on the argument rather
    /// than on the answer — because absent and zero are indistinguishable from the answer.
    /// </summary>
    [Fact]
    public void A_world_with_no_water_has_no_term_at_all()
    {
        World world = Generated(Key, "minimal.toml");

        Assert.False(world.Rules.Water.Stated, "minimal.toml has grown a [water] table");
        Assert.Null(world.Shore);
    }

    /// <summary>
    /// A world with water and no Bin also has no term — for a different reason, and the same way.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Two spellings of <em>no term</em> and they are not the same world.</b> Above there is no
    /// water; here there is water with no level to read. Both drop <c>w₅</c> rather than adding zero,
    /// and neither is <c>adr/0123</c>'s failure, because in both the absence is a property of the
    /// Ruleset rather than of the build.
    /// </remarks>
    [Fact]
    public void Water_with_no_Bin_has_no_term_either()
    {
        // coastal.toml with its four Bin keys struck out, which is a world that has water and no
        // level in it -- the state every shipped file except coastal.toml is in, reached here by
        // subtraction so the two differ in nothing else.
        string text = string.Join(
            '\n',
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Rulesets", "coastal.toml"))
                .Where(line => !line.StartsWith("carries", StringComparison.Ordinal)
                    && !line.StartsWith("capacity_per_cell", StringComparison.Ordinal)
                    && !line.StartsWith("outflow_per_exit_per_day", StringComparison.Ordinal)
                    && !line.StartsWith("runoff_per_sealed_cell_per_day", StringComparison.Ordinal)));

        RulesetLoadResult result = RulesetLoader.Parse(text, "coastal-without-a-bin.toml");
        Ruleset rules = result.Ruleset
            ?? throw new InvalidOperationException(result.Describe());

        World world = new(Citizens, rules, Key);

        Assert.True(world.Rules.Water.Stated);
        Assert.False(world.Rules.Water.HasBin);
        Assert.Null(world.Shore);
    }
}
