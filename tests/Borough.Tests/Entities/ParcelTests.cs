using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Entities;

/// <summary>
/// The ground under a Building — <c>plans/0052</c> stage 1.
/// </summary>
/// <remarks>
/// <para>
/// <b>Before this, the ground under a Building was invented independently in six places</b> — five in
/// the shell and one in the core — and two of those inventions landed on the same patch, which is
/// <c>plans/0049</c> <b>F21</b>. ***A partition of a block cannot overlap; five sizings can.***
/// </para>
/// <para>
/// 🔴 <b>A Lot is still an Address and still owns no ground in the sense <c>adr/0078</c> meant.</b>
/// What that ADR refused is an <b>authored depth key</b>, and there still is not one: the parcel is
/// derived, on the epoch, from the block's saved pattern and the lattice.
/// </para>
/// </remarks>
public sealed class ParcelTests
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

    private static World Populated(string file = "minimal.toml", int citizens = 4_000)
    {
        var world = new World(citizens, Shipped(file));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        return world;
    }

    private static (int East, int North, int Wide, int Deep) ParcelOf(World world, int slot) =>
        (world.Lots.ParcelEast[slot].Raw, world.Lots.ParcelNorth[slot].Raw,
         world.Lots.ParcelWide[slot].Raw, world.Lots.ParcelDeep[slot].Raw);

    /// <summary>
    /// 🔴 <b><c>RebuildDerived</c> reproduces every parcel exactly</b>, which is what makes it derived.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><c>Rows.Derived</c> ALLOCATES a column; it does not make anything rebuild it.</b> Four
    /// columns declared with nothing populating them would load a world in which every Building
    /// covered no ground and <b>nothing anywhere would fail</b> — which is the trap
    /// <c>DerivedRebuildAuditTests</c> exists for and which it caught on milestone 7's
    /// <c>car_park.segment_next</c>.
    /// </para>
    /// <para>
    /// <b>That audit checks the column is populated; this checks it is populated with the SAME
    /// thing.</b> The two are different questions and only the second would catch a rebuild that
    /// matched Lots to parcels by the wrong key.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_rebuild_reproduces_every_parcel()
    {
        World world = Populated();

        var before = new List<(int, int, int, int)>();

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot))
            {
                before.Add(ParcelOf(world, slot));
            }
        }

        Assert.NotEmpty(before);
        Assert.Contains(before, parcel => parcel.Item3 > 0 && parcel.Item4 > 0);

        world.RebuildDerived();

        var after = new List<(int, int, int, int)>();

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot))
            {
                after.Add(ParcelOf(world, slot));
            }
        }

        Assert.Equal(before, after);
    }

    /// <summary>
    /// <b>Every Lot with an Address has ground, and every Lot without one has none.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0079</c> read across: a Lot whose Street is gone keeps its position and its Building and
    /// loses its Address — and <b>ground with no Address on it is ground this table cannot name</b>, so
    /// it reads zero rather than keeping a stale rectangle.
    /// </remarks>
    [Fact]
    public void A_parcel_exists_exactly_where_an_address_does()
    {
        World world = Populated();

        int fronted = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot))
            {
                continue;
            }

            if (world.Lots.HasFrontage(slot))
            {
                Assert.True(
                    world.Lots.ParcelTiles(slot) > 0,
                    $"Lot {slot} has an Address and no ground.");

                fronted++;
            }
            else
            {
                Assert.Equal(0, world.Lots.ParcelTiles(slot));
            }
        }

        Assert.NotEqual(0, fronted);
    }

    /// <summary>
    /// 🔴 <b>NO TWO PARCELS IN THE CITY OVERLAP.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is <c>plans/0049</c> <b>F21</b> made impossible rather than repaired.</b> Two faces
    /// beside one junction each laid a Lot; both were correct <em>as Addresses</em>; and the shell had
    /// to invent which of them owned the ground. ***Two inventions landed on one patch.***
    /// </para>
    /// <para>
    /// ⚠ <b>It is asserted over the WHOLE CITY and not over one block</b>, which is the stronger
    /// claim: <c>BlockPatternTests</c> holds a pattern to partitioning its own block, and this holds
    /// the blocks to not partitioning each other's.
    /// </para>
    /// </remarks>
    [Fact]
    public void No_two_parcels_in_the_city_overlap()
    {
        World world = Populated();

        var parcels = new List<(int East, int North, int Wide, int Deep)>();

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (world.Lots.Rows.IsLive(slot) && world.Lots.ParcelTiles(slot) > 0)
            {
                parcels.Add(ParcelOf(world, slot));
            }
        }

        Assert.NotEmpty(parcels);

        // Sorted by east edge, so the scan can stop comparing as soon as the east ranges part. The
        // whole city is thousands of parcels and the quadratic form is minutes.
        parcels.Sort((a, b) => a.East.CompareTo(b.East));

        for (int i = 0; i < parcels.Count; i++)
        {
            for (int j = i + 1; j < parcels.Count; j++)
            {
                if (parcels[j].East >= parcels[i].East + parcels[i].Wide)
                {
                    break;
                }

                bool apartNorth =
                    parcels[j].North >= parcels[i].North + parcels[i].Deep
                    || parcels[i].North >= parcels[j].North + parcels[j].Deep;

                Assert.True(
                    apartNorth,
                    $"parcels {parcels[i]} and {parcels[j]} overlap.");
            }
        }
    }

    /// <summary>
    /// 🔴 <b>A Building seals its parcel, and <c>footprint_tiles</c> is gone.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0025</c>'s own line is what decides this</b>: block geometry determining parcel size
    /// <em>"is not a rule at all; it is arithmetic over what the player drew"</em>. ***An authored
    /// constant was standing exactly where the design says arithmetic belongs.***
    /// </para>
    /// <para>
    /// ⚠ <b>The Sealing is compared as a TOTAL rather than per Cell</b>, because a parcel can be
    /// bigger than a Cell and <c>MapLayers.SealGround</c> spreads it over the Cells it covers. See
    /// <c>SealingMeasurementTests</c> for what that is worth: at <c>severance.toml</c>'s
    /// <c>block_tiles = 256</c> the mean parcel is five Cells' worth of ground.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_buildings_footprint_is_its_parcel()
    {
        World world = Populated();

        long footprintTiles = 0;
        long parcelTiles = 0;

        for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
        {
            if (world.Buildings.Rows.IsLive(slot)
                && world.Lots.Rows.TryResolve(world.Buildings.Lot[slot], out int lotSlot))
            {
                int footprint = world.Lots.FootprintTiles(lotSlot);
                int parcel = world.Lots.ParcelTiles(lotSlot);

                footprintTiles += footprint < 1 ? 1 : footprint;
                parcelTiles += parcel < 1 ? 1 : parcel;
            }
        }

        Assert.True(footprintTiles > 0, "no Building stands on ground, so this measures nothing.");

        long sealed_ = 0;

        for (int slot = 0; slot < world.Layers.Cells.Rows.SlotCount; slot++)
        {
            if (world.Layers.Cells.Rows.IsLive(slot))
            {
                sealed_ += world.Layers.Cells.Sealing[slot];
            }
        }

        // Roads seal too, so the Buildings' share is a floor rather than the total. What this refuses
        // is the state before stage 1, where the Buildings' share was a per-kind constant and this
        // sum would have been 481 Tiles against tens of thousands of footprint.
        Assert.True(
            sealed_ >= footprintTiles,
            $"the city seals {sealed_} Tiles and its Buildings' footprints are {footprintTiles}.");

        // 🔴 AND THE GARDEN IS NOT SEALED, WHICH IS THE HALF THAT WAS WRONG UNTIL 2026-09-02. This
        // asserted `sealed_ >= parcelTiles` -- the whole holding, hedge to hedge -- while the shell
        // drew a wall on about half of it, so ***the simulation and the picture disagreed about the
        // same rectangle and only the picture was right.*** A garden is ground the city has not
        // spent. The comparison is a floor and not an equality because roads seal too, so this is
        // the weaker half; what it refuses is the footprint quietly becoming the parcel again.
        Assert.True(
            footprintTiles < parcelTiles,
            $"the Buildings cover {footprintTiles} Tiles of {parcelTiles} they hold, so nothing has "
            + "a garden and [lots] setback_tiles is doing nothing.");
    }

    /// <summary>
    /// <b>A block's pattern decides its parcels' shape</b>, which is the whole of why the parcel is
    /// derived from the pattern rather than from the Lot.
    /// </summary>
    /// <remarks>
    /// <b>Back-to-back plots meet along the centre line</b>, so their parcels are half the block deep
    /// where a detached block's are one strip deep. ⚠ <b>The depths are compared rather than
    /// asserted</b> — both derive from <c>block_tiles</c> and <c>lots_per_segment</c>, and a figure
    /// here would pin a derived quantity.
    /// </remarks>
    [Fact]
    public void A_terrace_holds_deeper_ground_than_a_detached_block()
    {
        int block = 32;
        int perSegment = 5;

        var detached = new Parcel[BlockPatterns.Ceiling(perSegment)];
        var terrace = new Parcel[BlockPatterns.Ceiling(perSegment)];

        BlockGround ground = BlockGround.At(BlockLattice.Even(block), 2, 2);

        int detachedCount = BlockPatterns.Carve(
            Borough.Core.Determinism.WorldKey.FromSeed(0),
            BlockPattern.Detached, ground, perSegment, detached);
        int terraceCount = BlockPatterns.Carve(
            Borough.Core.Determinism.WorldKey.FromSeed(0),
            BlockPattern.BackToBack, ground, perSegment, terrace);

        int deepestDetached = detached[..detachedCount]
            .Where(parcel => parcel.Face is BlockFace.South or BlockFace.North)
            .Max(parcel => parcel.Deep.Raw);

        int shallowestTerrace = terrace[..terraceCount].Min(parcel => parcel.Deep.Raw);

        Assert.True(
            shallowestTerrace > deepestDetached,
            $"a terrace's shallowest parcel is {shallowestTerrace} Tiles deep and a detached block's "
            + $"deepest is {deepestDetached}, so the pattern is not reaching the ground.");
    }
}
