using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

using Xunit.Abstractions;

namespace Borough.Tests.Movement;

/// <summary>
/// Counts failed-commute dismissals in the base package's founding city, which grows through its gate
/// with every Household owning a car and no connection ever broken (<c>plans/decline-recovery.md</c>).
/// </summary>
[Trait(Tier.Key, Tier.Instrument)]
public sealed class ParkingDismissalProbe(ITestOutputHelper output)
{
    private const int Days = 120;
    private const int ReportEvery = 5;
    private const int Window = 30;

    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// Runs the founding city with <c>[capacity] floor_tiles_per_parking_space</c> at
    /// <paramref name="floorTilesPerSpace"/>. The package ships 6; larger values supply fewer spaces.
    /// </summary>
    [Theory]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(48)]
    public void Founding_city_grows_for_120_days(int floorTilesPerSpace)
    {
        string package = Path.Combine(AppContext.BaseDirectory, "Rulesets", "base");
        string copy = Directory.CreateTempSubdirectory("borough-parking").FullName;

        foreach (string file in Directory.GetFiles(package))
        {
            File.Copy(file, Path.Combine(copy, Path.GetFileName(file)));
        }

        string city = Path.Combine(copy, "city.toml");
        string text = File.ReadAllText(city);
        Assert.Contains("floor_tiles_per_parking_space = 6\n", text, StringComparison.Ordinal);
        File.WriteAllText(city, text.Replace(
            "floor_tiles_per_parking_space = 6\n",
            $"floor_tiles_per_parking_space = {floorTilesPerSpace}\n",
            StringComparison.Ordinal));

        RulesetSourceResult source = RulesetSource.Load(Path.Combine(copy, "ruleset.toml"));
        Directory.Delete(copy, recursive: true);
        Assert.True(source.Ok, source.Diagnostics.Count > 0 ? source.Diagnostics[0].ToString() : "refused");

        InputLog log = InputLogCodec.FromText(
            File.ReadAllText(Path.Combine(package, "founding.borough")));
        Simulation simulation = Replay.Start(log, source.Ruleset!);
        World world = simulation.World;

        Assert.True(world.Rules.Jobs.DismissesFailedCommuters, "the base package no longer dismisses.");

        long completed = 0, noRoute = 0, overBudget = 0;
        int countedFrom = 0;
        long dismissedTotal = 0, workerDays = 0;
        var dismissedByDay = new long[Days];
        var workersByDay = new long[Days];

        _output.WriteLine($"floor_tiles_per_parking_space = {floorTilesPerSpace}");
        _output.WriteLine(
            "day\tcitizens\tworkers\tclocks\tdismissed\tcounted days\tover budget\tno route\tcompleted\tspaces\theld\tfull parks");

        for (int day = 0; day < Days; day++)
        {
            for (int tick = 0; tick < Ticks.PerDay; tick++)
            {
                Ticks now = simulation.Tick;
                simulation.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
                simulation.Employment.Drain();

                TripActivity trips = simulation.Trips.Drain();
                completed += trips.Completed.Sum;
                noRoute += trips.NoRouteFound.Sum;
                overBudget += trips.ExceededCommuteBudget.Sum;
            }

            int citizens = 0, workers = 0, clocks = 0, dismissed = 0;

            for (int citizen = 0; citizen < world.Citizens.Rows.SlotCount; citizen++)
            {
                if (!world.Citizens.Rows.IsLive(citizen)) continue;

                citizens++;
                if (!world.Citizens.Workplace[citizen].IsNone) workers++;
                if (world.Citizens.CommuteFailedSince[citizen] != Ticks.Zero) clocks++;

                if (world.Citizens.CommuteEpisodeOutcome[citizen] == (byte)CommuteEpisodeOutcome.Dismissed
                    && world.Citizens.CommuteEpisodeEndDay[citizen] == day)
                {
                    dismissed++;
                }
            }

            long spaces = 0, held = 0;
            int full = 0, parks = 0;

            for (int park = 0; park < world.CarParks.Rows.SlotCount; park++)
            {
                if (!world.CarParks.Rows.IsLive(park)) continue;

                parks++;
                spaces += world.CarParks.Capacity[park];
                held += world.CarParks.Occupied[park];
                if (world.CarParks.SpaceAt(park) <= 0) full++;
            }

            dismissedByDay[day] = dismissed;
            workersByDay[day] = workers;
            dismissedTotal += dismissed;
            workerDays += workers;

            if ((day + 1) % ReportEvery == 0 || dismissed > 0)
            {
                _output.WriteLine(
                    $"{day + 1}\t{citizens}\t{workers}\t{clocks}\t{dismissed}\t{day + 1 - countedFrom}\t{overBudget}\t{noRoute}\t{completed}\t{spaces}\t{held}\t{full}/{parks}");
                completed = noRoute = overBudget = 0;
                countedFrom = day + 1;
            }
        }

        for (int start = 0; start + Window <= Days; start += Window)
        {
            long lost = 0, exposure = 0;

            for (int day = start; day < start + Window; day++)
            {
                lost += dismissedByDay[day];
                exposure += workersByDay[day];
            }

            long meanWorkers = exposure / Window;
            _output.WriteLine(
                $"Days {start + 1}-{start + Window}: {lost} dismissed, mean {meanWorkers} workers, "
                + $"{(meanWorkers == 0 ? 0 : lost * 1000 / meanWorkers)} per thousand workers per {Window} Days");
        }

        _output.WriteLine($"total dismissed {dismissedTotal} over {Days} Days, {workerDays} worker-Days");
    }
}
