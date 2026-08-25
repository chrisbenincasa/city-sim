using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// Milestone 12 task 1: <see cref="SyntheticCity"/> can lay a world with <b>two centres</b> in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A world and not a mechanism, and it is FIRST in the milestone rather than last.</b>
/// <c>adr/0134</c> makes a District <i>a centre and its basin</i> — a watershed over Building density,
/// clipped to a road component, seeded where a concentration's prominence clears a threshold — so
/// <b>the count follows centres</b>. Every world this build could generate before this task has
/// exactly one, which makes the derivation not merely undemonstrated but <em>untestable</em>:
/// a watershed over a field with one peak in it returns one basin however it is written.
/// </para>
/// <para>
/// 🔴 <b>The two lattices are JOINED, and that is the whole point of the corridor.</b> <c>adr/0134</c>
/// considered and rejected <i>splitting only where the road graph disconnects</i> — it is
/// <c>adr/0013</c>'s <i>pool everything, city-wide</i> reached through a derivation. A world in two
/// road components would let component labelling pass for a watershed, which is the rejected mechanism
/// wearing the chosen one's name. <b>Joined, the only thing that can find the boundary is the density
/// field.</b>
/// </para>
/// <para>
/// ⚠ <b>What these tests do NOT assert is that there are two Districts</b>, because there is no
/// <c>DistrictTable</c> yet — that is task 3, and the prominence threshold it must choose does not
/// exist. What they hold is that the <em>world</em> is one any sane threshold splits: two equal
/// concentrations, a gap between them an order of magnitude wider than either is across, and nothing
/// standing in the gap. ***A world that calibrated the threshold would be the wrong world***, which is
/// why the shares are equal and the gap is not marginal.
/// </para>
/// <para>
/// ⚠ <b><c>[[lattice]]</c> is not spelled <c>[[settlement]]</c> and the difference is not cosmetic.</b>
/// <c>CONTEXT.md</c> → Settlement is a <em>derived</em> commute shed: <i>"connectivity is transitive,
/// so a contiguously-developed lattice is one Settlement however large the graph"</i>. These two
/// lattices are joined by road well inside the Commute Budget, so they are <b>one</b> Settlement and
/// two centres. A key called <c>settlement</c> would have authored a contradiction.
/// </para>
/// </remarks>
public sealed class TwinLatticeTests
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

    /// <summary>Every standing Building's east Tile, which is the axis the two lattices differ on.</summary>
    private static List<int> BuildingEastings(World world)
    {
        var found = new List<int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            found.Add(world.Lots.East[world.Lots.Rows.Resolve(world.Buildings.Lot[slot])].Raw);
        }

        return found;
    }

    // ---- the file ------------------------------------------------------------------------------

    /// <summary><c>twinned.toml</c> authors two lattices and nothing else minimal.toml does not.</summary>
    [Fact]
    public void The_shipped_ruleset_authors_two_lattices()
    {
        Ruleset rules = Shipped("twinned.toml");

        Assert.Equal(2, rules.Lattices.Length);
        Assert.Equal(new LatticeDefinition(0, 0), rules.Lattices[0]);
        Assert.Equal(0, rules.Lattices[1].OriginNorthTiles);

        Assert.True(
            rules.Lattices[1].OriginEastTiles > 0,
            "the second lattice stands on the first one's origin, so this file authors one centre.");
    }

    /// <summary>
    /// ⚠ <b><c>twinned.toml</c> is the only shipped Ruleset with MORE THAN ONE lattice, and
    /// <c>coastal.toml</c> is the only other one that authors any.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The check that makes <c>[[lattice]]</c>'s absence a behaviour rather than a claim.</b> An
    /// empty array is one lattice at the origin corner — <c>SyntheticCity.Lattices</c> — which is what
    /// the generator did unconditionally before this key existed.
    /// </para>
    /// <para>
    /// 🔴 <b>NARROWED 2026-08-24 by milestone 24 task 7, and it was this test that noticed.</b> It
    /// asserted every file but <c>twinned.toml</c> authors <em>none</em>, and its own remark said
    /// <em>"if a second file ever authors one, its golden baseline moves and this test is where that
    /// is noticed."</em> One did. <c>coastal.toml</c> now states an origin at the map's middle, because
    /// the origin corner is on the map's <b>edge</b> and <b>a map edge is where water leaves the
    /// world</b> — so a corner city's runoff drains off the map and milestone 24's whole water
    /// mechanism reads zero on it (<c>plans/0042</c> <b>F17</b>).
    /// </para>
    /// <para>
    /// ⚠ <b>The claim is narrowed to what is still load-bearing, not deleted.</b> What
    /// <c>twinned.toml</c> is for is being the only world with more than one <em>centre</em>, and that
    /// is what this now asserts. The exemption is by name and carries its reason, so a third file
    /// growing a lattice by accident still goes red here.
    /// </para>
    /// </remarks>
    [Fact]
    public void Only_twinned_authors_two_lattices_and_only_coastal_authors_another()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Rulesets");

        foreach (string path in Directory.GetFiles(directory, "*.toml").Order(StringComparer.Ordinal))
        {
            string file = Path.GetFileName(path);

            if (file == "twinned.toml")
            {
                continue;
            }

            int authored = Shipped(file).Lattices.Length;

            if (file == "coastal.toml")
            {
                Assert.True(
                    authored == 1,
                    $"coastal.toml authors {authored} lattices and the exemption is for exactly one. "
                    + "It states an origin at all only because the default (0, 0) puts the city on "
                    + "the map's edge, where water leaves the world and runoff reads zero — see its "
                    + "own [[lattice]] header and plans/0042 F17.");

                continue;
            }

            Assert.True(
                authored == 0,
                $"{file} authors a [[lattice]]. Every world but twinned.toml and coastal.toml is one "
                + "lattice at the origin corner, and authoring one moves that file's State Hash. If "
                + "this is deliberate, say why in the file's header and add it to the exemptions "
                + "above rather than widening the test.");
        }
    }

    // ---- the world -----------------------------------------------------------------------------

    /// <summary>
    /// 🔴 <b>The city is ONE road component in both modes</b>, so only the density field can split it.
    /// </summary>
    [Fact]
    public void The_two_lattices_are_one_road_component_in_both_modes()
    {
        World world = Populated("twinned.toml");
        RoadConnectivity connectivity = world.Roads.Connectivity;

        Assert.Equal(1, connectivity.CarComponents);
        Assert.Equal(1, connectivity.FootComponents);

        Assert.Equal(world.Roads.Nodes.Rows.LiveCount, connectivity.LargestCar);
        Assert.Equal(world.Roads.Nodes.Rows.LiveCount, connectivity.LargestFoot);
    }

    /// <summary>
    /// <b>Both lattices are built on, in equal numbers</b> — which is what makes each one a centre.
    /// </summary>
    /// <remarks>
    /// <b>Equal because the shares are derived and not authored</b>
    /// (<c>SyntheticCity.Share</c>). Two concentrations of the same height both clear any prominence
    /// threshold a sane person would pick, so this world demonstrates the derivation rather than
    /// calibrating it — and the threshold is task 3's, not this task's.
    /// </remarks>
    [Fact]
    public void Both_lattices_carry_half_the_buildings()
    {
        World world = Populated("twinned.toml");
        int boundary = Shipped("twinned.toml").Lattices[1].OriginEastTiles;

        List<int> eastings = BuildingEastings(world);
        int west = eastings.Count(east => east < boundary);
        int east = eastings.Count - west;

        Assert.True(west > 0 && east > 0,
            $"the Buildings are all in one lattice: {west} west of {boundary} and {east} east of it. "
            + "A lattice with nothing on it is not a centre, and the density field has one peak.");

        Assert.True(
            west - east <= 1 && east - west <= 1,
            $"{west} Buildings west and {east} east. The shares are an equal split with the "
            + "remainder to the first lattice, so they differ by at most one.");
    }

    /// <summary>
    /// 🔴 <b>Nothing stands in the gap, so the saddle in the density field is zero.</b>
    /// </summary>
    /// <remarks>
    /// <b>This is the assertion the corridor could most easily have broken.</b> The link is Street
    /// Segments on the block grid, so a map-wide subdivision walk would carve Lots along it and the
    /// generator would fill the gap with houses — one ridge of development between two towns, which is
    /// a single centre with a waist. <c>SyntheticCity.Subdivide</c> walks each lattice's own block box
    /// for exactly this reason.
    /// </remarks>
    [Fact]
    public void Nothing_is_built_in_the_gap_between_them()
    {
        Ruleset rules = Shipped("twinned.toml");
        World world = Populated("twinned.toml");

        // The box each lattice's own Lots can fall in, derived the way the generator derives it
        // rather than read back off the Buildings -- a bound taken from the answer proves nothing.
        int reach = SyntheticCity.PavedTiles(world) + rules.Roads.BlockTiles;
        int first = rules.Lattices[0].OriginEastTiles;
        int second = rules.Lattices[1].OriginEastTiles;

        Assert.True(
            first + reach < second,
            $"the two lattices reach {reach} Tiles from origins {first} and {second}, so their own "
            + "ground touches and there is no gap between them at all. This world is one centre.");

        foreach (int east in BuildingEastings(world))
        {
            Assert.True(
                (east >= first && east <= first + reach)
                || (east >= second && east <= second + reach),
                $"a Building stands at east {east}, outside both lattices. The ground between them "
                + "carries the corridor joining the two and nothing else -- a Building on it fills "
                + "the saddle the two centres are separated by.");
        }
    }

    /// <summary>
    /// ⚠ <b>The gap is an order of magnitude wider than either lattice is across</b>, which is what
    /// task 1 was asked for: a world any sane prominence threshold splits, rather than one that
    /// calibrates it.
    /// </summary>
    [Fact]
    public void The_gap_is_unambiguous_rather_than_marginal()
    {
        World world = Populated("twinned.toml");
        int origin = Shipped("twinned.toml").Lattices[1].OriginEastTiles;

        List<int> eastings = BuildingEastings(world);

        int westSpan = eastings.Where(east => east < origin).Max();
        int gap = origin - westSpan;

        Assert.True(
            gap > 4 * westSpan,
            $"the built part of the western lattice is {westSpan} Tiles across and the gap to the "
            + $"eastern one is {gap}. A gap of the same order as a lattice is a world that CHOOSES "
            + "the prominence threshold rather than demonstrating it, and the threshold is not "
            + "chosen until task 3.");
    }

    /// <summary>
    /// <b>Everybody is still housed</b> — the split divides the city, it does not shrink it.
    /// </summary>
    [Fact]
    public void Splitting_the_city_houses_the_same_people()
    {
        World twinned = Populated("twinned.toml");
        World single = Populated("minimal.toml");

        Assert.Equal(single.Citizens.Rows.LiveCount, twinned.Citizens.Rows.LiveCount);
        Assert.Equal(single.Households.Rows.LiveCount, twinned.Households.Rows.LiveCount);
        Assert.Equal(single.Buildings.Rows.LiveCount, twinned.Buildings.Rows.LiveCount);
    }

    // ---- the refusals --------------------------------------------------------------------------

    private static RulesetLoadResult Load(string body) =>
        RulesetLoader.Parse(body, "test.toml");

    private static string Twinned(string lattices) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
        + "\n"
        + lattices;

    /// <summary>An origin off the block grid is refused, because the corridor is laid in blocks.</summary>
    [Fact]
    public void An_origin_off_the_block_grid_is_refused()
    {
        RulesetLoadResult result = Load(Twinned(
            "[[lattice]]\norigin_east_tiles = 0\norigin_north_tiles = 0\n\n"
            + "[[lattice]]\norigin_east_tiles = 2049\norigin_north_tiles = 0\n"));

        Assert.Null(result.Ruleset);
        Assert.Contains("block_tiles", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>Two lattices on one origin are one lattice laid twice.</summary>
    [Fact]
    public void A_duplicate_origin_is_refused()
    {
        RulesetLoadResult result = Load(Twinned(
            "[[lattice]]\norigin_east_tiles = 2048\norigin_north_tiles = 0\n\n"
            + "[[lattice]]\norigin_east_tiles = 2048\norigin_north_tiles = 0\n"));

        Assert.Null(result.Ruleset);
        Assert.Contains("a second lattice", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>An origin off the map names ground the generator cannot lay on.</summary>
    [Fact]
    public void An_origin_off_the_map_is_refused()
    {
        RulesetLoadResult result = Load(Twinned(
            "[[lattice]]\norigin_east_tiles = 0\norigin_north_tiles = 0\n\n"
            + $"[[lattice]]\norigin_east_tiles = {CellGrid.WorldTiles}\norigin_north_tiles = 0\n"));

        Assert.Null(result.Ruleset);
        Assert.Contains("off the map", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Arterials and a second lattice are refused together</b>, because an Arterial is laid per
    /// lattice and the file would say <i>n cross the map</i> while meaning <i>n in each</i>.
    /// </summary>
    [Fact]
    public void Two_lattices_and_arterials_together_are_refused()
    {
        string body = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
            .Replace("arterial_count                 = 0", "arterial_count                 = 4",
                StringComparison.Ordinal);

        RulesetLoadResult result = Load(
            body
            + "\n[[lattice]]\norigin_east_tiles = 0\norigin_north_tiles = 0\n\n"
            + "[[lattice]]\norigin_east_tiles = 2048\norigin_north_tiles = 0\n");

        Assert.Null(result.Ruleset);
        Assert.Contains("arterial_count", result.Describe(), StringComparison.Ordinal);
    }

    /// <summary>A Lattice IS a Street lattice, so one without <c>[roads]</c> has nothing to be.</summary>
    [Fact]
    public void A_lattice_without_roads_is_refused()
    {
        RulesetLoadResult result = Load(
            "[[resource]]\nname = \"sundries\"\nfamily = \"good\"\n\n"
            + "[[lattice]]\norigin_east_tiles = 0\norigin_north_tiles = 0\n");

        Assert.Null(result.Ruleset);
        Assert.Contains("[roads]", result.Describe(), StringComparison.Ordinal);
    }
}
