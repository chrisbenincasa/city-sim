using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;
using Borough.Tests.Persistence;

namespace Borough.Tests.Entities;

/// <summary>
/// Land ownership slice 1: who holds a Lot (<c>L1</c>, <c>L2</c>, <c>L9</c> dissolution, <c>L14</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>The column is a severable handle, so the city holds a Lot in two different ways and both have
/// to read alike.</b> An unset handle is a Lot nobody has ever bought; a stale one is a Lot whose
/// Landowner died or left. <c>LotTable.IsCityHeld</c> and <c>LotEvidence.Landowner</c> are what make
/// the two indistinguishable to a reader, which is what <c>L9</c>'s <em>the Lot passes to the city
/// with the estate</em> asks for.
/// </para>
/// <para>
/// ⚠ <b>Nothing in this slice assigns a Landowner but these tests.</b> Purchase arrives in slice 2,
/// so a world built today holds every Lot in the city (<c>L3</c>, <c>L14</c>) and the owned cases are
/// reached by writing the column directly.
/// </para>
/// </remarks>
public sealed class LandownerTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(17);

    private const ulong InForce = 0x0BAD_F00D_0BAD_F00DUL;

    private static Ruleset Shipped(string file)
    {
        RulesetLoadResult result =
            RulesetLoader.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", file));

        return result.Ruleset
            ?? throw new InvalidOperationException(
                $"the shipped Ruleset {file} was refused, so this test cannot run:\n{result.Describe()}");
    }

    /// <summary>One Lot with a Building on it and one Household in it. A founding city.</summary>
    private static World Founded(out Handle<Lot> lot, out Handle<Household> household)
    {
        var world = new World(1_000, TestRulesets.MoneyOnly);

        lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(world.Lots, lot, kind: 1);
        household = world.CreateHousehold(building, lifeStage: 1);

        world.CreateCitizen(household);

        return world;
    }

    /// <summary>The Landowner the Evidence names, which is the figure a panel reads.</summary>
    private static Handle<Household> Named(World world, Handle<Lot> lot) =>
        Core.Evidence.Evidence.OfLot(world, lot).Landowner;

    /// <summary>A Lot carved or created today is held by the city, with no write to say so.</summary>
    /// <remarks>
    /// <c>L3</c>: the city holds a newly carved Lot until a Household buys it. The empty handle is
    /// what a fresh row zeroes to, so no creation site has to remember this.
    /// </remarks>
    [Fact]
    public void A_created_lot_is_held_by_the_city()
    {
        World world = Founded(out Handle<Lot> lot, out _);
        int slot = world.Lots.Rows.Resolve(lot);

        Assert.True(world.Lots.IsCityHeld(slot));
        Assert.Equal(Rows.NoSlot, world.Lots.LandownerOn(slot));
        Assert.True(Named(world, lot).IsNone);
    }

    /// <summary>Every Lot world creation builds is held by the city.</summary>
    /// <remarks>
    /// <c>L14</c>, and the carve path with it: <c>SyntheticCity</c> subdivides blocks into Lots and
    /// raises Buildings on them, so a generated city is the only fixture that exercises every Lot
    /// creation site at once. They sell through <c>L9</c>'s step in slice 5.
    /// </remarks>
    [Fact]
    public void Every_lot_a_generated_city_builds_is_held_by_the_city()
    {
        var world = new World(1_000, Shipped("minimal.toml"));

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int live = 0;

        for (int slot = 0; slot < world.Lots.Rows.SlotCount; slot++)
        {
            if (!world.Lots.Rows.IsLive(slot))
            {
                continue;
            }

            live++;

            Assert.True(
                world.Lots.IsCityHeld(slot),
                $"Lot {world.Lots.Rows.IdAt(slot)} came out of world creation already owned.");
        }

        Assert.True(live > 0, "the generated city carved no Lots, so this asserts nothing.");
    }

    /// <summary>A Lot with a Landowner names it, off the column and out of the Evidence alike.</summary>
    [Fact]
    public void An_owned_lot_names_its_landowner()
    {
        World world = Founded(out Handle<Lot> lot, out Handle<Household> household);
        int slot = world.Lots.Rows.Resolve(lot);

        world.Lots.Landowner[slot] = household;

        Assert.False(world.Lots.IsCityHeld(slot));
        Assert.Equal(world.Households.Rows.Resolve(household), world.Lots.LandownerOn(slot));
        Assert.Equal(household, Named(world, lot));
    }

    /// <summary>
    /// A dissolved Landowner's Lot reads as city-held, and <c>World.Dissolve</c> writes nothing to say
    /// so.
    /// </summary>
    /// <remarks>
    /// <c>L9</c>: on dissolution the Lot passes to the city with the estate. The severable handle is
    /// the mechanism — freeing the Household row bumps its generation, so the stored handle stops
    /// resolving — and the column is left alone deliberately: clearing it would move the State Hash
    /// on every death for a reason that has nothing to do with the Lot.
    /// </remarks>
    [Fact]
    public void A_dissolved_landowners_lot_reads_as_city_held()
    {
        World world = Founded(out Handle<Lot> lot, out Handle<Household> household);
        int slot = world.Lots.Rows.Resolve(lot);

        world.Lots.Landowner[slot] = household;

        world.Dissolve(household, world.Tick);

        Assert.True(world.Lots.IsCityHeld(slot));
        Assert.True(Named(world, lot).IsNone);
        Assert.False(
            world.Lots.Landowner[slot].IsNone,
            "the handle itself was cleared, so the Lot reads as city-held through a write rather "
            + "than through the severance.");
    }

    /// <summary>A departed Landowner's Lot reads as city-held too.</summary>
    /// <remarks>
    /// <c>L9</c>'s departure half, as far as this slice goes. Slice 5 replaces the unpaid reversion
    /// with a sale at the <c>L3</c> price.
    /// </remarks>
    [Fact]
    public void A_departed_landowners_lot_reads_as_city_held()
    {
        World world = Founded(out Handle<Lot> lot, out Handle<Household> household);
        int slot = world.Lots.Rows.Resolve(lot);

        world.Lots.Landowner[slot] = household;

        // Departure is the Unplaced Pool's sink, so the Household has to be in the Pool to use it.
        world.Unplace(household);
        world.Depart(household);

        Assert.False(world.Households.Rows.IsValid(household));
        Assert.True(world.Lots.IsCityHeld(slot));
        Assert.True(Named(world, lot).IsNone);
    }

    /// <summary>
    /// The Household slot a dissolution freed does not carry the Lot to whoever lands in it next.
    /// </summary>
    /// <remarks>
    /// <b>The generation is what separates them</b>, and a saved slot index would not have. The next
    /// Household allocated takes the freed slot, so a column holding a bare index would read as this
    /// Lot's Landowner the moment somebody moved in.
    /// </remarks>
    [Fact]
    public void A_recycled_household_slot_does_not_inherit_the_lot()
    {
        World world = Founded(out Handle<Lot> lot, out Handle<Household> household);
        int slot = world.Lots.Rows.Resolve(lot);
        int freed = world.Households.Rows.Resolve(household);

        world.Lots.Landowner[slot] = household;
        world.Dissolve(household, world.Tick);

        Handle<Building> building = world.Buildings.Rows.At(world.Lots.BuildingOn(slot));
        Handle<Household> next = world.CreateHousehold(building, lifeStage: 1);

        Assert.Equal(freed, world.Households.Rows.Resolve(next));
        Assert.True(world.Lots.IsCityHeld(slot));
        Assert.True(Named(world, lot).IsNone);
    }

    /// <summary>A Landowner comes back from a save, and the world comes back at its hash.</summary>
    [Fact]
    public void A_landowner_survives_a_save_and_reload()
    {
        World world = Founded(out Handle<Lot> lot, out Handle<Household> household);
        int slot = world.Lots.Rows.Resolve(lot);

        world.Lots.Landowner[slot] = household;

        ulong owner = world.Households.Rows.IdAt(world.Households.Rows.Resolve(household));
        ulong hash = world.HashState();

        var file = new MemorySave();
        SaveFile.Write(world, InForce, file);

        World loaded = SaveFile.Read(file, TestRulesets.MoneyOnly, out _);

        Assert.Equal(hash, loaded.HashState());

        int reloaded = loaded.Lots.LandownerOn(slot);

        Assert.NotEqual(Rows.NoSlot, reloaded);
        Assert.Equal(owner, loaded.Households.Rows.IdAt(reloaded));
        Assert.Equal(loaded.Households.Rows.At(reloaded), Named(loaded, loaded.Lots.Rows.At(slot)));
    }
}
