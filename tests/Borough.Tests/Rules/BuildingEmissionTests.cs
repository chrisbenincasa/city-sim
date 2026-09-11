using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// A Building's own emission: accumulated per Day, attributable to it, and readable as a Readout.
/// </summary>
/// <remarks>
/// <para>
/// <b>The thing under test is a number the Map Layer cannot answer for.</b>
/// <c>MapLayers.EmitPollution</c> adds into a Cell that many Lots share and that the operator
/// decays, so a query over the Layer can say <em>how dirty is it here</em> and can never say
/// <em>who made it that way</em>. Everything below is about the second question.
/// </para>
/// <para>
/// 🔴 <b>The half of this that is easy to get wrong is the STALE pair, not the running total.</b>
/// The Day rolls lazily on the first write of a new Day, so a Building that emits daily always looks
/// right — and a Building that emitted once a week ago has two columns full of last week's figures
/// and nothing in the row that says so. The gap cases below are the ones that would have shipped.
/// </para>
/// </remarks>
public sealed class BuildingEmissionTests
{
    private static readonly ResourceId Flour = new(1);

    private const byte Bakery = 1;
    private const uint Rate = 8;

    private static readonly ReadoutId EmissionReadout = new((ushort)Readout.Emission);

    // ---- the write site, through a real firing -------------------------------------------------------

    /// <summary>One Rule, six flour a firing, forty pollution an application, every eight Ticks.</summary>
    private static Ruleset Emitting(int max) => new(
        resources: [ResourceFamily.Good],
        rules:
        [
            new RuleDefinition(
                Bakery, Rate, ApplyCount.Band(1, max), RuleId.None, false, default,
                ConditionId.None, 0, 1, 0, 0, 0, 1),
        ],
        kinds: [new KindDefinition(0, 1, 0, 1)],
        inputs: [new Term(new BinRef(Scope.Local, Flour), 6)],
        outputs: [],
        emissions: [new MapEmission(Layer.IndustrialPollution, 40)],
        bins: [new BinDeclaration(Flour, BinCapacity.Of(60))],
        kindRules: [new RuleId(1)],
        zoneRules: []);

    private static (World World, Simulation Simulation, int Slot) Built(Ruleset ruleset)
    {
        var world = new World(1_000, ruleset);
        var simulation = new Simulation(world, WorldKey.FromSeed(1))
        {
            VerifyDecideWritesNothing = true,
        };

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, Bakery);

        foreach (BinDeclaration bin in ruleset.BinsOf(Bakery))
        {
            world.CreateBin(building, bin.Resource);
        }

        foreach (RuleId rule in ruleset.RulesOf(Bakery))
        {
            world.CreateRuleInstance(building, rule, simulation.Tick, delay: 1);
        }

        int slot = world.Buildings.Rows.Resolve(building);
        world.Deposit(world.Bins.Rows.At(world.FindBin(slot, Flour)), 60, Ticks.Zero);

        return (world, simulation, slot);
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(TickInput.Empty);
        }
    }

    /// <summary>
    /// <b>The figure recorded against the Building is the figure handed to the Map Layer</b>, for the
    /// same firing and for every application of it.
    /// </summary>
    /// <remarks>
    /// <b>Asserted as an equality between the two rather than against a literal on each side</b>,
    /// which is the only form that fails if the two ever stop being one number. A greedy Rule is used
    /// on purpose: <c>emission.Amount × applications</c> is where a per-firing recording rather than a
    /// per-application one would agree with a literal and disagree with the Layer.
    /// </remarks>
    [Fact]
    public void What_is_recorded_against_the_building_is_what_went_into_the_map_layer()
    {
        (World world, Simulation simulation, int slot) = Built(Emitting(max: 4));

        Step(simulation, 2);

        Assert.Equal(4 * 40, world.Layers.PollutionSource(Cells.Zero, Cells.Zero));
        Assert.Equal(
            world.Layers.PollutionSource(Cells.Zero, Cells.Zero),
            world.Buildings.DayEmitted[slot]);
    }

    /// <summary>
    /// <b>Emission accumulates across a Day</b> — the column is a Day's total and not the last
    /// firing's.
    /// </summary>
    /// <remarks>
    /// Two firings eight Ticks apart, both inside Day 0, because <see cref="Ticks.PerDay"/> is 2,048.
    /// <see cref="BuildingTable.EmittingDay"/> stays at 0 and
    /// <see cref="BuildingTable.PriorEmitted"/> stays at zero throughout: there is no previous Day.
    /// </remarks>
    [Fact]
    public void Emission_accumulates_within_a_day()
    {
        (World world, Simulation simulation, int slot) = Built(Emitting(max: 1));

        Step(simulation, 2);

        Assert.Equal(40, world.Buildings.DayEmitted[slot]);

        Step(simulation, 8);

        Assert.Equal(80, world.Buildings.DayEmitted[slot]);
        Assert.Equal(0, world.Buildings.EmittingDay[slot]);
        Assert.Equal(0, world.Buildings.PriorEmitted[slot]);
    }

    /// <summary>
    /// <b>A Rule that is evaluated and then starved records nothing</b>, because the recording is at
    /// <c>Fire</c> and not at <c>Check</c>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The distinction the write site turns on, asserted rather than assumed.</b> <c>Check</c>
    /// runs for every due Rule, for every rung of a failed chain and again on the phase 3 re-check,
    /// so a recording placed there would count <em>evaluations</em> — and it would be wrong in the
    /// direction nobody notices, because an evaluated Rule looks exactly like a fired one in every
    /// figure except the Layer's. Sixty flour affords ten firings; the eleventh is due, evaluated and
    /// blocked, and the Day's total must not move on it.
    /// </remarks>
    [Fact]
    public void A_starved_rule_is_evaluated_and_records_nothing()
    {
        (World world, Simulation simulation, int slot) = Built(Emitting(max: 1));

        // Ten firings, at Ticks 1, 9, 17 ... 73, which is all sixty flour.
        Step(simulation, 74);

        Assert.Equal(0, world.Bins.LevelAt(world.FindBin(slot, Flour)));
        Assert.Equal(10 * 40, world.Buildings.DayEmitted[slot]);

        // The eleventh is due at Tick 81 and has nothing to consume.
        Step(simulation, 16);

        Assert.True(world.RuleInstances.IsWaiting(0));
        Assert.Equal(10 * 40, world.Buildings.DayEmitted[slot]);
    }

    // ---- the roll, at the table ----------------------------------------------------------------------

    /// <summary>A Building on no Lot, which is all these cases need.</summary>
    private static (World World, int Slot) Bare()
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

        return (world, world.Buildings.Rows.Resolve(building));
    }

    /// <summary>
    /// <b>The first write of a new Day closes the old one into <c>PriorEmitted</c></b>, and starts the
    /// new Day's total at zero rather than on top of it.
    /// </summary>
    [Fact]
    public void The_first_write_of_a_new_day_rolls_the_finished_one_into_prior()
    {
        (World world, int slot) = Bare();

        world.Buildings.Emit(slot, 3, 100);
        world.Buildings.Emit(slot, 3, 25);

        Assert.Equal(3, world.Buildings.EmittingDay[slot]);
        Assert.Equal(125, world.Buildings.DayEmitted[slot]);
        Assert.Equal(0, world.Buildings.PriorEmitted[slot]);

        world.Buildings.Emit(slot, 4, 7);

        Assert.Equal(4, world.Buildings.EmittingDay[slot]);
        Assert.Equal(7, world.Buildings.DayEmitted[slot]);
        Assert.Equal(125, world.Buildings.PriorEmitted[slot]);
        Assert.Equal(125, world.Buildings.PriorEmissionOn(slot, 4));
    }

    /// <summary>
    /// 🔴 <b>A Day the Building was idle reads zero, and not whatever the last Day it emitted on
    /// held.</b>
    /// </summary>
    /// <remarks>
    /// The defect this closes: emit on Day 3, next on Day 9, and the columns are still carrying Day 3
    /// when Day 9's write arrives. Carrying that total forward as <em>yesterday</em> would charge the
    /// Building for six Days it emitted nothing on, and the figure would be plausible on every one of
    /// them. ***A stale accumulator is last week's number wearing today's label.***
    /// </remarks>
    [Fact]
    public void A_gap_of_more_than_one_day_reads_zero_rather_than_the_last_day_emitted()
    {
        (World world, int slot) = Bare();

        world.Buildings.Emit(slot, 3, 100);

        // Read on Day 9 before anything is written there: the pair is stale, and stale is zero.
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 9));

        world.Buildings.Emit(slot, 9, 5);

        Assert.Equal(0, world.Buildings.PriorEmitted[slot]);
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 9));
        Assert.Equal(5, world.Buildings.DayEmitted[slot]);
    }

    /// <summary>
    /// <b>Yesterday reads correctly before today's first write as well as after it</b>, which is what
    /// makes the read door a door rather than a convenience.
    /// </summary>
    /// <remarks>
    /// Nothing rolls the pair at midnight, so on a Day the Building has not yet emitted on, yesterday
    /// is still sitting in <see cref="BuildingTable.DayEmitted"/> — complete, because no write can
    /// ever land on a Day that has ended. A reader that went to <c>PriorEmitted</c> directly would get
    /// the Day <em>before</em> yesterday here and would be wrong by exactly one Day, all Day, every
    /// Day.
    /// </remarks>
    [Fact]
    public void Yesterday_reads_the_same_before_and_after_todays_first_write()
    {
        (World world, int slot) = Bare();

        world.Buildings.Emit(slot, 11, 60);

        Assert.Equal(60, world.Buildings.PriorEmissionOn(slot, 12));

        world.Buildings.Emit(slot, 12, 9);

        Assert.Equal(60, world.Buildings.PriorEmissionOn(slot, 12));
    }

    /// <summary>
    /// <b>A Building that has never emitted reads zero on every Day, with no case of its own.</b>
    /// </summary>
    /// <remarks>
    /// Day 0 and Day 1 are the two the zero fill could have got wrong — <c>EmittingDay</c> is 0 on a
    /// fresh row, which matches <em>today</em> on Day 0 and <em>yesterday</em> on Day 1 — and both
    /// land on accumulators that are themselves zero.
    /// </remarks>
    [Fact]
    public void A_building_that_has_never_emitted_reads_zero_on_every_day()
    {
        (World world, int slot) = Bare();

        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 0));
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 1));
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 2));
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 40_000));
    }

    /// <summary>
    /// <b>Day 0 asks about Day −1 and matches nothing</b> rather than wrapping to Day 65,535.
    /// </summary>
    /// <remarks>
    /// The comparison inside the door is made in <see cref="int"/> for exactly this. In
    /// <see cref="ushort"/> arithmetic <c>today - 1</c> on Day 0 is 65,535, which no row holds today
    /// and every row would hold after a long enough run.
    /// </remarks>
    [Fact]
    public void Day_zero_has_no_previous_day()
    {
        (World world, int slot) = Bare();

        world.Buildings.Emit(slot, 0, 500);

        Assert.Equal(500, world.Buildings.DayEmitted[slot]);
        Assert.Equal(0, world.Buildings.PriorEmissionOn(slot, 0));
    }

    // ---- the Readout ---------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The Readout resolves to the previous Day and never to the Day in progress.</b>
    /// </summary>
    /// <remarks>
    /// The property being held is that the answer does not depend on <em>when in the Day</em> the
    /// reader ran. Today's total is moved twice here, at two different points of Day 5, and the
    /// Readout does not move with it — it is still answering for Day 4, which is finished and cannot
    /// change.
    /// </remarks>
    [Fact]
    public void The_readout_reads_the_previous_day_and_not_the_one_in_progress()
    {
        (World world, int slot) = Bare();

        world.Clock.Tick[0] = new Ticks((ulong)Ticks.PerDay * 4);
        world.Buildings.Emit(slot, 4, 300);

        // Still inside Day 4: yesterday is Day 3, which this Building spent idle.
        Assert.Equal(0, Readouts.Read(world, slot, EmissionReadout));

        world.Clock.Tick[0] = new Ticks((ulong)Ticks.PerDay * 5);

        Assert.Equal(300, Readouts.Read(world, slot, EmissionReadout));

        world.Buildings.Emit(slot, 5, 7);
        Assert.Equal(300, Readouts.Read(world, slot, EmissionReadout));

        world.Buildings.Emit(slot, 5, 1_000_000);
        Assert.Equal(300, Readouts.Read(world, slot, EmissionReadout));

        // And it falls to zero on the Day after, because Day 5 is then yesterday only for one Day.
        world.Clock.Tick[0] = new Ticks((ulong)Ticks.PerDay * 6);
        Assert.Equal(1_000_007, Readouts.Read(world, slot, EmissionReadout));

        world.Clock.Tick[0] = new Ticks((ulong)Ticks.PerDay * 7);
        Assert.Equal(0, Readouts.Read(world, slot, EmissionReadout));
    }

    /// <summary>
    /// <b>The Day the Readout answers for comes off the world's clock</b>, so it is the same Day for
    /// every reader inside one Tick.
    /// </summary>
    /// <remarks>
    /// Asserted at a Tick in the middle of a Day rather than on a boundary: the Readout is a property
    /// of the Day, and every Tick of Day 5 must give the same answer as every other.
    /// </remarks>
    [Fact]
    public void The_readout_answers_for_the_whole_day_and_not_for_a_tick_of_it()
    {
        (World world, int slot) = Bare();

        world.Buildings.Emit(slot, 4, 300);

        foreach (int offset in new[] { 0, 1, 17, Ticks.PerDay / 2, Ticks.PerDay - 1 })
        {
            world.Clock.Tick[0] = new Ticks((ulong)((Ticks.PerDay * 5) + offset));

            Assert.Equal(300, Readouts.Read(world, slot, EmissionReadout));
        }
    }

    /// <summary>
    /// <b>Emission hangs off a Building</b>, which is part of its declaration and is what the loader
    /// checks a <c>[[rule]]</c> naming it against.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>This asserted <em>and against nothing else</em> until the scope opened, and the other
    /// two scopes are now <see cref="BusinessEmissionReadoutTests"/>'.</b> That file owns whether a
    /// Business and a Household may name it and why; a second copy here would be one fact in two
    /// places, which is <c>plans/0012</c> <b>Cause 1</b> by construction.
    /// </remarks>
    [Fact]
    public void Emission_is_readable_against_a_building()
    {
        Assert.True(Readouts.IsDeclared(EmissionReadout));
        Assert.True(Readouts.IsReadableAgainst(EmissionReadout, ReadoutScope.Building));
    }

    /// <summary>
    /// <b>A Ruleset spells it <c>emission</c></b>, and the name resolves to the id the core declares.
    /// </summary>
    /// <remarks>
    /// <c>ReadoutTests</c> holds the two sets against each other in general; this pins the one
    /// spelling, because a Ruleset that writes <c>derived = "emission"</c> is refused rather than
    /// defaulted if the table ever loses the entry.
    /// </remarks>
    [Fact]
    public void A_ruleset_spells_it_emission()
    {
        Assert.True(ReadoutNames.TryResolve("emission", out ReadoutId id));
        Assert.Equal((ushort)Readout.Emission, id.Raw);
        Assert.Equal("emission", ReadoutNames.NameOf(Readout.Emission));
    }

    /// <summary>
    /// <b>Reading it against a Household throws rather than returning zero</b>, which is the
    /// interpreter's half of <c>adr/0048</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A Business is no longer in this test and its removal is the whole of the scope
    /// change.</b> The two entry points now disagree on purpose:
    /// <c>Readouts.ReadBusiness</c> resolves the Business's premises and answers for it, while
    /// <c>ReadHousehold</c> still throws — because a Business occupies a Building and the design
    /// has settled that its emission is the Business's, and has settled nothing of the kind about
    /// an occupant.
    /// </remarks>
    [Fact]
    public void Reading_emission_against_a_household_throws()
    {
        (World world, int slot) = Bare();

        Assert.Throws<InvalidOperationException>(
            () => Readouts.ReadHousehold(world, slot, EmissionReadout));
    }
}
