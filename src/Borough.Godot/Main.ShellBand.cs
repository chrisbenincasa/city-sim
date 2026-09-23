using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Borough.Appearance;
using Godot;

namespace Borough.Shell;

/// <summary>
/// A measurement probe for the procedural Building generator (research/procedural-buildings
/// REPORT §3). It replaces every Building with a wing centre within a radius of the camera by
/// generated shells, and times collection, generation and main-thread upload. Requires
/// BOROUGH_RENDER_PROFILE=1.
/// </summary>
public partial class Main
{
    private readonly HashSet<ulong> _shelled = [];
    private readonly List<GeometryInstance3D> _shellNodes = [];
    private StandardMaterial3D? _shellMaterial;
    private StandardMaterial3D? _kitMaterial;
    private string _shellBandReport = string.Empty;

    private sealed class ShellChunk
    {
        public readonly List<Massing> Wings = [];
        public Vector3[] Positions = [];
        public Vector3[] Normals = [];
        public Vector2[] Uvs = [];
        public Color[] Colors = [];
        public int[] Indices = [];
        public readonly List<Transform3D> Kit = [];
    }

    /// <summary><c>ui shell-band RADIUS CHUNK KIT SHADOWS</c>; a radius of 0 restores the boxes.</summary>
    private void ShellBand(string[] words)
    {
        if (System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_PROFILE") is null)
        {
            _refused = "Shell-band measurements require BOROUGH_RENDER_PROFILE=1.";
            return;
        }

        if (words.Length != 5
            || !float.TryParse(words[1], CultureInfo.InvariantCulture, out float radius)
            || !float.TryParse(words[2], CultureInfo.InvariantCulture, out float chunkMetres)
            || !int.TryParse(words[3], CultureInfo.InvariantCulture, out int kit)
            || words[4] is not ("on" or "off"))
        {
            _refused = "shell-band RADIUS CHUNK KIT on|off";
            return;
        }

        foreach (GeometryInstance3D node in _shellNodes)
        {
            node.QueueFree();
        }

        _shellNodes.Clear();
        _shelled.Clear();
        _world.Changes!.Invalidate();
        _shellBandReport = string.Empty;
        if (radius <= 0f)
        {
            return;
        }

        bool shadows = words[4] == "on";
        var total = Stopwatch.StartNew();
        var chunks = new Dictionary<(long, long), ShellChunk>();
        Vector3 eye = _camera.GlobalPosition;
        List<Massing> all = [.. Buildings()];
        foreach (Massing one in all)
        {
            if (one.Body.Origin.DistanceTo(eye) <= radius) _shelled.Add(one.Id);
        }

        foreach (Massing one in all)
        {
            if (!_shelled.Contains(one.Id)) continue;
            Vector3 at = one.Body.Origin;
            (long, long) key = chunkMetres > 0f
                ? ((long)Mathf.Floor(at.X / chunkMetres), (long)Mathf.Floor(at.Z / chunkMetres))
                : ((long)one.Id, 0L);
            if (!chunks.TryGetValue(key, out ShellChunk? chunk)) chunks[key] = chunk = new ShellChunk();
            chunk.Wings.Add(one);
        }

        double collectMs = total.Elapsed.TotalMilliseconds;
        ShellChunk[] work = [.. chunks.Values];
        long vertices = 0;
        long triangles = 0;
        var generate = Stopwatch.StartNew();
        Parallel.ForEach(work, () => new ShellMesh(), (chunk, _, mesh) =>
        {
            mesh.Clear();
            ulong previous = 0;
            foreach (Massing wing in chunk.Wings)
            {
                WingShape shape = Wing(wing);
                Vector3 size = wing.Body.Basis.Scale;
                Vector3 origin = wing.Body.Origin - new Vector3(0f, size.Y * 0.5f, 0f);
                ShellBuilder.Append(mesh, shape, new Frame(new System.Numerics.Vector3(origin.X, origin.Y, origin.Z), 1f, 0f));
                if (wing.Id != previous) Kit(chunk.Kit, shape, origin, kit);
                previous = wing.Id;
            }

            chunk.Positions = [.. mesh.Positions.ToArray().Select(v => new Vector3(v.X, v.Y, v.Z))];
            chunk.Normals = [.. mesh.Normals.ToArray().Select(v => new Vector3(v.X, v.Y, v.Z))];
            chunk.Uvs = [.. mesh.Uvs.ToArray().Select(v => new Vector2(v.X, v.Y))];
            chunk.Colors = [.. mesh.Colors.ToArray().Select(v => new Color(v.X, v.Y, v.Z, v.W))];
            chunk.Indices = mesh.Indices.ToArray();
            return mesh;
        }, _ => { });
        double generateMs = generate.Elapsed.TotalMilliseconds;

        _shellMaterial ??= new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 0.85f };
        _kitMaterial ??= new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.79f, 0.74f), Roughness = 0.8f };
        var box = new BoxMesh { Size = Vector3.One };
        GeometryInstance3D.ShadowCastingSetting cast = shadows
            ? GeometryInstance3D.ShadowCastingSetting.On
            : GeometryInstance3D.ShadowCastingSetting.Off;

        var upload = Stopwatch.StartNew();
        double slowest = 0;
        foreach (ShellChunk chunk in work)
        {
            double before = upload.Elapsed.TotalMilliseconds;
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            arrays[(int)Mesh.ArrayType.Vertex] = chunk.Positions;
            arrays[(int)Mesh.ArrayType.Normal] = chunk.Normals;
            arrays[(int)Mesh.ArrayType.TexUV] = chunk.Uvs;
            arrays[(int)Mesh.ArrayType.Color] = chunk.Colors;
            arrays[(int)Mesh.ArrayType.Index] = chunk.Indices;
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(0, _shellMaterial);
            var node = new MeshInstance3D { Mesh = mesh, CastShadow = cast };
            AddChild(node);
            _shellNodes.Add(node);
            vertices += chunk.Positions.Length;
            triangles += chunk.Indices.Length / 3;

            if (chunk.Kit.Count > 0)
            {
                var pieces = new MultiMesh
                {
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                    Mesh = box,
                    InstanceCount = chunk.Kit.Count,
                };
                for (int i = 0; i < chunk.Kit.Count; i++) pieces.SetInstanceTransform(i, chunk.Kit[i]);
                var kitNode = new MultiMeshInstance3D { Multimesh = pieces, MaterialOverride = _kitMaterial, CastShadow = cast };
                AddChild(kitNode);
                _shellNodes.Add(kitNode);
            }

            slowest = System.Math.Max(slowest, upload.Elapsed.TotalMilliseconds - before);
        }

        double uploadMs = upload.Elapsed.TotalMilliseconds;
        int kitPieces = work.Sum(c => c.Kit.Count);
        _shellBandReport = string.Create(CultureInfo.InvariantCulture,
            $"shell_band\t{radius}\t{chunkMetres}\t{kit}\t{words[4]}\t{_shelled.Count}\t{work.Length}\t{vertices}\t{triangles}\t{kitPieces}\t{collectMs:F3}\t{generateMs:F3}\t{uploadMs:F3}\t{slowest:F3}\n");
    }

    private static WingShape Wing(Massing wing)
    {
        Vector3 size = wing.Body.Basis.Scale;
        float east = wing.Reads.R * 2f - 1f;
        float south = wing.Reads.G * 2f - 1f;
        int street = System.Math.Abs(east) < 0.5f && System.Math.Abs(south) < 0.5f ? -1
            : System.Math.Abs(south) >= System.Math.Abs(east) ? (south > 0f ? 0 : 2)
            : (east > 0f ? 1 : 3);
        RoofForm roof = wing.Cap switch
        {
            Cap.Parapet => RoofForm.Parapet,
            Cap.Hip => RoofForm.Hip,
            Cap.Gable or Cap.PairedGable => RoofForm.Gable,
            _ => RoofForm.Flat,
        };
        int storeys = System.Math.Max(1, (int)System.MathF.Round(size.Y / ShellBuilder.StoreyMetres));
        return new WingShape(size.X, size.Z, storeys, street, roof, (uint)(wing.Id * 0x9E3779B97F4A7C15UL >> 32));
    }

    /// <summary>Placeholder kit pieces (sills, canopies, cornice blocks) standing proud of the street face.</summary>
    private static void Kit(List<Transform3D> pieces, WingShape wing, Vector3 origin, int count)
    {
        int face = wing.StreetFace < 0 ? 0 : wing.StreetFace;
        Vector3 outward = face switch { 0 => Vector3.Back, 1 => Vector3.Right, 2 => Vector3.Forward, _ => Vector3.Left };
        Vector3 right = Vector3.Up.Cross(outward);
        float length = face is 0 or 2 ? wing.Width : wing.Depth;
        float reach = face is 0 or 2 ? wing.Depth / 2f : wing.Width / 2f;
        var scale = Basis.FromScale(face is 0 or 2 ? new Vector3(1.2f, 0.15f, 0.35f) : new Vector3(0.35f, 0.15f, 1.2f));
        for (int i = 0; i < count; i++)
        {
            float along = ((i + 0.5f) / count - 0.5f) * length;
            float up = (i % wing.Storeys) * ShellBuilder.StoreyMetres + 0.85f;
            pieces.Add(new Transform3D(scale, origin + outward * (reach + 0.1f) + right * along + Vector3.Up * up));
        }
    }
}
