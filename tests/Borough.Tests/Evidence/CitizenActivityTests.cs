using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Tests.Golden;

namespace Borough.Tests.Evidence;

using Evidence = Borough.Core.Evidence.Evidence;

/// <summary>
/// <c>CitizenTable.Activity</c> has a writer. <c>plans/0045</c>'s queue item 2.
/// </summary>
/// <remarks>
/// <para>
/// <b>The column was declared, saved, hashed and <c>Touch.PerTick</c> with no writer at all</b> —
/// one reference in the whole tree, and it was a read in <c>Evidence.OfCitizen</c>. So
/// <c>02 §9</c>'s <em>what are they doing</em> answered <b>0</b> for every Citizen in every world,
/// and the panel that says so looked correct because zero is a legitimate value.
/// </para>
/// <para>
/// ⚠ <b>The transition is decided from the value already standing, not from an argument</b>, because
/// <c>World.RecordTripFate</c> is the choke point four call sites converge on and none of them knows
/// which direction the journey was in. So the tests that matter are the four combinations of
/// <em>which way were they going</em> against <em>did they arrive</em> — a mechanism wired for one
/// direction passes a single-direction test and strands everybody at the far end.
/// </para>
/// </remarks>
public sealed class CitizenActivityTests
{
    /// <summary>A Citizen who has never moved is at home, and it is the zero value.</summary>
    /// <remarks>
    /// <c>CitizenActivity.AtHome</c> is zero deliberately: a freshly allocated row is zero-filled and
    /// somebody who has never travelled is at home, so the default reads as the truth rather than as
    /// a state nothing has written.
    /// </remarks>
    [Fact]
    public void A_citizen_who_has_never_moved_is_at_home()
    {
        World world = Populated().World;

        Assert.Equal(
            CitizenActivity.AtHome,
            (CitizenActivity)world.Citizens.Activity[FirstLiveCitizen(world)]);
    }

    /// <summary>
    /// <b>All four combinations of direction and outcome.</b>
    /// </summary>
    /// <remarks>
    /// The two refusal rows are the ones a naive mechanism gets wrong. <c>NoRouteFound</c> and
    /// <c>ExceededCommuteBudget</c> are both resolved inside <c>TripEngine.Start</c>, <em>before
    /// anybody has moved</em> — so a journey that did not complete leaves the Citizen where they set
    /// off from, and a mechanism that treated every resolution as an arrival would teleport them.
    /// </remarks>
    [Theory]
    [InlineData(CitizenActivity.TravellingToWork, TripFate.Completed, CitizenActivity.AtWork)]
    [InlineData(CitizenActivity.TravellingToWork, TripFate.NoRouteFound, CitizenActivity.AtHome)]
    [InlineData(CitizenActivity.TravellingHome, TripFate.Completed, CitizenActivity.AtHome)]
    [InlineData(
        CitizenActivity.TravellingHome, TripFate.ExceededCommuteBudget, CitizenActivity.AtWork)]
    public void A_resolved_journey_lands_them_where_the_outcome_says(
        CitizenActivity setOff, TripFate fate, CitizenActivity expected)
    {
        World world = Populated().World;
        int citizen = FirstLiveCitizen(world);

        world.Citizens.Activity[citizen] = (byte)setOff;
        world.RecordTripFate(citizen, fate);

        Assert.Equal(expected, (CitizenActivity)world.Citizens.Activity[citizen]);
    }

    /// <summary>
    /// <b>A journey nobody commuted on does not move the column.</b>
    /// </summary>
    /// <remarks>
    /// The Activity is written by <c>CommuteEngine</c> and not by <c>TripEngine</c>, and the split is
    /// deliberate: <c>adr/0080</c> makes <c>TripPurpose.Commanded</c> a test affordance rather than
    /// something a Citizen does, so a commanded journey is the harness moving somebody and not a
    /// person going anywhere. ***A column that says what somebody is doing must not be written by a
    /// door that exists for the harness.***
    /// </remarks>
    [Fact]
    public void A_commanded_journey_is_not_an_activity()
    {
        World world = Populated().World;
        int citizen = FirstLiveCitizen(world);

        world.RecordTripFate(citizen, TripFate.Completed);

        Assert.Equal(
            CitizenActivity.AtHome, (CitizenActivity)world.Citizens.Activity[citizen]);
    }

    /// <summary>
    /// <b>The writer fires in a real city</b> — somebody is somewhere other than at home.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The load-bearing test, and the only one that would have caught the original defect. Every
    /// assertion above drives <c>RecordTripFate</c> directly and would pass against a
    /// <c>CommuteEngine</c> that never wrote the column at all — which is exactly the state the build
    /// was in, with a saved and hashed column and no production writer.
    /// </para>
    /// <para>
    /// ⚠ <b>It asserts <em>somebody</em> and never a count.</b> How many people are out of the house
    /// at a given Tick is a property of the Shift draw and the geometry, so a number here would be a
    /// baseline in an assertion-tier test — and it would move whenever a Ruleset was retuned, which
    /// is <c>plans/0032</c>'s line between an assertion and an instrument.
    /// </para>
    /// </remarks>
    [Fact]
    public void Somebody_in_a_running_city_is_not_at_home()
    {
        Simulation simulation = Populated();

        for (int tick = 0; tick < Ticks.PerDay * 2; tick++)
        {
            simulation.Step(default);
        }

        Assert.Contains(
            LiveCitizens(simulation.World),
            citizen => (CitizenActivity)simulation.World.Citizens.Activity[citizen]
                != CitizenActivity.AtHome);
    }

    /// <summary><c>Evidence.OfCitizen</c> reports the same value the column holds.</summary>
    [Fact]
    public void The_evidence_panel_reports_what_the_column_holds()
    {
        World world = Populated().World;
        int citizen = FirstLiveCitizen(world);

        world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtWork;

        Assert.Equal(
            (byte)CitizenActivity.AtWork,
            Evidence.OfCitizen(world, world.Citizens.Rows.At(citizen)).Activity);
    }

    private static IEnumerable<int> LiveCitizens(World world)
    {
        for (int slot = 0; slot < world.Citizens.Rows.SlotCount; slot++)
        {
            if (world.Citizens.Rows.IsLive(slot))
            {
                yield return slot;
            }
        }
    }

    private static int FirstLiveCitizen(World world)
    {
        foreach (int slot in LiveCitizens(world))
        {
            return slot;
        }

        Assert.Fail("no live Citizen in this world.");

        return -1;
    }

    private static Simulation Populated()
    {
        InputLogBuilder builder = new(
            GoldenFixtures.Seed,
            new WorldConfiguration(GoldenFixtures.Population),
            GoldenFixtures.RulesetHash);

        Simulation simulation = Replay.Start(builder.Build(), GoldenFixtures.Rules());

        simulation.VerifyDecideWritesNothing = false;

        simulation.Step(new TickInput(
            [new Command(CommandKind.Populate, default, default)], rulesetHash: 0));

        return simulation;
    }
}
