using System.Numerics;

namespace Borough.Appearance;

public static partial class FamilyBodyBuilder
{
    /// <summary>One School or Gate form's proportions, as <c>scripts/art/civic-forms.py</c> authors them in <c>LOOKS</c>.</summary>
    /// <remarks>A field a form does not use keeps its zero default.</remarks>
    internal sealed record CivicLook(
        MidriseVariant Form,
        float Bay,
        (float W, float H, float Sill) Window = default,
        float Reveal = 0f,
        float Parapet = 0f,
        float EndBay = 0f,
        float EndWindow = 0f,
        float Entrance = 0f,
        float EntranceShare = 0f,
        float Recess = 0f,
        float RecessShare = 0f,
        (float Low, float High) Ribbon = default,
        (float Low, float High) Spandrel = default,
        (float Below, float Above, float Out) Fascia = default,
        float Hall = 0f,
        float HallShare = 0f,
        float HallLeast = 0f,
        float HallRise = 0f,
        float Stack = 0f,
        float Group = 0f,
        float Step = 0f,
        float Pitch = 0f,
        float Gable = 0f,
        float Plinth = 0f,
        float Cornice = 0f,
        float Fin = 0f,
        float Canopy = 0f,
        float Pylon = 0f,
        float Column = 0f,
        float Mullion = 0f,
        (float W, float H) Portal = default,
        float Span = 0f,
        (float Most, float Share) Pavilion = default,
        float Forward = 0f,
        float Rise = 0f,
        float Tower = 0f);

    internal static CivicLook RoofPlay { get; } = new(MidriseVariant.SchoolRoofPlay, 3.4f, (2.4f, 2.1f, .8f), Reveal: .3f,
        Parapet: .5f, EndBay: 5f, EndWindow: 1.6f, Entrance: 8f, EntranceShare: .3f, Recess: 2f);

    internal static CivicLook SchoolRibbon { get; } = new(MidriseVariant.SchoolRibbon, 2.4f, Ribbon: (.95f, 2.9f),
        Spandrel: (.12f, .9f), Fascia: (.1f, .45f, .4f), Hall: 15f, HallShare: .32f, HallLeast: 30f, HallRise: 2.5f, Stack: 8f);

    internal static CivicLook BoardSchool { get; } = new(MidriseVariant.SchoolBoard, 3f, (1.3f, 2.5f, .6f), Reveal: .2f,
        Group: 15f, Step: 2f, Pitch: 45f, Gable: 9f, Plinth: .45f, Cornice: .15f);

    internal static CivicLook Interchange { get; } = new(MidriseVariant.GateInterchange, 3f, Parapet: .6f, Recess: 4f,
        RecessShare: .2f, Ribbon: (.5f, 3f), Fin: 1.2f, Canopy: 2.5f, Pylon: 1.8f, Column: 7.2f);

    internal static CivicLook GabledHall { get; } = new(MidriseVariant.GateHall, 4f, Ribbon: (1f, 2.6f), Pitch: 35f,
        Mullion: 1.6f, Portal: (9f, 4.8f), Span: 40f);

    internal static CivicLook StationHouse { get; } = new(MidriseVariant.GateStation, 3f, (1.3f, 2.3f, .7f), Reveal: .2f,
        Canopy: 2f, Pitch: 35f, Pavilion: (12f, .42f), Forward: 1f, Rise: 1.5f, Tower: 3.4f);

    internal static bool IsCivic(MidriseVariant variant) => variant is MidriseVariant.SchoolRoofPlay or MidriseVariant.SchoolRibbon
        or MidriseVariant.SchoolBoard or MidriseVariant.GateInterchange or MidriseVariant.GateHall or MidriseVariant.GateStation;

    internal static CivicLook CivicLookOf(MidriseBody body)
    {
        CivicLook look = body.Variant switch
        {
            MidriseVariant.SchoolRoofPlay => RoofPlay,
            MidriseVariant.SchoolRibbon => SchoolRibbon,
            MidriseVariant.SchoolBoard => BoardSchool,
            MidriseVariant.GateInterchange => Interchange,
            MidriseVariant.GateHall => GabledHall,
            MidriseVariant.GateStation => StationHouse,
            _ => throw new ArgumentOutOfRangeException(nameof(body)),
        };
        return look with { Bay = body.ModuleMetres ?? look.Bay };
    }

    private static void BuildCivic(Writer writer, Rect bounds, Rect[] rects, bool ring, AttachedSides attached,
        int storeys, MidriseBody shape, FamilyBodyDetail detail)
    {
        CivicLook look = CivicLookOf(shape);
        bool far = detail == FamilyBodyDetail.Far;
        var site = new CivicSite(writer, rects, bounds, ring, attached, storeys, look, far);
        switch (look.Form)
        {
            case MidriseVariant.SchoolRoofPlay: site.RoofPlay(); break;
            case MidriseVariant.SchoolRibbon: site.Ribbon(); break;
            case MidriseVariant.SchoolBoard: site.Board(); break;
            case MidriseVariant.GateInterchange: site.Interchange(); break;
            case MidriseVariant.GateHall: site.Hall(); break;
            default: site.Station(); break;
        }
    }

    private readonly record struct CivicStretch(int Wing, TowerSide Name, TowerFace Side, Role Role, float A, float B)
    {
        public bool Front => Wing == 0 && Name == TowerSide.South;
    }

    /// <summary>One School or Gate body. The signature pieces stand on the first wing's street face.</summary>
    private sealed class CivicSite(Writer w, Rect[] rects, Rect bounds, bool ring, AttachedSides attached, int storeys,
        CivicLook look, bool far)
    {
        private float Top => storeys * Storey;

        private IEnumerable<CivicStretch> Stretches(Rect[] walls)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                foreach (TowerSide name in TowerSides)
                {
                    TowerFace side = FamilyBodyBuilder.Side(walls[i], name);
                    Role role = RoleOf(name, walls[i], bounds, ring, attached);
                    foreach ((float a, float b) in OpenStretches(side, walls[i], walls)) yield return new(i, name, side, role, a, b);
                }
            }
        }

        private void FarWall(TowerFace side, float a, float b, int storey, Role role, float depth = 0f) =>
            w.GridPlane(side.Face, a, b, storey * Storey, (storey + 1) * Storey, depth, 0f, MidriseBays(b - a, look.Bay).Count,
                MidriseFarCell(true, role, storey));

        private void FarWalls(TowerFace side, float a, float b, Role role)
        {
            for (int storey = 0; storey < storeys; storey++) FarWall(side, a, b, storey, role);
        }

        /// <summary>A party stretch: far quads at far detail, otherwise one blank wall to <paramref name="top"/>.</summary>
        private void Party(TowerFace side, float a, float b, float top)
        {
            if (far) FarWalls(side, a, b, Role.Party);
            else Slab(w, side, "wall", a, b, 0f, top, 0f, MidriseWall);
        }

        private void Frame(TowerFace side, Hole o, float d)
        {
            Slab(w, side, "frame", o.U0, o.U1, o.Z0, o.Z0 + .06f, d - .08f, d);
            Slab(w, side, "frame", o.U0, o.U1, o.Z1 - .06f, o.Z1, d - .08f, d);
            Slab(w, side, "frame", o.U0, o.U0 + .06f, o.Z0, o.Z1, d - .08f, d);
            Slab(w, side, "frame", o.U1 - .06f, o.U1, o.Z0, o.Z1, d - .08f, d);
        }

        private void CivicParapet(TowerFace side, float a, float b, float height, string part = "wall")
        {
            Slab(w, side, part, a, b, Top, Top + height, 0f, MidriseWall);
            Slab(w, side, "trim", a, b, Top + height, Top + height + .08f, -.04f, MidriseWall + .04f);
        }

        private void FlatRoofs()
        {
            foreach (Rect r in rects) Box(w, new Vector3(r.X0 + .1f, r.Y0 + .1f, Top - .2f), new Vector3(r.X1 - .1f, r.Y1 - .1f, Top + .05f), "membrane");
        }

        /// <summary>A polygon in a face's plane from (u, z) points, anticlockwise seen from outside.</summary>
        private void FacePolygon(TowerFace side, string part, IEnumerable<(float U, float Z)> points, float d) =>
            w.Polygon(part, [.. points.Select(p => side.Face.Point(p.U, p.Z, d))]);

        private void Disc(TowerFace side, string part, float u, float z, float radius, float d)
        {
            const int n = 16;
            FacePolygon(side, part, Enumerable.Range(0, n).Select(j =>
                (u + (radius * MathF.Cos(2f * MathF.PI * j / n)), z + (radius * MathF.Sin(2f * MathF.PI * j / n)))), d);
        }

        /// <summary>A clock face with a dark rim and hands at ten past ten.</summary>
        private void Clock(TowerFace side, float u, float z, float radius, float d)
        {
            Disc(side, "metal", u, z, radius + .12f, d);
            Disc(side, "lobby", u, z, radius, d - .01f);
            foreach ((float angle, float length) in (ReadOnlySpan<(float, float)>)[(MathF.PI / 3f, radius * .55f), (MathF.PI * 5f / 6f, radius * .8f)])
            {
                float a = MathF.Cos(angle), b = MathF.Sin(angle);
                FacePolygon(side, "metal",
                [
                    (u - (.05f * b), z + (.05f * a)), (u + (.05f * b), z - (.05f * a)),
                    (u + (.05f * b) + (length * a), z - (.05f * a) + (length * b)),
                    (u - (.05f * b) + (length * a), z + (.05f * a) + (length * b)),
                ], d - .02f);
            }
        }

        private void Pyramid(string part, float cx, float cy, float half, float z, float height)
        {
            Vector3 apex = new(cx, cy, z + height);
            Vector3[] c = [new(cx - half, cy - half, z), new(cx + half, cy - half, z), new(cx + half, cy + half, z), new(cx - half, cy + half, z)];
            for (int k = 0; k < 4; k++) w.Polygon(part, [c[k], c[(k + 1) % 4], apex]);
        }

        /// <summary>
        /// A hipped roof over <paramref name="r"/> from eaves height <paramref name="z"/>, rising
        /// <paramref name="rise"/> metres per metre in. A party end stands upright as a gable. Returns the ridge height.
        /// </summary>
        private float CivicHip(Rect r, float z, float rise, bool gableWest, bool gableEast)
        {
            if (r.Width >= r.Depth - 1e-3f)
            {
                float h = r.Depth / 2f * rise, cy = (r.Y0 + r.Y1) / 2f;
                float ra = gableWest ? r.X0 : r.X0 + (r.Depth / 2f);
                float rb = gableEast ? r.X1 : r.X1 - (r.Depth / 2f);
                if (rb - ra > 1e-3f)
                {
                    w.Polygon("roof", [new(r.X0, r.Y0, z), new(r.X1, r.Y0, z), new(rb, cy, z + h), new(ra, cy, z + h)]);
                    w.Polygon("roof", [new(r.X1, r.Y1, z), new(r.X0, r.Y1, z), new(ra, cy, z + h), new(rb, cy, z + h)]);
                }
                else
                {
                    w.Polygon("roof", [new(r.X0, r.Y0, z), new(r.X1, r.Y0, z), new(ra, cy, z + h)]);
                    w.Polygon("roof", [new(r.X1, r.Y1, z), new(r.X0, r.Y1, z), new(ra, cy, z + h)]);
                }

                w.Polygon(gableWest ? "wall" : "roof", [new(r.X0, r.Y1, z), new(r.X0, r.Y0, z), new(ra, cy, z + h)]);
                w.Polygon(gableEast ? "wall" : "roof", [new(r.X1, r.Y0, z), new(r.X1, r.Y1, z), new(rb, cy, z + h)]);
                return z + h;
            }

            float height = r.Width / 2f * rise, cx = (r.X0 + r.X1) / 2f;
            float ya = r.Y0 + (r.Width / 2f), yb = r.Y1 - (r.Width / 2f);
            w.Polygon("roof", [new(r.X0, r.Y1, z), new(r.X0, r.Y0, z), new(cx, ya, z + height), new(cx, yb, z + height)]);
            w.Polygon("roof", [new(r.X1, r.Y0, z), new(r.X1, r.Y1, z), new(cx, yb, z + height), new(cx, ya, z + height)]);
            w.Polygon("roof", [new(r.X0, r.Y0, z), new(r.X1, r.Y0, z), new(cx, ya, z + height)]);
            w.Polygon("roof", [new(r.X1, r.Y1, z), new(r.X0, r.Y1, z), new(cx, yb, z + height)]);
            return z + height;
        }

        private static Rect Grown(Rect r, float by) => new(r.X0 - by, r.Y0 - by, r.X1 + by, r.Y1 + by);

        private (bool West, bool East) PartyEnds(Rect r) =>
            (RoleOf(TowerSide.West, r, bounds, ring, attached) == Role.Party, RoleOf(TowerSide.East, r, bounds, ring, attached) == Role.Party);

        /// <summary>Alternating long and short stone blocks up a corner, from <paramref name="u"/> inward.</summary>
        private void Quoins(TowerFace side, float u, float top, bool left)
        {
            int count = (int)MathF.Floor((top - .4f) / .7f);
            for (int k = 0; k < count; k++)
            {
                float width = k % 2 == 0 ? .6f : .4f;
                (float a, float b) = left ? (u, u + width) : (u - width, u);
                Slab(w, side, "band", a, b, k * .7f, (k * .7f) + .6f, -.05f, 0f);
            }
        }

        /// <summary>An urban school: a double-height entrance with a colored lining, deep classroom reveals and a roof playground.</summary>
        public void RoofPlay()
        {
            float d = look.Reveal + MidriseWall;
            (float ww, float wh, float sill) = look.Window;
            foreach (CivicStretch s in Stretches(rects))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                if (far)
                {
                    FarWalls(side, a, b, role);
                    if (role != Role.Party) CivicParapet(side, a, b, look.Parapet, "far-facade");
                    continue;
                }

                if (role == Role.Party)
                {
                    Slab(w, side, "wall", a, b, 0f, Top, 0f, MidriseWall);
                    continue;
                }

                float? e0 = null, e1 = null;
                if (s.Front && b - a >= 16f)
                {
                    float ew = MathF.Min(look.Entrance, (b - a) * look.EntranceShare);
                    e0 = (a + b - ew) / 2f;
                    e1 = e0 + ew;
                }

                int lifted = Math.Min(2, storeys);
                (float bay, float width) = role == Role.End ? (look.EndBay, look.EndWindow) : (look.Bay, ww);
                for (int k = 0; k < storeys; k++)
                {
                    float z = k * Storey;
                    bool entry = e0 is not null && k < lifted;
                    var holes = new List<Hole>();
                    foreach ((float u0, float u1) in MidriseBays(b - a, bay))
                    {
                        Hole o = Centred(a + u0, a + u1, width, z + sill, wh);
                        if (entry && o.U1 > e0 - .3f && o.U0 < e1 + .3f) continue;
                        holes.Add(o);
                    }

                    List<Hole> cut = entry ? [new Hole(e0!.Value, e1!.Value, z, z + Storey, null)] : [];
                    WallWithHoles(w, side, "wall", a, b, z, z + Storey, [.. holes, .. cut], 0f, d);
                    foreach (Hole o in holes) Frame(side, o, d);
                }

                if (e0 is { } f0 && e1 is { } f1)
                {
                    float eh = lifted * Storey, r = look.Recess, c = (f0 + f1) / 2f;
                    Plane(w, side.Face, "glass", f0, f1, 0f, eh, r);
                    Slab(w, side, "accent", f0, f0 + .1f, 0f, eh, 0f, r);
                    Slab(w, side, "accent", f1 - .1f, f1, 0f, eh, 0f, r);
                    Slab(w, side, "accent", f0, f1, eh - .1f, eh, 0f, r);
                    Plane(w, side.Face, "door", c - 1.2f, c + 1.2f, 0f, 2.6f, r - .02f);
                    w.StreetDoor(side.Face, c - 1.2f, c + 1.2f, 0f, r - .02f);
                    if (eh + 1f < Top) Slab(w, side, "band", f0 + .4f, f1 - .4f, eh + .25f, eh + .85f, -.06f, 0f);
                }

                CivicParapet(side, a, b, look.Parapet);
            }

            FlatRoofs();
            Playground(rects[0], Top);
        }

        /// <summary>A fenced play deck: a court with hoops, a shade canopy and a stair overrun.</summary>
        private void Playground(Rect r, float z)
        {
            if (r.Width < 16f || r.Depth < 12f) return;
            Box(w, new Vector3(r.X0 + .3f, r.Y0 + .3f, z + .05f), new Vector3(r.X1 - .3f, r.Y1 - .3f, z + .1f), "paving");
            float zc = z + .1f;
            float cw = MathF.Min(28f, r.Width - 12f), cd = MathF.Min(15f, r.Depth - 3f), cy = (r.Y0 + r.Y1) / 2f;
            float cx0 = r.X0 + 1.5f, cx1 = cx0 + cw;
            if (cw >= 8f)
            {
                Box(w, new Vector3(cx0, cy - (cd / 2f), zc), new Vector3(cx1, cy + (cd / 2f), zc + .03f), "court");
                if (!far)
                {
                    float mid = (cx0 + cx1) / 2f;
                    ((float, float), (float, float))[] lines =
                    [
                        ((cx0, cy - (cd / 2f)), (cx1, cy - (cd / 2f) + .08f)), ((cx0, cy + (cd / 2f) - .08f), (cx1, cy + (cd / 2f))),
                        ((cx0, cy - (cd / 2f)), (cx0 + .08f, cy + (cd / 2f))), ((cx1 - .08f, cy - (cd / 2f)), (cx1, cy + (cd / 2f))),
                        ((mid - .04f, cy - (cd / 2f)), (mid + .04f, cy + (cd / 2f))),
                    ];
                    foreach (((float ax, float ay), (float bx, float by)) in lines)
                    {
                        Box(w, new Vector3(ax, ay, zc + .03f), new Vector3(bx, by, zc + .045f), "band");
                    }

                    foreach ((float post, float board) in (ReadOnlySpan<(float, float)>)[(cx0 + .6f, cx0 + .7f), (cx1 - .6f, cx1 - .7f)])
                    {
                        Box(w, new Vector3(post - .08f, cy - .08f, zc), new Vector3(post + .08f, cy + .08f, zc + 2.6f), "metal");
                        Box(w, new Vector3(board - .03f, cy - .9f, zc + 2.4f), new Vector3(board + .03f, cy + .9f, zc + 3.25f), "band");
                    }
                }
            }

            if (!far)
            {
                (float X, float Y)[] corners = [(r.X0 + .3f, r.Y0 + .3f), (r.X1 - .3f, r.Y0 + .3f), (r.X1 - .3f, r.Y1 - .3f), (r.X0 + .3f, r.Y1 - .3f)];
                for (int k = 0; k < 4; k++)
                {
                    ((float ax, float ay), (float bx, float by)) = (corners[k], corners[(k + 1) % 4]);
                    int n = Math.Max(1, RoundStoreys(MathF.Sqrt(((bx - ax) * (bx - ax)) + ((by - ay) * (by - ay))) / 3f));
                    for (int j = 0; j < n; j++)
                    {
                        float px = ax + ((bx - ax) * j / n), py = ay + ((by - ay) * j / n);
                        Box(w, new Vector3(px - .05f, py - .05f, zc), new Vector3(px + .05f, py + .05f, zc + 3.2f), "metal");
                    }

                    foreach (float h in (ReadOnlySpan<float>)[1.1f, 2.2f, 3.14f])
                    {
                        Box(w, new Vector3(MathF.Min(ax, bx) - .02f, MathF.Min(ay, by) - .02f, zc + h),
                            new Vector3(MathF.Max(ax, bx) + .02f, MathF.Max(ay, by) + .02f, zc + h + .06f), "metal");
                    }
                }
            }

            float sx = cw >= 8f ? cx1 + 1f : r.X0 + 1.5f;
            float sw = MathF.Min(9f, r.X1 - 5.5f - sx);
            if (sw >= 3f)
            {
                if (!far)
                {
                    foreach (float px in (ReadOnlySpan<float>)[sx, sx + sw])
                    {
                        foreach (float py in (ReadOnlySpan<float>)[r.Y0 + 1.5f, r.Y1 - 1.5f])
                        {
                            Box(w, new Vector3(px - .08f, py - .08f, zc), new Vector3(px + .08f, py + .08f, zc + 2.8f), "metal");
                        }
                    }
                }

                Box(w, new Vector3(sx - .4f, r.Y0 + 1.1f, zc + 2.8f), new Vector3(sx + sw + .4f, r.Y1 - 1.1f, zc + 2.95f), "canopy");
            }

            Box(w, new Vector3(r.X1 - 4.5f, r.Y1 - 6f, zc), new Vector3(r.X1 - .5f, r.Y1 - .5f, zc + 3.1f), "accent");
            Box(w, new Vector3(r.X1 - 4.6f, r.Y1 - 6.1f, zc + 3.1f), new Vector3(r.X1 - .4f, r.Y1 - .4f, zc + 3.25f), "metal");
        }

        /// <summary>The ribbon school's hall on the first wing's faces, as a u interval.</summary>
        private static (float A, float B)? HallSpan(int wing, TowerSide name, float length, float hw)
        {
            if (wing != 0 || hw == 0f) return null;
            return name switch
            {
                TowerSide.South => (length - hw, length),
                TowerSide.North => (0f, hw),
                TowerSide.East => (0f, length),
                _ => null,
            };
        }

        private void RibbonWall(TowerFace side, float a, float b, Role role, bool front)
        {
            (float lo, float hi) = look.Ribbon;
            (float s0, float s1) = look.Spandrel;
            if (role == Role.End)
            {
                Slab(w, side, "wall-end", a, b, 0f, Top, 0f, MidriseWall);
                return;
            }

            List<(float U0, float U1)> modules = MidriseBays(b - a, look.Bay);
            for (int k = 0; k < storeys; k++)
            {
                float z = k * Storey;
                Slab(w, side, "trim", a, b, z, z + s0, -.03f, MidriseWall);
                for (int j = 0; j < modules.Count; j++)
                {
                    Slab(w, side, (j + k) % 2 == 0 ? "spandrel" : "panel", a + modules[j].U0, a + modules[j].U1, z + s0, z + s1, -.03f, MidriseWall);
                }

                Slab(w, side, "trim", a, b, z + s1, z + lo, -.05f, MidriseWall);
                Plane(w, side.Face, "glass", a, b, z + lo, z + hi, .12f);
                Slab(w, side, "wall", a, b, z + hi, z + Storey, 0f, MidriseWall);
            }

            int count = modules.Count;
            for (int j = 0; j <= count; j++)
            {
                float u = MathF.Min(MathF.Max(a + ((b - a) * j / count) - .09f, a), b - .18f);
                Slab(w, side, "trim", u, u + .18f, 0f, Top, -.08f, 0f);
            }

            if (front && b - a >= 8f)
            {
                float c = (a + b) / 2f;
                Plane(w, side.Face, "door", c - 1.2f, c + 1.2f, 0f, 2.6f, -.06f);
                w.StreetDoor(side.Face, c - 1.2f, c + 1.2f, 0f, -.06f);
                Slab(w, side, "canopy", c - 3.3f, c + 3.3f, 2.95f, 3.15f, -3f, 0f);
                foreach (float u in (ReadOnlySpan<float>)[c - 3f, c + 3f]) Slab(w, side, "metal", u - .06f, u + .06f, 0f, 2.95f, -2.8f, -2.68f);
            }
        }

        /// <summary>
        /// A light-frame school: ribbon windows over colored spandrel panels between slim white columns, a deep
        /// white roof edge, a taller hall with a clerestory, and a brick boiler stack.
        /// </summary>
        public void Ribbon()
        {
            Rect first = rects[0];
            float hw = !ring && first.Width >= look.HallLeast ? MathF.Min(look.Hall, first.Width * look.HallShare) : 0f;
            float hh = Top + look.HallRise;
            (float f0, float f1, float outward) = look.Fascia;
            foreach (CivicStretch s in Stretches(rects))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                List<(float P, float Q, bool InHall)> pieces = [(a, b, false)];
                if (HallSpan(s.Wing, s.Name, side.Length, hw) is { } span)
                {
                    (float P, float Q, bool InHall)[] split =
                        [(a, MathF.Min(b, span.A), false), (MathF.Max(a, span.A), MathF.Min(b, span.B), true), (MathF.Max(a, span.B), b, false)];
                    pieces = [.. split.Where(piece => piece.Q - piece.P > 1e-4f)];
                }

                foreach ((float p, float q, bool inHall) in pieces)
                {
                    if (role == Role.Party)
                    {
                        if (far && !inHall) FarWalls(side, p, q, role);
                        else Slab(w, side, "wall", p, q, 0f, inHall ? hh : Top, 0f, MidriseWall);
                        continue;
                    }

                    if (far)
                    {
                        FarWalls(side, p, q, role);
                        if (inHall) Slab(w, side, "wall-end", p, q, Top, hh, 0f, MidriseWall);
                    }
                    else if (inHall)
                    {
                        List<Hole> holes = q - p > 2f ? [new Hole(p + .5f, q - .5f, hh - 2.3f, hh - .7f)] : [];
                        WallWithHoles(w, side, "wall-end", p, q, 0f, hh, holes, 0f, MidriseWall);
                    }
                    else
                    {
                        RibbonWall(side, p, q, role, s.Front);
                    }

                    if (inHall)
                    {
                        float ext = role == Role.End ? .3f : 0f;
                        Slab(w, side, "trim", p - ext, q + ext, hh - .1f, hh + .35f, -.3f, .3f);
                    }
                    else
                    {
                        float ext = role == Role.End ? outward : 0f;
                        Slab(w, side, "trim", p - ext, q + ext, Top - f0, Top + f1, -outward, outward);
                    }
                }
            }

            FlatRoofs();
            if (hw > 0f)
            {
                Rect hall = new(first.X1 - hw, first.Y0, first.X1, first.Y1);
                TowerFace side = FamilyBodyBuilder.Side(hall, TowerSide.West);
                Slab(w, side, "wall-end", 0f, side.Length, Top, hh, 0f, MidriseWall);
                Slab(w, side, "trim", 0f, side.Length, hh - .1f, hh + .35f, -.3f, .3f);
                Box(w, new Vector3(hall.X0 + .1f, first.Y0 + .1f, hh - .2f), new Vector3(first.X1 - .1f, first.Y1 - .1f, hh + .05f), "membrane");
            }

            float sx = hw > 0f ? first.X1 - (hw / 2f) : first.X1 - 3f;
            Box(w, new Vector3(sx - .75f, first.Y1 - 1.7f, Top - .2f), new Vector3(sx + .75f, first.Y1 - .2f, Top + look.Stack), "wall-end");
            Box(w, new Vector3(sx - .85f, first.Y1 - 1.8f, Top + look.Stack), new Vector3(sx + .85f, first.Y1 - .1f, Top + look.Stack + .3f), "trim");
        }

        /// <summary>
        /// A board school: tall brick storeys, a steep hipped roof, a gable over each group of tall windows,
        /// stone bands, two entrances, and a bell cupola on the ridge.
        /// </summary>
        public void Board()
        {
            float rise = MathF.Tan(look.Pitch * MathF.PI / 180f);
            float d = look.Reveal + MidriseWall;
            (float ww, float wh, float sill) = look.Window;
            Rect first = rects[0];
            int groups = Math.Max(1, RoundStoreys(first.Width / look.Group));
            float gw = MathF.Min(look.Gable, (first.Width / groups) - 3f);
            float c0 = look.Cornice;
            foreach (CivicStretch s in Stretches(rects))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                if (role == Role.Party)
                {
                    Party(side, a, b, Top);
                    continue;
                }

                float ext = role == Role.End ? c0 : 0f;
                Slab(w, side, "band", a - ext, b + ext, Top - .3f, Top, -c0, 0f);
                if (far)
                {
                    FarWalls(side, a, b, role);
                    continue;
                }

                List<float> doors = [];
                List<float> centres = [];
                if (s.Front)
                {
                    centres = [.. Enumerable.Range(0, groups).Select(k => a + ((b - a) * (k + .5f) / groups))];
                    doors = b - a > 20f ? [a + 1.6f, b - 3.4f] : [a + 1.6f];
                }

                for (int k = 0; k < storeys; k++)
                {
                    float z = k * Storey;
                    List<Hole> windows = s.Front
                        ? [.. centres.SelectMany(c => ((int[])[-1, 0, 1]).Select(j =>
                            new Hole(c + (j * look.Step) - (ww / 2f), c + (j * look.Step) + (ww / 2f), z + sill, z + sill + wh)))]
                        : [.. MidriseBays(b - a, look.Bay).Select(bay => Centred(a + bay.U0, a + bay.U1, ww, z + sill, wh))];
                    List<Hole> entries = [];
                    if (k == 0)
                    {
                        windows = [.. windows.Where(o => !doors.Any(q => o.U1 > q - .3f && o.U0 < q + 2.1f))];
                        entries = [.. doors.Select(q => new Hole(q, q + 1.8f, look.Plinth, 3f, "door"))];
                    }

                    WallWithHoles(w, side, "wall", a, b, z, z + Storey, [.. windows, .. entries], 0f, d);
                    foreach (Hole o in windows)
                    {
                        Slab(w, side, "band", o.U0 - .12f, o.U1 + .12f, o.Z1, o.Z1 + .3f, -.05f, 0f);
                        Slab(w, side, "frame", o.U0, o.U1, o.Z0 + (wh * .7f) - .04f, o.Z0 + (wh * .7f) + .04f, d - .07f, d - .02f);
                    }
                }

                foreach (float q in doors)
                {
                    Slab(w, side, "band", q - .3f, q, look.Plinth, 3.45f, -.12f, 0f);
                    Slab(w, side, "band", q + 1.8f, q + 2.1f, look.Plinth, 3.45f, -.12f, 0f);
                    Slab(w, side, "band", q - .3f, q + 2.1f, 3f, 3.45f, -.12f, 0f);
                    Slab(w, side, "band", q - .5f, q + 2.3f, 3.45f, 3.7f, -.4f, 0f);
                }

                for (int k = 1; k < storeys; k++) Slab(w, side, "band", a, b, (k * Storey) - .05f, (k * Storey) + .2f, -.06f, 0f);
                Slab(w, side, "band", a, b, 0f, look.Plinth, -.08f, 0f);
            }

            float ridge = Top;
            for (int k = 0; k < rects.Length; k++)
            {
                (bool west, bool east) = PartyEnds(rects[k]);
                float z = CivicHip(Grown(rects[k], c0), Top, rise, west, east);
                if (k == 0) ridge = z;
            }

            Rect roof = Grown(first, c0);
            if (roof.Width < roof.Depth) return;
            if (gw >= 4f)
            {
                TowerFace front = FamilyBodyBuilder.Side(first, TowerSide.South);
                float peak = gw / 2f * rise, run = (gw / 2f) + .3f;
                for (int k = 0; k < groups; k++)
                {
                    float c = first.X0 + (first.Width * (k + .5f) / groups);
                    float y = roof.Y0;
                    w.Polygon("roof", [new(c - (gw / 2f), y, Top), new(c, y, Top + peak), new(c, y + run, Top + peak), new(c - (gw / 2f), y + run, Top)]);
                    w.Polygon("roof", [new(c, y, Top + peak), new(c + (gw / 2f), y, Top), new(c + (gw / 2f), y + run, Top), new(c, y + run, Top + peak)]);
                    w.Polygon("wall", [new(c - (gw / 2f), y, Top), new(c + (gw / 2f), y, Top), new(c, y, Top + peak)]);
                    if (far) continue;
                    float u = c - first.X0;
                    for (int j = -1; j <= 1; j++)
                    {
                        float height = MathF.Min(peak * .55f, 3.2f) - (j != 0 ? .5f : 0f);
                        if (height <= .8f) continue;
                        Slab(w, front, "glass", u + (j * 1.3f) - .45f, u + (j * 1.3f) + .45f, Top + .5f, Top + .5f + height, -c0 - .02f, -c0);
                        Slab(w, front, "band", u + (j * 1.3f) - .55f, u + (j * 1.3f) + .55f, Top + .5f + height, Top + .7f + height, -c0 - .05f, -c0);
                    }

                    Slab(w, front, "band", u - (gw / 2f), u + (gw / 2f), Top, Top + .2f, -c0 - .06f, -c0);
                }
            }

            float bx = (first.X0 + first.X1) / 2f, by = (first.Y0 + first.Y1) / 2f;
            float zr = ridge - .4f;
            Box(w, new Vector3(bx - 1.1f, by - 1.1f, zr - .6f), new Vector3(bx + 1.1f, by + 1.1f, zr + .1f), "trim");
            Box(w, new Vector3(bx - .9f, by - .9f, zr), new Vector3(bx + .9f, by + .9f, zr + 2.4f), "trim");
            if (!far) Box(w, new Vector3(bx - .55f, by - .95f, zr + .5f), new Vector3(bx + .55f, by - .9f, zr + 2f), "metal");
            Box(w, new Vector3(bx - 1.05f, by - 1.05f, zr + 2.4f), new Vector3(bx + 1.05f, by + 1.05f, zr + 2.6f), "trim");
            Pyramid("roof", bx, by, 1.05f, zr + 2.6f, 1.6f);
            Box(w, new Vector3(bx - .04f, by - .04f, zr + 4.2f), new Vector3(bx + .04f, by + .04f, zr + 5f), "metal");
            float reach = (roof.Width - roof.Depth) / 2f;
            if (reach <= 2f) return;
            float offset = MathF.Min(first.Width * .3f, reach - 1f);
            foreach (float sx in (ReadOnlySpan<float>)[bx - offset, bx + offset])
            {
                Box(w, new Vector3(sx - .6f, by - .5f, ridge - 1.5f), new Vector3(sx + .6f, by + .5f, ridge + 1.6f), "wall");
                Box(w, new Vector3(sx - .7f, by - .6f, ridge + 1.6f), new Vector3(sx + .7f, by + .6f, ridge + 1.8f), "band");
            }
        }

        /// <summary>
        /// A transit interchange: a glazed concourse set back under the upper storeys, a thin canopy, fins over
        /// ribbon windows, and a clock pylon at one corner.
        /// </summary>
        public void Interchange()
        {
            Rect first = rects[0];
            float r = MathF.Min(look.Recess, first.Depth * look.RecessShare);
            (float lo, float hi) = look.Ribbon;
            foreach (CivicStretch s in Stretches(rects))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                if (role == Role.Party)
                {
                    Party(side, a, b, Top);
                    continue;
                }

                float ga = a, gb = b;
                if (s.Wing == 0 && s.Name == TowerSide.East) ga = MathF.Max(a, r);
                if (s.Wing == 0 && s.Name == TowerSide.West) gb = MathF.Min(b, side.Length - r);
                if (far)
                {
                    if (s.Front) FarWall(side, a, b, 0, role, r);
                    else if (gb - ga > 1e-3f) FarWall(side, ga, gb, 0, role);
                    for (int k = 1; k < storeys; k++) FarWall(side, a, b, k, role);
                }
                else
                {
                    if (s.Front)
                    {
                        Slab(w, side, "metal", a, b, 0f, .3f, r, r + MidriseWall);
                        Plane(w, side.Face, "glass", a, b, .3f, Storey - .3f, r + .1f);
                        foreach ((float U0, float U1) bay in MidriseBays(b - a, look.Bay).Skip(1))
                        {
                            Slab(w, side, "frame", a + bay.U0 - .04f, a + bay.U0 + .04f, .3f, Storey - .3f, r, r + .1f);
                        }

                        Slab(w, side, "metal", a, b, Storey - .3f, Storey, r, r + MidriseWall);
                        Slab(w, side, "trim", a, b, Storey - .2f, Storey, 0f, r);
                        List<(float U0, float U1)> spans = MidriseBays(b - a, look.Column);
                        foreach (float u in spans.Skip(1).Select(span => a + span.U0).Append(b - .3f))
                        {
                            Slab(w, side, "metal", u - .15f, u + .15f, 0f, Storey - .2f, .2f, .5f);
                        }

                        (float m0, float m1) = spans[spans.Count / 2];
                        float c = a + ((m0 + m1) / 2f);
                        Plane(w, side.Face, "door", c - 1.6f, c + 1.6f, 0f, 2.7f, r - .02f);
                        w.StreetDoor(side.Face, c - 1.6f, c + 1.6f, 0f, r - .02f);
                    }
                    else if (gb - ga > 1e-3f)
                    {
                        List<Hole> holes = [.. MidriseBays(gb - ga, 4f).Select(bay => Centred(ga + bay.U0, ga + bay.U1, 2.4f, .6f, 2.2f))];
                        WallWithHoles(w, side, "wall-end", ga, gb, 0f, Storey, holes, 0f, .1f + MidriseWall);
                    }

                    for (int k = 1; k < storeys; k++)
                    {
                        float z = k * Storey;
                        WallWithHoles(w, side, "wall", a, b, z, z + Storey, [new Hole(a + .4f, b - .4f, z + lo, z + hi)], 0f, .15f + MidriseWall);
                    }

                    if (role != Role.End && storeys > 1)
                    {
                        int n = Math.Max(1, RoundStoreys((b - a) / look.Fin));
                        for (int j = 0; j <= n; j++)
                        {
                            float u = a + ((b - a) * j / n);
                            Slab(w, side, "metal", MathF.Max(a, u - .06f), MathF.Min(b, u + .06f), Storey, Top, -.5f, 0f);
                        }
                    }
                }

                CivicParapet(side, a, b, look.Parapet, far ? "far-facade" : "wall");
                if (!s.Front) continue;
                Slab(w, side, "canopy", a, b, Storey - .05f, Storey + .3f, -look.Canopy, 0f);
                Slab(w, side, "metal", a, b, Storey + .05f, Storey + .4f, -look.Canopy - .05f, -look.Canopy + .1f);
                float p0 = a + .3f, p1 = a + .3f + look.Pylon;
                Slab(w, side, "accent", p0, p1, 0f, Top + 7f, -.6f, 1.2f);
                if (far) continue;
                Clock(side, (p0 + p1) / 2f, Top + 5.4f, .7f, -.61f);
                Slab(w, side, "band", p0 + .2f, p1 - .2f, Top + 2.2f, Top + 4.2f, -.64f, -.6f);
            }

            FlatRoofs();
        }

        private static (float P, float Q)[] GableSpans(float length, float most)
        {
            int n = length > most ? 2 : 1;
            return [.. Enumerable.Range(0, n).Select(k => (length * k / n, length * (k + 1) / n))];
        }

        /// <summary>
        /// A great gabled hall: one or two steep gables to the street, glazed from the concourse floor to the
        /// ridge, timber sides with ribbon windows, and a deep portal around the doors.
        /// </summary>
        public void Hall()
        {
            float rise = MathF.Tan(look.Pitch * MathF.PI / 180f);
            (float lo, float hi) = look.Ribbon;
            float d = .1f + MidriseWall;
            foreach (CivicStretch s in Stretches(rects))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                if (role == Role.Party)
                {
                    Party(side, a, b, Top);
                    continue;
                }

                if (role == Role.End) Slab(w, side, "trim", a, b, Top - .25f, Top, -.25f, 0f);
                if (far)
                {
                    FarWalls(side, a, b, role);
                    continue;
                }

                if (s.Front)
                {
                    (float P, float Q)[] spans = [.. GableSpans(b - a, look.Span).Select(g => (a + g.P, a + g.Q))];
                    WallWithHoles(w, side, "wall", a, b, 0f, Top, [.. spans.Select(g => new Hole(g.P + .5f, g.Q - .5f, .3f, Top - .6f))], 0f, d);
                    foreach ((float p, float q) in spans)
                    {
                        float gw = q - p, c = (p + q) / 2f;
                        int m = Math.Max(2, (int)(gw / look.Mullion));
                        for (int j = 1; j < m; j++)
                        {
                            float u = p + (gw * j / m);
                            Slab(w, side, "frame", u - .07f, u + .07f, .3f, Top + MathF.Max(0f, (((gw / 2f) - MathF.Abs(u - c)) * rise) - .4f), -.14f, -.02f);
                        }

                        for (int k = 1; k <= storeys; k++)
                        {
                            float z = k < storeys ? k * Storey : Top - .1f;
                            Slab(w, side, "frame", p + .4f, q - .4f, z, z + .14f, -.14f, -.02f);
                        }

                        float peak = gw / 2f * rise;
                        Clock(side, c, Top + (peak * .55f), MathF.Min(1.1f, peak * .18f), -.15f);
                    }

                    (float pw, float ph) = look.Portal;
                    pw = MathF.Min(pw, (spans[0].Q - spans[0].P) * .5f);
                    float pc = (a + b) / 2f;
                    Slab(w, side, "accent", pc - (pw / 2f), pc - (pw / 2f) + .5f, 0f, ph - .2f, -1.2f, 0f);
                    Slab(w, side, "accent", pc + (pw / 2f) - .5f, pc + (pw / 2f), 0f, ph - .2f, -1.2f, 0f);
                    Slab(w, side, "accent", pc - (pw / 2f), pc + (pw / 2f), ph - .6f, ph, -1.2f, 0f);
                    Plane(w, side.Face, "door", pc - 1.6f, pc + 1.6f, 0f, 2.8f, -.03f);
                    w.StreetDoor(side.Face, pc - 1.6f, pc + 1.6f, 0f, -.03f);
                }
                else if (role == Role.End)
                {
                    for (int k = 0; k < storeys; k++)
                    {
                        float z = k * Storey;
                        List<Hole> holes = b - a > 2f ? [new Hole(a + .6f, b - .6f, z + lo, z + hi)] : [];
                        WallWithHoles(w, side, "wall", a, b, z, z + Storey, holes, 0f, d);
                    }
                }
                else
                {
                    for (int k = 0; k < storeys; k++)
                    {
                        float z = k * Storey;
                        List<Hole> holes = [.. MidriseBays(b - a, look.Bay).Select(bay => Centred(a + bay.U0, a + bay.U1, 2f, z + .9f, 1.8f))];
                        WallWithHoles(w, side, "wall", a, b, z, z + Storey, holes, 0f, d);
                    }
                }
            }

            for (int k = 0; k < rects.Length; k++)
            {
                Rect r = rects[k];
                bool alongX = !ring || r.Width >= r.Depth;
                float start = alongX ? r.X0 : r.Y0, end = alongX ? r.X1 : r.Y1;
                foreach ((float g0, float g1) in GableSpans(end - start, look.Span))
                {
                    float p = start + g0, q = start + g1;
                    float c = (p + q) / 2f, h = Top + ((q - p) / 2f * rise);
                    if (alongX)
                    {
                        w.Polygon("roof", [new(p, r.Y0, Top), new(c, r.Y0, h), new(c, r.Y1, h), new(p, r.Y1, Top)]);
                        w.Polygon("roof", [new(c, r.Y0, h), new(q, r.Y0, Top), new(q, r.Y1, Top), new(c, r.Y1, h)]);
                        w.Polygon("wall", [new(p, r.Y0, Top), new(q, r.Y0, Top), new(c, r.Y0, h)]);
                        w.Polygon("wall", [new(q, r.Y1, Top), new(p, r.Y1, Top), new(c, r.Y1, h)]);
                        if (k == 0 && !far)
                        {
                            float y = r.Y0 - .01f;
                            w.Polygon("glass", [new(p + .4f, y, Top), new(q - .4f, y, Top), new(c, y, h - .4f)]);
                        }
                    }
                    else
                    {
                        w.Polygon("roof", [new(r.X0, p, Top), new(r.X1, p, Top), new(r.X1, c, h), new(r.X0, c, h)]);
                        w.Polygon("roof", [new(r.X1, q, Top), new(r.X0, q, Top), new(r.X0, c, h), new(r.X1, c, h)]);
                        w.Polygon("wall", [new(r.X0, q, Top), new(r.X0, p, Top), new(r.X0, c, h)]);
                        w.Polygon("wall", [new(r.X1, p, Top), new(r.X1, q, Top), new(r.X1, c, h)]);
                    }
                }
            }
        }

        private void StationWall(TowerFace side, float p, float q, bool quoinLeft, bool quoinRight)
        {
            float d = look.Reveal + MidriseWall;
            (float ww, float wh, float sill) = look.Window;
            for (int k = 0; k < storeys; k++)
            {
                float z = k * Storey;
                List<Hole> windows = [.. MidriseBays(q - p, look.Bay).Select(bay => Centred(p + bay.U0, p + bay.U1, ww, z + sill, wh))];
                WallWithHoles(w, side, "wall", p, q, z, z + Storey, windows, 0f, d);
                foreach (Hole o in windows)
                {
                    float mid = (o.U0 + o.U1) / 2f;
                    Slab(w, side, "band", o.U0 - .15f, o.U1 + .15f, o.Z1, o.Z1 + .32f, -.05f, 0f);
                    Slab(w, side, "band", mid - .14f, mid + .14f, o.Z1 - .05f, o.Z1 + .42f, -.09f, 0f);
                }
            }

            if (quoinLeft) Quoins(side, p, Top, true);
            if (quoinRight) Quoins(side, q, Top, false);
        }

        /// <summary>
        /// A station head-house: brick wings under a hipped roof, a centre pavilion with a tall arched entrance,
        /// a clock tower with a pyramid roof, stone quoins and dressings, and glazed iron canopies on columns.
        /// </summary>
        public void Station()
        {
            float rise = MathF.Tan(look.Pitch * MathF.PI / 180f);
            float forward = ring ? 0f : look.Forward;
            Rect first = rects[0];
            Rect[] walls = ring ? rects : [first with { Y0 = first.Y0 + forward }];
            float cx = (first.X0 + first.X1) / 2f;
            float cw = MathF.Min(look.Pavilion.Most, first.Width * look.Pavilion.Share);
            float deep = MathF.Min(6f, walls[0].Depth - 2f);
            Rect pavilion = new(cx - (cw / 2f), first.Y0, cx + (cw / 2f), walls[0].Y0 + deep);
            float ph = Top + look.Rise;
            (float s0, float s1) = (pavilion.X0 - first.X0, pavilion.X1 - first.X0);
            foreach (CivicStretch s in Stretches(walls))
            {
                (TowerFace side, Role role, float a, float b) = (s.Side, s.Role, s.A, s.B);
                if (role == Role.Party)
                {
                    Party(side, a, b, Top);
                    continue;
                }

                float ext = role == Role.End ? .15f : 0f;
                Slab(w, side, "band", a - ext, b + ext, Top - .3f, Top, -.15f, 0f);
                if (far)
                {
                    FarWalls(side, a, b, role);
                    continue;
                }

                if (!s.Front)
                {
                    StationWall(side, a, b, true, true);
                    continue;
                }

                foreach ((float p, float q, bool left, bool right) in (ReadOnlySpan<(float, float, bool, bool)>)[(a, MathF.Min(b, s0), true, false), (MathF.Max(a, s1), b, false, true)])
                {
                    if (q - p < 1e-3f) continue;
                    StationWall(side, p, q, left, right);
                    float ua = p + .4f, ub = q - .4f;
                    if (ub - ua < 3f) continue;
                    float c0 = look.Canopy;
                    w.Polygon("glass", [side.Face.Point(ua, 3.5f, -c0), side.Face.Point(ub, 3.5f, -c0), side.Face.Point(ub, 4.1f, 0f), side.Face.Point(ua, 4.1f, 0f)]);
                    Slab(w, side, "canopy", ua, ub, 3.15f, 3.55f, -c0 - .05f, -c0 + .05f);
                    int cols = Math.Max(1, RoundStoreys((ub - ua) / 4f));
                    for (int j = 0; j <= cols; j++)
                    {
                        float u = ua + .2f + ((ub - ua - .4f) * j / cols);
                        Slab(w, side, "canopy", u - .1f, u + .1f, 0f, 3.5f, -c0 + .1f, -c0 + .3f);
                    }
                }
            }

            float ridge = Top;
            for (int k = 0; k < walls.Length; k++)
            {
                (bool west, bool east) = PartyEnds(walls[k]);
                float z = CivicHip(Grown(walls[k], .15f), Top, rise, west, east);
                if (k == 0) ridge = z;
            }

            if (far) Box(w, new Vector3(pavilion.X0, pavilion.Y0, 0f), new Vector3(pavilion.X1, pavilion.Y1, ph), "wall");
            else Pavilion(pavilion, cw, ph, forward);
            Box(w, new Vector3(pavilion.X0 - .1f, pavilion.Y0 - .1f, ph - .05f), new Vector3(pavilion.X1 + .1f, pavilion.Y1, ph + .25f), "roof");
            float tw = look.Tower;
            Rect tower = new(cx - (tw / 2f), pavilion.Y0 + .4f, cx + (tw / 2f), pavilion.Y0 + .4f + tw);
            float th = MathF.Max(ridge + 3f, ph + 6.5f);
            float ty = (tower.Y0 + tower.Y1) / 2f;
            Box(w, new Vector3(tower.X0, tower.Y0, ph), new Vector3(tower.X1, tower.Y1, th), "wall");
            if (!far) Clock(FamilyBodyBuilder.Side(tower, TowerSide.South), tw / 2f, th - 1.9f, 1.1f, -.01f);
            Box(w, new Vector3(tower.X0 - .25f, tower.Y0 - .25f, th), new Vector3(tower.X1 + .25f, tower.Y1 + .25f, th + .35f), "band");
            Pyramid("roof", cx, ty, (tw / 2f) + .25f, th + .35f, 3.2f);
            Box(w, new Vector3(cx - .05f, ty - .05f, th + 3.5f), new Vector3(cx + .05f, ty + .05f, th + 4.6f), "metal");
        }

        /// <summary>The station's centre pavilion: an arched entrance under voussoirs, upper windows and quoined corners.</summary>
        private void Pavilion(Rect pavilion, float cw, float ph, float forward)
        {
            TowerFace front = FamilyBodyBuilder.Side(pavilion, TowerSide.South);
            float rad = MathF.Min(2.6f, cw * .22f);
            float spring = MathF.Min(2f * Storey, ph - rad - 1.2f);
            float c = cw / 2f;
            float d = look.Reveal + MidriseWall;
            List<Hole> uppers = storeys > 2
                ? [Centred(1f, 2.2f, 1.2f, spring + .8f, 2.2f), Centred(cw - 2.2f, cw - 1f, 1.2f, spring + .8f, 2.2f)]
                : [];
            WallWithHoles(w, front, "wall", 0f, cw, 0f, ph, [new Hole(c - rad, c + rad, 0f, spring + rad, null), .. uppers], 0f, d);
            foreach (Hole o in uppers) Slab(w, front, "band", o.U0 - .15f, o.U1 + .15f, o.Z1, o.Z1 + .32f, -.05f, 0f);

            const int n = 12;
            (float X, float Y)[] arc = [.. Enumerable.Range(0, n + 1).Select(j => (MathF.Cos(MathF.PI * j / n), MathF.Sin(MathF.PI * j / n)))];
            (float, float) At((float X, float Y) p, float radius) => (c + (radius * p.X), spring + (radius * p.Y));
            FacePolygon(front, "wall", [(c - rad, spring + rad), (c - rad, spring), .. Enumerable.Reverse(arc[((n / 2) + 1)..n]).Select(p => At(p, rad)), (c, spring + rad)], 0f);
            FacePolygon(front, "wall", [(c + rad, spring + rad), (c, spring + rad), .. Enumerable.Reverse(arc[1..(n / 2)]).Select(p => At(p, rad)), (c + rad, spring)], 0f);
            Plane(w, front.Face, "glass", c - rad, c + rad, 0f, spring, d);
            FacePolygon(front, "glass", arc.Select(p => At(p, rad)), d);
            float outer = rad + .45f;
            for (int j = 0; j < n; j++)
            {
                FacePolygon(front, "band", [At(arc[j], rad), At(arc[j], outer), At(arc[j + 1], outer), At(arc[j + 1], rad)], -.06f);
            }

            Plane(w, front.Face, "door", c - 1.4f, c + 1.4f, 0f, 3f, d - .02f);
            w.StreetDoor(front.Face, c - 1.4f, c + 1.4f, 0f, d - .02f);
            Slab(w, front, "band", -.2f, cw + .2f, ph - .4f, ph, -.25f, 0f);
            Quoins(front, 0f, ph, true);
            Quoins(front, cw, ph, false);

            TowerFace east = FamilyBodyBuilder.Side(pavilion, TowerSide.East), west = FamilyBodyBuilder.Side(pavilion, TowerSide.West);
            float reach = pavilion.Depth;
            if (forward > 0f)
            {
                Slab(w, east, "wall", 0f, forward, 0f, ph, 0f, MidriseWall);
                Slab(w, west, "wall", reach - forward, reach, 0f, ph, 0f, MidriseWall);
            }

            Slab(w, east, "wall", forward, reach, Top, ph, 0f, MidriseWall);
            Slab(w, west, "wall", 0f, reach - forward, Top, ph, 0f, MidriseWall);
            Slab(w, east, "band", 0f, reach, ph - .4f, ph, -.25f, 0f);
            Slab(w, west, "band", 0f, reach, ph - .4f, ph, -.25f, 0f);
            Slab(w, FamilyBodyBuilder.Side(pavilion, TowerSide.North), "wall", 0f, cw, Top, ph, 0f, MidriseWall);
        }
    }
}
