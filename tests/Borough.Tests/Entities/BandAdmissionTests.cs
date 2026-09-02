using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// The density band biting — <c>adr/0025</c>'s cap taking a permission away.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>plans/0053</c> step 2, second half.</b> The first half recorded a band on every block and
/// nothing read it, which is a state worth having tests for exactly once: ***a value nothing reads is
/// indistinguishable from a value nothing writes***, and only the day something reads it does the
/// difference become checkable.
/// </para>
/// <para>
/// <b>The claim under test is one sentence of <c>adr/0025</c></b> — <em>a band expresses itself as
/// which kinds a Lot permits</em> — so every assertion here is about a permission set and none is
/// about a Building count. A count would be a second thing: whether the Rule fired.
/// </para>
/// </remarks>
public sealed class BandAdmissionTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static World Populated(string file, int citizens)
    {
        var world = new World(citizens, Shipped(file));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>The band over a Lot, read the way <c>World.BandAdmitting</c> finds it.</summary>
    private static byte BandOver(World world, int lot)
    {
        if (!Frontage.BlockOf(
                world.Roads.Streets, world.Lots.East[lot], world.Lots.North[lot],
                (StreetSide)world.Lots.Side[lot], out int column, out int row)
            || !world.BlockIndex.Contains(column, row))
        {
            return 0;
        }

        int slot = world.BlockIndex.Slot(column, row);

        return slot == BlockResidency.NotResident ? (byte)0 : world.Blocks.Band[slot];
    }

    /// <summary>
    /// 🔴 <b>A world with no <c>[[band]]</c> admits exactly what it admitted before bands existed.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is why step 2 moved no State Hash</b>, stated as an assertion rather than left to the
    /// golden baselines to imply. Every absence is permissive — see
    /// <see cref="World.BandAdmitting"/> — and a bandless world is nothing but absences.
    /// </remarks>
    [Fact]
    public void A_bandless_world_admits_everything_on_every_lot()
    {
        World world = Populated("minimal.toml", 1_000);

        int checkedLots = 0;

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot))
            {
                continue;
            }

            Assert.Equal(ushort.MaxValue, world.BandAdmitting(lot));
            checkedLots++;
        }

        Assert.NotEqual(0, checkedLots);
    }

    /// <summary>
    /// 🔴 <b>The cap takes a permission the player painted, which is the whole mechanism.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The Lot carries the Trade bit and the band refuses it anyway.</b> That is the case worth
    /// naming: land refused because it was never zoned proves nothing, because nothing zoned it. What
    /// <c>adr/0025</c> asks for is a <em>cap</em> — a Lot that is zoned for trade, on a block whose
    /// band does not run to trade, where the intersection is empty and the zone bit stands untouched.
    /// </para>
    /// <para>
    /// ⚠ <b>It asserts that BOTH cases exist in this world</b>, and the second one is the guard that
    /// matters: a `BandAdmitting` hard-wired to return zero would satisfy the first assertion on its
    /// own.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_suburban_band_refuses_a_trade_bit_the_lot_carries()
    {
        World world = Populated("banded.toml", 4_000);

        int refused = 0;
        int kept = 0;

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot) || (world.Lots.Zone[lot] & LotTable.Trade) == 0)
            {
                continue;
            }

            bool admitted = (world.BandAdmitting(lot) & LotTable.Trade) != 0;

            if (admitted)
            {
                kept++;
            }
            else
            {
                refused++;

                // The zone bit is untouched. A cap refuses; it does not repaint, and a player who
                // widens the band later gets the Lot back with the permission they gave it.
                Assert.NotEqual(0, world.Lots.Zone[lot] & LotTable.Trade);
            }
        }

        Assert.True(refused > 0, "no Lot zoned for trade was refused by its band.");
        Assert.True(kept > 0, "every Lot zoned for trade was refused, so the cap is not a cap.");
    }

    /// <summary>
    /// <b>The mask a Lot gets is its own block's band and not a neighbour's</b>, which is what
    /// <c>Frontage.BlockOf</c>'s side term exists for.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Two blocks share every face line, so a position alone cannot say which block a Lot
    /// belongs to.</b> Without the side term half the Lots on the lattice would take the band of the
    /// block across the street — an off-by-one that would be invisible in a world whose bands happened
    /// to be uniform, and wrong everywhere in one whose bands are not.
    /// </remarks>
    [Fact]
    public void A_lots_mask_is_its_own_blocks_band()
    {
        World world = Populated("banded.toml", 4_000);

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot))
            {
                continue;
            }

            Assert.Equal(
                world.Rules.Band(BandOver(world, lot)).Admits,
                world.BandAdmitting(lot));
        }
    }

    /// <summary>
    /// 🔴 <b>Every Lot the subdivider carved for a block resolves back to that block.</b>
    /// </summary>
    /// <remarks>
    /// <b><c>Frontage.BlockOf</c> is the inverse of <c>LotSubdivider</c>'s four faces, and this is the
    /// round trip.</b> It is the one test that would fail if either half moved: the subdivider's
    /// choice of which side of a face belongs to a block, or the inverse's reading of it. ⚠ <b>A Lot
    /// carved by the fallback path fronts nothing and is excluded</b> — there is no lattice under it
    /// to invert.
    /// </remarks>
    [Fact]
    public void Every_carved_lot_resolves_back_to_a_block_that_exists()
    {
        World world = Populated("banded.toml", 4_000);

        int resolved = 0;

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot) || world.Lots.FrontageSlot[lot] == 0)
            {
                continue;
            }

            Assert.True(
                Frontage.BlockOf(
                    world.Roads.Streets, world.Lots.East[lot], world.Lots.North[lot],
                    (StreetSide)world.Lots.Side[lot], out int column, out int row),
                $"Lot {lot} fronts a Segment and belongs to no block.");

            Assert.True(
                world.BlockIndex.Contains(column, row),
                $"Lot {lot} resolves to ({column}, {row}), which is off the lattice.");

            Assert.NotEqual(BlockResidency.NotResident, world.BlockIndex.Slot(column, row));

            resolved++;
        }

        Assert.NotEqual(0, resolved);
    }

    /// <summary>
    /// <b>The Zone Rule's admission carries the band term</b>, which is the site the rest of this
    /// exists to protect.
    /// </summary>
    /// <remarks>
    /// <b>Read through the engine rather than around it.</b> Every assertion above is about
    /// <c>World.BandAdmitting</c>; this one is about the predicate that calls it, and it is the
    /// difference between a mechanism that works and a mechanism that is wired in.
    /// </remarks>
    [Fact]
    public void A_trade_rule_is_refused_on_a_suburban_block()
    {
        World world = Populated("banded.toml", 4_000);

        ZoneRuleDefinition trade = default;
        bool found = false;

        foreach (ZoneRuleDefinition rule in world.Rules.ZoneRules)
        {
            if (rule.Zone == 1)
            {
                trade = rule;
                found = true;
            }
        }

        Assert.True(found, "banded.toml declares no Zone Rule on bit 1.");

        int refused = 0;
        int admitted = 0;

        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot) || (world.Lots.Zone[lot] & LotTable.Trade) == 0)
            {
                continue;
            }

            bool passes = (world.Lots.Zone[lot] & trade.Admits & world.BandAdmitting(lot)) != 0;

            if (passes)
            {
                admitted++;
            }
            else
            {
                refused++;
            }
        }

        Assert.True(refused > 0, "the trade Rule is admitted on every Lot, so the band does nothing.");
        Assert.True(admitted > 0, "the trade Rule is refused everywhere, so the band is not a cap.");
    }
}
