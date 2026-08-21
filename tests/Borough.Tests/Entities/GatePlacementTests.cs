using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 11 task 3: <see cref="SyntheticCity"/> puts a door in the world.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a task rather than an assumption because milestone 9 shipped a producer nothing could
/// observe</b> — <c>plans/0034</c> <b>F17</b>: the land value field was correct and read zero in
/// every world that existed, for want of Ruleset <em>content</em> rather than for want of code. A
/// gate kind that no Building is ever raised of is that failure one milestone later, so what these
/// tests hold is that a gate <em>stands</em>.
/// </para>
/// <para>
/// 🔴 ⚠ <b>Two of the four edges are unreachable by construction, and
/// <see cref="Only_the_edges_the_lattice_reaches_get_a_gate"/> is what keeps that from being
/// rediscovered.</b> <c>SyntheticCity.PavedTiles</c> sizes the lattice to the Lots the world was
/// allocated for rather than to the map, so it runs from the origin corner and stops — touching
/// <see cref="MapEdge.West"/> and <see cref="MapEdge.South"/> and never the far two, which would take
/// on the order of 2.6 million Lots. ***An edge a generator cannot reach is a market nothing can
/// arrive from.***
/// </para>
/// </remarks>
public sealed class GatePlacementTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static World Populated(string file, int citizens = 1_000)
    {
        var world = new World(citizens, Shipped(file));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>Which edge each standing Outside Connection is on.</summary>
    private static List<MapEdge> Gates(World world)
    {
        var found = new List<MapEdge>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            found.Add(world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[slot])));
        }

        return found;
    }

    /// <summary><c>bordered.toml</c> declares a gate and four markets behind four edges.</summary>
    [Fact]
    public void The_shipped_ruleset_declares_a_gate_and_four_hinterlands()
    {
        Ruleset rules = Shipped("bordered.toml");

        Assert.Equal(4, rules.Hinterlands.Length);

        foreach (MapEdge edge in new[] { MapEdge.North, MapEdge.South, MapEdge.East, MapEdge.West })
        {
            Assert.True(rules.TryHinterland(edge, out _), $"no Hinterland behind {edge}.");
        }

        int gateKinds = 0;

        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            if (rules.Kind((byte)kind).ArrivalsPerDay > 0)
            {
                gateKinds++;
            }
        }

        Assert.Equal(1, gateKinds);
    }

    /// <summary>A gate stands in the generated world, which is the whole of the task.</summary>
    [Fact]
    public void A_generated_world_has_a_door_in_it()
    {
        Assert.NotEmpty(Gates(Populated("bordered.toml")));
    }

    /// <summary>
    /// 🔴 <b>Only the west and south edges get one, because they are the only ones the land reaches.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the test that would fail if the generator ever reached the far edges</b>, at which
    /// point <c>bordered.toml</c>'s north and east bands stop being unratifiable and
    /// <c>plans/0002</c> §D1's two rows can be closed. It asserts the <em>set</em> rather than a
    /// count, so a change in either direction says which edge moved.
    /// </remarks>
    [Fact]
    public void Only_the_edges_the_lattice_reaches_get_a_gate()
    {
        Assert.Equal([MapEdge.West, MapEdge.South], [.. Gates(Populated("bordered.toml")).Order()]);
    }

    /// <summary>No gate stands on a corner, which would name two markets and therefore neither.</summary>
    [Fact]
    public void No_gate_stands_on_a_corner()
    {
        Assert.DoesNotContain(MapEdge.None, Gates(Populated("bordered.toml")));
    }

    /// <summary>
    /// A Ruleset that declares no gate kind raises none, and that is what protects every baseline.
    /// </summary>
    /// <remarks>
    /// <b>The generator's new pass is inert on eight of the nine shipped Rulesets</b>, so no State
    /// Hash moved when it landed. That is the same argument milestone 11 task 1 made for
    /// <c>arrivals_per_day</c> and it has to be re-made here, because this one changes the
    /// <em>generator</em> rather than a column nothing reads.
    /// </remarks>
    [Fact]
    public void A_ruleset_with_no_gate_kind_raises_none()
    {
        Assert.Empty(Gates(Populated("minimal.toml")));
    }

    /// <summary>
    /// Nobody is housed in the gate, and the Building slot offset is what makes that true.
    /// </summary>
    /// <remarks>
    /// <b>Gates go up before the dwellings because an edge Lot is an early Lot</b> — so they take
    /// Building slots <c>0..gates-1</c>, and <c>SyntheticCity.Dwelling</c> has to skip past them. Had
    /// it not, the first Households would have been housed in a port. ***A Household housed IN the
    /// gate is not a Household that came THROUGH it.***
    /// </remarks>
    [Fact]
    public void Nobody_is_housed_in_the_gate()
    {
        World world = Populated("bordered.toml");

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            Assert.Equal(0, world.Occupants.Length(slot));
        }
    }

    /// <summary>The gate takes a Lot from the housing stock and the population still fits.</summary>
    /// <remarks>
    /// <b>The dwelling loop walks vacant Lots rather than the first <c>n</c> slots</b>, which is the
    /// one behavioural change the gate pass forces on a gateless world's code path. This holds that
    /// every Household still has somewhere to live.
    /// </remarks>
    [Fact]
    public void Every_household_is_still_housed()
    {
        World world = Populated("bordered.toml");

        Assert.True(world.Households.Rows.LiveCount > 0);
        Assert.Equal(0, world.UnplacedPool.Count);
    }
}
