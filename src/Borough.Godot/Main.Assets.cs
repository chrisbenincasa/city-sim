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

    /// <summary>
    /// A flat-coloured Blender export merged into one mesh. Each material's albedo becomes a vertex
    /// colour, so the layer's stock material draws it and the instance colour multiplies it.
    /// </summary>
    /// <param name="painted">Materials baked white, so the instance colour is their paint.</param>
    /// <param name="unit">Rescale each axis to span 1, for layers whose instances carry the size.</param>
    private static ArrayMesh Baked(string asset, bool unit, params string[] painted)
    {
        var scene = GD.Load<PackedScene>($"res://assets/{asset}.glb").Instantiate<Node3D>();
        var parts = new List<(Vector3 At, Vector3 Normal, Color Paint)>();
        var bounds = new Aabb();
        bool first = true;

        foreach (MeshInstance3D part in scene.FindChildren("*", nameof(MeshInstance3D), owned: false))
        {
            Transform3D place = part.Transform;
            for (Node? up = part.GetParent(); up is Node3D parent && up != scene; up = up.GetParent())
                place = parent.Transform * place;

            for (int s = 0; s < part.Mesh.GetSurfaceCount(); s++)
            {
                var arrays = part.Mesh.SurfaceGetArrays(s);
                Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                int[] indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                if (indices.Length == 0) indices = Enumerable.Range(0, vertices.Length).ToArray();
                var material = part.GetActiveMaterial(s) as BaseMaterial3D;
                Color paint = material is null || painted.Contains(material.ResourceName)
                    ? Colors.White
                    : material.AlbedoColor.SrgbToLinear();

                foreach (int at in indices)
                {
                    Vector3 point = place * vertices[at];
                    parts.Add((point, (place.Basis * normals[at]).Normalized(), paint));
                    bounds = first ? new Aabb(point, Vector3.Zero) : bounds.Expand(point);
                    first = false;
                }
            }
        }

        scene.Free();
        Vector3 scale = unit ? Vector3.One / bounds.Size : Vector3.One;
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var (at, normal, paint) in parts)
        {
            surface.SetNormal((normal / scale).Normalized());
            surface.SetColor(paint);
            surface.AddVertex(new Vector3(at.X * scale.X, (at.Y - bounds.Position.Y) * scale.Y, at.Z * scale.Z));
        }

        return surface.Commit();
    }

    private static ArrayMesh TreeMesh() => Baked("visual-study/expanded/round-tree", unit: true);

    private static ArrayMesh WalkerMesh() => Baked("visual-study/expanded/walker", unit: false, "paint");

    private static ArrayMesh CarMesh() => Baked("visual-study/kit/car-middle", unit: false, "paint");

    private void DressSurfaces()
    {
        foreach (var layer in new[] { _roofs, _hips, _pairedRoofs, _parapets, _roads, _footways, _kerbs })
        {
            var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://surfaces.gdshader") };
            if (layer.Multimesh.Mesh is ArrayMesh) RoofMaterials.Configure(material);
            material.SetShaderParameter("surface_kind",
                layer == _roads ? 0
                    : layer == _footways || layer == _kerbs ? 1
                    : layer == _parapets ? 3
                    : 2);
            if (layer.Multimesh.Mesh is ArrayMesh roof) roof.SurfaceSetMaterial(0, material);
            else ((PrimitiveMesh)layer.Multimesh.Mesh).Material = material;
        }
    }
}
