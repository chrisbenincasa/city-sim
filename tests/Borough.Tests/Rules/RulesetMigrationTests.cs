using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 8 task 4, sub-tasks B and C: the migration map, and the one pass over the world.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three degradations, and the order between them is the mechanism rather than an implementation
/// detail.</b> <c>02 §4.3</c>: Bins whose Resource no longer exists are dropped, Buildings whose kind
/// no longer exists are <em>derelict rather than deleted</em>, and <b>all wait lists are dropped and
/// every Rule is woken with a stagger</b> — the last one first, because a subscription taken under
/// the old Ruleset may name a Bin the new one is about to free.
/// </para>
/// <para>
/// <b>What makes any of it possible is that a declaration's identity is its name.</b> Removing a
/// declaration from the middle of a file renumbers everything below it, so <em>every</em> real
/// removal these tests exist for is also a reordering — and a degradation that mapped ids positionally
/// would quietly relabel the survivors while it was busy derelicting the casualty.
/// </para>
/// </remarks>
public sealed class RulesetMigrationTests
{
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;

    private const byte Dwelling = 1;

    /// <summary>
    /// Two Goods, one kind holding a Bin of each, and two Rules — one that always succeeds and one
    /// that can never fire.
    /// </summary>
    /// <remarks>
    /// <b><c>upkeep</c> draws on a Resource nothing produces, which is <c>minimal.toml</c>'s shape and
    /// is here for the same reason:</b> it is the only way to get a Rule Instance that is genuinely
    /// <em>asleep on a Bin</em> rather than merely idle, and a wait list that is never occupied cannot
    /// be shown to be dropped.
    /// </remarks>
    private const string Both = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"

        [[building]]
        name = "dwelling"
        bins = [
            { resource = "sundries", capacity = 12 },
            { resource = "repairs",  capacity = 4 },
        ]

        [[rule]]
        name    = "restock"
        kind    = "dwelling"
        rate    = 8
        apply   = { min = 1, max = 4 }
        inputs  = []
        outputs = [ { scope = "local", resource = "sundries", amount = 1 } ]

        [[rule]]
        name    = "upkeep"
        kind    = "dwelling"
        rate    = 16
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []
        """;

    /// <summary>The same file with its two Resources declared the other way round.</summary>
    private const string Reordered = """
        [[resource]]
        name = "repairs"
        family = "good"

        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        bins = [
            { resource = "sundries", capacity = 12 },
            { resource = "repairs",  capacity = 4 },
        ]

        [[rule]]
        name    = "restock"
        kind    = "dwelling"
        rate    = 8
        apply   = { min = 1, max = 4 }
        inputs  = []
        outputs = [ { scope = "local", resource = "sundries", amount = 1 } ]

        [[rule]]
        name    = "upkeep"
        kind    = "dwelling"
        rate    = 16
        apply   = { min = 1, max = 1 }
        inputs  = [ { scope = "local", resource = "repairs", amount = 1 } ]
        outputs = []
        """;

    /// <summary><c>repairs</c> deleted, taking its Bin and the Rule that drew on it with it.</summary>
    private const string SundriesOnly = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        bins = [
            { resource = "sundries", capacity = 12 },
        ]

        [[rule]]
        name    = "restock"
        kind    = "dwelling"
        rate    = 8
        apply   = { min = 1, max = 4 }
        inputs  = []
        outputs = [ { scope = "local", resource = "sundries", amount = 1 } ]
        """;

    /// <summary>Both Resources, and no kind at all. Every Building in the city is derelict.</summary>
    private const string NoKinds = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "good"
        """;

    private const string TwoGoods = """
        [[resource]]
        name = "flour"
        family = "good"

        [[resource]]
        name = "bread"
        family = "good"
        """;

    private const string FlourIsMoney = """
        [[resource]]
        name = "flour"
        family = "money"

        [[resource]]
        name = "bread"
        family = "good"
        """;

    /// <summary>
    /// A third Resource <em>and</em> a family change, which is the pair that separates the map from
    /// <see cref="RulesetShape.Compare"/>.
    /// </summary>
    private const string WiderAndFlourIsMoney = """
        [[resource]]
        name = "flour"
        family = "money"

        [[resource]]
        name = "bread"
        family = "good"

        [[resource]]
        name = "yeast"
        family = "good"
        """;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0001UL);

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>Three dwellings, and enough Ticks for every Rule to have fired and then subscribed.</summary>
    private static (World World, Simulation Simulation) City(
        Ruleset opening, RulesetCatalogue? catalogue = null, int ticks = 64, int houses = 3)
    {
        var world = new World(1_000, opening);
        var simulation = new Simulation(world, Key, catalogue ?? RulesetCatalogue.None);

        for (int i = 0; i < houses; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), zone: 1);
            world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);
        }

        for (int i = 0; i < ticks; i++)
        {
            simulation.Step(new TickInput(default, HashA));
        }

        return (world, simulation);
    }

    /// <summary>The first dwelling. The three are raised into slots 0, 1 and 2 and none is freed.</summary>
    private const int First = 0;

    // ---- the map ------------------------------------------------------------------------------

    /// <summary>
    /// A declaration that moved is <em>followed</em>, not renumbered.
    /// </summary>
    [Fact]
    public void The_map_follows_a_declaration_that_moved_position()
    {
        RulesetMigration migration = RulesetMigration.Between(Load(Both), Load(Reordered));

        Assert.Equal(new ResourceId(2), migration.Resource(new ResourceId(1)));
        Assert.Equal(new ResourceId(1), migration.Resource(new ResourceId(2)));
        Assert.Equal(Dwelling, migration.Kind(Dwelling));
    }

    /// <summary>A declaration that went maps to zero, which is the only spelling of <em>gone</em>.</summary>
    [Fact]
    public void The_map_sends_a_deleted_declaration_to_zero()
    {
        RulesetMigration resources = RulesetMigration.Between(Load(Both), Load(SundriesOnly));
        RulesetMigration kinds = RulesetMigration.Between(Load(Both), Load(NoKinds));

        Assert.Equal(new ResourceId(1), resources.Resource(new ResourceId(1)));
        Assert.Equal(0, resources.Resource(new ResourceId(2)).Raw);
        Assert.Equal(0, kinds.Kind(Dwelling));
    }

    // ---- the residual refusal ----------------------------------------------------------------

    /// <summary>
    /// A surviving Resource that changes family is refused, because <c>adr/0024</c> conservation is
    /// not a degradation.
    /// </summary>
    [Fact]
    public void A_resource_that_changes_family_is_refused_rather_than_degraded()
    {
        RulesetMigration migration = RulesetMigration.Between(Load(TwoGoods), Load(FlourIsMoney));

        Assert.Equal(1, migration.FamilyChanged.Raw);
    }

    /// <summary>
    /// <b>The refusal cannot be read off <see cref="RulesetShape.Compare"/>, and this is the file that
    /// proves it.</b>
    /// </summary>
    /// <remarks>
    /// <c>Compare</c> returns the <em>first</em> way two Rulesets differ, and the Resource count is
    /// compared before any family is. So a file that adds a Resource and refamilies another reports
    /// <see cref="RulesetChange.ResourceCount"/> and never looks at the families — harmless while
    /// every structural difference was refused outright, and a silent hole the moment they stop being.
    /// </remarks>
    [Fact]
    public void The_family_refusal_survives_a_file_that_also_adds_a_resource()
    {
        Ruleset current = Load(TwoGoods);
        Ruleset replacement = Load(WiderAndFlourIsMoney);

        Assert.Equal(RulesetChange.ResourceCount, RulesetShape.Compare(current, replacement));
        Assert.Equal(1, RulesetMigration.Between(current, replacement).FamilyChanged.Raw);
    }

    /// <summary>And the reload itself refuses, leaving the previous Ruleset live.</summary>
    [Fact]
    public void A_reload_that_changes_a_family_leaves_the_previous_ruleset_live()
    {
        Ruleset opening = Load(TwoGoods);
        var world = new World(1_000, opening);

        Assert.Throws<NotSupportedException>(
            () => world.Adopt(Load(FlourIsMoney), Ticks.Zero, Key));

        Assert.Same(opening, world.Rules);
    }

    // ---- the pass -----------------------------------------------------------------------------

    /// <summary>
    /// A reload that moves only numbers still changes no row, which is task 1's behaviour kept.
    /// </summary>
    /// <remarks>
    /// <b>The State Hash is the test, because it is the project's own definition of the difference</b>
    /// — a change is an optimisation if the hash is unchanged and a design change otherwise. A tuning
    /// swap that re-armed every Rule Instance in the city would be a design change wearing a
    /// performance argument.
    /// </remarks>
    [Fact]
    public void A_tuning_reload_changes_no_row()
    {
        (World world, _) = City(Load(Both));

        ulong before = world.HashState();
        RulesetDegradation cost = world.Adopt(
            Load(Both.Replace("rate    = 8", "rate    = 12", StringComparison.Ordinal)),
            new Ticks(64),
            Key);

        Assert.True(cost.IsNothing);
        Assert.Equal(before, world.HashState());
    }

    /// <summary>A live Bin follows its Resource across a reordering, keeping its stock.</summary>
    /// <remarks>
    /// <b>This is the failure sub-task A found, seen from the row rather than from the comparison.</b>
    /// Without the map the Bin would keep id 1 and silently begin naming <c>repairs</c> — the city
    /// converting one Good into another with nothing anywhere recording that it happened.
    /// </remarks>
    [Fact]
    public void A_bin_follows_its_resource_across_a_reordering()
    {
        (World world, _) = City(Load(Both));

        int sundries = world.FindBin(First, new ResourceId(1));
        int level = world.Bins.LevelAt(sundries);

        Assert.True(level > 0, "restock produced nothing, so there is no stock to follow.");

        RulesetDegradation cost = world.Adopt(Load(Reordered), new Ticks(64), Key);

        Assert.Equal(0, cost.BinsDropped);
        Assert.Equal(0, cost.BuildingsDerelicted);

        // The same row, holding the same stock, now filed under the id the new file gives sundries.
        Assert.Equal(sundries, world.FindBin(First, new ResourceId(2)));
        Assert.Equal(new ResourceId(2), world.Bins.Resource[sundries]);
        Assert.Equal(level, world.Bins.LevelAt(sundries));
    }

    /// <summary>A Bin whose Resource is not declared any more is dropped, and the drop is reported.</summary>
    [Fact]
    public void A_bin_whose_resource_went_is_dropped_and_counted()
    {
        (World world, _) = City(Load(Both));

        Handle<Bin> repairs = world.Bins.Rows.At(world.FindBin(First, new ResourceId(2)));

        RulesetDegradation cost = world.Adopt(Load(SundriesOnly), new Ticks(64), Key);

        Assert.Equal(3, cost.BinsDropped);
        Assert.Equal(0, cost.BuildingsDerelicted);
        Assert.False(world.Bins.Rows.IsValid(repairs));

        // The survivor is still there, still owned, and still holding what it held.
        Assert.NotEqual(Core.Tables.Rows.NoSlot, world.FindBin(First, new ResourceId(1)));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// <b>A Building whose kind went is derelict rather than deleted, and dereliction is
    /// <c>Kind == 0</c> with no flag anywhere.</b>
    /// </summary>
    /// <remarks>
    /// It keeps its Lot, its Bins and its Occupants and runs nothing, which is what makes the state
    /// legible: a designer who deleted a kind sees the Buildings still standing and inert, rather than
    /// a city that quietly lost a district. Deleting them would be the silent corruption
    /// <c>adr/0015</c> exists to prevent, arriving through the mechanism written to serve it.
    /// </remarks>
    [Fact]
    public void A_building_whose_kind_went_is_derelict_rather_than_deleted()
    {
        (World world, _) = City(Load(Both));

        Handle<Household> resident = world.CreateHousehold(world.Buildings.Rows.At(First), 0);

        RulesetDegradation cost = world.Adopt(Load(NoKinds), new Ticks(64), Key);

        Assert.Equal(3, cost.BuildingsDerelicted);
        Assert.Equal(0, cost.BinsDropped);
        Assert.Equal(0, cost.RuleInstancesRearmed);

        Assert.True(world.Buildings.Rows.IsLive(First));
        Assert.Equal(0, world.Buildings.Kind[First]);
        Assert.True(world.Households.Rows.IsValid(resident));
        Assert.NotEqual(Core.Tables.Rows.NoSlot, world.FindBin(First, new ResourceId(1)));

        Assert.Equal(0, world.RuleInstances.Rows.LiveCount);
        Assert.True(world.BuildingRules.IsEmpty(First));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A kind removed and re-added does not come back, and that is stated rather than fixed.
    /// </summary>
    /// <remarks>
    /// <b><c>Kind</c> is set to zero, so what the Building <em>was</em> is forgotten.</b> Remembering
    /// it costs a key per Building row for a case that recovers by reloading a save, and keeping the
    /// stale id instead would be worse than forgetting: kind ids are positional, so a re-added
    /// declaration at a different position would silently make every one of those Buildings a
    /// different species — which is the identity defect sub-task A was written to close.
    /// <c>HONEST DEGRADATION</c>.
    /// </remarks>
    [Fact]
    public void A_kind_removed_and_re_added_does_not_come_back()
    {
        (World world, _) = City(Load(Both));

        world.Adopt(Load(NoKinds), new Ticks(64), Key);
        RulesetDegradation cost = world.Adopt(Load(Both), new Ticks(65), Key);

        Assert.Equal(0, world.Buildings.Kind[First]);
        Assert.Equal(0, cost.RuleInstancesRearmed);
    }

    /// <summary>
    /// <b>Every Rule Instance in the world is dropped and re-armed, so a wait list is never
    /// cross-version state.</b>
    /// </summary>
    /// <remarks>
    /// <c>upkeep</c> draws on a Bin nothing fills, so by the time the reload happens every dwelling
    /// has one Rule asleep on it for ever. After the swap none of them is waiting: the subscription
    /// could have named a Bin the new file does not declare, and there is no way to tell from the row
    /// which of those it was.
    /// </remarks>
    [Fact]
    public void Every_rule_instance_is_dropped_and_re_armed_on_the_stagger()
    {
        (World world, _) = City(Load(Both));

        int asleep = 0;

        for (int slot = 0; slot < world.RuleInstances.Rows.SlotCount; slot++)
        {
            if (world.RuleInstances.Rows.IsLive(slot) && world.RuleInstances.IsWaiting(slot))
            {
                asleep++;
            }
        }

        Assert.True(asleep > 0, "no Rule ever subscribed, so there is no wait list to drop.");

        var now = new Ticks(64);
        RulesetDegradation cost = world.Adopt(Load(Reordered), now, Key);

        // Two Rules on each of three dwellings, all of them new rows.
        Assert.Equal(6, cost.RuleInstancesRearmed);
        Assert.Equal(6, world.RuleInstances.Rows.LiveCount);

        for (int slot = 0; slot < world.RuleInstances.Rows.SlotCount; slot++)
        {
            if (!world.RuleInstances.Rows.IsLive(slot))
            {
                continue;
            }

            Assert.False(world.RuleInstances.IsWaiting(slot));

            // Uniform over [1, rate], which is slice 7's stagger reused rather than re-derived: the
            // longest rate in the file is 16.
            Ticks next = world.RuleInstances.NextTick[slot];

            Assert.InRange(next.Raw, now.Raw + 1, now.Raw + 16);
        }

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// A kind that gains a Bin gets it on the Buildings that already stand, rather than only on the
    /// next one raised.
    /// </summary>
    /// <remarks>
    /// <b>The refit is <see cref="World.CreateBuilding"/>'s fit-out called again</b>, which is what
    /// keeps a Building raised before a reload the same Building as one raised after it. A refit that
    /// had drifted from the construction would produce two legal shapes for one kind and nothing that
    /// could tell them apart.
    /// </remarks>
    [Fact]
    public void A_kind_that_gains_a_bin_gets_it_on_buildings_that_already_stand()
    {
        (World world, _) = City(Load(SundriesOnly));

        Assert.Equal(Core.Tables.Rows.NoSlot, world.FindBin(First, new ResourceId(2)));

        RulesetDegradation cost = world.Adopt(Load(Both), new Ticks(64), Key);

        Assert.Equal(0, cost.BinsDropped);
        Assert.Equal(0, cost.BuildingsDerelicted);
        Assert.NotEqual(Core.Tables.Rows.NoSlot, world.FindBin(First, new ResourceId(2)));
        Assert.Equal(4, world.Bins.Capacity[world.FindBin(First, new ResourceId(2))]);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// The invariant fires when the failure is written by hand, rather than only passing.
    /// </summary>
    /// <remarks>
    /// <b>A derelict Building with its Rules left armed is the one shape sub-task C is most likely to
    /// produce</b>, because nothing about a re-arm loop makes the exclusion obvious — and it is
    /// invisible from every row involved, since each of them is individually legal. So the check is
    /// made to fail on purpose, which is the standing rule for every diagnostic in this project.
    /// </remarks>
    [Fact]
    public void A_derelict_building_that_kept_its_rules_is_reported()
    {
        (World world, _) = City(Load(Both));

        Assert.False(world.BuildingRules.IsEmpty(First));

        // Dereliction without the pass that goes with it.
        world.Buildings.Kind[First] = 0;

        Violation violation = Assert.Throws<InvariantViolationException>(
            () => world.Invariants.RunEndOfRun(world)).Violation;

        Assert.Equal(Invariant.DerelictBuildingRunsNoRules, violation.Invariant);
    }

    /// <summary>The city keeps running afterwards, which is the claim none of the row assertions makes.</summary>
    [Fact]
    public void The_city_runs_on_after_a_structural_reload()
    {
        Ruleset opening = Load(Both);

        (World world, Simulation simulation) = City(
            opening, RulesetCatalogue.Of([HashA, HashB], [opening, Load(SundriesOnly)]));

        for (int i = 0; i < 256; i++)
        {
            simulation.Step(new TickInput(default, HashB));
        }

        Assert.Equal(1, simulation.Reloads);
        Assert.Equal(3, simulation.LastReload.BinsDropped);
        Assert.Equal(3, simulation.LastReload.RuleInstancesRearmed);
        Assert.Equal(0, simulation.LastReload.BuildingsDerelicted);

        world.Invariants.RunEndOfRun(world);
    }
}
