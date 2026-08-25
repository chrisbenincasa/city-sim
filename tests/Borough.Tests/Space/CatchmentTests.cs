using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 6b: which Water Body each Cell's runoff reaches, <b>dry Cells included.</b>
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>This table exists because <see cref="WaterResidency"/> answers a different question.</b>
/// Residency says which body a Cell <em>is part of</em>, which is a fact about wet Cells and nothing
/// else. Every Building in the game stands on dry ground, so a Water Body's Bin addressed through
/// residency would be a Bin nothing could ever reach — permanently zero, which is <c>adr/0123</c>'s
/// own failure mode. <b>The catchment is what gives dry ground a body to name.</b>
/// </para>
/// <para>
/// <b>The claim under test is local, and that is deliberate.</b> A Cell drains where its steepest
/// descent drains; a wet Cell drains to itself. Asserted Cell by Cell over the whole map, those two
/// sentences pin the global answer by induction — <b>without this suite re-implementing the
/// memoisation the generator uses</b>, which would be a test that agrees with the code because it is
/// the code. <c>adr/0093</c>: read the mechanism, assert the property.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a share.</b> How much of a map drains anywhere falls out of the key's
/// own height field and the sea level together, exactly as <see cref="WaterTests"/> says of the
/// coastline. The figures live in <c>CatchmentCostTests</c>.
/// </para>
/// <para>
/// ⚠ <b>The obvious test — <em>a dry Cell beside water drains into it</em> — is deliberately absent,
/// because it is false and the fill is right to make it false.</b> A Cell's runoff goes down its
/// lowest spill path, and a path to the map's edge can be lower than the lake next door; a Cell ON
/// the edge is a seed of the fill that drains off the world by construction. Written and deleted
/// rather than never thought of: it failed at Cell (98,0), on the map's northern edge.
/// </para>
/// </remarks>
public sealed class CatchmentTests
{
    private const int Citizens = 1_000;
    private const int Neighbours = 4;

    private static readonly WorldKey Key = WorldKey.FromSeed(24_006);

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

    /// <summary>
    /// Re-runs the catchment pass on a generated world and hands back the field it filled.
    /// </summary>
    /// <remarks>
    /// <b>It checks its own reconstruction before the caller trusts it.</b> The three arguments are
    /// rebuilt from the world's own rows rather than taken from the generator, so the fingerprint
    /// assertion is what says the pass being re-run is the pass that ran.
    /// </remarks>
    private static int[] FilledHeights(World world, WorldKey key, out CatchmentCellTable rebuilt)
    {
        int[] height = ValueNoise.Field(key, PurposeTag.TerrainType);
        var label = new int[CellGrid.WorldCellCount];
        var handles = new Handle<WaterBody>[world.Water.Rows.SlotCount];

        for (int slot = 0; slot < world.Water.Rows.SlotCount; slot++)
        {
            handles[slot] = world.Water.Rows.At(slot);
        }

        for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
        {
            if (!world.WaterCells.Rows.IsLive(slot))
            {
                continue;
            }

            int cell = CellGrid.Index(world.WaterCells.East[slot], world.WaterCells.North[slot]);
            label[cell] = world.Water.Rows.Resolve(world.WaterCells.Body[slot]) + 1;
        }

        rebuilt = new CatchmentCellTable(world.Water);
        int[] filled = WaterGenerator.Catchments(rebuilt, height, label, handles);

        Assert.Equal(world.Catchment.Fingerprint(), rebuilt.Fingerprint());

        return filled;
    }

    /// <summary>The neighbour reached by one step, or −1 off the map. E, N, W, S.</summary>
    private static int Step(int east, int north, int step)
    {
        int nextEast = step switch { 0 => east + 1, 2 => east - 1, _ => east };
        int nextNorth = step switch { 1 => north + 1, 3 => north - 1, _ => north };

        return nextEast < 0
            || nextNorth < 0
            || nextEast >= CellGrid.WorldCells
            || nextNorth >= CellGrid.WorldCells
            ? -1
            : (nextNorth * CellGrid.WorldCells) + nextEast;
    }

    /// <summary>
    /// A wet Cell's catchment is the body it is part of. <b>The base case of the induction.</b>
    /// </summary>
    [Fact]
    public void A_wet_Cell_drains_to_the_body_it_is_part_of()
    {
        World world = Generated(Key);

        Assert.True(world.WaterInCells.Count > 0, "the coastal world laid no water at all");

        for (int slot = 0; slot < world.WaterCells.Rows.SlotCount; slot++)
        {
            if (!world.WaterCells.Rows.IsLive(slot))
            {
                continue;
            }

            Cells east = world.WaterCells.East[slot];
            Cells north = world.WaterCells.North[slot];

            Assert.Equal(world.WaterCells.Body[slot], world.Catchment.At(east, north));
        }
    }

    /// <summary>
    /// ⚠ <b>No Cell is a pit: every dry Cell has a neighbour no higher than itself that drains where
    /// it does.</b> The invariant the fill exists to establish, asserted at all 262,144 Cells.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This is the test that would have caught the first implementation, and it is written
    /// against the filled field rather than the raw one.</b> A plain steepest-descent walk satisfies
    /// nothing like it — on the raw heights ~11,000 Cells have no lower neighbour at all
    /// (<c>plans/0042</c> <b>F14</b>). Stated over the filled field it says a basin has been raised to
    /// its spill level and therefore drains out of itself.
    /// </para>
    /// <para>
    /// ⚠ <b>The map's edge is excluded and that is the claim rather than an exemption.</b> An edge
    /// Cell's runoff leaves the world — <c>CONTEXT.md</c> → Water Body's Hinterland terminus — so it
    /// is a seed of the fill rather than a Cell the fill has to satisfy.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_dry_Cell_is_a_pit()
    {
        World world = Generated(Key);
        int[] filled = FilledHeights(world, Key, out CatchmentCellTable rebuilt);

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            int east = cell % CellGrid.WorldCells;
            int north = IntegerMath.FloorDiv(cell, CellGrid.WorldCells);

            if (east == 0
                || north == 0
                || east == CellGrid.WorldCells - 1
                || north == CellGrid.WorldCells - 1)
            {
                continue;
            }

            if (world.WaterInCells.IsWet(new Cells(east), new Cells(north)))
            {
                continue;
            }

            Handle<WaterBody> found = rebuilt.At(new Cells(east), new Cells(north));
            bool downstream = false;

            for (int step = 0; step < Neighbours && !downstream; step++)
            {
                int to = Step(east, north, step);

                if (to < 0 || filled[to] > filled[cell])
                {
                    continue;
                }

                downstream = rebuilt.At(
                    new Cells(to % CellGrid.WorldCells),
                    new Cells(IntegerMath.FloorDiv(to, CellGrid.WorldCells))) == found;
            }

            Assert.True(downstream, $"Cell {cell} at filled height {filled[cell]} drains nowhere");
        }
    }

    /// <summary>
    /// The fill never lowers ground. <b>A spill level is a rim, so it is at or above the Cell.</b>
    /// </summary>
    [Fact]
    public void The_fill_never_lowers_a_Cell()
    {
        World world = Generated(Key);
        int[] height = ValueNoise.Field(Key, PurposeTag.TerrainType);
        int[] filled = FilledHeights(world, Key, out _);

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            Assert.True(filled[cell] >= height[cell], $"Cell {cell} was filled below its own ground");
        }
    }

    /// <summary>
    /// A Ruleset with no <c>[water]</c> has nothing to drain into, and every Cell says so.
    /// </summary>
    /// <remarks>
    /// <b>Absent reads as <c>default</c> rather than as a sentinel</b>, which is the same encoding a
    /// dry local minimum gets. That is not a collision worth separating: neither Cell has a body, and
    /// the caller's question is whether there is one.
    /// </remarks>
    [Fact]
    public void A_world_with_no_water_drains_nowhere()
    {
        World world = Generated(Key, "minimal.toml");

        for (int cell = 0; cell < CellGrid.WorldCellCount; cell++)
        {
            Assert.Equal(
                default,
                world.Catchment.At(
                    new Cells(cell % CellGrid.WorldCells),
                    new Cells(IntegerMath.FloorDiv(cell, CellGrid.WorldCells))));
        }
    }

    /// <summary>One key and one sea level give one catchment for ever.</summary>
    [Fact]
    public void One_key_gives_one_catchment()
    {
        Assert.Equal(
            Generated(Key).Catchment.Fingerprint(),
            Generated(Key).Catchment.Fingerprint());

        Assert.NotEqual(
            Generated(Key).Catchment.Fingerprint(),
            Generated(WorldKey.FromSeed(770_413)).Catchment.Fingerprint());
    }

    /// <summary>
    /// ⚠ <b>The catchment survives a save, and it has to be saved rather than derived to do it.</b> It
    /// is a function of the <see cref="WorldKey"/> — but a save does not carry the key back into the
    /// generator, so a Derived column here would load as a map that drains nowhere.
    /// </summary>
    [Fact]
    public void The_catchment_survives_a_save()
    {
        World world = Generated(Key);
        Ruleset rules = Load("coastal.toml");

        var file = new MemorySave();
        SaveFile.Write(world, 0x0BAD_F00D_0BAD_F00DUL, file);

        World loaded = SaveFile.Read(file, rules, out _);

        Assert.Equal(world.Catchment.Fingerprint(), loaded.Catchment.Fingerprint());
    }
}
