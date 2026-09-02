using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Evidence;

using Evidence = Borough.Core.Evidence.Evidence;
using CondemnationEvidence = Borough.Core.Evidence.CondemnationEvidence;
using LotEvidence = Borough.Core.Evidence.LotEvidence;

/// <summary>
/// Milestone 6 task 6: the long acceptance run for Evidence.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>This milestone's two halves fail this obligation in opposite directions, and saying so is
/// most of the run's work.</b> The <b>collection</b> cannot grow — <c>CondemnationTrailTable</c>
/// allocates <c>Retained + 1</c> rows in its constructor and never allocates another — so the
/// assertion is a regression guard against an edit to that constructor rather than a discovery. The
/// <b>magnitude</b> must grow: the aggregate's count climbs for ever on purpose, because
/// ***attribution decays to magnitude*** is what the milestone is for. So the run <em>states</em> the
/// exception rather than asserting flatness over it, and asserts flatness over the <b>flow</b> instead.
/// </para>
/// <para>
/// <b>The ground for the exception is this milestone's own D1 axis, <em>who reads it</em>.</b> Nothing
/// inside <c>step()</c> reads <c>Condemnations</c> — it is written at the condemnation door and read
/// only by a human on a click — so an unbounded value cannot change what the city does. ⚠ <b>What it
/// can do is overflow, and that was a live defect found while scouting this run</b>: the column was an
/// <c>int</c>, which wraps after roughly 162 hours of play at a million Citizens, and a wrapped count
/// reports that the city has un-condemned Buildings. It is a <c>long</c> now, and
/// <c>CondemnationTrailTests</c> holds the width as a compile error. ***A counter with no sink is
/// denominated in the life of the world, not in the size of the city.***
/// </para>
/// <para>
/// ⚠ <b>The ceiling is tightened and that is forced rather than chosen.</b> At the shipped fifty
/// minutes <c>CitizenTable.ReachFailures</c> is <b>0 for every Citizen at every sample across 100,000
/// Ticks</b> — measured — so the second magnitude this milestone added would be asserted flat on a
/// column no world writes to. <c>ReachFailureTests</c> owns the negative at the shipped value and its
/// lever is the same one: the paved extent is derived from the population, so a bigger fixture is a
/// bigger city with the same commutes in it, and tightening the Budget is the only way to put a reach
/// refusal in front of the mechanism without inventing a world.
/// </para>
/// <para>
/// <b><c>rulesets/diagnosed.toml</c> rather than <c>minimal.toml</c>, for the reason task 5 built that
/// file:</b> a trail whose condition column is <c>ConditionId.None</c> in every row exercises the
/// write door and not the payload. The two files are behaviour-identical — same Lots condemned on the
/// same Ticks, asserted by <c>EvidenceDumpTests</c> — so nothing here is a measurement of the choice.
/// </para>
/// <para>
/// ⚠ <b>Build the world through the three-argument <c>World</c> constructor, which is what
/// <see cref="Start"/> does by going through <c>Replay.Start</c>.</b> The two-argument form makes a
/// <em>different city</em>, and a probe that used it while scouting this run read a Trip slot
/// high-water mark still climbing at Tick 100,000 that vanished on the correct one. Every long-run
/// test in this suite uses the same door and the difference is silent.
/// </para>
/// </remarks>
public sealed class EvidenceLongRunTests(ITestOutputHelper output)
{
    /// <summary>Sixty-five whole Days — the settle window doubled and the tail kept its length.</summary>
    /// <remarks>
    /// <para>
    /// <b>Whole Days rather than a round 100,000</b>, on <c>TrafficLongRunTests</c>' correction: the
    /// commute empties and refills the city once a Day, so a window that is not a whole number of them
    /// reads two different cities at its two ends.
    /// </para>
    /// <para>
    /// 🔴 <b>FORTY-NINE UNTIL <c>plans/0053</c>, and it moved because <see cref="SettleDays"/> did.</b>
    /// The tail is what the flatness band is computed over, and halving it would have widened the
    /// band by making each half's variance noisier — ***a longer transient must not be paid for out
    /// of the assertion's strength.*** Thirty-three Days of tail remain, which is what remained at
    /// 49 and 16.
    /// </para>
    /// </remarks>
    private const int Days = 65;

    /// <summary>The golden fixture's population.</summary>
    private const int Population = 4_000;

    /// <summary>Days discarded as the transient.</summary>
    /// <remarks>
    /// The trail fills from empty and employment climbs from nobody, so the early Days are a ramp in
    /// both quantities. <c>[jobs] revisit_ticks</c> is 1,024 and the trail saturates well inside one
    /// Day at this population.
    /// <para>
    /// 🔴 <b>Eight until milestone 17, and the two quantities above stopped being the slowest ones
    /// in the world.</b> <c>adr/0167</c> made decline a duration and <c>diagnosed.toml</c> authors
    /// two Days, so the city's blight now takes tens of Days to reach steady state rather than the
    /// two it took at a 64-Tick threshold. The reach-failure carrier count read a 3.2-sigma rise
    /// across a tail that began at Day 8 — <em>which is the transient being sampled as if it were
    /// the tail</em>, not the city changing under the instrument.
    /// </para>
    /// <para>
    /// ⚠ <b>It is taken from an independent measurement and not fitted to this test.</b> The blight
    /// census on <c>declining.toml</c> is the measurement; choosing the number from the shape of
    /// <em>this</em> statistic would be picking a window because the data has that shape, which is
    /// the one thing a tripwire must not do. Thirty-three Days of tail remain.
    /// </para>
    /// <para>
    /// 🔴 <b>SIXTEEN UNTIL <c>plans/0053</c>, AND IT WAS RE-TAKEN RATHER THAN NUDGED.</b> The census
    /// read 41% then 39% of stock derelict at 32,768 and 65,536 Ticks, so it had converged by
    /// sixteen Days. Re-run 2026-09-02 on the same file and the same command it now reads
    /// <b>27% at 32,768, 30% at 65,536 and 29% at 131,072</b> — ***still climbing at the old
    /// window*** and settled by the next one, because occupancy divides the ground: a city whose
    /// Buildings hold different numbers rehouses a demolition's occupants over more Days than one
    /// where every Building held four. 65,536 Ticks is thirty-two Days.
    /// </para>
    /// <para>
    /// ⚠ <b>The reach-failure carrier count is what reported it</b>, at a rise of 34.6 against a
    /// 3-sigma band of 29.0 — which is the transient being sampled as the tail, exactly as the
    /// paragraph above this one describes it happening the last time.
    /// </para>
    /// </remarks>
    private const int SettleDays = 32;

    /// <summary>How far above the first half of the tail the second may read, in standard errors.</summary>
    /// <remarks>
    /// <b>Three, and copied from <c>TrafficLongRunTests</c> with its <em>derivation</em> rather than
    /// its band.</b> That file's own finding is that a fixed fraction cannot be transplanted between
    /// quantities because it is an assertion about variance as much as about trend; the sigma count
    /// can be, because the spread is taken from each quantity's own halves.
    /// </remarks>
    private const double Sigmas = 3;

    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// <b>The trail does not grow, the flow that feeds it does not climb, and the one thing that does
    /// climb is the thing this milestone exists to accumulate.</b>
    /// </summary>
    [Fact]
    public void The_hundred_thousand_Tick_evidence_run()
    {
        Day[] days = Run();
        Day[] tail = days[SettleDays..];

        // Printed before anything is asserted, on 5c task 8's finding: an acceptance run that only
        // speaks on success is one you cannot use on the day it fails, and the series is the diagnosis.
        _output.WriteLine($"Days                  {days.Length} ({days.Length * Ticks.PerDay:N0} Ticks)");
        _output.WriteLine($"condemnations a Day   {Mean(tail, d => d.Condemnations):N1}");
        _output.WriteLine($"carriers              {Mean(tail, d => d.Carriers):N1}");
        _output.WriteLine($"worst history         {Mean(tail, d => d.WorstHistory):N1}");
        _output.WriteLine(
            $"aggregate, first -> last  {tail[0].Aggregated:N0} -> {tail[^1].Aggregated:N0}");

        foreach (Day day in days)
        {
            _output.WriteLine(
                $"  slots {day.Slots,4}  retained {day.Retained,4}  aggregated {day.Aggregated,7}  "
                + $"condemned {day.Condemnations,5}  carriers {day.Carriers,5}  "
                + $"worst {day.WorstHistory,5}  sum {day.TotalHistory,7}");
        }

        // The vacuity guards come first, because every assertion below is satisfied perfectly by a
        // city in which nothing was ever condemned and nobody was ever refused a job.
        Assert.True(Mean(tail, d => d.Condemnations) > 0, "nothing was ever condemned.");
        Assert.True(Mean(tail, d => d.Carriers) > 0, "nobody was ever refused a job for distance.");

        // The collection half, and 🔴 IT IS A CEILING RATHER THAN A CONSTANT since milestone 17.
        // This read `Assert.Equal(Retained + 1, day.Slots)` over every Day and called it "fixed by
        // construction, so this is a guard on the constructor" -- which was wrong about the
        // mechanism and passed anyway, because the trail reached its cap inside Day 0 while
        // condemnation took 64 Ticks. The table GROWS to its cap; it does not open at it.
        //
        // ***adr/0093 exactly: the sentence named a time when it should have named a symbol.*** A
        // 2-Day threshold means Days 0 and 1 condemn nothing, so the trail holds one slot and no
        // entries, and the "constructor guard" failed on a world doing nothing wrong.
        //
        // What adr/0006 actually claims is the ceiling, so that is what is asserted over the whole
        // run -- and equality is kept over the tail, where the trail is full and a table that had
        // started handing out fresh rows instead of recycling would show it.
        foreach (Day day in days)
        {
            Assert.True(
                day.Slots <= CondemnationTrailTable.Retained + 1,
                $"the condemnation trail reached {day.Slots} slots against a cap of "
                + $"{CondemnationTrailTable.Retained + 1}. The trail is a ring: past the cap it "
                + "aggregates rather than allocating, so a slot count above it is adr/0006.");
        }

        foreach (Day day in tail)
        {
            Assert.Equal(CondemnationTrailTable.Retained + 1, day.Slots);
        }

        foreach (Day day in tail)
        {
            Assert.Equal(CondemnationTrailTable.Retained, day.Retained);
        }

        // The magnitude half. The aggregate is exempt and is asserted monotone below instead; what
        // has to be flat is the FLOW that feeds it, because an accumulator fed at a rising rate is a
        // city changing under the instrument and one fed at a flat rate is an instrument working.
        AssertFlat(tail, d => d.Condemnations, "condemnations per Day");
        AssertFlat(tail, d => d.WorstHistory, "the longest reach-failure history in the city");
        AssertFlat(tail, d => d.Carriers, "the number of people carrying a reach-failure history");
    }

    /// <summary>
    /// <b>The aggregate climbs for ever, and that is the mechanism rather than a leak.</b>
    /// </summary>
    [Fact]
    public void The_aggregate_grows_without_bound_and_nothing_in_the_tick_reads_it()
    {
        Day[] days = Run();

        for (int i = 1; i < days.Length; i++)
        {
            Assert.True(
                days[i].Aggregated >= days[i - 1].Aggregated,
                $"the aggregate went {days[i - 1].Aggregated:N0} -> {days[i].Aggregated:N0} on Day "
                + $"{i}. A count that goes down has either wrapped or been reset, and both report a "
                + "city with un-condemned Buildings in it.");
        }

        Assert.True(
            days[^1].Aggregated > days[SettleDays].Aggregated,
            "the aggregate stopped moving, so nothing has been folded away and the exception this "
            + "test states is not being exercised.");
    }

    /// <summary>
    /// <b>A Lot whose entry has been folded away reports that it no longer knows why.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The milestone's signature claim, and a long run is the only place it is true.</b> Over a
    /// short one every condemnation is still retained in full, so <c>LotEvidence.Condemnation</c> is
    /// present for every vacant Lot that was ever built on and the decay never happens. Here the
    /// trail has folded thousands of entries into a count, so the assembler must be able to say
    /// <em>vacant, and the reason is gone</em> — which it does by returning <c>null</c> rather than by
    /// inventing a reason or by reporting the wrong Lot's.
    /// </para>
    /// <para>
    /// ⚠ <b>Both halves are asserted.</b> An assembler that returned <c>null</c> for every Lot would
    /// satisfy the decay half perfectly and would be broken; one that returned an entry for every Lot
    /// would satisfy the attribution half and would be fabricating. Both populations are checked
    /// non-empty, and every entry that <em>is</em> returned is checked against the two things a
    /// fabricated one would fail.
    /// </para>
    /// <para>
    /// <b>The timeline check is <c>CondemnationTrailTable.Aggregate</c>'s own claim, asserted for the
    /// first time here.</b> That method sets the aggregate's Tick to the newest condemnation folded
    /// into it and says in its remark that this is <em>"what makes the trail readable as a timeline:
    /// everything before this Tick is a count, everything after it is named"</em> — so a retained
    /// entry older than the aggregate is a trail that has stopped being chronological, which is the
    /// invariant every reader of it assumes and which no short run can put under strain. The
    /// condition check is the second: the aggregate row's <c>Condition</c> is never written and stays
    /// <c>ConditionId.None</c>, so an assembler that read slot 0 by an off-by-one would return
    /// entries with no reason in them on a Ruleset where every condemnation has one.
    /// </para>
    /// </remarks>
    [Fact]
    public void Attribution_decays_to_magnitude_and_the_assembler_says_so()
    {
        Simulation simulation = Start();
        World world = simulation.World;

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        Ticks folded = world.CondemnationTrail.Tick[CondemnationTrailTable.AggregateSlot];

        int withReason = 0;
        int withoutReason = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot))
            {
                continue;
            }

            LotEvidence evidence = Evidence.OfLot(world, world.Lots.Rows.At(slot));

            if (!evidence.IsVacant)
            {
                continue;
            }

            if (evidence.Condemnation is not CondemnationEvidence kept)
            {
                withoutReason++;
                continue;
            }

            withReason++;

            Assert.True(
                kept.Tick.Raw >= folded.Raw,
                $"a retained entry is dated Tick {kept.Tick.Raw:N0} against an aggregate whose "
                + $"newest folded condemnation is Tick {folded.Raw:N0}. Everything before that Tick "
                + "is supposed to be a count and everything after it named, so the trail has stopped "
                + "being a timeline.");

            Assert.NotEqual(ConditionId.None, kept.Condition);
        }

        _output.WriteLine($"vacant Lots with a reason kept: {withReason}");
        _output.WriteLine($"vacant Lots whose reason has decayed: {withoutReason}");

        Assert.True(
            world.CondemnationTrail.Condemnations[CondemnationTrailTable.AggregateSlot] > 0,
            "nothing was folded away, so the run was too short to reach the decay this asserts.");

        Assert.True(withReason > 0, "no vacant Lot kept its reason, so attribution never worked.");
        Assert.True(
            withoutReason > 0,
            "every vacant Lot still has a reason after "
            + $"{Days * Ticks.PerDay:N0} Ticks, so nothing decayed and the trail is not the "
            + $"{CondemnationTrailTable.Retained}-entry window it says it is.");
    }

    /// <summary>One whole Day of the city, in the quantities this run is about.</summary>
    private readonly record struct Day(
        int Slots,
        int Retained,
        long Aggregated,
        long Condemnations,
        int Carriers,
        int WorstHistory,
        long TotalHistory);

    private static Day[] Run()
    {
        Simulation simulation = Start();
        World world = simulation.World;

        List<Day> days = [];
        long previous = 0;

        for (int tick = 0; tick < Days * Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            if ((tick + 1) % Ticks.PerDay != 0)
            {
                continue;
            }

            CondemnationTrailTable trail = world.CondemnationTrail;
            long recorded = trail.CondemnationsRecorded();

            (int carriers, int worst, long total) = Histories(world);

            days.Add(new Day(
                trail.Rows.SlotCount,
                trail.Count,
                trail.Condemnations[CondemnationTrailTable.AggregateSlot],
                recorded - previous,
                carriers,
                worst,
                total));

            previous = recorded;
        }

        return [.. days];
    }

    /// <summary>Who carries a reach-failure history, how long the longest is, and how much there is.</summary>
    private static (int Carriers, int Worst, long Total) Histories(World world)
    {
        int carriers = 0;
        int worst = 0;
        long total = 0;

        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (!world.Citizens.Rows.IsLive(slot))
            {
                continue;
            }

            int count = world.Citizens.ReachFailures[slot];

            if (count == 0)
            {
                continue;
            }

            carriers++;
            total += count;
            worst = Math.Max(worst, count);
        }

        return (carriers, worst, total);
    }

    private static Simulation Start()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed, new WorldConfiguration(Population), GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), Rules());

        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason.
        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }

    /// <summary>
    /// The shipped diagnosis Ruleset with the Commute Budget's ceiling brought down to three minutes.
    /// </summary>
    /// <remarks>
    /// <c>ReachFailureTests.WithCeiling</c>'s shape and its reasoning, pointed at
    /// <c>diagnosed.toml</c>: an edit to a shipped file rather than a Ruleset written here, so what
    /// runs is the city this repository has. The two lower rungs go to 1 and 2 because the loader
    /// requires three strictly increasing values, which is what puts the floor under an authorable
    /// ceiling at 3.
    /// </remarks>
    private static Ruleset Rules()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "diagnosed.toml"));

        foreach ((string key, string replacement) in new[]
        {
            ("commute_fast_minutes = 20", "commute_fast_minutes = 1"),
            ("commute_moderate_minutes = 40", "commute_moderate_minutes = 2"),
            ("commute_budget_minutes = 50", "commute_budget_minutes = 3"),
        })
        {
            Assert.Contains(key, toml, StringComparison.Ordinal);
            toml = toml.Replace(key, replacement, StringComparison.Ordinal);
        }

        RulesetLoadResult result = RulesetLoader.Parse(toml, "diagnosed-tight.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>
    /// The second half of the tail is not above the first by more than the series' own noise.
    /// </summary>
    /// <remarks>
    /// <c>TrafficLongRunTests.AssertFlat</c>, and the band is derived from each half's own spread for
    /// that file's reason: a flatness band is an assertion about the quantity's variance as much as
    /// about its trend, so a fixed fraction cannot be transplanted between quantities. One-sided,
    /// because what is being refused is an upward trend and a mechanism that stopped producing is
    /// caught by the vacuity guards.
    /// </remarks>
    private static void AssertFlat(Day[] tail, Func<Day, long> of, string what)
    {
        Day[] earlyDays = tail[..(tail.Length / 2)];
        Day[] lateDays = tail[(tail.Length / 2)..];

        double early = Mean(earlyDays, of);
        double late = Mean(lateDays, of);

        double error = Math.Sqrt(
            (Variance(earlyDays, of) / earlyDays.Length) + (Variance(lateDays, of) / lateDays.Length));

        double band = (Sigmas * error) + 1;

        Assert.True(
            late - early <= band,
            $"{what} read {early:F1} over the first half of the tail and {late:F1} over the second -- "
            + $"a rise of {late - early:F1} against a {Sigmas}-sigma band of {band:F1}, from a "
            + $"standard error of {error:F1} on the difference.");
    }

    private static double Mean(Day[] days, Func<Day, long> of)
    {
        long total = 0;

        foreach (Day day in days)
        {
            total += of(day);
        }

        return (double)total / days.Length;
    }

    private static double Variance(Day[] days, Func<Day, long> of)
    {
        double mean = Mean(days, of);
        double total = 0;

        foreach (Day day in days)
        {
            double error = of(day) - mean;

            total += error * error;
        }

        return total / days.Length;
    }
}
