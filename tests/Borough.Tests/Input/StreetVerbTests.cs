using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;

namespace Borough.Tests.Input;

/// <summary>
/// The <c>street</c> verb: a freeform lay that clears the Lots under its paved width at the
/// <c>demolish</c> price, all or nothing.
/// </summary>
public sealed class StreetVerbTests
{
    private const long PricePerTile = 1_000;

    private static (World World, Simulation Simulation) Priced(long treasury)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"))
            .Replace("[lots]\n", $"[lots]\ndemolition_price_per_tile = {PricePerTile}\n", StringComparison.Ordinal);
        RulesetLoadResult loaded = RulesetLoader.Parse(toml, "minimal.toml");
        Assert.Empty(loaded.Refusals);

        var key = WorldKey.FromSeed(0x57_4EE7UL);
        var world = new World(1_000, loaded.Ruleset!, key);
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        world.EndowTreasury(new Money(treasury));
        return (world, simulation);
    }

    /// <summary>
    /// A Street from the middle of an occupied Building's frontage, fourteen Tiles straight into
    /// its block, and that Building.
    /// </summary>
    internal static (Command Street, int Building) Through(World world)
    {
        for (int building = 0; building < world.Buildings.Rows.SlotCount; building++)
        {
            if (!world.Buildings.Rows.IsLive(building) || world.Occupants.IsEmpty(building)) { continue; }
            int lot = world.Lots.Rows.Resolve(world.Buildings.Lot[building]);
            int segment = world.Lots.FrontageOn(lot);
            if (segment < 0) { continue; }

            StreetArc line = world.Roads.Segments.Centerline[segment];
            OrientedRectangle parcel = world.Lots.Parcel(lot);
            int along = line.OffsetAlong((long)parcel.Center.East, parcel.Center.North);
            if (!line.IsStraight || along < 16 * Fixed.One || along > line.Length - (16 * Fixed.One)) { continue; }

            var at = line.PointAt(along);
            var tangent = line.TangentAt(along);
            long east = IntegerMath.RoundDiv(at.East, Fixed.One), north = IntegerMath.RoundDiv(at.North, Fixed.One);
            long side = ((parcel.Center.East - at.East) * -tangent.North) + ((parcel.Center.North - at.North) * tangent.East);
            int sign = side > 0 ? 1 : -1;
            long endEast = east + (sign * IntegerMath.RoundDiv(-14L * tangent.North, Fixed.One));
            long endNorth = north + (sign * IntegerMath.RoundDiv(14L * tangent.East, Fixed.One));

            return (Command.Street(new Tiles((int)east), new Tiles((int)north), new Tiles((int)endEast),
                new Tiles((int)endNorth), SubTiles.Zero), building);
        }

        throw new InvalidOperationException("no occupied Building fronts the middle of a straight Street.");
    }

    private static long PriceOf(World world, Command street)
    {
        StreetArc.TryCreate(street.East.Raw, street.North.Raw, street.EndEast.Raw, street.EndNorth.Raw, street.Sagitta.Raw, out StreetArc line);
        long price = 0;
        foreach (int lot in world.LotsUnder(line))
        {
            int building = world.Lots.BuildingOn(lot);
            if (building >= 0) { price += world.DemolitionPrice(building).Raw; }
        }

        return price;
    }

    [Fact]
    public void A_street_through_an_occupied_building_pays_the_displaced_and_clears_its_lot()
    {
        (World world, Simulation simulation) = Priced(treasury: 100_000_000);
        (Command street, int building) = Through(world);
        var standing = world.Buildings.Rows.At(building);
        long price = PriceOf(world, street);
        int segments = world.Roads.Segments.Rows.LiveCount;
        Assert.True(price > 0);

        Assert.Equal(Refusal.None, simulation.Refuses(street));
        simulation.DrainTreasuryFlows();
        simulation.Step(new TickInput([street], 0));

        Assert.Equal(0, simulation.CommandsRefused);
        Assert.False(world.Buildings.Rows.TryResolve(standing, out _));
        Assert.Equal(segments + 2, world.Roads.Segments.Rows.LiveCount);
        Assert.Equal(price, simulation.DrainTreasuryFlows().Compensation);

        StreetArc.TryCreate(street.East.Raw, street.North.Raw, street.EndEast.Raw, street.EndNorth.Raw, 0, out StreetArc line);
        foreach (int lot in world.LotsUnder(line))
        {
            Assert.Fail($"Lot {lot} still stands under the new Street.");
        }
    }

    [Fact]
    public void The_preview_names_the_price_and_buildings_the_lay_then_clears()
    {
        (World world, Simulation simulation) = Priced(treasury: 100_000_000);
        (Command street, int building) = Through(world);
        var standing = world.Buildings.Rows.At(building);

        StreetPreview preview = simulation.PreviewStreet(street);

        Assert.Equal(Refusal.None, preview.Refusal);
        Assert.Equal(PriceOf(world, street), preview.Price.Raw);
        Assert.Contains(building, preview.Buildings);

        simulation.DrainTreasuryFlows();
        simulation.Step(new TickInput([street], 0));

        Assert.Equal(preview.Price.Raw, simulation.DrainTreasuryFlows().Compensation);
        Assert.False(world.Buildings.Rows.TryResolve(standing, out _));
    }

    [Fact]
    public void Previewing_a_street_writes_nothing()
    {
        (World world, Simulation simulation) = Priced(treasury: 0);
        (Command street, int _) = Through(world);
        ulong before = world.HashState();

        StreetPreview shortOfMoney = simulation.PreviewStreet(street);
        StreetPreview tooShort = simulation.PreviewStreet(
            Command.Street(street.East, street.North, street.East, street.North + new Tiles(1), SubTiles.Zero));

        Assert.Equal(Refusal.StreetTreasuryCannotPay, shortOfMoney.Refusal);
        Assert.True(shortOfMoney.Price.Raw > 0);
        Assert.NotEqual(Refusal.None, tooShort.Refusal);
        Assert.Equal(0, tooShort.Price.Raw);
        Assert.Empty(tooShort.Buildings);
        Assert.Throws<ArgumentException>(() => simulation.PreviewStreet(Command.Gate(street.East, street.North, 0)));
        Assert.Equal(before, world.HashState());
    }

    [Fact]
    public void A_treasury_short_of_the_total_refuses_and_clears_nothing()
    {
        (World world, Simulation simulation) = Priced(treasury: 0);
        (Command street, int building) = Through(world);
        var standing = world.Buildings.Rows.At(building);
        int segments = world.Roads.Segments.Rows.LiveCount;

        Assert.Equal(Refusal.StreetTreasuryCannotPay, simulation.Refuses(street));
        simulation.Step(new TickInput([street], 0));

        Assert.Equal(1, simulation.CommandsRefused);
        Assert.True(world.Buildings.Rows.TryResolve(standing, out _));
        Assert.Equal(segments, world.Roads.Segments.Rows.LiveCount);
    }

    [Fact]
    public void A_street_line_survives_the_log_and_replays_to_the_same_city()
    {
        (World world, Simulation simulation) = Priced(treasury: 100_000_000);
        (Command street, int _) = Through(world);
        Command bent = Command.Street(street.East, street.North, street.EndEast, street.EndNorth, new SubTiles(-3 * Fixed.One));

        InputLog log = InputLogCodec.FromText(InputLogCodec.ToText(
            new InputLogBuilder(1, new WorldConfiguration(1_000), rulesetHash: 0).Append(Ticks.Zero, bent).Build()));
        Assert.Equal(1, log.At(Ticks.Zero).Length);
        Command read = log.At(Ticks.Zero)[0];

        Assert.Equal(CommandKind.Street, read.Kind);
        Assert.Equal((bent.East, bent.North, bent.EndEast, bent.EndNorth, bent.Sagitta),
            (read.East, read.North, read.EndEast, read.EndNorth, read.Sagitta));

        (World replayed, Simulation replay) = Priced(treasury: 100_000_000);
        simulation.Step(new TickInput([bent], 0));
        replay.Step(new TickInput(log.At(Ticks.Zero), 0));
        Assert.Equal(world.HashState(), replayed.HashState());
    }
}
