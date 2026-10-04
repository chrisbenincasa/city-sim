using System.Collections.Generic;
using Borough.Appearance;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each market hall and its square from the Blender modules in <c>assets/market-hall</c>,
/// which <c>scripts/art/market-hall.py</c> authors. The simulation supplies the hall's footprint and
/// one stall Unit per stall; the square fills the Lot north of the hall. A seeded pick per Building
/// chooses the family, and a stall draws open while let and shuttered while vacant. Stalls show from
/// above through the iron hall's see-through glass and between the stall shed's aisle roofs.
/// </summary>
public partial class Main
{
    private enum HallFamily { Iron, Concrete, Shed }

    private static readonly string[] HallModules =
    [
        "iron-wall", "iron-entrance", "iron-roof",
        "concrete-wall", "concrete-entrance", "concrete-roof",
        "shed-wall", "shed-entrance", "shed-roof",
        "stall-open", "stall-shut", "square-paving", "square-tree", "square-bench", "iron-glass",
    ];

    private const int HallWall = 0;
    private const int HallEntrance = 1;
    private const int HallRoof = 2;
    private const int StallOpen = 9;
    private const int StallShut = 10;
    private const int SquarePaving = 11;
    private const int SquareTree = 12;
    private const int SquareBench = 13;
    private const int IronGlass = 14;

    private const float GlassOpacity = .3f;

    // Trees stand on a grid this many Tiles apart, with a bench on the Tile south of each.
    private const int TreeEvery = 6;

    private readonly InstanceLayer[] _hallLayers = new InstanceLayer[HallModules.Length];
    private readonly List<ulong>[] _hallIds = new List<ulong>[HallModules.Length];

    private readonly record struct Hall(ulong Id, int Slot, int Lot, HallFamily Family);

    private void CreateMarketHallLayers()
    {
        for (int i = 0; i < HallModules.Length; i++)
        {
            _hallLayers[i] = Layer(Colors.White, Commit(Load($"market-hall/{HallModules[i]}").Corners),
                perInstance: true, casts: i != SquarePaving && i != IronGlass);
            _hallIds[i] = [];
        }

        var glass = (StandardMaterial3D)((ArrayMesh)_hallLayers[IronGlass].Multimesh.Mesh).SurfaceGetMaterial(0);
        glass.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        glass.AlbedoColor = new Color(1f, 1f, 1f, GlassOpacity);
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> MarketHallLayers()
    {
        for (int i = 0; i < HallModules.Length; i++)
        {
            yield return ($"hall-{HallModules[i]}", _hallLayers[i], true, _hallIds[i]);
        }
    }

    private IEnumerable<Hall> MarketHalls()
    {
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!_world.IsMarketHall(slot))
            {
                continue;
            }

            ulong id = rows.IdAt(slot);
            ulong draw = Randomness.Draw(_world.Key, id, Ticks.Zero, PurposeTag.AppearanceFamily);

            yield return new(id, slot, _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]), (HallFamily)(int)(draw % 3));
        }
    }

    private void FillMarketHalls()
    {
        var placed = new List<(ulong, Transform3D, Color)>[_hallLayers.Length];

        for (int i = 0; i < placed.Length; i++)
        {
            placed[i] = [];
        }

        foreach (Hall hall in MarketHalls())
        {
            PlaceHall(hall, placed);
            PlaceSquare(hall, placed);
        }

        for (int i = 0; i < _hallLayers.Length; i++)
        {
            Fill(_hallLayers[i], placed[i], _hallIds[i]);
        }
    }

    private void PlaceHall(Hall hall, List<(ulong, Transform3D, Color)>[] placed)
    {
        LotTable lots = _world.Lots;
        UnitTable units = _world.Units;
        int east = FootprintFrame(hall.Lot, trade: true).X;
        int north = FootprintFrame(hall.Lot, trade: true).Y;
        int wide = FootprintFrame(hall.Lot, trade: true).Width;
        int deep = FootprintFrame(hall.Lot, trade: true).Height;
        int family = 3 * (int)hall.Family;

        void Put(int module, Basis facing, int x, int y) =>
            placed[module].Add((hall.Id, OnTradeLot(hall.Lot, new Transform3D(facing, At(east + x + .5f, 0f, north + y + .5f))), Colors.White));

        bool Stall(int x)
        {
            for (int pair = 0; pair < MarketHall.Pairs(wide); pair++)
            {
                int west = MarketHall.PairEast(wide, pair);

                if (x == west || x == west + 1)
                {
                    return true;
                }
            }

            return false;
        }

        bool Aisle(int x) => !Stall(x) && x > 0 && x < wide - 1;

        for (int x = 0; x < wide; x++)
        {
            int edge = family + (Aisle(x) ? HallEntrance : HallWall);
            Put(edge, FacingSouth, x, 0);
            Put(edge, FacingNorth, x, deep - 1);

            for (int y = 0; y < deep; y++)
            {
                if (hall.Family != HallFamily.Shed)
                {
                    Put(family + HallRoof, Basis.Identity, x, y);
                }
                else if (!Stall(x))
                {
                    Put(family + HallRoof, x > 0 && !Stall(x - 1) ? FacingEast : FacingWest, x, y);
                }

                if (hall.Family == HallFamily.Iron)
                {
                    Put(IronGlass, Basis.Identity, x, y);
                }
            }
        }

        for (int y = 0; y < deep; y++)
        {
            Put(family + HallWall, FacingWest, 0, y);
            Put(family + HallWall, FacingEast, wide - 1, y);
        }

        foreach (int unit in _world.BuildingUnits.Walk(hall.Slot))
        {
            UnitLiveFacts.TryOf(_world, unit, out UnitLiveFacts live);
            Basis facing = units.Side[unit] == (byte)BlockFace.West ? FacingWest : FacingEast;
            Put(live.Let ? StallOpen : StallShut, facing, units.East[unit].Raw, units.North[unit].Raw);
        }
    }

    /// <summary>Paves the Lot from the hall's north wall to the north street, under a grid of trees.</summary>
    private void PlaceSquare(Hall hall, List<(ulong, Transform3D, Color)>[] placed)
    {
        LotTable lots = _world.Lots;
        int east = FootprintFrame(hall.Lot, trade: true).X;
        int wide = FootprintFrame(hall.Lot, trade: true).Width;
        int south = FootprintFrame(hall.Lot, trade: true).Y + FootprintFrame(hall.Lot, trade: true).Height;
        (Parcel parcel, BlockGround ground) = TradeGround(hall.Lot);
        int top = System.Math.Min(
            parcel.North.Raw + parcel.Deep.Raw, ground.North + ground.Deep - _world.Rules.Lots.StreetHalfWidthTiles);
        int margin = (wide % TreeEvery) / 2 + (TreeEvery / 2);

        void Put(int module, float x, float y) =>
            placed[module].Add((hall.Id, OnTradeLot(hall.Lot, new Transform3D(Basis.Identity, At(x, 0f, y))), Colors.White));

        for (int y = south; y < top; y++)
        {
            for (int x = 0; x < wide; x++)
            {
                Put(SquarePaving, east + x + .5f, y + .5f);

                bool tree = (x - margin) % TreeEvery == 0 && (top - 1 - y - (TreeEvery / 2)) % TreeEvery == 0
                    && x >= margin && y < top - 2 && y > south + 1;

                if (tree)
                {
                    Put(SquareTree, east + x + .5f, y + .5f);
                    Put(SquareBench, east + x + .5f, y - .5f);
                }
            }
        }
    }
}
