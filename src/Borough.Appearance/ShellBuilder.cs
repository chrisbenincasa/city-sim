using System.Numerics;

namespace Borough.Appearance;

public enum RoofForm
{
    Flat,
    Parapet,
    Gable,
    Hip,
}

/// <summary>
/// One wing of a Building in its own frame: centred on the origin, standing on y = 0, with
/// <see cref="Width"/> along local X and <see cref="Depth"/> along local Z. Metres.
/// </summary>
/// <param name="StreetFace">0 = +Z, 1 = +X, 2 = −Z, 3 = −X, or −1 for no street face.</param>
public readonly record struct WingShape(
    float Width, float Depth, int Storeys, int StreetFace, RoofForm Roof, uint Seed);

/// <summary>
/// Generates a wing's shell: storeys split into bays, a recessed opening per bay, a door on the
/// street face, a cornice and a roof. The storey and bay split depends only on the wing's size, so
/// any lower-detail drawing of the same wing can reproduce it.
/// </summary>
public static class ShellBuilder
{
    public const float StoreyMetres = 3.5f;
    public const float BayMetres = 3.5f;

    private const float Reveal = 0.15f;
    private const float SillHeight = 0.9f;
    private const float WindowHeight = 1.6f;
    private const float DoorWidth = 1.2f;
    private const float DoorHeight = 2.4f;
    private const float CorniceDepth = 0.3f;
    private const float CorniceHeight = 0.4f;
    private const float ParapetHeight = 1f;
    private const float ParapetThickness = 0.25f;
    private const float Eaves = 0.3f;
    private const float PitchRise = 0.7f;

    private static readonly Vector4 Glass = Linear(0.16f, 0.2f, 0.24f);
    private static readonly Vector4 Door = Linear(0.28f, 0.18f, 0.12f);
    private static readonly Vector4 Trim = Linear(0.86f, 0.85f, 0.8f);
    private static readonly Vector4 Roofing = Linear(0.22f, 0.23f, 0.25f);

    private static readonly Vector4[] Walls =
    [
        Linear(0.55f, 0.57f, 0.56f),
        Linear(0.82f, 0.8f, 0.74f),
        Linear(0.46f, 0.53f, 0.45f),
        Linear(0.27f, 0.33f, 0.42f),
        Linear(0.55f, 0.3f, 0.24f),
        Linear(0.66f, 0.6f, 0.5f),
    ];

    public static void Append(ShellMesh mesh, in WingShape wing, Frame frame)
    {
        float height = wing.Storeys * StoreyMetres;
        Vector4 wall = Walls[wing.Seed % (uint)Walls.Length];
        Vector4 reveal = wall * new Vector4(0.8f, 0.8f, 0.8f, 1f);
        float windowShare = 0.35f + (wing.Seed >> 8 & 7) * 0.03f;
        bool corniced = wing.Roof is RoofForm.Flat or RoofForm.Parapet;

        for (int face = 0; face < 4; face++)
        {
            (Vector3 start, Vector3 right, Vector3 outward, float length) = Face(wing, face);
            Facade(mesh, frame, start, right, outward, length, wing.Storeys,
                face == wing.StreetFace, windowShare, wall, reveal);

            if (corniced)
            {
                Cornice(mesh, frame, start, right, outward, length, height);
            }
        }

        switch (wing.Roof)
        {
            case RoofForm.Flat:
                Deck(mesh, frame, wing, height);
                break;
            case RoofForm.Parapet:
                Deck(mesh, frame, wing, height);
                for (int face = 0; face < 4; face++)
                {
                    (Vector3 start, Vector3 right, Vector3 outward, float length) = Face(wing, face);
                    Parapet(mesh, frame, start, right, outward, length, height, wall);
                }

                break;
            case RoofForm.Gable:
                Gable(mesh, frame, wing, height, wall);
                break;
            case RoofForm.Hip:
                Hip(mesh, frame, wing, height);
                break;
        }
    }

    /// <summary>Face corners run in a loop, so each face starts where the previous one ended.</summary>
    private static (Vector3 Start, Vector3 Right, Vector3 Outward, float Length) Face(in WingShape wing, int face)
    {
        float x = wing.Width / 2f;
        float z = wing.Depth / 2f;
        return face switch
        {
            0 => (new Vector3(-x, 0, z), Vector3.UnitX, Vector3.UnitZ, wing.Width),
            1 => (new Vector3(x, 0, z), -Vector3.UnitZ, Vector3.UnitX, wing.Depth),
            2 => (new Vector3(x, 0, -z), -Vector3.UnitX, -Vector3.UnitZ, wing.Width),
            _ => (new Vector3(-x, 0, -z), Vector3.UnitZ, -Vector3.UnitX, wing.Depth),
        };
    }

    private static void Facade(ShellMesh mesh, Frame frame, Vector3 start, Vector3 right, Vector3 outward,
        float length, int storeys, bool street, float windowShare, Vector4 wall, Vector4 reveal)
    {
        int bays = Math.Max(1, (int)MathF.Round(length / BayMetres));
        float bay = length / bays;
        float window = Math.Clamp(bay * windowShare, 0.6f, 1.6f);
        int doorBay = bays / 2;
        Vector3 inward = -outward * Reveal;

        for (int storey = 0; storey < storeys; storey++)
        {
            float floor = storey * StoreyMetres;
            float ceiling = floor + StoreyMetres;

            for (int b = 0; b < bays; b++)
            {
                float x0 = b * bay;
                float x1 = x0 + bay;
                float centre = x0 + bay / 2f;
                bool door = street && storey == 0 && b == doorBay;
                float half = door ? Math.Min(DoorWidth, bay * 0.8f) / 2f : window / 2f;
                float left = centre - half;
                float rightEdge = centre + half;
                float bottom = door ? floor : floor + SillHeight;
                float top = door ? floor + DoorHeight : bottom + WindowHeight;

                Vector3 At(float x, float y) => start + right * x + Vector3.UnitY * y;

                if (bottom > floor)
                {
                    Rect(mesh, frame, At(x0, floor), right * bay, Vector3.UnitY * (bottom - floor), outward, wall);
                }

                Rect(mesh, frame, At(x0, top), right * bay, Vector3.UnitY * (ceiling - top), outward, wall);
                Rect(mesh, frame, At(x0, bottom), right * (left - x0), Vector3.UnitY * (top - bottom), outward, wall);
                Rect(mesh, frame, At(rightEdge, bottom), right * (x1 - rightEdge), Vector3.UnitY * (top - bottom), outward, wall);

                Vector3 across = right * (rightEdge - left);
                Vector3 up = Vector3.UnitY * (top - bottom);
                Rect(mesh, frame, At(left, bottom), inward, up, right, reveal);
                Rect(mesh, frame, At(rightEdge, bottom), inward, up, -right, reveal);
                Rect(mesh, frame, At(left, top), inward, across, -Vector3.UnitY, reveal);
                if (!door)
                {
                    Rect(mesh, frame, At(left, bottom), inward, across, Vector3.UnitY, Trim);
                }

                Rect(mesh, frame, At(left, bottom) + inward, across, up, outward, door ? Door : Glass);
            }
        }
    }

    private static void Cornice(ShellMesh mesh, Frame frame, Vector3 start, Vector3 right, Vector3 outward,
        float length, float height)
    {
        Vector3 along = right * (length + 2 * CorniceDepth);
        Vector3 origin = start - right * CorniceDepth + Vector3.UnitY * (height - CorniceHeight);
        Vector3 out_ = outward * CorniceDepth;
        Rect(mesh, frame, origin + out_, along, Vector3.UnitY * CorniceHeight, outward, Trim);
        Rect(mesh, frame, origin, along, out_, -Vector3.UnitY, Trim);
        Rect(mesh, frame, origin + Vector3.UnitY * CorniceHeight, along, out_, Vector3.UnitY, Trim);
    }

    private static void Deck(ShellMesh mesh, Frame frame, in WingShape wing, float height)
    {
        var corner = new Vector3(-wing.Width / 2f, height, -wing.Depth / 2f);
        Rect(mesh, frame, corner, Vector3.UnitX * wing.Width, Vector3.UnitZ * wing.Depth, Vector3.UnitY, Roofing);
    }

    private static void Parapet(ShellMesh mesh, Frame frame, Vector3 start, Vector3 right, Vector3 outward,
        float length, float height, Vector4 wall)
    {
        Vector3 origin = start + Vector3.UnitY * height;
        Vector3 along = right * length;
        Vector3 up = Vector3.UnitY * ParapetHeight;
        Vector3 thick = -outward * ParapetThickness;
        Rect(mesh, frame, origin, along, up, outward, wall);
        Rect(mesh, frame, origin + thick, along, up, -outward, wall);
        Rect(mesh, frame, origin + up, along, thick, Vector3.UnitY, Trim);
    }

    /// <summary>The ridge runs along the longer side, so the gable ends face the shorter sides.</summary>
    private static void Gable(ShellMesh mesh, Frame frame, in WingShape wing, float height, Vector4 wall)
    {
        (Vector3 along, Vector3 across, float length, float span) = Axes(wing);
        float rise = span / 2f * PitchRise;
        float drop = Eaves * PitchRise;
        Vector3 ridge = Vector3.UnitY * (height + rise);
        Vector3 ends = along * (length / 2f + Eaves);

        foreach (float side in (ReadOnlySpan<float>)[1f, -1f])
        {
            Vector3 eave = across * side * (span / 2f + Eaves) + Vector3.UnitY * (height - drop);
            Rect(mesh, frame, eave - ends, ends * 2f, ridge - eave, across * side + Vector3.UnitY, Roofing);

            Vector3 end = along * side * (length / 2f) + Vector3.UnitY * height;
            Vector3 half = across * (span / 2f);
            Triangle(mesh, frame, end + half, end - half, end + Vector3.UnitY * rise, along * side, wall);
        }
    }

    private static void Hip(ShellMesh mesh, Frame frame, in WingShape wing, float height)
    {
        (Vector3 along, Vector3 across, float length, float span) = Axes(wing);
        float rise = span / 2f * PitchRise;
        Vector3 ridgeHalf = along * Math.Max(0f, (length - span) / 2f);
        Vector3 ridge = Vector3.UnitY * (height + rise);
        Vector3 eaveHalf = along * (length / 2f);
        Vector3 up = Vector3.UnitY * height;

        foreach (float side in (ReadOnlySpan<float>)[1f, -1f])
        {
            Vector3 edge = across * side * (span / 2f) + up;
            Quad(mesh, frame, edge - eaveHalf, edge + eaveHalf, ridge + ridgeHalf, ridge - ridgeHalf, across * side + Vector3.UnitY, Roofing);

            Vector3 end = along * side * (length / 2f) + up;
            Vector3 half = across * (span / 2f);
            Triangle(mesh, frame, end + half, end - half, ridge + ridgeHalf * side, along * side + Vector3.UnitY, Roofing);
        }
    }

    private static (Vector3 Along, Vector3 Across, float Length, float Span) Axes(in WingShape wing) =>
        wing.Width >= wing.Depth
            ? (Vector3.UnitX, Vector3.UnitZ, wing.Width, wing.Depth)
            : (Vector3.UnitZ, Vector3.UnitX, wing.Depth, wing.Width);

    /// <summary>A parallelogram at <paramref name="origin"/> spanned by <paramref name="a"/> and <paramref name="b"/>, facing <paramref name="hint"/>.</summary>
    private static void Rect(ShellMesh mesh, Frame frame, Vector3 origin, Vector3 a, Vector3 b, Vector3 hint, Vector4 color) =>
        Quad(mesh, frame, origin, origin + a, origin + a + b, origin + b, hint, color);

    /// <summary>A planar quad given in loop order either way round; it is wound to face <paramref name="hint"/>.</summary>
    private static void Quad(ShellMesh mesh, Frame frame, Vector3 p, Vector3 q, Vector3 r, Vector3 s, Vector3 hint, Vector4 color)
    {
        Vector3 cross = Vector3.Cross(q - p, s - p);
        if (cross.LengthSquared() < 1e-10f)
        {
            return;
        }

        Vector3 normal = Vector3.Normalize(cross);
        if (Vector3.Dot(normal, hint) > 0f)
        {
            mesh.Quad(p, s, r, q, normal, color, frame);
        }
        else
        {
            mesh.Quad(p, q, r, s, -normal, color, frame);
        }
    }

    private static void Triangle(ShellMesh mesh, Frame frame, Vector3 p, Vector3 q, Vector3 r, Vector3 hint, Vector4 color)
    {
        Vector3 cross = Vector3.Cross(q - p, r - p);
        if (cross.LengthSquared() < 1e-10f)
        {
            return;
        }

        Vector3 normal = Vector3.Normalize(cross);
        if (Vector3.Dot(normal, hint) > 0f)
        {
            mesh.Triangle(p, r, q, normal, color, frame);
        }
        else
        {
            mesh.Triangle(p, q, r, -normal, color, frame);
        }
    }

    private static Vector4 Linear(float r, float g, float b) => new(r * r, g * g, b * b, 1f);
}
