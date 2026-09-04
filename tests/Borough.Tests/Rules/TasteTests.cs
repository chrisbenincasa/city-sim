using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0045</c> row 9: a Household acquires something to want, and placement acquires something
/// to do with it. <c>adr/0027</c>.
/// </summary>
public sealed class TasteTests
{
    private const int Citizens = 2_000;

    /// <summary>A population at which a preference about where to live is measurable at all.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 ⚠ <b>THIS RAN AT 2,000 CITIZENS UNTIL 2026-09-01, WHERE THE EFFECT IS NOISE.</b> The gap
    /// between the two groups, swept over five seeds at 2,000: <b>−11, +2, +10, −5, −1</b>. It is
    /// centred on zero, and the committed reading of −11 was <i>one draw from it</i>. ***The test
    /// passed because seed 0 was lucky***, and a change to the Lot supply moved which seed was.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>AND IT THEN RAN AT 12,000, WHERE SEED 0 WAS LUCKY AGAIN.</b> <c>plans/0060</c>'s
    /// change to the parcel geometry moved it and the same failure returned three days later.
    /// Re-swept over five seeds, <c>roomMean − centreMean</c> with the sham gap beside it:
    /// </para>
    /// <list type="table">
    /// <item><term>12,000</term><description><b>−6</b>, +3, +11, +3, +8 — sham 2, 4, 6, 11, 10.
    /// ***Seed 0 is the one negative reading in the row.***</description></item>
    /// <item><term>20,000</term><description>+3, +1, +13, +7, +13 — sham 11, 3, 1, 4, 4. Right sign
    /// throughout, and two of the five are inside their own sham.</description></item>
    /// <item><term>32,000</term><description><b>+13, +14, +16, +15, +12</b> — sham 6, 6, 2, 8, 6.
    /// ✅ <b>Every seed positive, every seed clear of its sham.</b></description></item>
    /// </list>
    /// <para>
    /// ⚠ <b>The reason is the SAMPLE and not the taste</b> — a Household compares the three Lots it
    /// was shown (<c>adr/0017</c> refusing an optimiser), and in a small city three Lots barely
    /// differ in centrality. The preference has to have something to prefer, and how much there is
    /// to prefer scales with the city. ⚠ <b>A single-seed sign test on a quantity this size is what
    /// failed twice</b>; the sham comparison below is the part that makes one seed defensible, so
    /// read the two assertions as one instrument.
    /// </para>
    /// <para>
    /// ⚠ <b>The 161-against-172 in <c>rulesets/choosy.toml</c>'s header was taken at 2,000</b> and is
    /// one sample from the zero-centred distribution above. It is not a measurement of the taste.
    /// </para>
    /// </remarks>
    private const int TasteIsMeasurable = 32_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(0);

    [Fact]
    public void A_world_that_authors_no_opinion_has_none()
    {
        Ruleset rules = Load("aged.toml");

        Assert.False(rules.CentralityVaries);
        Assert.Equal(Ruleset.CentralityNeutral, rules.CentralityTaste(Key, 1UL, 1));
    }

    [Fact]
    public void A_world_that_authors_one_has_one()
    {
        Ruleset rules = Load("choosy.toml");

        Assert.True(rules.CentralityVaries);
    }

    [Fact]
    public void A_stage_with_a_width_spreads_its_households_across_it()
    {
        Ruleset rules = Load("choosy.toml");

        int low = int.MaxValue;
        int high = int.MinValue;

        for (ulong id = 1; id <= 500; id++)
        {
            int taste = rules.CentralityTaste(Key, id, 5);

            low = taste < low ? taste : low;
            high = taste > high ? taste : high;
        }

        // empty_nest is authored 30..90, the widest band in the file.
        Assert.True(low < Ruleset.CentralityNeutral, $"lowest taste was {low}");
        Assert.True(high > Ruleset.CentralityNeutral, $"highest taste was {high}");
    }

    [Fact]
    public void The_pool_is_reached_at_all()
    {
        World world = new(Citizens, Load("choosy.toml"), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int deepest = 0;

        for (ulong tick = 0; tick < 20_480; tick++)
        {
            simulation.Step(default);

            int waiting = world.UnplacedPool.Rows.LiveCount;
            deepest = waiting > deepest ? waiting : deepest;
        }

        Assert.True(
            deepest > 0,
            "the Unplaced Pool was empty on every one of 20,480 Ticks, so TryHouse never ran and "
                + "no taste could have been consulted whatever the Ruleset says.");
    }

    [Fact]
    public void A_household_that_wants_the_centre_ends_up_nearer_it()
    {
        (World world, _) = Run("choosy.toml", 20_480, TasteIsMeasurable);
        Ruleset rules = world.Rules;

        long centreWalk = 0;
        int centreCount = 0;
        long roomWalk = 0;
        int roomCount = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot))
            {
                continue;
            }

            if (!world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out int building))
            {
                continue;
            }

            if (!world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
            {
                continue;
            }

            int taste = rules.CentralityTaste(
                Key, world.Households.Rows.IdAt(slot), world.Households.LifeStage[slot]);

            long walked = Walk(world, lot);

            if (taste > Ruleset.CentralityNeutral)
            {
                centreWalk += walked;
                centreCount++;
            }
            else if (taste < Ruleset.CentralityNeutral)
            {
                roomWalk += walked;
                roomCount++;
            }
        }

        Assert.True(centreCount > 0 && roomCount > 0, $"{centreCount} central, {roomCount} spacious");

        long centreMean = centreWalk / centreCount;
        long roomMean = roomWalk / roomCount;

        Assert.True(
            centreMean < roomMean,
            $"Households wanting the centre live {centreMean} Tiles from it on average and "
                + $"Households wanting room live {roomMean} at {TasteIsMeasurable} Citizens. The "
                + "preference is not reaching placement. ⚠ Do NOT re-site this at a smaller city to "
                + "make it pass -- at 2,000 and at 12,000 the gap is noise a lucky seed 0 sat on "
                + "top of, which is what this test used to be, twice. The five-seed band here is "
                + "+12 to +16; a reading outside it is a change to the city and not a seed.");

        // 🔴 THE PLACEBO, AND IT IS THE HALF OF THIS TEST THAT CAN FAIL FOR AN INTERESTING REASON.
        //
        // The gap above is ~11 Tiles in a city ~350 Tiles across, which is small enough that a
        // reader is owed a reason to believe it is the mechanism and not the shape of the map. So
        // the same Households are split again on a taste they do not have -- drawn from an
        // unrelated stream, over the same ids, on the same Tick -- and that split must NOT sort
        // them. ***A signal that survives a sham grouping is a property of the city, not of the
        // preference.***
        //
        // ⚠ It is asserted as a MAGNITUDE and not a direction. The sham gap is noise, so its sign
        // is a coin flip and pinning it would make this test fail on an unrelated seed change.
        long shamGap = ShamGap(world);
        long realGap = roomMean - centreMean;

        Assert.True(
            shamGap < realGap,
            $"splitting the same Households on a taste they do not have moved the mean by "
                + $"{shamGap} Tiles against the real preference's {realGap}. The sorting is not "
                + "coming from the preference.");
    }

    /// <summary>
    /// The same comparison as above, on a grouping drawn from a stream placement never consulted.
    /// </summary>
    private static long ShamGap(World world)
    {
        long nearWalk = 0;
        int nearCount = 0;
        long farWalk = 0;
        int farCount = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (!world.Households.Rows.IsLive(slot)
                || !world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out int building)
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[building], out int lot))
            {
                continue;
            }

            // A different purpose_tag over the same id: the same shape of number, carrying none of
            // the meaning. This is exactly the correlation PurposeTag's own remarks exist to stop
            // happening by accident.
            ulong sham = Randomness.Draw(
                Key, world.Households.Rows.IdAt(slot), Ticks.Zero, PurposeTag.CarOwnership) % 2UL;

            long walked = Walk(world, lot);

            if (sham == 0)
            {
                nearWalk += walked;
                nearCount++;
            }
            else
            {
                farWalk += walked;
                farCount++;
            }
        }

        if (nearCount == 0 || farCount == 0)
        {
            return 0;
        }

        long difference = (nearWalk / nearCount) - (farWalk / farCount);

        return difference < 0 ? -difference : difference;
    }

    /// <summary>Manhattan Tiles from a Lot to the nearest authored lattice origin.</summary>
    private static long Walk(World world, int lot)
    {
        LatticeDefinition[] lattices = world.Rules.Lattices;
        long east = world.Lots.East[lot].Raw;
        long north = world.Lots.North[lot].Raw;
        long nearest = long.MaxValue;

        if (lattices.Length == 0)
        {
            return (east < 0 ? -east : east) + (north < 0 ? -north : north);
        }

        foreach (LatticeDefinition lattice in lattices)
        {
            long sideways = east - lattice.OriginEastTiles;
            long up = north - lattice.OriginNorthTiles;
            long walked = (sideways < 0 ? -sideways : sideways) + (up < 0 ? -up : up);

            nearest = walked < nearest ? walked : nearest;
        }

        return nearest;
    }

    private static int Placed(World world)
    {
        int housed = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot) && world.Households.Dwelling[slot] != default)
            {
                housed++;
            }
        }

        return housed;
    }

    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    private static (World World, Simulation Simulation) Run(
        string file, ulong ticks, int citizens = Citizens)
    {
        World world = new(citizens, Load(file), Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        for (ulong tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }

        return (world, simulation);
    }
}
