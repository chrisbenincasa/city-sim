using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Invariants;

/// <summary>
/// <c>plans/0045</c> row 31 task 2: the city's population account, and the doors it is written at.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim is about doors, so the tests are about doors</b> —
/// <see cref="MoneyConservationTests"/>'s sentence, about people instead of money. Freeing a Citizen
/// row is one implementation with six callers that mean different things, and nothing downstream can
/// recover which it was: a birth, a gate, an untreated illness, an emigration, a dissolution, a
/// fixture. What each test below asserts is that the reason was recorded where the decision was made.
/// </para>
/// <para>
/// <b>The two failures are a missing half and a doubled one, and neither is an illegal write.</b>
/// Every defect written here goes through a public door and then undoes the door's record, which is
/// exactly the shape of a caller that forgot — and it is the failure no per-Tick check can see,
/// because each write is individually correct.
/// </para>
/// </remarks>
public sealed class PopulationLedgerTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(31);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>One Building, one Household in it, two adults. A founding city and nothing else.</summary>
    private static World Founded(int adults = 2)
    {
        var world = new World(1_000, TestRulesets.MoneyOnly);

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        for (int adult = 0; adult < adults; adult++)
        {
            world.CreateCitizen(household);
        }

        return world;
    }

    private static Handle<Household> First(World world) => world.Households.Rows.At(0);

    private static long Ledger(Column<long> counter) => counter[PopulationLedgerTable.Slot];

    private static Violation CaughtAtEnd(World world) =>
        Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

    /// <summary>
    /// A world that has been built and never stepped holds its setup as explicit instructions.
    /// </summary>
    /// <remarks>
    /// <b>The state every guard is written against.</b> Nothing has folded yet, so the opening figure
    /// is zero and the people are all scenario additions — and the account still holds, because the
    /// equation is over the same terms either way.
    /// </remarks>
    [Fact]
    public void A_world_that_has_not_stepped_holds_its_setup_as_scenario_creation()
    {
        World world = Founded();

        Assert.Equal(0, world.PopulationLedger.Sealed[PopulationLedgerTable.Slot]);
        Assert.Equal(0, Ledger(world.PopulationLedger.OpeningCityPeople));
        Assert.Equal(2, Ledger(world.PopulationLedger.ScenarioAdditions));
        Assert.Equal(1, Ledger(world.PopulationLedger.HouseholdsCreated));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>The first Tick folds the setup into the opening figures, and only the first.</summary>
    [Fact]
    public void The_first_tick_seals_the_founding_figures_once()
    {
        World world = Founded();
        var simulation = new Simulation(world, Key);

        simulation.Step(default);

        Assert.Equal(1, world.PopulationLedger.Sealed[PopulationLedgerTable.Slot]);
        Assert.Equal(2, Ledger(world.PopulationLedger.OpeningCityPeople));
        Assert.Equal(1, Ledger(world.PopulationLedger.OpeningCityHouseholds));
        Assert.Equal(0, Ledger(world.PopulationLedger.ScenarioAdditions));

        world.CreateCitizen(First(world));
        simulation.Step(default);

        // Sealed once: the third adult is an ordinary recorded event rather than part of the founding,
        // and the opening figure a player reads is still what the city started with.
        Assert.Equal(2, Ledger(world.PopulationLedger.OpeningCityPeople));
        Assert.Equal(1, Ledger(world.PopulationLedger.ScenarioAdditions));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A birth is a birth and not an addition somebody made.</summary>
    /// <remarks>
    /// <b>The one growth channel that survives the late game</b> (<c>adr/0023</c>), so it is the one
    /// counter a player reads against immigration. Folded into scenario creation it would be
    /// unreadable, and folded into admissions it would claim a Hinterland had lost somebody.
    /// </remarks>
    [Fact]
    public void A_birth_is_neither_a_scenario_addition_nor_an_admission()
    {
        World world = Founded();
        var simulation = new Simulation(world, Key);

        simulation.Step(default);
        world.Bear(First(world));

        Assert.Equal(1, Ledger(world.PopulationLedger.Births));
        Assert.Equal(0, Ledger(world.PopulationLedger.ScenarioAdditions));
        Assert.Equal(0, Ledger(world.PopulationLedger.Admissions));
        Assert.Equal(3, world.PopulationLedger.People);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>An admission is one Household and the people who crossed with it.</summary>
    /// <remarks>
    /// <b>Counted at the gate rather than per member.</b> <c>World.TryArrive</c> creates its adults
    /// through the unaccounted door precisely so that this reads as one admission of two people — the
    /// alternative spelling, through <c>CreateCitizen</c>, would file an immigrant family as something
    /// a scenario added.
    /// </remarks>
    [Fact]
    public void An_admission_counts_one_household_and_everybody_in_it()
    {
        var world = new World(1_000, Shipped("bordered.toml"));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key);

        simulation.Step(default);

        int gate = Rows.NoSlot;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                gate = slot;
                break;
            }
        }

        Assert.NotEqual(Rows.NoSlot, gate);

        long people = world.PopulationLedger.People;

        Assert.True(
            world.TryArrive(
                world.Buildings.Rows.At(gate), lifeStage: 0, citizens: 3, world.Tick, out _));

        Assert.Equal(3, Ledger(world.PopulationLedger.Admissions));
        Assert.Equal(1, Ledger(world.PopulationLedger.HouseholdsAdmitted));
        Assert.Equal(people + 3, world.PopulationLedger.People);
        Assert.Equal(world.Citizens.Rows.LiveCount, world.PopulationLedger.People);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>An illness death is a death, and no fixture removed anybody.</summary>
    /// <remarks>
    /// <b>The care engine has been freeing Citizen rows since milestone 26</b> and the account could
    /// not tell those from a bulldozed row until this door existed. What the distinction is worth is
    /// the answer to <em>why did the population fall</em>.
    /// </remarks>
    [Fact]
    public void An_illness_death_is_not_a_scenario_removal()
    {
        World world = Founded();
        var simulation = new Simulation(world, Key);

        simulation.Step(default);

        world.DieCitizen(world.Citizens.Rows.At(0));

        Assert.Equal(1, Ledger(world.PopulationLedger.IllnessDeaths));
        Assert.Equal(0, Ledger(world.PopulationLedger.ScenarioRemovals));
        Assert.Equal(1, world.PopulationLedger.People);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A dissolution takes its members, and nobody out there gets them back.</summary>
    [Fact]
    public void A_dissolution_counts_its_people_and_credits_no_hinterland()
    {
        var world = new World(1_000, Shipped("attracted.toml"));

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        Handle<Household> household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household);
        world.CreateCitizen(household);

        var simulation = new Simulation(world, Key);

        simulation.Step(default);

        long outside = world.PopulationLedger.OpeningOutsideHouseholds[PopulationLedgerTable.Slot];

        world.Dissolve(First(world), world.Tick);

        Assert.Equal(2, Ledger(world.PopulationLedger.DissolutionPeople));
        Assert.Equal(1, Ledger(world.PopulationLedger.HouseholdsDissolved));
        Assert.Equal(0, Ledger(world.PopulationLedger.Departures));

        long standing = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            standing += world.Hinterlands.ReturnedHouseholds[edge];
        }

        Assert.Equal(0, standing);
        Assert.Equal(outside, world.PopulationLedger.OpeningOutsideHouseholds[PopulationLedgerTable.Slot]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A fixture destroying a Household removes exactly the people in it.</summary>
    [Fact]
    public void Destroying_a_household_removes_its_people_as_a_scenario()
    {
        World world = Founded(adults: 3);
        var simulation = new Simulation(world, Key);

        simulation.Step(default);
        world.DestroyHousehold(First(world));

        Assert.Equal(3, Ledger(world.PopulationLedger.ScenarioRemovals));
        Assert.Equal(1, Ledger(world.PopulationLedger.HouseholdsRemoved));
        Assert.Equal(0, world.PopulationLedger.People);
        Assert.Equal(0, world.PopulationLedger.Households);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A door that creates somebody and forgets to say so is caught, and the discrepancy says which way.
    /// </summary>
    /// <remarks>
    /// <b>The failure the account exists for.</b> The Citizen was created through the ordinary public
    /// door, so nothing at the write site can fire — what is wrong is a relation between a table and
    /// an anchor, which is what the whole-world tier is for.
    /// </remarks>
    [Fact]
    public void A_door_that_forgets_to_record_a_person_is_caught()
    {
        World world = Founded();

        world.CreateCitizen(First(world));
        world.PopulationLedger.ScenarioAdditions[PopulationLedgerTable.Slot]--;

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.CityPopulationIsAccounted, caught.Invariant);
        Assert.Equal(1, caught.Other);
    }

    /// <summary>A door that records the same person twice is caught the other way.</summary>
    [Fact]
    public void A_door_that_records_a_person_twice_is_caught()
    {
        World world = Founded();

        world.PopulationLedger.Births[PopulationLedgerTable.Slot]++;

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.CityPopulationIsAccounted, caught.Invariant);
        Assert.Equal(-1, caught.Other);
    }

    /// <summary>
    /// A Household the account never heard about is caught by its own equation, not by the people one.
    /// </summary>
    /// <remarks>
    /// <b>Which is why there are two.</b> This world has the right number of people in it and one
    /// unrecorded Household, and no count of people can say so.
    /// </remarks>
    [Fact]
    public void A_household_the_account_never_heard_about_is_caught()
    {
        World world = Founded();

        world.PopulationLedger.HouseholdsCreated[PopulationLedgerTable.Slot]--;

        Violation caught = CaughtAtEnd(world);

        Assert.Equal(Invariant.CityHouseholdsAreAccounted, caught.Invariant);
        Assert.Equal(1, caught.Other);
    }
}
