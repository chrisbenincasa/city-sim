using System;
using System.Collections.Generic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

/// <summary>
/// Draws each precinct's shop rows and walkways from the Blender modules in <c>assets/precinct</c>,
/// which <c>scripts/art/precinct.py</c> authors. The simulation supplies the footprint, the rows and
/// the storeys; the supermarket drawing draws the deck behind. A seeded pick per Building chooses the
/// family. No module is stretched: every shop and walkway piece is one Tile.
/// </summary>
public partial class Main
{
    private enum PrecinctFamily { Arcade, Open, Galleria }

    private static readonly string[] PrecinctModules =
    [
        "arcade-front", "arcade-door", "arcade-body", "arcade-cover", "arcade-gallery",
        "open-front", "open-door", "open-body", "open-cover", "open-gallery",
        "galleria-front", "galleria-door", "galleria-body", "galleria-cover", "galleria-gallery",
        "paving",
    ];

    // Each family's five modules, in PrecinctModules order: the front tile facing the walkway, the
    // front tile with a Unit's door, a tile behind the front, the walkway piece, and the gallery.
    private const int PrecinctFront = 0;
    private const int PrecinctDoor = 1;
    private const int PrecinctBody = 2;
    private const int PrecinctCover = 3;
    private const int PrecinctGallery = 4;

    private const int PrecinctPaving = 15;

    private const float PrecinctStoreyMetres = 3.5f;

    // The open family's planters stand on every third walkway Tile.
    private const int PlanterEvery = 3;

    private readonly InstanceLayer[] _precinctLayers = new InstanceLayer[PrecinctModules.Length];
    private readonly List<ulong>[] _precinctIds = new List<ulong>[PrecinctModules.Length];

    private readonly record struct PrecinctSite(ulong Id, int Lot, PrecinctFamily Family);

    private void CreatePrecinctLayers()
    {
        for (int i = 0; i < PrecinctModules.Length; i++)
        {
            _precinctLayers[i] = Layer(Colors.White, Commit(Load($"precinct/{PrecinctModules[i]}").Corners),
                perInstance: true, casts: i != PrecinctPaving);
            _precinctIds[i] = [];
        }
    }

    private IEnumerable<(string Name, InstanceLayer Layer, bool Colours, List<ulong>? Ids)> PrecinctLayers()
    {
        for (int i = 0; i < PrecinctModules.Length; i++)
        {
            yield return ($"precinct-{PrecinctModules[i]}", _precinctLayers[i], true, _precinctIds[i]);
        }
    }

    private void FillPrecincts()
    {
        var placed = new List<(ulong, Transform3D, Color)>[_precinctLayers.Length];

        for (int i = 0; i < placed.Length; i++)
        {
            placed[i] = [];
        }

        foreach (PrecinctSite site in Precincts())
        {
            PlacePrecinct(site, placed);
        }

        for (int i = 0; i < _precinctLayers.Length; i++)
        {
            Fill(_precinctLayers[i], placed[i], _precinctIds[i]);
        }
    }

    private IEnumerable<PrecinctSite> Precincts()
    {
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!_world.IsPrecinct(slot))
            {
                continue;
            }

            ulong id = rows.IdAt(slot);
            ulong draw = Randomness.Draw(_world.Key, id, Ticks.Zero, PurposeTag.AppearanceFamily);

            yield return new(id, _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]), (PrecinctFamily)(int)(draw % 3));
        }
    }

    private void PlacePrecinct(PrecinctSite site, List<(ulong, Transform3D, Color)>[] placed)
    {
        LotTable lots = _world.Lots;
        int east = lots.FootprintEast[site.Lot].Raw;
        int north = lots.FootprintNorth[site.Lot].Raw;
        int wide = lots.FootprintWide[site.Lot].Raw;
        int deep = lots.FootprintDeep[site.Lot].Raw;
        int storeys = lots.Storeys[site.Lot];
        int family = 5 * (int)site.Family;
        Span<Precinct.Row> rows = stackalloc Precinct.Row[Precinct.RowCount(wide)];
        int count = Precinct.Rows(wide, rows);

        void Put(int module, Basis facing, float x, float up, float y) =>
            placed[module].Add((site.Id, new Transform3D(facing, At(x, up, y)), Colors.White));

        for (int r = 0; r < count; r++)
        {
            Precinct.Row row = rows[r];
            bool facesEast = row.Face == BlockFace.East;
            Basis facing = facesEast ? FacingEast : FacingWest;
            int front = facesEast ? row.Wide - 1 : 0;

            for (int storey = 0; storey < storeys; storey++)
            {
                float up = storey * PrecinctStoreyMetres;

                for (int y = 0; y < deep; y++)
                {
                    for (int x = 0; x < row.Wide; x++)
                    {
                        int module = x != front ? PrecinctBody
                            : y % Precinct.UnitTiles == 0 ? PrecinctDoor
                            : PrecinctFront;
                        Put(family + module, facing, east + row.East + x + .5f, up, north + y + .5f);

                        if (x == front && storey > 0)
                        {
                            Put(family + PrecinctGallery, facing, east + row.East + x + .5f, up, north + y + .5f);
                        }
                    }
                }
            }
        }

        for (int r = 0; r + 1 < count; r += 2)
        {
            int walkway = east + rows[r].East + rows[r].Wide;
            float middle = walkway + (Precinct.WalkwayTiles * .5f);

            for (int y = 0; y < deep; y++)
            {
                for (int x = 0; x < Precinct.WalkwayTiles; x++)
                {
                    Put(PrecinctPaving, Basis.Identity, walkway + x + .5f, 0f, north + y + .5f);
                }

                if (site.Family != PrecinctFamily.Open)
                {
                    Put(family + PrecinctCover, Basis.Identity, middle, storeys * PrecinctStoreyMetres, north + y + .5f);
                }
                else if (y % PlanterEvery == 1)
                {
                    Put(family + PrecinctCover, Basis.Identity, middle, 0f, north + y + .5f);
                }
            }
        }
    }
}
