using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>plans/0045</c> row 31 task 5: which edits to a counted Outside a standing city can take, and
/// which ones it refuses.
/// </summary>
/// <remarks>
/// <para>
/// <b>The line is depth against speed</b> (<c>plans/0073</c> D12). What a home costs out there, how
/// central it feels, what an emigrant carries, how often anybody reconsiders and how fast the stock
/// refills are all comparisons the city makes again every Tick, so they retune while the world runs.
/// How many Households stand behind an edge and what they are made of is the state itself, and an
/// edit to it would have to invent or destroy people.
/// </para>
/// <para>
/// ⚠ <b>A refused reload leaves the world untouched, and that is asserted by State Hash</b> rather
/// than by inspection. A refusal that had already moved a fraction or a quota would be a reload that
/// half happened, which is the one outcome <c>adr/0015</c> rules out.
/// </para>
/// </remarks>
public sealed class HinterlandReloadTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(7);

    private static string Text(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static Ruleset Parsed(string text, string name)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, name);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>The shipped stock world with one line rewritten.</summary>
    private static Ruleset Edited(string from, string to)
    {
        string text = Text("attracted.toml");

        Assert.Contains(from, text, StringComparison.Ordinal);

        int at = text.IndexOf(from, StringComparison.Ordinal);

        return Parsed(text[..at] + to + text[(at + from.Length)..], "edited.toml");
    }

    private static (World World, Simulation Simulation) City()
    {
        var world = new World(1_000, Parsed(Text("attracted.toml"), "attracted.toml"), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        return (world, simulation);
    }

    private static void Step(Simulation simulation, int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            simulation.Step(default);
        }
    }

    /// <summary>The first live group behind the west edge.</summary>
    private static int Group(World world)
    {
        foreach (int slot in world.HinterlandPopulation.Groups(world.Hinterlands)
            .Walk(HinterlandTable.SlotOf(MapEdge.West)))
        {
            return slot;
        }

        throw new InvalidOperationException("nobody stands behind the west edge.");
    }

    private static long Stock(World world)
    {
        long stock = 0;

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot))
            {
                stock += world.HinterlandPopulation.Stock[slot];
            }
        }

        return stock;
    }

    // ---- what retunes ---------------------------------------------------------------------------

    /// <summary>What a home costs out there is a comparison, so it moves and nobody does.</summary>
    [Fact]
    public void An_Outside_rent_edit_changes_the_comparison_and_not_the_population()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 64);

        long standing = Stock(world);

        world.Adopt(Edited("rent = 900", "rent = 1500"), 0, world.Tick, Key);

        Assert.True(world.Rules.TryHinterland(MapEdge.West, out HinterlandDefinition west));
        Assert.Equal(1_500, west.Rent.Raw);
        Assert.Equal(standing, Stock(world));

        Step(simulation, 64);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A retuned cadence keeps the progress the old one had accrued.</summary>
    /// <remarks>
    /// <b>The fraction is meaningless without its denominator</b> (D12). Carrying it across unchanged
    /// would fire the next occasion early against a longer period; clearing it would make every
    /// retune cost the Outside the progress it had made.
    /// </remarks>
    [Fact]
    public void A_retuned_reconsider_period_rescales_the_fraction_it_had()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 37);

        int group = Group(world);
        long before = world.HinterlandPopulation.ReconsiderNumerator[group];

        Assert.True(before > 0, "no group had part of an occasion accrued, so nothing is being kept.");

        world.Adopt(
            Edited("reconsider_days       = 2", "reconsider_days       = 4"), 0, world.Tick, Key);

        Assert.Equal(
            before * 2, world.HinterlandPopulation.ReconsiderNumerator[group]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Switching recovery off clears the fraction and the direction it was moving in.</summary>
    [Fact]
    public void Recovery_switched_off_starts_again_from_nothing_when_it_returns()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, Ticks.PerDay);

        world.Adopt(Edited("recovery_days         = 32", "recovery_days         = 0"), 0, world.Tick, Key);

        for (int slot = 0; slot < world.HinterlandPopulation.Rows.SlotCount; slot++)
        {
            if (world.HinterlandPopulation.Rows.IsLive(slot))
            {
                Assert.Equal(0, world.HinterlandPopulation.RecoveryNumerator[slot]);
                Assert.Equal(0, world.HinterlandPopulation.RecoveryDirection[slot]);
            }
        }

        Step(simulation, 64);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A family already waiting keeps the purse it was evaluated with.</summary>
    /// <remarks>
    /// <b>A redrawn purse would make waiting a lottery over wealth</b> (D5), and the affordability
    /// test on the other side of the wait would then be about somebody else.
    /// </remarks>
    [Fact]
    public void A_retuned_purse_range_leaves_a_queued_family_holding_what_it_drew()
    {
        (World world, Simulation simulation) = City();

        Waiting(world, simulation);

        int head = FirstWaiting(world);
        Money purse = world.HinterlandQueue.Purse[head];

        world.Adopt(
            Edited("emigrant_balance_min = 800", "emigrant_balance_min = 2500"), 0, world.Tick, Key);

        Assert.Equal(purse, world.HinterlandQueue.Purse[head]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A quota cut below what a door has already taken shuts it for the rest of the Day.</summary>
    /// <remarks>
    /// <b>The meter counts what a Day has spent and the Ruleset states the ceiling</b>, so a lowered
    /// ceiling is read against a meter that has already passed it. The alternative — carrying the old
    /// ceiling until the Day rolls — would make a reload take effect at a time nobody asked for.
    /// </remarks>
    [Fact]
    public void A_quota_cut_below_what_a_door_has_taken_admits_nobody_else()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 64);
        EveryDoorTakesOne(world);

        long admitted = world.PopulationLedger.Admissions[PopulationLedgerTable.Slot];
        long standing = Stock(world);

        Assert.True(admitted > 0, "nobody had been admitted, so a shut door proves nothing.");

        foreach (int slot in Gates(world))
        {
            Assert.True(
                world.GateHasRoom(slot, world.Tick),
                "a door was already shut before the quota was cut.");
        }

        world.Adopt(Edited("arrivals_per_day = 96", "arrivals_per_day = 1"), 0, world.Tick, Key);

        foreach (int slot in Gates(world))
        {
            Assert.False(
                world.GateHasRoom(slot, world.Tick),
                "a door that had taken one family still had room under a quota of one.");
        }

        while (world.Tick.Raw % Ticks.PerDay != 0)
        {
            simulation.Step(default);
        }

        Assert.Equal(admitted, world.PopulationLedger.Admissions[PopulationLedgerTable.Slot]);
        Assert.Equal(standing, Stock(world));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A retune that empties a band keeps the stock standing in it, and reopening it restores it.</summary>
    /// <remarks>
    /// <b>Authoring stock into an empty band is refused and arriving at one by retune is not</b>
    /// (D12). A band is a third of the purse range, so narrowing the range empties the upper bands —
    /// and a group already filed under one is counted, recovers and turns over as before. Deleting or
    /// refiling it would be a stock edit performed by the loader.
    /// </remarks>
    [Fact]
    public void A_retune_that_empties_a_band_keeps_the_stock_standing_in_it()
    {
        var world = new World(1_000, Parsed(OneBandWest(), "one-band.toml"), Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        var simulation = new Simulation(world, Key) { VerifyDecideWritesNothing = false };

        HinterlandComposition young = HinterlandComposition.Of(world.Rules.HinterlandPopulations[0]);
        int rich = world.ReturnToHinterland(MapEdge.West, young with { MoneyBand = 2 });

        Assert.True(world.Rules.TryHinterland(MapEdge.West, out HinterlandDefinition wide));
        Assert.True(wide.DeclaresBand(2));

        world.Adopt(Parsed(NarrowWest(), "narrow.toml"), 0, world.Tick, Key);

        Assert.True(world.Rules.TryHinterland(MapEdge.West, out HinterlandDefinition narrow));
        Assert.False(narrow.DeclaresBand(2), "the retune was supposed to leave the top band empty.");

        Assert.True(world.HinterlandPopulation.Rows.IsLive(rich));
        Assert.Equal(2, world.HinterlandPopulation.MoneyBand[rich]);
        Assert.Equal(1, world.HinterlandPopulation.Stock[rich]);

        Step(simulation, 64);

        world.Adopt(Parsed(OneBandWest(), "one-band.toml"), 0, world.Tick, Key);

        Assert.True(world.Rules.TryHinterland(MapEdge.West, out HinterlandDefinition reopened));
        Assert.True(reopened.DeclaresBand(2));
        Assert.Equal(1, world.HinterlandPopulation.Stock[rich]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A waiting family is held to the duration in force, and keeps the Tick it joined at.</summary>
    /// <remarks>
    /// <b>The wait is measured rather than counted down</b> (D12), so the Tick a family joined at is
    /// what a retune must leave alone. Restarting it would give everybody already queued a fresh full
    /// wait; clearing it would expire the lot on the next pass.
    /// </remarks>
    [Fact]
    public void A_longer_wait_keeps_the_Tick_a_family_joined_at()
    {
        (World world, Simulation simulation) = City();

        Waiting(world, simulation);

        int head = FirstWaiting(world);
        Ticks since = world.HinterlandQueue.Since[head];
        Ticks reviewed = world.HinterlandQueue.Reviewed[head];

        world.Adopt(
            Edited("queue_wait_days       = 2", "queue_wait_days       = 4"), 0, world.Tick, Key);

        Assert.Equal(since, world.HinterlandQueue.Since[head]);
        Assert.Equal(reviewed, world.HinterlandQueue.Reviewed[head]);

        ulong expired = since.Raw + (2UL * Ticks.PerDay);

        while (world.Tick.Raw <= expired + 1)
        {
            ShutTheDoors(world);
            simulation.Step(default);
        }

        Assert.True(
            world.HinterlandQueue.Rows.IsLive(head) && world.HinterlandQueue.Since[head] == since,
            "the family expired against the wait it joined under rather than the one now in force.");

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Recovery switched back on starts from nothing and still empties the group.</summary>
    /// <remarks>
    /// ⚠ <b>The pair with the switch-off case is the test.</b> A cleared fraction that no later
    /// reload could refill would make disabling recovery permanent, and the Outside would keep every
    /// group the city ever returned to it.
    /// </remarks>
    [Fact]
    public void Recovery_switched_back_on_starts_from_zero_and_still_drains()
    {
        (World world, Simulation simulation) = City();

        HinterlandComposition young = HinterlandComposition.Of(world.Rules.HinterlandPopulations[0]);
        HinterlandComposition unauthored = young with { Children = 4 };

        int group = world.ReturnToHinterland(MapEdge.West, unauthored);

        Assert.Equal(0, world.HinterlandPopulation.Target[group]);

        world.Adopt(
            Edited("recovery_days         = 32", "recovery_days         = 0"), 0, world.Tick, Key);

        Step(simulation, 256);

        Assert.Equal(1, world.HinterlandPopulation.Stock[group]);

        world.Adopt(
            Edited("recovery_days         = 32", "recovery_days         = 1"), 0, world.Tick, Key);

        Assert.Equal(0, world.HinterlandPopulation.RecoveryNumerator[group]);
        Assert.Equal(0, world.HinterlandPopulation.RecoveryDirection[group]);

        for (int tick = 0; tick <= Ticks.PerDay; tick++)
        {
            simulation.Step(default);
        }

        Assert.False(
            world.HinterlandCompositions.TryFind(
                world.HinterlandPopulation, MapEdge.West, unauthored, out _),
            "the group stood through a whole recovery period after recovery came back.");

        world.Invariants.RunEndOfRun(world);
    }

    // ---- what is refused ------------------------------------------------------------------------

    /// <summary>A counted Outside cannot be switched on or off under a standing city.</summary>
    [Fact]
    public void Taking_the_Outside_away_is_refused()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 32);

        Refused(world, Parsed(Text("bordered.toml"), "bordered.toml"));
    }

    /// <summary>A composition edit is refused, and refused even beside an edit that would be allowed.</summary>
    /// <remarks>
    /// 🔴 <b>The pair is the test.</b> A refusal that depended on which difference was noticed first
    /// would let a composition change through whenever a rent change sat beside it, and the stock
    /// would then be filed under a key nothing declares.
    /// </remarks>
    [Fact]
    public void Changing_a_composition_is_refused_alongside_a_change_that_would_be_allowed()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 32);

        string text = Text("attracted.toml")
            .Replace("rent = 900", "rent = 1500", StringComparison.Ordinal);

        int at = text.IndexOf("children        = 2", StringComparison.Ordinal);

        Refused(
            world,
            Parsed(text[..at] + "children        = 3" + text[(at + "children        = 2".Length)..],
                "edited.toml"));
    }

    /// <summary>Moving a resting count is moving people, so it is refused.</summary>
    /// <remarks>
    /// <b>Target immutable, recovery speed tunable</b> (D12). A lowered target is a population the
    /// recovery pass would remove as turnover, which is a Shock under another name.
    /// </remarks>
    [Fact]
    public void Changing_a_resting_count_is_refused()
    {
        (World world, Simulation simulation) = City();

        Step(simulation, 32);

        Refused(world, Edited("households      = 600", "households      = 500"));
    }

    /// <summary>A refusal leaves the Ruleset in force, the State Hash and the stock exactly as they were.</summary>
    private static void Refused(World world, Ruleset rules)
    {
        Ruleset was = world.Rules;
        ulong hash = world.HashState();
        long standing = Stock(world);

        Assert.ThrowsAny<Exception>(() => world.Adopt(rules, 0, world.Tick, Key));

        Assert.Same(was, world.Rules);
        Assert.Equal(hash, world.HashState());
        Assert.Equal(standing, Stock(world));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Shuts every door and steps until somebody is standing at one.</summary>
    private static void Waiting(World world, Simulation simulation)
    {
        for (int tick = 0; tick < 8 * Ticks.PerDay; tick++)
        {
            ShutTheDoors(world);
            simulation.Step(default);

            if (world.HinterlandQueue.Rows.LiveCount > 0)
            {
                return;
            }
        }

        Assert.Fail("no family ever waited, so there is no queued purse to keep.");
    }

    private static int FirstWaiting(World world)
    {
        for (int slot = 0; slot < world.HinterlandQueue.Rows.SlotCount; slot++)
        {
            if (world.HinterlandQueue.Rows.IsLive(slot))
            {
                return slot;
            }
        }

        throw new InvalidOperationException("nobody is waiting.");
    }

    /// <summary>Every live gate in the world.</summary>
    private static List<int> Gates(World world)
    {
        var gates = new List<int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                gates.Add(slot);
            }
        }

        Assert.NotEmpty(gates);

        return gates;
    }

    /// <summary>Gives every door a Day in which it has taken exactly one family.</summary>
    private static void EveryDoorTakesOne(World world)
    {
        foreach (int slot in Gates(world))
        {
            world.Buildings.ArrivalDay[slot] = (int)(world.Tick.Raw / Ticks.PerDay);
            world.Buildings.ArrivalsToday[slot] = 1;
        }
    }

    /// <summary>The shipped world with only the lowest-band group behind west.</summary>
    /// <remarks>
    /// <b>So the purse range can be narrowed without the loader refusing the file.</b> Stock authored
    /// into a band the range does not reach is refused at load, which is the rule this fixture stays
    /// inside — the group that ends up in an empty band is one the city returned, not one the file
    /// states.
    /// </remarks>
    private static string OneBandWest()
    {
        string text = Text("attracted.toml");

        int family = text.IndexOf(
            "[[hinterland.population]]\nstage           = \"family\"", StringComparison.Ordinal);
        int south = text.IndexOf("[[hinterland]]\nedge = \"south\"", StringComparison.Ordinal);

        Assert.InRange(family, 0, south);

        return text[..family] + text[south..];
    }

    /// <summary>The same world with west's purse range cut to a single amount.</summary>
    private static string NarrowWest() =>
        OneBandWest().Replace(
            "emigrant_balance_max = 4000", "emigrant_balance_max = 800", StringComparison.Ordinal);

    private static void ShutTheDoors(World world)
    {
        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            world.Buildings.ArrivalDay[slot] = (int)(world.Tick.Raw / Ticks.PerDay);
            world.Buildings.ArrivalsToday[slot] =
                world.Rules.Kind(world.Buildings.Kind[slot]).ArrivalsPerDay;
        }
    }
}
