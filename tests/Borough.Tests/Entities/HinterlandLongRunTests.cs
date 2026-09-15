using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0073</c>'s long run: 50 Days of a counted Outside, in a world whose circuit is still
/// running at the end of them.
/// </summary>
/// <remarks>
/// The declining fixture keeps housing and immigration active after the opening transient.
/// The attracted fixture eventually fills, so its long run checks accounts without asserting
/// continued flow. Measurement conditions and results are recorded in plans/0073.
/// </remarks>
public sealed class HinterlandLongRunTests
    : IClassFixture<SteadyOutsideLongRun>, IClassFixture<GrowingOutsideLongRun>
{
    private readonly SteadyOutsideLongRun _steady;
    private readonly GrowingOutsideLongRun _growing;

    public HinterlandLongRunTests(SteadyOutsideLongRun steady, GrowingOutsideLongRun growing)
    {
        _steady = steady;
        _growing = growing;
    }

    // Allow one authored recovery period before comparing tail means. An eight-Day warmup
    // measures initial stock adjustment when recovery itself takes thirty-two Days.
    private OutsideLongRun.Sample[] Tail => _steady.Samples[_steady.RecoveryDays..];

    /// <summary>
    /// 🔴 <b>The circuit was still running on the last Day, so the rest of this class is about a
    /// mechanism rather than about a stopped one.</b>
    /// </summary>
    /// <remarks>
    /// Require actual admissions and willing occasions so an idle circuit cannot satisfy the bounds.
    /// </remarks>
    [Fact]
    public void The_circuit_was_still_running_at_the_end()
    {
        OutsideLongRun.Sample[] tail = Tail;

        Assert.True(
            tail[^1].Admitted > tail[0].Admitted,
            $"no Household crossed any gate over the last {tail.Length} Days of the run, so every "
            + "assertion in this class is about an Outside that nobody ever left.");

        bool wanted = false;

        foreach (OutsideLongRun.Sample sample in tail)
        {
            wanted |= sample.Willing > 0;
        }

        Assert.True(
            wanted,
            "nobody compared the city and wanted to come on any Day of the tail. A world where every "
            + "occasion is `no sample` has no feasible dwelling in it, and this class would be "
            + "asserting the bounds of a queue nobody can join.");

        foreach (OutsideLongRun.Sample sample in _steady.Samples)
        {
            foreach (OutsideLongRun.EdgeSample edge in sample.Edges)
            {
                Assert.Equal(
                    edge.Occasions, edge.NoConnection + edge.NoSample + edge.StayedOutside + edge.Willing);
            }
        }
    }

    /// <summary>
    /// <b>No edge ever held a negative stock, or promised more Households than it had.</b>
    /// </summary>
    [Fact]
    public void Stock_never_goes_negative_and_never_promises_more_than_it_holds()
    {
        foreach (OutsideLongRun.Sample sample in _steady.Samples)
        {
            foreach (OutsideLongRun.EdgeSample edge in sample.Edges)
            {
                Assert.True(
                    edge.Stock >= 0,
                    $"the {edge.Edge} edge held {edge.Stock} Households on Day {sample.Day}.");

                Assert.True(
                    edge.Reserved >= 0 && edge.Reserved <= edge.Stock,
                    $"the {edge.Edge} edge promised {edge.Reserved} of {edge.Stock} Households to "
                    + $"waiting places on Day {sample.Day}.");
            }
        }
    }

    /// <summary>
    /// <b>Nobody waited outside a full door for longer than the Ruleset allows.</b>
    /// </summary>
    /// <remarks>
    /// Assert that the fixture actually queues; a permanently empty queue would pass vacuously.
    /// </remarks>
    [Fact]
    public void Nobody_waits_longer_than_the_authored_wait()
    {
        ulong allowed = (ulong)_steady.QueueWaitTicks;

        foreach (OutsideLongRun.Sample sample in _steady.Samples)
        {
            foreach (OutsideLongRun.EdgeSample edge in sample.Edges)
            {
                Assert.True(
                    edge.OldestWait <= allowed,
                    $"a Household had waited {edge.OldestWait} Ticks at the {edge.Edge} edge on Day "
                    + $"{sample.Day}, against an authored wait of {allowed}. The expiry pass has "
                    + "stopped firing, and a reservation nobody collects is held for ever.");
            }
        }
    }

    /// <summary>
    /// 🔴 <b>Emigration opened groups the file never authored, and the Outside retired them again.</b>
    /// </summary>
    /// <remarks>
    /// Require both return-created groups and a later decrease in live groups; slots may be reused.
    /// </remarks>
    [Fact]
    public void A_returned_group_is_opened_and_later_retired()
    {
        int authored = _steady.AuthoredGroups;
        int peak = 0;
        bool retired = false;

        for (int at = 0; at < _steady.Samples.Length; at++)
        {
            OutsideLongRun.Sample sample = _steady.Samples[at];

            peak = sample.Unauthored > peak ? sample.Unauthored : peak;

            if (at > 0 && sample.Groups < _steady.Samples[at - 1].Groups)
            {
                retired = true;
            }
        }

        Assert.True(
            peak > 0,
            "no return ever opened a group the Ruleset had not authored, so the crediting path this "
            + "run is supposed to exercise never ran.");

        Assert.True(
            retired,
            $"the live group count never fell over {_steady.Samples.Length} Days. Groups were opened "
            + $"by returns and none was ever retired, so the {authored} authored rows are joined by a "
            + "set that only grows.");
    }

    /// <summary>
    /// <b>The rows the Outside occupies stay bounded while returns keep opening new ones.</b>
    /// </summary>
    /// <remarks>
    /// Tail drift is a regression tripwire, not proof of a plateau. The 50-Day fixture still
    /// showed growth at the reviewed revision; see plans/0073 for its measurement conditions.
    /// </remarks>
    [Fact]
    public void The_groups_the_Outside_occupies_stay_bounded()
    {
        OutsideLongRun.Sample[] tail = Tail;

        long early = Mean(tail[..(tail.Length / 2)], s => s.Groups);
        long late = Mean(tail[(tail.Length / 2)..], s => s.Groups);
        long drift = ((late - early) * 1_000) / early;

        Assert.True(
            drift <= 125,
            $"the Outside occupied {early} group rows over the first half of the tail and {late} over "
            + $"the second, a drift of {drift / 10}.{Abs(drift) % 10}%. Returns are opening groups "
            + "faster than decay retires them, and a composition is a storage key rather than a "
            + "population — so the storage is what has to be bounded.");
    }

    /// <summary>
    /// 🔴 <b>Fifty Days on, the people and the Money still add up exactly.</b>
    /// </summary>
    [Fact]
    public void The_people_and_the_Money_still_add_up_after_a_hundred_thousand_ticks()
    {
        _steady.CheckEndOfRun();

        Assert.True(
            _steady.SupplyAtEnd != _steady.SupplyAtStart,
            "the money supply never moved over the whole run, so conservation held for the reason it "
            + "holds in a world with no gate in it.");
    }

    /// <summary>
    /// <b>The saturating city keeps its books, and nothing here says its population stood still.</b>
    /// </summary>
    /// <remarks>
    /// The growing fixture checks accounting even when its immigration circuit later becomes idle.
    /// </remarks>
    [Fact]
    public void The_books_balance_in_a_city_that_is_still_changing()
    {
        _growing.CheckEndOfRun();

        Assert.NotEqual(_growing.PeopleAtStart, _growing.Samples[^1].People);

        foreach (OutsideLongRun.Sample sample in _growing.Samples)
        {
            foreach (OutsideLongRun.EdgeSample edge in sample.Edges)
            {
                Assert.True(edge.Stock >= 0);
                Assert.True(edge.Reserved <= edge.Stock);
            }
        }
    }

    private static long Abs(long value) => value < 0 ? -value : value;

    private static long Mean(OutsideLongRun.Sample[] samples, Func<OutsideLongRun.Sample, long> of)
    {
        long total = 0;

        foreach (OutsideLongRun.Sample sample in samples)
        {
            total += of(sample);
        }

        return total / samples.Length;
    }
}

/// <summary>The steady world: <c>attracted-declining.toml</c>, whose circuit never stops.</summary>
public sealed class SteadyOutsideLongRun : OutsideLongRun
{
    public SteadyOutsideLongRun()
        : base("attracted-declining.toml")
    {
    }
}

/// <summary>The growing world: <c>attracted.toml</c>, which fills up and falls quiet.</summary>
public sealed class GrowingOutsideLongRun : OutsideLongRun
{
    public GrowingOutsideLongRun()
        : base("attracted.toml")
    {
    }
}

/// <summary>One long run of a counted Outside, sampled once a Day.</summary>
/// <remarks>
/// Share the expensive run across assertions.
/// </remarks>
public abstract class OutsideLongRun
{
    /// <summary>50 Days — 102,400 Ticks, over the plan's hundred thousand.</summary>
    private const int Days = 50;

    private const int Population = 1_000;

    private readonly Simulation _simulation;
    private readonly World _world;

    protected OutsideLongRun(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Assert.True(result.Ok, $"rulesets/{file} was refused:\n  {result.Describe()}");

        var key = WorldKey.FromSeed(11);

        _world = new World(Population, result.Ruleset!, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(_world, key, Ticks.Zero);

        QueueWaitTicks = _world.Rules.Immigration.QueueWaitTicks;
        RecoveryDays = _world.Rules.Immigration.RecoveryDays;
        AuthoredGroups = _world.Rules.HinterlandPopulations.Length;
        SupplyAtStart = _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
        PeopleAtStart = _world.Citizens.Rows.LiveCount;

        Samples = Run();

        SupplyAtEnd = _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
    }

    public Sample[] Samples { get; }

    public int QueueWaitTicks { get; }

    public int RecoveryDays { get; }

    public int AuthoredGroups { get; }

    public long SupplyAtStart { get; }

    public long SupplyAtEnd { get; }

    public long PeopleAtStart { get; }

    public void CheckEndOfRun() => _simulation.CheckEndOfRun();

    /// <summary>One edge as it stood on one Day.</summary>
    public readonly record struct EdgeSample(
        MapEdge Edge,
        int Stock,
        int Reserved,
        int Queue,
        ulong OldestWait,
        int Occasions,
        int NoConnection,
        int NoSample,
        int StayedOutside,
        int Willing);

    /// <summary>One Day's reading of the whole circuit.</summary>
    public readonly record struct Sample(
        ulong Day,
        int Pool,
        long People,
        long Supply,
        long Admitted,
        long Willing,
        int Groups,
        int Unauthored,
        EdgeSample[] Edges);

    private Sample[] Run()
    {
        List<Sample> samples = [];

        for (ulong tick = 0; tick < (ulong)Days * Ticks.PerDay; tick++)
        {
            if (tick % Ticks.PerDay == 0)
            {
                samples.Add(Read(tick / Ticks.PerDay));
            }

            _simulation.Step(default);
        }

        samples.Add(Read((ulong)Days));

        return [.. samples];
    }

    /// <summary>
    /// Reads the whole circuit, and the reading changes nothing — <c>HinterlandReading</c> consumes no
    /// draw and resets no meter, which is why a run may sample every Day without altering itself.
    /// </summary>
    private Sample Read(ulong day)
    {
        var edges = new EdgeSample[HinterlandTable.Edges];
        long admitted = 0;
        long willing = 0;
        int unauthored = 0;

        Span<HinterlandGroupReading> groups = new HinterlandGroupReading[64];

        for (int slot = 0; slot < HinterlandTable.Edges; slot++)
        {
            MapEdge edge = HinterlandTable.EdgeAt(slot);
            HinterlandReading reading = HinterlandReading.Of(_world, edge);

            edges[slot] = new EdgeSample(
                edge,
                reading.StockHouseholds,
                reading.ReservedHouseholds,
                reading.QueueHouseholds,
                reading.OldestWait,
                reading.Today.Occasions,
                reading.Today.NoConnection,
                reading.Today.NoSample,
                reading.Today.StayedOutside,
                reading.Today.Willing);

            admitted += _world.Hinterlands.AdmittedHouseholds[slot];
            willing += reading.Today.Willing + reading.Yesterday.Willing;

            int written = HinterlandGroupReading.Of(_world, edge, groups);

            for (int at = 0; at < written; at++)
            {
                if (!groups[at].Authored)
                {
                    unauthored++;
                }
            }
        }

        return new Sample(
            day,
            _world.UnplacedPool.Count,
            _world.Citizens.Rows.LiveCount,
            _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw,
            admitted,
            willing,
            _world.HinterlandPopulation.Rows.LiveCount,
            unauthored,
            edges);
    }
}
