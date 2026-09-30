using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each town supermarket from the Blender modules in <c>assets/supermarket</c>, which
/// <c>scripts/art/supermarket.py</c> authors. The simulation supplies every position and count, and
/// no module is stretched: the store is one bay a Tile, and the car park one surface or slab a Tile.
/// </summary>
public partial class Main
{
    private const float DeckLevelMetres = 3f;

    private InstanceLayer _storeBays = null!;
    private InstanceLayer _storeEntrances = null!;
    private InstanceLayer _parkingSurfaces = null!;
    private InstanceLayer _deckSlabs = null!;
    private InstanceLayer _deckEdges = null!;
    private InstanceLayer _supermarketStalls = null!;
    private readonly List<ulong> _storeBayIds = [];
    private readonly List<ulong> _storeEntranceIds = [];
    private readonly List<ulong> _parkingSurfaceIds = [];
    private readonly List<ulong> _deckSlabIds = [];
    private readonly List<ulong> _deckEdgeIds = [];
    private readonly List<ulong> _supermarketStallIds = [];

    /// <summary>Which Buildings the Blender modules draw, so the massing pass leaves them out.</summary>
    private readonly HashSet<ulong> _moduleDrawnIds = [];

    /// <summary>A Building whose car park this file draws. A precinct's shops are drawn elsewhere.</summary>
    private readonly record struct Supermarket(ulong Id, int Lot, int Levels, bool Store);

    private void CreateSupermarketLayers()
    {
        _storeBays = Layer(Colors.White, Module("store-bay"), perInstance: true);
        _storeEntrances = Layer(Colors.White, Module("store-entrance"), perInstance: true);
        _parkingSurfaces = Layer(Colors.White, Module("parking-surface"), perInstance: true, casts: false);
        _deckSlabs = Layer(Colors.White, Module("deck-slab"), perInstance: true);
        _deckEdges = Layer(Colors.White, Module("deck-edge"), perInstance: true);
        _supermarketStalls = Layer(Colors.White, Module("stall"), perInstance: true, casts: false);
    }

    private static ArrayMesh Module(string name) => Commit(Load($"supermarket/{name}").Corners);

    private void FillSupermarkets()
    {
        Fill(_storeBays, White(StoreBays(entrance: false)), _storeBayIds);
        Fill(_storeEntrances, White(StoreBays(entrance: true)), _storeEntranceIds);
        Fill(_parkingSurfaces, White(ParkingTiles(raised: false)), _parkingSurfaceIds);
        Fill(_deckSlabs, White(ParkingTiles(raised: true)), _deckSlabIds);
        Fill(_deckEdges, White(DeckEdges()), _deckEdgeIds);
        Fill(_supermarketStalls, White(SupermarketStalls()), _supermarketStallIds);
    }

    private void RefreshModuleDrawnIds()
    {
        _moduleDrawnIds.Clear();

        foreach (Supermarket each in Supermarkets())
        {
            _moduleDrawnIds.Add(each.Id);
        }

        foreach (Store each in DepartmentStores())
        {
            _moduleDrawnIds.Add(each.Id);
        }

        foreach (Pad each in PadSites())
        {
            _moduleDrawnIds.Add(each.Id);
        }

        foreach (Yard each in SalesYards())
        {
            _moduleDrawnIds.Add(each.Id);
        }
    }

    private static IEnumerable<(ulong, Transform3D, Color)> White(IEnumerable<(ulong Id, Transform3D Where)> places)
    {
        foreach ((ulong id, Transform3D where) in places)
        {
            yield return (id, where, Colors.White);
        }
    }

    private IEnumerable<Supermarket> Supermarkets()
    {
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (_world.IsSupermarket(slot))
            {
                int lot = _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]);
                yield return new(rows.IdAt(slot), lot, TownSupermarket.Levels(_world.Lots.PatternOf(lot)), Store: true);
            }
            else if (_world.IsPrecinct(slot))
            {
                int lot = _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]);
                yield return new(rows.IdAt(slot), lot, TownSupermarket.DeckLevels, Store: false);
            }
        }
    }

    /// <summary>One bay a Tile along the store's front, with the entrance at the middle bay.</summary>
    private IEnumerable<(ulong Id, Transform3D Where)> StoreBays(bool entrance)
    {
        LotTable lots = _world.Lots;

        foreach (Supermarket each in Supermarkets())
        {
            if (!each.Store)
            {
                continue;
            }

            int wide = lots.FootprintWide[each.Lot].Raw;
            int middle = wide / 2;
            float east = lots.FootprintEast[each.Lot].Raw * MetresPerTile;
            float north = lots.FootprintNorth[each.Lot].Raw * MetresPerTile;
            float deep = lots.FootprintDeep[each.Lot].Raw;
            float centre = north + (deep * MetresPerTile * .5f);

            for (int bay = 0; bay < wide; bay++)
            {
                if ((bay == middle) == entrance)
                {
                    yield return (each.Id, new Transform3D(
                        Basis.Identity, new Vector3(east + ((bay + .5f) * MetresPerTile), 0f, -centre)));
                }
            }
        }
    }

    private (int East, int North, int Along, int Toward) SupermarketCarPark(int lot)
    {
        LotTable lots = _world.Lots;

        return TownSupermarket.CarPark(
            lots.ParcelNorth[lot].Raw, lots.ParcelDeep[lot].Raw, lots.FootprintEast[lot].Raw,
            lots.FootprintNorth[lot].Raw, lots.FootprintWide[lot].Raw, lots.FootprintDeep[lot].Raw,
            _world.Rules.Lots.StreetHalfWidthTiles);
    }

    /// <summary>
    /// One module a Tile over the car park: asphalt on the ground, and a slab on each raised deck level.
    /// </summary>
    private IEnumerable<(ulong Id, Transform3D Where)> ParkingTiles(bool raised)
    {
        foreach (Supermarket each in Supermarkets())
        {
            (int east, int north, int along, int toward) = SupermarketCarPark(each.Lot);

            for (int level = raised ? 1 : 0; level < (raised ? each.Levels : 1); level++)
            {
                for (int x = 0; x < along; x++)
                {
                    for (int y = 0; y < toward; y++)
                    {
                        yield return (each.Id, new Transform3D(Basis.Identity, new Vector3(
                            (east + x + .5f) * MetresPerTile, level * DeckLevelMetres,
                            -((north + y + .5f) * MetresPerTile))));
                    }
                }
            }
        }
    }

    /// <summary>
    /// A barrier a Tile along each raised deck level's open sides. The side against the store has none.
    /// </summary>
    private IEnumerable<(ulong Id, Transform3D Where)> DeckEdges()
    {
        var turned = new Basis(Vector3.Up, Mathf.Pi * .5f);

        foreach (Supermarket each in Supermarkets())
        {
            (int east, int north, int along, int toward) = SupermarketCarPark(each.Lot);
            float west = east * MetresPerTile;
            float eastEdge = (east + along) * MetresPerTile;
            float rear = (north + toward) * MetresPerTile;

            for (int level = 1; level < each.Levels; level++)
            {
                float height = level * DeckLevelMetres;

                for (int x = 0; x < along; x++)
                {
                    yield return (each.Id, new Transform3D(Basis.Identity,
                        new Vector3((east + x + .5f) * MetresPerTile, height, -rear)));
                }

                for (int y = 0; y < toward; y++)
                {
                    float z = -((north + y + .5f) * MetresPerTile);
                    yield return (each.Id, new Transform3D(turned, new Vector3(west, height, z)));
                    yield return (each.Id, new Transform3D(turned, new Vector3(eastEdge, height, z)));
                }
            }
        }
    }

    /// <summary>
    /// One stall marking at each stall of the layout, on every level, so the draw list holds one row
    /// per stall and its count is the Car Park's capacity.
    /// </summary>
    private IEnumerable<(ulong Id, Transform3D Where)> SupermarketStalls()
    {
        foreach (Supermarket each in Supermarkets())
        {
            (int east, int north, int along, int toward) = SupermarketCarPark(each.Lot);
            int count = StallLayout.Of(along, toward, _world.Rules.Parking.Stalls).Stalls;

            if (_placed.Length < count)
            {
                _placed = new Stall[count];
            }

            StallLayout.Place(along, toward, _world.Rules.Parking.Stalls, _placed);

            float west = east * MetresPerTile;
            float south = north * MetresPerTile;

            for (int level = 0; level < each.Levels; level++)
            {
                for (int i = 0; i < count; i++)
                {
                    Stall stall = _placed[i];
                    float x = west + ((stall.EastCentimetres + (stall.WideCentimetres * .5f)) * .01f);
                    float z = south + ((stall.NorthCentimetres + (stall.DeepCentimetres * .5f)) * .01f);

                    yield return (each.Id, new Transform3D(
                        Basis.Identity, new Vector3(x, level * DeckLevelMetres, -z)));
                }
            }
        }
    }
}
