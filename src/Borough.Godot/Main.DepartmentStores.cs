using System;
using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each high-street block's department store from the Blender modules in
/// <c>assets/department-store</c>, which <c>scripts/art/department-store.py</c> authors. The
/// simulation supplies the footprint, storeys and Unit layout, and no module is stretched: the body is
/// one cell a Tile a storey, and each street wall one facade bay a Tile a storey.
/// </summary>
public partial class Main
{
    private InstanceLayer _storeBodies = null!;
    private InstanceLayer _storeRoofs = null!;
    private InstanceLayer _storeParapets = null!;
    private InstanceLayer _storeWindows = null!;
    private InstanceLayer _storeAnchorBays = null!;
    private InstanceLayer _storeDoors = null!;
    private InstanceLayer _storeCornerBays = null!;
    private InstanceLayer _storeTowers = null!;
    private readonly List<ulong> _storeBodyIds = [];
    private readonly List<ulong> _storeRoofIds = [];
    private readonly List<ulong> _storeParapetIds = [];
    private readonly List<ulong> _storeWindowIds = [];
    private readonly List<ulong> _storeAnchorBayIds = [];
    private readonly List<ulong> _storeDoorIds = [];
    private readonly List<ulong> _storeCornerBayIds = [];
    private readonly List<ulong> _storeTowerIds = [];

    private enum Facade { Window, Anchor, Entrance, Corner }

    private readonly record struct Store(ulong Id, int Lot, int East, int North, int Wide, int Deep, int Storeys);

    private static readonly Basis FacingSouth = Basis.Identity;
    private static readonly Basis FacingNorth = new(Vector3.Up, Mathf.Pi);
    private static readonly Basis FacingWest = new(Vector3.Up, -Mathf.Pi * .5f);
    private static readonly Basis FacingEast = new(Vector3.Up, Mathf.Pi * .5f);

    private void CreateDepartmentStoreLayers()
    {
        _storeBodies = Layer(Colors.White, StoreModule("body"), perInstance: true);
        _storeRoofs = Layer(Colors.White, StoreModule("roof"), perInstance: true, casts: false);
        _storeParapets = Layer(Colors.White, StoreModule("parapet"), perInstance: true);
        _storeWindows = Layer(Colors.White, StoreModule("window-bay"), perInstance: true, casts: false);
        _storeAnchorBays = Layer(Colors.White, StoreModule("anchor-bay"), perInstance: true, casts: false);
        _storeDoors = Layer(Colors.White, StoreModule("entrance"), perInstance: true);
        _storeCornerBays = Layer(Colors.White, StoreModule("corner-bay"), perInstance: true, casts: false);
        _storeTowers = Layer(Colors.White, StoreModule("corner-tower"), perInstance: true);
    }

    private static ArrayMesh StoreModule(string name) => Commit(Load($"department-store/{name}").Corners);

    private void FillDepartmentStores()
    {
        Fill(_storeBodies, White(StoreCells(roof: false)), _storeBodyIds);
        Fill(_storeRoofs, White(StoreCells(roof: true)), _storeRoofIds);
        Fill(_storeParapets, White(StoreParapets()), _storeParapetIds);
        Fill(_storeWindows, White(StoreFacades(Facade.Window)), _storeWindowIds);
        Fill(_storeAnchorBays, White(StoreFacades(Facade.Anchor)), _storeAnchorBayIds);
        Fill(_storeDoors, White(StoreFacades(Facade.Entrance)), _storeDoorIds);
        Fill(_storeCornerBays, White(StoreFacades(Facade.Corner)), _storeCornerBayIds);
        Fill(_storeTowers, White(StoreTowers()), _storeTowerIds);
    }

    private IEnumerable<Store> DepartmentStores()
    {
        var rows = _world.Buildings.Rows;
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (_world.IsDepartmentStore(slot))
            {
                int lot = lots.Rows.Resolve(_world.Buildings.Lot[slot]);
                yield return new(rows.IdAt(slot), lot, FootprintFrame(lot, trade: true).X, FootprintFrame(lot, trade: true).Y,
                    FootprintFrame(lot, trade: true).Width, FootprintFrame(lot, trade: true).Height, Math.Max(1, (int)lots.Storeys[lot]));
            }
        }
    }

    private static Vector3 At(float east, float up, float north) =>
        new(east * MetresPerTile, up, -(north * MetresPerTile));

    /// <summary>One body cell a Tile a storey, or one roof tile a Tile on top.</summary>
    private IEnumerable<(ulong Id, Transform3D Where)> StoreCells(bool roof)
    {
        foreach (Store each in DepartmentStores())
        {
            for (int storey = roof ? each.Storeys : 0; storey < (roof ? each.Storeys + 1 : each.Storeys); storey++)
            {
                for (int x = 0; x < each.Wide; x++)
                {
                    for (int y = 0; y < each.Deep; y++)
                    {
                        yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(Basis.Identity,
                            At(each.East + x + .5f, storey * StoreyMetres, each.North + y + .5f))));
                    }
                }
            }
        }
    }

    private IEnumerable<(ulong Id, Transform3D Where)> StoreParapets()
    {
        foreach (Store each in DepartmentStores())
        {
            float top = each.Storeys * StoreyMetres;

            for (int x = 0; x < each.Wide; x++)
            {
                float east = each.East + x + .5f;
                yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(FacingSouth, At(east, top, each.North))));
                yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(FacingNorth, At(east, top, each.North + each.Deep))));
            }

            for (int y = 0; y < each.Deep; y++)
            {
                float north = each.North + y + .5f;
                yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(FacingWest, At(each.East, top, north))));
                yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(FacingEast, At(each.East + each.Wide, top, north))));
            }
        }
    }

    /// <summary>
    /// One facade bay a Tile a storey on the three street walls. The ground storey shows each Unit's
    /// shopfront, with the entrance at the middle of the anchor, and every storey above has windows.
    /// </summary>
    private List<(ulong Id, Transform3D Where)> StoreFacades(Facade wanted)
    {
        Span<DepartmentStore.Bay> bays = stackalloc DepartmentStore.Bay[DepartmentStore.MaxUnits];
        var places = new List<(ulong Id, Transform3D Where)>();

        foreach (Store each in DepartmentStores())
        {
            int count = DepartmentStore.Units(each.Wide, bays);
            bool corners = count > 1;

            for (int storey = 0; storey < each.Storeys; storey++)
            {
                float up = storey * StoreyMetres;

                for (int i = 0; i < count; i++)
                {
                    DepartmentStore.Bay bay = bays[i];

                    for (int x = bay.East; x < bay.East + bay.Wide; x++)
                    {
                        Facade facade = storey > 0 ? Facade.Window
                            : !bay.Anchor ? Facade.Corner
                            : x == bay.East + (bay.Wide / 2) ? Facade.Entrance
                            : Facade.Anchor;

                        if (facade == wanted)
                        {
                            places.Add((each.Id, OnTradeLot(each.Lot, new Transform3D(FacingSouth, At(each.East + x + .5f, up, each.North)))));
                        }
                    }
                }

                Facade side = storey > 0 ? Facade.Window : corners ? Facade.Corner : Facade.Anchor;

                if (side != wanted)
                {
                    continue;
                }

                for (int y = 0; y < each.Deep; y++)
                {
                    float north = each.North + y + .5f;
                    places.Add((each.Id, OnTradeLot(each.Lot, new Transform3D(FacingWest, At(each.East, up, north)))));
                    places.Add((each.Id, OnTradeLot(each.Lot, new Transform3D(FacingEast, At(each.East + each.Wide, up, north)))));
                }
            }
        }

        return places;
    }

    /// <summary>A corner tower on the roof over the south end of each corner Unit.</summary>
    private IEnumerable<(ulong Id, Transform3D Where)> StoreTowers()
    {
        const float roofMetres = .15f;
        const int half = DepartmentStore.CornerTiles / 2;

        foreach (Store each in DepartmentStores())
        {
            if (each.Wide <= 2 * DepartmentStore.CornerTiles || each.Deep < DepartmentStore.CornerTiles)
            {
                continue;
            }

            float top = (each.Storeys * StoreyMetres) + roofMetres;
            yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(Basis.Identity, At(each.East + half, top, each.North + half))));
            yield return (each.Id, OnTradeLot(each.Lot, new Transform3D(Basis.Identity, At(each.East + each.Wide - half, top, each.North + half))));
        }
    }
}
