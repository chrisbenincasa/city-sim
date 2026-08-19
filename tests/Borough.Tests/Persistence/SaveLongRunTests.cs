using System.Diagnostics;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Headless;
using Borough.Tests.Golden;
using Xunit.Abstractions;

namespace Borough.Tests.Persistence;

/// <summary>
/// Milestone 8 task 9 — the long acceptance run, with saves in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three claims, and only the first is about the format.</b> A save taken every Day for forty-nine
/// Days <em>changes nothing</em>; the machinery that takes it <em>accumulates nothing</em>; and the
/// last one <em>reloads into the run it came from</em> and carries on agreeing. <b>The last is the one
/// no shorter test can make</b> — <c>FactorioTests</c> saves at Ticks 0 to 256, where every derived
/// structure is close to the state it was built in, and ***a rebuild that is right at Tick 256 is not
/// thereby right at Tick 100,352***.
/// </para>
/// <para>
/// ⚠ <b>It runs over whole Days rather than a round 100,000 Ticks, on <c>TrafficLongRunTests</c>'
/// correction.</b> The city this Ruleset builds is periodic in the Day — the commute empties it and
/// refills it — so a run of 100,000 Ticks ends part-way through one and its last reading is taken at
/// an arbitrary point in a cycle. Forty-nine Days is <b>100,352</b> Ticks, the first multiple of the
/// Day above 100,000.
/// </para>
/// <para>
/// ⚠ <b>The two costs are reported as two numbers and not one, and they go to opposite sides of D4's
/// seam.</b> The <b>copy</b> is what the simulation thread must pay and what <c>adr/0087</c> predicts
/// at ~10 ms at 1,000,000 Citizens; the <b>write</b> is the half a background thread would take. A
/// single combined figure would read as a refutation of that prediction when it is not one, and would
/// tell nobody what the eventual thread is worth. ⚠ <b>They are <em>not</em> <c>copy</c> against
/// <c>hash + serialise</c>, which is how <c>plans/0030</c> stated it</b>: task 6 found
/// <c>adr/0087</c>'s hash clause unbuildable — a fold over the copy's bytes is not the State Hash —
/// so the file carries no hash and the seam is back at <c>copy | write</c>, which is that ADR's own
/// shape table.
/// </para>
/// <para>
/// ⚠ <b>The timings are taken outside the Tick and the correctness is asserted inside it.</b>
/// <c>Borough.Core</c> may not read a clock (<c>05 §4</c>), so a duration cannot be measured from
/// where the save is taken — but a copy costs what it costs wherever it is called from, and D4 made
/// the seam a function boundary precisely so that this is true. What must be measured at the real
/// door is that the save is an observer, and the two tests here split that between them.
/// </para>
/// </remarks>
public sealed class SaveLongRunTests(ITestOutputHelper output)
{
    /// <summary>Forty-nine whole Days, which is the first multiple of the Day above 100,000 Ticks.</summary>
    private const int Days = 49;

    /// <summary>
    /// The Ruleset in force, as the header records it. Any value: nothing here resolves it.
    /// </summary>
    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// <b>Saving changes nothing, the machinery accumulates nothing, and the last save reloads into
    /// the run it came from.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The observer check is exact and costs one world.</b> The city's own State Hash is taken
    /// either side of the write, every Day, for forty-nine of them — so a writer that mutated
    /// anything it walked fails on the Day it did. Every other test in this namespace saves a city
    /// and then stops looking at it. ⚠ <b>The other half — that
    /// <c>Simulation.SaveAtEndOfTick</c>'s bookkeeping inside <c>Step</c> perturbs nothing — needs a
    /// second world stepped in lockstep, and that is
    /// <see cref="A_city_saved_every_day_is_the_city_that_never_saved"/> over four Days rather than
    /// forty-nine: it fails on the first save or not at all.
    /// </para>
    /// <para>
    /// ⚠ <b>The buffer's growth is the <c>adr/0006</c> half, and it is asserted as a bound rather than
    /// as a trend.</b> <c>WorldSnapshot</c> keeps its buffer between saves and grows it by doubling, so
    /// the honest claim is not <em>it stops growing</em> — a city that gains Lots makes a bigger file
    /// and must be allowed to — but that it grows <b>with the file and not with the number of
    /// saves</b>. Forty-nine saves of a city whose size oscillates is what separates those.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_hundred_thousand_Tick_save_run()
    {
        Ruleset rules = GoldenFixtures.Rules();

        (World world, Simulation run) = Fresh(rules);

        string path = Path.Combine(
            Path.GetTempPath(), $"borough-save-long-run-{Environment.ProcessId}.borosave");

        var snapshot = new WorldSnapshot();
        List<Day> days = [];

        try
        {
            for (int day = 0; day < Days; day++)
            {
                for (int tick = 0; tick < Ticks.PerDay; tick++)
                {
                    run.Step(default);
                }

                // Through the real door, on the next Tick, so the run exercises what a host would
                // call and not only what a test can reach.
                var file = new MemorySave();
                run.SaveAtEndOfTick(file);
                run.Step(default);

                Assert.NotEmpty(file.Bytes);

                // ⚠ The observer check, and it is exact rather than statistical: the world's own hash
                // across the write. Every Day, for forty-nine of them, on a city whose free lists are
                // fragmenting the whole time.
                ulong before = world.HashState();
                Day reading = Measure(day, world, snapshot, path, file.Bytes.Length);

                Assert.Equal(before, world.HashState());

                days.Add(reading);
            }

            Report(days, world);

            // The magnitude half. The buffer may grow with the file and must not grow with the number
            // of saves -- so the largest file the run produced is what bounds it, and a doubling
            // allocator is allowed its last doubling on top.
            int widest = days.Max(d => d.Bytes);

            Assert.True(
                snapshot.Capacity <= widest * 2,
                $"the snapshot holds {snapshot.Capacity:N0} bytes against a largest file of "
                + $"{widest:N0}: it is growing with the saves rather than with the city.");

            // ⚠ A slot-exact save cannot shrink, and that is structural rather than incidental. The
            // file's size is SlotCount and not LiveCount, slot counts are allocator high-water marks,
            // and adr/0086 forbids compaction -- so a file that got smaller would mean dead slots had
            // been dropped, which is the one thing that decision refuses by name. This is the only
            // assertion available over the series that is not fitted to what the series happened to
            // do: WHERE it saturates is a property of the run's length, THAT it never falls is a
            // property of the format.
            for (int i = 1; i < days.Count; i++)
            {
                Assert.True(
                    days[i].Bytes >= days[i - 1].Bytes,
                    $"the file shrank on Day {days[i].Index}, {days[i - 1].Bytes:N0} -> "
                    + $"{days[i].Bytes:N0}: something compacted.");
            }

            // And the third claim: the last save is the run, and running on keeps agreeing.
            AssertReloadsIntoTheRun(path, rules, world, run);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// ⚠ <b>A city saved every Day is the same city as one that never saved, through the real
    /// door.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Short on purpose, and it is the half the long run cannot afford.</b> Two worlds stepped in
    /// lockstep is twice the work, and over forty-nine Days that is twelve minutes against six — so
    /// the long run asserts the writer is an observer by hashing <em>across the write</em>, which is
    /// exact and needs one world, and this asserts the other half: that
    /// <c>Simulation.SaveAtEndOfTick</c>'s own bookkeeping inside <c>Step</c> perturbs nothing either.
    /// </para>
    /// <para>
    /// ⚠ <b>The split is a claim about where each defect would show, not a convenience.</b> A writer
    /// that mutated the world would show at any length and is caught above at every one of
    /// forty-nine Days; a <em>Step</em> that consumed a random draw or advanced a counter would show
    /// on the first save and never need a long run. ***A property that fails immediately does not
    /// need a hundred thousand Ticks to fail in.***
    /// </para>
    /// </remarks>
    [Fact]
    public void A_city_saved_every_day_is_the_city_that_never_saved()
    {
        Ruleset rules = GoldenFixtures.Rules();

        (World control, Simulation controlRun) = Fresh(rules);
        (World subject, Simulation subjectRun) = Fresh(rules);

        for (int day = 0; day < 4; day++)
        {
            for (int tick = 0; tick < Ticks.PerDay; tick++)
            {
                controlRun.Step(default);
                subjectRun.Step(default);

                Assert.Equal(control.HashState(), subject.HashState());
            }

            var file = new MemorySave();
            subjectRun.SaveAtEndOfTick(file);

            controlRun.Step(default);
            subjectRun.Step(default);

            Assert.Equal(control.HashState(), subject.HashState());
            Assert.NotEmpty(file.Bytes);
        }
    }

    /// <summary>
    /// Takes the two costs and the file, at a Day boundary, outside the Tick. See the class remark.
    /// </summary>
    private static Day Measure(
        int day, World world, WorldSnapshot snapshot, string path, int throughTheDoor)
    {
        snapshot.Reset();

        var copy = Stopwatch.StartNew();
        SaveFile.Write(world, InForce, snapshot);
        copy.Stop();

        using var stream = File.Create(path);
        var sink = new SaveSink(stream);

        var write = Stopwatch.StartNew();
        snapshot.DrainTo(sink);
        write.Stop();

        stream.Flush();

        return new Day(
            day,
            snapshot.Length,
            copy.Elapsed.TotalMilliseconds,
            write.Elapsed.TotalMilliseconds,
            world.Lots.Rows.LiveCount,
            world.Buildings.Rows.LiveCount,
            throughTheDoor);
    }

    /// <summary>
    /// The Factorio test at the far end of a hundred thousand Ticks.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It is not a repeat of <c>FactorioTests</c> and the difference is the only reason it is
    /// here.</b> That suite saves at Ticks 0 to 256, where every derived structure is close to the
    /// state it was built in. This one saves a city that has been demolishing and rebuilding for
    /// forty-nine Days, whose free lists are fragmented, whose ids are far from their slots and whose
    /// Event Wheel has been round the clock forty-nine times. ***A rebuild that is right at Tick 256
    /// is not thereby right at Tick 100,352***, and nothing shorter than this run can say so.
    /// </remarks>
    private void AssertReloadsIntoTheRun(
        string path, Ruleset rules, World world, Simulation run)
    {
        World reloaded;
        SaveHeader header;

        using (var stream = File.OpenRead(path))
        {
            reloaded = SaveFile.Read(new SaveSource(stream), rules, out header);
        }

        Assert.Equal(world.HashState(), reloaded.HashState());

        var resumed = new Simulation(reloaded, header.Key)
        {
        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        VerifyDecideWritesNothing = false,
        };

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            run.Step(default);
            resumed.Step(default);

            Assert.Equal(world.HashState(), reloaded.HashState());
        }

        _output.WriteLine(
            $"reloaded and ran a further Day to Tick {run.Tick.Raw}, {world.HashState():X16}");
    }

    /// <summary>
    /// Prints the series before anything is asserted, and the size against what the declaration says
    /// it should be.
    /// </summary>
    /// <remarks>
    /// <b>Printed first, because a long run that fails on its own first assertion otherwise reports a
    /// number and withholds the series it came out of</b> — 5c task 8's finding, and the series is the
    /// whole diagnosis.
    /// </remarks>
    private void Report(List<Day> days, World world)
    {
        int declared = SaveHeader.Bytes;

        foreach (Rows table in world.Tables)
        {
            declared += 20 + (table.SavedBytesPerRow * table.SlotCount);
        }

        Day last = days[^1];

        _output.WriteLine($"Days                 {days.Count} ({days.Count * Ticks.PerDay:N0} Ticks)");
        _output.WriteLine($"saves                {days.Count}");
        _output.WriteLine($"file, first -> last  {days[0].Bytes:N0} -> {last.Bytes:N0} bytes");
        _output.WriteLine($"declared total       {declared:N0} bytes (field declaration, task 2)");
        _output.WriteLine($"copy                 {Mean(days, d => d.CopyMs):F2} ms mean, "
            + $"{days.Max(d => d.CopyMs):F2} ms worst");
        _output.WriteLine($"write                {Mean(days, d => d.WriteMs):F2} ms mean, "
            + $"{days.Max(d => d.WriteMs):F2} ms worst");

        // ⚠ A rate rather than a duration, because a duration taken over a 604 KB file says nothing
        // about a 131.33 MiB one and would be quoted as though it did (plans/0012 Cause 5). What
        // this is a measurement OF is the copy's bandwidth on this machine and this build.
        _output.WriteLine(
            $"copy rate            {last.Bytes / Mean(days, d => d.CopyMs) / 1e6:F2} GB/s "
            + "(Debug, and this file fits in cache where a 1M one will not)");
        _output.WriteLine(
            $"write rate           {last.Bytes / Mean(days, d => d.WriteMs) / 1e6:F2} GB/s "
            + "(to the page cache, not to a platter)");
        _output.WriteLine(string.Empty);

        foreach (Day day in days)
        {
            _output.WriteLine(
                $"  day {day.Index,3}  {day.Bytes,9:N0} B  copy {day.CopyMs,6:F2} ms  "
                + $"write {day.WriteMs,6:F2} ms  lots {day.Lots,5}  buildings {day.Buildings,5}");
        }

        // The size the declaration implies IS the file, at every Day of the run -- which is
        // SaveFileTests' assertion at the scale it is worth restating, because the declaration is the
        // format and a divergence here would mean the two had come apart under churn.
        Assert.Equal(declared, last.Bytes);
        Assert.Equal(last.Bytes, last.ThroughTheDoor);
    }

    private static double Mean(List<Day> days, Func<Day, double> of) => days.Average(of);

    private static (World World, Simulation Simulation) Fresh(Ruleset rules)
    {
        var key = WorldKey.FromSeed(GoldenFixtures.Seed);
        var world = new World(GoldenFixtures.Population, rules, key);
        var simulation = new Simulation(world, key)
        {
        // O(world) twice per Tick against a phase meant to be O(woken). --no-decide-guard's reason,
        // and the guard's own correctness is covered by the tests written for it.
        VerifyDecideWritesNothing = false,
        };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        return (world, simulation);
    }

    /// <summary>One Day's reading.</summary>
    private readonly record struct Day(
        int Index,
        int Bytes,
        double CopyMs,
        double WriteMs,
        int Lots,
        int Buildings,
        int ThroughTheDoor);
}
