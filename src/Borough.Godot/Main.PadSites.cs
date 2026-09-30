using System;
using System.Collections.Generic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each pad site from the Blender modules in <c>assets/pad-site</c>, which
/// <c>scripts/art/pad-site.py</c> authors. The simulation supplies the footprint, the facing side and
/// the stall band. A seeded pick per Building chooses the family, which decides what fills the
/// forecourt. No module is stretched: bays and forecourt pieces are one Tile along the street.
/// </summary>
public partial class Main
{
    private enum PadFamily { Petrol, FastFood, Bank }

    private static readonly string[] PadModules =
    [
        "kiosk-bay", "kiosk-door", "canopy", "pump-island",
        "pavilion-bay", "pavilion-door", "drive-lane", "menu-board",
        "bank-bay", "bank-door", "drive-up-lane", "teller",
    ];

    // Each family's four modules, in PadModules order: bay, door bay, the forecourt piece laid along
    // its rows, and the one feature standing in it.
    private const int PadBay = 0;
    private const int PadDoor = 1;
    private const int PadCover = 2;
    private const int PadFeature = 3;

    private const int PadSurface = 12;
    private const int PadStall = 13;

    private readonly InstanceLayer[] _padLayers = new InstanceLayer[PadModules.Length + 2];
    private readonly List<ulong>[] _padIds = new List<ulong>[PadModules.Length + 2];

    private readonly record struct Pad(
        ulong Id, int East, int North, int Wide, int Deep, BlockFace Face, PadFamily Family);

    private void CreatePadSiteLayers()
    {
        for (int i = 0; i < PadModules.Length; i++)
        {
            bool ground = PadModules[i] is "drive-lane" or "drive-up-lane";
            _padLayers[i] = Layer(Colors.White, Commit(Load($"pad-site/{PadModules[i]}").Corners), perInstance: true,
                casts: !ground);
        }

        _padLayers[PadSurface] = Layer(Colors.White, Module("parking-surface"), perInstance: true, casts: false);
        _padLayers[PadStall] = Layer(Colors.White, Module("stall"), perInstance: true, casts: false);

        for (int i = 0; i < _padIds.Length; i++)
        {
            _padIds[i] = [];
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> PadLayers()
    {
        for (int i = 0; i < PadModules.Length; i++)
        {
            yield return ($"pad-{PadModules[i]}", _padLayers[i], true, _padIds[i]);
        }

        yield return ("pad-surface", _padLayers[PadSurface], true, _padIds[PadSurface]);
        yield return ("pad-stall", _padLayers[PadStall], true, _padIds[PadStall]);
    }

    private void FillPadSites()
    {
        var placed = new List<(ulong, Transform3D, Color)>[_padLayers.Length];

        for (int i = 0; i < placed.Length; i++)
        {
            placed[i] = [];
        }

        foreach (Pad pad in PadSites())
        {
            PlacePad(pad, placed);
        }

        for (int i = 0; i < _padLayers.Length; i++)
        {
            Fill(_padLayers[i], placed[i], _padIds[i]);
        }
    }

    private IEnumerable<Pad> PadSites()
    {
        var rows = _world.Buildings.Rows;
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!_world.IsPadSite(slot))
            {
                continue;
            }

            int lot = lots.Rows.Resolve(_world.Buildings.Lot[slot]);
            BlockFace face = BlockFace.East;

            foreach (int unit in _world.BuildingUnits.Walk(slot))
            {
                face = (BlockFace)_world.Units.Side[unit];
            }

            ulong id = rows.IdAt(slot);
            ulong draw = Randomness.Draw(_world.Key, id, Ticks.Zero, PurposeTag.AppearanceFamily);

            yield return new(id, lots.FootprintEast[lot].Raw, lots.FootprintNorth[lot].Raw,
                lots.FootprintWide[lot].Raw, lots.FootprintDeep[lot].Raw, face, (PadFamily)(int)(draw % 3));
        }
    }

    private void PlacePad(Pad pad, List<(ulong, Transform3D, Color)>[] placed)
    {
        Basis facing = pad.Face == BlockFace.West ? FacingWest : FacingEast;
        int family = 4 * (int)pad.Family;
        int door = pad.Deep / 2;

        void Put(int module, float east, float north) =>
            placed[module].Add((pad.Id, new Transform3D(facing, At(east, 0f, north)), Colors.White));

        float Across(float fromFront) => pad.Face == BlockFace.West
            ? pad.East - fromFront
            : pad.East + pad.Wide + fromFront;

        for (int j = 0; j < pad.Deep; j++)
        {
            float north = pad.North + j + .5f;
            Put(family + (j == door ? PadDoor : PadBay), pad.East + (pad.Wide * .5f), north);

            for (int i = 0; i < PadSite.ForecourtTiles; i++)
            {
                bool lane = i == 0 && pad.Family != PadFamily.Petrol;
                Put(lane ? family + PadCover : PadSurface, Across(i + .5f), north);

                if (pad.Family == PadFamily.Petrol && i is 1 or 2)
                {
                    Put(family + PadCover, Across(i + .5f), north);

                    if (j % 2 == 1)
                    {
                        Put(family + PadFeature, Across(i + .5f), north);
                    }
                }
            }
        }

        if (pad.Family == PadFamily.FastFood)
        {
            Put(family + PadFeature, Across(1.2f), pad.North + pad.Deep - .5f);
        }
        else if (pad.Family == PadFamily.Bank)
        {
            Put(family + PadFeature, Across(1.1f), pad.North + door + .5f);
        }

        PlacePadCarPark(pad, placed);
    }

    private void PlacePadCarPark(Pad pad, List<(ulong, Transform3D, Color)>[] placed)
    {
        (int east, int north, int along, int toward) = PadSite.CarPark(pad.East, pad.North, pad.Wide, pad.Face);

        for (int x = 0; x < along; x++)
        {
            for (int y = 0; y < toward; y++)
            {
                placed[PadSurface].Add((pad.Id, new Transform3D(Basis.Identity, At(east + x + .5f, 0f, north + y + .5f)),
                    Colors.White));
            }
        }

        int count = StallLayout.Of(along, toward, _world.Rules.Parking.Stalls).Stalls;

        if (_placed.Length < count)
        {
            _placed = new Stall[count];
        }

        StallLayout.Place(along, toward, _world.Rules.Parking.Stalls, _placed);

        float west = east * MetresPerTile;
        float south = north * MetresPerTile;

        for (int i = 0; i < count; i++)
        {
            Stall stall = _placed[i];
            float x = west + ((stall.EastCentimetres + (stall.WideCentimetres * .5f)) * .01f);
            float z = south + ((stall.NorthCentimetres + (stall.DeepCentimetres * .5f)) * .01f);

            placed[PadStall].Add((pad.Id, new Transform3D(Basis.Identity, new Vector3(x, 0f, -z)), Colors.White));
        }
    }
}
