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
                Vector3 a = vertices[indices[t + (corner+1)%3]], b = vertices[indices[t + (corner+2)%3]];
                bool diagonal = false;
                for (int other = 0; other < indices.Length; other += 3)
                {
                    if (other == t || Mathf.Abs(normal.Dot(Face(other))) < .9999f) continue;
                    bool hasA = false, hasB = false;
                    for (int v = 0; v < 3; v++)
                    {
                        Vector3 p = vertices[indices[other+v]];
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
                surface.AddVertex(vertices[indices[t+corner]]);
            }
        }
        return surface.Commit();

        Vector3 Face(int t) => (vertices[indices[t+1]] - vertices[indices[t]])
            .Cross(vertices[indices[t+2]] - vertices[indices[t]]).Normalized();
    }
    private static Mesh Source(int family)
    {
        if (family == 0) return new PrismMesh { Size = Vector3.One };
        var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
        if (family == 1)
        {
            Vector3 a = new(-.5f,-.5f,-.5f), b = new(.5f,-.5f,-.5f);
            Vector3 c = new(.5f,-.5f,.5f), d = new(-.5f,-.5f,.5f);
            Vector3 e = new(0,.5f,-.15f), f = new(0,.5f,.15f);
            Triangle(a,b,e); Triangle(b,c,f); Triangle(b,f,e);
            Triangle(c,d,f); Triangle(d,a,e); Triangle(d,e,f);
            Triangle(a,d,c); Triangle(a,c,b);
        }
        else
        {
            var arrays = new PrismMesh { Size = Vector3.One }.GetMeshArrays();
            Vector3[] v = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            int[] ix = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            for (int half = 0; half < 2; half++)
            for (int t = 0; t < ix.Length; t += 3)
            {
                Vector3 Point(int at) => new(v[at].X*.5f + (half == 0 ? -.25f : .25f), v[at].Y, v[at].Z);
                Triangle(Point(ix[t]), Point(ix[t+1]), Point(ix[t+2]));
            }
        }
        surface.Index(); return surface.Commit();

        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = (c-a).Cross(b-a).Normalized();
            surface.SetNormal(normal); surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c);
        }
    }

}
