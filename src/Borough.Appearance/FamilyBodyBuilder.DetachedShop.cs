using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// The detached shop forms: one store across the ground storey of a Building standing alone on its Lot,
/// with flats above, on 2-3 storeys and 16-60 m of frontage. Every wall is free.
/// </summary>
/// <remarks>
/// <c>scripts/art/detached-shop-forms.py</c> drew the options; this file is the reference. The forms take
/// the wide houses' contemporary or mid-century look; the commercial block takes a mid-century look over
/// brick walls, cream trim and a timber shopfront. Each Building draws its own look, store and sign. The
/// colonnade and the glass base's set-back top draw 91-97% of the floor the Building holds, inside the
/// drawing's 20% (<c>docs/07-the-drawing.md</c>).
/// </remarks>
public static partial class FamilyBodyBuilder
{
    private static readonly string[] StoreFrames = ["1f2124", "2b2e31", "3a3d40", "b9bdbf"];
    private static readonly string[] MidCenturyStores = ["b9bdbf", "c8ccce", "2b2e31"];
    private static readonly string[] Brands = ["2f6b4f", "b4472f", "1f4e79", "c9a227", "6b2f4a", "2b2e31", "d9d4c8"];
    private static readonly string[] TimberStores = ["2f4a3c", "6a2f2a", "24303a", "1f2124", "2e5a5a", "7a3b52"];
    private static readonly string[] Creams = ["e3dccb", "d8d2c4", "ddd6c4"];
    private static readonly string StoreBack = Dressed("door", null, "55595c");
    private static readonly string Paving = Dressed("plinth", null, "a8a49b");

    /// <summary>Whether a shop form is a detached store rather than a shop house.</summary>
    internal static bool IsDetachedShop(ShopForm form) => form is ShopForm.GlassBase or ShopForm.GabledHall or ShopForm.Colonnade
        or ShopForm.MonoPitch or ShopForm.Canopy or ShopForm.FoldedPlate or ShopForm.CommercialBlock;

    private enum StorePeriod : byte
    {
        Contemporary,
        MidCentury,
        Traditional,
    }

    private static StorePeriod PeriodOf(ShopForm form) => form switch
    {
        ShopForm.Canopy or ShopForm.FoldedPlate => StorePeriod.MidCentury,
        ShopForm.CommercialBlock => StorePeriod.Traditional,
        _ => StorePeriod.Contemporary,
    };

    private static FamilyBodyMesh BuildDetachedShop(FamilyBody body, ShopForm form, float frontage, float depth, int storeys, int seed, float pitch)
    {
        var mesh = new FamilyBodyMesh();
        var w = new Writer(mesh, body) { Look = new Dictionary<string, string>(StringComparer.Ordinal) { ["metal"] = Dressed("metal", null, "55595c") } };
        var site = new StoreSite(w, form, frontage, depth, storeys, seed, pitch);
        switch (form)
        {
            case ShopForm.GlassBase:
                site.GlassBase();
                break;
            case ShopForm.GabledHall:
                site.GabledHall();
                break;
            case ShopForm.Colonnade:
                site.Colonnade();
                break;
            case ShopForm.MonoPitch:
                site.MonoPitch();
                break;
            case ShopForm.Canopy:
                site.Canopy();
                break;
            case ShopForm.FoldedPlate:
                site.FoldedPlate();
                break;
            default:
                site.CommercialBlock();
                break;
        }

        return mesh;
    }

    /// <summary>One detached store as it is drawn: its look, its store's paint and sign, and the walls it stands in.</summary>
    private sealed class StoreSite
    {
        private readonly Writer _w;
        private readonly WideLook _s;
        private readonly StorePeriod _period;
        private readonly string _store;
        private readonly string _sign;
        private readonly int _entrance;
        private readonly float _bay;
        private readonly int _bays;
        private readonly float _x;
        private readonly float _y;
        private HouseDraw _r;

        public StoreSite(Writer w, ShopForm form, float frontage, float depth, int storeys, int seed, float pitch)
        {
            _w = w;
            _period = PeriodOf(form);
            WideLook look = WideLook.Draw(_period == StorePeriod.Contemporary, seed, frontage, depth, storeys) with { Pitch = pitch };
            var r = new HouseDraw(((ulong)seed << 8) | 21);
            _entrance = r.Pick(3);
            _sign = Dressed("trim", null, r.Pick(Brands));
            if (_period == StorePeriod.Traditional)
            {
                string brick = Dressed("wall", r.Pick(NaturalBricks), null);
                look = look with
                {
                    Ground = brick,
                    Upper = brick,
                    End = brick,
                    Trim = Dressed("trim", null, r.Pick(Creams)),
                    Frame = Dressed("frame", null, "ece8dc"),
                };
                _store = Dressed("frame", null, r.Pick(TimberStores));
            }
            else
            {
                _store = Dressed("frame", null, r.Pick(_period == StorePeriod.MidCentury ? MidCenturyStores : StoreFrames));
            }

            _s = look;
            _x = frontage / 2f;
            _y = depth / 2f;
            var bays = new HouseDraw(((ulong)seed << 8) | 31);
            _bays = Math.Max(1, (int)MathF.Round(frontage / bays.Range(3.6f, 4.4f)));
            _bay = frontage / _bays;
            _r = Proportions(look);
        }

        private float H => _s.H;

        private static (Face Face, float Length) Front(float x0, float x1, float y0, float z) =>
            (new Face(new Vector3(x0, y0, z), Vector3.UnitX, -Vector3.UnitY), x1 - x0);

        private static (Face Face, float Length) Right(float x1, float y0, float y1, float z) =>
            (new Face(new Vector3(x1, y0, z), Vector3.UnitY, Vector3.UnitX), y1 - y0);

        private static (Face Face, float Length) Back(float x0, float x1, float y1, float z) =>
            (new Face(new Vector3(x1, y1, z), -Vector3.UnitX, Vector3.UnitY), x1 - x0);

        private static (Face Face, float Length) Left(float x0, float y0, float y1, float z) =>
            (new Face(new Vector3(x0, y1, z), -Vector3.UnitY, -Vector3.UnitX), y1 - y0);

        private void Framed(Face face, List<Opening> openings)
        {
            const float Bar = .07f;
            foreach (Opening o in openings)
            {
                if (o.Kind != OpeningKind.Window) continue;
                float a = o.U, b = o.U + o.W, v = o.V, h = o.V + o.H;
                _w.Slab(face, a, b, v, v + Bar, Reveal - .06f, Reveal, _s.Frame);
                _w.Slab(face, a, b, h - Bar, h, Reveal - .06f, Reveal, _s.Frame);
                _w.Slab(face, a, a + Bar, v, h, Reveal - .06f, Reveal, _s.Frame);
                _w.Slab(face, b - Bar, b, v, h, Reveal - .06f, Reveal, _s.Frame);
            }
        }

        private void Base(Face face, float width) => _w.Slab(face, -.02f, width + .02f, 0f, .15f, -.03f, 0f, WidePlinth);

        /// <summary>The store's street front: glazed bays between piers and one entrance.</summary>
        private void StoreFront(Face face, float width)
        {
            (float top, float riser, float pier) = _period switch
            {
                StorePeriod.Contemporary => (2.8f, .2f, .22f),
                StorePeriod.MidCentury => (2.85f, .45f, .3f),
                _ => (2.9f, .7f, .35f),
            };
            int n = _bays;
            float bay = width / n;
            int door = _entrance switch { 0 => 0, 1 => n / 2, _ => n - 1 };
            var openings = new List<Opening>();
            float doorAt = 0f, doorWidth = 0f;
            for (int i = 0; i < n; i++)
            {
                float u0 = i * bay;
                if (i == door)
                {
                    doorWidth = MathF.Min(2f, bay - (2f * pier) - .3f);
                    doorAt = u0 + ((bay - doorWidth) / 2f);
                    openings.Add(new Opening(doorAt, .15f, doorWidth, top - .15f, OpeningKind.Door));
                }
                else
                {
                    openings.Add(new Opening(u0 + pier, riser, bay - (2f * pier), top - riser, OpeningKind.Shop));
                }
            }

            string wall = _period == StorePeriod.Contemporary ? _store : _s.End;
            _w.Facade(face, width, Storey, openings, wall, _s.Trim, _store, _s.Frame);
            _w.Slab(face, doorAt + .12f, doorAt + doorWidth - .12f, .3f, top - .12f, Reveal + .03f, Reveal + .04f, "glass");
            Base(face, width);
            if (_period == StorePeriod.Contemporary)
            {
                _w.Slab(face, 0f, width, top + .05f, Storey - .02f, -.06f, 0f, _store);
            }
            else if (_period == StorePeriod.Traditional)
            {
                for (int i = 0; i <= n; i++)
                {
                    float a = Math.Clamp((i * bay) - (pier / 2f), 0f, width - pier);
                    _w.Slab(face, a, a + pier, 0f, 3f, -.12f, 0f, _store);
                }

                _w.Slab(face, 0f, width, 3f, 3.4f, -.2f, 0f, _store);
                _w.Slab(face, -.1f, width + .1f, 3.4f, 3.52f, -.35f, 0f, _s.Trim);
            }

            float sign = MathF.Min(12f, width * .5f), c = width / 2f;
            if (_period == StorePeriod.Contemporary) _w.Slab(face, c - (sign / 2f), c + (sign / 2f), top + .12f, Storey - .1f, -.1f, -.06f, _sign);
            else if (_period == StorePeriod.Traditional) _w.Slab(face, c - (sign / 2f), c + (sign / 2f), 3.07f, 3.33f, -.22f, -.2f, _sign);
        }

        /// <summary>A free side wall's ground storey: the store's glass turning the corner, then the flats' door.</summary>
        /// <param name="frontFirst">The wall runs back from the street, so its street end is at U = 0.</param>
        private void StoreSide(Face face, float depth, bool frontFirst)
        {
            float At(float u, float span) => frontFirst ? u : depth - u - span;
            bool contemporary = _period == StorePeriod.Contemporary;
            float wrap = _period == StorePeriod.Traditional ? MathF.Min(3.2f, depth * .25f) : MathF.Min(6f, depth * .35f);
            var openings = new List<Opening>
            {
                new(At(.4f, wrap), contemporary ? .2f : .45f, wrap, contemporary ? 2.8f : 2.4f, OpeningKind.Shop),
            };
            if (depth - wrap > 6f)
            {
                openings.Add(new Opening(At(depth - 3.6f, 1.1f), .15f, 1.1f, 2.4f, OpeningKind.Door));
                openings.Add(new Opening(At(depth - 2.1f, 1f), 1f, 1f, 1.4f, OpeningKind.Window));
            }

            _w.Facade(face, depth, Storey, openings, contemporary ? _s.Ground : _s.End, _s.Trim, _s.Doors[0], _s.Frame);
            Framed(face, openings);
            Base(face, depth);
        }

        private void StoreRear(Face face, float width)
        {
            var openings = new List<Opening> { new(1.2f, .15f, 3.2f, 3f, OpeningKind.Roller), new(5.2f, .15f, 1f, 2.3f, OpeningKind.Door) };
            if (width > 14f) openings.Add(new Opening(width - 3f, .9f, 1.6f, 1.4f, OpeningKind.Window));
            _w.Facade(face, width, Storey, openings, _s.Ground, _s.Trim, StoreBack, _s.Frame);
            Framed(face, openings);
            Base(face, width);
        }

        private List<Opening> UpperOpenings(float width, int rows, bool side)
        {
            var openings = new List<Opening>();
            if (_period == StorePeriod.MidCentury && !side)
            {
                if (width > 2f)
                {
                    for (int k = 0; k < rows; k++) openings.Add(new Opening(.5f, (k * Storey) + .95f, width - 1f, 1.35f, OpeningKind.Window));
                }

                return openings;
            }

            if (_period == StorePeriod.Traditional)
            {
                int sashes = Math.Max(1, (int)MathF.Round(width / 2.9f));
                for (int k = 0; k < rows; k++)
                {
                    for (int i = 0; i < sashes; i++) openings.Add(new Opening((width * (i + .5f) / sashes) - .55f, (k * Storey) + .75f, 1.1f, 1.9f, OpeningKind.Window));
                }

                return openings;
            }

            int n = Math.Max(1, (int)MathF.Round(width / (side ? 4.5f : 3.6f)));
            bool contemporary = _period == StorePeriod.Contemporary;
            for (int k = 0; k < rows; k++)
            {
                for (int i = 0; i < n; i++)
                {
                    float c = width * (i + .5f) / n;
                    openings.Add(contemporary && !side && (i + k) % 3 == 1
                        ? new Opening(c - .8f, (k * Storey) + .15f, 1.6f, 2.6f, OpeningKind.Window)
                        : new Opening(c - .8f, (k * Storey) + .7f, 1.6f, contemporary ? 1.8f : 1.5f, OpeningKind.Window));
                }
            }

            return openings;
        }

        /// <summary>The flats' storeys from <paramref name="z0"/>, windows in the period's pattern on every wall.</summary>
        private void Uppers(float x0, float x1, float y0, float y1, float z0, int rows, string? material = null)
        {
            if (rows <= 0) return;
            string wall = material ?? _s.Upper;
            foreach (((Face face, float n), bool side) in (ReadOnlySpan<((Face, float), bool)>)
                [(Front(x0, x1, y0, z0), false), (Right(x1, y0, y1, z0), true), (Back(x0, x1, y1, z0), false), (Left(x0, y0, y1, z0), true)])
            {
                List<Opening> openings = UpperOpenings(n, rows, side);
                _w.Facade(face, n, rows * Storey, openings, wall, _s.Trim, _s.Doors[0], _s.Frame);
                Framed(face, openings);
                foreach (Opening o in openings)
                {
                    float a = o.U, b = o.U + o.W, v = o.V, h = o.V + o.H;
                    if (_period == StorePeriod.Contemporary && o.H > 2.5f)
                    {
                        _w.Slab(face, a, b, v + .95f, v + 1f, -.08f, 0f, _s.Frame);
                    }
                    else if (_period == StorePeriod.Traditional)
                    {
                        _w.Slab(face, a - .12f, b + .12f, h, h + .22f, -.04f, 0f, _s.Trim);
                        _w.Slab(face, a, b, v + (o.H * .5f) - .03f, v + (o.H * .5f) + .03f, Reveal - .07f, Reveal - .02f, _s.Frame);
                    }
                }

                if (_period == StorePeriod.MidCentury && !side && n > 2f)
                {
                    for (int k = 0; k < rows; k++) _w.Slab(face, .5f, n - .5f, (k * Storey) + .1f, (k * Storey) + .95f, -.03f, 0f, _s.Accent);
                }
            }
        }

        private void Ground(float x0, float x1, float y0, float y1)
        {
            (Face front, float width) = Front(x0, x1, y0, 0f);
            StoreFront(front, width);
            (Face back, float across) = Back(x0, x1, y1, 0f);
            StoreRear(back, across);
            (Face right, float depth) = Right(x1, y0, y1, 0f);
            StoreSide(right, depth, frontFirst: true);
            (Face left, _) = Left(x0, y0, y1, 0f);
            StoreSide(left, depth, frontFirst: false);
        }

        private void Block(int storeys)
        {
            Ground(-_x, _x, -_y, _y);
            Uppers(-_x, _x, -_y, _y, Storey, storeys - 1);
        }

        /// <summary>A fully glazed store under flats. At three storeys the top storey stands back behind a roof terrace.</summary>
        public void GlassBase()
        {
            int low = _s.Storeys >= 3 ? _s.Storeys - 1 : _s.Storeys;
            float lowH = low * Storey;
            Block(low);
            Parapet(_w, _s, -_x, _x, -_y, _y, lowH);
            if (low == _s.Storeys) return;
            float back = _r.Range(1.8f, 2.6f);
            Uppers(-_x + .4f, _x - .4f, -_y + back, _y - .4f, lowH, 1, _s.Accent);
            _w.Box(new Vector3(-_x, -_y + back - .4f, H), new Vector3(_x, _y, H + .25f), _s.Trim);
            _w.Box(new Vector3(-_x + .3f, -_y + .1f, lowH + .5f), new Vector3(_x - .3f, -_y + .14f, lowH + 1.05f), "glass");
        }

        /// <summary>Steep plain gables across the frontage, no eaves, metal, glass in every gable end.</summary>
        public void GabledHall()
        {
            int n = Math.Max(2, (int)MathF.Round(2f * _x / _r.Range(9f, 12f)));
            float half = _x / n;
            Block(_s.Storeys);
            for (int i = 0; i < n; i++)
            {
                float cx = -_x + ((2 * i) + 1) * half;
                HipGable(_w, _s.Roof, _s.End, alongY: true, cx, 0f, half, _y, H, _s.Pitch, 0f, front: true, back: true, _s.Eaves);
                GableEdges(_w, _s.Edges, alongY: true, cx, 0f, half, _y, H, _s.Pitch, _s.Eaves);
                GableGlass(_w, _s, cx, -_y, half);
            }
        }

        /// <summary>The store set back behind square piers, the flats flush with the street above them.</summary>
        public void Colonnade()
        {
            float back = _r.Range(2.4f, 3f), y0 = -_y;
            Ground(-_x, _x, y0 + back, _y);
            Uppers(-_x, _x, y0, _y, Storey, _s.Storeys - 1);
            Parapet(_w, _s, -_x, _x, y0, _y, H, height: .6f);
            int n = Math.Max(2, (int)MathF.Round(2f * _x / _r.Range(3.6f, 4.4f)));
            for (int i = 0; i <= n; i++)
            {
                float x = -_x + (2f * _x * i / n);
                _w.Box(new Vector3(MathF.Max(-_x, x - .25f), y0, 0f), new Vector3(MathF.Min(_x, x + .25f), y0 + .5f, Storey), _s.Upper);
            }

            _w.Box(new Vector3(-_x, y0, Storey - .2f), new Vector3(_x, y0 + back, Storey), _s.Trim);
            _w.Box(new Vector3(-_x, y0, 0f), new Vector3(_x, y0 + back, .14f), Paving);
        }

        /// <summary>One low mono-pitch over the whole Building rising toward the street, a clerestory under its high edge.</summary>
        public void MonoPitch()
        {
            float t = MathF.Min(MathF.Tan(_s.Pitch * MathF.PI / 180f), 3.5f / (2f * _y)), rise = 2f * _y * t;
            float x0 = -_x, x1 = _x, y0 = -_y, y1 = _y;
            Block(_s.Storeys);
            var face = new Face(new Vector3(x0, y0, H), Vector3.UnitX, -Vector3.UnitY);
            int n = Math.Max(1, (int)MathF.Round(2f * _x / 4f));
            var openings = new List<Opening>();
            if (rise > 1.2f)
            {
                for (int i = 0; i < n; i++) openings.Add(new Opening((2f * _x * (i + .5f) / n) - 1.5f, .3f, 3f, rise - .6f, OpeningKind.Window));
            }

            _w.Facade(face, 2f * _x, rise, openings, _s.Accent, _s.Trim, _s.Doors[0], _s.Frame);
            foreach (float x in (ReadOnlySpan<float>)[x0, x1])
            {
                Shape(_w, _s.Accent, [new(x, y0, H), new(x, y1, H), new(x, y0, H + rise)], new Vector3(MathF.Sign(x), 0f, 0f));
            }

            const float Fore = .9f, Aft = .35f;
            Vector3 a = new(x0 - .3f, y1 + Aft, H - (Aft * t)), b = new(x1 + .3f, y1 + Aft, H - (Aft * t));
            Vector3 c = new(x0 - .3f, y0 - Fore, H + rise + (Fore * t)), d = new(x1 + .3f, y0 - Fore, H + rise + (Fore * t));
            Sheet(_w, _s.Roof, [c, d, b, a], Vector3.UnitZ);
            SlopeEdges(_w, _s.Edges, a, b, c, d, ridge: false);
            _w.Box(new Vector3(x0 - .3f, y0 - Fore - .04f, c.Z - .3f), new Vector3(x1 + .3f, y0 - Fore, c.Z + .02f), _s.Trim);
        }

        /// <summary>A thin flat roof, ribbon windows over colored panels, and a deep canopy along the store that turns both corners.</summary>
        public void Canopy()
        {
            float x0 = -_x, x1 = _x, y0 = -_y, y1 = _y;
            Block(_s.Storeys);
            _w.Box(new Vector3(x0, y0, H - .05f), new Vector3(x1, y1, H + .02f), Deck);
            _w.Box(new Vector3(x0 - .5f, y0 - .5f, H - .35f), new Vector3(x1 + .5f, y1 + .5f, H - .05f), _s.Trim);
            _w.Box(new Vector3(x0 - .5f, y0 - .5f, H - .05f), new Vector3(x1 + .5f, y0 - .3f, H + .25f), _s.Trim);
            _w.Box(new Vector3(x0 - .5f, y1 + .3f, H - .05f), new Vector3(x1 + .5f, y1 + .5f, H + .25f), _s.Trim);
            float deep = _r.Range(2.2f, 2.8f), turn = MathF.Min(6.5f, 2f * _y * .4f);
            _w.Box(new Vector3(x0 - deep, y0 - deep, 3.05f), new Vector3(x1 + deep, y0, 3.3f), _s.Trim);
            foreach ((float x, float sign) in (ReadOnlySpan<(float, float)>)[(x0, -1f), (x1, 1f)])
            {
                Cuboid(_w, new Vector3(x, y0, 3.05f), new Vector3(x + (sign * deep), y0 + turn, 3.3f), _s.Trim);
                Cuboid(_w, new Vector3(x, y0 + turn, 0f), new Vector3(x + (sign * .3f), y1, H - .35f), _s.Fin);
            }

            float panel = MathF.Min(10f, 2f * _x * .4f);
            _w.Box(new Vector3(-panel / 2f, y0 - deep - .05f, 3.3f), new Vector3(panel / 2f, y0 - deep + .1f, 3.95f), _sign);
        }

        /// <summary>A folded-plate roof: shallow folds across the frontage, glazed triangles in the folds at the front.</summary>
        public void FoldedPlate()
        {
            const float E = .6f;
            float x0 = -_x, y0 = -_y, y1 = _y;
            Block(_s.Storeys);
            int n = Math.Max(2, (int)MathF.Round(2f * _x / _r.Range(4f, 5.5f)));
            float fold = 2f * _x / n, rise = fold / 2f * MathF.Tan(_s.Pitch * MathF.PI / 180f);
            for (int i = 0; i < n; i++)
            {
                float a = x0 + (i * fold), c = x0 + ((i + .5f) * fold), b = a + fold;
                foreach (float lo in (ReadOnlySpan<float>)[a, b])
                {
                    Sheet(_w, _s.Roof, [new(lo, y0 - E, H), new(c, y0 - E, H + rise), new(c, y1 + E, H + rise), new(lo, y1 + E, H)], Vector3.UnitZ);
                    Cuboid(_w, new Vector3(lo, y0 - E - .06f, H - .25f), new Vector3(c, y0 - E, H - .05f), _s.Trim);
                }

                Shape(_w, "glass", [new(a, y0, H), new(b, y0, H), new(c, y0, H + rise)], -Vector3.UnitY);
                Shape(_w, _s.End, [new(b, y1, H), new(a, y1, H), new(c, y1, H + rise)], Vector3.UnitY);
                Vector3 across = new(0f, .05f, 0f), up = new(0f, 0f, .14f);
                _w.Beam(new Vector3(a, y0 - E - .05f, H), new Vector3(c, y0 - E - .05f, H + rise), across, up, _s.Trim);
                _w.Beam(new Vector3(c, y0 - E - .05f, H + rise), new Vector3(b, y0 - E - .05f, H), across, up, _s.Trim);
            }

            _w.Box(new Vector3(x0 + .2f, y0 - 1.6f, 2.95f), new Vector3(_x - .2f, y0, 3.1f), _s.Trim);
        }

        /// <summary>
        /// A two-part commercial block: one timber shopfront under a fascia, flats in brick behind sash
        /// windows, a corbelled cornice and a name panel on the parapet.
        /// </summary>
        public void CommercialBlock()
        {
            float x0 = -_x, x1 = _x, y0 = -_y, y1 = _y;
            Block(_s.Storeys);
            Parapet(_w, _s, x0, x1, y0, y1, H, _s.Upper, height: .9f);
            foreach ((float z0, float z1, float reach) in (ReadOnlySpan<(float, float, float)>)
                [(H - .55f, H - .4f, .1f), (H - .4f, H - .25f, .2f), (H - .25f, H - .1f, .32f), (H - .1f, H + .05f, .42f)])
            {
                _w.Box(new Vector3(x0 - reach, y0 - reach, z0), new Vector3(x1 + reach, y0, z1), _s.Trim);
                Cuboid(_w, new Vector3(x0, y0, z0), new Vector3(x0 - reach, y1, z1), _s.Trim);
                Cuboid(_w, new Vector3(x1, y0, z0), new Vector3(x1 + reach, y1, z1), _s.Trim);
            }

            float pw = MathF.Min(5f, 2f * _x * .3f);
            _w.Box(new Vector3(-pw, y0, H + .9f), new Vector3(pw, y0 + .3f, H + 1.9f), _s.Upper);
            _w.Box(new Vector3(-pw + .4f, y0 - .04f, H + 1.05f), new Vector3(pw - .4f, y0, H + 1.7f), _s.Trim);
            _w.Box(new Vector3(-pw - .05f, y0 - .05f, H + 1.9f), new Vector3(pw + .05f, y0 + .35f, H + 2f), _s.Trim);
            _w.Box(new Vector3(x0 - .05f, y0 - .06f, Storey - .02f), new Vector3(x1 + .05f, y0, Storey + .2f), _s.Trim);
        }
    }
}
