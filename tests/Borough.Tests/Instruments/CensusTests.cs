using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests.Instruments;

/// <summary>
/// The Census: <c>adr/0006</c>'s instrument, and the history behind <c>series(metric, window)</c>.
/// </summary>
/// <remarks>
/// <b>There is no test here that a series fails to trend upward, and that is deliberate.</b> Nothing
/// in the world grows or shrinks before slice 7 — no Event Wheel, no Rules, no Trips — so an
/// assertion about steady state would pass against an empty world and a static one equally, and would
/// read as covering a property it cannot see. What is tested is that the instrument reports what
/// happened, including the case it is built for: a population that churns and comes back.
/// </remarks>
public sealed class CensusTests
{
    /// <summary>Declaration order, which is what <see cref="Metric.Table"/> indexes.</summary>
    private const int Lots = 0;

    /// <inheritdoc cref="Lots"/>
    private const int Citizens = 3;

    [Fact]
    public void A_reading_records_every_counter_of_every_table()
    {
        var world = new World(16);
        var census = new Census(world);

        world.Lots.Create(new Tiles(1), new Tiles(1), zone: 1);
        world.Lots.Create(new Tiles(2), new Tiles(2), zone: 1);
        census.Observe(world, new Ticks(0), default);

        Assert.Equal(1, census.Count);
        Assert.Equal(2, Latest(census, Lots, CensusCounter.Live));
        Assert.Equal(2, Latest(census, Lots, CensusCounter.Slots));
        Assert.Equal(world.Lots.Rows.Capacity, Latest(census, Lots, CensusCounter.Capacity));
    }

    /// <summary>
    /// The signature <c>adr/0006</c> is actually about, and the one a row count cannot show.
    /// </summary>
    /// <remarks>
    /// A city whose population churns and returns to its starting size shows a flat <c>live</c>
    /// whether or not the slots underneath it are being recycled. <c>slots</c> is the counter that
    /// separates the two, because it only ever rises when a create finds the free list empty — so a
    /// leak is <em>slots climbing while live is flat</em>. Thirty-two rounds of create-and-destroy
    /// cost exactly one slot here; a free list that was not being returned to would have cost
    /// thirty-two.
    /// </remarks>
    [Fact]
    public void Churn_that_returns_to_its_starting_size_leaves_no_slots_behind()
    {
        var world = new World(64);
        var census = new Census(world);

        Handle<Building> home = world.Buildings.Create(world.Lots, 
            world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1), kind: 1);

        Handle<Household> household = world.CreateHousehold(home, lifeStage: 1);

        for (int i = 0; i < 8; i++)
        {
            world.CreateCitizen(household, new Ticks(0));
        }

        census.Observe(world, new Ticks(0), default);
        long slotsAtRest = Latest(census, Citizens, CensusCounter.Slots);

        for (int round = 0; round < 32; round++)
        {
            Handle<Citizen> passing = world.CreateCitizen(household, new Ticks(0));
            world.DestroyCitizen(passing);
        }

        census.Observe(world, new Ticks(1), default);

        Assert.Equal(8, Latest(census, Citizens, CensusCounter.Live));
        Assert.Equal(slotsAtRest + 1, Latest(census, Citizens, CensusCounter.Slots));
    }

    [Fact]
    public void The_ring_overwrites_its_oldest_reading_rather_than_growing()
    {
        var world = new World(16);
        var census = new Census(world, capacity: 4);

        for (ulong tick = 0; tick < 10; tick++)
        {
            census.Observe(world, new Ticks(tick), default);
        }

        Assert.Equal(4, census.Count);
        Assert.Equal(4, census.Capacity);
        Assert.Equal(10UL, census.Taken);

        Series series = census.Series(Metric.Of(Lots, CensusCounter.Live), new Ticks(1_000));

        Assert.Equal(4, series.Count);
        Assert.Equal(6UL, series.Samples.Span[0].Tick.Raw);
        Assert.Equal(9UL, series.Samples.Span[^1].Tick.Raw);
    }

    [Fact]
    public void A_window_returns_only_readings_inside_it()
    {
        var world = new World(16);
        var census = new Census(world);

        for (ulong tick = 0; tick <= 100; tick += 10)
        {
            census.Observe(world, new Ticks(tick), default);
        }

        Series series = census.Series(Metric.Of(Lots, CensusCounter.Live), new Ticks(30));

        // Newest reading is Tick 100, so the window floor is 70: four readings, not eleven.
        Assert.Equal(4, series.Count);
        Assert.Equal(70UL, series.Samples.Span[0].Tick.Raw);
        Assert.True(series.Complete);
    }

    /// <summary>
    /// The one thing a finite ring must never do quietly.
    /// </summary>
    /// <remarks>
    /// A window longer than the surviving history can only be answered over part of it. Returning the
    /// short answer unmarked would let a caller conclude <em>flat over the whole run</em> from its
    /// tail, which is a claim the data does not support and which nothing downstream could catch.
    /// </remarks>
    [Fact]
    public void A_window_reaching_past_the_discarded_readings_is_not_complete()
    {
        var world = new World(16);
        var census = new Census(world, capacity: 4);

        for (ulong tick = 0; tick < 10; tick++)
        {
            census.Observe(world, new Ticks(tick), default);
        }

        var metric = Metric.Of(Lots, CensusCounter.Live);

        Assert.False(census.Series(metric, new Ticks(100)).Complete);
        Assert.True(census.Series(metric, new Ticks(2)).Complete);
    }

    [Fact]
    public void A_census_that_has_never_looked_returns_nothing_and_claims_nothing_is_missing()
    {
        var census = new Census(new World(16));

        Series series = census.Series(Metric.Of(Lots, CensusCounter.Live), new Ticks(1_000));

        Assert.Equal(0, series.Count);
        Assert.True(series.Complete);
    }

    [Fact]
    public void A_metric_naming_a_table_this_census_does_not_have_is_refused()
    {
        var census = new Census(new World(16));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => census.Series(Metric.Of(census.Tables, CensusCounter.Live), new Ticks(1)));
    }

    /// <summary>
    /// The instrument does not touch the city.
    /// </summary>
    /// <remarks>
    /// <c>05 §4</c>'s test for whether something is an optimisation or a design change is whether the
    /// State Hash moved. A census that moved it would be an instrument that changed what it measures,
    /// which is the defect every observability mechanism in this project is written to avoid.
    /// </remarks>
    [Fact]
    public void Taking_a_census_does_not_move_the_state_hash()
    {
        InputLog log = new InputLogBuilder(0x0B07, new WorldConfiguration(256), 0).Build();

        ulong[] without = Replay.Run(log, new Ticks(512), hashEvery: 64);

        Simulation simulation = Replay.Start(log, Ruleset.Empty);
        var with = new List<ulong>();

        Replay.Trace(simulation, log, new Ticks(512), 64, with, new Census(simulation.World));

        Assert.Equal(without, with);
    }

    [Fact]
    public void A_traced_run_takes_one_reading_per_hash()
    {
        InputLog log = new InputLogBuilder(0x0B07, new WorldConfiguration(256), 0).Build();

        Simulation simulation = Replay.Start(log, Ruleset.Empty);
        var hashes = new List<ulong>();
        var census = new Census(simulation.World);

        Replay.Trace(simulation, log, new Ticks(1_000), 100, hashes, census);

        Assert.Equal(10, hashes.Count);
        Assert.Equal(10, census.Count);

        Series series = census.Series(Metric.Of(Lots, CensusCounter.Live), new Ticks(1_000));

        Assert.Equal(100UL, series.Samples.Span[0].Tick.Raw);
        Assert.Equal(1_000UL, series.Samples.Span[^1].Tick.Raw);
    }

    /// <summary>The newest reading of one metric, which a zero-length window is exactly.</summary>
    private static long Latest(Census census, int table, CensusCounter counter)
    {
        Series series = census.Series(Metric.Of(table, counter), new Ticks(0));

        return series.Samples.Span[^1].Value;
    }
}
