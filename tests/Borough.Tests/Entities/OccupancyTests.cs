using Borough.Core.Determinism;
using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// <c>adr/0068</c>: a Building's occupancy is declared by its kind, and an over-capacity Building
/// evicts.
/// </summary>
/// <remarks>
/// <para>
/// <b>The pair of properties under test is the one the decision turns on.</b> Occupancy is a
/// property of the <em>Ruleset in force</em>, so lowering a ceiling reaches every Building already
/// standing — that half is <c>adr/0064</c> applied a second time. What does <em>not</em> transplant
/// is the over-capacity answer: an over-full Bin is left to drain because something consumes it, and
/// occupancy has no consumer at all, so a Building left over its ceiling would sit there for the life
/// of the city consuming at a number its kind denies.
/// </para>
/// <para>
/// <b>The fixture keeps a Rule banded on <c>occupancy</c></b>, because that is not decoration: it is
/// the one declared Readout and the reason an over-capacity Building is a behavioural problem rather
/// than a cosmetic one.
/// </para>
/// </remarks>
public sealed class OccupancyTests
{
    private const ulong HashA = 0x1111_1111_1111_1111UL;
    private const ulong HashB = 0x2222_2222_2222_2222UL;

    private const byte Dwelling = 1;

    private static readonly WorldKey Key = WorldKey.FromSeed(0x8000_0001UL);

    /// <summary>A dwelling, with the ceiling left as a token for <see cref="Housing"/> to fill.</summary>
    private const string Template = """
        [[resource]]
        name = "sundries"
        family = "good"

        [[building]]
        name = "dwelling"
        occupants = HOLDS
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

        [[rule]]
        name    = "consume"
        kind    = "dwelling"
        rate    = 32
        apply   = { derived = "occupancy" }
        inputs  = [ { scope = "local", resource = "sundries", amount = 1 } ]
        outputs = []
        """;

    /// <summary>A dwelling holding <paramref name="occupants"/> Households.</summary>
    private static string Housing(int occupants) => Template.Replace(
        "HOLDS",
        occupants.ToString(CultureInfo.InvariantCulture),
        StringComparison.Ordinal);

    /// <summary>The same file with no <c>[[building]]</c> at all: every Building is derelict.</summary>
    private const string NoKinds = """
        [[resource]]
        name = "sundries"
        family = "good"
        """;

    private static Ruleset Load(string toml)
    {
        RulesetLoadResult result = RulesetLoader.Parse(toml, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return result.Ruleset!;
    }

    /// <summary>
    /// One Building holding <paramref name="households"/> Households, under a Ruleset that allows
    /// them.
    /// </summary>
    private static World City(int households, int occupants, WorldKey? key = null)
    {
        var world = new World(1_000, Load(Housing(occupants)));

        Handle<Lot> lot = world.Lots.Create(new Tiles(0), new Tiles(0), zone: 1);
        Handle<Building> building = world.CreateBuilding(lot, Dwelling, Ticks.Zero, key ?? Key);

        for (int i = 0; i < households; i++)
        {
            world.CreateHousehold(building, lifeStage: 0);
        }

        return world;
    }

    /// <summary>Which Households are in the Pool, by monotonic id, in Pool order.</summary>
    private static ulong[] Pooled(World world) =>
        [.. Enumerable.Range(0, world.UnplacedPool.Count)
            .Select(world.UnplacedPool.At)
            .Select(h => world.Households.Rows.IdAt(world.Households.Rows.Resolve(h)))];

    // ---- the ceiling reaches Buildings already standing ------------------------------------------

    /// <summary>
    /// <b>Lowering a kind's occupancy evicts the overflow into the Unplaced Pool.</b>
    /// </summary>
    /// <remarks>
    /// The acceptance test for <c>adr/0068</c>, and note what it is <em>not</em>: no migration, no
    /// degradation, no structural change. Occupancy is a <c>RulesetChange.None</c> edit, exactly as a
    /// Bin ceiling is, so this runs on the path a designer uses a hundred times in a sitting.
    /// </remarks>
    [Fact]
    public void Lowering_the_ceiling_evicts_the_overflow_into_the_pool()
    {
        World world = City(households: 4, occupants: 4);

        Assert.Equal(0, world.UnplacedPool.Count);

        world.Adopt(Load(Housing(1)), HashB, new Ticks(64), Key);

        Assert.Equal(3, world.UnplacedPool.Count);
        Assert.Equal(1, world.Occupants.Length(0));
    }

    /// <summary>
    /// The evicted keep their Money and Savings, which is what makes eviction free
    /// (<c>adr/0054</c>).
    /// </summary>
    [Fact]
    public void The_evicted_keep_what_they_own()
    {
        World world = City(households: 2, occupants: 2);
        int slot = world.Households.Rows.Resolve(world.Households.Rows.At(0));

        world.Households.Money[slot] = new Money(1_234);
        world.Households.Savings[slot] = new Money(5_678);

        world.Adopt(Load(Housing(0)), HashB, new Ticks(64), Key);

        Assert.Equal(2, world.UnplacedPool.Count);
        Assert.Equal(new Money(1_234), world.Households.Money[slot]);
        Assert.Equal(new Money(5_678), world.Households.Savings[slot]);
    }

    /// <summary>Raising the ceiling evicts nobody, and a reload that changes nothing evicts nobody.</summary>
    [Fact]
    public void Raising_the_ceiling_evicts_nobody()
    {
        World world = City(households: 3, occupants: 3);

        world.Adopt(Load(Housing(9)), HashB, new Ticks(64), Key);
        world.Adopt(Load(Housing(9)), HashB, new Ticks(65), Key);

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(3, world.Occupants.Length(0));
    }

    /// <summary>
    /// <b>A Building whose kind the incoming Ruleset dropped keeps its Occupants.</b>
    /// </summary>
    /// <remarks>
    /// <b>The case that makes *declared zero* and *not declared* two different things.</b> Collapsing
    /// them would make a designer deleting a <c>[[building]]</c> paragraph evict a District — the
    /// loudest possible consequence for the quietest possible edit — and <c>CONTEXT</c> → Derelict
    /// Building says the opposite in its own words: it *still stands, still holds its Occupants and
    /// still occupies its Lot*.
    /// </remarks>
    [Fact]
    public void A_kind_the_ruleset_dropped_keeps_its_occupants()
    {
        World world = City(households: 3, occupants: 3);

        world.Adopt(Load(NoKinds), HashB, new Ticks(64), Key);

        Assert.Equal(0, world.UnplacedPool.Count);
        Assert.Equal(3, world.Occupants.Length(0));
    }

    // ---- admission ------------------------------------------------------------------------------

    /// <summary>A full Building has no room, and an emptied place opens one.</summary>
    [Fact]
    public void A_full_building_has_no_room_and_a_departure_opens_one()
    {
        World world = City(households: 2, occupants: 2);

        Assert.False(world.HasRoom(0));

        world.Unplace(world.Households.Rows.At(0));

        Assert.True(world.HasRoom(0));
    }

    /// <summary>A derelict Building admits nobody, even though it evicts nobody either.</summary>
    [Fact]
    public void A_derelict_building_admits_nobody()
    {
        World world = City(households: 1, occupants: 3);

        world.Unplace(world.Households.Rows.At(0));
        world.Adopt(Load(NoKinds), HashB, new Ticks(64), Key);

        Assert.False(world.HasRoom(0));
    }

    /// <summary>
    /// Placing into a full Building is refused at the write site rather than silently overfilling.
    /// </summary>
    /// <remarks>
    /// <b><see cref="World.HasRoom"/> is the predicate and this is the guard</b>, which is
    /// <c>adr/0064</c>'s id-14 finding applied to the second capacity a Building has. A caller that
    /// asks first never sees this; one that does not is a bug, and the Household stays where it was
    /// rather than being housed nowhere.
    /// </remarks>
    [Fact]
    public void Placing_into_a_full_building_is_refused()
    {
        World world = City(households: 2, occupants: 2);
        Handle<Household> evicted = world.Households.Rows.At(0);

        world.Unplace(evicted);
        world.Adopt(Load(Housing(1)), HashB, new Ticks(64), Key);

        // One place, one occupant, and one Household in the Pool wanting it.
        Assert.Equal(1, world.Occupants.Length(0));
        Assert.False(world.HasRoom(0));

        Assert.Throws<InvariantViolationException>(
            () => world.Place(evicted, world.Buildings.Rows.At(0)));

        Assert.True(world.Households.IsUnplaced(world.Households.Rows.Resolve(evicted)));
    }

    // ---- who leaves -----------------------------------------------------------------------------

    /// <summary>
    /// <b>Which Occupants a lowered ceiling evicts is a draw, and the draw is keyed on the world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two assertions in one, because either alone is satisfiable by the wrong implementation. **Two
    /// worlds with one key evict the same families** — without that the eviction is not deterministic
    /// and no replay reproduces it. **Two worlds with different keys evict different families** —
    /// without that the "draw" is list order wearing a hash, which is what <c>02 §8</c> rule 5
    /// forbids: the same families would be turned out of every Building on every patch, for ever,
    /// with nothing in any readout to say why.
    /// </para>
    /// <para>
    /// The second half is a property of a specific pair of seeds rather than of all pairs, which is
    /// what makes it a test rather than a proof. It is worth having anyway: an implementation that
    /// ignored the key entirely would fail it every time.
    /// </para>
    /// </remarks>
    [Fact]
    public void Who_is_evicted_is_drawn_rather_than_taken_from_the_end_of_the_list()
    {
        var other = WorldKey.FromSeed(0x9000_0009UL);

        World first = City(households: 6, occupants: 6);
        World again = City(households: 6, occupants: 6);
        World elsewhere = City(households: 6, occupants: 6, key: other);

        first.Adopt(Load(Housing(2)), HashB, new Ticks(64), Key);
        again.Adopt(Load(Housing(2)), HashB, new Ticks(64), Key);
        elsewhere.Adopt(Load(Housing(2)), HashB, new Ticks(64), other);

        Assert.Equal(Pooled(first), Pooled(again));
        Assert.NotEqual(Pooled(first).Order(), Pooled(elsewhere).Order());
    }

    // ---- the populator ---------------------------------------------------------------------------

    /// <summary>
    /// <b>The populator takes its Households-per-Building from the Ruleset.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the disagreement <c>0002</c> §B recorded, closed at one of its two ends.</b>
    /// <c>SyntheticCity</c> held <c>HouseholdsPerBuilding = 3</c> as a <c>const</c> while a Zone Rule
    /// housed <c>1</c>, and nothing could reconcile them because the quantity was not expressible.
    /// Both now read the same declaration.
    /// </remarks>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void The_populator_houses_what_the_ruleset_declares(int occupants)
    {
        var world = new World(1_000, Load(Housing(occupants)));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int households = world.Households.Rows.LiveCount;
        int buildings = world.Buildings.Rows.LiveCount;

        Assert.Equal((households / occupants) + 1, buildings);

        for (int slot = 0; slot < buildings; slot++)
        {
            Assert.True(
                world.Occupants.Length(slot) <= occupants,
                $"Building {slot} holds {world.Occupants.Length(slot)} of {occupants}.");
        }

        Assert.Equal(0, world.UnplacedPool.Count);
    }
}
