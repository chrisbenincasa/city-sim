using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 5: the other side of the population account, where a Household that
/// gave up looking becomes people standing behind an edge again.
/// </summary>
/// <remarks>
/// <para>
/// <b>Departure was a one-sided entry until this row.</b> The ledger counted who left and the money
/// supply lost their savings, and then the people were simply gone — no Outside held them, so a city
/// could empty itself into nowhere and the accounts would still balance. <c>plans/0073</c> D9 closes
/// it: the family draws a destination with the same kernel a resident uses, and one Household of its
/// exact composition joins the stock behind that edge.
/// </para>
/// <para>
/// ⚠ <b>Only a Departure credits.</b> A dissolution and a fixture's destruction both free the same
/// rows, and neither is somebody moving away. Each has its own test here because the three doors are
/// one method apart and a credit written in the wrong one would invent a family.
/// </para>
/// </remarks>
public sealed class HinterlandDepartureTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "attracted.toml"));

    private static Ruleset Parsed(string text)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, "attracted.toml");

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>The only shipped world whose edges have anybody behind them, with a city on it.</summary>
    private static World Attracted() => Attracted(Parsed(Text()));

    private static World Attracted(Ruleset rules)
    {
        World world = new(1_000, rules, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>A Household the generated city housed, taken out of its dwelling and into the Pool.</summary>
    /// <remarks>
    /// <b>It never crossed a gate</b>, so its <c>Arrived</c> column is zero and there is no edge to
    /// inherit a destination from. That is what makes it the fixture for a locally formed family.
    /// </remarks>
    private static Handle<Household> Local(World world)
    {
        Handle<Household> handle = Housed(world);

        world.Unplace(handle);

        return handle;
    }

    /// <summary>A Household the generated city housed, left where it is.</summary>
    private static Handle<Household> Housed(World world)
    {
        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Households.Arrived[slot] == 0
                && world.Buildings.Rows.TryResolve(world.Households.Dwelling[slot], out _))
            {
                return world.Households.Rows.At(slot);
            }
        }

        throw new InvalidOperationException("the generated city housed nobody.");
    }

    private static int GateOn(World world, MapEdge edge)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot])
                && world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lot)
                && world.EdgeOf(lot) == edge)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"this world has no gate on its {edge} edge.");
    }

    /// <summary>The group behind <paramref name="edge"/> whose Households hold that many children.</summary>
    private static int GroupWith(World world, MapEdge edge, int children)
    {
        foreach (int slot in world.HinterlandPopulation.Groups(world.Hinterlands)
            .Walk(HinterlandTable.SlotOf(edge)))
        {
            if (world.HinterlandPopulation.Children[slot] == children)
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"no group behind {edge} holds {children} children.");
    }

    /// <summary>Admits one Household of an exact Outside composition, which leaves it in the Pool.</summary>
    private static Handle<Household> Admit(World world, MapEdge edge, int children)
    {
        int group = GroupWith(world, edge, children);

        Assert.True(world.Rules.TryHinterland(edge, out HinterlandDefinition source));

        var prospect = ArrivalProspect.Of(
            Key,
            source,
            world.HinterlandPopulation.Rows.At(group),
            edge,
            world.HinterlandPopulation.CompositionAt(group),
            identity: 1);

        Assert.Equal(
            Admission.Admitted,
            world.TryAdmitProspect(
                prospect,
                world.Buildings.Rows.At(GateOn(world, edge)),
                world.Tick,
                out Handle<Household> household));

        return household;
    }

    /// <summary>The one group the Outside credited, and the assertion that there is exactly one.</summary>
    private static int Credited(World world)
    {
        int found = Rows.NoSlot;

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (!world.HinterlandPopulation.Rows.IsLive(slot)
                || world.HinterlandPopulation.Returned[slot] == 0)
            {
                continue;
            }

            Assert.Equal(Rows.NoSlot, found);
            found = slot;
        }

        Assert.NotEqual(Rows.NoSlot, found);

        return found;
    }

    private static (long Households, long People) ReturnedAcrossEdges(World world)
    {
        long households = 0;
        long people = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            households += world.Hinterlands.ReturnedHouseholds[edge];
            people += world.Hinterlands.ReturnedPeople[edge];
        }

        return (households, people);
    }

    private static void Empty(World world, Handle<Household> household)
    {
        int slot = world.Households.Rows.Resolve(household);

        var members = new List<Handle<Citizen>>();

        foreach (int member in world.Members.Walk(slot))
        {
            members.Add(world.Citizens.Rows.At(member));
        }

        foreach (Handle<Citizen> member in members)
        {
            world.DestroyCitizen(member);
        }
    }

    /// <summary>A family of one adult at each of two tiers with two children returns as exactly that.</summary>
    /// <remarks>
    /// <b>The composition is counted off the Citizens who are still here</b> (D9), so this is also the
    /// assertion that the credit runs before the members are destroyed. A credit written after the
    /// retirement would find nobody and file an empty Household.
    /// </remarks>
    [Fact]
    public void A_family_returns_at_the_composition_it_actually_had()
    {
        World world = Attracted();

        Handle<Household> family = Admit(world, MapEdge.West, children: 2);
        byte stage = world.Households.LifeStage[world.Households.Rows.Resolve(family)];

        world.Depart(family);

        int group = Credited(world);
        HinterlandPopulationTable groups = world.HinterlandPopulation;

        Assert.Equal(stage, groups.Stage[group]);
        Assert.Equal(1, groups.AdultsTier1[group]);
        Assert.Equal(1, groups.AdultsTier2[group]);
        Assert.Equal(0, groups.AdultsTier3[group]);
        Assert.Equal(2, groups.Children[group]);
        Assert.Equal(1L, groups.Returned[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A family the city produced itself leaves for an Outside it never came from.</summary>
    [Fact]
    public void A_locally_formed_family_still_has_somewhere_to_go()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);
        int slot = world.Households.Rows.Resolve(family);

        Assert.Equal(0, world.Households.Arrived[slot]);

        int people = world.Members.Length(slot);

        world.Depart(family);

        Assert.Equal(people, world.HinterlandPopulation.Members(Credited(world)));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A demolished door does not keep anybody in the city.</summary>
    /// <remarks>
    /// <b>The destination is an accounting edge and not a journey</b> (D9). Where the people went is
    /// a question with an answer whether or not a gate still stands to walk out of.
    /// </remarks>
    [Fact]
    public void A_city_with_no_gates_left_still_sends_its_leavers_somewhere()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                world.DestroyBuilding(world.Buildings.Rows.At(slot), world.Tick);
            }
        }

        world.Depart(family);

        Assert.Equal(1L, world.HinterlandPopulation.Returned[Credited(world)]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Every Departure credits one Household and its people once, and no more.</summary>
    [Fact]
    public void Every_Departure_credits_exactly_once()
    {
        World world = Attracted();

        world.Depart(Admit(world, MapEdge.West, children: 2));
        world.Depart(Admit(world, MapEdge.East, children: 0));

        for (int leaver = 0; leaver < 3; leaver++)
        {
            world.Depart(Local(world));
        }

        (long households, long people) = ReturnedAcrossEdges(world);

        Assert.Equal(5L, households);
        Assert.Equal(
            world.PopulationLedger.HouseholdsDeparted[PopulationLedgerTable.Slot], households);
        Assert.Equal(world.PopulationLedger.Departures[PopulationLedgerTable.Slot], people);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Where a family goes follows the Outside economy, and not the edge it arrived by.</summary>
    /// <remarks>
    /// <para>
    /// <b>A family admitted at the dearest edge leaves by the cheapest one</b>, which is the whole of
    /// D9's claim that provenance is not a return address. There is no incumbent bonus and nothing
    /// about the arrival edge enters the comparison.
    /// </para>
    /// <para>
    /// ⚠ <b>The second world is what makes the first assertion mean anything.</b> Any code that
    /// happened to always name one edge would pass the first half, so the Ruleset is retuned to make
    /// the cheapest Outside the dearest and the destination has to move with it. The draw over four
    /// widely separated worths is near enough deterministic to assert on directly.
    /// </para>
    /// </remarks>
    [Fact]
    public void The_destination_follows_the_Outside_economy_rather_than_provenance()
    {
        World cheap = Attracted();

        cheap.Depart(Admit(cheap, MapEdge.West, children: 2));

        MapEdge went = WhereTheyWent(cheap);

        Assert.NotEqual(MapEdge.West, went);

        World dear = Attracted(
            Parsed(Text().Replace("\nrent = 300\n", "\nrent = 5000\n", StringComparison.Ordinal)));

        dear.Depart(Admit(dear, MapEdge.West, children: 2));

        Assert.NotEqual(went, WhereTheyWent(dear));

        cheap.Invariants.RunEndOfRun(cheap);
        dear.Invariants.RunEndOfRun(dear);
    }

    /// <summary>The one edge that took a return, and the assertion that only one did.</summary>
    private static MapEdge WhereTheyWent(World world)
    {
        MapEdge found = MapEdge.None;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            if (world.Hinterlands.ReturnedHouseholds[edge] == 0)
            {
                continue;
            }

            Assert.Equal(MapEdge.None, found);
            found = HinterlandTable.EdgeAt(edge);
        }

        Assert.NotEqual(MapEdge.None, found);

        return found;
    }

    /// <summary>A dissolution and a fixture's destruction put nobody back behind an edge.</summary>
    [Fact]
    public void Nothing_but_a_Departure_refills_the_Outside()
    {
        World world = Attracted();

        world.Dissolve(Local(world), world.Tick);
        world.DestroyHousehold(Housed(world));

        Assert.Equal((0L, 0L), ReturnedAcrossEdges(world));

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot))
            {
                Assert.Equal(0L, world.HinterlandPopulation.Returned[slot]);
            }
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A Household holding nobody returns as a Household holding nobody.</summary>
    /// <remarks>
    /// <b>One Household and zero people, which are separate counts for this reason.</b> Collapsing
    /// them would either lose the row or credit a person who does not exist.
    /// </remarks>
    [Fact]
    public void An_empty_Household_returns_as_an_empty_Household()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);

        Empty(world, family);

        world.Depart(family);

        int group = Credited(world);

        Assert.Equal(0, world.HinterlandPopulation.Members(group));
        Assert.Equal(1, world.HinterlandPopulation.Stock[group]);
        Assert.Equal((1L, 0L), ReturnedAcrossEdges(world));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A Household of nothing but children is counted, and rests at nobody.</summary>
    /// <remarks>
    /// <b>No authored group has this shape and none ever will</b>, so the row it lands in is created
    /// with a resting count of zero — counted while it is there, and never a population the Outside
    /// maintains (D1, D2).
    /// </remarks>
    [Fact]
    public void A_Household_of_children_lands_in_a_group_that_rests_at_nobody()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);

        world.Bear(family);
        Empty(world, family);
        world.Bear(family);

        world.Depart(family);

        int group = Credited(world);
        HinterlandPopulationTable groups = world.HinterlandPopulation;

        Assert.Equal(0, groups.AdultsTier1[group] + groups.AdultsTier2[group] + groups.AdultsTier3[group]);
        Assert.Equal(1, groups.Children[group]);
        Assert.Equal(1, groups.Stock[group]);
        Assert.Equal(0, groups.Target[group]);
        Assert.Equal(0, groups.Authored[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A purse below every authored range files into the bottom band rather than nowhere.</summary>
    [Fact]
    public void A_Household_carrying_nothing_files_into_the_bottom_band()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);
        int slot = world.Households.Rows.Resolve(family);

        world.Withdraw(
            world.Households.Balance[slot], world.BalanceOf(family).Raw, world.Tick);

        Assert.Equal(Money.Zero, world.BalanceOf(family));

        world.Depart(family);

        Assert.Equal(0, world.HinterlandPopulation.MoneyBand[Credited(world)]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A purse above every authored range files into the top band rather than nowhere.</summary>
    /// <remarks>
    /// <b>The band belongs to the destination and the destination is drawn</b>, so the amount has to
    /// clear the widest authored ceiling of the four to make the assertion hold whichever edge the
    /// family goes to.
    /// </remarks>
    [Fact]
    public void A_Household_carrying_more_than_any_range_files_into_the_top_band()
    {
        World world = Attracted();

        Handle<Household> family = Local(world);

        long richest = 0;

        for (int edge = 0; edge < HinterlandTable.Edges; edge++)
        {
            if (world.Rules.TryHinterland(HinterlandTable.EdgeAt(edge), out HinterlandDefinition outside)
                && outside.EmigrantBalanceMax.Raw > richest)
            {
                richest = outside.EmigrantBalanceMax.Raw;
            }
        }

        long wanted = richest + 1 - world.BalanceOf(family).Raw;

        Assert.True(wanted > 0, "the fixture already carries more than every authored ceiling.");

        world.Endow(family, new Money(wanted));
        world.Depart(family);

        Assert.Equal(
            HinterlandDefinition.MoneyBands - 1,
            world.HinterlandPopulation.MoneyBand[Credited(world)]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A group nobody authored drains away again and takes its row with it.</summary>
    /// <remarks>
    /// <b>Recovery works from both directions</b> (D2), so a row resting at zero with one Household in
    /// it is removed and reported as turnover, and the row is freed once it holds nobody. Without that
    /// every Departure to an unusual composition would leave a permanent row behind.
    /// </remarks>
    [Fact]
    public void A_returned_group_recovers_back_to_nothing()
    {
        World world = Attracted(
            Parsed(Text().Replace("recovery_days         = 32", "recovery_days         = 1", StringComparison.Ordinal)));

        Handle<Household> family = Local(world);

        Empty(world, family);
        world.Bear(family);

        world.Depart(family);

        int group = Credited(world);
        var edge = (MapEdge)world.HinterlandPopulation.Edge[group];
        HinterlandComposition composition = world.HinterlandPopulation.CompositionAt(group);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        Assert.False(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, edge, composition, out _),
            "the returned group was still standing a whole recovery period later.");

        Assert.True(
            world.Hinterlands.TurnoverHouseholds[HinterlandTable.SlotOf(edge)] > 0,
            "the group disappeared without being reported as turnover.");

        world.Invariants.RunEndOfRun(world);
    }
}
