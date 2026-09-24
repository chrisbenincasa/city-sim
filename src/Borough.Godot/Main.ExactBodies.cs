using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

/// <summary>
/// A study of pass 03's exact game bodies in the live city (research/city-architecture pass 03,
/// review step 5). Each authored body replaces the massing of the lowest-id Building whose plan it
/// matches exactly. The body faces that Building's street.
/// </summary>
/// <remarks>
/// The match reads the plan and the storey count, never the kind (adr/0150). A workplace body can
/// therefore stand on a Building that houses people, and the review records where it does.
/// </remarks>
public partial class Main
{
    private static readonly (string Asset, float Along, float Deep, int Storeys, bool Labelled)[] ExactBodies =
    [
        ("w1-workplace", 36f, 20f, 2, false),
        ("g003-two-tenancy", 24f, 16f, 3, true),
    ];

    private readonly Dictionary<ulong, Node3D> _exactBodies = [];

    /// <summary><c>ui exact-bodies on|off</c>.</summary>
    private void ExactBodyStudy(string[] words)
    {
        if (words.Length != 2 || words[1] is not ("on" or "off"))
        {
            _refused = "exact-bodies on|off";
            return;
        }

        foreach (Node3D node in _exactBodies.Values)
        {
            node.QueueFree();
        }

        _exactBodies.Clear();
        _world.Changes!.Invalidate();
        if (words[1] == "off")
        {
            return;
        }

        foreach ((string asset, float along, float deep, int storeys, bool labelled) in ExactBodies)
        {
            int chosen = -1;
            Massing match = default;
            for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
            {
                if (!_world.Buildings.Rows.IsLive(slot)) continue;
                using IEnumerator<Massing> parts = Buildings(slot).GetEnumerator();
                if (!parts.MoveNext()) continue;
                Massing one = parts.Current;
                if (parts.MoveNext() || !Matches(one, along, deep, storeys)) continue;
                if (chosen < 0 || one.Id < match.Id)
                {
                    chosen = slot;
                    match = one;
                }
            }

            if (chosen < 0)
            {
                GD.Print($"exact_body\t{asset}\tno Building has a {along}x{deep} m plan at {storeys} storeys");
                continue;
            }

            float faceEast = (match.Reads.R * 2f) - 1f;
            float faceSouth = (match.Reads.G * 2f) - 1f;
            Vector3 ground = match.Body.Origin with { Y = 0f };
            var body = GD.Load<PackedScene>($"res://assets/test-street/{asset}.glb").Instantiate<Node3D>();
            body.Position = ground;
            body.Rotation = new Vector3(0f, Mathf.Atan2(faceEast, faceSouth), 0f);
            AddChild(body);
            _exactBodies[match.Id] = body;

            int ceiling = _world.DeclaredOccupancy(chosen);
            if (labelled)
            {
                body.AddChild(new Label3D
                {
                    Text = $"G003 study\nceiling {ceiling} tenancies",
                    Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    NoDepthTest = true,
                    FixedSize = true,
                    FontSize = 22,
                    PixelSize = .0005f,
                    Modulate = new Color("ecf8ff"),
                    Position = new Vector3(0f, (storeys * StoreyMetres) + 6f, 0f),
                });
            }

            GD.Print($"exact_body\t{asset}\tbuilding {match.Id}\tkind {_world.Buildings.Kind[chosen]}"
                + $"\ttile {Mathf.RoundToInt(ground.X / MetresPerTile)} {Mathf.RoundToInt(-ground.Z / MetresPerTile)}"
                + $"\tceiling {ceiling}");
        }
    }

    private static bool Matches(Massing one, float along, float deep, int storeys)
    {
        Vector3 size = one.Body.Basis.Scale;
        bool facesNorthSouth = Mathf.Abs((one.Reads.G * 2f) - 1f) > 0.5f;
        float frontage = facesNorthSouth ? size.X : size.Z;
        float depth = facesNorthSouth ? size.Z : size.X;
        return Mathf.IsEqualApprox(frontage, along) && Mathf.IsEqualApprox(depth, deep)
            && Mathf.RoundToInt(size.Y / StoreyMetres) == storeys;
    }
}
