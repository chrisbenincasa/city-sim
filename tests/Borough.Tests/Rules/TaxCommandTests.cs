using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0072</c> D3 — <b>the four controls the player has over the Citizen income tax</b>, and
/// the verb that moves them.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>Govern</c> is the model at every step and this is not a case of it.</b> A Policy holds one
/// amount and is addressed by declaration position; an income tax holds four numbers that constrain
/// each other, kept as a <em>schedule per Day</em> because a late wage is taxed at the Day it was
/// earned. <see cref="GovernTests"/> is the sibling file.
/// </para>
/// <para>
/// 🔴 <b>Two claims here are the design and the rest is plumbing.</b> The first is that a change
/// applies from <em>tomorrow</em> — D6, and the reason the table is a ring rather than four fields.
/// The second is that a refusal is composed against the schedule the player is <em>building</em>,
/// which is what makes the four controls settable in any order: tested against today's schedule, a
/// city moving from 10/20 to 30/40 could not make the first of the two moves.
/// </para>
/// </remarks>
public sealed class TaxCommandTests
{
    private const int Seed = 20_260_910;

    /// <summary>What the Ruleset authors, and what every test below varies one number of.</summary>
    private static readonly IncomeTaxSchedule Authored = new(50, 200, 10, 20);

    // ---- the four controls ----------------------------------------------------------------------

    /// <summary>The allowance moves and nothing else does.</summary>
    [Fact]
    public void The_allowance_control_sets_the_allowance_alone()
    {
        IncomeTaxSchedule after = Pending(Command.Tax(TaxControl.Allowance, 60));

        Assert.Equal(new IncomeTaxSchedule(60, 200, 10, 20), after);
    }

    /// <summary>The upper threshold moves and nothing else does.</summary>
    [Fact]
    public void The_threshold_control_sets_the_threshold_alone()
    {
        IncomeTaxSchedule after = Pending(Command.Tax(TaxControl.UpperThreshold, 300));

        Assert.Equal(new IncomeTaxSchedule(50, 300, 10, 20), after);
    }

    /// <summary>The middle rate moves and nothing else does.</summary>
    [Fact]
    public void The_middle_rate_control_sets_the_middle_rate_alone()
    {
        IncomeTaxSchedule after = Pending(Command.Tax(TaxControl.MiddleRate, 15));

        Assert.Equal(new IncomeTaxSchedule(50, 200, 15, 20), after);
    }

    /// <summary>The upper rate moves and nothing else does.</summary>
    [Fact]
    public void The_upper_rate_control_sets_the_upper_rate_alone()
    {
        IncomeTaxSchedule after = Pending(Command.Tax(TaxControl.UpperRate, 30));

        Assert.Equal(new IncomeTaxSchedule(50, 200, 10, 30), after);
    }

    // ---- D6: prospectively, and never over a Day already being earned ---------------------------

    /// <summary>
    /// 🔴 <b>A change reaches tomorrow and leaves today exactly as it was</b> — <c>plans/0072</c> D6.
    /// </summary>
    /// <remarks>
    /// <b>Without this the ring is pointless</b> and four fields on the world would do. A rate moved
    /// at noon that repriced the morning behind it would be asking an employer for back a withholding
    /// already paid over, which is not a thing any part of the build can do.
    /// </remarks>
    [Fact]
    public void A_change_governs_tomorrow_and_not_today()
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, Command.Tax(TaxControl.Allowance, 60));

        Assert.Equal(Authored, world.IncomeTaxRates.ScheduleFor(0, Authored));
        Assert.Equal(
            new IncomeTaxSchedule(60, 200, 10, 20), world.IncomeTaxRates.ScheduleFor(1, Authored));
    }

    /// <summary>
    /// And the Day it lands on is the Tick's own Day plus one, rather than always Day 1.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The arithmetic is <c>WageEngine.Sweep</c>'s expression and has to stay it.</b> A verb
    /// that computed a different Day from the same Tick would write a schedule the withholding never
    /// reads, and every assertion at Tick 0 above would still pass.
    /// </remarks>
    [Fact]
    public void The_day_it_governs_is_the_day_the_command_was_issued_plus_one()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, (3 * Ticks.PerDay) + 100);
        Issue(simulation, Command.Tax(TaxControl.Allowance, 60));

        Assert.Equal(Authored, world.IncomeTaxRates.ScheduleFor(3, Authored));
        Assert.Equal(
            new IncomeTaxSchedule(60, 200, 10, 20), world.IncomeTaxRates.ScheduleFor(4, Authored));
    }

    /// <summary>
    /// 🔴 <b>Four controls set on one Day leave ONE ring entry</b>, because
    /// <see cref="IncomeTaxTable.Govern"/> is indexed by the Day and replaces rather than appends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what makes four commands a whole schedule rather than four partial ones.</b> Each
    /// reads the pending schedule back out, replaces the one field it names, and writes the lot
    /// again — so the history stays exactly as deep as the wage window it has to cover however many
    /// times a player moves a rate.
    /// </para>
    /// <para>
    /// ⚠ <b>The order is not arbitrary and that is D7 rather than an implementation detail.</b> The
    /// upper rate is raised before the middle one, because a middle rate above the upper rate is
    /// refused whichever end the player moved. ***A schedule is reached through states that are
    /// themselves legal.***
    /// </para>
    /// </remarks>
    [Fact]
    public void Four_controls_on_one_day_leave_one_entry()
    {
        (World world, Simulation simulation) = City();

        Issue(
            simulation,
            Command.Tax(TaxControl.Allowance, 100),
            Command.Tax(TaxControl.UpperThreshold, 400),
            Command.Tax(TaxControl.UpperRate, 40),
            Command.Tax(TaxControl.MiddleRate, 30));

        Assert.Equal(1, Stamped(world));
        Assert.Equal(
            new IncomeTaxSchedule(100, 400, 30, 40), world.IncomeTaxRates.ScheduleFor(1, Authored));
    }

    /// <summary>
    /// 🔴 <b>The refusals read the PENDING schedule, so the pair above is reachable in one Day.</b>
    /// </summary>
    /// <remarks>
    /// <b>The defect this is written against</b>: check the new value against the schedule in force
    /// today and the second command sees an upper rate of 20 that the first one already replaced, so
    /// a middle rate of 30 is refused and the player can never leave 10/20 at all.
    /// </remarks>
    [Fact]
    public void The_second_of_a_pair_is_tested_against_what_the_first_one_set()
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, Command.Tax(TaxControl.UpperRate, 40));
        Issue(simulation, Command.Tax(TaxControl.MiddleRate, 30));

        Assert.Equal(
            new IncomeTaxSchedule(50, 200, 30, 40), world.IncomeTaxRates.ScheduleFor(1, Authored));
    }

    // ---- the refusals ---------------------------------------------------------------------------

    /// <summary>A control outside the four is a command with no subject.</summary>
    [Fact]
    public void A_control_that_is_not_one_of_the_four_is_refused()
    {
        Assert.Equal(
            Refusal.TaxControlNotDeclared,
            Refuses(new Command(CommandKind.Tax, new Tiles(10), default, zone: 9)));
    }

    /// <summary>A marginal rate is a percentage, and it is refused rather than clamped.</summary>
    [Theory]
    [InlineData(TaxControl.MiddleRate, -1)]
    [InlineData(TaxControl.MiddleRate, 101)]
    [InlineData(TaxControl.UpperRate, -1)]
    [InlineData(TaxControl.UpperRate, 101)]
    public void A_rate_outside_zero_to_a_hundred_is_refused(TaxControl control, int value)
    {
        Assert.Equal(Refusal.TaxRateOutOfRange, Refuses(Command.Tax(control, value)));
    }

    /// <summary>A negative allowance is a threshold no Day can be on the wrong side of.</summary>
    [Fact]
    public void A_negative_allowance_is_refused()
    {
        Assert.Equal(
            Refusal.TaxAllowanceIsNegative, Refuses(Command.Tax(TaxControl.Allowance, -1)));
    }

    /// <summary>
    /// 🔴 <b>An upper rate below the middle one is refused from EITHER end</b> —
    /// <c>plans/0072</c> D7.
    /// </summary>
    /// <remarks>
    /// Both rates are marginal, so take-home income would step downward at the threshold: a Citizen
    /// who earned a pound more would keep less. ⚠ <b>The two cases are one rule and not two</b> —
    /// what is checked is the pair that would result, so it does not matter which end moved.
    /// </remarks>
    [Theory]
    [InlineData(TaxControl.UpperRate, 5)]
    [InlineData(TaxControl.MiddleRate, 25)]
    public void A_rate_that_falls_as_earnings_rise_is_refused(TaxControl control, int value)
    {
        Assert.Equal(
            Refusal.TaxUpperRateBelowMiddleRate, Refuses(Command.Tax(control, value)));
    }

    /// <summary>A band that opens before taxation does is refused, from either end.</summary>
    [Theory]
    [InlineData(TaxControl.UpperThreshold, 20)]
    [InlineData(TaxControl.Allowance, 500)]
    public void An_upper_band_starting_below_the_allowance_is_refused(TaxControl control, int value)
    {
        Assert.Equal(
            Refusal.TaxUpperThresholdBelowAllowance, Refuses(Command.Tax(control, value)));
    }

    /// <summary>The applier throws on what the query refuses, which is the guard being one rule.</summary>
    [Fact]
    public void A_refused_command_throws_out_of_phase_zero()
    {
        (World world, Simulation simulation) = City();

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(
            () => Issue(simulation, Command.Tax(TaxControl.UpperRate, 5)));

        Assert.Contains("DOWNWARD", refused.Message, StringComparison.Ordinal);
        Assert.Equal(0, Stamped(world));
    }

    // ---- the log ---------------------------------------------------------------------------------

    /// <summary>
    /// <b>A <c>tax</c> line survives the round trip with its control and its value</b>, not merely
    /// with its verb.
    /// </summary>
    /// <remarks>
    /// <b>A verb the simulation applies and the codec cannot spell is a session that cannot be
    /// reported</b>, and a log is what a crash artifact is made of. ⚠ <b>The control rides in the
    /// zone word and the value in <c>East</c></b>, so a codec that carried only the Tile pair would
    /// round-trip every verb, keep every hash it had, and silently turn every tax edit into a move of
    /// the allowance to zero.
    /// </remarks>
    [Theory]
    [InlineData(TaxControl.Allowance, 60)]
    [InlineData(TaxControl.UpperThreshold, 300)]
    [InlineData(TaxControl.MiddleRate, 15)]
    [InlineData(TaxControl.UpperRate, 30)]
    public void A_tax_command_survives_the_input_log_round_trip(TaxControl control, int value)
    {
        InputLogBuilder builder = new(1, new WorldConfiguration(8), rulesetHash: 0);
        builder.Append(new Ticks(7), Command.Tax(control, value));

        InputLog restored = InputLogCodec.FromText(InputLogCodec.ToText(builder.Build()));
        Command command = restored.Entry(0).Command;

        Assert.Equal(CommandKind.Tax, command.Kind);
        Assert.Equal((ushort)control, command.Zone);
        Assert.Equal(new Tiles(value), command.East);
    }

    // ---- the world --------------------------------------------------------------------------------

    /// <summary>What the schedule for tomorrow becomes once these commands have applied.</summary>
    private static IncomeTaxSchedule Pending(params Command[] commands)
    {
        (World world, Simulation simulation) = City();

        Issue(simulation, commands);

        return world.IncomeTaxRates.ScheduleFor(1, Authored);
    }

    /// <summary>Why this command would not apply in a freshly authored city.</summary>
    private static Refusal Refuses(Command command)
    {
        (World _, Simulation simulation) = City();

        return simulation.Refuses(command);
    }

    private static void Issue(Simulation simulation, params Command[] commands) =>
        simulation.Step(new TickInput(commands, 0));

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>How many rows of the ring hold a schedule at all.</summary>
    private static int Stamped(World world)
    {
        int stamped = 0;

        for (int slot = 0; slot < IncomeTaxTable.Retained; slot++)
        {
            if (world.IncomeTaxRates.Stamped[slot] != 0)
            {
                stamped++;
            }
        }

        return stamped;
    }

    private static (World World, Simulation Simulation) City()
    {
        RulesetLoadResult result = RulesetLoader.Parse(Taxed, "test.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(Seed);
        var world = new World(64, result.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        Assert.Equal(Authored, world.Rules.IncomeTax);

        return (world, simulation);
    }

    /// <summary>
    /// A city that authors an income tax and nothing else of note. <b>Deliberately unpopulated</b>:
    /// this class asserts what the verb writes into the schedule table, and a synthetic city would
    /// add several thousand Citizens to every case without changing one assertion.
    /// </summary>
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
        """;
}
