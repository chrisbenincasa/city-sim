using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Invariants;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 12 task 5: Pool Bins — a Bin per Good per District.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>Scope.Pool</c> still throws after this task, and that is what these tests are written
/// against.</b> Nothing in the build can put a unit into a Pool, so every assertion below that
/// involves stock puts it there by hand through <c>World.Deposit</c>. ***A mechanism whose only
/// producer is a test is a mechanism the suite is the sole evidence for***, which is why the two
/// invariants matter more here than the arithmetic does.
/// </para>
/// <para>
/// <b>The Pool hangs off the owner row and not off the Bin</b> — <c>DistrictPoolTable</c>, because
/// <c>BinTable.Owner</c> is a handle bound to the Building table and a District cannot go in it. So
/// there are two things to check that a Building's Bins never needed: that the join survives a reload
/// (it is the only saved statement of the relation), and that a District's death takes its Pool with
/// it rather than leaving rows naming a freed row.
/// </para>
/// </remarks>
public sealed class DistrictPoolTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    /// <summary>The first <c>[[resource]]</c> of the shipped files, and its family is <c>good</c>.</summary>
    private static readonly ResourceId Sundries = new(1);

    /// <summary>The second, also a Good.</summary>
    private static readonly ResourceId Repairs = new(2);

    /// <summary>The third, and <c>money</c> — which a Pool must NOT hold at task 5.</summary>
    private static readonly ResourceId Money = new(3);

    private const int Percent = 50;
    private const int Revisit = 2_048;
    private const int Band = 50;
    private const int Migrate = 16;

    private const int PeakCells = 2;
    private const int Valleys = 4;
    private const int Tall = 8;
    private const int Short = 3;
    private const int Cells = (PeakCells * 2) + Valleys;

    private static string Body(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

    private static string Strip(string body)
    {
        List<string> kept = [];
        bool inside = false;

        foreach (string line in body.Split('\n'))
        {
            if (line.TrimStart().StartsWith('['))
            {
                inside = line.TrimStart().StartsWith("[districts]", StringComparison.Ordinal);
            }

            if (!inside)
            {
                kept.Add(line);
            }
        }

        return string.Join('\n', kept);
    }

    private static Ruleset Parsed(string text, string name)
    {
        RulesetLoadResult result = RulesetLoader.Parse(text, name);

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the Ruleset {name} was refused, so this test cannot run:\n{result.Describe()}");
    }

    private static Ruleset Rules(string file, int percent = Percent) =>
        Parsed(
            $"{Strip(Body(file))}\n[districts]\nprominence_percent = {percent}\n"
            + $"revisit_ticks = {Revisit}\nhysteresis_percent = {Band}\nmigrate_cells = {Migrate}\n",
            file);

    /// <summary>The same file with no <c>[districts]</c> table at all, so no District exists.</summary>
    private static Ruleset Without(string file) => Parsed(Strip(Body(file)), file);

    /// <summary>
    /// One Street chain, tall Cells at each end and a low ridge between — two Districts.
    /// </summary>
    /// <remarks>
    /// <b><c>DistrictReevaluationTests.Ridge</c>'s world, and it is duplicated rather than shared on
    /// purpose.</b> That file's copy exists to exercise hysteresis and its constants are chosen for
    /// the band; this one exists to give two Districts that can be made to merge. ***A fixture shared
    /// between two tests that need different things from it becomes a fixture neither can change***,
    /// and the shape is nine lines.
    /// </remarks>
    private static World Ridge(int percent = Percent)
    {
        var world = new World(100, Rules("minimal.toml", percent), Key);

        Lay(world);

        for (int cell = 0; cell < Cells; cell++)
        {
            bool peak = cell < PeakCells || cell >= Cells - PeakCells;

            Raise(world, cell, peak ? Tall : Short, rebuild: false);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        return world;
    }

    private static void Lay(World world)
    {
        int block = world.Rules.Roads.BlockTiles;

        Handle<RoadNode> previous = world.Roads.Nodes.Create(Tiles.Zero, Tiles.Zero);

        for (int step = 1; step <= Cells; step++)
        {
            Handle<RoadNode> next = world.Roads.Nodes.Create(new Tiles(step * block), Tiles.Zero);

            world.Roads.Segments.Create(
                previous, next, new Tiles(block), RoadKind.Street, TravelMode.Any, TravelMode.Any);

            previous = next;
        }

        world.Roads.RebuildDerived();
    }

    private static void Raise(World world, int cell, int count, bool rebuild = true)
    {
        int block = world.Rules.Roads.BlockTiles;
        int standing = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot)
                && world.Lots.East[slot].Raw >= cell * block
                && world.Lots.East[slot].Raw < (cell + 1) * block)
            {
                standing++;
            }
        }

        for (int i = 0; i < count; i++)
        {
            Handle<Lot> lot = world.Lots.Create(
                new Tiles((cell * block) + 4 + ((standing + i) * 2)), Tiles.Zero, zone: 1);

            world.CreateBuilding(lot, kind: 0, Ticks.Zero, Key);
        }

        if (rebuild)
        {
            world.RebuildDerived();
        }
    }

    private static List<int> Standing(World world)
    {
        List<int> standing = [];

        for (int slot = 0; slot < world.Districts.Rows.SlotCount; slot++)
        {
            if (world.Districts.Rows.IsLive(slot))
            {
                standing.Add(slot);
            }
        }

        return standing;
    }

    private static int PoolRows(World world)
    {
        int rows = 0;

        for (int slot = 0; slot < world.DistrictPools.Rows.SlotCount; slot++)
        {
            if (world.DistrictPools.Rows.IsLive(slot))
            {
                rows++;
            }
        }

        return rows;
    }

    private static void Fill(World world, int districtSlot, ResourceId resource, long amount) =>
        world.Deposit(
            world.Bins.Rows.At(world.FindDistrictPoolBin(districtSlot, resource)), amount, Ticks.Zero);

    private static long Held(World world, int districtSlot, ResourceId resource)
    {
        int bin = world.FindDistrictPoolBin(districtSlot, resource);

        return bin == Rows.NoSlot ? -1 : world.Bins.LevelAt(bin);
    }

    // ---- what a Pool is -------------------------------------------------------------------------

    /// <summary>Every District gets one Bin per Good, and none for money.</summary>
    /// <remarks>
    /// <b>The money half is the assertion, not the padding.</b> <c>plans/0037</c> decision 10 —
    /// <em>who holds the Pool's money between a Provider's deposit and a consumer's draw</em> — is
    /// open, and a Bin opened before it is answered would BE the answer, written by whoever needed a
    /// column rather than by the sitting that owes it.
    /// </remarks>
    [Fact]
    public void A_district_holds_one_bin_per_good_and_none_for_money()
    {
        World world = Ridge();

        List<int> districts = Standing(world);

        Assert.Equal(2, districts.Count);

        foreach (int district in districts)
        {
            Assert.NotEqual(Rows.NoSlot, world.FindDistrictPoolBin(district, Sundries));
            Assert.NotEqual(Rows.NoSlot, world.FindDistrictPoolBin(district, Repairs));
            Assert.Equal(Rows.NoSlot, world.FindDistrictPoolBin(district, Money));
        }

        Assert.Equal(districts.Count * 2, PoolRows(world));
    }

    /// <summary>A Pool Bin names its kind and leaves the Building handle unset.</summary>
    /// <remarks>
    /// <b>This is the shape the task was specified against.</b> <c>BinTable.Owner</c> is a
    /// <c>HandleColumn&lt;Building&gt;</c> bound to the Building table at construction, so a District
    /// cannot address its owner through it — and a Bin that quietly resolved to Building slot 0 would
    /// be worse than one that resolves to nothing.
    /// </remarks>
    [Fact]
    public void A_pool_bin_names_its_kind_and_no_building()
    {
        World world = Ridge();

        int bin = world.FindDistrictPoolBin(Standing(world)[0], Sundries);

        Assert.Equal(BinOwnerKind.District, world.Bins.OwnerKind[bin]);
        Assert.True(world.Bins.Owner[bin].IsNone);
    }

    /// <summary>A Pool Bin is unbounded, and a reload derives the same ceiling.</summary>
    /// <remarks>
    /// <b>The second half is the one that has been a live defect before</b>, on the treasury: capacity
    /// is <c>Derived</c>, so it is not in the file, and a load creates a Bin through <c>Rows.Restore</c>
    /// rather than through the method that sets the ceiling. A reloaded Pool that came back at zero
    /// would refuse every deposit into it.
    /// </remarks>
    [Fact]
    public void A_pool_bin_is_unbounded_and_stays_so_across_a_rebuild()
    {
        World world = Ridge();

        int bin = world.FindDistrictPoolBin(Standing(world)[0], Sundries);

        Assert.Equal(long.MaxValue, world.Bins.Capacity[bin]);

        world.RebuildDerived();

        Assert.Equal(long.MaxValue, world.Bins.Capacity[bin]);
    }

    /// <summary>A Ruleset that states no <c>[districts]</c> opens no Pool at all.</summary>
    [Fact]
    public void A_world_with_no_districts_has_no_pool()
    {
        var world = new World(100, Without("minimal.toml"), Key);

        Lay(world);

        for (int cell = 0; cell < Cells; cell++)
        {
            Raise(world, cell, Short, rebuild: false);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Empty(Standing(world));
        Assert.Equal(0, PoolRows(world));
    }

    /// <summary>Re-evaluating an unchanged world leaves every Pool Bin the same row.</summary>
    /// <remarks>
    /// <b>This is what task 4's persistence was FOR, arriving at its first consumer.</b> A District
    /// that kept its row across a re-evaluation but whose Pool was reopened would be a Pool emptied
    /// once a Day, and nothing in the identity assertions of task 4 would have noticed.
    /// </remarks>
    [Fact]
    public void A_reevaluation_that_changes_nothing_leaves_the_pool_alone()
    {
        World world = Ridge();

        int before = world.FindDistrictPoolBin(Standing(world)[0], Sundries);

        Fill(world, Standing(world)[0], Sundries, 7);

        world.EvaluateDistricts();

        Assert.Equal(before, world.FindDistrictPoolBin(Standing(world)[0], Sundries));
        Assert.Equal(7, Held(world, Standing(world)[0], Sundries));
        Assert.Equal(Standing(world).Count * 2, PoolRows(world));
    }

    /// <summary>A Good the reload introduced reaches the Districts that already exist.</summary>
    /// <remarks>
    /// <b>Without this the new Good is unpoolable until the next evaluation</b>, which is a cadence
    /// measured in Days — so a hot reload would appear to work and then not, which is the failure
    /// <c>adr/0015</c>'s acceptance test exists to prevent.
    /// </remarks>
    [Fact]
    public void A_ruleset_swap_that_adds_a_good_fits_the_standing_districts()
    {
        World world = Ridge();

        int before = PoolRows(world);

        // APPENDED and not inserted, and the difference is not cosmetic: a ResourceId is the
        // declaration's POSITION, so a resource added in the middle renumbers every one after it and
        // every standing Bin keeps an id that now names something else. This test is about fitting a
        // Pool; the renumbering is filed in plans/0012 rather than demonstrated here.
        string added = $"{Strip(Body("minimal.toml"))}\n"
            + "[[resource]]\nname = \"clinker\"\nfamily = \"good\"\n";

        world.Adopt(
            Parsed(
                $"{added}\n[districts]\nprominence_percent = {Percent}\nrevisit_ticks = {Revisit}\n"
                + $"hysteresis_percent = {Band}\nmigrate_cells = {Migrate}\n",
                "minimal.toml"),
            0xD15C_0000_0000_0005UL,
            Ticks.Zero,
            Key);

        Assert.Equal(before + Standing(world).Count, PoolRows(world));
    }

    // ---- what happens when a District dies -------------------------------------------------------

    /// <summary>A dying District's stock goes to whoever took its centre Cell.</summary>
    /// <remarks>
    /// <b>Raising the valley to the peaks' own height is what merges them.</b> Every Cell then holds
    /// the same count, so the two basins meet at their own level, the loser's prominence is zero and
    /// it stops being a seed — and its centre Cell is inside the survivor. ⚠ <b>The assertion is on
    /// the SUM and not on which row survived</b>: which of two equal peaks wins is Cell-index order,
    /// which is a property of the map and not of this mechanism.
    /// </remarks>
    [Fact]
    public void A_dying_districts_stock_moves_to_the_district_that_took_its_centre()
    {
        World world = Ridge();

        List<int> before = Standing(world);

        Assert.Equal(2, before.Count);

        Fill(world, before[0], Sundries, 11);
        Fill(world, before[1], Sundries, 4);

        for (int cell = PeakCells; cell < Cells - PeakCells; cell++)
        {
            Raise(world, cell, Tall - Short, rebuild: false);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        List<int> after = Standing(world);

        Assert.Single(after);
        Assert.Equal(15, Held(world, after[0], Sundries));
        Assert.Equal(2, PoolRows(world));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>A District that dies leaves no Pool row and no Bin behind.</summary>
    /// <remarks>
    /// <b>The State Hash cannot report the alternative</b>: a Pool row naming a freed District folds
    /// the target's monotonic id, and a freed target folds as zero — so two runs would reproduce the
    /// same dangling row and every determinism test would agree about it.
    /// </remarks>
    [Fact]
    public void A_dying_district_takes_its_pool_rows_and_its_bins_with_it()
    {
        World world = Ridge();

        int doomed = Standing(world)[1];
        int bin = world.FindDistrictPoolBin(doomed, Sundries);

        for (int cell = PeakCells; cell < Cells - PeakCells; cell++)
        {
            Raise(world, cell, Tall - Short, rebuild: false);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.False(world.Districts.Rows.IsLive(doomed));
        Assert.Equal(2, PoolRows(world));
        Assert.True(!world.Bins.Rows.IsLive(bin) || world.Bins.OwnerKind[bin] != BinOwnerKind.District
            || world.FindDistrictPoolBin(Standing(world)[0], Sundries) == bin);

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>An empty Pool dying with no heir at all is legal, and is what a demolition does.</summary>
    [Fact]
    public void A_district_whose_ground_is_demolished_dies_with_no_heir_and_no_complaint()
    {
        World world = Ridge();

        List<Handle<Building>> condemned = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                condemned.Add(world.Buildings.Rows.At(slot));
            }
        }

        foreach (Handle<Building> building in condemned)
        {
            world.DestroyBuilding(building, Ticks.Zero);
        }

        world.RebuildDerived();
        world.EvaluateDistricts();

        Assert.Empty(Standing(world));
        Assert.Equal(0, PoolRows(world));

        world.Invariants.RunEndOfRun(world);
    }

    /// <summary>Stock in a District that dies with no heir is reported rather than lost.</summary>
    /// <remarks>
    /// <b>This is <c>04 §2</c>'s audit standing where a District dies</b> — <em>"if a hundred units of
    /// Food entered the District, a hundred units must be accounted for."</em> ⚠ <b>It cannot happen
    /// through any path the build has today</b>, because <c>Scope.Pool</c> throws and no Pool is ever
    /// non-empty; the deposit below is the test putting the world into the state task 7 will make
    /// reachable. ***A check nothing can currently trip is still the difference between a hole that
    /// fails and a hole that leaks.***
    /// </remarks>
    [Fact]
    public void Stock_in_a_district_that_dies_heirless_is_reported()
    {
        World world = Ridge();

        Fill(world, Standing(world)[0], Sundries, 3);

        List<Handle<Building>> condemned = [];

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot))
            {
                condemned.Add(world.Buildings.Rows.At(slot));
            }
        }

        foreach (Handle<Building> building in condemned)
        {
            world.DestroyBuilding(building, Ticks.Zero);
        }

        world.RebuildDerived();

        Assert.Equal(
            Invariant.ADistrictDiesWithAnHeirOrAnEmptyPool,
            Assert.Throws<InvariantViolationException>(world.EvaluateDistricts).Violation.Invariant);
    }

    // ---- the end-of-run check --------------------------------------------------------------------

    /// <summary>A Pool row whose District handle names nothing is reported.</summary>
    /// <remarks>
    /// <para>
    /// <b>The check is called directly rather than through <c>RunEndOfRun</c></b>, which is what
    /// <c>DistrictReevaluationTests</c> does and for a reason this test found the hard way: a
    /// <em>freed</em> District is already reported by
    /// <see cref="Invariant.CrossTableHandleResolves"/>, which is registered first and is column-driven,
    /// so it covers every saved handle column the day it is declared. Going through the registry
    /// asserts the registration order.
    /// </para>
    /// <para>
    /// ⚠ <b>So the case left to this member is the UNSET handle</b> — a row created before the row it
    /// names, which is not dangling and which the generic walk is right not to report. The overlap is
    /// stated at the member rather than removed, because the sentence <em>a Pool row names a live
    /// District</em> is the one a reader wants and half of it being covered elsewhere is not a reason
    /// to write down the other half alone.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_pool_row_naming_no_district_is_reported()
    {
        World world = Ridge();

        world.Invariants.Collect = true;
        WorldInvariants.DistrictPoolsAreOneLiveBinPerGood(world, world.Invariants);

        Assert.Empty(world.Invariants.Collected);

        world.DistrictPools.Create(
            default, world.Bins.Rows.At(world.FindDistrictPoolBin(Standing(world)[0], Sundries)));

        WorldInvariants.DistrictPoolsAreOneLiveBinPerGood(world, world.Invariants);

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.ADistrictPoolIsOneLiveBinPerGood);
    }

    /// <summary>A District missing a Good's Bin is reported, which is a missed fit.</summary>
    /// <remarks>
    /// <b>The half that catches the failure that reads as a starving city.</b> A District whose Pool
    /// was never opened has every Good permanently out of stock, and <c>02 §5.9</c>'s starvation looks
    /// exactly like that from every panel the player has. ⚠ <b>Nothing generic covers this one</b>:
    /// the column walk asks whether what is there resolves, and this is about what is not there.
    /// </remarks>
    [Fact]
    public void A_district_missing_a_goods_bin_is_reported()
    {
        World world = Ridge();

        int bin = world.FindDistrictPoolBin(Standing(world)[0], Repairs);

        for (int slot = 0; slot < world.DistrictPools.Rows.SlotCount; slot++)
        {
            if (world.DistrictPools.Rows.IsLive(slot)
                && world.Bins.Rows.TryResolve(world.DistrictPools.Bin[slot], out int held)
                && held == bin)
            {
                world.DistrictPools.Rows.Free(world.DistrictPools.Rows.At(slot));
                break;
            }
        }

        world.Invariants.Collect = true;
        WorldInvariants.DistrictPoolsAreOneLiveBinPerGood(world, world.Invariants);

        Assert.Contains(
            world.Invariants.Collected,
            violation => violation.Invariant == Invariant.ADistrictPoolIsOneLiveBinPerGood);
    }

    /// <summary>A world with Districts and Pools passes the check it ships with.</summary>
    [Fact]
    public void A_fitted_world_is_clean()
    {
        World world = Ridge();

        world.Invariants.RunEndOfRun(world);
    }
}
