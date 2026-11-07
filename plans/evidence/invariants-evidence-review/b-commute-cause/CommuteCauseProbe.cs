using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.ReviewProbes;

// Review probe (sweep 03, investigator B). Not a test of record.
//
// Runs the base package's founding city and compares the cause the shell's CommuteReading
// (src/Borough.Godot/Main.Diagnosis.cs) would print for a failing commuter with the Fate the
// outbound commute that started the failing run actually had.
[Trait(Tier.Key, Tier.Instrument)]
public sealed class CommuteCauseProbe(ITestOutputHelper output)
{
    private const int TicksToRun = 20_000;

    private const int TightenAt = 6_000;

    private const int CrossingSeconds = 2_400;

    private const int InjectAt = 8_000;

    [Fact]
    public void Failing_commuter_cause_against_last_trip_fate()
    {
        string package = Path.Combine(AppContext.BaseDirectory, "Rulesets", "base");
        RulesetSourceResult source = RulesetSource.Load(Path.Combine(package, "ruleset.toml"));
        Assert.True(source.Ok);

        // A hot reload at TightenAt raises the street-crossing cost past the Commute Budget, so outbound commutes that cross a Street start failing with
        // ExceededCommuteBudget while shopping and service Trips carry on.
        string copy = Directory.CreateTempSubdirectory("borough-commute-cause").FullName;
        foreach (string file in Directory.GetFiles(package)) File.Copy(file, Path.Combine(copy, Path.GetFileName(file)));
        string city = Path.Combine(copy, "city.toml");
        string text = File.ReadAllText(city);
        const string Crossing = "crossing_seconds = 30\n";
        Assert.Contains(Crossing, text, StringComparison.Ordinal);
        File.WriteAllText(city, text.Replace(Crossing, $"crossing_seconds = {CrossingSeconds}\n", StringComparison.Ordinal));
        RulesetSourceResult tight = RulesetSource.Load(Path.Combine(copy, "ruleset.toml"));
        Directory.Delete(copy, recursive: true);
        Assert.True(tight.Ok, tight.Diagnostics.Count > 0 ? tight.Diagnostics[0].ToString() : "refused");

        InputLog log = InputLogCodec.FromText(File.ReadAllText(Path.Combine(package, "founding.borough")));
        const ulong TightHash = 0xB0B0_0000_0000_0001UL;
        Simulation simulation = Replay.Start(
            log, Borough.Core.Rules.RulesetCatalogue.Of([log.RulesetHash, TightHash], [source.Ruleset!, tight.Ruleset!]));
        World world = simulation.World;
        Assert.True(world.Rules.Jobs.DismissesFailedCommuters);

        var firstFate = new Dictionary<ulong, TripFate>();
        var everEnded = new HashSet<ulong>();
        var prevSince = new Dictionary<ulong, ulong>();
        long samples = 0, nullShown = 0, completedShown = 0, matched = 0, differs = 0;
        long hiddenWhileTravelling = 0;
        var mismatchCitizens = new HashSet<ulong>();
        var hiddenCitizens = new HashSet<ulong>();
        var failingCitizens = new HashSet<ulong>();
        var shownCounts = new SortedDictionary<string, long>();
        int examples = 0;
        long completed = 0, noRoute = 0, overBudget = 0;
        int injected = 0;

        for (int t = 0; t < TicksToRun; t++)
        {
            Ticks now = simulation.Tick;
            ulong hash = t < TightenAt ? log.RulesetHashAt(now) : TightHash;
            simulation.Step(new TickInput(log.At(now), hash));
            simulation.Employment.Drain();
            if (t == InjectAt)
            {
                // The reload above did not make outbound commutes fail in this city, so the failing
                // run is started through the same door TripEngine.Start's budget refusal uses
                // (World.ResolveTrip -> RecordTripFate) for every worker standing at home. Everything
                // after this Tick is the simulation's own behavior.
                for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
                {
                    if (!world.Citizens.Rows.IsLive(slot) || world.Citizens.Workplace[slot].IsNone) continue;
                    if ((CitizenActivity)world.Citizens.Activity[slot] != CitizenActivity.AtHome) continue;
                    world.Citizens.Activity[slot] = (byte)CitizenActivity.TravellingToWork;
                    world.RecordTripFate(slot, TripFate.ExceededCommuteBudget);
                    injected++;
                }
            }

            TripActivity trips = simulation.Trips.Drain();
            completed += trips.Completed.Sum; noRoute += trips.NoRouteFound.Sum; overBudget += trips.ExceededCommuteBudget.Sum;

            for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
            {
                if (!world.Citizens.Rows.IsLive(slot)) continue;

                ulong id = world.Citizens.Rows.IdAt(slot);
                var stored = (TripFate)world.Citizens.LastTripFate[slot];
                var activity = (CitizenActivity)world.Citizens.Activity[slot];
                ulong since = world.Citizens.CommuteFailedSince[slot].Raw;
                prevSince.TryGetValue(id, out ulong before);
                prevSince[id] = since;

                if (stored != TripFate.InFlight) everEnded.Add(id);

                if (before == 0 && since != 0) firstFate[id] = stored;

                if (activity is CitizenActivity.ShoppingTravelling or CitizenActivity.ServiceTravelling
                    && stored == TripFate.InFlight && everEnded.Contains(id))
                {
                    hiddenWhileTravelling++;
                    hiddenCitizens.Add(id);
                }

                if (since == 0) continue;

                failingCitizens.Add(id);
                samples++;
                CitizenEvidence who = Borough.Core.Evidence.Evidence.OfCitizen(world, world.Citizens.Rows.At(slot));

                // The shell's mapping, verbatim from Main.Diagnosis.CommuteReading.
                string shown = who.LastTrip?.Fate switch
                {
                    TripFate.NoRouteFound => "no route to work",
                    TripFate.ExceededCommuteBudget => "the commute exceeds the budget",
                    _ => "the commute failed",
                };
                shownCounts[shown + " | stored " + (who.LastTrip?.Fate.ToString() ?? "null")] =
                    shownCounts.GetValueOrDefault(shown + " | stored " + (who.LastTrip?.Fate.ToString() ?? "null")) + 1;

                TripFate first = firstFate.GetValueOrDefault(id, TripFate.InFlight);

                if (who.LastTrip is null) nullShown++;
                else if (who.LastTrip.Value.Fate == TripFate.Completed) completedShown++;
                else if (who.LastTrip.Value.Fate == first) matched++;
                else differs++;

                if (who.LastTrip is null || who.LastTrip.Value.Fate != first)
                {
                    mismatchCitizens.Add(id);

                    if (examples++ < 8)
                    {
                        output.WriteLine(
                            $"tick {simulation.Tick.Raw} citizen {id} activity {activity} run-start fate {first} "
                            + $"evidence LastTrip {(who.LastTrip?.Fate.ToString() ?? "null")} -> shell says \"{shown}\"");
                    }
                }
            }
        }

        output.WriteLine($"ticks run {TicksToRun}, crossing raised to {CrossingSeconds} s at tick {TightenAt}, "
            + $"in force 0x{simulation.RulesetInForce:X16}, live citizens at end {world.Citizens.Rows.LiveCount}");
        int workers = 0;
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
            if (world.Citizens.Rows.IsLive(slot) && !world.Citizens.Workplace[slot].IsNone) workers++;
        output.WriteLine($"workers at end {workers}; trips completed {completed}, no route {noRoute}, over budget {overBudget}; "
            + $"dismissal on {world.Rules.Jobs.DismissesFailedCommuters}, last reload {simulation.LastReload}");
        output.WriteLine($"injected budget refusals at tick {InjectAt}: {injected}");
        output.WriteLine($"failing-commuter samples {samples} over {failingCitizens.Count} citizens");
        output.WriteLine($"  LastTrip null {nullShown}, Completed {completedShown}, equals run-start fate {matched}, other fate {differs}");
        output.WriteLine($"  citizens with at least one mismatched sample {mismatchCitizens.Count}");
        foreach (var pair in shownCounts) output.WriteLine($"  shown: {pair.Key} x{pair.Value}");
        output.WriteLine(
            $"shopping/service-travelling samples whose evidence LastTrip is null after an earlier ended Trip "
            + $"{hiddenWhileTravelling} over {hiddenCitizens.Count} citizens");
    }
}
