using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 8 task 7: the provenance trail, and <c>adr/0006</c>'s sink built with the collection.
/// </summary>
/// <remarks>
/// <para>
/// <b>The claim being tested is <c>05 §7</c>'s, and it is about a bug no other test in this repository
/// can reach.</b> A defect <em>caused</em> by a degradation three patches ago and surfacing now is
/// upstream of every snapshot anybody holds, so no replay starts early enough to reproduce it. The
/// trail is what turns <em>"why does this city have three derelict Buildings nobody built"</em> into a
/// line item, and the tests below are therefore about what a diagnosis three patches later can still
/// read — not about what a warning said at the time, which is
/// <see cref="Simulation.LastReload"/>'s job.
/// </para>
/// <para>
/// <b>The cap is tested by overflowing it rather than by reading the constant back.</b> The acceptance
/// clause is <em>a long-run test with more transitions than the cap, and it does not trend</em>, and
/// the only version of that which could fail is one that actually drives the collection past its
/// bound and watches the row count.
/// </para>
/// </remarks>
public sealed class RulesetTrailTests
{
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;

    private const byte Dwelling = 1;

    /// <summary>Two Goods, one kind holding a Bin of each, and a Rule that draws on the second.</summary>
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

    /// <summary>
    /// <c>repairs</c> becomes Money, which is the one reload the world refuses outright.
    /// </summary>
    /// <remarks>
    /// Its Bin goes with it, because a money Bin declares no capacity — so this file is
    /// <see cref="SundriesOnly"/> plus a re-familied declaration, and the refusal it trips is
    /// <see cref="RulesetMigration.FamilyChanged"/> rather than anything about the Bin.
    /// </remarks>
    private const string RepairsIsMoney = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[resource]]
        name = "repairs"
        family = "money"

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

    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0001UL);

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>Three dwellings, built and left alone. Nothing here needs the Tick to have run.</summary>
    private static World City(Ruleset opening)
    {
        var world = new World(1_000, opening);

        for (int i = 0; i < 3; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i), new Tiles(0), zone: 1);
            world.CreateBuilding(lot, Dwelling, Ticks.Zero, Key);
        }

        return world;
    }

    // ---- what is recorded, and what is not -----------------------------------------------------

    /// <summary>A new world has an aggregate row and no history.</summary>
    /// <remarks>
    /// <b>The aggregate is allocated at world creation rather than on the first overflow</b>, so slot 0
    /// means the same thing in every world there has ever been and nothing reading the trail needs a
    /// liveness branch. This is the test that would fail if it were made lazy.
    /// </remarks>
    [Fact]
    public void A_world_that_never_reloaded_has_an_empty_trail()
    {
        World world = City(Load(Both));

        Assert.Equal(0, world.RulesetTrail.Count);
        Assert.Equal(1, world.RulesetTrail.Rows.LiveCount);
        Assert.Equal(0, world.RulesetTrail.TransitionsRecorded());
        Assert.True(world.RulesetTrail.Total().IsNothing);
    }

    /// <summary>
    /// A tuning reload leaves no trace, and that is the collection's shape rather than a saving.
    /// </summary>
    /// <remarks>
    /// The trail answers <em>what did a reload destroy</em>. A reload that moved only numbers destroyed
    /// nothing, and an all-zero entry would push a real one out of a window sized for diagnosis.
    /// <em>How many Rulesets this session was played against</em> is a different question with its own
    /// answer already built — <see cref="Simulation.Reloads"/>.
    /// </remarks>
    [Fact]
    public void A_tuning_reload_is_not_in_the_trail()
    {
        World world = City(Load(Both));

        RulesetDegradation cost = world.Adopt(
            Load(Both.Replace("rate    = 8", "rate    = 12", StringComparison.Ordinal)),
            HashB,
            new Ticks(64),
            Key);

        Assert.True(cost.IsNothing);
        Assert.Equal(0, world.RulesetTrail.Count);
    }

    /// <summary>A degrading reload is recorded, naming its Ruleset, its Tick and its casualties.</summary>
    [Fact]
    public void A_degrading_reload_names_its_ruleset_and_its_casualties()
    {
        World world = City(Load(Both));

        RulesetDegradation cost = world.Adopt(Load(SundriesOnly), HashB, new Ticks(64), Key);

        RulesetTrailTable trail = world.RulesetTrail;
        int slot = trail.EntrySlot(0);

        Assert.Equal(1, trail.Count);
        Assert.Equal(HashB, trail.Ruleset[slot]);
        Assert.Equal(new Ticks(64), trail.Tick[slot]);
        Assert.Equal(1, trail.Transitions[slot]);
        Assert.Equal(cost.BinsDropped, trail.BinsDropped[slot]);
        Assert.Equal(cost.BuildingsDerelicted, trail.BuildingsDerelicted[slot]);
        Assert.Equal(cost.RuleInstancesRearmed, trail.RuleInstancesRearmed[slot]);

        // The three dwellings each had a repairs Bin, and repairs is gone.
        Assert.Equal(3, cost.BinsDropped);
    }

    /// <summary>Entries are in chronological order, oldest first, and the newest is last.</summary>
    /// <remarks>
    /// <b>Order is not decoration here.</b> Index order is hash composition order (<c>02 §8</c>), so a
    /// trail that recorded the same transitions in a different arrangement would hash differently —
    /// which is why the window slides rather than rotating around a cursor.
    /// </remarks>
    [Fact]
    public void The_trail_reads_oldest_first()
    {
        World world = City(Load(Both));

        world.Adopt(Load(SundriesOnly), HashB, new Ticks(64), Key);
        world.Adopt(Load(Both), HashA, new Ticks(128), Key);

        RulesetTrailTable trail = world.RulesetTrail;

        Assert.Equal(2, trail.Count);
        Assert.Equal(new Ticks(64), trail.Tick[trail.EntrySlot(0)]);
        Assert.Equal(new Ticks(128), trail.Tick[trail.EntrySlot(1)]);
        Assert.Equal(HashB, trail.Ruleset[trail.EntrySlot(0)]);
        Assert.Equal(HashA, trail.Ruleset[trail.EntrySlot(1)]);
    }

    // ---- the cap, which is adr/0006's clause ---------------------------------------------------

    /// <summary>
    /// More transitions than the cap: the row count stops climbing, and nothing is lost that a count
    /// can carry.
    /// </summary>
    /// <remarks>
    /// <b>This is the long-run clause of the slice's acceptance, and it is stated over the collection
    /// rather than over the counts.</b> The aggregate's totals climb — that is what <c>05 §7</c> asked
    /// for, and a counter is not a collection — while the row count is flat at
    /// <see cref="RulesetTrailTable.Retained"/> + 1 from the moment the window fills. A trail that grew
    /// would be <c>adr/0006</c>'s defect arriving through the mechanism written to diagnose it.
    /// </remarks>
    [Fact]
    public void The_trail_caps_and_the_row_count_stops_climbing()
    {
        const int Transitions = 200;

        World world = City(Load(Both));
        RulesetTrailTable trail = world.RulesetTrail;

        int dropped = 0;
        int rearmed = 0;
        int derelicted = 0;

        for (int i = 0; i < Transitions; i++)
        {
            bool wide = i % 2 == 1;

            RulesetDegradation cost = world.Adopt(
                Load(wide ? Both : SundriesOnly),
                wide ? HashA : HashB,
                new Ticks((ulong)(64 + i)),
                Key);

            Assert.False(cost.IsNothing, $"transition {i} recorded nothing, so it tests no cap");

            dropped += cost.BinsDropped;
            rearmed += cost.RuleInstancesRearmed;
            derelicted += cost.BuildingsDerelicted;

            Assert.Equal(Math.Min(i + 1, RulesetTrailTable.Retained), trail.Count);
            Assert.Equal(Math.Min(i + 2, RulesetTrailTable.Retained + 1), trail.Rows.LiveCount);
        }

        Assert.Equal(RulesetTrailTable.Retained, trail.Count);
        Assert.Equal(Transitions, trail.TransitionsRecorded());
        Assert.Equal(
            Transitions - RulesetTrailTable.Retained,
            trail.Transitions[RulesetTrailTable.AggregateSlot]);

        // Nothing a count can carry is lost when an entry ages out of the window.
        Assert.Equal(new RulesetDegradation(derelicted, dropped, rearmed), trail.Total());

        // The retained window is the newest N, and the aggregate ends where the window begins.
        Assert.Equal(new Ticks(64 + Transitions - 1), trail.Tick[trail.EntrySlot(RulesetTrailTable.Retained - 1)]);
        Assert.Equal(
            new Ticks(64 + Transitions - RulesetTrailTable.Retained - 1),
            trail.Tick[RulesetTrailTable.AggregateSlot]);
    }

    /// <summary>The aggregate names no Ruleset, because it is more than one.</summary>
    /// <remarks>
    /// <b>0 is this project's spelling for <em>no single Ruleset</em></b> — the same sentinel
    /// <see cref="Simulation.RulesetInForce"/> uses for <em>before the first Tick</em> — and it is what
    /// stops a reader attributing a swallowed transition's casualties to whichever Ruleset happened to
    /// be last through the door.
    /// </remarks>
    [Fact]
    public void The_aggregate_names_no_ruleset()
    {
        World world = City(Load(Both));

        for (int i = 0; i <= RulesetTrailTable.Retained; i++)
        {
            bool wide = i % 2 == 1;
            world.Adopt(Load(wide ? Both : SundriesOnly), wide ? HashA : HashB, new Ticks((ulong)(64 + i)), Key);
        }

        Assert.Equal(0UL, world.RulesetTrail.Ruleset[RulesetTrailTable.AggregateSlot]);
        Assert.Equal(1, world.RulesetTrail.Transitions[RulesetTrailTable.AggregateSlot]);
    }

    // ---- it is state, which is the whole point --------------------------------------------------

    /// <summary>
    /// The trail is saved state, so it reaches the State Hash and therefore the save.
    /// </summary>
    /// <remarks>
    /// <b>This is <c>05 §7</c>'s actual requirement</b> — <em>a save carries a provenance trail</em> —
    /// and the field declaration is what makes it true: declaring a column
    /// <see cref="Rows.Saved{TField}"/> is what puts it in the hash and in the save at once, so there
    /// is no way to have one without the other.
    /// </remarks>
    [Fact]
    public void The_trail_folds_into_the_state_hash()
    {
        World world = City(Load(Both));

        ulong before = 0;
        world.RulesetTrail.Rows.Fold(ref before);

        world.Adopt(Load(SundriesOnly), HashB, new Ticks(64), Key);

        ulong after = 0;
        world.RulesetTrail.Rows.Fold(ref after);

        Assert.NotEqual(before, after);
    }

    /// <summary>
    /// A refused reload records nothing, because it did not happen.
    /// </summary>
    /// <remarks>
    /// <b>The trail is written after the migration pass rather than before it</b>, and this is the case
    /// that distinguishes the two orderings. A trail entry for a transition the world refused would be
    /// a record of a Tick nobody can replay to, and the whole value of the collection is being believed
    /// about exactly that kind of Tick.
    /// </remarks>
    [Fact]
    public void A_refused_reload_leaves_no_entry()
    {
        World world = City(Load(Both));

        Assert.Throws<NotSupportedException>(
            () => world.Adopt(Load(RepairsIsMoney), HashB, new Ticks(64), Key));

        Assert.Equal(0, world.RulesetTrail.Count);
    }
}
