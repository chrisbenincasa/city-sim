using System.Numerics;

namespace Borough.Appearance;

public static partial class FamilyBodyBuilder
{
    private const float PerimeterEntryEvery = 14f;

    /// <summary>One perimeter form's proportions, as <c>scripts/art/perimeter-forms.py</c> authors them in <c>FORMS</c>.</summary>
    /// <remarks>A field a form does not use keeps its zero default.</remarks>
    internal sealed record PerimeterLook(
        MidriseVariant Form,
        float Bay,
        (float W, float H, float Sill) Window = default,
        float Reveal = MidriseWall,
        bool Attic = false,
        float AtticSetback = 0f,
        float Parapet = .5f,
        int LoggiaEvery = 0,
        float LoggiaDepth = 0f,
        bool StoneBand = false,
        bool YardBalconies = false,
        int BoxEvery = 0,
        float[]? Widths = null,
        float WindowHeight = 0f,
        float Sill = 0f,
        bool CornerBalconies = false,
        bool SlabBalconies = false,
        bool Accent = false,
        float Module = 0f,
        float Deep = 0f,
        float Pile = 0f,
        float Pitch = 0f,
        bool GableWindow = false,
        (float Low, float High) Ribbon = default,
        float Mullion = 0f,
        float GroundRecess = 0f,
        float Canopy = 0f,
        (float H, float Out)? Fascia = null,
        bool StairTower = false,
        int PierEvery = 0,
        (float W, float Out) Pier = default);

    internal static PerimeterLook BrickLoggia { get; } = new(MidriseVariant.BrickLoggia, 3.6f, (1.8f, 2.5f, .4f), .45f,
        Attic: true, AtticSetback: 2.2f, Parapet: .6f, LoggiaEvery: 3, LoggiaDepth: 1.6f, StoneBand: true, YardBalconies: true);

    internal static PerimeterLook TimberFrame { get; } = new(MidriseVariant.TimberFrame, 3f, (1.4f, 2.6f, .1f), .3f,
        Attic: true, AtticSetback: 2f, Parapet: .5f, BoxEvery: 4, YardBalconies: true);

    internal static PerimeterLook WhiteRender { get; } = new(MidriseVariant.WhiteRender, 3.2f, Reveal: .12f, Parapet: .4f,
        Widths: [1.1f, 2.3f, 1.1f, 1.6f], WindowHeight: 1.7f, Sill: .8f, CornerBalconies: true, LoggiaDepth: 1.8f,
        SlabBalconies: true, Accent: true);

    internal static PerimeterLook GableFront { get; } = new(MidriseVariant.GableFront, 4f, (1f, 2.1f, .6f), .1f,
        Module: 8f, Deep: 16f, Pile: 12f, Pitch: 45f, GableWindow: true);

    internal static PerimeterLook RibbonBand { get; } = new(MidriseVariant.Ribbon, 3f, Ribbon: (.9f, 2.3f), Mullion: 1.5f,
        GroundRecess: 1.6f, Canopy: 2.2f, Fascia: (.45f, .7f), StairTower: true);

    internal static PerimeterLook Crosswall { get; } = new(MidriseVariant.Crosswall, 3f, (2f, 1.6f, .9f), .15f,
        Fascia: (.35f, .9f), PierEvery: 2, Pier: (.6f, .55f));

    internal static bool IsPerimeter(MidriseVariant variant) => variant is not (MidriseVariant.Mansion or MidriseVariant.PanelSlab);

    internal static PerimeterLook PerimeterLookOf(MidriseBody body)
    {
        PerimeterLook look = body.Variant switch
        {
            MidriseVariant.BrickLoggia => BrickLoggia,
            MidriseVariant.TimberFrame => TimberFrame,
            MidriseVariant.WhiteRender => WhiteRender,
            MidriseVariant.GableFront => GableFront,
            MidriseVariant.Ribbon => RibbonBand,
            MidriseVariant.Crosswall => Crosswall,
            _ => throw new ArgumentOutOfRangeException(nameof(body)),
        };
        return look with { Bay = body.ModuleMetres ?? look.Bay };
    }

    private enum PerimeterRoof : byte
    {
        Solar,
        Sedum,
        Plant,
    }

    private static void BuildPerimeter(Writer writer, Rect bounds, Rect[] rects, bool ring, AttachedSides attached,
        int storeys, MidriseBody shape, FamilyBodyDetail detail)
    {
        PerimeterLook look = PerimeterLookOf(shape);
        bool far = detail == FamilyBodyDetail.Far;
        foreach (Rect rect in rects)
        {
            foreach (TowerSide name in TowerSides)
            {
                TowerFace side = Side(rect, name);
                Role role = RoleOf(name, rect, bounds, ring, attached);
                foreach ((float a, float b) in OpenStretches(side, rect, rects))
                {
                    if (far) PerimeterFaceFar(writer, side, a, b, storeys, role, look);
                    else PerimeterFace(writer, side, a, b, storeys, role, look);
                }
            }
        }

        // The roof detail follows the body's size so equal bodies share one mesh.
        var kit = (PerimeterRoof)((((int)bounds.Width) + (3 * (int)bounds.Depth) + (7 * storeys)) % 3);
        PerimeterRoofs(writer, rects, bounds, ring, attached, storeys, look, kit, far);
    }

    private static int FullStoreys(int storeys, PerimeterLook look) => look.Attic && storeys >= 4 ? storeys - 1 : storeys;

    private static void Entrance(Writer writer, TowerFace side, float u, float d)
    {
        Slab(writer, side, "reveal", u - .95f, u - .9f, 0f, 2.9f, d, d + 1.2f);
        Slab(writer, side, "reveal", u + .9f, u + .95f, 0f, 2.9f, d, d + 1.2f);
        Plane(writer, side.Face, "door", u - .9f, u + .9f, 0f, 2.6f, d + 1.2f);
        Plane(writer, side.Face, "lobby", u - .9f, u + .9f, 2.6f, 2.9f, d + 1.2f);
        Slab(writer, side, "canopy", u - 1.6f, u + 1.6f, 2.95f, 3.12f, -1.3f, d);
    }

    private static void PerimeterGround(Writer writer, TowerFace side, float a, float b, Role role, PerimeterLook look)
    {
        List<float> doors = role == Role.Street ? [.. Entries(b - a, PerimeterEntryEvery).Select(e => a + e)] : [];
        float d = look.Reveal + MidriseWall;
        if (look.GroundRecess > 0f && role == Role.Street)
        {
            float r = look.GroundRecess;
            Slab(writer, side, "wall-end", a, b, 0f, .3f, -.05f, r + MidriseWall);
            List<Hole> glazing = [.. MidriseBays(b - a, look.Bay).Select(bay => new Hole(a + bay.U0 + .15f, a + bay.U1 - .15f, .3f, 3f))];
            WallWithHoles(writer, side, "wall-end", a, b, .3f, Storey, glazing, r, r + MidriseWall);
            List<float> piers = [a + .3f, .. MidriseBays(b - a, 6f).Skip(1).Select(bay => a + bay.U0), b - .3f];
            foreach (float u in piers) Slab(writer, side, "trim", u - .2f, u + .2f, 0f, Storey, .2f, .6f);
            Slab(writer, side, "trim", a, b, Storey - .25f, Storey, -look.Canopy, r);
            return;
        }

        var openings = new List<Hole>();
        foreach ((float s0, float s1) in MidriseBays(b - a, look.Bay))
        {
            float u0 = a + s0, u1 = a + s1;
            if (doors.Any(q => u0 <= q && q < u1)) continue;
            if (role == Role.Street) openings.Add(new Hole(u0 + .35f, u1 - .35f, .6f, 3f));
            else if (role != Role.End) openings.Add(Centred(u0, u1, 1.4f, .9f, 1.8f));
        }

        WallWithHoles(writer, side, "wall-end", a, b, 0f, Storey, [.. openings, .. doors.Select(q => new Hole(q - .9f, q + .9f, 0f, 2.9f, null))], 0f, d);
        foreach (Hole o in openings) Frame(writer, side, o, look);
        foreach (float q in doors) Entrance(writer, side, q, d);
        if (look.StoneBand) Slab(writer, side, "band", a, b, Storey - .35f, Storey, -.06f, d);
    }

    private static void Frame(Writer writer, TowerFace side, Hole o, PerimeterLook look)
    {
        float d = look.Reveal + MidriseWall;
        Slab(writer, side, "frame", o.U0, o.U1, o.Z0, o.Z0 + .06f, d - .08f, d);
        Slab(writer, side, "frame", o.U0, o.U1, o.Z1 - .06f, o.Z1, d - .08f, d);
        Slab(writer, side, "frame", o.U0, o.U0 + .06f, o.Z0, o.Z1, d - .08f, d);
        Slab(writer, side, "frame", o.U1 - .06f, o.U1, o.Z0, o.Z1, d - .08f, d);
    }

    private static void UpperOpenings(PerimeterLook look, float a, int storey, Role role, List<(float U0, float U1)> stretch,
        List<Hole> openings, List<Hole> loggias, List<Hole> boxes)
    {
        float z = storey * Storey;
        for (int i = 0; i < stretch.Count; i++)
        {
            float u0 = a + stretch[i].U0, u1 = a + stretch[i].U1;
            if (role == Role.End && i % 2 == 1) continue;
            if (look.Form == MidriseVariant.WhiteRender)
            {
                if (look.CornerBalconies && role == Role.Street && (i == 0 || i == stretch.Count - 1))
                {
                    loggias.Add(new Hole(u0, u1, z, z + Storey - .35f, null));
                    continue;
                }

                float w = look.Widths![(i + storey) % look.Widths.Length];
                openings.Add(Centred(u0, u1, w, z + look.Sill, look.WindowHeight));
            }
            else if (look.Form == MidriseVariant.BrickLoggia && role == Role.Street && i % look.LoggiaEvery == 1
                && i > 0 && i < stretch.Count - 1)
            {
                loggias.Add(new Hole(u0 + .3f, u1 - .3f, z, z + 2.9f, null));
            }
            else
            {
                openings.Add(Centred(u0, u1, look.Window.W, z + look.Window.Sill, look.Window.H));
                if (look.Form == MidriseVariant.TimberFrame && role == Role.Street && i % look.BoxEvery == 2) boxes.Add(openings[^1]);
            }
        }
    }

    private static void Balcony(Writer writer, TowerFace side, float u0, float u1, float z, float depth)
    {
        Slab(writer, side, "trim", u0, u1, z - .18f, z, -depth, 0f);
        Slab(writer, side, "glass", u0, u1, z, z + 1f, -depth, -depth + .03f);
        Slab(writer, side, "trim", u0, u1, z + 1f, z + 1.06f, -depth - .02f, -depth + .05f);
    }

    private static void StairTowers(Writer writer, TowerFace side, float a, float b, float top, Role role, PerimeterLook look, bool far)
    {
        if (!look.StairTower || role != Role.Street || b - a <= 20f) return;
        foreach (float e in Entries(b - a, PerimeterEntryEvery))
        {
            float q = a + e;
            Slab(writer, side, "wall", q - 1.6f, q + 1.6f, Storey, top + 1.2f, -1f, 0f);
            if (!far) Slab(writer, side, "glass", q - .5f, q + .5f, Storey + .4f, top - .4f, -1.03f, -1f);
        }
    }

    private static void PerimeterFace(Writer writer, TowerFace side, float a, float b, int storeys, Role role, PerimeterLook look)
    {
        if (role == Role.Party)
        {
            Slab(writer, side, "wall", a, b, 0f, storeys * Storey, 0f, MidriseWall);
            return;
        }

        int full = FullStoreys(storeys, look);
        float top = full * Storey;
        List<(float U0, float U1)> stretch = MidriseBays(b - a, look.Bay);
        PerimeterGround(writer, side, a, b, role, look);

        if (look.Form == MidriseVariant.Ribbon)
        {
            (float lo, float hi) = look.Ribbon;
            for (int storey = 1; storey < full; storey++)
            {
                float z = storey * Storey;
                Slab(writer, side, "spandrel", a + .3f, b - .3f, z, z + lo, -.03f, MidriseWall);
                Slab(writer, side, "wall", a, a + .3f, z, z + Storey, 0f, MidriseWall);
                Slab(writer, side, "wall", b - .3f, b, z, z + Storey, 0f, MidriseWall);
                Slab(writer, side, "spandrel", a + .3f, b - .3f, z + hi, z + Storey, -.03f, MidriseWall);
                Plane(writer, side.Face, "glass", a + .3f, b - .3f, z + lo, z + hi, .12f);
                int n = Math.Max(1, RoundStoreys((b - a - .6f) / look.Mullion));
                for (int k = 0; k <= n; k++)
                {
                    float u = a + .3f + ((b - a - .6f) * k / n);
                    Slab(writer, side, "frame", u - .04f, u + .04f, z + lo, z + hi, .05f, .14f);
                }
            }

            StairTowers(writer, side, a, b, top, role, look, far: false);
            if (role == Role.Yard)
            {
                for (int storey = 1; storey < full; storey++)
                {
                    for (int i = 0; i < stretch.Count; i += 2)
                    {
                        Balcony(writer, side, a + stretch[i].U0 + .2f, a + stretch[i].U1 - .2f, storey * Storey, 1.3f);
                    }
                }
            }
        }
        else if (look.Form == MidriseVariant.Crosswall)
        {
            (float pw, float outward) = look.Pier;
            List<float> piers = [a];
            for (int j = 1; j < stretch.Count; j++)
            {
                if (j % look.PierEvery == 0) piers.Add(a + stretch[j].U0);
            }

            piers.Add(b);
            for (int storey = 1; storey < full; storey++)
            {
                float z = storey * Storey;
                List<Hole> openings = [.. stretch.Select(s => Centred(a + s.U0, a + s.U1, look.Window.W, z + look.Window.Sill, look.Window.H))];
                WallWithHoles(writer, side, "panel", a, b, z, z + Storey, openings, .1f, .1f + MidriseWall);
                foreach (Hole o in openings) Slab(writer, side, "frame", o.U0, o.U1, o.Z0 - .08f, o.Z0, 0f, .15f);
                Slab(writer, side, "trim", a, b, z - .12f, z + .12f, -.05f, MidriseWall);
                if (role != Role.Yard) continue;
                for (int p = 0; p + 1 < piers.Count; p++)
                {
                    Slab(writer, side, "trim", piers[p] + pw, piers[p + 1] - pw, z - .15f, z, -1.4f, 0f);
                    Railing(writer, side, piers[p] + pw, piers[p + 1] - pw, z, -1.35f);
                }
            }

            foreach (float u in piers)
            {
                Slab(writer, side, "wall", Math.Max(a, u - (pw / 2f)), Math.Min(b, u + (pw / 2f)), Storey, top, -outward, MidriseWall);
            }
        }
        else
        {
            float d = look.Reveal + MidriseWall;
            var openings = new List<Hole>();
            var loggias = new List<Hole>();
            var boxes = new List<Hole>();
            for (int storey = 1; storey < full; storey++)
            {
                float z = storey * Storey;
                openings.Clear();
                loggias.Clear();
                boxes.Clear();
                UpperOpenings(look, a, storey, role, stretch, openings, loggias, boxes);
                WallWithHoles(writer, side, "wall", a, b, z, z + Storey, [.. openings, .. loggias], 0f, d);
                foreach (Hole o in openings) Frame(writer, side, o, look);
                foreach (Hole l in loggias)
                {
                    float ld = look.LoggiaDepth;
                    WallWithHoles(writer, side, "reveal", l.U0, l.U1, l.Z0, l.Z1,
                        [Centred(l.U0, l.U1, Math.Min(2.4f, l.U1 - l.U0 - .4f), l.Z0, 2.5f)], ld, ld + .1f);
                    Slab(writer, side, "reveal", l.U0 - .05f, l.U0, l.Z0, l.Z1, d, ld);
                    Slab(writer, side, "reveal", l.U1, l.U1 + .05f, l.Z0, l.Z1, d, ld);
                    Slab(writer, side, "reveal", l.U0, l.U1, l.Z1, l.Z1 + .05f, d, ld);
                    Slab(writer, side, "trim", l.U0, l.U1, l.Z0, l.Z0 + .1f, 0f, ld);
                    Railing(writer, side, l.U0, l.U1, l.Z0 + .1f, .1f);
                }

                foreach (Hole x in boxes)
                {
                    Slab(writer, side, "trim", x.U0 - .25f, x.U0, x.Z0 - .25f, x.Z1 + .25f, -.5f, 0f);
                    Slab(writer, side, "trim", x.U1, x.U1 + .25f, x.Z0 - .25f, x.Z1 + .25f, -.5f, 0f);
                    Slab(writer, side, "trim", x.U0, x.U1, x.Z1, x.Z1 + .25f, -.5f, 0f);
                    Slab(writer, side, "trim", x.U0, x.U1, x.Z0 - .25f, x.Z0, -.5f, 0f);
                }

                if (look.Form == MidriseVariant.TimberFrame)
                {
                    foreach (Hole o in openings)
                    {
                        if (!boxes.Contains(o)) Railing(writer, side, o.U0, o.U1, o.Z0, .05f);
                    }
                }

                if (role == Role.Yard && look.YardBalconies)
                {
                    for (int i = 1; i < stretch.Count; i += 3) Balcony(writer, side, a + stretch[i].U0 - .4f, a + stretch[i].U1 + .4f, z, 1.6f);
                }

                if (look.SlabBalconies && role == Role.Street)
                {
                    for (int i = 1; i < stretch.Count - 1; i++)
                    {
                        if ((i + storey) % 3 == 0) Balcony(writer, side, a + stretch[i].U0 - .3f, a + stretch[i].U1 + .3f, z, 1.2f);
                    }
                }
            }

            if (look.Accent && role == Role.Street)
            {
                foreach (float e in Entries(b - a, PerimeterEntryEvery))
                {
                    float q = a + e;
                    Slab(writer, side, "accent", q - 1.1f, q + 1.1f, Storey, top, -.02f, .02f);
                    for (int storey = 1; storey < full; storey++)
                    {
                        float z = storey * Storey;
                        Slab(writer, side, "glass", q - .35f, q + .35f, z + .6f, z + 2.9f, -.04f, -.02f);
                    }
                }
            }
        }

        Cap(writer, side, a, b, top, role, look, "wall");
    }

    private static void Cap(Writer writer, TowerFace side, float a, float b, float top, Role role, PerimeterLook look, string wall)
    {
        if (look.Fascia is (float h, float outward))
        {
            Slab(writer, side, "trim", a - (role == Role.End ? outward : 0f), b, top - .05f, top + h, -outward, MidriseWall);
        }
        else if (look.Form == MidriseVariant.GableFront)
        {
            Slab(writer, side, "trim", a, b, top - .15f, top, -.05f, MidriseWall);
        }
        else
        {
            float p = look.Parapet;
            Slab(writer, side, wall, a, b, top, top + p, 0f, MidriseWall);
            Slab(writer, side, "trim", a, b, top + p, top + p + .08f, -.04f, MidriseWall + .04f);
        }
    }

    private static void PerimeterFaceFar(Writer writer, TowerFace side, float a, float b, int storeys, Role role, PerimeterLook look)
    {
        if (role == Role.Party)
        {
            for (int storey = 0; storey < storeys; storey++) FarWall(writer, side, a, b, storey, true, role, look.Bay);
            return;
        }

        int full = FullStoreys(storeys, look);
        float top = full * Storey;
        for (int storey = 0; storey < full; storey++) FarWall(writer, side, a, b, storey, true, role, look.Bay);
        StairTowers(writer, side, a, b, top, role, look, far: true);
        Cap(writer, side, a, b, top, role, look, "far-facade");
    }

    private static void AtticFace(Writer writer, TowerFace side, float a, float b, float z, PerimeterLook look)
    {
        List<Hole> openings = [.. MidriseBays(b - a, look.Bay)
            .Select(bay => Centred(a + bay.U0, a + bay.U1, Math.Min(2.6f, bay.U1 - bay.U0 - .5f), z + .2f, 2.6f))];
        WallWithHoles(writer, side, "attic", a, b, z, z + Storey - .2f, openings);
        Slab(writer, side, "trim", a, b, z + Storey - .2f, z + Storey, -.1f, MidriseWall);
    }

    private static void RoofDetail(Writer writer, Rect rect, float z, PerimeterRoof kit)
    {
        if (rect.Width < 8f || rect.Depth < 8f) return;
        switch (kit)
        {
            case PerimeterRoof.Sedum:
                Box(writer, new Vector3(rect.X0 + .7f, rect.Y0 + .7f, z + .05f), new Vector3(rect.X1 - .7f, rect.Y1 - .7f, z + .15f), "sedum");
                return;
            case PerimeterRoof.Solar:
                int rows = (int)MathF.Floor(((rect.Depth - 1.6f) / 2.6f) + 1e-3f);
                for (int k = 0; k < rows; k++)
                {
                    float y = rect.Y0 + 1.5f + (2.6f * k);
                    writer.Polygon("solar",
                    [
                        new(rect.X0 + 1.5f, y, z + .3f), new(rect.X1 - 1.5f, y, z + .3f),
                        new(rect.X1 - 1.5f, y + 1.2f, z + .85f), new(rect.X0 + 1.5f, y + 1.2f, z + .85f),
                    ]);
                    Box(writer, new Vector3(rect.X0 + 1.5f, y + 1.1f, z), new Vector3(rect.X1 - 1.5f, y + 1.2f, z + .85f), "metal");
                }

                return;
            default:
                float cx = (rect.X0 + rect.X1) / 2f, cy = (rect.Y0 + rect.Y1) / 2f;
                Box(writer, new Vector3(cx - 3f, cy - 2f, z), new Vector3(cx + 3f, cy + 2f, z + 2.2f), "plant");
                Box(writer, new Vector3(cx - 3.4f, cy - 2.4f, z), new Vector3(cx + 3.4f, cy - 2.3f, z + 2.4f), "metal");
                Box(writer, new Vector3(cx - 3.4f, cy + 2.3f, z), new Vector3(cx + 3.4f, cy + 2.4f, z + 2.4f), "metal");
                return;
        }
    }

    /// <summary>Relative gable widths along a row, so equal gables do not repeat down a whole block.</summary>
    private static readonly float[] GableRhythm = [1f, 1.3f, .8f, 1.15f, .75f];

    private static float[] GableWidths(float length, float module)
    {
        int n = Math.Max(1, RoundStoreys(length / module));
        float[] weights = [.. Enumerable.Range(0, n).Select(k => GableRhythm[(k + (int)length) % GableRhythm.Length])];
        float sum = weights.Sum();
        return [.. weights.Select(w => length * w / sum)];
    }

    /// <summary>
    /// Steep gables turned to the street, in a row of varied widths. A deep solid body keeps a band of
    /// street gables at most <see cref="PerimeterLook.Pile"/> deep and a flat roof behind it.
    /// </summary>
    private static void Gables(Writer writer, Rect[] rects, bool ring, float top, PerimeterLook look, PerimeterRoof kit, bool far)
    {
        float rise = MathF.Tan(look.Pitch * MathF.PI / 180f);
        foreach (Rect r in rects)
        {
            if (!ring && r.Depth > look.Deep)
            {
                float front = Math.Min(look.Pile, r.Depth / 2f);
                GableRow(writer, new Rect(r.X0, r.Y0, r.X1, r.Y0 + front), true, top, rise, look, far);
                Box(writer, new Vector3(r.X0 + .1f, r.Y0 + front, top - .2f), new Vector3(r.X1 - .1f, r.Y1 - .1f, top + .05f), "membrane");
                RoofDetail(writer, new Rect(r.X0, r.Y0 + front, r.X1, r.Y1), top + .05f, kit);
                continue;
            }

            GableRow(writer, r, !ring || r.Width >= r.Depth, top, rise, look, far);
        }
    }

    private static void GableRow(Writer writer, Rect r, bool alongX, float top, float rise, PerimeterLook look, bool far)
    {
        float a = alongX ? r.X0 : r.Y0;
        foreach (float m in GableWidths(alongX ? r.Width : r.Depth, look.Module))
        {
            float b = a + m;
            float c = (a + b) / 2f, h = top + (m / 2f * rise);
            if (alongX)
            {
                writer.Polygon("roof", [new(a, r.Y0, top), new(c, r.Y0, h), new(c, r.Y1, h), new(a, r.Y1, top)]);
                writer.Polygon("roof", [new(c, r.Y0, h), new(b, r.Y0, top), new(b, r.Y1, top), new(c, r.Y1, h)]);
                writer.Polygon("wall", [new(a, r.Y0, top), new(b, r.Y0, top), new(c, r.Y0, h)]);
                writer.Polygon("wall", [new(b, r.Y1, top), new(a, r.Y1, top), new(c, r.Y1, h)]);
                if (look.GableWindow && !far)
                {
                    float y = r.Y0 - .02f;
                    writer.Polygon("glass",
                        [new(c - .5f, y, top + .4f), new(c + .5f, y, top + .4f), new(c + .5f, y, top + 2.4f), new(c - .5f, y, top + 2.4f)]);
                }
            }
            else
            {
                writer.Polygon("roof", [new(r.X0, a, top), new(r.X1, a, top), new(r.X1, c, h), new(r.X0, c, h)]);
                writer.Polygon("roof", [new(r.X1, b, top), new(r.X0, b, top), new(r.X0, c, h), new(r.X1, c, h)]);
                writer.Polygon("wall", [new(r.X0, b, top), new(r.X0, a, top), new(r.X0, c, h)]);
                writer.Polygon("wall", [new(r.X1, a, top), new(r.X1, b, top), new(r.X1, c, h)]);
            }

            a = b;
        }
    }

    private static void PerimeterRoofs(Writer writer, Rect[] rects, Rect bounds, bool ring, AttachedSides attached,
        int storeys, PerimeterLook look, PerimeterRoof kit, bool far)
    {
        if (look.Attic && storeys >= 4)
        {
            float fullTop = (storeys - 1) * Storey;
            float s = look.AtticSetback;
            var attics = new Rect[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                bool Open(TowerSide side) => RoleOf(side, r, bounds, ring, attached) is Role.Street or Role.End;
                attics[i] = new Rect(r.X0 + (Open(TowerSide.West) ? s : 0f), r.Y0 + (Open(TowerSide.South) ? s : 0f),
                    r.X1 - (Open(TowerSide.East) ? s : 0f), r.Y1 - (Open(TowerSide.North) ? s : 0f));
                Box(writer, new Vector3(r.X0 + .1f, r.Y0 + .1f, fullTop - .2f), new Vector3(r.X1 - .1f, r.Y1 - .1f, fullTop + .05f), "paving");
                if (far) continue;
                foreach (TowerSide name in TowerSides)
                {
                    if (RoleOf(name, r, bounds, ring, attached) != Role.Street) continue;
                    TowerFace side = Side(r, name);
                    Slab(writer, side, "glass", .2f, side.Length - .2f, fullTop + .05f, fullTop + 1.05f, .05f, .08f);
                }
            }

            for (int i = 0; i < rects.Length; i++)
            {
                Rect attic = attics[i], rect = rects[i];
                foreach (TowerSide name in TowerSides)
                {
                    TowerFace side = Side(attic, name);
                    Role role = RoleOf(name, rect, bounds, ring, attached);
                    foreach ((float a, float b) in OpenStretches(side, attic, attics))
                    {
                        if (far)
                        {
                            FarWall(writer, side, a, b, storeys - 1, true, role, look.Bay, MansionAtticFarCell);
                            if (role != Role.Party) Slab(writer, side, "trim", a, b, fullTop + Storey - .2f, fullTop + Storey, -.1f, MidriseWall);
                        }
                        else if (role == Role.Party)
                        {
                            Slab(writer, side, "wall", a, b, fullTop, fullTop + Storey, 0f, MidriseWall);
                        }
                        else
                        {
                            AtticFace(writer, side, a, b, fullTop, look);
                        }
                    }
                }

                Box(writer, new Vector3(attic.X0 + .1f, attic.Y0 + .1f, fullTop + Storey - .2f),
                    new Vector3(attic.X1 - .1f, attic.Y1 - .1f, fullTop + Storey), "membrane");
                RoofDetail(writer, attic, fullTop + Storey, kit);
            }

            return;
        }

        float top = storeys * Storey;
        if (look.Form == MidriseVariant.GableFront)
        {
            Gables(writer, rects, ring, top, look, kit, far);
            return;
        }

        foreach (Rect r in rects)
        {
            Box(writer, new Vector3(r.X0 + .1f, r.Y0 + .1f, top - .2f), new Vector3(r.X1 - .1f, r.Y1 - .1f, top + .05f), "membrane");
            RoofDetail(writer, r, top + .05f, kit);
        }
    }
}
