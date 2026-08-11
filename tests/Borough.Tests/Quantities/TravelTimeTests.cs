using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Borough.Core.Arithmetic;
using Borough.Core.Quantities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Borough.Tests.Quantities;

/// <summary>
/// adr/0071 — travel time is sub-Tick, and Q16.16 is a scale rather than a meaning.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim under test is not that the arithmetic works.</b> It is that the quantity routing
/// minimises is the quantity a panel shows, at a resolution fine enough that the two cannot diverge —
/// <c>02 §5.9</c>'s constraint, and SC4's failure. At whole-Tick resolution a 4-Tile stub and a 60-Tile
/// run both cost 1, so A* over that graph minimises <b>hop count</b> while every column says Ticks.
/// The tests below are the ones that would fail the day somebody rounds a cost to a whole Tick.
/// </para>
/// <para>
/// <b>The compile-time half is asserted, not asserted-in-a-comment.</b> adr/0071's stated benefit is
/// that no quantity is assignable to another, which no runtime assertion can reach — so the probe
/// compilations at the end follow <see cref="NegativeCompilationTests"/>' form, for the reason that
/// file gives: a convention needs a type or a lint, because it never survives on discipline.
/// </para>
/// </remarks>
public class TravelTimeTests
{
    /// <summary>The exchange rate stated in <c>Speed</c>'s own derivation, restated here on purpose.</summary>
    /// <remarks>
    /// Recomputed from the comment rather than read from the private const, so that a change to the
    /// factor has to be argued against the derivation instead of silently agreeing with itself.
    /// </remarks>
    private const int RawPerKilometrePerHour = 48_000;

    // ---------------------------------------------------------------------------------------------
    // Speed: the conversion a human authors through, and the one adr/0071 says 05 §121 would corrupt.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The km/h factor is exact, so a Street's 50 is an exact raw and carries no rounding of its own.
    /// </summary>
    [Fact]
    public void A_speed_in_kilometres_per_hour_converts_exactly()
    {
        Assert.Equal(
            50 * RawPerKilometrePerHour, Speed.FromKilometresPerHour(50).Raw);

        Assert.Equal(Speed.Zero, Speed.FromKilometresPerHour(0));
    }

    /// <summary>
    /// <b>The 20% error adr/0071 exists to refuse.</b> A walking pace of 5 km/h is 3.66 Tiles/Tick;
    /// applied literally, <c>05 §121</c>'s <i>"Q16.16 is for sub-Tile positions and nothing else"</i>
    /// forces it into whole Tiles/Tick and it becomes 3 — on the mode the whole pedestrian layer is
    /// made of.
    /// </summary>
    /// <remarks>
    /// Both halves are asserted because either alone is weak: that the floor <em>is</em> 3 shows what
    /// the literal reading would keep, and that the raw is <em>not</em> <c>3 × Fixed.One</c> shows what
    /// it would throw away.
    /// </remarks>
    [Fact]
    public void A_walking_pace_keeps_the_fraction_the_whole_tile_reading_would_lose()
    {
        Speed walk = Speed.FromKilometresPerHour(5);

        Assert.Equal(5 * RawPerKilometrePerHour, walk.Raw);
        Assert.Equal(new Tiles(3), walk.ToTilesPerTickFloor());

        // The 20% error, stated as the inequality it is.
        Assert.NotEqual(3 * Fixed.One, walk.Raw);
        Assert.True(walk.Raw > 3 * Fixed.One);
        Assert.True(walk.Raw < 4 * Fixed.One);
    }

    [Fact]
    public void A_speed_refuses_to_be_negative() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Speed.FromKilometresPerHour(-1));

    /// <summary>
    /// The Q16.16 ceiling, taken from the format rather than from prose.
    /// </summary>
    /// <remarks>
    /// <b>The docstring on <c>Speed.FromKilometresPerHour</c> says the range is exceeded "at ~682
    /// km/h" and the guard it documents refuses at 44,739.</b> The guard is
    /// <c>FloorDiv(Fixed.MaxValue, 48_000)</c>, and <c>Fixed.MaxValue</c> is <c>int.MaxValue</c> — the
    /// raw ceiling, not the whole-value one. 682 is <c>32,767 ÷ 48</c>: the whole part of the format's
    /// range divided by the factor with its thousand dropped, which is a units slip of exactly 65.536×.
    /// The <em>guard</em> is right — a raw of 2,147,472,000 is the largest speed the format holds — so
    /// this test asserts the code and the prose is what owes a correction.
    /// </remarks>
    [Fact]
    public void A_speed_refuses_to_leave_the_format_and_accepts_the_boundary()
    {
        int ceiling = IntegerMath.FloorDiv(Fixed.MaxValue, RawPerKilometrePerHour);

        Assert.Equal(44_739, ceiling);

        Speed fastest = Speed.FromKilometresPerHour(ceiling);
        Assert.Equal(ceiling * RawPerKilometrePerHour, fastest.Raw);
        Assert.True(fastest.Raw > 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Speed.FromKilometresPerHour(ceiling + 1));
    }

    /// <summary>
    /// <c>min(the mode's own ceiling, the road's free-flow speed)</c> — a pedestrian walks at walking
    /// pace on a boulevard, and a car is held to the road.
    /// </summary>
    [Fact]
    public void The_slower_of_two_speeds_is_the_smaller_one()
    {
        Speed walk = Speed.FromKilometresPerHour(5);
        Speed street = Speed.FromKilometresPerHour(50);

        Assert.Equal(walk, Speed.SlowerOf(walk, street));
        Assert.Equal(walk, Speed.SlowerOf(street, walk));
        Assert.Equal(walk, Speed.SlowerOf(walk, walk));
        Assert.True(walk < street);
    }

    // ---------------------------------------------------------------------------------------------
    // TravelTime: the load-bearing half.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// <b>adr/0071's core claim, as an assertion.</b> A 32-Tile Street at 50 km/h is 0.87 Ticks — a
    /// real cost, strictly inside one Tick, which whole-Tick resolution has no way to hold.
    /// </summary>
    [Fact]
    public void A_street_segment_costs_less_than_one_whole_tick()
    {
        TravelTime cost = TravelTime.Over(new Tiles(32), Speed.FromKilometresPerHour(50));

        Assert.True(cost > TravelTime.Zero);
        Assert.True(cost < TravelTime.FromTicks(1));

        // 0.87 Ticks, to the resolution the format has: 57,266 / 65,536.
        Assert.Equal(57_266, cost.Raw);

        // What the panel would print, and the reason it must never be what A* compares.
        Assert.Equal(new Ticks(0), cost.ToTicksFloor());
    }

    /// <summary>
    /// <b>The failure adr/0071 exists to prevent, stated as the inequality that prevents it.</b> Round
    /// these three to whole Ticks and a 4-Tile stub, a 32-Tile Street and a 60-Tile run cost 1, 1 and
    /// 1 — at which point A* is minimising hop count while appearing to route on time.
    /// </summary>
    [Fact]
    public void A_stub_a_street_and_a_long_run_cost_three_different_things()
    {
        Speed street = Speed.FromKilometresPerHour(50);

        TravelTime stub = TravelTime.Over(new Tiles(4), street);
        TravelTime segment = TravelTime.Over(new Tiles(32), street);
        TravelTime run = TravelTime.Over(new Tiles(60), street);

        Assert.True(stub < segment);
        Assert.True(segment < run);
        Assert.NotEqual(stub, segment);
        Assert.NotEqual(segment, run);

        // And the three are indistinguishable once whole Ticks are all you have: two of them floor to
        // the same number, which is the whole content of the claim.
        Assert.Equal(stub.ToTicksFloor(), segment.ToTicksFloor());
    }

    /// <summary>
    /// <b>The distance a cost is proportional to, not the hop it sits on.</b> Twice the Tiles at one
    /// speed is twice the cost, exactly, when the distance divides cleanly.
    /// </summary>
    [Fact]
    public void Cost_scales_with_distance_rather_than_with_hop_count()
    {
        Speed street = Speed.FromKilometresPerHour(50);

        TravelTime one = TravelTime.Over(new Tiles(16), street);
        TravelTime two = TravelTime.Over(new Tiles(32), street);

        Assert.Equal(one * 2, two);
    }

    /// <summary>
    /// <b>Exact addition is the property A* actually depends on</b> — a path's cost is the exact sum of
    /// its Arcs' costs, so two routes are compared on what they are rather than on where the rounding
    /// fell.
    /// </summary>
    /// <remarks>
    /// The case is chosen so that whole-Tick rounding would lose everything: seven 32-Tile Streets at
    /// 50 km/h are 6.12 Ticks, and seven costs each floored to a whole Tick sum to <b>zero</b>. The
    /// sum is asserted against <c>cost × 7</c> rather than against a literal, because the claim is
    /// exactness rather than a number.
    /// </remarks>
    [Fact]
    public void Summing_arcs_is_exact_where_whole_ticks_would_lose_everything()
    {
        const int Arcs = 7;

        TravelTime arc = TravelTime.Over(new Tiles(32), Speed.FromKilometresPerHour(50));

        var path = TravelTime.Zero;
        for (int i = 0; i < Arcs; i++)
        {
            path += arc;
        }

        Assert.Equal(arc * Arcs, path);
        Assert.Equal(Arcs * arc, path);
        Assert.Equal(57_266 * Arcs, path.Raw);

        // Six whole Ticks of travel that whole-Tick arithmetic would have priced at nothing: each
        // arc on its own floors to zero, so seven of them would too.
        Assert.Equal(new Ticks(6), path.ToTicksFloor());
        Assert.Equal(Ticks.Zero, arc.ToTicksFloor());
    }

    /// <summary>Subtraction and the additive identity, which a search relaxes through.</summary>
    [Fact]
    public void Zero_is_the_additive_identity_and_subtraction_undoes_addition()
    {
        TravelTime arc = TravelTime.Over(new Tiles(32), Speed.FromKilometresPerHour(50));

        Assert.Equal(arc, arc + TravelTime.Zero);
        Assert.Equal(TravelTime.Zero, arc - arc);
        Assert.Equal(arc, (arc + arc) - arc);
    }

    /// <summary>Rounding is floor, so a cost underestimates by at most one step — the safe direction.</summary>
    /// <remarks>
    /// Asserted as a bracket rather than as a literal: the true 32-Tile cost at 50 km/h is
    /// 57,266.23 raw, so the stored value is the floor and the next step up is strictly greater than
    /// the truth.
    /// </remarks>
    [Fact]
    public void Cost_floors_rather_than_rounding_up()
    {
        Speed street = Speed.FromKilometresPerHour(50);
        TravelTime cost = TravelTime.Over(new Tiles(32), street);

        long exactNumerator = (long)Fixed.FromInt(32) << Fixed.FractionalBits;
        long truth = exactNumerator / street.Raw;

        Assert.Equal(truth, (long)cost.Raw);
        Assert.True((long)cost.Raw * street.Raw <= exactNumerator);
        Assert.True((long)(cost.Raw + 1) * street.Raw > exactNumerator);
    }

    /// <summary>
    /// <b>Impassable is a sentinel, and a search must refuse it rather than relax through it.</b> It
    /// compares greater than every real cost by construction: it is the format's ceiling.
    /// </summary>
    [Fact]
    public void Impassable_is_greater_than_any_real_cost()
    {
        TravelTime longest = TravelTime.Over(new Tiles(4_096), Speed.FromKilometresPerHour(5));

        Assert.True(TravelTime.Impassable.IsImpassable);
        Assert.False(longest.IsImpassable);
        Assert.False(TravelTime.Zero.IsImpassable);

        Assert.True(TravelTime.Impassable > longest);
        Assert.True(TravelTime.Impassable > TravelTime.FromTicks(32_767));
        Assert.True(longest < TravelTime.Impassable);
        Assert.Equal(Fixed.MaxValue, TravelTime.Impassable.Raw);
    }

    /// <summary>
    /// <b>Adding to <c>Impassable</c> saturates, so a sentinel survives being summed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This test was written against the docstring and found the code disagreed with it.</b> The
    /// remarks on <c>TravelTime.Impassable</c> claimed <i>"adding to it would overflow, which is the
    /// correct failure"</i>; <c>operator +</c> was a bare <c>int</c> addition, <c>Fixed</c> is the one
    /// place in <c>Borough.Core</c> where <c>checked</c> appears, and nothing in the build turns
    /// ambient checking on — so the sentinel plus a Tick was a large <em>negative</em> cost, which a
    /// search would have relaxed through and preferred to every real route.
    /// </para>
    /// <para>
    /// It was resolved by making the code true rather than the comment weaker: the operator now
    /// saturates. Saturation beats the throw the docstring promised, because the caller is a
    /// relaxation loop and an impassable Arc should be a route nobody takes rather than a crash — and
    /// it is monotone, so a partial path that has reached the sentinel can never come back down below
    /// a real one.
    /// </para>
    /// </remarks>
    [Fact]
    public void Adding_to_the_impassable_sentinel_saturates()
    {
        Assert.True((TravelTime.Impassable + TravelTime.FromTicks(1)).IsImpassable);
        Assert.True((TravelTime.FromTicks(1) + TravelTime.Impassable).IsImpassable);
        Assert.True((TravelTime.Impassable + TravelTime.Impassable).IsImpassable);
    }

    /// <summary>A sum that would reach the ceiling from below saturates rather than wrapping.</summary>
    /// <remarks>
    /// The half that is not about the sentinel: two costs each well inside the format whose sum is
    /// not. Without saturation this is the same negative-cost bug arrived at without anybody ever
    /// naming <c>Impassable</c>, which is the likelier way to meet it once a volume-delay function is
    /// driving a jammed Arc's cost up.
    /// </remarks>
    [Fact]
    public void A_sum_that_reaches_the_ceiling_saturates_rather_than_wrapping()
    {
        TravelTime half = new(Fixed.MaxValue / 2);
        TravelTime sum = half + half + TravelTime.FromTicks(1);

        Assert.True(sum.IsImpassable);
        Assert.True(sum > TravelTime.Zero);
    }

    /// <summary>A stationary mode has no traversal time; it has no traversal.</summary>
    /// <remarks>
    /// <c>Speed.Zero</c>'s own summary says a Segment nobody may traverse is a <em>mask</em>, not a
    /// zero speed — so this throwing is the design refusing to answer a question with no answer,
    /// rather than an unhandled case.
    /// </remarks>
    [Fact]
    public void Travelling_at_no_speed_at_all_throws()
    {
        Assert.Throws<DivideByZeroException>(
            () => TravelTime.Over(new Tiles(32), Speed.Zero));
    }

    /// <summary>A cost that leaves the format raises rather than wrapping — adr/0071's overflow policy.</summary>
    /// <remarks>
    /// The live risk the ADR names is a volume-delay function driving a jammed Arc's cost without
    /// bound. The signal is an exception rather than a wrong answer, which is the whole reason for
    /// building on an arithmetic that already had one.
    /// </remarks>
    [Fact]
    public void A_cost_that_leaves_the_format_throws_rather_than_wrapping()
    {
        // Four Days of headroom against a crawl: 4,096 Tiles at 1 km/h is 5,592 Ticks and fits; the
        // whole-Tick lift past the ceiling does not.
        Assert.True(TravelTime.Over(new Tiles(4_096), Speed.FromKilometresPerHour(1)).Raw > 0);

        Assert.Throws<OverflowException>(() => TravelTime.FromTicks(32_768));
    }

    /// <summary>A dimensionless ratio scales a cost — what a volume-delay function will do to an Arc.</summary>
    [Fact]
    public void A_ratio_scales_a_cost_and_the_product_is_still_a_cost()
    {
        TravelTime free = TravelTime.FromTicks(4);

        Assert.Equal(TravelTime.FromTicks(2), free * Ratio.FromFraction(1, 2));
        Assert.Equal(TravelTime.FromTicks(2), Ratio.FromFraction(1, 2) * free);
        Assert.Equal(free, free * Ratio.One);
    }

    [Fact]
    public void Costs_order_by_their_representation()
    {
        Assert.True(new TravelTime(1) > TravelTime.Zero);
        Assert.True(new TravelTime(1) >= new TravelTime(1));
        Assert.True(new TravelTime(1) <= new TravelTime(1));
        Assert.Equal(0, new TravelTime(5).CompareTo(new TravelTime(5)));
        Assert.True(new TravelTime(4).CompareTo(new TravelTime(5)) < 0);
        Assert.True(Speed.Zero.CompareTo(Speed.FromKilometresPerHour(1)) < 0);
    }

    // ---------------------------------------------------------------------------------------------
    // The type discipline. adr/0071: "This is the whole benefit and it is a compile-time one."
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// CLAUDE.md invariant 7 and adr/0036 — a quantity that may sit in a table row is <c>unmanaged</c>.
    /// </summary>
    [Fact]
    public void Both_new_quantities_are_unmanaged()
    {
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<TravelTime>());
        Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<Speed>());
    }

    /// <summary>
    /// The erasure claim, and adr/0071's <i>"the State Hash does not move"</i>: a record struct over an
    /// <c>int</c> costs exactly the <c>int</c> and folds the bits the <c>int</c> folded.
    /// </summary>
    [Fact]
    public void Both_new_quantities_are_the_size_of_their_representation()
    {
        Assert.Equal(sizeof(int), Unsafe.SizeOf<TravelTime>());
        Assert.Equal(sizeof(int), Unsafe.SizeOf<Speed>());
    }

    /// <summary>
    /// <b>Neither converts to the other, nor to <see cref="Ticks"/>, nor to its representation.</b>
    /// </summary>
    /// <remarks>
    /// adr/0071's consequence in full: <i>"Neither converts to the other implicitly, so a traversal
    /// cost can never be armed into the Wheel by accident, and a Wheel period can never be summed into
    /// a route."</i> That is a compile-time property, so it is asserted by compiling — the probe is
    /// <see cref="NegativeCompilationTests"/>' and lives here rather than there so this file stands
    /// alone against adr/0071.
    /// </remarks>
    [Theory]
    // A cost is not a clock reading, in either direction.
    [InlineData("TravelTime + Ticks", "var _ = new TravelTime(1) + new Ticks(1);")]
    [InlineData("TravelTime to Ticks", "Ticks _ = new TravelTime(1);")]
    [InlineData("Ticks to TravelTime", "TravelTime _ = new Ticks(1);")]
    // A speed is not a duration, and a duration is not a distance.
    [InlineData("TravelTime to Speed", "Speed _ = new TravelTime(1);")]
    [InlineData("Speed to TravelTime", "TravelTime _ = new Speed(1);")]
    [InlineData("TravelTime + Tiles", "var _ = new TravelTime(1) + new Tiles(1);")]
    [InlineData("TravelTime + SubTiles", "var _ = new TravelTime(1) + new SubTiles(1);")]
    [InlineData("Speed + Speed", "var _ = new Speed(1) + new Speed(2);")]
    // adr/0003: fixed x fixed is legal only on dimensionless ratios.
    [InlineData("TravelTime x TravelTime", "var _ = new TravelTime(2) * new TravelTime(3);")]
    [InlineData("Speed x Speed", "var _ = new Speed(2) * new Speed(3);")]
    [InlineData("Tiles / Speed", "var _ = new Tiles(2) / new Speed(3);")]
    // A quantity must not silently become its representation.
    [InlineData("TravelTime from int", "TravelTime _ = 5;")]
    [InlineData("TravelTime to int", "int _ = new TravelTime(5);")]
    [InlineData("Speed to int", "int _ = new Speed(5);")]
    public void Illegal_arithmetic_on_the_new_quantities_does_not_compile(
        string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.False(
            errors.IsEmpty,
            $"'{description}' compiled, and it must not. adr/0071's benefit is entirely a "
            + "compile-time one — if this operator or conversion now exists, it was added without "
            + "the argument.");
    }

    /// <summary>The dimensionally sound operations adr/0071 does offer, held open the same way.</summary>
    [Theory]
    [InlineData("Tiles / Speed -> TravelTime", "TravelTime _ = TravelTime.Over(new Tiles(2), Speed.FromKilometresPerHour(50));")]
    [InlineData("TravelTime + TravelTime", "var _ = new TravelTime(2) + new TravelTime(3);")]
    [InlineData("TravelTime - TravelTime", "var _ = new TravelTime(3) - new TravelTime(2);")]
    [InlineData("TravelTime x int", "var _ = new TravelTime(2) * 3;")]
    [InlineData("int x TravelTime", "var _ = 3 * new TravelTime(2);")]
    [InlineData("TravelTime x Ratio", "var _ = new TravelTime(2) * Ratio.One;")]
    [InlineData("Speed x Ratio", "var _ = new Speed(2) * Ratio.One;")]
    public void Legal_arithmetic_on_the_new_quantities_still_compiles(
        string description, string statement)
    {
        ImmutableArray<Diagnostic> errors = Compile(statement);

        Assert.True(
            errors.IsEmpty,
            $"'{description}' must compile but did not: "
            + string.Join("; ", errors.Select(e => e.GetMessage())));
    }

    /// <summary>Compiles one statement against the real Borough.Core and returns its errors.</summary>
    private static ImmutableArray<Diagnostic> Compile(string statement)
    {
        string source = $$"""
            using Borough.Core.Quantities;

            internal static class Probe
            {
                internal static void Run()
                {
                    {{statement}}
                }
            }
            """;

        var compilation = CSharpCompilation.Create(
            assemblyName: "TravelTimeCompilationProbe",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.WithCore,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
    }
}
