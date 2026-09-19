using Borough.Examples;
using Borough.Shell;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project examples/UrbanNeighbourhood -- RULESET OUTPUT.borough-city");
    return 1;
}
string toml = File.ReadAllText(args[0]);
var world = UrbanNeighbourhood.Create(toml);
CitySave.Write(args[1], world, toml, UrbanNeighbourhood.Seed);
Console.WriteLine($"Saved {Path.GetFullPath(args[1])}: Tick {world.Tick.Raw}, {world.Citizens.Rows.LiveCount} Citizens, {world.UnplacedPool.Count} seeking Households.");
return 0;
