using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Rules;

/// <summary>
/// A Business reads what its own premises emitted — <c>plans/0072</c> D9 and D29.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>Without this the design's own worked example is unauthorable.</b> A Building holds the
/// emission because <c>RuleEngine.Emit</c> attributes a firing to the Building the Rule Instance
/// stands in — but a <c>[[policy]]</c> cannot sweep Buildings. There is no predicate that selects a
/// Building population and the loader refuses <c>sweeps = "building"</c> by name, so a
/// Building-only Emission Readout is one nothing could ever be charged on. ***A charge on pollution
/// would have had to be a charge on a balance***, which is a wealth tax wearing a charge's name.
/// </para>
/// <para>
/// ⚠ <b>A Household is deliberately not admitted.</b> A dwelling emits and its occupant did not
/// decide to, in any sense this design has settled.
/// </para>
/// </remarks>
public sealed class BusinessEmissionReadoutTests
{
    private static readonly ReadoutId Emission = new((ushort)Readout.Emission);

    [Fact]
    public void A_business_is_a_scope_this_readout_can_be_read_against()
    {
        Assert.True(Readouts.IsReadableAgainst(Emission, ReadoutScope.Business));
        Assert.True(Readouts.IsReadableAgainst(Emission, ReadoutScope.Building));
    }

    /// <summary>A dwelling's emission is not its occupant's doing.</summary>
    [Fact]
    public void A_household_is_not()
    {
        Assert.False(Readouts.IsReadableAgainst(Emission, ReadoutScope.Household));
    }

    [Fact]
    public void A_business_reads_what_its_premises_emitted_over_the_previous_whole_day()
    {
        (World world, int building, int business) = Tenanted();

        world.Buildings.Emit(building, 4, 700);

        Advance(world, day: 5);

        Assert.Equal(700, Readouts.ReadBusiness(world, business, Emission));
    }

    /// <summary>The Day being accumulated is not the Day being charged.</summary>
    [Fact]
    public void A_business_does_not_read_the_day_still_being_emitted()
    {
        (World world, int building, int business) = Tenanted();

        world.Buildings.Emit(building, 4, 700);

        Advance(world, day: 4);

        Assert.Equal(0, Readouts.ReadBusiness(world, business, Emission));
    }

    /// <summary>
    /// A trade in the Unplaced Pool holds no Building, has fired no Rule and has emitted nothing.
    /// </summary>
    [Fact]
    public void An_unpremised_business_reads_zero_rather_than_throwing()
    {
        (World world, int _, int _) = Tenanted();

        Handle<Business> homeless = world.CreateBusiness(default);

        Advance(world, day: 5);

        Assert.Equal(
            0, Readouts.ReadBusiness(world, world.Businesses.Rows.Resolve(homeless), Emission));
    }

    /// <summary>Two trades in two Buildings are charged for their own emission and not each other's.</summary>
    [Fact]
    public void Two_businesses_read_their_own_premises_and_not_each_others()
    {
        (World world, int first, int firstTrade) = Tenanted();

        Handle<Lot> lot = world.Lots.Create(new Tiles(9), new Tiles(9), zone: 1);
        Handle<Building> second = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Business> secondTrade = world.CreateBusiness(second);

        world.Buildings.Emit(first, 4, 700);
        world.Buildings.Emit(world.Buildings.Rows.Resolve(second), 4, 25);

        Advance(world, day: 5);

        Assert.Equal(700, Readouts.ReadBusiness(world, firstTrade, Emission));
        Assert.Equal(
            25, Readouts.ReadBusiness(world, world.Businesses.Rows.Resolve(secondTrade), Emission));
    }

    // ---- the fixture ----------------------------------------------------------------------------

    private static void Advance(World world, int day)
    {
        while (world.Tick.Raw < (ulong)(day * Ticks.PerDay))
        {
            world.Advance();
        }
    }

    private static (World World, int Building, int Business) Tenanted()
    {
        var world = new World(
            1_000,
            new Ruleset(
                resources: [ResourceFamily.Good],
                rules: [],
                kinds: [new KindDefinition(0, 0, 0, 0)],
                inputs: [],
                outputs: [],
                emissions: [],
                bins: [],
                kindRules: [],
                zoneRules: []));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Business> business = world.CreateBusiness(building);

        return (
            world,
            world.Buildings.Rows.Resolve(building),
            world.Businesses.Rows.Resolve(business));
    }
}
