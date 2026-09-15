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
