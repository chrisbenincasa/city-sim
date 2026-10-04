using System.Collections.Generic;
using Borough.Appearance;
using Borough.Core.Entities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private const float StallLineMetres = 0.12f;
    private const float FasciaMetres = 0.8f;
    private const float FasciaProudMetres = 0.3f;
    private const float FasciaGapMetres = 0.4f;

    private static readonly Color Asphalt = new(0.20f, 0.20f, 0.21f);
    private static readonly Color StallPaint = new(0.86f, 0.86f, 0.82f);
    private static readonly Color AnchorFascia = new(0.18f, 0.30f, 0.45f);
    private static readonly Color LetFascia = new(0.55f, 0.22f, 0.16f);
    private static readonly Color VacantFascia = new(0.46f, 0.46f, 0.46f);

    private InstanceLayer _carParks = null!;
    private InstanceLayer _stalls = null!;
    private InstanceLayer _units = null!;
    private readonly List<ulong> _carParkIds = [];
    private readonly List<ulong> _stallIds = [];
    private readonly List<ulong> _unitIds = [];
    private Stall[] _placed = [];
    private readonly List<int> _walked = [];

    private void CreateCarParkLayers()
    {
        _carParks = Layer(Asphalt, Vector3.One, casts: false);
        _stalls = Layer(StallPaint, Vector3.One, casts: false);
        _units = Layer(Colors.White, Vector3.One, perInstance: true);
    }

    private void FillCarParks()
    {
        Fill(_carParks, CarParkSurfaces(), _carParkIds);
        Fill(_stalls, StallLines(), _stallIds);
        Fill(_units, Fascias(), _unitIds);
        FillSupermarkets();
        FillDepartmentStores();
        FillPadSites();
        FillSalesYards();
        FillPrecincts();
        FillMarketHalls();
    }

    private IEnumerable<(ulong Id, int Lot)> Centres()
    {
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (_world.IsTradeCentre(slot))
            {
                yield return (rows.IdAt(slot), _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]));
            }
        }
    }

    private (int East, int North, int Along, int Toward) CarParkOf(int lot)
    {
        LandRectangle parcel = ParcelFrame(lot, trade: true), foot = FootprintFrame(lot, trade: true);

        return CarParkCentre.CarPark(
            parcel.Y, foot.X, foot.Y, foot.Width, _world.Rules.Lots.StreetHalfWidthTiles);
    }

    private IEnumerable<(ulong Id, Transform3D Where)> CarParkSurfaces()
    {
        foreach ((ulong id, int lot) in Centres())
        {
            (int east, int north, int along, int toward) = CarParkOf(lot);

            if (along <= 0 || toward <= 0)
            {
                continue;
            }

            float wide = along * MetresPerTile;
            float deep = toward * MetresPerTile;
            Vector3 centre = new((east * MetresPerTile) + (wide * .5f), 0.03f, -((north * MetresPerTile) + (deep * .5f)));

            yield return (id, OnTradeLot(lot, new Transform3D(Basis.FromScale(new Vector3(wide, 0.01f, deep)), centre)));
        }
    }

    /// <summary>
    /// One painted line along the north edge of each stall, so the draw list holds one row per stall
    /// and its count is the Car Park's capacity.
    /// </summary>
    private IEnumerable<(ulong Id, Transform3D Where)> StallLines()
    {
        foreach ((ulong id, int lot) in Centres())
        {
            (int east, int north, int along, int toward) = CarParkOf(lot);
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
                float length = stall.WideCentimetres * .01f;
                float x = west + ((stall.EastCentimetres * .01f) + (length * .5f));
                float z = south + ((stall.NorthCentimetres + stall.DeepCentimetres) * .01f);

                yield return (id, OnTradeLot(lot, new Transform3D(
                    Basis.FromScale(new Vector3(length, 0.01f, StallLineMetres)), new Vector3(x, 0.045f, -z))));
            }
        }
    }

    /// <summary>
    /// A fascia band across each centre Unit's front, short of its neighbours so the row reads as
    /// separate shops. Its colour says whether the Unit is the anchor, let or vacant.
    /// </summary>
    private IEnumerable<(ulong Id, Transform3D Where, Color What)> Fascias()
    {
        LotTable lots = _world.Lots;
        UnitTable units = _world.Units;
        var rows = _world.Buildings.Rows;

        for (int slot = 0; slot < rows.SlotCount; slot++)
        {
            if (!_world.IsTradeCentre(slot))
            {
                continue;
            }

            int lot = lots.Rows.Resolve(_world.Buildings.Lot[slot]);
            LandRectangle foot = FootprintFrame(lot, trade: true);
            int footEast = foot.X;
            int footNorth = foot.Y;
            float top = System.Math.Max(1, (int)lots.Storeys[lot]) * StoreyMetres;

            _walked.Clear();

            foreach (int unit in _world.BuildingUnits.Walk(slot))
            {
                _walked.Add(unit);
            }

            foreach (int unit in _walked)
            {
                if (units.Side[unit] != (byte)BlockFace.South)
                {
                    continue;
                }

                float wide = (units.Wide[unit].Raw * MetresPerTile) - FasciaGapMetres;
                float x = (footEast + units.East[unit].Raw + (units.Wide[unit].Raw * .5f)) * MetresPerTile;
                float z = ((footNorth + units.North[unit].Raw) * MetresPerTile) - (FasciaProudMetres * .5f);
                UnitLiveFacts.TryOf(_world, unit, out UnitLiveFacts live);
                Color colour = units.Anchor[unit] != 0 ? AnchorFascia
                    : live.Let ? LetFascia
                    : VacantFascia;

                yield return (
                    units.Rows.IdAt(unit),
                    OnTradeLot(lot, new Transform3D(
                        Basis.FromScale(new Vector3(wide, FasciaMetres, FasciaProudMetres)),
                        new Vector3(x, top - (FasciaMetres * .5f) - 0.2f, -z))),
                    colour.SrgbToLinear());
            }
        }
    }
}
