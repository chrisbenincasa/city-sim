using Godot;

namespace Borough.Shell;

public static class RoofMeshes
{
    public static Mesh Create(int family)
    {
        var arrays = Source(family).SurfaceGetArrays(0);
        Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        int[] indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        for (int t = 0; t < indices.Length; t += 3)
        {
            Vector3 normal = Face(t);
            if (normal.LengthSquared() < .5f) continue; // Cone tips contain degenerate triangles.
            int mask = 0;
            for (int corner = 0; corner < 3; corner++)
            {
                Vector3 a = vertices[indices[t + (corner + 1) % 3]], b = vertices[indices[t + (corner + 2) % 3]];
                bool diagonal = false;
                for (int other = 0; other < indices.Length; other += 3)
                {
                    if (other == t || Mathf.Abs(normal.Dot(Face(other))) < .9999f) continue;
                    bool hasA = false, hasB = false;
                    for (int v = 0; v < 3; v++)
                    {
                        Vector3 p = vertices[indices[other + v]];
                        hasA |= p.IsEqualApprox(a); hasB |= p.IsEqualApprox(b);
                    }
                    if (hasA && hasB) { diagonal = true; break; }
                }
                if (!diagonal) mask |= 1 << corner;
            }
            if (normal.Dot(normals[indices[t]]) < 0) normal = -normal;
            for (int corner = 0; corner < 3; corner++)
            {
                surface.SetNormal(normal);
                // Roof materials use metre-space projection. UV is reserved for edge metadata;
                // UV2 carries barycentric coordinates, with the third component implicit.
                surface.SetUV(new Vector2(mask, -17));
                surface.SetUV2(corner == 0 ? Vector2.Right : corner == 1 ? Vector2.Down : Vector2.Zero);
                surface.AddVertex(vertices[indices[t + corner]]);
            }
        }
        return surface.Commit();

        Vector3 Face(int t) => (vertices[indices[t + 1]] - vertices[indices[t]])
            .Cross(vertices[indices[t + 2]] - vertices[indices[t]]).Normalized();
    }
    // How much of a parapet's own width the upstand takes, each side. Small on purpose: it is a
    // FRACTION of the body and a real coping is a thickness, so the two only agree at one span.
    // At the fixture's broad bodies -- 20 m to 60 m -- it draws 0.30 m to 0.90 m of upstand, which
    // is the plausible band. It would be wrong on a body far outside it.
    private const float Upstand = 0.015f;

    // Where the deck sits between the wall head and the coping, as a share of the parapet's height.
    // ⚠ THE DECK IS FLAT AND THE RESEARCH'S FALL IS NOT DRAWN. pass03 proposes 1:60 across a 10 m
    // run, which is 167 mm -- less than the upstand it drains behind, and under a pixel at every
    // distance this city is looked at from. ***Drawing it would be asserting a gradient nobody can
    // see***; what reads is that the deck is BELOW the coping and the roof is a tray rather than
    // a lid.
    private const float Deck = 0.30f;

    private static Mesh Source(int family)
    {
        if (family == 0) return new PrismMesh { Size = Vector3.One };
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        if (family == 3)
        {
            float outer = .5f, inner = .5f - Upstand, deck = -.5f + Deck;

            // The coping, the two skins it caps, and the tray inside them. Four sides, and each is
            // the same four quads turned -- so the seams fall on the corners rather than across a
            // face, which is where a real coping's joints are.
            for (int side = 0; side < 4; side++)
            {
                // (ex, ez) runs ALONG this side; (nx, nz) is its outward normal.
                float nx = side == 0 ? 0 : side == 1 ? 1 : side == 2 ? 0 : -1;
                float nz = side == 0 ? -1 : side == 1 ? 0 : side == 2 ? 1 : 0;
                float ex = -nz, ez = nx;

                // ⚠ THE CORNER BELONGS TO THE ±Z SIDES, and it is the same partition Wings() takes
                // for the same reason. Two copings meeting at a corner are COPLANAR, so a shared
                // corner square is z-fighting rather than a seam -- 0.3 m of it flickering on every
                // roof in the city. The outer skins are perpendicular and meet at an edge, so they
                // keep their full run.
                float end = side % 2 == 0 ? outer : inner;

                Vector3 At(float across, float along, float y) =>
                    new(nx * across + ex * along, y, nz * across + ez * along);

                // The outer skin, full height.
                Quad(At(outer, -outer, -.5f), At(outer, outer, -.5f),
                     At(outer, outer, .5f), At(outer, -outer, .5f));

                // The coping, from the outer edge in to the upstand's inner face.
                Quad(At(outer, -end, .5f), At(outer, end, .5f),
                     At(inner, end, .5f), At(inner, -end, .5f));

                // The inner skin, from the coping down to the deck, facing IN across the tray.
                Quad(At(inner, end, .5f), At(inner, end, deck),
                     At(inner, -end, deck), At(inner, -end, .5f));
            }

            // The tray. Flat, and see Deck.
            Quad(new Vector3(-inner, deck, -inner), new Vector3(inner, deck, -inner),
                 new Vector3(inner, deck, inner), new Vector3(-inner, deck, inner));

            surface.Index(); return surface.Commit();
        }
        if (family == 1)
        {
            Vector3 a = new(-.5f, -.5f, -.5f), b = new(.5f, -.5f, -.5f);
            Vector3 c = new(.5f, -.5f, .5f), d = new(-.5f, -.5f, .5f);
            Vector3 e = new(0, .5f, -.15f), f = new(0, .5f, .15f);
            Triangle(a, b, e); Triangle(b, c, f); Triangle(b, f, e);
            Triangle(c, d, f); Triangle(d, a, e); Triangle(d, e, f);
            Triangle(a, d, c); Triangle(a, c, b);
        }
        else
        {
            var arrays = new PrismMesh { Size = Vector3.One }.GetMeshArrays();
            Vector3[] v = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            int[] ix = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            for (int half = 0; half < 2; half++)
                for (int t = 0; t < ix.Length; t += 3)
                {
                    Vector3 Point(int at) => new(v[at].X * .5f + (half == 0 ? -.25f : .25f), v[at].Y, v[at].Z);
                    Triangle(Point(ix[t]), Point(ix[t + 1]), Point(ix[t + 2]));
                }
        }
        surface.Index(); return surface.Commit();

        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = (c - a).Cross(b - a).Normalized();
            surface.SetNormal(normal); surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
        }

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Triangle(a, b, c); Triangle(a, c, d);
        }
    }

}
