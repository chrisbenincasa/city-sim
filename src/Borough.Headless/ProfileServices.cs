using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Rules;

namespace Borough.Headless;

internal static class ProfileServices
{
    internal static bool Place(World world, WorldKey key, TextWriter output)
    {
        if (!world.Rules.Care.Runs || !world.Rules.School.Runs)
        { output.WriteLine("--profile-services needs both [school] and [care]."); return false; }
        var vacant = Enumerable.Range(0, world.Lots.Rows.SlotCount)
            .Where(l => world.Lots.Rows.IsLive(l) && world.Lots.IsVacant(l)).ToArray();
        int population = world.Citizens.Rows.LiveCount;
        int placed = 0;
        for (byte kind = 1; kind <= world.Rules.KindCount; kind++)
        {
            var definition = world.Rules.Kind(kind);
            // PROVISIONAL fixture sizing, reported with each capture; not production coverage.
            int per = definition.Serves == Need.Education ? 1000
                : definition.Serves == Need.Health ? definition.BedPercent > 0 ? 5000 : 500 : 0;
            if (per == 0) { continue; }
            int requested = Math.Max(1, (population + per - 1) / per);
            for (int n = 0; n < requested; n++)
            {
                int begin = (int)((long)n * vacant.Length / requested);
                int lot = -1;
                for (int j = 0; j < vacant.Length; j++)
                {
                    int candidate = vacant[(begin + j) % vacant.Length];
                    if (world.Lots.IsVacant(candidate)) { lot = candidate; break; }
                }
                if (lot < 0) { output.WriteLine("Insufficient vacant Lots for profiling services."); return false; }
                world.CreateBuilding(world.Lots.Rows.At(lot), kind, world.Tick, key);
                placed++;
            }
            output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "services",
                kind,
                serves = definition.Serves.ToString(),
                definition.BedPercent,
                requested
            }));
        }
        return placed > 0;
    }
    internal static object Read(Simulation sim) => new
    {
        type = "civic",
        tick = sim.World.Tick.Raw,
        care = sim.Civic.Read()
    };
}
