using System.Reflection;
using System.Runtime.CompilerServices;
using Borough.Core.Arithmetic;
using Borough.Core.Quantities;

namespace Borough.Tests.Quantities;

/// <summary>plans/0005 task 1.</summary>
public class QuantityTests
{
    private static readonly Type[] AllQuantities =
    [
        typeof(Money), typeof(Ticks), typeof(Tiles), typeof(SubTiles), typeof(Ratio),
    ];

    /// <summary>
    /// CLAUDE.md invariant 7 — every table row type satisfies `unmanaged`. These are the first
    /// candidate row fields, so they are the first things that have to.
    /// </summary>
    [Fact]
    public void Every_quantity_is_unmanaged()
    {
        MethodInfo probe = typeof(RuntimeHelpers)
            .GetMethod(nameof(RuntimeHelpers.IsReferenceOrContainsReferences))!;

        foreach (Type quantity in AllQuantities)
        {
            bool holdsReferences = (bool)probe.MakeGenericMethod(quantity).Invoke(null, null)!;
            Assert.False(holdsReferences, $"{quantity.Name} is not unmanaged.");
        }
    }

    /// <summary>
    /// The erasure claim: a quantity costs exactly its underlying integer and nothing else.
    /// </summary>
    [Fact]
    public void Every_quantity_is_the_size_of_its_representation()
    {
        Assert.Equal(sizeof(long), Unsafe.SizeOf<Money>());
        Assert.Equal(sizeof(ulong), Unsafe.SizeOf<Ticks>());
        Assert.Equal(sizeof(int), Unsafe.SizeOf<Tiles>());
        Assert.Equal(sizeof(int), Unsafe.SizeOf<SubTiles>());
        Assert.Equal(sizeof(int), Unsafe.SizeOf<Ratio>());
    }

    /// <summary>
    /// Arithmetic on the wrappers must not allocate. This is the cheap continuous form of the
    /// benchmark plans/0005 asks for — it catches boxing, which is the failure that would actually
    /// happen, rather than proving codegen identity.
    /// </summary>
    [Fact]
    public void Arithmetic_on_quantities_allocates_nothing()
    {
        // Warm the paths so first-call JIT allocation is not attributed to the measured region.
        Accumulate(4);

        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);
        long before = GC.GetAllocatedBytesForCurrentThread();
        long result = Accumulate(10_000);
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.NotEqual(0, result);

        AllocationProbe.Check(
            "QuantityTests.Arithmetic_on_quantities_allocates_nothing", before, after, gen0, gen1, gen2);

        static long Accumulate(int iterations)
        {
            var money = Money.Zero;
            var tiles = Tiles.Zero;
            var position = SubTiles.Zero;
            var ratio = Ratio.FromFraction(1, 2);

            for (int i = 1; i <= iterations; i++)
            {
                money += new Money(i);
                tiles += new Tiles(1);
                position += new SubTiles(Fixed.One) * ratio;
            }

            return money.Raw + tiles.Raw + position.Raw;
        }
    }

    [Fact]
    public void Money_debits_only_what_the_balance_covers()
    {
        var balance = new Money(100);

        Assert.True(balance.TryDebit(new Money(40), out Money remaining));
        Assert.Equal(new Money(60), remaining);

        Assert.True(balance.TryDebit(new Money(100), out remaining));
        Assert.Equal(Money.Zero, remaining);
    }

    [Fact]
    public void Money_refuses_a_debit_that_would_go_negative_and_leaves_the_balance_alone()
    {
        var balance = new Money(100);

        Assert.False(balance.TryDebit(new Money(101), out Money remaining));
        Assert.Equal(balance, remaining);
    }

    /// <summary>
    /// The signed representation earning its place: a shortfall is a number, not a wrap.
    /// </summary>
    [Fact]
    public void Money_subtraction_yields_a_signed_shortfall()
    {
        Money shortfall = new Money(100) - new Money(250);

        Assert.True(shortfall.IsNegative);
        Assert.Equal(new Money(-150), shortfall);
    }

    [Fact]
    public void Money_survives_a_flow_that_overflows_32_bits()
    {
        // ~10^9 per period against an int max of 2.1x10^9 — i32 is exceeded within three periods.
        var perPeriod = new Money(1_000_000_000);
        Money total = perPeriod * 12;

        Assert.Equal(12_000_000_000, total.Raw);
        Assert.True(total.Raw > int.MaxValue);
    }

    [Fact]
    public void Ticks_subtraction_refuses_to_wrap()
    {
        var later = new Ticks(8192);
        var earlier = new Ticks(4096);

        Assert.True(later.TrySubtract(earlier, out Ticks elapsed));
        Assert.Equal(new Ticks(4096), elapsed);

        // The case the operator would have wrapped to ~1.8x10^19.
        Assert.False(earlier.TrySubtract(later, out Ticks none));
        Assert.Equal(Ticks.Zero, none);
    }

    [Fact]
    public void Ratio_multiplication_is_the_one_legal_fixed_by_fixed()
    {
        Ratio half = Ratio.FromFraction(1, 2);
        Ratio quarter = half * half;

        Assert.Equal(Ratio.FromFraction(1, 4), quarter);
        Assert.Equal(half, quarter / half);
        Assert.Equal(half, half * Ratio.One);
    }

    [Fact]
    public void SubTiles_scales_by_a_count_and_by_a_ratio()
    {
        SubTiles position = SubTiles.FromTiles(new Tiles(4));

        Assert.Equal(SubTiles.FromTiles(new Tiles(8)), position * 2);

        // A ratio with a power-of-two denominator is exact in Q16.16, so this one is clean.
        Assert.Equal(SubTiles.FromTiles(new Tiles(1)), position * Ratio.FromFraction(1, 4));
    }

    /// <summary>
    /// <b>Floor rounding compounds, and this is the shape it compounds in.</b> One third has no
    /// Q16.16 representation, so <c>Ratio.FromFraction(1, 3)</c> floors to just under a third and
    /// three of them come to just under one — the result is one representable step low, not equal.
    /// </summary>
    /// <remarks>
    /// Asserted rather than avoided because the loss is always downward, never upward, and that is
    /// what makes it safe here and dangerous elsewhere. A position that drifts a ten-thousandth of a
    /// Tile short is nothing. <b>A conserved quantity split three ways by this arithmetic loses
    /// units every time</b>, and CLAUDE.md's definition of done requires Goods to be conserved — so
    /// whatever splits a Bin must reconcile the remainder rather than scale each share
    /// independently. Recorded in plans/0002; it belongs to the Rule engine, not here.
    /// </remarks>
    [Fact]
    public void Scaling_by_a_non_representable_ratio_loses_one_step_downward()
    {
        SubTiles position = SubTiles.FromTiles(new Tiles(3));
        SubTiles third = position * Ratio.FromFraction(1, 3);

        SubTiles exact = SubTiles.FromTiles(new Tiles(1));
        Assert.Equal(exact - new SubTiles(1), third);
        Assert.True(third < exact);
    }

    [Fact]
    public void SubTiles_floors_to_the_containing_Tile()
    {
        SubTiles justInside = SubTiles.FromTiles(new Tiles(2)) - new SubTiles(1);
        SubTiles justBelowZero = new(-1);

        Assert.Equal(new Tiles(1), justInside.ToTilesFloor());
        Assert.Equal(new Tiles(-1), justBelowZero.ToTilesFloor());
    }

    /// <summary>
    /// <b>A range authored in metres becomes Tiles rounded <em>up</em>, because a range is a
    /// reach.</b>
    /// </summary>
    /// <remarks>
    /// <c>CellGrid.FromMetres</c>' rule at the finer unit, and the direction is the whole content:
    /// rounding down silently shortens something that was defended from reality, and the truncation
    /// is invisible in whatever the reach produced — a plume that does not carry, a Parking Shed that
    /// cannot see the Car Park at its own stated edge. <b>Metres are an authoring unit and never a
    /// stored one</b>, so this conversion runs once at load and nothing downstream holds a metre.
    /// </remarks>
    [Fact]
    public void Tiles_convert_from_metres_by_rounding_up()
    {
        Assert.Equal(new Tiles(1), Tiles.FromMetres(Tiles.Metres));
        Assert.Equal(new Tiles(100), Tiles.FromMetres(400));

        // Every metre past a boundary buys a whole Tile, and the last one before the next boundary
        // buys nothing more -- which is the assertion that fails if somebody makes this round to
        // nearest for tidiness.
        Assert.Equal(new Tiles(101), Tiles.FromMetres(401));
        Assert.Equal(new Tiles(101), Tiles.FromMetres(404));
        Assert.Equal(new Tiles(102), Tiles.FromMetres(405));

        // A range smaller than a Tile is still a range.
        Assert.Equal(new Tiles(1), Tiles.FromMetres(1));
        Assert.Equal(Tiles.Zero, Tiles.FromMetres(0));
    }

    [Fact]
    public void Quantities_order_by_their_representation()
    {
        Assert.True(new Money(1) < new Money(2));
        Assert.True(new Ticks(2) > new Ticks(1));
        Assert.True(new Tiles(-1) < Tiles.Zero);
        Assert.True(Ratio.FromFraction(1, 4) < Ratio.FromFraction(1, 3));
        Assert.True(new SubTiles(1) >= new SubTiles(1));
    }

    [Fact]
    public void Tiles_magnitude_discards_direction() =>
        Assert.Equal(new Tiles(5), new Tiles(-5).Magnitude);
}
