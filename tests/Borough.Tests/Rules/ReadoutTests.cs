using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Slice 7 task 7: the Readout set, and the seam between the set and its spelling.
/// </summary>
/// <remarks>
/// <b>The drift test is the point of this file.</b> <c>adr/0048</c> refuses a second validator, so
/// nothing at runtime checks that <c>Borough.Core.Rules.Readout</c> and
/// <c>Borough.Formats.ReadoutNames</c> describe the same set. That check is here instead, and without
/// it the failure mode is silent in the worse direction: a declared Readout no Ruleset can name reads
/// as *the feature does not work* with nothing to grep for.
/// </remarks>
public sealed class ReadoutTests
{
    private static Ruleset Empty() => new(
        resources: [ResourceFamily.Good],
        rules: [],
        kinds: [new KindDefinition(0, 0, 0, 0)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: []);

    private static IEnumerable<Readout> Members =>
        Enum.GetValues<Readout>().Where(readout => readout != Readout.None);

    // ---- the set and its spelling agree -------------------------------------------------------------

    [Fact]
    public void Every_declared_readout_has_a_name_a_ruleset_can_write()
    {
        foreach (Readout readout in Members)
        {
            string name = ReadoutNames.NameOf(readout);

            Assert.True(
                ReadoutNames.TryResolve(name, out ReadoutId id),
                $"'{name}' is the declared name of {readout} and does not resolve.");

            Assert.Equal((ushort)readout, id.Raw);
        }
    }

    [Fact]
    public void Every_name_resolves_to_a_readout_the_simulation_declares()
    {
        foreach (string name in ReadoutNames.Declared.Split(", "))
        {
            Assert.True(ReadoutNames.TryResolve(name, out ReadoutId id));
            Assert.True(
                Readouts.IsDeclared(id),
                $"'{name}' resolves to id {id.Raw}, which Borough.Core does not declare.");
        }
    }

    /// <summary>
    /// <see cref="Readouts.Declared"/> is what a shell enumerates to build an inspector, so listing
    /// <see cref="Readout.None"/> would offer the player a row that reads nothing.
    /// </summary>
    [Fact]
    public void The_declared_set_is_every_member_except_none()
    {
        Assert.Equal(Members.ToArray(), Readouts.Declared.ToArray());
        Assert.False(Readouts.IsDeclared(ReadoutId.None));
    }

    // ---- reading ------------------------------------------------------------------------------------

    [Fact]
    public void Occupancy_counts_the_households_in_the_building()
    {
        var world = new World(1_000, LayerRuleset.Default, Empty());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(lot, kind: 1);
        int slot = world.Buildings.Rows.Resolve(building);

        var occupancy = new ReadoutId((ushort)Readout.Occupancy);

        Assert.Equal(0, Readouts.Read(world, slot, occupancy));

        world.CreateHousehold(building, lifeStage: 1);
        world.CreateHousehold(building, lifeStage: 1);

        Assert.Equal(2, Readouts.Read(world, slot, occupancy));
    }

    /// <summary>
    /// <b>An undeclared id throws rather than reading zero</b>, and the reason is sharper for a Readout
    /// than for the usual named hole: the value is an apply count, so a silent zero is a Rule that
    /// *succeeds* doing nothing, re-arms for ever, and produces no failure and no Evidence.
    /// </summary>
    [Fact]
    public void An_undeclared_id_throws_rather_than_reading_zero()
    {
        var world = new World(1_000, LayerRuleset.Default, Empty());

        Handle<Lot> lot = world.Lots.Create(new Tiles(1), new Tiles(2), zone: 1);
        Handle<Building> building = world.Buildings.Create(lot, kind: 1);
        int slot = world.Buildings.Rows.Resolve(building);

        Assert.Throws<InvalidOperationException>(
            () => Readouts.Read(world, slot, new ReadoutId(9999)));

        Assert.Throws<InvalidOperationException>(
            () => Readouts.Read(world, slot, ReadoutId.None));
    }
}
