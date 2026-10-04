using System.Collections.Generic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each sales yard from the Blender modules in <c>assets/sales-yard</c>, which
/// <c>scripts/art/sales-yard.py</c> authors. The simulation supplies the shed, the yard beside it and
/// the stall band along the street. A seeded pick per Building chooses the family, which decides the
/// shed and what fills the yard. No module is stretched: bays and yard pieces are one Tile along the street.
/// </summary>
public partial class Main
{
    private enum YardFamily { Showroom, GardenCentre, BuildersMerchant }

    private static readonly string[] YardModules =
    [
        "showroom-bay", "showroom-door", "car-lot", "totem",
        "glasshouse-bay", "glasshouse-door", "plant-bench", "tree-pot",
        "warehouse-bay", "warehouse-door", "timber-rack", "brick-pallets",
    ];

    // Each family's four modules, in YardModules order: bay, door bay, the piece filling the yard,
    // and the one feature at the yard's street corner.
    private const int YardBay = 0;
    private const int YardDoor = 1;
    private const int YardCover = 2;
    private const int YardFeature = 3;

    private const int YardSurface = 12;
    private const int YardStall = 13;

    private readonly InstanceLayer[] _salesLayers = new InstanceLayer[YardModules.Length + 2];
    private readonly List<ulong>[] _salesIds = new List<ulong>[YardModules.Length + 2];

    private readonly record struct Yard(ulong Id, int Lot, YardFamily Family);

    private void CreateSalesYardLayers()
    {
        for (int i = 0; i < YardModules.Length; i++)
        {
            _salesLayers[i] = Layer(Colors.White, Commit(Load($"sales-yard/{YardModules[i]}").Corners), perInstance: true);
        }

        _salesLayers[YardSurface] = Layer(Colors.White, Module("parking-surface"), perInstance: true, casts: false);
        _salesLayers[YardStall] = Layer(Colors.White, Module("stall"), perInstance: true, casts: false);

        for (int i = 0; i < _salesIds.Length; i++)
        {
            _salesIds[i] = [];
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> SalesYardLayers()
    {
        for (int i = 0; i < YardModules.Length; i++)
        {
            yield return ($"sales-{YardModules[i]}", _salesLayers[i], true, _salesIds[i]);
        }

        yield return ("sales-surface", _salesLayers[YardSurface], true, _salesIds[YardSurface]);
        yield return ("sales-stall", _salesLayers[YardStall], true, _salesIds[YardStall]);
    }

    private void FillSalesYards()
    {
        var placed = new List<(ulong, Transform3D, Color)>[_salesLayers.Length];

        for (int i = 0; i < placed.Length; i++)
        {
            placed[i] = [];
        }

        foreach (Yard yard in SalesYards())
        {
            PlaceSalesYard(yard, placed);
        }

        for (int i = 0; i < _salesLayers.Length; i++)
        {
            Fill(_salesLayers[i], placed[i], _salesIds[i]);
        }
    }

    private IEnumerable<Yard> SalesYards()
    {
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!_world.IsSalesYard(slot))
            {
                continue;
            }

            ulong id = rows.IdAt(slot);
            ulong draw = Randomness.Draw(_world.Key, id, Ticks.Zero, PurposeTag.AppearanceFamily);

            yield return new(id, _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]), (YardFamily)(int)(draw % 3));
        }
    }

    private void PlaceSalesYard(Yard yard, List<(ulong, Transform3D, Color)>[] placed)
    {
        LotTable lots = _world.Lots;
        (Parcel parcel, BlockGround ground) = TradeGround(yard.Lot);
        int half = _world.Rules.Lots.StreetHalfWidthTiles;
        int shedEast = FootprintFrame(yard.Lot, trade: true).X;
        int shedNorth = FootprintFrame(yard.Lot, trade: true).Y;
        int shedWide = FootprintFrame(yard.Lot, trade: true).Width;
        int shedDeep = FootprintFrame(yard.Lot, trade: true).Height;
        bool south = parcel.Face == BlockFace.South;
        Basis facing = south ? FacingSouth : FacingNorth;
        int family = 4 * (int)yard.Family;

        void Put(int module, float east, float north) =>
            placed[module].Add((yard.Id, OnTradeLot(yard.Lot, new Transform3D(facing, At(east, 0f, north))), Colors.White));

        for (int x = 0; x < shedWide; x++)
        {
            Put(family + (x == shedWide / 2 ? YardDoor : YardBay), shedEast + x + .5f, shedNorth + (shedDeep * .5f));
        }

        (int east, int north, int wide, int deep) = SalesYard.Yard(parcel, ground, half, shedEast, shedWide);
        int cornerX = shedEast > east ? 0 : wide - 1;
        int cornerY = south ? 0 : deep - 1;

        for (int x = 0; x < wide; x++)
        {
            for (int y = 0; y < deep; y++)
            {
                bool corner = x == cornerX && y == cornerY;
                bool aisle = (south ? y : deep - 1 - y) % 2 == 1;

                if (corner || !aisle)
                {
                    Put(family + (corner ? YardFeature : YardCover), east + x + .5f, north + y + .5f);
                }
                else
                {
                    Put(YardSurface, east + x + .5f, north + y + .5f);
                }
            }
        }

        (int bandEast, int bandNorth, int along, int toward) = SalesYard.CarPark(parcel, ground, half);
        PlaceStallBand(yard.Id, yard.Lot, bandEast, bandNorth, along, toward, placed[YardSurface], placed[YardStall]);
    }
}
