using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;
using Xunit.Abstractions;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/offices.toml</c>: an office turns labour into a Day's units of <c>accounting</c>
/// (30-minute reach, 90 at the Pool) and <c>insurance</c> (remote, 30), and shops buy one of each
/// under <c>adr/0178</c>'s contract. A local sale is a transfer and an import is not, so which
/// provider a firm holds is visible in the Money supply as well as in the contract.
/// </summary>
public sealed class LocalServiceTests(ITestOutputHelper output)
{
    private const long Capital = 100_000;
    private const byte Shopfront = 2;
    private const byte OfficeKind = 4;
    private const int North = 0;
    private const long AccountingPrice = 90;
    private const long InsurancePrice = 30;

    private static ResourceId Accounting => new(3);

    private static ResourceId Insurance => new(4);

    private static string Text() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "offices.toml"));

    /// <summary>
    /// The fixture's Rules, without the <c>offices</c> Zone Rule unless <paramref name="raising"/>.
    /// </summary>
    /// <remarks>
    /// Most tests put an office on a chosen Lot and count who contracts with it, and an office the
    /// city raised on its own would take clients from it.
    /// </remarks>
    private static Ruleset Rules(string? text = null, bool raising = false)
    {
        text ??= Text();

        if (!raising)
        {
            int start = text.IndexOf("[[zone_rule]]\nname          = \"offices\"", StringComparison.Ordinal);
            Assert.True(start >= 0, "offices.toml has no `offices` Zone Rule to remove");
            int end = text.IndexOf("[placement]", start, StringComparison.Ordinal);
            Assert.True(end > start, "offices.toml has no [placement] after the `offices` Zone Rule");
            text = text.Remove(start, end - start);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(text, "offices.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        return loaded.Ruleset!;
    }

    /// <summary>
    /// One unit per firing, so that the second buyer in a firing's window finds the allowance empty.
    /// </summary>
    /// <remarks>
    /// The rate is left alone. A Rule firing less often than its labour keeps is refused at load,
    /// because each firing would see only the last shelf life's work — so an office cannot be made
    /// scarce by slowing it down, only by making less.
    /// </remarks>
    private static string Scarce() => Text()
        .Replace(
            "{ scope = \"local\", resource = \"accounting\", amount = 64 }",
            "{ scope = \"local\", resource = \"accounting\", amount = 1 }")
        .Replace(
            "{ scope = \"local\", resource = \"insurance\",  amount = 64 }",
            "{ scope = \"local\", resource = \"insurance\",  amount = 1 }");

    /// <summary>A reach no office in this city satisfies from more than a block away.</summary>
    private static string Near() => Text().Replace("reach_minutes = 30", "reach_minutes = 1");

    /// <summary>
    /// A shop that buys four units of <c>accounting</c> at a time, out of an office making one per
    /// firing.
    /// </summary>
    /// <remarks>
    /// The batch a buyer needs is what the market wake has to be budgeted against: the office passes
    /// one, two and three units without being able to serve anybody.
    /// </remarks>
    private static string Batched() => Text()
        .Replace(
            "{ scope = \"local\", resource = \"accounting\", amount = 64 }",
            "{ scope = \"local\", resource = \"accounting\", amount = 1 }")
        .Replace(
            "{ scope = \"pool\", resource = \"accounting\", amount = 1 },",
            "{ scope = \"pool\", resource = \"accounting\", amount = 4 },");

    /// <summary>
    /// A world in which an office holds a service allowance and a firm holds a contract naming it,
    /// for the table audits. <c>ServiceImportTests.Contracted</c>'s shape and its reason: nothing in
    /// a shipped Ruleset raises an office yet (slice 3b), so no stepped world reaches these rows.
    /// </summary>
    internal static World Supplied()
    {
        var (world, sim) = Start(Rules());
        int office = OpenOffice(world, sim, prove: 0);

        Assert.True(
            StepUntil(world, sim, w => Shops(w).Exists(s => Provider(w, s, Accounting) == office),
                Ticks.PerDay),
            "no shop contracted with the office");

        return world;
    }

    /// <summary>
    /// The walk between two firms' premises, which is the quantity the engine's reach test measures.
    /// </summary>
    private static TravelTime Walk(World world, int from, int to) =>
        WalkRouting.Cost(
            world.Roads,
            TravelMode.Foot,
            world.AccessPoint(
                world.Buildings.Rows.Resolve(world.Businesses.Building[from]), TravelMode.Foot),
            world.AccessPoint(
                world.Buildings.Rows.Resolve(world.Businesses.Building[to]), TravelMode.Foot),
            world.Rules.Trips.CrossingCost,
            new WalkScratch());

    private static (World World, Simulation Sim) Start(Ruleset rules)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(400, rules, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int t = 0; t < 2048 && Capitalise(world) == 0; t++)
        {
            sim.Step(default);
        }

        Assert.True(Capitalise(world) > 0, "the city holds no firm to capitalise");

        for (int t = 0; t < 4 * Ticks.PerDay && Shops(world).TrueForAll(s => !Staffed(world, s)); t++)
        {
            sim.Step(default);
        }

        Assert.Contains(true, Shops(world).ConvertAll(s => Staffed(world, s)));
        return (world, sim);
    }

    /// <summary>
    /// Puts a staffed office on a vacant Lot, steps until it has made its first units, then proves
    /// Phase 2 writes nothing across <paramref name="prove"/> Ticks of the provider match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The proof folds every column every Tick and costs about forty times an ordinary Tick, so it
    /// runs over the window this file is about rather than over the warm-up, which has no office in
    /// it. The default covers more than a client Rule's rate, so every shop re-reads its contract
    /// inside it.
    /// </para>
    /// <para>
    /// The office is staffed by the assignment pass rather than by hand, which is slower in Ticks and
    /// far cheaper in work: a Citizen hired into premises across town re-plans a commute it cannot
    /// make, every Tick, for as long as the job lasts.
    /// </para>
    /// </remarks>
    private static int OpenOffice(World world, Simulation sim, int prove = 128)
    {
        int office = PlaceOffice(world);

        Assert.True(Supplying(world, sim, office), "the office never made a unit of accounting");

        sim.VerifyDecideWritesNothing = true;

        for (int t = 0; t < prove; t++)
        {
            sim.Step(default);
        }

        sim.VerifyDecideWritesNothing = false;

        return office;
    }

    /// <summary>
    /// Puts an office on a vacant Lot and pays in its opening capital.
    /// </summary>
    /// <remarks>
    /// Vacant Lots are taken in slot order, so two calls put two offices on two Lots. The firm is
    /// found through the Building just raised rather than by kind, which is what lets more than one
    /// stand at a time.
    /// </remarks>
    private static int PlaceOffice(World world, int skip = 0)
    {
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
        {
            if (!world.Lots.Rows.IsLive(lot) || !world.Lots.IsVacant(lot) || skip-- > 0)
            {
                continue;
            }

            Handle<Building> premises =
                world.CreateBuilding(world.Lots.Rows.At(lot), OfficeKind, world.Tick, world.Key);
            Capitalise(world);
            int office = Tenant(world, premises);

            Assert.NotEqual(Rows.NoSlot, office);
            return office;
        }

        throw new InvalidOperationException("no vacant Lot to put an office on");
    }

    /// <summary>The Business standing in one Building.</summary>
    private static int Tenant(World world, Handle<Building> premises)
    {
        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (world.Businesses.Rows.IsLive(business)
                && world.Businesses.Building[business] == premises)
            {
                return business;
            }
        }

        return Rows.NoSlot;
    }

    private static List<int> Shops(World world) => Businesses(world, Shopfront);

    private static List<int> Businesses(World world, byte kind)
    {
        var found = new List<int>();

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (world.Businesses.Rows.IsLive(business)
                && world.Buildings.Rows.TryResolve(world.Businesses.Building[business], out int at)
                && world.Buildings.Kind[at] == kind)
            {
                found.Add(business);
            }
        }

        return found;
    }

    private static int Capitalise(World world)
    {
        world.TryMoneyResource(out ResourceId money);
        int firms = 0;

        for (int business = 0; business < world.Businesses.Rows.SlotCount; business++)
        {
            if (!world.Businesses.Rows.IsLive(business)
                || !world.Bins.Rows.TryResolve(world.Businesses.Balance[business], out int balance))
            {
                continue;
            }

            firms++;

            if (world.Bins.LevelAt(balance) < Capital)
            {
                long change = Capital - world.Bins.LevelAt(balance);
                world.Deposit(world.Bins.Rows.At(balance), change, world.Tick);
                world.MoneySupply.Issued[MoneySupplyTable.Slot] += new Money(change);
            }
        }

        return firms;
    }

    /// <summary>
    /// Takes a firm's whole balance out of the city, so that it can buy nothing until it is
    /// capitalised again.
    /// </summary>
    /// <remarks>
    /// <see cref="Capitalise"/>'s inverse, and it moves <c>Issued</c> the same way so that
    /// <c>MoneyIsConserved</c> still holds. A firm with nothing to pay with proposes a purchase that
    /// the affordability walk rejects, so it writes no contract — which is how two offices can both
    /// become providers before anybody has matched with either.
    /// </remarks>
    private static void Drain(World world, int business)
    {
        int balance = world.Bins.Rows.Resolve(world.Businesses.Balance[business]);
        long held = world.Bins.LevelAt(balance);

        if (held > 0)
        {
            world.Withdraw(world.Bins.Rows.At(balance), held, world.Tick);
            world.MoneySupply.Issued[MoneySupplyTable.Slot] -= new Money(held);
        }
    }

    /// <summary>What a firm has recognised as expense today, or zero if it has recognised nothing.</summary>
    private static long Expense(World world, int business) =>
        world.Businesses.TradingDay[business] == BusinessAccounts.DayOf(world.Tick)
            ? world.Businesses.DayExpense[business]
            : 0;

    /// <inheritdoc cref="Expense"/>
    private static long Revenue(World world, int business) =>
        world.Businesses.TradingDay[business] == BusinessAccounts.DayOf(world.Tick)
            ? world.Businesses.DayRevenue[business]
            : 0;

    private static bool Staffed(World world, int business)
    {
        for (Handle<Bin> at = world.Businesses.BinHead[business];
             world.Bins.Rows.TryResolve(at, out int bin);
             at = world.Bins.OwnerNext[bin])
        {
            if (world.Rules.Family(world.Bins.Resource[bin]) == ResourceFamily.Labour
                && world.Bins.LevelAt(bin) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static long Balance(World world, int business) =>
        world.Bins.LevelAt(world.Bins.Rows.Resolve(world.Businesses.Balance[business]));

    private static long Issued(World world) =>
        world.MoneySupply.Issued[MoneySupplyTable.Slot].Raw;

    private static long Units(World world, int provider, ResourceId service) =>
        world.UnitsLeft(world.FindAllowance(provider, service));

    private static long Sold(World world, int provider, ResourceId service)
    {
        int allowance = world.FindAllowance(provider, service);
        return allowance == Rows.NoSlot ? 0 : world.ServiceAllowances.SoldToday[allowance];
    }

    /// <summary>The Business a firm's contract names, or <see cref="Rows.NoSlot"/> for a Hinterland.</summary>
    private static int Provider(World world, int buyer, ResourceId service)
    {
        int contract = world.FindContract(buyer, service);

        return contract != Rows.NoSlot
            && world.Businesses.Rows.TryResolve(world.ServiceContracts.Provider[contract], out int office)
                ? office
                : Rows.NoSlot;
    }

    private static int Hinterland(World world, int buyer, ResourceId service)
    {
        int contract = world.FindContract(buyer, service);
        return contract == Rows.NoSlot ? -1 : world.ServiceContracts.Hinterland[contract];
    }

    /// <summary>What an office has taken today, at the Pool price its clients paid.</summary>
    private static long Takings(World world, int office) =>
        (Sold(world, office, Accounting) * AccountingPrice)
        + (Sold(world, office, Insurance) * InsurancePrice);

    /// <summary>
    /// Steps until an office sells something, and reports that one Tick's three readings: what its
    /// clients paid, what its balance gained, and what the Money supply did.
    /// </summary>
    /// <remarks>
    /// One Tick, because an office pays wages out of the same balance on its own pay Day and a window
    /// wider than the sale would mix the two.
    /// </remarks>
    private static (long Paid, long Gained, long Issued) NextSale(
        World world, Simulation sim, int office, int ticks)
    {
        for (int t = 0; t < ticks; t++)
        {
            long paid = Takings(world, office);
            long balance = Balance(world, office);
            long issued = Issued(world);

            sim.Step(default);

            if (Takings(world, office) > paid)
            {
                return (
                    Takings(world, office) - paid,
                    Balance(world, office) - balance,
                    Issued(world) - issued);
            }
        }

        return (0, 0, 0);
    }

    /// <summary>One of the found slots, or a named failure. Slot zero is a real slot, so a
    /// not-found sentinel of zero would read as a match.</summary>
    private static int One(List<int> found, string missing)
    {
        Assert.True(found.Count > 0, missing);
        return found[0];
    }

    /// <summary>Steps until the predicate holds, and reports whether it ever did.</summary>
    private static bool StepUntil(World world, Simulation sim, Func<World, bool> held, int ticks)
    {
        for (int t = 0; t < ticks && !held(world); t++)
        {
            sim.Step(default);
        }

        return held(world);
    }

    /// <summary>Steps until an office's staff have arrived and made the Day's first units.</summary>
    private static bool Supplying(World world, Simulation sim, int office, int ticks = 8 * Ticks.PerDay)
    {
        for (int t = 0; t < ticks && Units(world, office, Accounting) == 0; t++)
        {
            sim.Step(default);

            if (t % Ticks.PerDay == 0)
            {
                Capitalise(world);
            }
        }

        return Units(world, office, Accounting) > 0;
    }

    /// <summary>
    /// Two offices supplying at once: one on the first vacant Lot, one on the second.
    /// </summary>
    /// <remarks>
    /// The city's shops are held broke until both are providers, because a firm keeps a provider that
    /// still supplies it (<c>adr/0178</c>) — whichever office supplied first would otherwise hold
    /// every client and the ranking would never run. A shop with an empty purse proposes a purchase
    /// its affordability walk rejects, so it writes no contract and nothing is decided early.
    /// </remarks>
    private static (int First, int Second) TwoOffices(World world, Simulation sim)
    {
        int second = PlaceOffice(world, skip: 1);
        int first = PlaceOffice(world);

        bool Both(World w) =>
            Units(w, first, Accounting) > 0 && Units(w, second, Accounting) > 0;

        for (int t = 0; t < 8 * Ticks.PerDay && !Both(world); t++)
        {
            Capitalise(world);
            Shops(world).ForEach(shop => Drain(world, shop));
            sim.Step(default);
        }

        Assert.True(Both(world), "the two offices were never both supplying");
        Capitalise(world);

        return (first, second);
    }

    private static List<int> StarvedOf(World world, ResourceId service)
    {
        var starved = new List<int>();

        for (int i = 0; i < world.RuleInstances.Rows.SlotCount; i++)
        {
            if (world.RuleInstances.Rows.IsLive(i)
                && world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[i], out int bin)
                && world.Bins.OwnerKind[bin] == BinOwnerKind.District
                && world.Bins.Resource[bin] == service
                && world.RuleInstances.IsStarving(i))
            {
                starved.Add(i);
            }
        }

        return starved;
    }

    private static List<(Handle<Lot> Lot, byte Kind)> RazeGates(World world)
    {
        var razed = new List<(Handle<Lot>, byte)>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                razed.Add((world.Buildings.Lot[slot], world.Buildings.Kind[slot]));
                world.DestroyBuilding(world.Buildings.Rows.At(slot), world.Tick);
            }
        }

        return razed;
    }

    [Fact]
    public void A_staffed_office_in_reach_is_paid_by_its_clients_and_the_money_supply_does_not_move()
    {
        var (world, sim) = Start(Rules());
        int office = OpenOffice(world, sim);

        (long paid, long gained, long issued) = NextSale(world, sim, office, Ticks.PerDay);

        Assert.True(paid > 0, "no shop ever bought from the office");

        // The purse pays the provider's balance and nobody pays the Outside, so the sale is a
        // transfer -- what the office gained is what its clients spent, at the buyer's market price.
        Assert.Equal(paid, gained);
        Assert.Equal(0, issued);
        Assert.Contains(office, Shops(world).ConvertAll(s => Provider(world, s, Accounting)));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void An_office_with_nobody_at_work_supplies_nothing_and_its_neighbours_import()
    {
        var (world, sim) = Start(Rules());
        int office = PlaceOffice(world);

        Assert.False(Staffed(world, office), "the office was staffed the Tick it was built");

        bool imported = false;
        bool contracted = false;
        bool staffed = false;

        for (int t = 0;
             t < 8 * Ticks.PerDay && world.FindAllowance(office, Accounting) == Rows.NoSlot;
             t++)
        {
            // No allowance row at all is the state the match reads as "this office is not a
            // provider", and it needs no second column saying so.
            contracted |= Shops(world).Exists(s => Provider(world, s, Accounting) != Rows.NoSlot);
            imported |= Shops(world).Exists(s => Hinterland(world, s, Accounting) == North);

            sim.Step(default);
            staffed |= Staffed(world, office);
        }

        Assert.False(contracted, "a shop contracted with an office that had made nothing");
        Assert.True(imported, "no shop imported accounting while the office supplied nothing");
        Assert.True(staffed, "the office made its first units with nobody ever at work");
        sim.CheckEndOfRun();
    }

    [Fact]
    public void Reach_cuts_off_a_distant_office_and_a_remote_service_ignores_the_distance()
    {
        var (world, sim) = Start(Rules(Near()));
        int office = OpenOffice(world, sim);

        int far = One(
            Shops(world).FindAll(s => Walk(world, s, office) > TravelTime.FromMinutes(1)),
            "every shop in this city is within a minute's walk of the office");

        Assert.True(
            StepUntil(world, sim, w => Provider(w, far, Insurance) == office, 2 * Ticks.PerDay),
            "a shop never bought the remote service from the only office making it");

        // Same office, same Tick, two services: the one with a reach is bought from the Hinterland
        // because this shop is too far to walk to, and the remote one is bought locally.
        Assert.Equal(office, Provider(world, far, Insurance));
        Assert.Equal(Rows.NoSlot, Provider(world, far, Accounting));
        Assert.Equal(North, Hinterland(world, far, Accounting));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_spent_allowance_sends_the_next_buyer_to_a_hinterland()
    {
        var (world, sim) = Start(Rules(Scarce()));

        // No proof window here: it would step past the one unit this test is about. The other tests
        // on this fixture carry the Decide proof.
        int office = OpenOffice(world, sim, prove: 0);

        Assert.Equal(1, Units(world, office, Accounting));

        Assert.True(
            StepUntil(
                world,
                sim,
                w => Shops(w).Exists(s => Provider(w, s, Accounting) == office),
                Ticks.PerDay),
            "no shop bought the office's one unit");

        int first = One(
            Shops(world).FindAll(s => Provider(world, s, Accounting) == office), "unreachable");

        // What the office had is gone, and the next buyer is not made to wait for it: a provider with
        // nothing left is not a candidate, so the match falls back to the cheapest gated Hinterland.
        Assert.Equal(0, Units(world, office, Accounting));

        Assert.True(
            StepUntil(
                world,
                sim,
                w => Shops(w).Exists(s => s != first && Hinterland(w, s, Accounting) == North),
                2 * Ticks.PerDay),
            "no second shop imported while the office was sold out");

        // ⚠ The client it served is re-matched too, and that is `Retain`'s rule rather than an
        // oversight: the units test applies to the provider a contract already names, so a standing
        // client of a spent office goes to a Hinterland on its next purchase and comes back when the
        // office makes units again. Both halves are asserted, because the flip alone would also be
        // true of a client that had simply lost its provider.
        Assert.True(
            StepUntil(world, sim, w => Hinterland(w, first, Accounting) == North, 2 * Ticks.PerDay),
            "a standing client of a sold-out office never fell back to a Hinterland");

        Assert.Equal(Rows.NoSlot, Provider(world, first, Accounting));

        Assert.True(
            StepUntil(world, sim, w => Provider(w, first, Accounting) == office, 2 * Ticks.PerDay),
            "a client that fell back to a Hinterland never returned to the office");
        sim.CheckEndOfRun();
    }

    [Fact]
    public void The_nearer_of_two_supplying_offices_takes_the_client()
    {
        var (world, sim) = Start(Rules());
        (int first, int second) = TwoOffices(world, sim);

        int client = One(
            Shops(world).FindAll(shop =>
                Walk(world, shop, first) < Walk(world, shop, second)
                && Walk(world, shop, second) <= TravelTime.FromMinutes(30)),
            "no shop has both offices in reach with one of them strictly nearer");

        Assert.True(
            StepUntil(
                world, sim, w => Provider(w, client, Accounting) != Rows.NoSlot, 2 * Ticks.PerDay),
            "a shop with two offices in reach contracted with neither");

        // Both offices hold units for the whole batch and both are in reach, so the walk decides and
        // the tie-break draw is never reached: shortest wins (02 section 8 rule 5).
        Assert.Equal(first, Provider(world, client, Accounting));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void An_office_out_of_reach_loses_to_one_in_reach_whatever_it_holds()
    {
        var (world, sim) = Start(Rules(Near()));
        (int first, int second) = TwoOffices(world, sim);

        int client = One(
            Shops(world).FindAll(shop =>
                Walk(world, shop, first) <= TravelTime.FromMinutes(1)
                && Walk(world, shop, second) > TravelTime.FromMinutes(1)),
            "no shop has one office inside a minute's walk and the other outside it");

        Assert.True(
            StepUntil(
                world, sim, w => Provider(w, client, Accounting) != Rows.NoSlot, 2 * Ticks.PerDay),
            "a shop with an office a minute away contracted with nobody");

        // The office beyond the reach is not a candidate at all, however many units it holds, so the
        // one inside it wins without the two being compared.
        Assert.Equal(first, Provider(world, client, Accounting));

        // Same two offices, same Tick, the remote service: distance says nothing, so both are
        // candidates and the draw picks between them.
        Assert.True(
            StepUntil(
                world, sim, w => Provider(w, client, Insurance) != Rows.NoSlot, 2 * Ticks.PerDay),
            "a shop never bought the remote service from either office");

        Assert.Contains(Provider(world, client, Insurance), new[] { first, second });
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_tie_on_a_remote_service_is_drawn_and_the_draw_survives_replay_and_reload()
    {
        Ruleset rules = Rules();

        (List<int> Chosen, ulong Hash) Run()
        {
            var (world, sim) = Start(rules);
            (int first, int second) = TwoOffices(world, sim);

            // Every provider of a remote service reports the same distance, so `Office` finds a tie
            // between the two and `PurposeTag.ProviderChoice` is what resolves it.
            Assert.True(
                StepUntil(
                    world,
                    sim,
                    w => Shops(w).Exists(s => Provider(w, s, Insurance) != Rows.NoSlot),
                    2 * Ticks.PerDay),
                "a shop never bought the remote service from either office");

            List<int> chosen = Shops(world)
                .ConvertAll(s => Provider(world, s, Insurance))
                .FindAll(provider => provider != Rows.NoSlot);

            Assert.NotEmpty(chosen);
            Assert.All(chosen, provider => Assert.Contains(provider, new[] { first, second }));
            sim.CheckEndOfRun();

            return (chosen, world.HashState());
        }

        (List<int> chosen, ulong hash) = Run();
        (List<int> again, ulong replayed) = Run();

        Assert.Equal(chosen, again);
        Assert.Equal(hash, replayed);

        // Saved and read back: the contract carries the drawn provider, so nothing is drawn again on
        // the other side of a reload and the two worlds step on together.
        var (world, sim) = Start(rules);
        TwoOffices(world, sim);

        Assert.True(
            StepUntil(
                world,
                sim,
                w => Shops(w).Exists(s => Provider(w, s, Insurance) != Rows.NoSlot),
                2 * Ticks.PerDay),
            "a shop never bought the remote service before the save");

        var file = new MemorySave();
        sim.SaveAtEndOfTick(file);
        sim.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.All(
            Shops(reloaded),
            s => Assert.Equal(Provider(world, s, Insurance), Provider(reloaded, s, Insurance)));

        for (int t = 0; t < 256; t++)
        {
            sim.Step(default);
            resumed.Step(default);
        }

        Assert.Equal(world.HashState(), reloaded.HashState());
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_buyer_needing_a_whole_batch_wakes_when_the_allowance_reaches_it()
    {
        var (world, sim) = Start(Rules(Batched()));
        RazeGates(world);

        Assert.True(
            StepUntil(world, sim, w => StarvedOf(w, Accounting).Count > 0, 2 * Ticks.PerDay),
            "no shop starved of accounting with every gate down");

        int office = OpenOffice(world, sim, prove: 0);

        // From a Day boundary, so that the Day's roll cannot be what wakes the buyer: the allowance
        // starts the Day empty and climbs one unit per firing, and a buyer that needs four sleeps
        // through the first three deposits unless every deposit rings the market.
        Assert.True(
            StepUntil(world, sim, w => w.Tick.Raw % Ticks.PerDay == 0, Ticks.PerDay),
            "the run never reached a Day boundary");

        Capitalise(world);

        int day = BusinessAccounts.DayOf(world.Tick);

        Assert.True(
            StepUntil(world, sim, w => Sold(w, office, Accounting) > 0, Ticks.PerDay - 1),
            "a buyer needing four units slept through the office reaching four");

        Assert.Equal(day, BusinessAccounts.DayOf(world.Tick));
        Assert.True(Sold(world, office, Accounting) >= 4, "the sale was not the whole batch");
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_client_expenses_what_it_paid_an_office_and_the_office_books_the_revenue()
    {
        var (world, sim) = Start(Rules());
        int office = OpenOffice(world, sim);

        // One Tick at a time, and only a Tick with exactly one buyer in it is asserted on: wages and
        // the cost of stock land on the same column, so a wider window could not attribute what
        // moved. A build that books no expense finds no such Tick and fails at the bottom.
        for (int t = 0; t < 2 * Ticks.PerDay; t++)
        {
            long takings = Takings(world, office);
            long revenue = Revenue(world, office);
            List<int> shops = Shops(world);
            List<long> before = shops.ConvertAll(shop => Expense(world, shop));

            sim.Step(default);

            long paid = Takings(world, office) - takings;
            var moved = new List<long>();

            for (int i = 0; i < shops.Count; i++)
            {
                moved.Add(Expense(world, shops[i]) - before[i]);
            }

            if (paid <= 0 || moved.FindAll(delta => delta != 0) is not [long only] || only != paid)
            {
                continue;
            }

            // The office's revenue and its client's expense are the same number, so the pair nets to
            // nothing city-wide and `BusinessTaxEngine` taxes a real profit on each side.
            Assert.Equal(paid, Revenue(world, office) - revenue);
            sim.CheckEndOfRun();

            return;
        }

        Assert.Fail("no Tick in two Days had one buyer whose expense rose by what the office took");
    }

    [Fact]
    public void An_allowance_does_not_accumulate_across_days()
    {
        var (world, sim) = Start(Rules());
        int office = OpenOffice(world, sim);

        // What one Day of `advise` can make: one firing every 64 Ticks, 64 units each.
        long ceiling = Ticks.PerDay / 64 * 64;
        long most = 0;

        // Made-today is reset by the first firing of a new Day rather than by a sweep, so three Days
        // of firing is what shows the reset happening: a figure past one Day's ceiling would be an
        // allowance accumulating across Days.
        for (int t = 0; t < 3 * Ticks.PerDay; t++)
        {
            sim.Step(default);

            int row = world.FindAllowance(office, Accounting);

            if (row != Rows.NoSlot)
            {
                most = Math.Max(most, world.ServiceAllowances.MadeToday[row]);
            }
        }

        Assert.True(most > 0, "the office made nothing in three Days");
        Assert.True(
            most <= ceiling,
            $"an office made {most} units inside one Day, over the {ceiling} its Rule can make");
        sim.CheckEndOfRun();
    }

    [Fact]
    public void Firms_importing_a_service_raise_an_office_that_staffs_up_and_takes_them_on()
    {
        var (world, sim) = Start(Rules(raising: true));
        Capitalise(world);

        Assert.True(
            StepUntil(world, sim, w => Shops(w).Exists(s => Hinterland(w, s, Accounting) == North),
                2 * Ticks.PerDay),
            "no shop ever imported accounting");
        Assert.Empty(Businesses(world, OfficeKind));

        Assert.True(
            StepUntil(world, sim, w => Businesses(w, OfficeKind).Count > 0, 4 * Ticks.PerDay),
            "a city importing accounting never raised an office");

        int office = Businesses(world, OfficeKind)[0];
        Capitalise(world);

        Assert.True(Supplying(world, sim, office), "the raised office never made a unit of accounting");
        Assert.True(
            StepUntil(world, sim, w => Shops(w).Exists(s => Provider(w, s, Accounting) == office),
                2 * Ticks.PerDay),
            "no importer moved to the office the city raised");

        sim.CheckEndOfRun();
    }

    [Fact]
    public void Importers_no_office_reaches_raise_one_office_per_district_and_no_more()
    {
        var (world, sim) = Start(Rules(Near(), raising: true));
        Capitalise(world);

        Assert.True(
            StepUntil(world, sim, w => Businesses(w, OfficeKind).Count > 0, 6 * Ticks.PerDay),
            "a city importing accounting never raised an office");

        for (int t = 0; t < 6 * Ticks.PerDay; t++)
        {
            if (t % Ticks.PerDay == 0)
            {
                Capitalise(world);
            }

            sim.Step(default);
        }

        Assert.True(
            Shops(world).Exists(s => Hinterland(world, s, Accounting) >= 0),
            "no shop is still importing, so nothing tested the bound");
        Assert.InRange(Businesses(world, OfficeKind).Count, 1, world.Districts.Rows.LiveCount);
        sim.CheckEndOfRun();
    }

    /// <summary>
    /// The provider choice's walk work over two Days of a city that raised its own offices. The counts
    /// are deterministic. The wall time is whatever the machine gave the run.
    /// </summary>
    [Fact]
    [Trait(Tier.Key, Tier.Instrument)]
    public void Provider_choice_walk_work_in_a_city_that_raises_its_offices()
    {
        var (world, sim) = Start(Rules(raising: true));
        Capitalise(world);

        Assert.True(
            StepUntil(world, sim, w => Businesses(w, OfficeKind).Count > 0, 6 * Ticks.PerDay),
            "a city importing accounting never raised an office");
        Assert.True(Supplying(world, sim, Businesses(world, OfficeKind)[0]), "the raised office never made a unit");

        var work = new RouteWork();
        int largest = 0;
        work.ObserveFinishedSearchWith(settled => largest = settled > largest ? settled : largest);
        sim.Rules.ProviderRouteWork = work;

        long started = System.Diagnostics.Stopwatch.GetTimestamp();

        for (int t = 0; t < 2 * Ticks.PerDay; t++)
        {
            if (t % Ticks.PerDay == 0)
            {
                Capitalise(world);
            }

            sim.Step(default);
        }

        TimeSpan elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);
        sim.Rules.ProviderRouteWork = null;

        output.WriteLine(
            $"nodes={world.Roads.Nodes.Rows.LiveCount} offices={Businesses(world, OfficeKind).Count} "
            + $"shops={Shops(world).Count} ticks={2 * Ticks.PerDay} searches={work.Searches} "
            + $"settled={work.Settled} largestSearch={largest} stepMs={elapsed.TotalMilliseconds:F0}");

        Assert.True(work.Searches > 0, "no provider choice walked");
    }

    [Fact]
    public void A_firm_on_a_hinterland_contract_switches_to_an_office_that_opens_in_reach()
    {
        var (world, sim) = Start(Rules());

        // Topped up before the wait, because `rates` spends 16,384 a Day out of the same balance the
        // purchase pays from: a shop that has run its capital down buys no service at all, and the
        // import this test needs would never happen.
        Capitalise(world);

        Assert.True(
            StepUntil(world, sim, w => Shops(w).Exists(s => Hinterland(w, s, Accounting) == North),
                2 * Ticks.PerDay),
            "no shop ever imported accounting");

        int importer = One(
            Shops(world).FindAll(s => Hinterland(world, s, Accounting) == North), "unreachable");
        int office = OpenOffice(world, sim);
        Assert.True(
            StepUntil(world, sim, w => Provider(w, importer, Accounting) == office, 2 * Ticks.PerDay),
            "an importer never took up the office in its own city");

        // A firm keeps its provider while that provider supplies it (adr/0178), so the switch happens
        // on the re-match an import already runs, and a city provider wins it.
        Assert.Equal(-1, Hinterland(world, importer, Accounting));
        sim.CheckEndOfRun();
    }

    [Fact]
    public void Demolishing_an_office_frees_its_allowance_and_sends_its_clients_back_to_the_pool()
    {
        var (world, sim) = Start(Rules());
        int office = OpenOffice(world, sim);
        Assert.True(
            StepUntil(world, sim, w => Shops(w).Exists(s => Provider(w, s, Accounting) == office),
                Ticks.PerDay),
            "no shop ever contracted with the office");

        int client = One(
            Shops(world).FindAll(s => Provider(world, s, Accounting) == office), "unreachable");
        int contract = world.FindContract(client, Accounting);
        Handle<Building> premises = world.Businesses.Building[office];

        world.DestroyBuilding(premises, world.Tick);

        // Severable: the client's contract outlives the provider and the stale handle IS the fact it
        // re-matches on. The provider's own allowance rows are freed with it.
        Assert.False(world.Businesses.Rows.IsLive(office));
        Assert.True(world.ServiceContracts.Rows.IsLive(contract));
        Assert.Equal(Rows.NoSlot, Provider(world, client, Accounting));
        Assert.Equal(0, LiveAllowances(world));

        Capitalise(world);

        Assert.True(
            StepUntil(world, sim, w => Hinterland(w, client, Accounting) == North, 2 * Ticks.PerDay),
            "a client of a demolished office never went back to the Pool");
        sim.CheckEndOfRun();
    }

    private static int LiveAllowances(World world)
    {
        int live = 0;

        for (int row = 0; row < world.ServiceAllowances.Rows.SlotCount; row++)
        {
            if (world.ServiceAllowances.Rows.IsLive(row))
            {
                live++;
            }
        }

        return live;
    }

    [Fact]
    public void A_firm_stalled_on_the_service_market_wakes_when_an_office_first_makes_units()
    {
        var (world, sim) = Start(Rules());
        RazeGates(world);

        Assert.True(
            StepUntil(world, sim, w => StarvedOf(w, Accounting).Count > 0, 2 * Ticks.PerDay),
            "no shop starved of accounting with every gate down");

        int office = OpenOffice(world, sim);

        // The first units arrive in Phase 3 and wake the market row's waiters for the next Tick, so a
        // firm that stopped asking starts again without polling (adr/0045).
        for (int t = 0; t < 16; t++)
        {
            sim.Step(default);
        }

        Assert.Empty(StarvedOf(world, Accounting));

        (long paid, long gained, long issued) = NextSale(world, sim, office, 2 * Ticks.PerDay);

        Assert.True(paid > 0, "a woken firm never bought from the office");
        Assert.Equal(paid, gained);
        Assert.Equal(0, issued);
        sim.CheckEndOfRun();
    }

    [Fact]
    public void An_allowance_and_its_contracts_survive_save_and_reload()
    {
        Ruleset rules = Rules();
        var (world, sim) = Start(rules);
        int office = OpenOffice(world, sim);
        Assert.True(
            StepUntil(world, sim, w => Sold(w, office, Accounting) > 0, Ticks.PerDay),
            "nothing was sold before the save");
        Assert.True(world.Tick.Raw % Ticks.PerDay != 0, "the save landed on a Day boundary");

        var file = new MemorySave();
        sim.SaveAtEndOfTick(file);
        sim.Step(default);

        World reloaded = SaveFile.Read(file, rules, out SaveHeader header);
        var resumed = new Simulation(reloaded, header.Key) { VerifyDecideWritesNothing = false };

        Assert.Equal(world.HashState(), reloaded.HashState());
        Assert.Equal(LiveAllowances(world), LiveAllowances(reloaded));
        Assert.Equal(Sold(world, office, Accounting), Sold(reloaded, office, Accounting));
        Assert.Equal(Units(world, office, Accounting), Units(reloaded, office, Accounting));
        Assert.All(
            Shops(reloaded),
            s => Assert.Equal(Provider(world, s, Accounting), Provider(reloaded, s, Accounting)));

        for (int t = 0; t < Ticks.PerDay; t++)
        {
            sim.Step(default);
            resumed.Step(default);
        }

        Assert.Equal(world.HashState(), reloaded.HashState());
        sim.CheckEndOfRun();
    }

    [Fact]
    public void A_replayed_run_reaches_the_same_state()
    {
        Ruleset rules = Rules();
        ulong Run()
        {
            var (world, sim) = Start(rules);
            int office = OpenOffice(world, sim);

            for (int t = 0; t < 512; t++)
            {
                sim.Step(default);
            }

            Assert.True(Sold(world, office, Accounting) > 0, "the office sold nothing");
            sim.CheckEndOfRun();
            return world.HashState();
        }

        Assert.Equal(Run(), Run());
    }
}
