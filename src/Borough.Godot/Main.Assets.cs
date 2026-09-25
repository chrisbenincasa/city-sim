using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private readonly record struct Corner(Vector3 At, Vector3 Normal, Color Paint);

    /// <summary>
    /// A flat-coloured Blender export as triangle corners. Each material's albedo becomes a vertex
    /// colour, so the layer's stock material draws it and the instance colour multiplies it.
    /// </summary>
    /// <param name="painted">Materials baked white, so the instance colour is their paint.</param>
    private static (List<Corner> Corners, Dictionary<string, Color> Paints) Load(string asset, params string[] painted)
    {
        var scene = GD.Load<PackedScene>($"res://assets/{asset}.glb").Instantiate<Node3D>();
        var corners = new List<Corner>();
        var paints = new Dictionary<string, Color>();

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
                if (material is not null) paints[material.ResourceName] = paint;

                foreach (int at in indices)
                    corners.Add(new Corner(place * vertices[at], (place.Basis * normals[at]).Normalized(), paint));
            }
        }

        scene.Free();
        return (corners, paints);
    }

    private static Aabb Bounds(List<Corner> corners)
    {
        var bounds = new Aabb(corners[0].At, Vector3.Zero);
        foreach (Corner corner in corners) bounds = bounds.Expand(corner.At);
        return bounds;
    }

    /// <param name="unit">Rescales each axis so these bounds span 1 and stand on 0.</param>
    private static ArrayMesh Commit(List<Corner> corners, Aabb? unit = null)
    {
        Vector3 scale = unit is { } bounds ? Vector3.One / bounds.Size : Vector3.One;
        float floor = unit?.Position.Y ?? 0f;
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (var (at, normal, paint) in corners)
        {
            surface.SetNormal((normal / scale).Normalized());
            surface.SetColor(paint);
            surface.AddVertex(new Vector3(at.X * scale.X, (at.Y - floor) * scale.Y, at.Z * scale.Z));
        }

        return surface.Commit();
    }

    private static void Add(List<Corner> corners, PrimitiveMesh mesh, Vector3 at, Vector3 size, Color paint)
    {
        var arrays = mesh.GetMeshArrays();
        Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
        foreach (int i in arrays[(int)Mesh.ArrayType.Index].AsInt32Array())
            corners.Add(new Corner(at + (vertices[i] * size), (normals[i] / size).Normalized(), paint));
    }

    /// <summary>The round tree near the camera, a coarse stand-in beyond it, and a blob far away.</summary>
    /// <remarks>
    /// The stand-in repeats the trunk and nine crowns of <c>tree()</c> in
    /// <c>scripts/art/expanded-kit.py</c> with coarse spheres, and the blob is one crown round them
    /// all. Each is scaled by the detailed tree's bounds, so a chunk that changes mesh keeps its
    /// silhouette.
    /// </remarks>
    private static (ArrayMesh Near, ArrayMesh Middle, ArrayMesh Far) TreeMeshes()
    {
        var (corners, paints) = Load("visual-study/expanded/round-tree");
        Aabb bounds = Bounds(corners);
        var middle = new List<Corner>();
        var far = new List<Corner>();
        var crown = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 5, Rings = 2 };
        var trunk = new CylinderMesh
        {
            TopRadius = 1f,
            BottomRadius = 1f,
            Height = 1f,
            RadialSegments = 5,
            Rings = 0,
            CapTop = false,
            CapBottom = false
        };

        Add(middle, trunk, new Vector3(0f, 2.75f, 0f), new Vector3(0.23f, 5.5f, 0.23f), paints["bark"]);
        for (int j = 0; j < 9; j++)
        {
            float turn = j * 2.4f;
            float reach = j < 7 ? 1.4f : 0.5f;
            var at = new Vector3(Mathf.Cos(turn) * reach, 4.4f + (j % 3 * 0.65f), Mathf.Sin(turn) * reach);
            Add(middle, crown, at, new Vector3(1.45f, 1.35f, 1.25f), paints[j % 3 == 0 ? "foliage-dark" : "foliage"]);
        }

        trunk.RadialSegments = 3;
        Add(far, trunk, new Vector3(0f, 1.75f, 0f), new Vector3(0.3f, 3.5f, 0.3f), paints["bark"]);
        Add(far, new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 5, Rings = 1 },
            new Vector3(0f, 5.05f, 0f), new Vector3(2.8f, 2f, 2.6f), paints["foliage"]);

        return (Commit(corners, bounds), Commit(middle, bounds), Commit(far, bounds));
    }

    private static ArrayMesh WalkerMesh() => Commit(Load("visual-study/expanded/walker", "paint").Corners);

    private static ArrayMesh CarMesh() => Commit(Load("visual-study/kit/car-middle", "paint").Corners);

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
