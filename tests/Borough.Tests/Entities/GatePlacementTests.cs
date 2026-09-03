using Borough.Core.Determinism;
using Borough.Core.Movement;
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
/// <b>All four edges get one, and <see cref="A_far_gate_is_routable_and_still_beyond_the_budget"/> is
/// what keeps the second half of that from being forgotten.</b> A gate <em>standing</em> on the north
/// edge and a gate a Trip can <em>complete to</em> are different claims, and only the first is the
/// generator's. ***An edge a generator cannot reach is a market nothing can arrive from***, and an
/// edge it reaches but no Trip can cross is the same market one measurement later.
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
    /// <b>Every map edge gets a gate</b>, which takes paving to the boundary and carving a block there.
    /// </summary>
    /// <remarks>
    /// <b>It asserts the <em>set</em> rather than a count</b>, so a change in either direction says
    /// which edge moved. Both halves of the generator are load-bearing: without
    /// <c>SyntheticCity.ReachesTheBoundary</c> the lattice stops 160 Tiles from the origin, and
    /// without <c>CarveEdgeBlock</c> it reaches the boundary with no Lot beside the Street.
    /// </remarks>
    [Fact]
    public void All_four_edges_get_a_gate()
    {
        Assert.Equal(
            [MapEdge.West, MapEdge.East, MapEdge.South, MapEdge.North],
            [.. Gates(Populated("bordered.toml")).Order()]);
    }

    /// <summary>
    /// 🔴 <b>Every gate is routable by car, and the Commute Budget admits some of them and refuses
    /// at least one.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two claims, because separating them is the whole point.</b> Every gate has a finite car
    /// route to the city — that is the paving and the carved block having genuinely joined the
    /// lattice, and it is what would have been <see cref="TravelTime.Impassable"/> before. And the
    /// Budget is a real sieve over that set rather than a formality: something is admitted and
    /// something is refused.
    /// </para>
    /// <para>
    /// 🔴 <b>THE SECOND CLAIM NAMED COMPASS POINTS UNTIL <c>plans/0055</c>, AND THE COMPASS WAS THE
    /// CITY'S ADDRESS RATHER THAN THE MECHANISM.</b> It read <i>the far two are further from that
    /// city than any Trip may travel — east 62 minutes and north 73 against a ceiling of 49, where
    /// west and south are 0</i>. Every one of those numbers was a property of a city sitting in the
    /// <b>origin corner</b> of a 65.5 km map, which is where the generator's reading-order walk left
    /// it. It builds outward from the middle now, so the same world measures <b>west 39.7, south
    /// 39.7, north 48.9 and east 52.9 minutes</b> — ***a spread straddling the ceiling instead of two
    /// zeroes and two impossibilities***, and <c>bordered.toml</c>'s four Hinterlands are four live
    /// markets where two of them used to be unreachable by construction.
    /// </para>
    /// <para>
    /// ⚠ <b>So the assertion is written against the SIEVE and no longer against the compass.</b> A
    /// test that names the edge it expects to fail is a test that has to be rewritten every time the
    /// city moves, and it would go green on a world where the ceiling refuses nothing — which is
    /// <c>plans/0049</c> <b>F26</b>'s failure exactly. What must hold is that the Budget divides the
    /// gates, and which edge falls on which side is the geometry's business.
    /// </para>
    /// <para>
    /// ⚠ <b><c>TripEngine</c> judges the Commute Budget on every Trip and not only on a commute</b>,
    /// so a move-in from a far gate to a corner dwelling fails with
    /// <c>TripFate.ExceededCommuteBudget</c>. <b>That is <c>adr/0089</c> rather than a defect</b> —
    /// the map is sized by how many Commute Budgets fit across it, so a map several budgets wide puts
    /// its far edge outside one by construction. ***A far gate is made usable by a dwelling beside it,
    /// not by a faster road***: sixteen Arterials buy 16 minutes on one edge and 7 on the other, and a
    /// pure-Arterial run of that distance is 43 minutes with no route pure. The carved block leaves
    /// vacant Lots beside every gate, and placing an arrival in reach of the gate it came through is
    /// <b>task 6</b>'s.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_far_gate_is_routable_and_still_beyond_the_budget()
    {
        World world = Populated("bordered.toml");
        TripRuleset trips = world.Rules.Trips;
        var scratch = new WalkScratch();

        var homes = new List<int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                homes.Add(slot);
            }
        }

        Assert.NotEmpty(homes);

        var withinBudget = new Dictionary<MapEdge, int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot)
                || !world.IsOutsideConnection(world.Buildings.Kind[slot]))
            {
                continue;
            }

            MapEdge edge = world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[slot]));
            int reached = 0;
            bool routable = false;

            foreach (int home in homes)
            {
                TravelTime cost = WalkRouting.Cost(
                    world.Roads,
                    TravelMode.Car,
                    world.AccessPoint(slot, TravelMode.Car),
                    world.AccessPoint(home, TravelMode.Car),
                    trips.CrossingCost,
                    scratch);

                if (cost.IsImpassable)
                {
                    continue;
                }

                routable = true;

                if (trips.WithinBudget(cost))
                {
                    reached++;
                }
            }

            Assert.True(routable, $"the {edge} gate has no car route to any dwelling at all.");
            withinBudget[edge] = reached;
        }

        Assert.Equal(4, withinBudget.Count);

        int admitted = 0;
        int refused = 0;

        foreach ((MapEdge edge, int reached) in withinBudget)
        {
            _ = edge;

            if (reached > 0)
            {
                admitted++;
            }
            else
            {
                refused++;
            }
        }

        Assert.True(
            admitted > 0,
            "no gate reaches a single dwelling inside the Commute Budget, so every Hinterland is "
                + "unreachable and the four markets are decoration.");

        Assert.True(
            refused > 0,
            "every gate is inside the Commute Budget, so the ceiling refuses nothing in this world "
                + "and this fact is green for the wrong reason. A world whose far edge is inside one "
                + "budget is a world adr/0089's map sizing does not describe -- check the paved "
                + "extent before lowering this.");
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
