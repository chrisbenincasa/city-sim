using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Borough.Core.Space;

namespace Borough.Tests.Space;

public sealed class SegmentSideTests
{
    private static SidePlot[] Cut(StreetArc line, StreetSide side, int wide = 6, int deep = 10, int half = 1)
    {
        var plots = new SidePlot[SegmentSide.Count(line, wide)];
        int count = SegmentSide.Cut(line, side, wide, deep, half, plots);
        return plots[..count];
    }

    private static StreetArc Line(int bEast, int bNorth, int sagittaTiles = 0)
    {
        Assert.True(StreetArc.TryCreate(0, 0, bEast, bNorth, sagittaTiles * Fixed.One, out StreetArc line));
        return line;
    }

    [Fact]
    public void A_straight_left_side_cuts_grid_rectangles_behind_the_street()
    {
        SidePlot[] plots = Cut(Line(30, 0), StreetSide.Left);

        Assert.Equal(5, plots.Length);
        for (int k = 0; k < plots.Length; k++)
        {
            Assert.Equal(new Tiles(6 * k), plots[k].Offset);
            Assert.Equal(OrientedRectangle.FromBounds(new LandRectangle(6 * k, 1, 6, 10)), plots[k].Geometry);
        }
    }

    [Fact]
    public void A_straight_right_side_cuts_the_mirror_rectangles()
    {
        SidePlot[] plots = Cut(Line(30, 0), StreetSide.Right);

        Assert.Equal(5, plots.Length);
        for (int k = 0; k < plots.Length; k++)
        {
            Assert.Equal(new Tiles(6 * k), plots[k].Offset);
            Assert.Equal(new LandRectangle(6 * k, -11, 6, 10), plots[k].Geometry.Bounds);
        }
    }

    [Fact]
    public void A_length_short_of_one_plot_cuts_nothing()
    {
        Assert.Empty(Cut(Line(5, 0), StreetSide.Left));
    }

    [Fact]
    public void A_curve_fans_out_on_its_outside_and_converges_on_its_inside()
    {
        StreetArc line = Line(64, 0, sagittaTiles: 12);
        Assert.False(line.IsStraight);

        SidePlot[] outside = Cut(line, StreetSide.Left);
        SidePlot[] inside = Cut(line, StreetSide.Right);

        Assert.Equal(SegmentSide.Count(line, 6), outside.Length);
        Assert.Equal(outside.Length, inside.Length);
        Assert.False(AnyOverlap(outside), "plots on the outside of the curve overlap each other.");
        Assert.True(AnyOverlap(inside), "plots on the inside of the curve never converge.");

        foreach (SidePlot plot in outside.Concat(inside))
        {
            var (east, north) = plot.Geometry.Center;
            Assert.True(line.DistanceTo(east, north) > Fixed.One, "a plot's middle sits on the street.");
        }
    }

    private static bool AnyOverlap(SidePlot[] plots)
    {
        for (int i = 0; i < plots.Length; i++)
        {
            for (int j = i + 1; j < plots.Length; j++)
            {
                if (plots[i].Geometry.Overlaps(plots[j].Geometry)) { return true; }
            }
        }

        return false;
    }
}
