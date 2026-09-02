using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Space;

/// <summary>
/// Milestone 12 task 2: the <b>Building-density field</b> the District watershed reads
/// (<c>adr/0134</c>).
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The field was already built, and task 2 is the name arriving rather than the storage.</b>
/// <see cref="BuildingResidency"/> has cached its own per-Cell list lengths since 5b-bis, because the
/// job search needed a fair draw over a box, and <c>adr/0134</c>'s <i>Building-density field on the
/// Cell grid</i> is exactly that array. ***A value per Cell is a field whatever the consumer that
/// first wanted it was called.*** What these tests hold is the part that was not established: that the
/// field has the SHAPE the watershed needs, on the worlds that exist.
/// </para>
/// <para>
/// ⚠ <b>What they assert about is a WORLD and not a mechanism</b>, exactly as task 1's did. There is
/// no watershed yet — that is task 3 — so nothing here counts Districts. What it counts is
/// <b>maximal plateau components</b>: the connected sets of Cells no neighbour exceeds, which is the
/// set of things a prominence-seeded watershed would consider as candidate centres. One on a
/// one-lattice world, two on a two-lattice one.
/// </para>
/// <para>
/// ⚠ <b>The field is FLAT and that is the measurement worth carrying.</b> The generator lays Lots
/// uniformly, so a Cell inside the built area holds <b>exactly ten</b> Buildings — there is no
/// gradient inside a lattice at all, and a concentration is a plateau with a cliff at its edge rather
/// than a hill. ***That is why smoothing buys nothing here***: a kernel is what you reach for against
/// noise, and this field has none. It also means <b>the whole of the boundary information lives in the
/// gap</b>, which is what makes <c>twinned.toml</c> the only world that carries any.
/// </para>
/// </remarks>
public sealed class BuildingDensityFieldTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(1);

    /// <summary>Buildings an interior Cell holds once the generator has filled around it.</summary>
    /// <remarks>
    /// 🔴 <b>EIGHT AND NOT TEN SINCE THE CORNER GOT AN OWNER</b>
    /// (<see cref="Borough.Core.Entities.LotSubdivider.CornerTiles"/>). Four faces at
    /// <c>lots_per_segment = 5</c> split by parity gives ten, and two of the ten stood on ground a
    /// perpendicular face had already claimed. ⚠ <b>The field is still FLAT</b> — every interior
    /// Cell moved together — which is what this test is actually about, and it is why this is one
    /// constant rather than a table of exceptions.
    /// </remarks>
    private const int BlockDensity = 8;

    /// <summary>A population inside the band where each lattice's edge is still ragged.</summary>
    /// <remarks>
    /// <para>
    /// <b>Measured, and stated in <c>rulesets/twinned.toml</c>'s header for the same reason it is
    /// here.</b> A lattice that has not filled its own ground has a ragged edge, and a ragged edge
    /// carries maxima of its own — more components than there are centres. ***A demonstration world
    /// has a size at which it demonstrates something else***, and the number is cheap to state and
    /// expensive to rediscover.
    /// </para>
    /// <para>
    /// <b>600 rather than the band's edge on purpose.</b> The sweep reads 2 at 200 and 2 at 1,000,
    /// so a test sited at either end is one small change away from measuring the boundary instead
    /// of the effect.
    /// </para>
    /// </remarks>
    private const int RaggedBandCitizens = 600;

    private static World Populated(string file, int citizens)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        Ruleset rules = result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused:\n{result.Describe()}");

        var world = new World(citizens, rules);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    /// <summary>
    /// <b>The height the WATERSHED sees</b>, which is Buildings plus the Lots a Zone is holding
    /// vacant for a trade.
    /// </summary>
    /// <remarks>
    /// <b>Not <c>BuildingsInCells.Density</c> alone, and the difference is <c>adr/0165</c>.</b> The
    /// land-use split leaves one block in <c>SyntheticCity.TradeBlockStride</c> permitted to a trade
    /// and unbuilt, and a block's Lots sit on the faces it shares with its neighbours — so counting
    /// only Buildings makes a commercial block **thin the Cells around it**, and this class measured
    /// an interior Cell at 5 where every other held 10. ***That is a hole in the instrument, not a
    /// gradient in the city***, and `DistrictWatershed.HeldForTrade` is where the simulation says so.
    /// This helper mirrors it so the assertions below keep measuring the field the watershed reads.
    /// </remarks>
    private static int Density(World world, int east, int north)
    {
        int height = world.BuildingsInCells.Density(new Cells(east), new Cells(north));

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot)
                || !world.Lots.IsVacant(slot)
                || (world.Lots.Zone[slot] & LotTable.Trade) == 0)
            {
                continue;
            }

            if (CellGrid.ToCells(world.Lots.East[slot]).Raw == east
                && CellGrid.ToCells(world.Lots.North[slot]).Raw == north)
            {
                height++;
            }
        }

        return height;
    }

    /// <summary>The smallest box of Cells holding every Cell with a Building in it.</summary>
    private static (int East, int North, int EastEnd, int NorthEnd) Built(World world)
    {
        int east = int.MaxValue, north = int.MaxValue, eastEnd = -1, northEnd = -1;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[slot]);
            int e = world.Lots.East[lot].Raw / CellGrid.TilesPerCell;
            int n = world.Lots.North[lot].Raw / CellGrid.TilesPerCell;

            east = e < east ? e : east;
            north = n < north ? n : north;
            eastEnd = e > eastEnd ? e : eastEnd;
            northEnd = n > northEnd ? n : northEnd;
        }

        return (east, north, eastEnd, northEnd);
    }

    /// <summary>Whether no neighbour of this Cell holds more Buildings than it does.</summary>
    private static bool IsMaximal(World world, int east, int north)
    {
        int here = Density(world, east, north);

        if (here == 0)
        {
            return false;
        }

        for (int dn = -1; dn <= 1; dn++)
        {
            for (int de = -1; de <= 1; de++)
            {
                if ((de != 0 || dn != 0) && Density(world, east + de, north + dn) > here)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// How many connected plateaus of maximal Cells the field has — <b>the candidate centres</b>.
    /// </summary>
    /// <remarks>
    /// <b>Components rather than Cells, because a flat field's maxima are plateaus.</b> A city whose
    /// interior is uniformly ten has hundreds of Cells no neighbour exceeds, and they are one
    /// concentration rather than hundreds. ⚠ **This is not the watershed and must not be mistaken for
    /// a preview of it**: it applies no prominence threshold, so it counts every plateau however
    /// slight — which is why it reports four on a 1,000-Citizen `twinned.toml` where the watershed
    /// would report two.
    /// </remarks>
    private static int Plateaus(World world)
    {
        (int east, int north, int eastEnd, int northEnd) = Built(world);

        var maximal = new HashSet<(int East, int North)>();

        for (int n = north; n <= northEnd; n++)
        {
            for (int e = east; e <= eastEnd; e++)
            {
                if (IsMaximal(world, e, n))
                {
                    maximal.Add((e, n));
                }
            }
        }

        int components = 0;

        while (maximal.Count > 0)
        {
            components++;

            var stack = new Stack<(int East, int North)>();
            (int East, int North) seed = maximal.First();

            maximal.Remove(seed);
            stack.Push(seed);

            while (stack.Count > 0)
            {
                (int e, int n) = stack.Pop();

                for (int dn = -1; dn <= 1; dn++)
                {
                    for (int de = -1; de <= 1; de++)
                    {
                        if (maximal.Remove((e + de, n + dn)))
                        {
                            stack.Push((e + de, n + dn));
                        }
                    }
                }
            }
        }

        return components;
    }

    // ---- the field itself ----------------------------------------------------------------------

    /// <summary>
    /// <b>The field is the count of Buildings in that Cell</b>, computed independently of the index.
    /// </summary>
    /// <remarks>
    /// <b>Walked off the Building table rather than compared against <see cref="BuildingResidency.CountIn"/>.</b>
    /// The two read the same array, so agreeing would establish nothing; what this asks is whether the
    /// array agrees with the Buildings.
    /// </remarks>
    [Fact]
    public void The_field_counts_the_buildings_standing_in_each_cell()
    {
        World world = Populated("twinned.toml", 4_000);

        var expected = new Dictionary<(int, int), int>();

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (!world.Buildings.Rows.IsLive(slot))
            {
                continue;
            }

            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[slot]);
            (int, int) cell = (
                world.Lots.East[lot].Raw / CellGrid.TilesPerCell,
                world.Lots.North[lot].Raw / CellGrid.TilesPerCell);

            expected[cell] = expected.GetValueOrDefault(cell) + 1;
        }

        Assert.NotEmpty(expected);

        // ⚠ THE RAW FIELD and not Density() above, because this is the one test whose subject IS the
        // Building count. Density() adds the Lots a Zone holds vacant for a trade, which is the
        // height the WATERSHED reads (adr/0165); asking it here would make this test assert the
        // watershed's input while claiming to assert the table's contents.
        foreach (((int east, int north), int count) in expected)
        {
            Assert.Equal(
                count, world.BuildingsInCells.Density(new Cells(east), new Cells(north)));
        }
    }

    /// <summary>
    /// <b>A Cell off the map reads zero rather than throwing</b>, which is what a neighbourhood walk
    /// at the map's edge needs.
    /// </summary>
    [Fact]
    public void The_field_is_zero_off_the_map()
    {
        World world = Populated("minimal.toml", 1_000);

        Assert.Equal(0, Density(world, -1, 0));
        Assert.Equal(0, Density(world, 0, -1));
        Assert.Equal(0, Density(world, CellGrid.WorldCells, 0));
        Assert.Equal(0, Density(world, 0, CellGrid.WorldCells));
    }

    /// <summary>
    /// <b>The field is exactly recoverable from saved state</b> — `(derived AND rebuilt)`, and the
    /// rebuild reproduces it rather than approximating it.
    /// </summary>
    [Fact]
    public void The_field_survives_a_rebuild_exactly()
    {
        World world = Populated("twinned.toml", 4_000);

        (int east, int north, int eastEnd, int northEnd) = Built(world);

        var before = new List<int>();

        for (int n = north; n <= northEnd; n++)
        {
            for (int e = east; e <= eastEnd; e++)
            {
                before.Add(Density(world, e, n));
            }
        }

        world.RebuildDerived();

        int index = 0;

        for (int n = north; n <= northEnd; n++)
        {
            for (int e = east; e <= eastEnd; e++)
            {
                Assert.Equal(before[index++], Density(world, e, n));
            }
        }
    }

    // ---- the shape the watershed will read -----------------------------------------------------

    /// <summary>
    /// ⚠ <b>The field is FLAT inside the built area — every interior Cell holds exactly ten.</b>
    /// </summary>
    /// <remarks>
    /// <b>Committed so that the day the generator stops laying Lots uniformly, this says so.</b> A
    /// flat field is a property of the fixture and not of the city model, and every argument about
    /// what the watershed needs — that smoothing buys nothing, that all the boundary information is in
    /// the gap, that prominence is doing the work — rests on it. ***An assumption a whole task rests
    /// on should fail a test when it stops holding, rather than being re-derived by whoever notices.***
    /// Milestone 15's agglomeration is what would put a gradient here.
    /// </remarks>
    [Fact]
    public void The_field_is_flat_inside_the_built_area()
    {
        World world = Populated("minimal.toml", 16_000);

        (int east, int north, int eastEnd, int northEnd) = Built(world);

        // The interior: Cells whose eight neighbours all hold Buildings, so no edge effect reaches
        // them. On a lattice this large there are many.
        int interior = 0;

        for (int n = north + 1; n < northEnd; n++)
        {
            for (int e = east + 1; e < eastEnd; e++)
            {
                bool surrounded = true;

                for (int dn = -1; dn <= 1 && surrounded; dn++)
                {
                    for (int de = -1; de <= 1; de++)
                    {
                        if (Density(world, e + de, n + dn) == 0)
                        {
                            surrounded = false;
                            break;
                        }
                    }
                }

                if (!surrounded)
                {
                    continue;
                }

                interior++;

                Assert.True(
                    Density(world, e, n) == BlockDensity,
                    $"Cell ({e}, {n}) holds {Density(world, e, n)} Buildings where an interior Cell "
                    + $"should hold {BlockDensity}. The generator has stopped laying Lots uniformly, so "
                    + "the density field now has a GRADIENT in it -- which is a better world and "
                    + "invalidates the argument that smoothing buys nothing (plans/0037 F8). "
                    + "Re-take that decision rather than lowering this.");
            }
        }

        Assert.True(interior > 20, $"only {interior} interior Cells, which is too few to read.");
    }

    /// <summary><b>A one-lattice world has ONE concentration</b>, at every size.</summary>
    /// <remarks>
    /// ⚠ <b>This is the assertion that makes task 1 necessary rather than nice.</b> A watershed over
    /// this field returns one basin however it is written, so nothing about `adr/0134` is testable
    /// here — which is why the milestone's first task was a world.
    /// </remarks>
    [Theory]
    [InlineData(1_000)]
    [InlineData(4_000)]
    [InlineData(16_000)]
    [InlineData(64_000)]
    public void A_one_lattice_world_has_one_concentration(int citizens) =>
        Assert.Equal(1, Plateaus(Populated("minimal.toml", citizens)));

    /// <summary>
    /// 🔴 <b>A two-lattice world has TWO concentrations</b>, above the ragged band and below it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The low rung is 200 and not the old 2,000 floor</b>: the band is a window rather than a
    /// minimum (see <see cref="RaggedBandCitizens"/>), so a city too SMALL to have a ragged edge
    /// reads two just as a filled one does, and both sides of the window are worth pinning.
    /// <para>
    /// 🔴 <b>It was 400, and 400 is INSIDE the band as of <c>plans/0053</c>.</b> Occupancy divides
    /// the ground, so a city of a given population takes a different number of Buildings and the
    /// band moved down and widened: it now runs <b>300–800</b> where it ran 500–800. The low rung
    /// moved to the nearest size still below it rather than the assertion being loosened.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(200)]
    [InlineData(4_000)]
    [InlineData(16_000)]
    [InlineData(64_000)]
    public void A_two_lattice_world_has_two_concentrations(int citizens) =>
        Assert.Equal(2, Plateaus(Populated("twinned.toml", citizens)));

    /// <summary>
    /// ⚠ <b>And inside the ragged band it does not</b> — <c>twinned.toml</c> at 600 Citizens is
    /// three plateaus.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Committed as a fact rather than left in prose, because it is the reading that would be
    /// mistaken for a defect.</b> Each lattice holds ~61 Buildings over ~11 Cells at that size and
    /// does not fill its own ground, so its ragged edge carries maxima of its own. ⚠ **The watershed
    /// would still find two**, because prominence is what discards a plateau whose saddle to a higher
    /// one is barely below it — ***this counts candidate centres and the watershed counts accepted
    /// ones***, and the gap between those two numbers is exactly what the prominence threshold is for.
    /// <b>If this ever reads 2, the band has moved and the file's header is now wrong.</b>
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>IT IS A BAND AND NOT A FLOOR, AND IT SAID <i>FLOOR</i> UNTIL 2026-09-01.</b> Giving
    /// the block's corner an owner drops a block from ten Lots to eight
    /// (<see cref="Borough.Core.Entities.LotSubdivider.CornerTiles"/>), which moved the reading at
    /// 1,000 Citizens from 4 to <b>2</b> and sent somebody to sweep the whole range. The count is
    /// <b>NOT MONOTONIC</b>: 200 → 2, <b>300 → 3, 400 → 4, 500 → 3, 600 → 3, 800 → 3</b>, 1,000 → 2,
    /// and 2 at every size above (re-swept at <c>plans/0053</c>, which moved the band's lower edge
    /// from 500 to 300 by changing how many Buildings a population takes). ***A city too small to have a ragged edge reads the same as a city
    /// that has filled its own ground***, so a single threshold could never have described this and
    /// the old name asserted one. What the extra maxima need is a lattice large enough to be ragged
    /// and too small to be full, which is a window rather than a minimum.
    /// </para>
    /// </remarks>
    [Fact]
    public void Inside_the_ragged_band_the_two_lattices_are_not_yet_two_plateaus()
    {
        int plateaus = Plateaus(Populated("twinned.toml", RaggedBandCitizens));

        Assert.True(
            plateaus > 2,
            $"twinned.toml at {RaggedBandCitizens} Citizens now has {plateaus} plateaus where it "
            + "measured 3. The band has moved -- re-sweep it and update this and twinned.toml's "
            + "header rather than deleting this.");
    }

    /// <summary>
    /// 🔴 <b>The two concentrations are separated by ZERO</b>, so each one's prominence is its whole
    /// height and no threshold a sane person would choose can merge them.
    /// </summary>
    /// <remarks>
    /// <b>This is what <i>make the gap unambiguous</i> means, stated as a property of the field rather
    /// than of the Ruleset.</b> Prominence is a concentration's height above the saddle joining it to
    /// a higher one; a saddle at zero makes it the full height. ***The prominence threshold cannot be
    /// calibrated against this world, which is the whole reason the gap was authored wide.***
    /// </remarks>
    [Fact]
    public void The_gap_between_the_two_concentrations_is_zero()
    {
        World world = Populated("twinned.toml", 16_000);

        (int east, int north, int eastEnd, int northEnd) = Built(world);

        // A row crossing both concentrations: the one with Buildings at each end of the built box.
        int crossing = -1;

        for (int n = north; n <= northEnd && crossing < 0; n++)
        {
            if (Density(world, east, n) > 0 && Density(world, eastEnd, n) > 0)
            {
                crossing = n;
            }
        }

        Assert.True(
            crossing >= 0,
            "no row of Cells has Buildings at both ends of the built area, so this world does not "
            + "have two concentrations side by side and nothing below can be read.");

        // The saddle along that row: the lowest value anything travelling between the two ends must
        // descend to. Prominence is height above the saddle, so a saddle of zero makes each
        // concentration's prominence its whole height.
        int saddle = int.MaxValue;
        int empty = 0;

        for (int e = east + 1; e < eastEnd; e++)
        {
            int density = Density(world, e, crossing);

            saddle = density < saddle ? density : saddle;
            empty += density == 0 ? 1 : 0;
        }

        Assert.True(
            empty > 20,
            $"only {empty} Cells between the two concentrations hold nothing. Either the lattices "
            + "have grown into each other or the corridor is carrying Buildings, and in both cases "
            + "the gap this world exists to have is gone.");

        Assert.Equal(0, saddle);
    }
}
