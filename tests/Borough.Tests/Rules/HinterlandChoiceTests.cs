using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0045</c> row 28, task 3: the Outside as an ordinary row in 02 section 5.4's comparison.
/// </summary>
/// <remarks>
/// <para>
/// <b>adr/0023's first line is what is under test — <i>there is no immigration rate</i>.</b> Before
/// this, <c>Simulation.ApplyArrive</c> admitted whoever knocked until the door's daily ceiling
/// bound, so the only number deciding how many people wanted to live here was
/// <c>[[building]] arrivals_per_day</c>. What decides now is a comparison, and the ceiling is back
/// to being the width of a door.
/// </para>
/// <para>
/// ⚠ <b>Every assertion here is about ORDERING between the four edges rather than about a level.</b>
/// adr/0023's own argument for four Hinterlands is that a single anchor has no referent and
/// <i>four comparable markets are each other's referent</i>; a test asserting that the north sends
/// <em>n</em> Households would be asserting the thing that ADR says nobody can defend.
/// </para>
/// </remarks>
public sealed class HinterlandChoiceTests
{
    private const int Citizens = 1_000;

    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Welcomed()
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "welcomed.toml"));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"welcomed.toml was refused:\n{result.Describe()}");
    }

    /// <summary>
    /// Knocks at every gate every Day and counts who crossed, per edge.
    /// </summary>
    /// <remarks>
    /// <b>It asks for more than any door can take</b>, which is <c>ArrivalDump</c>'s discipline: a
    /// demonstration that chose its own rate would be showing the demonstration. What varies between
    /// the four columns is therefore the city and the Hinterland, never the asking.
    /// </remarks>
    private static Dictionary<MapEdge, int> Crossings(Ruleset rules, ulong ticks)
    {
        World world = new(Citizens, rules, Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var gates = new List<(int Slot, int Lot, MapEdge Edge)>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot])
                || !world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot))
            {
                continue;
            }

            gates.Add((slot, lot, world.EdgeOf(lot)));
        }

        Assert.NotEmpty(gates);

        var crossed = new Dictionary<MapEdge, int>();
        var before = new Dictionary<MapEdge, int>();

        foreach ((_, _, MapEdge edge) in gates)
        {
            crossed[edge] = 0;
        }

        for (ulong tick = 0; tick < ticks; tick++)
        {
            if (tick % Ticks.PerDay != 0)
            {
                simulation.Step(default);
                continue;
            }

            foreach ((int slot, int lot, MapEdge edge) in gates)
            {
                before[edge] = world.Buildings.ArrivalsToday[slot];
            }

            var commands = new Command[gates.Count];

            for (int i = 0; i < gates.Count; i++)
            {
                commands[i] = new Command(
                    CommandKind.Arrive,
                    world.Lots.East[gates[i].Lot],
                    world.Lots.North[gates[i].Lot],
                    new ArrivePayload(Households: 200, LifeStage: 0, Citizens: 2).Encode());
            }

            simulation.Step(new TickInput(commands, 0));

            foreach ((int slot, _, MapEdge edge) in gates)
            {
                crossed[edge] += world.Buildings.ArrivalsToday[slot] - before[edge];
            }
        }

        return crossed;
    }

    /// <summary>
    /// Households in the Pool that were shown homes and preferred the Outside, over a run.
    /// </summary>
    /// <remarks>
    /// <b>The gates have to be knocked on for this to measure anything.</b> <c>SyntheticCity</c>
    /// houses everybody it makes, so a world nobody arrives at has an empty Pool and
    /// <c>TryHouse</c> never runs — which is the trap <c>rulesets/choosy.toml</c>'s header records
    /// finding the hard way.
    /// </remarks>
    private static long Declines(Ruleset rules, ulong ticks)
    {
        World world = new(Citizens, rules, Key);
        Simulation simulation = new(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var gates = new List<int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                gates.Add(slot);
            }
        }

        long declined = 0;

        for (ulong tick = 0; tick < ticks; tick++)
        {
            TickInput input = default;

            if (tick % Ticks.PerDay == 0)
            {
                var commands = new Command[gates.Count];

                for (int i = 0; i < gates.Count; i++)
                {
                    int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[gates[i]]);

                    commands[i] = new Command(
                        CommandKind.Arrive,
                        world.Lots.East[lot],
                        world.Lots.North[lot],
                        new ArrivePayload(Households: 200, LifeStage: 0, Citizens: 2).Encode());
                }

                input = new TickInput(commands, 0);
            }

            simulation.Step(input);
            declined += simulation.Placement.Drain().Declined.Sum;
        }

        return declined;
    }

    /// <summary>
    /// The dearest Outside sends more people than the cheapest one, over the same asking.
    /// </summary>
    /// <remarks>
    /// <b>The whole of adr/0023's <i>the willing are taken first</i>, minus the stock.</b>
    /// <c>welcomed.toml</c> prices the east at 300 a Day and the north at 1,500, and a home here
    /// costs 400 — so the east has little to gain by leaving and the north has a great deal. Nothing
    /// anywhere states how many people want to come.
    /// </remarks>
    [Fact]
    public void A_dearer_outside_sends_more_people_than_a_cheaper_one()
    {
        Dictionary<MapEdge, int> crossed = Crossings(Welcomed(), 8 * Ticks.PerDay);

        Assert.True(
            crossed[MapEdge.North] > crossed[MapEdge.East],
            $"the north (rent 1,500) sent {crossed[MapEdge.North]} Households and the east "
                + $"(rent 300) sent {crossed[MapEdge.East]}. Nobody is comparing anything.");

        Assert.True(
            crossed[MapEdge.South] > crossed[MapEdge.East],
            $"the south (rent 600) sent {crossed[MapEdge.South]} and the east (rent 300) sent "
                + $"{crossed[MapEdge.East]}.");
    }

    /// <summary>
    /// The door is a ceiling again rather than a quota, which is what adr/0023 always called it.
    /// </summary>
    /// <remarks>
    /// <b>Before this task every gate ran flat against its ceiling every Day</b>, whatever the city
    /// was like and however the Outside was priced, because nothing between the command and the
    /// meter could say no. A busiest edge under the ceiling is the model being consulted.
    /// </remarks>
    [Fact]
    public void No_gate_runs_flat_against_its_ceiling_any_more()
    {
        Dictionary<MapEdge, int> asked = Crossings(Welcomed(), 4 * Ticks.PerDay);

        int ceiling = 0;

        foreach (int count in asked.Values)
        {
            ceiling = count > ceiling ? count : ceiling;
        }

        Assert.True(
            ceiling < 4 * 96,
            $"the busiest edge admitted {ceiling} Households over four Days against a ceiling of "
                + "96 a Day, so nothing declined and the model is not being consulted.");
    }

    /// <summary>
    /// A Household already waiting can prefer the Outside to every home it is shown.
    /// </summary>
    /// <remarks>
    /// <b>02 section 5.4's <i>everything available is terrible, nobody moves in</i>, which was
    /// inexpressible until the Pool had a stay-put row.</b> ⚠ It is not a sink and must not become
    /// one — <c>gives_up_after_days</c> still bounds the Pool, and a family that declines this
    /// occasion is looking again on the next.
    /// </remarks>
    [Fact]
    public void A_household_waiting_at_a_gate_can_prefer_the_life_it_left()
    {
        Assert.True(
            Declines(Welcomed(), 8 * Ticks.PerDay) > 0,
            "no Household in the Pool preferred the Outside to everything it was shown, so the "
                + "stay-put row there is never the one drawn.");
    }
}
