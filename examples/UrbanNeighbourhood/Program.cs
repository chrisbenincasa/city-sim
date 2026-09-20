using Borough.Examples;
using Borough.Formats;
using Borough.Shell;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project examples/UrbanNeighbourhood -- RULESET OUTPUT.borough-city");
    return 1;
}
RulesetCaptureResult captured = RulesetCapture.Read(args[0]);
if (!captured.Ok)
{
    foreach (RulesetDiagnostic diagnostic in captured.Diagnostics) Console.Error.WriteLine(diagnostic);
    return 1;
}
var world = UrbanNeighbourhood.Create(File.ReadAllText(args[0]));
CitySave.Write(args[1], world, captured.Capture!, UrbanNeighbourhood.Seed);
Console.WriteLine($"Saved {Path.GetFullPath(args[1])}: Tick {world.Tick.Raw}, {world.Citizens.Rows.LiveCount} Citizens, {world.UnplacedPool.Count} seeking Households.");
return 0;
