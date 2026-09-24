using System.Numerics;

namespace Borough.Appearance;

/// <summary>
/// Growable vertex and index arrays for one or more generated shells. Reused across Buildings, so
/// steady-state generation allocates nothing.
/// </summary>
public sealed class ShellMesh
{
    private Vector3[] _positions = new Vector3[1024];
    private Vector3[] _normals = new Vector3[1024];
    private Vector2[] _uvs = new Vector2[1024];
    private Vector4[] _colors = new Vector4[1024];
    private int[] _indices = new int[1536];

    public int VertexCount { get; private set; }
    public int IndexCount { get; private set; }

    public ReadOnlySpan<Vector3> Positions => _positions.AsSpan(0, VertexCount);
    public ReadOnlySpan<Vector3> Normals => _normals.AsSpan(0, VertexCount);
    public ReadOnlySpan<Vector2> Uvs => _uvs.AsSpan(0, VertexCount);

    /// <summary>Linear RGBA, one per vertex.</summary>
    public ReadOnlySpan<Vector4> Colors => _colors.AsSpan(0, VertexCount);

    public ReadOnlySpan<int> Indices => _indices.AsSpan(0, IndexCount);

    public void Clear()
    {
        VertexCount = 0;
        IndexCount = 0;
    }

    /// <summary>Appends a planar quad. Corners run clockwise seen from the normal's side, Godot's front face.</summary>
    internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector4 color, Frame frame)
    {
        Reserve(4, 6);
        int at = VertexCount;
        Vertex(a, normal, color, frame);
        Vertex(b, normal, color, frame);
        Vertex(c, normal, color, frame);
        Vertex(d, normal, color, frame);
        int[] indices = _indices;
        int i = IndexCount;
        indices[i] = at;
        indices[i + 1] = at + 1;
        indices[i + 2] = at + 2;
        indices[i + 3] = at;
        indices[i + 4] = at + 2;
        indices[i + 5] = at + 3;
        IndexCount = i + 6;
    }

    /// <summary>Appends a triangle wound like <see cref="Quad"/>.</summary>
    internal void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector4 color, Frame frame)
    {
        Reserve(3, 3);
        int at = VertexCount;
        Vertex(a, normal, color, frame);
        Vertex(b, normal, color, frame);
        Vertex(c, normal, color, frame);
        _indices[IndexCount] = at;
        _indices[IndexCount + 1] = at + 1;
        _indices[IndexCount + 2] = at + 2;
        IndexCount += 3;
    }

    /// <summary>Appends a planar polygon with its own UVs, fanned from its first corner. Corners run like <see cref="Quad"/>.</summary>
    internal void Polygon(ReadOnlySpan<Vector3> corners, ReadOnlySpan<Vector2> uvs, Vector3 normal)
    {
        Reserve(corners.Length, (corners.Length - 2) * 3);
        int at = VertexCount;
        for (int i = 0; i < corners.Length; i++)
        {
            _positions[at + i] = corners[i];
            _normals[at + i] = normal;
            _uvs[at + i] = uvs[i];
            _colors[at + i] = Vector4.One;
        }

        VertexCount += corners.Length;
        for (int i = 1; i + 1 < corners.Length; i++)
        {
            _indices[IndexCount++] = at;
            _indices[IndexCount++] = at + i;
            _indices[IndexCount++] = at + i + 1;
        }
    }

    private void Vertex(Vector3 local, Vector3 normal, Vector4 color, Frame frame)
    {
        int at = VertexCount++;
        _positions[at] = frame.Point(local);
        _normals[at] = frame.Direction(normal);
        _uvs[at] = new Vector2(local.X + local.Z, local.Y);
        _colors[at] = color;
    }

    private void Reserve(int vertices, int indices)
    {
        if (VertexCount + vertices > _positions.Length)
        {
            int size = _positions.Length * 2;
            Array.Resize(ref _positions, size);
            Array.Resize(ref _normals, size);
            Array.Resize(ref _uvs, size);
            Array.Resize(ref _colors, size);
        }

        if (IndexCount + indices > _indices.Length)
        {
            Array.Resize(ref _indices, _indices.Length * 2);
        }
    }
}

/// <summary>A wing's placement: a yaw about +Y, then a translation. Buildings never tilt.</summary>
public readonly record struct Frame(Vector3 Origin, float Cos, float Sin)
{
    public static readonly Frame Identity = new(Vector3.Zero, 1f, 0f);

    public static Frame At(Vector3 origin, float yawRadians) =>
        new(origin, MathF.Cos(yawRadians), MathF.Sin(yawRadians));

    public Vector3 Direction(Vector3 v) => new(v.X * Cos + v.Z * Sin, v.Y, -v.X * Sin + v.Z * Cos);

    public Vector3 Point(Vector3 v) => Origin + Direction(v);
}
