using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 24 task 2: what <see cref="TerrainGenerator"/> lays, and what it lays it from.
/// </summary>
/// <remarks>
/// <para>
/// <c>adr/0158</c>. <b>The pass is a pure function of the <see cref="WorldKey"/></b>, and the three
/// properties below are what that means operationally: the same key gives the same map, a different
/// key gives a different one, and the Ruleset changes neither.
/// </para>
/// <para>
/// ⚠ <b>Nothing here asserts a share.</b> How much of a map is rock is a property of the key and of
/// the generator's derived shape, and pinning one would make a re-baseline out of every future change
/// to the height field. What is asserted is that <b>all five types exist</b>, which is the property
/// <c>MapLayers.Fertility</c> and task 4's decay rate both need and which no share fixes.
/// </para>
/// </remarks>
public sealed class TerrainGeneratorTests
{
    private static Ruleset Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException($"{file} was refused:\n{result.Describe()}");
    }

    /// <summary>Every Cell's terrain type, in <see cref="CellGrid.Index"/> order.</summary>
    private static TerrainKind[] Map(WorldKey key)
    {
        TerrainCellTable terrain = new();
        TerrainGenerator.LayInto(terrain, key);

        var map = new TerrainKind[CellGrid.WorldCellCount];

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                map[CellGrid.Index(x, y)] = terrain.At(x, y);
            }
        }

        return map;
    }

    private static int Differences(TerrainKind[] left, TerrainKind[] right)
    {
        int differences = 0;

        for (int cell = 0; cell < left.Length; cell++)
        {
            if (left[cell] != right[cell])
            {
                differences++;
            }
        }

        return differences;
    }

    // ---- the key is the whole input --------------------------------------------------------------

    /// <summary>The same <see cref="WorldKey"/> gives the same map, every time.</summary>
    /// <remarks>
    /// <b>The property the save rests on.</b> The type column is <c>(saved AND hashed)</c>, so two
    /// runs of one key that disagreed would be two different cities wearing one seed — and
    /// <c>05 §4</c>'s replay equivalence would fail on the Tick the world was made.
    /// </remarks>
    [Fact]
    public void The_same_key_gives_the_same_map()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);

        Assert.Equal(0, Differences(Map(key), Map(key)));
    }

    /// <summary>A different <see cref="WorldKey"/> gives a different map.</summary>
    /// <remarks>
    /// ⚠ <b>Asserted as <em>most Cells differ</em> rather than <em>some Cell differs</em>.</b> Two
    /// unrelated maps agree on about a third of their Cells by coincidence — the shares are far from
    /// uniform — so <em>some Cell differs</em> would also pass for a generator that varied only its
    /// fine detail and kept one world's geography for every seed.
    /// </remarks>
    [Fact]
    public void A_different_key_gives_a_different_map()
    {
        TerrainKind[] one = Map(WorldKey.FromSeed(0x5EA1U));
        TerrainKind[] other = Map(WorldKey.FromSeed(0x1234U));

        Assert.True(
            Differences(one, other) > CellGrid.WorldCellCount / 2,
            $"only {Differences(one, other)} of {CellGrid.WorldCellCount} Cells differ between two "
            + "world keys, so the map is barely a function of the key.");
    }

    // ---- what a map contains ----------------------------------------------------------------------

    /// <summary>Every terrain type appears on a generated map.</summary>
    /// <remarks>
    /// <para>
    /// <b>It holds for any key, by construction rather than by luck.</b> The five bands partition the
    /// height range <em>this key actually produced</em>, so the lowest Cell is in the lowest band and
    /// the highest is in the highest whatever the seed. Banding the range the sum <em>could</em>
    /// produce is what would have made this a property of the seed.
    /// </para>
    /// <para>
    /// ⚠ <b>It is asserted per key rather than once</b>, because "any key" is the claim.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0x5EA1U)]
    [InlineData(0x1234U)]
    [InlineData(0xABCDU)]
    [InlineData(1U)]
    [InlineData(uint.MaxValue)]
    public void Every_terrain_type_appears(uint seed)
    {
        var seen = new bool[TerrainRuleset.Kinds];

        foreach (TerrainKind kind in Map(WorldKey.FromSeed(seed)))
        {
            seen[(int)kind] = true;
        }

        for (int kind = 0; kind < seen.Length; kind++)
        {
            Assert.True(seen[kind], $"{(TerrainKind)kind} is on no Cell of seed {seed:X}.");
        }
    }

    /// <summary>
    /// Terrain comes in <b>patches</b> and not in speckle.
    /// </summary>
    /// <remarks>
    /// <b>The property that makes a siting decision possible at all</b>, and the one a per-Cell draw
    /// would fail while producing identical shares. It is asserted as a floor on the mean run length
    /// along a row — far below what is measured — rather than as the measured figure, because the
    /// figure is an instrument's and this is an assertion: what must not regress is that a run is
    /// many Cells long, not that it is any particular number of them.
    /// </remarks>
    [Fact]
    public void Terrain_comes_in_patches()
    {
        TerrainKind[] map = Map(WorldKey.FromSeed(0x5EA1U));
        int runs = 0;

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            runs++;

            for (int east = 1; east < CellGrid.WorldCells; east++)
            {
                if (map[(north * CellGrid.WorldCells) + east]
                    != map[(north * CellGrid.WorldCells) + east - 1])
                {
                    runs++;
                }
            }
        }

        int mean = CellGrid.WorldCellCount / runs;

        Assert.True(mean > 8, $"the mean run of one terrain type along a row is {mean} Cells, which "
            + "is speckle rather than geography.");
    }

    // ---- what does NOT shape the map ---------------------------------------------------------------

    /// <summary>
    /// The Ruleset does not shape the terrain. <b><c>varied.toml</c> and <c>minimal.toml</c> generate
    /// the identical map.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The assumption <c>TerrainRuleset.Kinds</c>' all-five refusal rests on.</b> That refusal
    /// argues an unstated type would be <em>ground the world contains and the file prices at zero</em>,
    /// which is only true if the generator ignores the Ruleset. This is where that is checked rather
    /// than assumed — and it is also the sentence <c>varied.toml</c>'s header exists to stop a reader
    /// getting backwards.
    /// </remarks>
    [Fact]
    public void The_ruleset_does_not_shape_the_terrain()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);

        World plain = new(1_000, Load("minimal.toml"), key);
        World priced = new(1_000, Load("varied.toml"), key);

        SyntheticCity.PopulateInto(plain, key, Ticks.Zero);
        SyntheticCity.PopulateInto(priced, key, Ticks.Zero);

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                Assert.Equal(plain.Layers.Terrain.At(x, y), priced.Layers.Terrain.At(x, y));
            }
        }
    }

    /// <summary>
    /// A populated world's terrain is the generator's, and populating it is what wrote it.
    /// </summary>
    /// <remarks>
    /// <c>plans/0042</c> decision 3 places the pass inside
    /// <see cref="SyntheticCity.PopulateInto"/>, between the already-populated refusal and
    /// <c>LayLand</c>. This asserts the wiring rather than the placement — a pass nobody calls lays
    /// nothing, and every column would read <see cref="TerrainKind.Ordinary"/> without saying so.
    /// </remarks>
    [Fact]
    public void Populating_a_world_lays_its_terrain()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(1_000, Load("varied.toml"), key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        TerrainKind[] expected = Map(key);

        for (int north = 0; north < CellGrid.WorldCells; north++)
        {
            for (int east = 0; east < CellGrid.WorldCells; east++)
            {
                Cells x = new(east);
                Cells y = new(north);

                Assert.Equal(expected[CellGrid.Index(x, y)], world.Layers.Terrain.At(x, y));
            }
        }
    }

    /// <summary>A Cell off the map has no terrain, and asking is an error rather than a default.</summary>
    [Fact]
    public void A_cell_off_the_map_has_no_terrain()
    {
        TerrainCellTable terrain = new();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => terrain.At(new Cells(CellGrid.WorldCells), new Cells(0)));
    }

    // ---- the guard that replaced a guard -----------------------------------------------------------

    /// <summary>
    /// Terrain written to after the ground was laid is reported by the end-of-run tier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>This check is what pays for <c>Simulation.VerifyDecideWritesNothing</c> skipping the
    /// terrain table</b> (<see cref="World.TablesAPhaseCanWrite"/>), so it is the one test in this file
    /// that a performance change is allowed to depend on. **Writing the violation and watching it fire
    /// is the whole point** — a guard traded away for a check nobody proved fires is not a trade, it is
    /// a hole with a comment on it.
    /// </para>
    /// <para>
    /// <b>The column is written directly, past the generator that is the only legitimate writer</b>, on
    /// <c>An_over_sealed_Cell_is_reported_by_the_end_of_run_tier</c>'s reasoning: a check must not
    /// depend on the setter it is checking.
    /// </para>
    /// </remarks>
    [Fact]
    public void Terrain_written_to_after_it_was_laid_is_reported_by_the_end_of_run_tier()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(1_000, Load("varied.toml"), key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.Invariants.Collect = true;

        Cells east = new(7);
        Cells north = new(11);

        // Whatever the generator laid here, this is not it.
        TerrainKind laid = world.Layers.Terrain.At(east, north);
        world.Layers.Terrain.Set(
            east, north, laid == TerrainKind.Rock ? TerrainKind.Marsh : TerrainKind.Rock);

        new Simulation(world, key).CheckEndOfRun();

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.TerrainIsUnchangedSinceItWasLaid);
    }

    /// <summary>An untouched world passes the same check.</summary>
    /// <remarks>
    /// The other half, and not a formality: a check that fires on everything is as useless as one that
    /// fires on nothing, and this one re-runs a generator whose determinism is the thing under test.
    /// </remarks>
    [Fact]
    public void A_generated_world_passes_the_end_of_run_terrain_check()
    {
        WorldKey key = WorldKey.FromSeed(0x5EA1U);
        World world = new(1_000, Load("varied.toml"), key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.Invariants.Collect = true;

        new Simulation(world, key).CheckEndOfRun();

        Assert.DoesNotContain(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.TerrainIsUnchangedSinceItWasLaid);
    }
}
