using Borough.Core.Space;

namespace Borough.Tests.Space;

/// <summary>
/// <c>plans/0053</c> step 4 — the daylight bound, and the courtyard it cuts.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>THE CONSTANT SHIPPED IN STEP 3 WITH NO TEST AT ALL, which is what this file is really
/// about.</b> <see cref="BuildingPlan"/> is the only thing between a parcel's area and how many
/// people live on it, and nothing held it to anything — so the wing thickness could have moved by a
/// Tile in either direction and every assertion in the suite would still have passed, because they
/// all read occupancy against whatever the plan happened to say. ***A derivation with two readers
/// and no test is a convention.***
/// </para>
/// <para>
/// ⚠ <b>What is asserted here is the SHAPE and never a capacity.</b> A test naming a Building's
/// occupancy would be reading the rate as well, and the rate is
/// <c>[capacity] floor_tiles_per_occupant</c> — Ruleset data, and free to move. These hold the two
/// things that are not: that a wing is <see cref="BuildingPlan.DaylightTiles"/> thick, and that the
/// hole and the floor are the same partition of the same rectangle.
/// </para>
/// </remarks>
public sealed class BuildingPlanTests
{
    /// <summary>
    /// A footprint under the bound is solid, and every Tile of it is floor.
    /// </summary>
    /// <remarks>
    /// <b>The shipped lattice lives entirely in here</b>, which is the point of the case: at
    /// <c>block_tiles = 32</c> the footprints run to about 16 × 20, so the ordinary city is solid
    /// buildings and a courtyard is the exception a large parcel earns.
    /// </remarks>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(6, 16)]
    [InlineData(11, 11)]
    [InlineData(16, 11)]
    [InlineData(11, 40)]
    public void A_footprint_under_the_bound_is_solid(int wide, int deep)
    {
        Assert.False(
            BuildingPlan.Hollow(wide, deep, out int holeWide, out int holeDeep),
            $"{wide}x{deep} was hollowed. A hole under {BuildingPlan.DaylightTiles} Tiles across is "
            + "a light well and not a courtyard, and the plan is solid over it.");

        Assert.Equal(0, holeWide);
        Assert.Equal(0, holeDeep);
        Assert.Equal(wide * deep, BuildingPlan.HabitableTiles(wide, deep));
    }

    /// <summary>
    /// <b>Twelve Tiles on both axes is the first footprint that hollows</b>, and it is derived.
    /// </summary>
    /// <remarks>
    /// <b>Two wings and a gap the same thickness</b> — <c>4 + 4 + 4</c> — so the threshold is
    /// <see cref="BuildingPlan.DaylightTiles"/> three times over and no second constant exists to
    /// tune. ***The assertion is written in terms of the constant rather than of 12***, so moving
    /// the daylight depth moves this test with it instead of breaking it.
    /// </remarks>
    [Fact]
    public void The_first_hollow_footprint_is_three_wings_across()
    {
        int first = 3 * BuildingPlan.DaylightTiles;

        Assert.False(BuildingPlan.Hollow(first - 1, first, out _, out _));
        Assert.False(BuildingPlan.Hollow(first, first - 1, out _, out _));

        Assert.True(
            BuildingPlan.Hollow(first, first, out int holeWide, out int holeDeep),
            $"{first}x{first} is two wings and a gap of the same thickness, which is the smallest "
            + "ring this bound can cut.");

        Assert.Equal(BuildingPlan.DaylightTiles, holeWide);
        Assert.Equal(BuildingPlan.DaylightTiles, holeDeep);
    }

    /// <summary>
    /// <b>A wing is <see cref="BuildingPlan.DaylightTiles"/> thick on every side, at every size.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>This is the assertion the prose was wrong about.</b> The remark on the type read
    /// <em>"no point may be further than DaylightTiles from an outside wall"</em>, which describes a
    /// wing of <b>5</b>; the code has always cut <b>4</b>, which is <em>16 m across, lit from both
    /// faces</em>. ***Both readings are coherent and they are a factor of two apart***, so the one
    /// the build means is written down here as well as in a sentence.
    /// </remarks>
    [Theory]
    [InlineData(12, 12)]
    [InlineData(16, 16)]
    [InlineData(48, 48)]
    [InlineData(13, 97)]
    public void A_wing_is_one_daylight_depth_thick(int wide, int deep)
    {
        Assert.True(BuildingPlan.Hollow(wide, deep, out int holeWide, out int holeDeep));

        Assert.Equal(wide - (2 * BuildingPlan.DaylightTiles), holeWide);
        Assert.Equal(deep - (2 * BuildingPlan.DaylightTiles), holeDeep);
    }

    /// <summary>
    /// <b>The floor and the hole partition the footprint exactly</b>, which is what lets the shell
    /// draw what the capacity counted.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is the invariant and not an example.</b> <c>Borough.Godot</c> draws four wings from
    /// <see cref="BuildingPlan.Hollow"/> and <c>LotTable.FloorTiles</c> counts from
    /// <see cref="BuildingPlan.HabitableTiles"/>; if those two ever stopped summing to the rectangle
    /// the picture and the city would disagree about the same Building again, which is the defect
    /// <c>plans/0052</c> stage 1 and <c>plans/0053</c> step 3 each spent a session closing.
    /// </remarks>
    [Fact]
    public void The_floor_and_the_hole_partition_the_footprint()
    {
        for (int wide = 1; wide <= 60; wide++)
        {
            for (int deep = 1; deep <= 60; deep++)
            {
                int floor = BuildingPlan.HabitableTiles(wide, deep);
                int hole = BuildingPlan.Hollow(wide, deep, out int holeWide, out int holeDeep)
                    ? holeWide * holeDeep
                    : 0;

                Assert.True(
                    floor + hole == wide * deep,
                    $"{wide}x{deep}: {floor} of floor and {hole} of courtyard is "
                    + $"{floor + hole}, and the footprint is {wide * deep}. The two readers of this "
                    + "file would draw a different Building from the one the city counted.");

                Assert.True(
                    floor > 0,
                    $"{wide}x{deep} carries no floor at all, so a Building standing on it holds "
                    + "nobody and nothing says why.");
            }
        }
    }

    /// <summary>A footprint with no extent carries no floor and is not a ring.</summary>
    [Theory]
    [InlineData(0, 12)]
    [InlineData(12, 0)]
    [InlineData(-3, 12)]
    public void No_ground_is_no_floor(int wide, int deep)
    {
        Assert.Equal(0, BuildingPlan.HabitableTiles(wide, deep));
        Assert.False(BuildingPlan.Hollow(wide, deep, out _, out _));
    }
}
