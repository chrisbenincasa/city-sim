using System.Numerics;

namespace Borough.Appearance;

/// <summary>A generated body: one mesh per material part, in the order parts were first used.</summary>
public sealed class FamilyBodyMesh
{
    private readonly List<(string Part, ShellMesh Mesh)> _parts = [];

    public IReadOnlyList<(string Part, ShellMesh Mesh)> Parts => _parts;

    internal ShellMesh Part(string name)
    {
        foreach ((string part, ShellMesh mesh) in _parts)
        {
            if (part == name) return mesh;
        }

        var fresh = new ShellMesh();
        _parts.Add((name, fresh));
        return fresh;
    }
}

/// <summary>
/// Builds a family's body at a Building's own size. The street face looks down +Z, the origin is
/// the footprint centre at ground level, and every storey is <see cref="ShellBuilder.StoreyMetres"/>.
/// </summary>
/// <remarks>
/// It follows <c>scripts/art/test-street.py</c>, which authors in Blender's Z-up frame with the
/// street face down -Y. Geometry is written in that frame and turned into Godot's Y-up frame as
/// each polygon is emitted, so the two sources stay line-for-line comparable. UVs are projected
/// the way the script's <c>project_uvs</c> does, at each part's true tile size.
/// </remarks>
public static class FamilyBodyBuilder
{
    private const float Storey = ShellBuilder.StoreyMetres;
    private const float Reveal = .18f;
    private const float PlinthHeight = .3f;

    private static readonly int[][] BoxFaces = [[0, 3, 2, 1], [4, 5, 6, 7], [0, 1, 5, 4], [1, 2, 6, 5], [2, 3, 7, 6], [3, 0, 4, 7]];

    /// <param name="attached">Side walls shared with a neighbour. They are blank, and a gable stops at them or hips down to a crosswise one.</param>
    public static FamilyBodyMesh Build(FamilyBody body, float frontage, float depth, int storeys,
        AttachedSides attached = AttachedSides.None)
    {
        ArgumentNullException.ThrowIfNull(body);
        var mesh = new FamilyBodyMesh();
        var writer = new Writer(mesh, body);
        float height = storeys * Storey;
        float x = frontage / 2f;
        float y = depth / 2f;

        var front = new Face(new Vector3(-x, -y, 0), Vector3.UnitX, -Vector3.UnitY);
        var back = new Face(new Vector3(x, y, 0), -Vector3.UnitX, Vector3.UnitY);
        var left = new Face(new Vector3(-x, y, 0), -Vector3.UnitY, -Vector3.UnitX);
        var right = new Face(new Vector3(x, -y, 0), Vector3.UnitY, Vector3.UnitX);

        var entries = new List<float>();
        var rollers = new List<(float Low, float High)>();
        Facade(writer, body, front, frontage, height, storeys, body.Street, street: true, entries, null);
        Facade(writer, body, back, frontage, height, storeys, body.Back, street: false, null, rollers);
        var party = new WallRule(BayRow.Blank, BayRow.Blank);
        Facade(writer, body, left, depth, height, storeys, attached.HasFlag(AttachedSides.Left) ? party : body.Side, street: false, null, null);
        Facade(writer, body, right, depth, height, storeys, attached.HasFlag(AttachedSides.Right) ? party : body.Side, street: false, null, null);

        float plinthLeft = attached.HasFlag(AttachedSides.Left) ? -x : -x - .05f;
        float plinthRight = attached.HasFlag(AttachedSides.Right) ? x : x + .05f;
        writer.Box(new Vector3(plinthLeft, -y - .05f, 0), new Vector3(plinthRight, y + .05f, PlinthHeight), "plinth");
        if (body.GableDegrees > 0f) Gable(writer, body, frontage, depth, height, attached);
        else if (body.ParapetMetres > 0f) Parapet(writer, frontage, depth, height, body.ParapetMetres);
        else writer.Box(new Vector3(-x, -y, height - .2f), new Vector3(x, y, height), "membrane");

        if (body.Pilasters)
        {
            int bays = Bays(frontage, body.BayMetres);
            for (int bay = 0; bay <= bays; bay++)
            {
                float at = -x + (bay * frontage / bays);
                float top = height + body.ParapetMetres;
                writer.Box(new Vector3(at - .2f, -y - .2f, PlinthHeight), new Vector3(at + .2f, -y, top), "wall-end");
                writer.Box(new Vector3(at - .2f, y, PlinthHeight), new Vector3(at + .2f, y + .2f, top), "wall-end");
            }
        }

        foreach (float centre in entries)
        {
            writer.Box(new Vector3(centre - 2f, -y - 1.4f, 3.1f), new Vector3(centre + 2f, -y - .2f, 3.25f), "trim");
        }

        foreach ((float low, float high) in rollers)
        {
            writer.Box(new Vector3(low - .3f, y + .2f, 4.5f), new Vector3(high + .3f, y + 2.2f, 4.65f), "metal");
        }

        for (int unit = 0; unit < body.Plant; unit++)
        {
            float at = -x + (frontage * (unit + 1) / (body.Plant + 1));
            writer.Box(new Vector3(at - 2.5f, -2f, height), new Vector3(at + 2.5f, 2f, height + .35f), "metal");
            writer.Box(new Vector3(at - 2f, -1.5f, height + .35f), new Vector3(at + 2f, 1.5f, height + 1.5f), "metal");
        }

        if (body.RoofHatch) writer.Box(new Vector3(-1f, 1f, height), new Vector3(1f, 2.2f, height + .5f), "metal");
        for (int vent = 0; vent < body.Vents; vent++)
        {
            float at = -x + (frontage * (vent + 1) / (body.Vents + 1));
            writer.Box(new Vector3(at - .4f, 3f, height), new Vector3(at + .4f, 3.8f, height + .9f), "metal");
        }

        return mesh;
    }

    /// <summary>The whole bays a wall holds, each as near the family's bay width as the wall allows.</summary>
    public static int Bays(float length, float bayMetres) => Math.Max(1, (int)MathF.Round(length / bayMetres));

    private static void Facade(Writer writer, FamilyBody body, Face face, float width, float height, int storeys,
        WallRule rule, bool street, List<float>? entries, List<(float, float)>? rollers)
    {
        int bays = Bays(width, body.BayMetres);
        float bay = width / bays;
        BayKind[] ground = rule.Ground.Over(bays);
        BayKind[] above = rule.Upper.Over(bays);
        var openings = new List<Opening>();
        for (int storey = 0; storey < storeys; storey++)
        {
            BayKind[] row = storey == 0 ? ground : above;
            for (int b = 0; b < bays; b++)
            {
                if (storey == 0 && row[b] == BayKind.Hall)
                {
                    int last = b;
                    while (last + 1 < bays && row[last + 1] == BayKind.Hall) last++;
                    float centre = (b + last + 1) * bay / 2f;
                    openings.Add(new Opening(centre - 1.2f, .3f, 2.4f, 2.5f, OpeningKind.Door));
                    if (street) writer.Slab(face, centre - 1.8f, centre + 1.8f, 2.9f, 3.05f, -1.2f, 0f, "trim");
                    b = last;
                    continue;
                }

                Openings(openings, body.Openings, row[b], b * bay, bay, storey * Storey, storey == 0, height);
                if (storey != 0) continue;
                if (row[b] == BayKind.Entry) entries?.Add(face.AlongX(b * bay + .8f + 1.2f));
                if (row[b] == BayKind.Roller) rollers?.Add(face.SpanX(b * bay + .8f, (b + 1) * bay - .8f));
                if (row[b] == BayKind.Door && street && body.Steps)
                {
                    float at = openings[^1].U + (openings[^1].W / 2f);
                    writer.Slab(face, at - .75f, at + .75f, 0f, PlinthHeight, -1f, 0f, "plinth");
                }

                if (row[b] == BayKind.Door && storeys > 1 && above[b] == BayKind.Stair)
                {
                    Opening door = openings[^1];
                    float top = door.V + door.H;
                    writer.Slab(face, door.U - .3f, door.U + door.W + .3f, top + .1f, top + .25f, -.5f, 0f, "trim");
                }
            }
        }

        openings.RemoveAll(o => !(o.U > 0f && o.U + o.W < width && o.V > 0f && o.V + o.H < height));
        writer.Facade(face, width, height, openings);
    }

    private static void Openings(List<Opening> openings, IReadOnlyDictionary<BayKind, OpeningSize> sizes, BayKind kind,
        float start, float bay, float floor, bool ground, float roof)
    {
        float centre = start + bay / 2f;
        switch (kind)
        {
            case BayKind.Window or BayKind.Hall:
                Centred(BayKind.Window, new OpeningSize(1.4f, 1.7f, .95f), OpeningKind.Window);
                break;
            case BayKind.Shop:
                openings.Add(new Opening(start + .8f, floor + 1f, bay - 1.6f, 1.9f, OpeningKind.Shop));
                break;
            case BayKind.Entry when ground:
                openings.Add(new Opening(start + .8f, floor + .3f, 2.4f, 2.6f, OpeningKind.Door));
                float shop = bay - 4.4f;
                if (shop >= 1f) openings.Add(new Opening(start + 3.6f, floor + 1f, shop, 1.9f, OpeningKind.Shop));
                break;
            case BayKind.Entry:
                openings.Add(new Opening(start + .8f, floor + 1f, bay - 1.6f, 1.9f, OpeningKind.Shop));
                break;
            case BayKind.Door:
                Centred(BayKind.Door, new OpeningSize(1f, 2.3f, .3f), OpeningKind.Door);
                break;
            case BayKind.Roller when ground:
                openings.Add(new Opening(start + .8f, floor + .3f, bay - 1.6f, 4f, OpeningKind.Roller));
                break;
            case BayKind.Stair:
                Centred(BayKind.Stair, ground ? new OpeningSize(4f, 2.5f, .3f) : new OpeningSize(4f, 1.9f, 1f), OpeningKind.Stair);
                break;
        }

        // An opening keeps 0.4 m of wall either side within its bay, and stops 0.3 m under the roof deck.
        void Centred(BayKind sized, OpeningSize own, OpeningKind drawn)
        {
            OpeningSize size = sizes.TryGetValue(sized, out OpeningSize given) ? given with { Sill = given.Sill ?? own.Sill } : own;
            float w = Math.Min(size.Width, bay - .8f);
            float foot = floor + size.Sill!.Value;
            openings.Add(new Opening(centre - w / 2f, foot, w, Math.Min(size.Height, roof - .3f - foot), drawn));
        }
    }

    /// <summary>
    /// A pitched roof whose rafters span the depth, so the ridge runs along the street. It overhangs a
    /// free end and stops at a shared one, where a party-wall upstand covers the joint. The upstand is
    /// half the wall's thickness, so two neighbours make one whole.
    /// </summary>
    private static void Gable(Writer writer, FamilyBody body, float width, float depth, float height, AttachedSides attached)
    {
        const float Eaves = .4f, Thick = .2f, Upstand = .15f;
        float slope = MathF.Tan(body.GableDegrees * MathF.PI / 180f);
        float x = width / 2f, y = depth / 2f, eave = y + Eaves;
        bool left = attached.HasFlag(AttachedSides.Left), right = attached.HasFlag(AttachedSides.Right);
        bool hipLeft = attached.HasFlag(AttachedSides.LeftCrosswise), hipRight = attached.HasFlag(AttachedSides.RightCrosswise);
        float x0 = hipLeft ? -x - Eaves : left ? -x : -x - (Eaves / 2f);
        float x1 = hipRight ? x + Eaves : right ? x : x + (Eaves / 2f);
        float r0 = hipLeft ? x0 + eave : x0, r1 = hipRight ? x1 - eave : x1;
        if (r0 > r1) r0 = r1 = (r0 + r1) / 2f;

        float low = height - (Eaves * slope), top = height + (y * slope);
        Vector3 a = new(x0, -eave, low), b = new(x1, -eave, low), c = new(x1, eave, low), d = new(x0, eave, low);
        Vector3 p = new(r0, 0f, top), q = new(r1, 0f, top), up = new(0f, 0f, Thick);
        Sheet([a, b, q, p]);
        Sheet([c, d, p, q]);
        if (hipLeft) Sheet([d, a, p]);
        else
        {
            writer.Polygon("roof", [a, a + up, p + up, p]);
            writer.Polygon("roof", [p, p + up, d + up, d]);
            writer.Polygon("wall", [new(-x, 0f, top), new(-x, y, height), new(-x, -y, height)]);
        }

        if (hipRight) Sheet([b, c, q]);
        else
        {
            writer.Polygon("roof", [q, q + up, b + up, b]);
            writer.Polygon("roof", [c, c + up, q + up, q]);
            writer.Polygon("wall", [new(x, -y, height), new(x, y, height), new(x, 0f, top)]);
        }

        writer.Polygon("roof", [a, b, b + up, a + up]);
        writer.Polygon("roof", [c, d, d + up, c + up]);
        if (hipLeft) writer.Polygon("roof", [d, a, a + up, d + up]);
        if (hipRight) writer.Polygon("roof", [b, c, c + up, b + up]);

        float edge = y + Upstand;
        Vector2[] profile =
        [
            new(-edge, height - .3f), new(edge, height - .3f), new(edge, Surface(edge) + .25f),
            new(0f, Surface(0f) + .25f), new(-edge, Surface(edge) + .25f),
        ];
        if (left && !hipLeft) writer.Prism(profile, -x, -x + Upstand, "wall");
        if (right && !hipRight) writer.Prism(profile, x - Upstand, x, "wall");
        if (body.Chimney && !(hipLeft && hipRight))
        {
            float stack = hipRight ? -x + 1f : x - 1.6f;
            writer.Box(new Vector3(stack, .8f, Surface(1.4f) - .6f), new Vector3(stack + .6f, 1.4f, Surface(0f) + .9f), "wall-end");
        }

        float Surface(float distance) => top + Thick - (MathF.Abs(distance) * slope);

        void Sheet(ReadOnlySpan<Vector3> under)
        {
            Span<Vector3> over = stackalloc Vector3[under.Length];
            Span<Vector3> below = stackalloc Vector3[under.Length];
            int n = 0;
            foreach (Vector3 corner in under)
            {
                if (n > 0 && corner == over[n - 1] - up) continue;
                over[n++] = corner + up;
            }

            for (int i = 0; i < n; i++) below[i] = over[n - 1 - i] - up;
            writer.Polygon("roof", over[..n]);
            writer.Polygon("roof", below[..n]);
        }
    }

    private static void Parapet(Writer writer, float width, float depth, float height, float parapet)
    {
        float x = width / 2f, y = depth / 2f, top = height + parapet;
        writer.Box(new Vector3(-x + .25f, -y + .25f, height - .2f), new Vector3(x - .25f, y - .25f, height), "membrane");
        writer.Box(new Vector3(-x, -y, height), new Vector3(x, -y + .25f, top), "wall");
        writer.Box(new Vector3(-x, y - .25f, height), new Vector3(x, y, top), "wall");
        writer.Box(new Vector3(-x, -y + .25f, height), new Vector3(-x + .25f, y - .25f, top), "wall");
        writer.Box(new Vector3(x - .25f, -y + .25f, height), new Vector3(x, y - .25f, top), "wall");
        writer.Box(new Vector3(-x - .05f, -y - .05f, top), new Vector3(x + .05f, -y + .3f, top + .08f), "trim");
        writer.Box(new Vector3(-x - .05f, y - .3f, top), new Vector3(x + .05f, y + .05f, top + .08f), "trim");
        writer.Box(new Vector3(-x - .05f, -y + .3f, top), new Vector3(-x + .3f, y - .3f, top + .08f), "trim");
        writer.Box(new Vector3(x - .3f, -y + .3f, top), new Vector3(x + .05f, y - .3f, top + .08f), "trim");
    }

    private enum OpeningKind : byte
    {
        Window,
        Shop,
        Door,
        Roller,
        Stair,
    }

    private readonly record struct Opening(float U, float V, float W, float H, OpeningKind Kind);

    /// <summary>
    /// A wall plane. U runs along the wall to the right of someone outside facing it, V runs up, and
    /// depth runs inward.
    /// </summary>
    private readonly record struct Face(Vector3 Origin, Vector3 Along, Vector3 Outward)
    {
        public Vector3 Point(float u, float v, float depth = 0f) =>
            Origin + (Along * u) - (Outward * depth) + new Vector3(0, 0, v);

        public float AlongX(float u) => Point(u, 0).X;

        public (float Low, float High) SpanX(float u0, float u1)
        {
            float a = AlongX(u0), b = AlongX(u1);
            return (Math.Min(a, b), Math.Max(a, b));
        }
    }

    private sealed class Writer(FamilyBodyMesh mesh, FamilyBody body)
    {

        public void Facade(Face face, float width, float height, List<Opening> openings)
        {
            var us = new SortedSet<float> { 0f, width };
            var vs = new SortedSet<float> { 0f, height };
            foreach (Opening o in openings)
            {
                us.Add(o.U);
                us.Add(o.U + o.W);
                vs.Add(o.V);
                vs.Add(o.V + o.H);
            }

            float[] u = [.. us];
            float[] v = [.. vs];
            for (int i = 0; i + 1 < u.Length; i++)
            {
                for (int j = 0; j + 1 < v.Length; j++)
                {
                    float mu = (u[i] + u[i + 1]) / 2f, mv = (v[j] + v[j + 1]) / 2f;
                    if (openings.Exists(o => o.U < mu && mu < o.U + o.W && o.V < mv && mv < o.V + o.H)) continue;
                    Quad("wall", face.Point(u[i], v[j]), face.Point(u[i + 1], v[j]), face.Point(u[i + 1], v[j + 1]), face.Point(u[i], v[j + 1]));
                }
            }

            foreach (Opening o in openings)
            {
                Recess(face, o);
            }
        }

        private void Recess(Face face, Opening o)
        {
            bool solid = o.Kind is OpeningKind.Door or OpeningKind.Roller;
            float depth = Reveal + (solid ? .05f : 0f);
            (float, float)[] corners = [(o.U, o.V), (o.U + o.W, o.V), (o.U + o.W, o.V + o.H), (o.U, o.V + o.H)];
            for (int i = 0; i < 4; i++)
            {
                (float p, float q) = corners[i];
                (float r, float s) = corners[(i + 1) % 4];
                Quad("trim", face.Point(p, q), face.Point(r, s), face.Point(r, s, depth), face.Point(p, q, depth));
            }

            Quad(solid ? "door" : "glass", face.Point(o.U, o.V, depth), face.Point(o.U + o.W, o.V, depth),
                face.Point(o.U + o.W, o.V + o.H, depth), face.Point(o.U, o.V + o.H, depth));

            if (o.Kind == OpeningKind.Window)
            {
                Slab(face, o.U - .06f, o.U + o.W + .06f, o.V - .07f, o.V, -.06f, .02f, "trim");
            }

            if (o.Kind is OpeningKind.Shop or OpeningKind.Stair or OpeningKind.Window && o.W > 1.6f)
            {
                int count = (int)MathF.Round(o.W / 1.5f) - 1;
                for (int i = 1; i <= count; i++)
                {
                    float at = o.U + (o.W * i / (count + 1));
                    Slab(face, at - .035f, at + .035f, o.V, o.V + o.H, depth - .08f, depth, "frame");
                }
            }

            if (o.Kind == OpeningKind.Roller)
            {
                for (int i = 1; i < (int)(o.H / .5f); i++)
                {
                    Slab(face, o.U, o.U + o.W, o.V + (i * .5f) - .015f, o.V + (i * .5f) + .015f, depth - .03f, depth, "metal");
                }
            }
        }

        public void Slab(Face face, float u0, float u1, float v0, float v1, float d0, float d1, string part)
        {
            Span<Vector3> corners =
            [
                face.Point(u0, v0, d0), face.Point(u1, v0, d0), face.Point(u1, v0, d1), face.Point(u0, v0, d1),
                face.Point(u0, v1, d0), face.Point(u1, v1, d0), face.Point(u1, v1, d1), face.Point(u0, v1, d1),
            ];
            Solid(corners, part);
        }

        public void Box(Vector3 low, Vector3 high, string part)
        {
            Span<Vector3> corners =
            [
                new(low.X, low.Y, low.Z), new(high.X, low.Y, low.Z), new(high.X, high.Y, low.Z), new(low.X, high.Y, low.Z),
                new(low.X, low.Y, high.Z), new(high.X, low.Y, high.Z), new(high.X, high.Y, high.Z), new(low.X, high.Y, high.Z),
            ];
            Solid(corners, part);
        }

        /// <summary>Eight corners in the order the script's <c>BOX_FACES</c> reads them.</summary>
        private void Solid(ReadOnlySpan<Vector3> corners, string part)
        {
            foreach (int[] f in BoxFaces)
            {
                Quad(part, corners[f[0]], corners[f[1]], corners[f[2]], corners[f[3]]);
            }
        }

        /// <summary>
        /// A solid from <paramref name="x0"/> to <paramref name="x1"/> whose cross-section is a convex
        /// (y, z) profile, anticlockwise seen from +X, as the script's <c>prism</c> builds it.
        /// </summary>
        public void Prism(ReadOnlySpan<Vector2> profile, float x0, float x1, string part)
        {
            int n = profile.Length;
            Span<Vector3> near = stackalloc Vector3[n];
            Span<Vector3> far = stackalloc Vector3[n];
            for (int i = 0; i < n; i++)
            {
                near[i] = new Vector3(x0, profile[i].X, profile[i].Y);
                far[i] = new Vector3(x1, profile[i].X, profile[i].Y);
            }

            Span<Vector3> cap = stackalloc Vector3[n];
            for (int i = 0; i < n; i++) cap[i] = near[n - 1 - i];
            Polygon(part, cap);
            Polygon(part, far);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Quad(part, near[i], near[j], far[j], far[i]);
            }
        }

        private void Quad(string part, Vector3 a, Vector3 b, Vector3 c, Vector3 d) => Polygon(part, [a, b, c, d]);

        /// <summary>A convex polygon in the script's frame, anticlockwise seen from outside.</summary>
        public void Polygon(string part, ReadOnlySpan<Vector3> points)
        {
            Vector3 cross = Vector3.Cross(points[1] - points[0], points[^1] - points[0]);
            if (cross.LengthSquared() < 1e-10f) return;
            Vector3 n = Vector3.Normalize(cross);

            (float tileAlong, float tileUp) = body.TileMetres.TryGetValue(part, out (float, float) tile) ? tile : (1f, 1f);
            Vector3 along = MathF.Abs(n.Z) < .95f ? Vector3.Normalize(new Vector3(-n.Y, n.X, 0)) : Vector3.UnitX;
            Vector3 up = Vector3.Cross(n, along);

            Span<Vector3> corners = stackalloc Vector3[points.Length];
            Span<Vector2> uvs = stackalloc Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[points.Length - 1 - i];
                corners[i] = Godot(p);
                uvs[i] = new Vector2(Vector3.Dot(p, along) / tileAlong, 1f - (Vector3.Dot(p, up) / tileUp));
            }

            mesh.Part(part).Polygon(corners, uvs, Godot(n));
        }

        private static Vector3 Godot(Vector3 v) => new(v.X, v.Z, -v.Y);
    }
}
