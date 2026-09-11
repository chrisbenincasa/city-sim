using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0072</c> D8 — <b>the three controls the player has over the Business profit tax</b>,
/// and the one verb they share with the four earnings controls.
/// </summary>
/// <remarks>
/// <para>
/// <b>The two schedules share <c>CommandKind.Tax</c> and nothing else.</b> A Citizen pays on what
/// they earned in a Day; a Business pays on what it made in one. <see cref="TaxCommandTests"/> is
/// the sibling file and carries the earnings half.
/// </para>
/// <para>
/// 🔴 <b>The claim worth the file is that governing one schedule leaves the other alone.</b> Both
/// live in one ring, one row per effective Day, so a shared stamp would mean that a player nudging
/// an earnings rate set every profit band to zero on the way past — no refusal and no diagnostic,
/// just a tax that stopped collecting. <c>TaxScheduleRingTests</c> holds that at the table; this
/// file holds it through the verb.
/// </para>
/// <para>
/// ⚠ <b>One asymmetry between the two schedules is real and is not an oversight.</b> A negative
/// allowance and a negative profit threshold are refused for different reasons: earnings cannot be
/// negative, so the allowance's floor is the quantity's floor, while profit ranges below zero and
/// its band boundary still does not. ***A quantity's range is not its boundary's range.***
/// </para>
/// </remarks>
public sealed class ProfitTaxCommandTests
{
    private const int Seed = 20_260_910;

    /// <summary>What the Ruleset authors, and what every test below varies one number of.</summary>
    private static readonly BusinessTaxSchedule Authored = new(400, 15, 30);

    private static readonly IncomeTaxSchedule AuthoredEarnings = new(50, 200, 10, 20);

    [Fact]
    public void The_threshold_control_sets_the_threshold_alone()
    {
        Assert.Equal(
            new BusinessTaxSchedule(900, 15, 30),
            Pending(Command.Tax(TaxControl.ProfitThreshold, 900)));
    }

    [Fact]
    public void The_lower_rate_control_sets_the_lower_rate_alone()
    {
        Assert.Equal(
            new BusinessTaxSchedule(400, 25, 30),
            Pending(Command.Tax(TaxControl.ProfitLowerRate, 25)));
    }

    [Fact]
    public void The_upper_rate_control_sets_the_upper_rate_alone()
    {
        Assert.Equal(
            new BusinessTaxSchedule(400, 15, 45),
            Pending(Command.Tax(TaxControl.ProfitUpperRate, 45)));
    }

    /// <summary>A zero lower rate is how the schedule spells a tax-free band.</summary>
    /// <remarks>
    /// D8: there is no separate tax-free band and no control for one. Setting the lower rate to
    /// zero gives one, and the threshold then acts as the allowance.
    /// </remarks>
    [Fact]
    public void A_lower_rate_of_zero_is_accepted_and_is_how_a_tax_free_band_is_spelled()
    {
        BusinessTaxSchedule after = Pending(Command.Tax(TaxControl.ProfitLowerRate, 0));

        Assert.Equal(0, after.LowerRatePercent);
        Assert.Equal(0, BusinessTax.DueOn(399, after));
        Assert.Equal(30, BusinessTax.DueOn(500, after));
    }

    // ---- the refusals ---------------------------------------------------------------------------

    [Fact]
    public void A_profit_threshold_below_zero_is_refused()
    {
        Assert.Equal(
            Refusal.TaxProfitThresholdIsNegative,
            Refuses(Command.Tax(TaxControl.ProfitThreshold, -1)));
    }

    [Theory]
    [InlineData(TaxControl.ProfitLowerRate, -1)]
    [InlineData(TaxControl.ProfitLowerRate, 101)]
    [InlineData(TaxControl.ProfitUpperRate, -1)]
    [InlineData(TaxControl.ProfitUpperRate, 101)]
    public void A_profit_rate_outside_a_percentage_is_refused(TaxControl control, int value)
    {
        Assert.Equal(Refusal.TaxRateOutOfRange, Refuses(Command.Tax(control, value)));
    }

    /// <summary>The pair is checked, so the refusal fires from whichever end the player moved.</summary>
    [Theory]
    [InlineData(TaxControl.ProfitLowerRate, 40)]
    [InlineData(TaxControl.ProfitUpperRate, 10)]
    public void A_falling_marginal_rate_is_refused_from_either_end(TaxControl control, int value)
    {
        Assert.Equal(
            Refusal.TaxProfitUpperRateBelowLowerRate, Refuses(Command.Tax(control, value)));
    }

    [Fact]
    public void Equal_rates_are_accepted()
    {
        Assert.Equal(
            Refusal.None, Refuses(Command.Tax(TaxControl.ProfitUpperRate, 15)));
    }

    // ---- the two schedules do not disturb each other ---------------------------------------------

    [Fact]
    public void Governing_a_profit_band_leaves_the_authored_earnings_schedule_alone()
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, Command.Tax(TaxControl.ProfitUpperRate, 45));

        Assert.Equal(
            AuthoredEarnings, world.IncomeTaxRates.ScheduleFor(1, world.Rules.IncomeTax));
    }

    [Fact]
    public void Governing_an_earnings_band_leaves_the_authored_profit_schedule_alone()
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, Command.Tax(TaxControl.MiddleRate, 15));

        Assert.Equal(
            Authored, world.IncomeTaxRates.ProfitScheduleFor(1, world.Rules.BusinessTax));
    }

    [Fact]
    public void Both_schedules_governed_on_one_day_keep_both_changes()
    {
        (World world, Simulation simulation) = City();

        Issue(
            simulation,
            Command.Tax(TaxControl.ProfitUpperRate, 45),
            Command.Tax(TaxControl.MiddleRate, 15));

        Assert.Equal(45, world.IncomeTaxRates.ProfitScheduleFor(1, Authored).UpperRatePercent);
        Assert.Equal(
            15, world.IncomeTaxRates.ScheduleFor(1, AuthoredEarnings).MiddleRatePercent);
    }

    /// <summary>A change reaches tomorrow and leaves today alone.</summary>
    [Fact]
    public void A_change_applies_from_the_next_day_and_not_the_current_one()
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, Command.Tax(TaxControl.ProfitUpperRate, 45));

        Assert.Equal(Authored, world.IncomeTaxRates.ProfitScheduleFor(0, Authored));
        Assert.Equal(45, world.IncomeTaxRates.ProfitScheduleFor(1, Authored).UpperRatePercent);
    }

    // ---- the fixture ----------------------------------------------------------------------------

    private static BusinessTaxSchedule Pending(params Command[] commands)
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, commands);

        return world.IncomeTaxRates.ProfitScheduleFor(1, Authored);
    }

    private static Refusal Refuses(Command command)
    {
        (World _, Simulation simulation) = City();

        return simulation.Refuses(command);
    }

    private static void Issue(Simulation simulation, params Command[] commands) =>
        simulation.Step(new TickInput(commands, 0));

    private static (World World, Simulation Simulation) City()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Taxed, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(64, result.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        Assert.Equal(Authored, world.Rules.BusinessTax);
        Assert.Equal(AuthoredEarnings, world.Rules.IncomeTax);

        return (world, simulation);
    }

    private const string Taxed = """
        [[resource]]
        name = "money"
        family = "money"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        bins = [ { resource = "sundries", capacity = 48 } ]

        [roads]
        block_tiles = 32
        arterial_count = 0
        arterial_junction_tiles = 512
        foot_crossing_every = 4
        foot_paths_per_thousand_blocks = 40
        street_speed_kph = 50
        arterial_speed_kph = 90
        walk_speed_kph = 5
        street_capacity_per_hour = 3600
        arterial_capacity_per_hour = 12000
        foot_path_capacity_per_hour = 1000

        [lots]
        lots_per_segment = 5
        setback_tiles = 2

        [capacity]
        floor_tiles_per_occupant      = 6
        floor_tiles_per_job           = 1
        floor_tiles_per_parking_space = 6

        [households]
        car_ownership_percent = 0
        opening_balance_min = 0
        opening_balance_max = 1000

        [income_tax]
        allowance_per_day       = 50
        upper_threshold_per_day = 200
        middle_rate_percent     = 10
        upper_rate_percent      = 20

        [business_tax]
        threshold_per_day  = 400
        lower_rate_percent = 15
        upper_rate_percent = 30
        """;
}
