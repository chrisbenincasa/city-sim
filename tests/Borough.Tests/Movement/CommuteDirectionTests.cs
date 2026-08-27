using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Movement;

/// <summary>
/// A commute goes somewhere. <c>plans/0045</c>'s queue item 4.
/// </summary>
/// <remarks>
/// <para>
/// <b>The roster fires on the Day's phase and <c>CommuteEngine.Travel</c> never asked where the
/// Citizen was.</b> Both lists are walked unconditionally, so a Citizen standing at home at their
/// <em>homeward</em> phase took a journey from their workplace to their dwelling — having never left
/// it — and one standing at their workplace at their <em>outbound</em> phase set off for work again.
/// </para>
/// <para>
/// ⚠ <b>The mechanism was in the tree from milestone 5b-bis and nothing could see it.</b>
/// <c>CitizenTable.Activity</c> had no writer, so the only observable was a Trip count — and a Trip
/// from home to home is a Trip. Measured on <c>minimal.toml</c> at 2,000 Citizens over one Day:
/// <b>163 journeys home from home and 69 to work from work</b>, against 369 honest departures.
/// ***Roughly a fifth of this city's commuting was somebody travelling to where they already
/// stood.***
/// </para>
/// <para>
/// <b>Why anybody is in the wrong place is not a defect and is left alone.</b> Employment is
/// assigned on a cadence (<c>adr/0081</c>), so a Citizen hired after their outbound phase has passed
/// meets their homeward phase first — 149 were hired during the measured Day. <c>adr/0101</c> anchors
/// both journeys on the Shift and says nothing about somebody who missed the first one; the guard
/// here makes them wait for tomorrow morning rather than inventing a journey for them.
/// </para>
/// </remarks>
public sealed class CommuteDirectionTests
{
    /// <summary>
    /// <b>Nobody travels to where they already are.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole-city form, and it has to be: the bug needs a Citizen whose Shift phases straddle
    /// their hiring Tick, which is a property of the assignment cadence meeting the Shift draw. A
    /// single-Citizen fixture cannot produce it without hand-placing both, at which point the test
    /// asserts the fixture.
    /// </para>
    /// <para>
    /// ⚠ <b>Asserted as exactly zero rather than as a proportion.</b> A tolerance would pass the
    /// original defect at a smaller population, and there is no honest number of journeys-to-where-
    /// you-are that a city should have.
    /// </para>
    /// </remarks>
    [Fact]
    public void Nobody_sets_off_for_a_place_they_are_already_standing_in()
    {
        (Simulation simulation, World world) = Settled();

        var before = new byte[world.Citizens.Rows.SlotCount];

        for (int slot = 0; slot < before.Length; slot++)
        {
            before[slot] = world.Citizens.Activity[slot];
        }

        int homeFromHome = 0;
        int workFromWork = 0;

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            for (int slot = 0; slot < before.Length; slot++)
            {
                if (!world.Citizens.Rows.IsLive(slot))
                {
                    continue;
                }

                var was = (CitizenActivity)before[slot];
                var now = (CitizenActivity)world.Citizens.Activity[slot];

                if (was == CitizenActivity.AtHome && now == CitizenActivity.TravellingHome)
                {
                    homeFromHome++;
                }

                if (was == CitizenActivity.AtWork && now == CitizenActivity.TravellingToWork)
                {
                    workFromWork++;
                }

                before[slot] = world.Citizens.Activity[slot];
            }
        }

        Assert.Equal(0, homeFromHome);
        Assert.Equal(0, workFromWork);
    }

    /// <summary>
    /// <b>The city still commutes.</b> A guard that refused every journey would pass the test above.
    /// </summary>
    /// <remarks>
    /// The paired assertion, and the reason it is not folded into the one above: <em>nobody travels
    /// pointlessly</em> and <em>somebody travels</em> are different claims, and the cheapest wrong fix
    /// satisfies the first by breaking the second. ⚠ <b>A floor and not a figure</b> — how many people
    /// commute in a Day is a property of the Shift draw and the Ruleset, so an exact count here would
    /// be a baseline in an assertion-tier test (<c>plans/0032</c>).
    /// </remarks>
    [Fact]
    public void Somebody_still_makes_the_journey_in_each_direction()
    {
        (Simulation simulation, World world) = Settled();

        var before = new byte[world.Citizens.Rows.SlotCount];

        for (int slot = 0; slot < before.Length; slot++)
        {
            before[slot] = world.Citizens.Activity[slot];
        }

        int toWork = 0;
        int toHome = 0;

        for (int tick = 0; tick < Ticks.PerDay; tick++)
        {
            simulation.Step(default);

            for (int slot = 0; slot < before.Length; slot++)
            {
                if (!world.Citizens.Rows.IsLive(slot))
                {
                    continue;
                }

                var was = (CitizenActivity)before[slot];
                var now = (CitizenActivity)world.Citizens.Activity[slot];

                if (was == CitizenActivity.AtHome && now == CitizenActivity.TravellingToWork)
                {
                    toWork++;
                }

                if (was == CitizenActivity.AtWork && now == CitizenActivity.TravellingHome)
                {
                    toHome++;
                }

                before[slot] = world.Citizens.Activity[slot];
            }
        }

        Assert.True(toWork > 0, "nobody went to work all Day.");
        Assert.True(toHome > 0, "nobody went home all Day.");
    }

    /// <summary>
    /// <c>minimal.toml</c> at 2,000 Citizens, stepped until the job cadence has placed people.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>4,096 Ticks is two Days and is measured rather than picked.</b> The defect needs Citizens
    /// hired mid-Day, which needs the assignment pass to still be placing people when the trace
    /// starts — and it is: employment rose from 1,583 to 1,732 across the Day this was written
    /// against.
    /// </remarks>
    private static (Simulation Simulation, World World) Settled()
    {
        string toml = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));

        RulesetLoadResult result = RulesetLoader.Parse(toml, "minimal.toml");

        Assert.True(result.Ok, result.Describe());

        var key = WorldKey.FromSeed(0);
        World world = new(2_000, result.Ruleset!);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        for (int tick = 0; tick < 4_096; tick++)
        {
            simulation.Step(default);
        }

        return (simulation, world);
    }
}
