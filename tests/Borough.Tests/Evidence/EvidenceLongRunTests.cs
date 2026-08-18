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
    /// <summary>Forty-nine whole Days, which is the first multiple of the Day above 100,000 Ticks.</summary>
    /// <remarks>
    /// <b>Whole Days rather than a round 100,000</b>, on <c>TrafficLongRunTests</c>' correction: the
    /// commute empties and refills the city once a Day, so a window that is not a whole number of them
    /// reads two different cities at its two ends.
    /// </remarks>
    private const int Days = 49;

    /// <summary>The golden fixture's population.</summary>
    private const int Population = 4_000;

    /// <summary>Days discarded as the transient.</summary>
    /// <remarks>
    /// The trail fills from empty and employment climbs from nobody, so the early Days are a ramp in
    /// both quantities. <c>[jobs] revisit_ticks</c> is 1,024 and the trail saturates well inside one
    /// Day at this population; eight is generous for both and leaves forty-one Days of tail.
    /// </remarks>
    private const int SettleDays = 8;

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

        // The collection half. Fixed by construction, so this is a guard on the constructor.
        foreach (Day day in days)
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
