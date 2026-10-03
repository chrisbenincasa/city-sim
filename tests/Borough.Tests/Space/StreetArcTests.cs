using Borough.Core.Arithmetic;
using Borough.Core.Space;

namespace Borough.Tests.Space;

public class StreetArcTests
{
    private const double PointTolerance = 1.0 / 64;

    [Fact]
    public void Straight_arc_clamps_points_and_projects_to_the_finite_chord()
    {
        var arc = Create(0, 0, 30, 40, 0);
        Assert.True(arc.IsStraight);
        Assert.Equal(50 * Fixed.One, arc.Length);
        Assert.Equal((0L, 0L), arc.PointAt(-1));
        Assert.Equal((30L * Fixed.One, 40L * Fixed.One), arc.PointAt(int.MaxValue));
        Assert.Equal((15L * Fixed.One, 20L * Fixed.One), arc.PointAt(arc.Length / 2));
        var tangent = arc.TangentAt(0);
        Assert.InRange(Math.Abs(tangent.East - (0.6 * Fixed.One)), 0, 1);
        Assert.InRange(Math.Abs(tangent.North - (0.8 * Fixed.One)), 0, 1);
        Assert.InRange(Math.Abs(arc.OffsetAlong(11L * Fixed.One, 23L * Fixed.One) - (25L * Fixed.One)), 0, 2);
        Assert.InRange(Math.Abs(arc.DistanceTo(11L * Fixed.One, 23L * Fixed.One) - (5L * Fixed.One)), 0, 2);
        Assert.Equal(0, arc.OffsetAlong(-30L * Fixed.One, -40L * Fixed.One));
        Assert.Equal(arc.Length, arc.OffsetAlong(60L * Fixed.One, 80L * Fixed.One));
    }

    [Fact]
    public void Degenerate_and_over_quarter_turn_arcs_are_refused()
    {
        Assert.False(StreetArc.TryCreate(1, 2, 1, 2, 0, out _));
        int limit = (int)Math.Floor(100 * (Math.Sqrt(2) - 1) * Fixed.One / 2);
        Assert.True(StreetArc.TryCreate(0, 0, 100, 0, limit, out _));
        Assert.True(StreetArc.TryCreate(0, 0, 100, 0, -limit, out _));
        Assert.False(StreetArc.TryCreate(0, 0, 100, 0, limit + 1, out _));
        Assert.False(StreetArc.TryCreate(0, 0, 100, 0, -limit - 1, out _));
        Assert.False(StreetArc.TryCreate(0, 0, 100, 0, int.MinValue, out _));
        Assert.False(StreetArc.TryCreate(0, 0, int.MaxValue, int.MinValue, 0, out _));
    }

    [Theory]
    [InlineData(0, 0, 100, 0, 20)]
    [InlineData(100, 100, 0, 0, 20)]
    [InlineData(0, 100, 100, 0, -20)]
    [InlineData(16300, 16200, 16200, 16300, 20)]
    public void Points_and_lengths_match_a_reference_circle(int aE, int aN, int bE, int bN, int s)
    {
        var arc = Create(aE, aN, bE, bN, s * Fixed.One);
        CheckReference(arc, aE, aN, bE, bN, s);

        // Endpoint tolerance is one raw Q16.16 unit; construction preserves them exactly.
        Assert.InRange(Math.Abs(arc.PointAt(0).East - ((long)aE * Fixed.One)), 0, 1);
        Assert.InRange(Math.Abs(arc.PointAt(0).North - ((long)aN * Fixed.One)), 0, 1);
        Assert.InRange(Math.Abs(arc.PointAt(arc.Length).East - ((long)bE * Fixed.One)), 0, 1);
        Assert.InRange(Math.Abs(arc.PointAt(arc.Length).North - ((long)bN * Fixed.One)), 0, 1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(6553600)]
    public void Long_gentle_arcs_stay_within_one_sixty_fourth_tile(int sagitta)
    {
        var arc = Create(0, 100, CellGrid.WorldTiles, 100, sagitta);
        CheckReference(arc, 0, 100, CellGrid.WorldTiles, 100, sagitta / (double)Fixed.One);
        if (sagitta >= 1024)
        {
            Assert.False(arc.IsStraight);
            Assert.True(arc.Radius > int.MaxValue);
            var middle = arc.PointAt(arc.Length / 2);
            Assert.InRange(Math.Abs((middle.North / (double)Fixed.One) - 100 - (sagitta / (double)Fixed.One)), 0, PointTolerance);
            Assert.InRange(Math.Abs(arc.OffsetAlong(middle.East, middle.North) - (arc.Length / 2.0)), 0, Fixed.One * PointTolerance);
            Assert.InRange(arc.DistanceTo(middle.East, middle.North), 0, Fixed.One * PointTolerance);
        }
    }

    [Theory]
    [InlineData(123, 321, 15673, 12677, 1)]
    [InlineData(123, 321, 15673, 12677, 1024)]
    [InlineData(15673, 12677, 123, 321, -2048)]
    [InlineData(0, 16384, 16384, 0, 4096)]
    [InlineData(0, 0, 3, 4, 1)]
    public void Gentle_rotated_arcs_keep_sub_tile_precision(int aE, int aN, int bE, int bN, int sagitta)
    {
        var arc = Create(aE, aN, bE, bN, sagitta);
        CheckReference(arc, aE, aN, bE, bN, sagitta / (double)Fixed.One);
    }

    [Fact]
    public void Positive_sagitta_bulges_left_and_reversing_endpoints_reverses_the_sign()
    {
        var left = Create(0, 0, 100, 0, 20 * Fixed.One);
        var right = Create(0, 0, 100, 0, -20 * Fixed.One);
        var reverse = Create(100, 0, 0, 0, -20 * Fixed.One);
        Assert.True(left.Sweep < 0);
        Assert.True(right.Sweep > 0);
        Assert.InRange(left.PointAt(left.Length / 2).North, (20L * Fixed.One) - 8, (20L * Fixed.One) + 8);
        Assert.InRange(right.PointAt(right.Length / 2).North, (-20L * Fixed.One) - 8, (-20L * Fixed.One) + 8);
        Assert.Equal(left.Center, reverse.Center);
        for (int i = 0; i <= 10; i++)
        {
            var forward = left.PointAt(left.Length * i / 10);
            var backward = reverse.PointAt(reverse.Length - (reverse.Length * i / 10));
            Assert.InRange(Math.Abs(forward.East - backward.East), 0, 8);
            Assert.InRange(Math.Abs(forward.North - backward.North), 0, 8);
        }
    }

    [Theory]
    [InlineData(20)]
    [InlineData(-20)]
    public void Nearest_point_queries_round_trip_and_clamp_outside_the_sweep(int sagitta)
    {
        var arc = Create(0, 0, 100, 0, sagitta * Fixed.One);
        for (int i = 0; i <= 20; i++)
        {
            int offset = arc.Length * i / 20;
            var point = arc.PointAt(offset);
            var tangent = arc.TangentAt(offset);
            Assert.InRange(Math.Abs(double.Hypot(tangent.East, tangent.North) - Fixed.One), 0, 2);
            Assert.InRange(Math.Abs(arc.OffsetAlong(point.East, point.North) - offset), 0, 16);
            Assert.InRange(arc.DistanceTo(point.East, point.North), 0, 16);
            long east = point.East - (3L * tangent.North);
            long north = point.North + (3L * tangent.East);
            Assert.InRange(Math.Abs(arc.OffsetAlong(east, north) - offset), 0, 16);
            Assert.InRange(Math.Abs(arc.DistanceTo(east, north) - (3 * Fixed.One)), 0, 16);
        }

        var startTangent = arc.TangentAt(0);
        var endTangent = arc.TangentAt(arc.Length);
        long beforeE = arc.A.East - (10L * startTangent.East);
        long beforeN = arc.A.North - (10L * startTangent.North);
        Assert.Equal(0, arc.OffsetAlong(beforeE, beforeN));
        Assert.InRange(Math.Abs(arc.DistanceTo(beforeE, beforeN) - (10 * Fixed.One)), 0, 16);
        Assert.Equal(arc.Length, arc.OffsetAlong(arc.B.East + (10L * endTangent.East), arc.B.North + (10L * endTangent.North)));
    }

    [Fact]
    public void Parallel_curves_keep_the_center_and_shift_left_without_tile_rounding()
    {
        var left = Create(0, 0, 100, 0, 20 * Fixed.One);
        var shifted = left.Parallel(3 * Fixed.One);
        Assert.Equal(left.Center, shifted.Center);
        Assert.Equal(left.Radius + (3L * Fixed.One), shifted.Radius);
        Assert.Equal(left.StartAngle, shifted.StartAngle);
        Assert.Equal(left.Sweep, shifted.Sweep);
        var middle = shifted.PointAt(shifted.Length / 2);
        Assert.InRange(Math.Abs(middle.North - (23L * Fixed.One)), 0, 16);
        Assert.NotEqual(0, shifted.A.East % Fixed.One);

        var right = Create(0, 0, 100, 0, -20 * Fixed.One);
        Assert.Equal(right.Radius - (3L * Fixed.One), right.Parallel(3 * Fixed.One).Radius);
        Assert.Throws<ArgumentOutOfRangeException>(() => right.Parallel((int)right.Radius));
        var gentle = Create(0, 0, 100, 0, -Fixed.One);
        Assert.Throws<ArgumentOutOfRangeException>(() => gentle.Parallel((int)gentle.Radius - 1));

        var line = Create(0, 0, 100, 0, 0).Parallel(Fixed.One / 2);
        Assert.True(line.IsStraight);
        Assert.Equal((0L, (long)Fixed.One / 2), line.A);
        Assert.Equal((100L * Fixed.One, (long)Fixed.One / 2), line.B);
        var diagonal = Create(0, 0, 3, 4, 0).Parallel(123 * Fixed.One + (Fixed.One / 8));
        Assert.InRange(Math.Abs(diagonal.A.East - (-98.5 * Fixed.One)), 0, 2);
        Assert.InRange(Math.Abs(diagonal.A.North - (73.875 * Fixed.One)), 0, 2);
    }

    [Theory]
    [InlineData(0, 0, 50, 20, 100, 0, 20)]
    [InlineData(0, 0, 50, -20, 100, 0, -20)]
    [InlineData(100, 0, 50, 20, 0, 0, -20)]
    [InlineData(10, 10, -10, 60, 10, 110, 20)]
    [InlineData(0, 0, 50, 0, 100, 0, 0)]
    [InlineData(0, 0, 64, 8, 78, 0, 13)]
    [InlineData(0, 0, 5, 20, 10, 0, 20)]
    public void Sagitta_through_three_points_recovers_the_arc(int aE, int aN, int mE, int mN, int bE, int bN, int expected)
    {
        int sagitta = StreetArc.SagittaThrough(aE, aN, mE, mN, bE, bN);
        Assert.InRange(Math.Abs(sagitta - (expected * Fixed.One)), 0, 8);
    }

    [Fact]
    public void Nearly_collinear_world_scale_points_do_not_overflow_the_circle_fit()
    {
        Assert.Equal(-2, StreetArc.SagittaThrough(0, 0, 8190, 8189, 16381, 16379));
        Assert.Equal(2, StreetArc.SagittaThrough(16381, 16379, 8190, 8189, 0, 0));
    }

    [Theory]
    [InlineData(0, 0, 100, 0, 20, 0.5)]
    [InlineData(0, 0, 100, 0, -20, 0.3)]
    [InlineData(0, 0, 100, 100, 20, 0.7)]
    [InlineData(0, 0, 100, 100, 0, 0.333)]
    public void Splits_pass_through_the_rounded_node_and_preserve_length_within_rounding_tolerance(
        int aE, int aN, int bE, int bN, int sagitta, double fraction)
    {
        var arc = Create(aE, aN, bE, bN, sagitta * Fixed.One);
        int offset = (int)(arc.Length * fraction);
        Assert.True(arc.TrySplitAt(offset, out int nodeE, out int nodeN, out int s1, out int s2));
        var point = arc.PointAt(offset);
        Assert.InRange(Math.Abs(nodeE - (point.East / (double)Fixed.One)), 0, 0.5);
        Assert.InRange(Math.Abs(nodeN - (point.North / (double)Fixed.One)), 0, 0.5);
        var first = Create(aE, aN, nodeE, nodeN, s1);
        var second = Create(nodeE, nodeN, bE, bN, s2);
        Assert.Equal(first.B, second.A);
        // Tile rounding changes the fitted circle, so length tolerance is one Tile.
        Assert.InRange(Math.Abs((long)first.Length + second.Length - arc.Length), 0, Fixed.One);
        if (s1 != 0 && s2 != 0)
        {
            Assert.InRange(Math.Abs(first.Center.East - second.Center.East), 0, Fixed.One * PointTolerance);
            Assert.InRange(Math.Abs(first.Center.North - second.Center.North), 0, Fixed.One * PointTolerance);
        }
    }

    [Fact]
    public void Splits_at_or_rounded_onto_endpoints_are_refused()
    {
        var arc = Create(0, 0, 100, 0, 20 * Fixed.One);
        Assert.False(arc.TrySplitAt(0, out _, out _, out _, out _));
        Assert.False(arc.TrySplitAt(arc.Length, out _, out _, out _, out _));
        Assert.False(arc.TrySplitAt(1, out _, out _, out _, out _));
    }

    private static StreetArc Create(int aE, int aN, int bE, int bN, int sagitta)
    {
        Assert.True(StreetArc.TryCreate(aE, aN, bE, bN, sagitta, out var arc));
        return arc;
    }

    private static void CheckReference(StreetArc arc, int aE, int aN, int bE, int bN, double sagitta)
    {
        double dx = bE - aE;
        double dy = bN - aN;
        double chord = double.Hypot(dx, dy);
        double s = Math.Abs(sagitta);
        double radius = (chord * chord / (8 * s)) + (s / 2);
        double sweep = 4 * Math.Atan2(2 * s, chord);
        double expectedLength = radius * sweep;
        Assert.InRange(Math.Abs((arc.Length / (double)Fixed.One) - expectedLength), 0, 16.0 / Fixed.One);
        for (int i = 0; i <= 100; i++)
        {
            int offset = (int)((long)arc.Length * i / 100);
            double t = offset / (double)arc.Length;
            double angle = (t - 0.5) * sweep;
            double along = radius * Math.Sin(angle);
            // This form avoids subtracting the very large center from the radius.
            double across = s - (2 * radius * Math.Pow(Math.Sin(angle / 2), 2));
            across = Math.CopySign(across, sagitta);
            double expectedE = ((aE + bE) / 2.0) + (dx * along / chord) - (dy * across / chord);
            double expectedN = ((aN + bN) / 2.0) + (dy * along / chord) + (dx * across / chord);
            var point = arc.PointAt(offset);
            Assert.InRange(Math.Abs((point.East / (double)Fixed.One) - expectedE), 0, PointTolerance);
            Assert.InRange(Math.Abs((point.North / (double)Fixed.One) - expectedN), 0, PointTolerance);
        }
    }
}
