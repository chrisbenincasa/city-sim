using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
namespace Borough.Headless;

internal static class CareDump
{
    internal static int Run(Options options, TextWriter output)
    {
        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out _) || !rules.Care.Runs)
        { output.WriteLine("--care needs a Ruleset declaring [care]."); return 2; }
        var key = WorldKey.FromSeed(options.Seed);
        var world = new World(options.Citizens, rules, key);
        var sim = new Simulation(world, key) { VerifyDecideWritesNothing = options.DecideGuard };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            KindDefinition definition = rules.Kind(kind);
            int count = definition.Serves == Need.Education ? options.Schools
                : definition.Serves == Need.Health ? definition.BedPercent > 0 ? options.Hospitals : options.Clinics : 0;
            for (int n = 0; n < count; n++)
            {
                int lot = -1;
                for (int l = 0; l < world.Lots.Rows.SlotCount; l++)
                    if (world.Lots.Rows.IsLive(l) && world.Lots.IsVacant(l)) { lot = l; break; }
                if (lot < 0) { output.WriteLine("No vacant Lot for requested service."); return 2; }
                world.CreateBuilding(world.Lots.Rows.At(lot), kind, world.Tick, key);
                output.WriteLine($"Placed kind {kind} at {world.Lots.East[lot].Raw} {world.Lots.North[lot].Raw}");
            }
        }
        output.WriteLine("tick sick serious waiting booked treating awaiting-bed inpatient beds treatment-places deaths-awaiting-bed-in-report-window missed-work-ticks lost-earnings");
        for (ulong t = 0; t < options.Ticks; t++)
        {
            sim.Step(default);
            if ((t + 1) % Ticks.PerDay != 0 && t + 1 != options.Ticks) { continue; }
            CareReading r = sim.Civic.Read();
            output.WriteLine($"{world.Tick.Raw} {r.Sick} {r.Serious} {r.Waiting} {r.Booked} {r.Treating} {r.AwaitingBed} {r.Inpatient} {r.Beds} {r.TreatmentPlaces} {r.DeathsAwaitingBed} {r.MissedTicks} {r.LostWages}");
        }
        output.WriteLine("Retained care events: tick citizen household facility event severity value");
        var history = world.CareHistory;
        foreach (int e in Enumerable.Range(0, history.Rows.SlotCount).Where(history.Rows.IsLive).OrderBy(e => history.Rows.IdAt(e)))
            output.WriteLine($"{history.Tick[e].Raw} {history.CitizenId[e]} {history.HouseholdId[e]} {history.BuildingId[e]} {(CareEventKind)history.Event[e]} {history.Severity[e]} {history.Value[e]}");
        sim.CheckEndOfRun(); return 0;
    }
}
