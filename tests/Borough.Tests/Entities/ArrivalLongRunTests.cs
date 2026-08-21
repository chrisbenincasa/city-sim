using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 11 task 9: the acceptance run, on a world where arrivals outpace housing.
/// </summary>
/// <remarks>
/// <para>
/// <b>The world is the point and not a setting.</b> <c>rulesets/crowded.toml</c> is the only shipped
/// file in which the Unplaced Pool is under pressure — 96 arrivals a Day through each of four gates
/// against a give-up bound of 2 Days — and it exists because <c>plans/0035</c> <b>F25</b> found that
/// no such world did. On every other file the Pool is empty within a few Ticks of world creation, so
/// every assertion below would pass against a mechanism that had been deleted.
/// </para>
/// <para>
/// 🔴 <b>What <c>adr/0006</c> asks here has no answer until a gate exists.</b> Before milestone 11
/// nothing created a Household after world creation, so the Pool was a subset of a fixed population
/// and could not grow with elapsed time whatever it did. The gate removed that reason and
/// <c>adr/0130</c>'s give-up bound is what replaces it — this run is the only place the pair is
/// exercised against each other.
/// </para>
/// <para>
/// ⚠ <b>Nothing in the simulation decides to arrive</b> (<c>adr/0128</c>), so the run knocks on every
/// door once a Day with a <see cref="CommandKind.Arrive"/> Command. ***A test asserting the city grew
/// would be asserting the Command it just issued***, which is why the assertions below are about what
/// the city does with arrivals rather than about there being any.
/// </para>
/// <para>
/// ⚠ <b>The run is 32 Days rather than the 100,000 Ticks <c>CLAUDE.md</c>'s Definition of done
/// names, and the narrowing is stated rather than quiet.</b> A gated world pays a paved lattice
/// (<b>0.51 ms</b> a Tick) and this file's arrival rate pays for the churn on top, so the whole run
/// costs <b>2.2 ms</b> a Tick and 100,000 Ticks would be <b>3m45s</b> — 4.5× the working tier for one
/// test, and fifteen seconds under <c>TierBudgetTests</c>' four-minute bound. The 100,000-Tick run
/// <em>was</em> made, by hand, on the day this landed: the Pool read <b>1,464</b> on Day 16 and
/// <b>1,458</b> on Day 48. ***The obligation was discharged by running it; this test is what keeps it
/// from regressing***, and the tail below starts four Days after the Pool settles.
/// </para>
/// </remarks>
public sealed class ArrivalLongRunTests : IClassFixture<ArrivalLongRun>
{
    private readonly ArrivalLongRun _run;

    public ArrivalLongRunTests(ArrivalLongRun run) => _run = run;

    /// <summary>Readings discarded as the transient: the Pool fills from empty over four Days.</summary>
    private const int SettleDays = 8;

    private ArrivalLongRun.Reading[] Tail => _run.Readings[SettleDays..];

    // ---- vacuity, and none of it is a formality ---------------------------------------------------

    /// <summary>
    /// <b>The world has doors in it and they let people through.</b>
    /// </summary>
    /// <remarks>
    /// <b>Milestone 9's F17 is why this is asserted rather than assumed.</b> That milestone shipped a
    /// land-value producer that was correct and unobservable, because no world it could run in had the
    /// content it read. A gate kind no Building is ever raised of is the same failure, and every
    /// assertion in this class would pass in that world.
    /// </remarks>
    [Fact]
    public void The_run_has_gates_and_they_admit()
    {
        Assert.Equal(4, _run.Gates);

        Assert.True(
            _run.Arrived > 0,
            "no Household ever came through a gate, so every assertion in this class is about a "
            + "Pool that only ever held the population the world was created with.");
    }

    /// <summary>
    /// <b>Both flows ran: the city looked at dwellings and it housed people.</b>
    /// </summary>
    /// <remarks>
    /// <b>Two counters and not one, because they fail differently.</b> A Pool that stops growing
    /// because placement stopped looking reads the same on a <c>placed</c> column as one that stopped
    /// growing because the city found room — <c>considered</c> is what separates them, and it is
    /// <c>00-vision.md</c>'s own Evidence line arriving as a test.
    /// </remarks>
    [Fact]
    public void The_placement_pass_ran_throughout()
    {
        Assert.True(Mean(Tail, r => r.Considered) > 0, "placement never looked at anybody.");
        Assert.True(Mean(Tail, r => r.Placed) > 0, "placement never housed anybody.");

        Assert.True(
            Mean(Tail, r => r.Pool) > 0,
            "the Pool was empty throughout, so this world does not put it under pressure and is not "
            + "the world this run was specified against.");
    }

    // ---- the claims -------------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The Unplaced Pool does not grow without bound.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0006</c> aimed at the one collection whose sink is a mechanism rather than a
    /// table</b>, in a world built so that its inflow exceeds what the city can house. The Pool is
    /// <em>supposed</em> to stand at a level here — that level is the housing shortage, and a city
    /// that housed everybody would be one where the bound was never tested. What is asserted is that
    /// the level does not climb.
    /// </para>
    /// <para>
    /// ⚠ <b>A drift over the tail rather than a ceiling</b>, on
    /// <see cref="Rules.PlacementLongRunTests"/>' discipline: a ceiling is a number somebody has to
    /// choose and this corpus has no ratifier for one, while a trend is a property of the mechanism.
    /// A city whose give-up bound had been deleted climbs monotonically and fails this by a wide
    /// margin; one whose bound merely moved does not fail it at all, which is correct — the bound is
    /// a designer's number and <c>plans/0002</c> §D1 is where it is answerable.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_pool_does_not_grow_over_the_run()
    {
        ArrivalLongRun.Reading[] tail = Tail;

        long early = Mean(tail[..(tail.Length / 2)], r => r.Pool);
        long late = Mean(tail[(tail.Length / 2)..], r => r.Pool);
        long drift = ((late - early) * 1_000) / early;

        Assert.True(
            drift <= 62,
            $"the Unplaced Pool stood at {early} over the first half of the tail and {late} over the "
            + $"second, a drift of {drift / 10}.{Abs(drift) % 10}%. Arrivals are outrunning both the "
            + "housing pass and the give-up bound, which is adr/0006 for the one collection in this "
            + "build that has an inflow from outside the world.");
    }

    /// <summary>
    /// 🔴 <b>The give-up channel is what holds it, and it is asserted separately.</b>
    /// </summary>
    /// <remarks>
    /// <b>Without this the test above is satisfied by a city that houses everybody.</b> The drift
    /// claim is about a level and says nothing about which of the two sinks holds it; a build whose
    /// <c>Depart</c> was never called would pass it in any world roomy enough. ***A bound that never
    /// fires is a bound nobody has tested***, and this world's whole purpose is to make it fire.
    /// </remarks>
    [Fact]
    public void The_give_up_bound_fires_and_keeps_firing()
    {
        ArrivalLongRun.Reading[] tail = Tail;

        Assert.True(
            Mean(tail, r => r.Departed) > 0,
            "nobody ever gave up and left, so the Pool's only sink over this run was housing -- and "
            + "adr/0130's bound, which this world sets to 2 Days precisely so that it is reachable, "
            + "was never reached.");

        // Every Day of the tail and not the mean, because a sink that fired early and stopped
        // averages to the same number as one that never stopped.
        foreach (ArrivalLongRun.Reading reading in tail)
        {
            Assert.True(
                reading.Departed > 0,
                $"nobody gave up on Day {reading.Day}. The sink is intermittent, which a mean over "
                + "the tail cannot distinguish from one that stopped.");
        }
    }

    /// <summary>
    /// 🔴 <b>Money's supply is not constant, and it is still conserved.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pair is the claim.</b> <c>adr/0024</c> makes the Outside Connection money's only source
    /// and sink, and until milestone 11 task 5 a world's supply was a constant because nothing crossed
    /// the gate. A run in which the supply never moves is one where <c>MoneyIsConserved</c> holds for
    /// the reason it always did, and proves nothing about the flow.
    /// </para>
    /// <para>
    /// ⚠ <b>The equality is exact and has no flow term</b> — <c>plans/0035</c> <b>F20</b>, where the
    /// invariant this milestone was scoped to rewrite turned out to need nothing.
    /// <c>MoneySupply.Issued</c> is declared net of what has left, and both sides move in one call:
    /// <c>World.Endow</c> on the way in and <c>World.Depart</c> on the way out.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_supply_moves_and_stays_conserved()
    {
        Assert.NotEqual(_run.IssuedAtStart, _run.IssuedAtEnd);

        Assert.True(
            _run.IssuedAtEnd > _run.IssuedAtStart,
            $"the supply fell from {_run.IssuedAtStart} to {_run.IssuedAtEnd} over a run in which "
            + "arrivals outnumber departures, which means money left faster than it came in.");

        // The invariant itself, at the tier it is declared at. It ran staggered throughout the run;
        // this is the whole-world fold, and it throws rather than returning a verdict.
        _run.CheckEndOfRun();
    }

    /// <summary>
    /// 🔴 <b>The four Hinterlands produce visibly different arrivals.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the second half of what <c>plans/0002</c> §D1 names as the emigrant bands'
    /// ratifying quantity</b>, and it is the half that is easy to leave undone: the first — that
    /// money is conserved across the gate — is satisfied by a build in which every edge draws from
    /// one pooled figure. ⚠ <b>A band that produced the same Household from every edge would be an
    /// anchor that does not reach the thing it anchors</b>, which is
    /// <see href="../../../docs/adr/0131-the-gate-carries-people-and-the-money-they-hold-and-a-hinterland-field-lands-in-the-milestone-that-reads-it.md">adr/0131</see>'s
    /// whole reason for refusing <c>[households] opening_balance</c>.
    /// </para>
    /// <para>
    /// ⚠ <b>What is asserted is the ORDER and not the figures.</b> <c>crowded.toml</c> authors west
    /// poorest and north richest, and the file's own header says the order carries no claim about
    /// geography — it is there so that choosing an edge buys something. So the test asserts that the
    /// means come out in the authored order, which fails if the draw ignores the Hinterland and
    /// passes for any band spacing a designer later chooses. ***Asserting the numbers would be
    /// asserting the Ruleset back to itself.***
    /// </para>
    /// </remarks>
    [Fact]
    public void The_four_edges_produce_distinguishable_arrivals()
    {
        // Indexed by the enum, whose zero is MapEdge.None -- the ordinary answer for a Lot that is
        // not on the boundary, and not an edge a Hinterland can be authored for.
        long[] totals = new long[5];
        int[] counts = new int[5];

        _run.WalkPool((edge, balance) =>
        {
            totals[(int)edge] += balance;
            counts[(int)edge]++;
        });

        foreach (MapEdge edge in (MapEdge[])[MapEdge.West, MapEdge.South, MapEdge.East, MapEdge.North])
        {
            Assert.True(
                counts[(int)edge] > 0,
                $"nobody in the Pool came through the {edge} gate, so this world does not exercise "
                + "all four Hinterlands and the comparison below is between fewer than four markets.");
        }

        Assert.Equal(0, counts[(int)MapEdge.None]);

        long west = totals[(int)MapEdge.West] / counts[(int)MapEdge.West];
        long south = totals[(int)MapEdge.South] / counts[(int)MapEdge.South];
        long east = totals[(int)MapEdge.East] / counts[(int)MapEdge.East];
        long north = totals[(int)MapEdge.North] / counts[(int)MapEdge.North];

        Assert.True(
            west < south && south < east && east < north,
            $"the four edges' mean arriving balances are west {west}, south {south}, east {east}, "
            + $"north {north}, which is not the order crowded.toml authors. Either the draw is not "
            + "reading the arriving Household's own Hinterland, or the four bands have collapsed "
            + "into one -- and an anchor that does not reach the thing it anchors is decoration "
            + "(adr/0131).");
    }

    private static long Abs(long value) => value < 0 ? -value : value;

    private static long Mean(ArrivalLongRun.Reading[] readings, Func<ArrivalLongRun.Reading, long> of)
    {
        long total = 0;

        foreach (ArrivalLongRun.Reading reading in readings)
        {
            total += of(reading);
        }

        return total / readings.Length;
    }
}

/// <summary>The run, done once and read by every assertion above.</summary>
/// <remarks>
/// <b>A class fixture because the run is the expensive part</b>, on
/// <c>LandValueLongRun</c>'s shape and <c>plans/0035</c> <b>F27</b>'s lesson: six tests each
/// building an identical world took 1m30s where one shared build took 18s. ***A budget per test does
/// not bound a suite.***
/// </remarks>
public sealed class ArrivalLongRun
{
    /// <summary>
    /// 32 Days. ⚠ <b>Not 100,000 Ticks, and <see cref="ArrivalLongRunTests"/> says why at length.</b>
    /// </summary>
    private const int TickCount = 65_536;

    private const int Population = 1_000;

    /// <summary>
    /// Citizens per arriving Household — <b>the instrument's number and not the city's</b>.
    /// </summary>
    /// <remarks>
    /// Nothing in the build models Life Stage to household composition, so a figure derived here
    /// would be a model nobody wrote. It is the same 2 <c>--arrivals</c> uses, so the two readouts
    /// are comparable.
    /// </remarks>
    private const byte CitizensPerHousehold = 2;

    private readonly Simulation _simulation;
    private readonly World _world;

    public ArrivalLongRun()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "crowded.toml"));

        Assert.True(result.Ok, $"rulesets/crowded.toml was refused:\n  {result.Describe()}");

        var key = WorldKey.FromSeed(1);

        _world = new World(Population, result.Ruleset!, key);

        // O(world) twice a Tick against a phase meant to be O(woken), on a file that paves the
        // lattice to the map's boundary: 535,817 Segments, ~19 ms a fold. plans/0035 F26, where the
        // guard's cost was measured and filed as if it were the city's.
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(_world, key, Ticks.Zero);

        IssuedAtStart = _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

        Readings = Run(Doors());

        IssuedAtEnd = _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;
    }

    public Reading[] Readings { get; }

    public int Gates { get; private set; }

    public long Arrived { get; private set; }

    public long IssuedAtStart { get; }

    public long IssuedAtEnd { get; }

    public void CheckEndOfRun() => _simulation.CheckEndOfRun();

    /// <summary>
    /// Every Household still in the Pool, as the map edge it came through and what it holds.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The Pool rather than the housed population, and the difference is what makes it
    /// readable.</b> <c>UnplacedTable.Gate</c> records which door a Household came through
    /// (<c>adr/0129</c>) and a Household that has been placed has left the Pool, taking the record
    /// with it — so the standing Pool is the only place the pairing survives. In a world where
    /// arrivals outpace housing that is most of them, which is the one thing this world guarantees.
    /// </remarks>
    public void WalkPool(Action<MapEdge, long> visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        for (int position = 0; position < _world.UnplacedPool.Count; position++)
        {
            if (!_world.Buildings.Rows.TryResolve(_world.UnplacedPool.GateAt(position), out int gate)
                || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[gate], out int lot))
            {
                // A Household the city generated rather than admitted, or a gate demolished under
                // one that was. Neither is a violation and neither is evidence about a band.
                continue;
            }

            int slot = _world.Households.Rows.Resolve(_world.UnplacedPool.At(position));

            if (_world.Bins.Rows.TryResolve(_world.Households.Balance[slot], out int balance))
            {
                visit(_world.EdgeOf(lot), _world.Bins.LevelAt(balance));
            }
        }
    }

    /// <summary>One Day's reading of the stock and the three flows.</summary>
    public readonly record struct Reading(
        ulong Day, int Pool, long Considered, long Placed, long Departed, long Issued);

    /// <summary>Every gate this world raised, with the Command that knocks on it.</summary>
    /// <remarks>
    /// ⚠ <b>The ask is four over the ceiling</b>, which is enough that the door refuses somebody and
    /// cheap enough that the overage is not most of the work. The <see cref="ArrivePayload"/>'s
    /// household count is eight bits, so a ceiling at or above 255 could not be saturated at all.
    /// </remarks>
    private Command[] Doors()
    {
        var knocks = new List<Command>();

        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (!_world.Buildings.Rows.IsLive(slot)
                || !_world.IsOutsideConnection(_world.Buildings.Kind[slot])
                || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[slot], out int lot))
            {
                continue;
            }

            int ceiling = _world.Rules.Kind(_world.Buildings.Kind[slot]).ArrivalsPerDay;
            int ask = ceiling + 4 > byte.MaxValue ? byte.MaxValue : ceiling + 4;

            knocks.Add(new Command(
                CommandKind.Arrive,
                _world.Lots.East[lot],
                _world.Lots.North[lot],
                new ArrivePayload((byte)ask, 0, CitizensPerHousehold).Encode()));
        }

        Gates = knocks.Count;

        return [.. knocks];
    }

    /// <summary>Steps the world, knocking on every door once a Day and sampling once a Day.</summary>
    /// <remarks>
    /// <b>The knock is on the Day boundary because the meter is</b>: <c>World.TryArrive</c> resets a
    /// gate's quota when the Day number changes, so asking at any other point would meet a ceiling
    /// half spent by the previous knock.
    /// </remarks>
    private Reading[] Run(Command[] doors)
    {
        List<Reading> readings = [];
        Span<Command> knock = stackalloc Command[1];

        for (ulong tick = 0; tick < TickCount; tick++)
        {
            if (tick % Ticks.PerDay == 0)
            {
                foreach (Command door in doors)
                {
                    int before = _world.Households.Rows.LiveCount;

                    knock[0] = door;
                    _simulation.Step(new TickInput(knock, _simulation.RulesetInForce));

                    Arrived += Math.Max(0, _world.Households.Rows.LiveCount - before);
                }

                // Drained rather than read through a Census, and the two are mutually exclusive:
                // Census.Observe drains the same engine, so a run doing both reads each flow at
                // whichever ran second and gets zero.
                PlacementActivity activity = _simulation.Placement.Drain();

                readings.Add(new Reading(
                    tick / Ticks.PerDay,
                    _world.UnplacedPool.Count,
                    activity.Considered.Sum,
                    activity.Placed.Sum,
                    activity.Departed.Sum,
                    _world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw));
            }

            _simulation.Step(default);
        }

        return [.. readings];
    }
}
