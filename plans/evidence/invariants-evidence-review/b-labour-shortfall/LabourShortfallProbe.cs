using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Xunit.Abstractions;

namespace Borough.Tests.ReviewProbes;

// Review probe (sweep 03, investigator B). Not a test of record.
//
// In the base founding city, counts Rule Instances blocked on a Labour Bin. RuleEngine.Stop treats
// these as a closed shift and clears StarvedSince, but the evidence counts them as supply shortfalls
// and the inspector prints "Current shortfall since Tick 0" for each (Main.Information.cs).
[Trait(Tier.Key, Tier.Instrument)]
public sealed class LabourShortfallProbe(ITestOutputHelper output)
{
    private const int TicksToRun = 20_000;

    [Fact]
    public void Labour_blocks_read_as_shortfalls_since_tick_zero()
    {
        string package = Path.Combine(AppContext.BaseDirectory, "Rulesets", "base");
        RulesetSourceResult source = RulesetSource.Load(Path.Combine(package, "ruleset.toml"));
        Assert.True(source.Ok);
        InputLog log = InputLogCodec.FromText(File.ReadAllText(Path.Combine(package, "founding.borough")));
        Simulation simulation = Replay.Start(log, source.Ruleset!);
        World world = simulation.World;

        long labourSamples = 0, labourSinceZero = 0, otherSupply = 0, buildingsShowingShortfall = 0;
        long cityLabourSubjects = 0, cityTroubleSubjects = 0;
        int examples = 0;

        for (int t = 0; t < TicksToRun; t++)
        {
            Ticks now = simulation.Tick;
            simulation.Step(new TickInput(log.At(now), log.RulesetHashAt(now)));
            simulation.Employment.Drain();
            simulation.Trips.Drain();

            if (t % 256 != 255) continue;

            for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
            {
                if (!world.Buildings.Rows.IsLive(slot)) continue;
                Handle<Building> building = world.Buildings.Rows.At(slot);
                bool labourOnly = false, anySupply = false;

                foreach (int instance in world.BuildingRules.Walk(slot))
                {
                    if ((Blocking)world.RuleInstances.Blocked[instance] != Blocking.Supply) continue;
                    if (!world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[instance], out int bin)) continue;
                    bool labour = world.Rules.Family(world.Bins.Resource[bin]) == ResourceFamily.Labour;
                    if (labour)
                    {
                        labourSamples++;
                        labourOnly = true;
                        if (world.RuleInstances.StarvedSince[instance] == Ticks.Zero) labourSinceZero++;
                    }
                    else { otherSupply++; anySupply = true; }
                }

                if (labourOnly && !anySupply)
                {
                    SupplyEvidence supply = Borough.Core.Evidence.Evidence.SupplyOfBuilding(world, building);
                    if (supply.Shortfalls > 0)
                    {
                        buildingsShowingShortfall++;
                        if (examples++ < 4)
                        {
                            RuleEvidence primary = supply.Primary;
                            output.WriteLine(
                                $"tick {world.Tick.Raw} building {world.Buildings.Rows.IdAt(slot)}: Shortfalls {supply.Shortfalls}, "
                                + $"primary waits for resource {primary.WaitingFor.Raw} (Labour), StarvedSince {primary.StarvedSince.Raw}, "
                                + $"MissedFirings {primary.MissedFirings} -> inspector: \"Current shortfall since Tick {primary.StarvedSince.Raw} · {primary.MissedFirings:N0} missed firings\"");
                        }
                    }
                }
            }

            CityEvidence city = Borough.Core.Evidence.Evidence.OfCity(world);
            foreach (CitySubject subject in city.Subjects.Span)
            {
                CityCause cause = city.Groups.Span[subject.Group].Cause;
                if (cause.Blocked != Blocking.Supply) continue;
                cityTroubleSubjects++;
                if (cause.WaitingFor.Raw != 0 && world.Rules.Family(cause.WaitingFor) == ResourceFamily.Labour) cityLabourSubjects++;
            }
        }

        output.WriteLine($"samples every 256 Ticks over {TicksToRun} Ticks");
        output.WriteLine($"labour-blocked Rule Instance samples {labourSamples}, of which StarvedSince == 0: {labourSinceZero}");
        output.WriteLine($"non-labour supply-blocked samples {otherSupply}");
        output.WriteLine($"Building samples blocked only on labour whose SupplyEvidence reports Shortfalls > 0: {buildingsShowingShortfall}");
        output.WriteLine($"City Evidence supply-trouble subjects {cityTroubleSubjects}, of which waiting for Labour {cityLabourSubjects}");
    }
}
