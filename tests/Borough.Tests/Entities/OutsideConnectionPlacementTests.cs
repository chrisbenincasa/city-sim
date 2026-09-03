using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 11 task 1: a gate stands on an edge, and the guard that says so.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>adr/0088</c>'s *"a position constrained to an edge"*, which is the whole of the siting
/// rule.</b> The edge selects a market — <c>CONTEXT.md</c> → Hinterland is per edge — and *where
/// along that edge* selects nothing economic at all, so there is deliberately no test here about
/// which Segment a gate sits on.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts that the city grew.</b> The door has no caller until milestone 16
/// (<c>adr/0128</c>), and at task 1 it has no caller at all — what ships is a kind, a constraint and
/// a ceiling. Tests that a Household arrived belong to task 4 and later.
/// </para>
/// </remarks>
public sealed class OutsideConnectionPlacementTests
{
    /// <summary>Kind 1 is an ordinary dwelling; kind 2 is a gate.</summary>
    /// <remarks>
    /// ⚠ <b>The <c>[placement]</c> table is here because the gate kind is</b>, not because anything
    /// in this class places a Building through it. A Ruleset with a door into the Unplaced Pool and
    /// no way out of it is refused at load (<c>plans/0035</c> <b>F28</b>): the Pool would have an
    /// inflow and no sink, which <c>adr/0006</c> forbids. Every fixture in the corpus that declares
    /// <c>arrivals_per_day</c> now carries a sink, and that is the point of the refusal rather than
    /// a tax it levies.
    /// </remarks>
    private const string TwoKinds = """
        [[resource]]
        name = "flour"
        family = "good"

        [[building]]
        name = "dwelling"
        houses = true
        premises = true
        [[building]]
        name = "port"
        arrivals_per_day = 40

        [placement]
        interval = 32
        revisit_ticks = 1024
        candidates = 3
        gives_up_after_days = 120
        """;

    private const byte Dwelling = 1;
    private const byte Port = 2;

    private static World Built()
    {
        RulesetLoadResult result = RulesetLoader.Parse(TwoKinds, "test.toml");

        Assert.True(result.Ok, result.Describe());

        return new World(1_000, result.Ruleset!);
    }

    private static Handle<Lot> LotAt(World world, int east, int north) =>
        world.Lots.Create(new Tiles(east), new Tiles(north), zone: 1);

    /// <summary>The kind query is the whole test for whether something is a gate.</summary>
    [Fact]
    public void A_kind_is_a_gate_exactly_when_it_declares_a_throughput()
    {
        World world = Built();

        Assert.False(world.IsOutsideConnection(Dwelling));
        Assert.True(world.IsOutsideConnection(Port));
    }

    /// <summary>
    /// A kind the Ruleset does not declare is not a gate, which is what dereliction reads as.
    /// </summary>
    /// <remarks>
    /// <b><c>adr/0057</c>: dereliction is <c>Kind == 0</c></b>, a Building the Ruleset in force cannot
    /// describe. A derelict Building must not start reading as a door into the city because its kind
    /// left the Ruleset — which is the failure <see cref="World.TryDeclaredOccupancy"/> exists to
    /// prevent for occupancy, arriving here for the same reason.
    /// </remarks>
    [Fact]
    public void An_undeclared_kind_is_not_a_gate()
    {
        World world = Built();

        Assert.False(world.IsOutsideConnection(0));
        Assert.False(world.IsOutsideConnection(9));
        Assert.False(world.TryArrivalsPerDay(9, out int arrivals));
        Assert.Equal(0, arrivals);
    }

    /// <summary>The declared ceiling survives to the site that will meter against it at task 4.</summary>
    [Fact]
    public void A_gates_ceiling_is_readable_from_its_kind()
    {
        World world = Built();

        Assert.True(world.TryArrivalsPerDay(Port, out int arrivals));
        Assert.Equal(40, arrivals);

        Assert.False(world.TryArrivalsPerDay(Dwelling, out int none));
        Assert.Equal(0, none);
    }

    /// <summary>A gate on an edge goes up, on all four of them.</summary>
    [Theory]
    [InlineData(0, 4096)]
    [InlineData(CellGrid.WorldTiles, 4096)]
    [InlineData(4096, 0)]
    [InlineData(4096, CellGrid.WorldTiles)]
    public void A_gate_stands_on_any_of_the_four_edges(int east, int north)
    {
        World world = Built();

        Handle<Building> gate = world.CreateBuilding(
            LotAt(world, east, north), Port, default, WorldKey.FromSeed(1));

        Assert.NotEqual(MapEdge.None, world.EdgeOf(world.Lots.Rows.Resolve(world.Buildings.Lot[
            world.Buildings.Rows.Resolve(gate)])));
    }

    /// <summary>
    /// A gate in the middle of the city is caught at the write site.
    /// </summary>
    /// <remarks>
    /// <b>The violation this diagnostic exists for</b>, written and watched to fire, per
    /// <c>CLAUDE.md</c>'s rule that no diagnostic ships without one.
    /// </remarks>
    [Fact]
    public void A_gate_off_the_edge_is_caught_at_the_write_site()
    {
        World world = Built();
        Handle<Lot> inland = LotAt(world, 4096, 4096);

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.CreateBuilding(inland, Port, default, WorldKey.FromSeed(1)));

        Assert.Equal(Invariant.OutsideConnectionStandsOnOneEdge, failure.Violation.Invariant);
    }

    /// <summary>
    /// A gate on a corner is caught too, and it is a different failure wearing the same invariant.
    /// </summary>
    /// <remarks>
    /// <b>Under <c>adr/0088</c> the edge selects a market</b>, so a corner sits in two Hinterlands
    /// with nothing in the world to say which one its emigrants came from. That is refused rather
    /// than resolved: picking one would invent the rule, and the milestone that would own the rule is
    /// this one. ***A tie the design has no answer to is not broken at a write site.***
    /// </remarks>
    [Fact]
    public void A_gate_on_a_corner_is_caught_because_it_names_two_markets()
    {
        World world = Built();
        Handle<Lot> corner = LotAt(world, 0, 0);

        InvariantViolationException failure = Assert.Throws<InvariantViolationException>(
            () => world.CreateBuilding(corner, Port, default, WorldKey.FromSeed(1)));

        Assert.Equal(Invariant.OutsideConnectionStandsOnOneEdge, failure.Violation.Invariant);
    }

    /// <summary>
    /// A reload that turns a standing inland Building into a gate is caught at end of run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The failure the write-site guard cannot see, because nothing is written.</b> Under
    /// <c>adr/0015</c> a Ruleset is hot-reloadable and <c>arrivals_per_day</c> is what makes a kind a
    /// gate, so adding the key to a kind whose Buildings already stand converts every one of them
    /// **without calling <see cref="World.CreateBuilding"/> once**. ***A guard at the write site
    /// checks the kind a Building was born with, and a hot-reloadable kind is not a property a
    /// Building was born with.***
    /// </para>
    /// <para>
    /// <b>It reports rather than repairing</b>, unlike <c>adr/0068</c>'s lowered occupancy, which
    /// evicts: an Occupant can be moved and a Building cannot. This is design-time state in
    /// <c>adr/0057</c>'s sense — only a Ruleset edit under a running city reaches it.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_reload_that_makes_a_standing_inland_building_a_gate_is_caught()
    {
        World world = Built();

        world.CreateBuilding(LotAt(world, 4096, 4096), Dwelling, default, WorldKey.FromSeed(1));

        // The same Ruleset with the dwelling kind turned into a gate. Nothing is placed after this.
        RulesetLoadResult reloaded = RulesetLoader.Parse(
            TwoKinds.Replace(
                "name = \"dwelling\"\nhouses = true\npremises = true",
                "name = \"dwelling\"\nhouses = true\npremises = true\narrivals_per_day = 40",
                StringComparison.Ordinal),
            "reload.toml");

        Assert.True(reloaded.Ok, reloaded.Describe());

        world.Adopt(reloaded.Ruleset!, contentHash: 2, new Ticks(64), WorldKey.FromSeed(1));

        Assert.Equal(
            Invariant.OutsideConnectionStandsOnOneEdge,
            Assert.Throws<InvariantViolationException>(
                () => world.Invariants.RunEndOfRun(world)).Violation.Invariant);
    }

    /// <summary>A gate that is genuinely on an edge survives the same sweep.</summary>
    /// <remarks>
    /// <b>Without this the check above passes for the wrong reason</b> — a sweep that reported every
    /// gate would be indistinguishable from one that reported the inland ones.
    /// </remarks>
    [Fact]
    public void A_gate_on_an_edge_survives_the_end_of_run_sweep()
    {
        World world = Built();

        world.CreateBuilding(LotAt(world, 0, 4096), Port, default, WorldKey.FromSeed(1));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>
    /// An ordinary Building is unaffected wherever it stands, including on an edge.
    /// </summary>
    /// <remarks>
    /// <b>The constraint is on gates and not on the edge.</b> The boundary lattice is ordinary
    /// buildable land — under <c>adr/0090</c> the map is open, with *"no unlock, no serviceability
    /// gate, no boundary"* — so a dwelling on the first lattice line must go up exactly as one
    /// inland does. Without this, the guard would read as a rule about where the city may build.
    /// </remarks>
    [Theory]
    [InlineData(4096, 4096)]
    [InlineData(0, 4096)]
    [InlineData(0, 0)]
    public void An_ordinary_building_is_unconstrained(int east, int north)
    {
        World world = Built();

        world.CreateBuilding(LotAt(world, east, north), Dwelling, default, WorldKey.FromSeed(1));

        Assert.Equal(1, world.Buildings.Rows.LiveCount);
    }
}
