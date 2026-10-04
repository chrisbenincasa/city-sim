using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Tests.Persistence;

namespace Borough.Tests.Space;

public sealed class OrientedRectangleTests
{
    [Fact]
    public void Axis_aligned_bounds_and_overlap_match_the_integer_rectangles()
    {
        var a = new LandRectangle(12, 14, 4, 6);
        var rectangle = OrientedRectangle.FromBounds(a);
        Assert.Equal(a, rectangle.Bounds);
        Assert.Equal((Fixed.FromInt(14), Fixed.FromInt(17)), rectangle.Center);
        for (int y = 7; y <= 22; y++)
        {
            for (int x = 7; x <= 22; x++)
            {
                var b = new LandRectangle(x, y, 3, 5);
                bool old = a.X < b.X + b.Width && b.X < a.X + a.Width
                    && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                Assert.Equal(b, OrientedRectangle.FromBounds(b).Bounds);
                Assert.Equal(old, rectangle.Overlaps(OrientedRectangle.FromBounds(b)));
                Assert.Equal(old, World.Overlaps(a, b));
            }
        }
        Assert.True(rectangle.Contains(Fixed.FromInt(12), Fixed.FromInt(14)));
        Assert.False(rectangle.Contains(Fixed.FromInt(16), Fixed.FromInt(14)));
        Assert.False(rectangle.Contains(Fixed.FromInt(12), Fixed.FromInt(20)));
    }

    [Fact]
    public void Diagonal_rectangle_uses_its_edges_rather_than_its_bounding_box()
    {
        var diagonal = new OrientedRectangle(Fixed.FromInt(10), Fixed.FromInt(10), 46341, 46341, 4, 4);
        var inside = OrientedRectangle.FromBounds(new(9, 12, 1, 1));
        var outside = OrientedRectangle.FromBounds(new(12, 10, 1, 1));
        Assert.Equal(new LandRectangle(7, 10, 6, 6), diagonal.Bounds);
        Assert.True(World.Overlaps(diagonal.Bounds, outside.Bounds));
        Assert.True(diagonal.Overlaps(inside));
        Assert.True(inside.Overlaps(diagonal));
        Assert.False(diagonal.Overlaps(outside));
        Assert.False(outside.Overlaps(diagonal));
        Assert.True(diagonal.Contains(inside));
        Assert.False(diagonal.Contains(outside));
        Assert.True(diagonal.Contains(Fixed.FromInt(10), Fixed.FromInt(12)));
        Assert.False(diagonal.Contains(Fixed.FromInt(12), Fixed.FromInt(10)));
        Assert.True(diagonal.Contains(diagonal));
    }

    [Fact]
    public void Rotated_shared_edges_and_single_Q16_gaps_do_not_overlap()
    {
        var a = new OrientedRectangle(Fixed.FromInt(10), Fixed.FromInt(10), 46341, 46341, 4, 3);
        var touching = a with { EastQ16 = a.EastQ16 + 4 * a.AxisEastQ16, NorthQ16 = a.NorthQ16 + 4 * a.AxisNorthQ16 };
        var gap = touching with { EastQ16 = touching.EastQ16 + 1 };
        var overlap = touching with { EastQ16 = touching.EastQ16 - 1 };
        Assert.False(a.Overlaps(touching));
        Assert.False(touching.Overlaps(a));
        Assert.False(a.Overlaps(gap));
        Assert.True(a.Overlaps(overlap));
        Assert.False(a.Contains(touching.EastQ16, touching.NorthQ16));
        var inner = a with { Wide = 2, Deep = 2 };
        Assert.True(a.Contains(inner));
        Assert.False(inner.Contains(a));
    }

    [Fact]
    public void Bounds_round_all_corners_outward_and_center_keeps_fractional_tiles()
    {
        var rectangle = new OrientedRectangle(Fixed.FromInt(10) + 1, Fixed.FromInt(12) + 1, -Fixed.One, 0, 3, 2);
        Assert.Equal(new LandRectangle(7, 10, 4, 3), rectangle.Bounds);
        Assert.Equal((Fixed.FromInt(8) + Fixed.One / 2 + 1, Fixed.FromInt(11) + 1), rectangle.Center);
        var quarterTurn = new OrientedRectangle(Fixed.FromInt(10), Fixed.FromInt(10), 0, Fixed.One, 4, 3);
        Assert.Equal(new LandRectangle(7, 10, 3, 4), quarterTurn.Bounds);
        Assert.True(quarterTurn.Contains(OrientedRectangle.FromBounds(quarterTurn.Bounds)));
    }

    [Fact]
    public void World_extent_projections_fit_and_invalid_ground_has_no_interior()
    {
        int size = CellGrid.WorldCells * CellGrid.TilesPerCell;
        var world = OrientedRectangle.FromBounds(new(0, 0, size, size));
        var corner = OrientedRectangle.FromBounds(new(size - 1, size - 1, 1, 1));
        var diagonal = new OrientedRectangle(Fixed.FromInt(size / 2), 0, 46340, 46340, size / 2, size / 2);
        Assert.True(world.Contains(corner));
        Assert.True(world.Overlaps(corner));
        Assert.True(world.Contains(diagonal));
        Assert.True(world.Overlaps(diagonal));
        Assert.False(world.Overlaps(default));
        Assert.False(world.Overlaps(corner with { Wide = 0 }));
        Assert.False(world.Contains(corner with { AxisEastQ16 = int.MaxValue }));
    }

    [Fact]
    public void Lot_creation_preserves_bounds_and_saved_rotated_ground_survives_reload()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        int row = world.Lots.Rows.Resolve(lots[1]);
        for (int i = 0; i < world.Lots.Rows.SlotCount; i++)
        {
            Assert.Equal(Fixed.One, world.Lots.AxisEastQ16[i]);
            Assert.Equal(0, world.Lots.AxisNorthQ16[i]);
            Assert.Equal(new LandRectangle(Fixed.ToIntFloor(world.Lots.ParcelEastQ16[i]),
                Fixed.ToIntFloor(world.Lots.ParcelNorthQ16[i]), world.Lots.ParcelWide[i].Raw,
                world.Lots.ParcelDeep[i].Raw), world.Lots.ParcelBounds(i));
            Assert.Equal(new LandRectangle(Fixed.ToIntFloor(world.Lots.FootprintEastQ16[i]),
                Fixed.ToIntFloor(world.Lots.FootprintNorthQ16[i]), world.Lots.FootprintWide[i].Raw,
                world.Lots.FootprintDeep[i].Raw), world.Lots.FootprintBounds(i));
        }
        world.Lots.ParcelEastQ16[row] = Fixed.FromInt(40) + 123;
        world.Lots.FootprintEastQ16[row] = Fixed.FromInt(40) + 456;
        world.Lots.AxisEastQ16[row] = 46341;
        world.Lots.AxisNorthQ16[row] = 46341;
        var saved = new MemorySave();
        SaveFile.Write(world, 62002, saved);
        World loaded = SaveFile.Read(saved, world.Rules, out _);
        Assert.Equal(world.HashState(), loaded.HashState());
        Assert.Equal(world.Lots.Parcel(row), loaded.Lots.Parcel(row));
        Assert.Equal(world.Lots.Footprint(row), loaded.Lots.Footprint(row));
    }

    [Fact]
    public void Subdivider_hit_test_uses_saved_preview_geometry()
    {
        var geometry = new OrientedRectangle(Fixed.FromInt(10), Fixed.FromInt(10), 46341, 46341, 4, 4);
        var parcel = new Parcel(BlockFace.South, StreetSide.Left, Tiles.Zero,
            new Tiles(7), new Tiles(10), new Tiles(6), new Tiles(6))
        { Geometry = geometry };
        Assert.True(LotSubdivider.Contains(parcel, new Tiles(10), new Tiles(12)));
        Assert.False(LotSubdivider.Contains(parcel, new Tiles(12), new Tiles(10)));
    }

    [Fact]
    public void Local_assembly_checks_the_saved_shape_of_other_lots()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        var extra = world.Lots.Create(new Tiles(100), new Tiles(100), LotTable.Housing,
            wide: new Tiles(4), deep: new Tiles(4));
        int row = world.Lots.Rows.Resolve(extra);
        world.Lots.ParcelEastQ16[row] = Fixed.FromInt(proposal.Site.X + proposal.Site.Width + 1);
        world.Lots.ParcelNorthQ16[row] = Fixed.FromInt(proposal.Site.Y + proposal.Site.Height - 1);
        world.Lots.AxisEastQ16[row] = 46341;
        world.Lots.AxisNorthQ16[row] = 46341;
        Assert.True(World.Overlaps(proposal.Site, world.Lots.ParcelBounds(row)));
        Assert.False(World.Overlaps(proposal.Site, world.Lots.Parcel(row)));
        Assert.True(LocalLayout.Evaluate(world, [lots[1]], proposal.Building, out _).Accepted);
        world.Lots.ParcelEastQ16[row] -= Fixed.FromInt(2);
        Assert.Equal(LocalLayoutRefusal.Overlap,
            LocalLayout.Evaluate(world, [lots[1]], proposal.Building, out _).Refusal);
    }

    [Fact]
    public void Local_assembly_refuses_rotated_sources_and_revalidation_captures_the_axis()
    {
        var (world, lots) = LocalLayoutTests.Fixture();
        var proposal = LocalLayoutTests.Evaluate(world, [lots[1]], BlockPattern.BackToBack);
        int row = world.Lots.Rows.Resolve(lots[1]);
        world.Lots.AxisEastQ16[row] = 46341;
        world.Lots.AxisNorthQ16[row] = 46341;
        ulong before = world.HashState();
        Assert.Equal(LocalLayoutRefusal.InvalidGeometry,
            LocalLayout.Evaluate(world, [lots[1]], proposal.Building, out _).Refusal);
        Assert.Equal(LocalLayoutRefusal.StaleProposal,
            new Simulation(world, world.Key).CommitLocalLayout(proposal, out _).Refusal);
        Assert.Equal(before, world.HashState());
    }
}
