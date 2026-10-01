using System.Numerics;

namespace Borough.Appearance;

public static partial class FamilyBodyBuilder
{
    private const float MidriseWall = .25f;

    /// <summary>The thickness of a ring's wings, which is <c>BuildingPlan.DaylightTiles</c> four-metre Tiles.</summary>
    public const float RingWingMetres = 16f;

    internal sealed record MansionLook(
        float Bay,
        (float W, float H, float Sill) Window,
        (float W, float H, float Sill) GroundWindow,
        (float W, float H, float Sill) Door,
        (float W, float H, float Sill) AtticWindow,
        float EntryEvery,
        int LoggiaEvery,
        float LoggiaDepth,
        int BalconyEvery,
        float BalconyDepth,
        float AtticSetback,
        (float H, float Out) Cornice,
        (float H, float Out) StringCourse,
        float Parapet);

    internal sealed record SlabLook(
        float Bay,
        (float W, float H, float Sill) Window,
        (float W, float H, float Sill) GalleryDoor,
        (float W, float H, float Sill) GalleryWindow,
        float EntryEvery,
        int LoggiaEvery,
        float LoggiaDepth,
        float GalleryDepth,
        float Balustrade,
        float GroundRecess,
        float PilotiEvery,
        float Piloti,
        (float W, float Out) Joint,
        float PlantEvery,
        (float W, float D, float H) Plant,
        float Parapet);

    internal static MansionLook Mansion { get; } = new(
        3.3f, (1.3f, 1.9f, .9f), (1.7f, 2.1f, .7f), (1.6f, 2.7f, .3f), (2.4f, 2.3f, .5f), 16f, 3, 1.4f, 2, 1.4f,
        1.8f, (.45f, .35f), (.25f, .12f), .9f);

    internal static SlabLook PanelSlab { get; } = new(
        3.6f, (2.1f, 1.5f, .9f), (1f, 2.2f, 0f), (1.2f, 1f, 1.2f), 30f, 4, 1.2f, 1.8f, 1.1f, 1.5f, 7.2f, .5f,
        (.06f, .02f), 30f, (6f, 4f, 3f), .6f);

    /// <summary>
    /// Builds a mid-rise body over a Building's whole footprint, as <c>scripts/art/midrise-families.py</c>
    /// authors it.
    /// </summary>
    /// <param name="frontage">The footprint's width along the street, in metres.</param>
    /// <param name="depth">The footprint's depth away from the street, in metres.</param>
    /// <param name="storeys">The Building's storeys.</param>
    /// <param name="ring">
    /// The simulation draws the footprint as a ring: full-width wings along the street and the back,
    /// and two flank wings between them, each <see cref="RingWingMetres"/> thick.
    /// </param>
    /// <param name="attached">Flanks shared with a neighbour, drawn as blank party walls.</param>
    public static FamilyBodyMesh BuildMidrise(FamilyBody body, float frontage, float depth, int storeys, bool ring,
        AttachedSides attached = AttachedSides.None)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.Midrise is null) throw new ArgumentException("A mid-rise body must declare a mid-rise variant.", nameof(body));

        var mesh = new FamilyBodyMesh();
        var writer = new Writer(mesh, body);
        int total = Math.Max(1, storeys);
        Rect bounds = new(-frontage / 2f, -depth / 2f, frontage / 2f, depth / 2f);
        Rect[] rects = MidriseWings(bounds, ring);
        MidriseBody shape = body.Midrise;
        bool mansion = shape.Variant == MidriseVariant.Mansion;
        MansionLook mansionLook = Mansion with { Bay = shape.ModuleMetres ?? Mansion.Bay };
        SlabLook slabLook = PanelSlab with { Bay = shape.ModuleMetres ?? PanelSlab.Bay };

        foreach (Rect rect in rects)
        {
            foreach (TowerSide name in TowerSides)
            {
                TowerFace side = Side(rect, name);
                Role role = RoleOf(name, rect, bounds, ring, attached);
                foreach ((float a, float b) in OpenStretches(side, rect, rects))
                {
                    if (mansion) MansionFace(writer, side, a, b, total, role, mansionLook, shape);
                    else SlabFace(writer, side, a, b, total, role, slabLook, shape);
                }
            }
        }

        if (mansion && shape.Attic)
        {
            MansionRoofs(writer, rects, bounds, ring, attached, total, mansionLook);
        }
        else
        {
            float top = total * Storey;
            foreach (Rect rect in rects)
            {
                Box(writer, new Vector3(rect.X0 + .1f, rect.Y0 + .1f, top - .2f),
                    new Vector3(rect.X1 - .1f, rect.Y1 - .1f, top + .05f), "membrane");
                if (!mansion && (!ring || rect.Width > rect.Depth))
                {
                    PlantRooms(writer, new Rect(rect.X0 + 4f, rect.Y0 + 4f, rect.X1 - 4f, rect.Y1 - 4f), top, slabLook);
                }
            }
        }

        return mesh;
    }

    private enum Role : byte
    {
        Street,
        Yard,
        End,
        Party,
    }

    private readonly record struct Hole(float U0, float U1, float Z0, float Z1, string? Part = "glass");

    private static Rect[] MidriseWings(Rect b, bool ring)
    {
        if (!ring) return [b];
        float w = RingWingMetres;
        return
        [
            new Rect(b.X0, b.Y0, b.X1, b.Y0 + w), new Rect(b.X0, b.Y1 - w, b.X1, b.Y1),
            new Rect(b.X0, b.Y0 + w, b.X0 + w, b.Y1 - w), new Rect(b.X1 - w, b.Y0 + w, b.X1, b.Y1 - w),
        ];
    }

    private static Role RoleOf(TowerSide side, Rect rect, Rect site, bool ring, AttachedSides attached)
    {
        bool onEdge = side switch
        {
            TowerSide.South => MathF.Abs(rect.Y0 - site.Y0) < 1e-3f,
            TowerSide.North => MathF.Abs(rect.Y1 - site.Y1) < 1e-3f,
            TowerSide.West => MathF.Abs(rect.X0 - site.X0) < 1e-3f,
            _ => MathF.Abs(rect.X1 - site.X1) < 1e-3f,
        };
        bool shared = side switch
        {
            TowerSide.West => attached.HasFlag(AttachedSides.Left),
            TowerSide.East => attached.HasFlag(AttachedSides.Right),
            _ => false,
        };
        if (shared && onEdge) return Role.Party;
        if (ring) return onEdge ? Role.Street : Role.Yard;
        return side switch
        {
            TowerSide.South => Role.Street,
            TowerSide.North => Role.Yard,
            _ => Role.End,
        };
    }

    /// <summary>The stretches of a face no other wing stands against.</summary>
    private static List<(float A, float B)> OpenStretches(TowerFace side, Rect rect, Rect[] rects)
    {
        var cuts = new SortedSet<float> { 0f, side.Length };
        foreach (Rect other in rects)
        {
            if (other == rect || Touching(side, rect, other) is not { } t) continue;
            cuts.Add(t.Low);
            cuts.Add(t.High);
        }

        float[] at = [.. cuts];
        var open = new List<(float, float)>();
        for (int i = 0; i + 1 < at.Length; i++)
        {
            float a = at[i], b = at[i + 1];
            bool covered = false;
            foreach (Rect other in rects)
            {
                if (other != rect && Touching(side, rect, other) is { } t && t.Low <= a + 1e-4f && b - 1e-4f <= t.High)
                {
                    covered = true;
                    break;
                }
            }

            if (!covered) open.Add((a, b));
        }

        return open;
    }

    private static void Slab(Writer writer, TowerFace side, string part, float u0, float u1, float z0, float z1,
        float d0, float d1)
    {
        if (u1 - u0 < 1e-4f || z1 - z0 < 1e-4f || d1 - d0 < 1e-4f) return;
        writer.Slab(side.Face, u0, u1, z0, z1, d0, d1, part);
    }

    /// <summary>A wall slab with rectangular holes cut through it, each glazed at the slab's back unless its part is null.</summary>
    private static void WallWithHoles(Writer writer, TowerFace side, string part, float u0, float u1, float z0, float z1,
        List<Hole> holes, float d0 = 0f, float d1 = MidriseWall)
    {
        var us = new SortedSet<float> { u0, u1 };
        var zs = new SortedSet<float> { z0, z1 };
        foreach (Hole h in holes)
        {
            us.Add(h.U0);
            us.Add(h.U1);
            zs.Add(h.Z0);
            zs.Add(h.Z1);
        }

        float[] u = [.. us], z = [.. zs];
        for (int j = 0; j + 1 < z.Length; j++)
        {
            float za = z[j], zb = z[j + 1];
            float? start = null;
            for (int i = 0; i + 1 < u.Length; i++)
            {
                float ua = u[i], ub = u[i + 1];
                bool hole = false;
                foreach (Hole h in holes)
                {
                    if (h.U0 <= ua + 1e-4f && ub - 1e-4f <= h.U1 && h.Z0 <= za + 1e-4f && zb - 1e-4f <= h.Z1)
                    {
                        hole = true;
                        break;
                    }
                }

                if (!hole && start is null) start = ua;
                if (hole && start is { } s)
                {
                    Slab(writer, side, part, s, ua, za, zb, d0, d1);
                    start = null;
                }
            }

            if (start is { } last) Slab(writer, side, part, last, u[^1], za, zb, d0, d1);
        }

        foreach (Hole h in holes)
        {
            if (h.Part is { } glazing) Plane(writer, side.Face, glazing, h.U0, h.U1, h.Z0, h.Z1, d1);
        }
    }

    private static List<(float U0, float U1)> MidriseBays(float length, float bay)
    {
        int count = Math.Max(1, RoundStoreys(length / bay));
        float width = length / count;
        var bays = new List<(float, float)>(count);
        for (int i = 0; i < count; i++) bays.Add((i * width, (i + 1) * width));
        return bays;
    }

    private static Hole Centred(float a, float b, float width, float z, float height, string? part = "glass")
    {
        float middle = (a + b) / 2f;
        return new Hole(middle - (width / 2f), middle + (width / 2f), z, z + height, part);
    }

    private static List<float> Entries(float length, float every)
    {
        int count = Math.Max(1, RoundStoreys(length / every));
        var entries = new List<float>(count);
        for (int i = 0; i < count; i++) entries.Add(length * (i + .5f) / count);
        return entries;
    }

    private static void Railing(Writer writer, TowerFace side, float u0, float u1, float z, float depth)
    {
        Slab(writer, side, "metal", u0, u1, z + 1f, z + 1.08f, depth - .04f, depth + .04f);
        var posts = new List<float> { u0 + .05f };
        for (int i = 1; i < 8; i++) posts.Add(u0 + ((u1 - u0) * i / 8f));
        posts.Add(u1 - .05f);
        foreach (float u in posts) Slab(writer, side, "metal", u - .02f, u + .02f, z, z + 1f, depth - .02f, depth + .02f);
    }

    private static void MansionFace(Writer writer, TowerFace side, float a, float b, int storeys, Role role, MansionLook look,
        MidriseBody shape)
    {
        if (role == Role.Party)
        {
            Slab(writer, side, "wall", a, b, 0f, storeys * Storey, 0f, MidriseWall);
            return;
        }

        int full = shape.Attic ? storeys - 1 : storeys;
        float top = full * Storey;
        List<(float U0, float U1)> stretch = MidriseBays(b - a, look.Bay);
        List<float> doors = role == Role.Street ? [.. Entries(b - a, look.EntryEvery).Select(e => a + e)] : [];
        bool shops = shape.Shops && role == Role.Street;

        Slab(writer, side, "plinth", a, b, 0f, .4f, -.05f, MidriseWall);
        var ground = new List<Hole>();
        for (int i = 0; i < stretch.Count; i++)
        {
            float u0 = a + stretch[i].U0, u1 = a + stretch[i].U1;
            if (doors.Any(d => u0 <= d && d < u1))
            {
                Hole door = Centred(u0, u1, look.Door.W, look.Door.Sill, look.Door.H, "door");
                ground.Add(door with { Z0 = Math.Max(door.Z0, .4f) });
            }
            else if (shops)
            {
                ground.Add(new Hole(u0 + .3f, u1 - .3f, .4f, 2.9f));
            }
            else if (role != Role.End || i % 2 == 0)
            {
                ground.Add(Centred(u0, u1, look.GroundWindow.W, look.GroundWindow.Sill, look.GroundWindow.H));
            }
        }

        WallWithHoles(writer, side, "wall-end", a, b, .4f, Storey, ground);
        foreach (float d in doors) Slab(writer, side, "trim", d - 1.3f, d + 1.3f, 3.05f, 3.2f, -.9f, 0f);
        if (shops) Slab(writer, side, "trim", a, b, 2.9f, 3.25f, -.15f, MidriseWall);
        Slab(writer, side, "trim", a, b, Storey - look.StringCourse.H, Storey, -look.StringCourse.Out, MidriseWall);

        for (int storey = 1; storey < full; storey++)
        {
            float z = storey * Storey;
            var holes = new List<Hole>();
            var loggias = new List<Hole>();
            for (int i = 0; i < stretch.Count; i++)
            {
                float u0 = a + stretch[i].U0, u1 = a + stretch[i].U1;
                bool streetLoggias = role == Role.Street && shape.Street == StreetOpenings.Loggias;
                if (streetLoggias && i % look.LoggiaEvery == 1 && i > 0 && i < stretch.Count - 1)
                {
                    var loggia = new Hole(u0 + .35f, u1 - .35f, z, z + 2.9f, null);
                    holes.Add(loggia);
                    loggias.Add(loggia);
                }
                else if (role != Role.End || i % 2 == 0)
                {
                    holes.Add(Centred(u0, u1, look.Window.W, z + look.Window.Sill, look.Window.H));
                }

                bool streetBalconies = role == Role.Street && shape.Street == StreetOpenings.Balconies;
                if ((role == Role.Yard || streetBalconies) && i % look.BalconyEvery == 1)
                {
                    float d = look.BalconyDepth;
                    Slab(writer, side, "trim", u0 + .2f, u1 - .2f, z - .15f, z, -d, 0f);
                    Railing(writer, side, u0 + .2f, u1 - .2f, z, -d + .05f);
                }
            }

            WallWithHoles(writer, side, "wall", a, b, z, z + Storey, holes);
            foreach (Hole l in loggias)
            {
                float d = look.LoggiaDepth;
                float w = l.U1 - l.U0;
                WallWithHoles(writer, side, "reveal", l.U0, l.U1, l.Z0, l.Z1,
                [
                    Centred(l.U0, l.U0 + (w * .45f), 1f, l.Z0, 2.3f),
                    Centred(l.U0 + (w * .55f), l.U1, 1.2f, l.Z0 + .9f, 1.5f),
                ], d, d + .1f);
                Slab(writer, side, "wall", l.U0 - .1f, l.U0, l.Z0, l.Z1, MidriseWall, d);
                Slab(writer, side, "wall", l.U1, l.U1 + .1f, l.Z0, l.Z1, MidriseWall, d);
                Slab(writer, side, "trim", l.U0, l.U1, l.Z0, l.Z0 + .12f, -.05f, d);
                Railing(writer, side, l.U0, l.U1, l.Z0 + .12f, .05f);
            }
        }

        (float ch, float cout) = role == Role.Street ? look.Cornice : (.2f, .08f);
        Slab(writer, side, "trim", a, b, top - .1f, top + ch - .1f, -cout, MidriseWall);
        if (!shape.Attic)
        {
            Slab(writer, side, "wall", a, b, top + ch - .1f, top + look.Parapet, 0f, MidriseWall);
            Slab(writer, side, "trim", a, b, top + look.Parapet, top + look.Parapet + .1f, -.05f, MidriseWall + .05f);
        }
    }

    private static void MansionAttic(Writer writer, TowerFace side, float a, float b, float z, MansionLook look)
    {
        var holes = new List<Hole>();
        foreach ((float u0, float u1) in MidriseBays(b - a, look.Bay))
        {
            holes.Add(Centred(a + u0, a + u1, look.AtticWindow.W, z + look.AtticWindow.Sill, look.AtticWindow.H));
        }

        WallWithHoles(writer, side, "wall-end", a, b, z, z + Storey, holes);
        Slab(writer, side, "trim", a, b, z + Storey, z + Storey + .25f, -.5f, MidriseWall);
    }

    private static void MansionRoofs(Writer writer, Rect[] rects, Rect bounds, bool ring, AttachedSides attached,
        int storeys, MansionLook look)
    {
        float fullTop = (storeys - 1) * Storey;
        float s = look.AtticSetback;
        var attics = new Rect[rects.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            Rect r = rects[i];
            bool Street(TowerSide side) => RoleOf(side, r, bounds, ring, attached) == Role.Street;
            attics[i] = new Rect(r.X0 + (Street(TowerSide.West) ? s : 0f), r.Y0 + (Street(TowerSide.South) ? s : 0f),
                r.X1 - (Street(TowerSide.East) ? s : 0f), r.Y1 - (Street(TowerSide.North) ? s : 0f));
            Box(writer, new Vector3(r.X0 + .1f, r.Y0 + .1f, fullTop - .2f), new Vector3(r.X1 - .1f, r.Y1 - .1f, fullTop + .05f), "paving");
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
                    if (role == Role.Party) Slab(writer, side, "wall", a, b, fullTop, fullTop + Storey, 0f, MidriseWall);
                    else MansionAttic(writer, side, a, b, fullTop, look);
                }
            }

            foreach (TowerSide name in TowerSides)
            {
                if (RoleOf(name, rect, bounds, ring, attached) != Role.Street) continue;
                TowerFace side = Side(rect, name);
                Railing(writer, side, .2f, side.Length - .2f, fullTop + .05f, .1f);
            }

            Box(writer, new Vector3(attic.X0 + .1f, attic.Y0 + .1f, fullTop + Storey - .2f),
                new Vector3(attic.X1 - .1f, attic.Y1 - .1f, fullTop + Storey + .05f), "membrane");
        }
    }

    private static void SlabFace(Writer writer, TowerFace side, float a, float b, int storeys, Role role, SlabLook look,
        MidriseBody shape)
    {
        if (role == Role.Party)
        {
            Slab(writer, side, "wall", a, b, 0f, storeys * Storey, 0f, MidriseWall);
            return;
        }

        float top = storeys * Storey;
        List<(float U0, float U1)> stretch = MidriseBays(b - a, look.Bay);
        bool recessed = role is Role.Street or Role.Yard;
        float recess = recessed ? look.GroundRecess : 0f;
        List<float> doors = recessed ? [.. Entries(b - a, look.EntryEvery).Select(e => a + e)] : [];

        Slab(writer, side, "plinth", a, b, 0f, .3f, -.05f, recess + MidriseWall);
        var ground = new List<Hole>();
        foreach ((float s0, float s1) in stretch)
        {
            float u0 = a + s0, u1 = a + s1;
            if (doors.Any(d => u0 <= d && d < u1)) ground.Add(Centred(u0, u1, 2.4f, .3f, 2.5f, "door"));
            else if (shape.Shops && role == Role.Street) ground.Add(new Hole(u0 + .2f, u1 - .2f, .3f, 2.8f));
            else if (role != Role.End) ground.Add(Centred(u0, u1, 2.8f, .6f, 2.2f));
        }

        WallWithHoles(writer, side, "wall-end", a, b, .3f, Storey, ground, recess, recess + MidriseWall);
        if (recessed)
        {
            Slab(writer, side, "spandrel", a, b, Storey - .4f, Storey, -.02f, recess);
            int n = Math.Max(1, RoundStoreys((b - a) / look.PilotiEvery));
            for (int i = 0; i <= n; i++)
            {
                float u = Math.Min(Math.Max(a + ((b - a) * i / n), a + (look.Piloti / 2f)), b - (look.Piloti / 2f));
                Slab(writer, side, "wall", u - (look.Piloti / 2f), u + (look.Piloti / 2f), .3f, Storey - .4f, 0f, look.Piloti);
            }

            foreach (float d in doors) Slab(writer, side, "trim", d - 2f, d + 2f, 2.9f, 3.05f, -1.2f, recess);
        }

        bool galleries = role == Role.Yard && shape.Galleries;
        for (int storey = 1; storey < storeys; storey++)
        {
            float z = storey * Storey;
            var holes = new List<Hole>();
            var loggias = new List<(float U0, float U1)>();
            for (int i = 0; i < stretch.Count; i++)
            {
                float u0 = a + stretch[i].U0, u1 = a + stretch[i].U1;
                if (role == Role.Street && shape.Street == StreetOpenings.Loggias && i % look.LoggiaEvery == 2)
                {
                    loggias.Add((u0, u1));
                    holes.Add(new Hole(u0, u1, z, z + Storey, null));
                }
                else if (galleries)
                {
                    float third = (u1 - u0) / 3f;
                    holes.Add(Centred(u0, u0 + (third * 1.4f), look.GalleryDoor.W, z, look.GalleryDoor.H, "door"));
                    holes.Add(Centred(u0 + (third * 1.4f), u1, look.GalleryWindow.W, z + look.GalleryWindow.Sill, look.GalleryWindow.H));
                }
                else if (role != Role.End || i % 4 == 1)
                {
                    holes.Add(Centred(u0, u1, role != Role.End ? look.Window.W : .9f, z + look.Window.Sill, look.Window.H));
                }

                if (role == Role.Street && shape.Street == StreetOpenings.Balconies && i % 2 == 1)
                {
                    float d = look.LoggiaDepth;
                    Slab(writer, side, "trim", u0 + .15f, u1 - .15f, z - .2f, z, -d, 0f);
                    Slab(writer, side, "spandrel", u0 + .15f, u1 - .15f, z, z + look.Balustrade, -d, -d + .12f);
                }
            }

            WallWithHoles(writer, side, "wall", a, b, z, z + Storey, holes);
            foreach ((float u0, float u1) in loggias)
            {
                float d = look.LoggiaDepth;
                WallWithHoles(writer, side, "reveal", u0, u1, z, z + Storey, [Centred(u0, u1, 2.6f, z, 2.5f)], d, d + .1f);
                Slab(writer, side, "trim", u0, u1, z + Storey - .2f, z + Storey, MidriseWall, d);
                Slab(writer, side, "spandrel", u0, u1, z, z + look.Balustrade, 0f, .15f);
            }

            if (galleries)
            {
                float d = look.GalleryDepth;
                Slab(writer, side, "trim", a, b, z - .2f, z, -d, 0f);
                Slab(writer, side, "spandrel", a, b, z, z + look.Balustrade, -d, -d + .12f);
            }

            (float jw, float jout) = look.Joint;
            Slab(writer, side, "trim", a, b, z - (jw / 2f), z + (jw / 2f), -jout, 0f);
            if (role != Role.End)
            {
                for (int i = 1; i < stretch.Count; i++)
                {
                    float u = a + stretch[i].U0;
                    Slab(writer, side, "trim", u - (jw / 2f), u + (jw / 2f), z, z + Storey, -jout, 0f);
                }
            }
        }

        Slab(writer, side, "wall", a, b, top, top + look.Parapet, 0f, MidriseWall);
        Slab(writer, side, "trim", a, b, top + look.Parapet, top + look.Parapet + .1f, -.05f, MidriseWall + .05f);
    }

    private static void PlantRooms(Writer writer, Rect rect, float z, SlabLook look)
    {
        (float w, float d, float h) = look.Plant;
        bool longX = rect.Width >= rect.Depth;
        float length = longX ? rect.Width : rect.Depth;
        foreach (float c in Entries(length, look.PlantEvery))
        {
            Vector3 low, high;
            if (longX)
            {
                float cx = rect.X0 + c, cy = (rect.Y0 + rect.Y1) / 2f;
                low = new Vector3(cx - (w / 2f), cy - (d / 2f), z);
                high = new Vector3(cx + (w / 2f), cy + (d / 2f), z + h);
            }
            else
            {
                float cx = (rect.X0 + rect.X1) / 2f, cy = rect.Y0 + c;
                low = new Vector3(cx - (d / 2f), cy - (w / 2f), z);
                high = new Vector3(cx + (d / 2f), cy + (w / 2f), z + h);
            }

            Box(writer, low, high, "wall-end");
            Box(writer, new Vector3(low.X - .1f, low.Y - .1f, high.Z), new Vector3(high.X + .1f, high.Y + .1f, high.Z + .15f), "trim");
        }
    }
}
