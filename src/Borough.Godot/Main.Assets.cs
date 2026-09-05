using Godot;

namespace Borough.Shell;

public partial class Main
{
    // PROVISIONAL visual geometry and material choices, with no simulation meaning.
    // Parts are merged once at startup: one surface and one instance per tree/person/car.
    private static ArrayMesh Assemble(params (PrimitiveMesh Mesh, Vector3 At, Color Paint)[] parts)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var part in parts)
        {
            var arrays = part.Mesh.GetMeshArrays();
            Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            int[] indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            Color paint = part.Paint.SrgbToLinear();
            for (int i = 0; i < indices.Length; i++)
            {
                int at = indices[i];
                surface.SetNormal(normals[at]);
                surface.SetColor(paint);
                surface.AddVertex(vertices[at] + part.At);
            }
        }
        return surface.Commit();
    }

    private static BoxMesh Box(float x, float y, float z) => new() { Size = new Vector3(x, y, z) };
    private static SphereMesh CrownMesh(float radius, float height) =>
        new() { Radius = radius, Height = height, RadialSegments = 7, Rings = 4 };

    private static ArrayMesh TreeMesh() => Assemble(
        (new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.045f, Height = 0.52f,
            RadialSegments = 6, Rings = 0 }, new Vector3(0f, 0.26f, 0f), new Color(0.32f, 0.25f, 0.17f)),
        (CrownMesh(0.31f, 0.55f), new Vector3(-0.17f, 0.57f, 0.01f), new Color(0.29f, 0.42f, 0.18f)),
        (CrownMesh(0.30f, 0.52f), new Vector3(0.18f, 0.62f, 0.08f), new Color(0.25f, 0.38f, 0.16f)),
        (CrownMesh(0.29f, 0.55f), new Vector3(0f, 0.74f, -0.10f), new Color(0.35f, 0.46f, 0.21f)));

    private static ArrayMesh WalkerMesh() => Assemble(
        (Box(0.40f, 0.65f, 0.27f), new Vector3(0f, 1.12f, 0f), Colors.White),
        (CrownMesh(0.16f, 0.33f), new Vector3(0f, 1.62f, 0f), new Color(0.85f, 0.68f, 0.52f)),
        (Box(0.14f, 0.77f, 0.20f), new Vector3(-0.12f, 0.40f, 0f), new Color(0.22f, 0.25f, 0.29f)),
        (Box(0.14f, 0.77f, 0.20f), new Vector3(0.12f, 0.40f, 0f), new Color(0.22f, 0.25f, 0.29f)));

    private static ArrayMesh CarMesh()
    {
        var tyre = new Color(0.085f, 0.09f, 0.10f);
        var glass = new Color(0.24f, 0.34f, 0.40f);
        return Assemble(
            (Box(1.8f, 0.62f, 4.2f), new Vector3(0f, 0.64f, 0f), Colors.White),
            (Box(1.54f, 0.55f, 2.15f), new Vector3(0f, 1.21f, -0.12f), glass),
            (Box(1.6f, 0.10f, 2.18f), new Vector3(0f, 1.51f, -0.12f), Colors.White),
            (Box(0.12f, 0.57f, 0.12f), new Vector3(-0.79f, 1.20f, -0.12f), Colors.White),
            (Box(0.12f, 0.57f, 0.12f), new Vector3(0.79f, 1.20f, -0.12f), Colors.White),
            (CrownMesh(0.33f, 0.64f), new Vector3(-0.84f, 0.34f, -1.3f), tyre),
            (CrownMesh(0.33f, 0.64f), new Vector3(0.84f, 0.34f, -1.3f), tyre),
            (CrownMesh(0.33f, 0.64f), new Vector3(-0.84f, 0.34f, 1.3f), tyre),
            (CrownMesh(0.33f, 0.64f), new Vector3(0.84f, 0.34f, 1.3f), tyre),
            (Box(1.4f, 0.16f, 0.03f), new Vector3(0f, 0.72f, 2.11f), new Color(0.93f, 0.90f, 0.70f)),
            (Box(1.4f, 0.14f, 0.03f), new Vector3(0f, 0.72f, -2.11f), new Color(0.60f, 0.10f, 0.07f)));
    }

    private void DressSurfaces()
    {
        foreach (var layer in new[] { _roofs, _hips, _mansards, _roads, _footways, _kerbs })
        {
            var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://surfaces.gdshader") };
            material.SetShaderParameter("surface_kind", layer == _roads ? 0 : layer == _footways || layer == _kerbs ? 1 : 2);
            ((PrimitiveMesh)layer.Multimesh.Mesh).Material = material;
        }
    }
}
