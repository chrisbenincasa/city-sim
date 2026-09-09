using Godot;

namespace Borough.Shell;

public partial class MapRuler : Control
{
    public Camera3D Camera { get; set; } = null!;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (Camera is null) return;
        Vector2 centre = GetViewportRect().Size * .5f;
        var plane = new Plane(Vector3.Up, 0);
        Vector3? a = plane.IntersectsRay(Camera.ProjectRayOrigin(centre), Camera.ProjectRayNormal(centre));
        Vector2 beside = centre + new Vector2(120, 0);
        Vector3? b = plane.IntersectsRay(Camera.ProjectRayOrigin(beside), Camera.ProjectRayNormal(beside));
        if (a is null || b is null) return;
        float span = a.Value.DistanceTo(b.Value);
        if (!float.IsFinite(span) || span <= 0) return;
        float power = Mathf.Pow(10, Mathf.Floor(Mathf.Log(span) / Mathf.Log(10)));
        float units = span / power;
        float metres = power * (units >= 5 ? 5 : units >= 2 ? 2 : 1);
        float pixels = 120 * metres / span;
        DrawRect(new Rect2(0, 0, 175, 62), new Color("182838dd"));
        string label = metres >= 1000 ? $"{metres / 1000:0.#} km" : $"{metres:0.#} m";
        DrawString(ThemeDB.FallbackFont, new Vector2(12, 20), label, fontSize: 16);
        DrawLine(new Vector2(12, 31), new Vector2(12 + pixels, 31), Colors.White, 2);
        DrawLine(new Vector2(12, 27), new Vector2(12, 35), Colors.White, 2);
        DrawLine(new Vector2(12 + pixels, 27), new Vector2(12 + pixels, 35), Colors.White, 2);
        DrawString(ThemeDB.FallbackFont, new Vector2(12, 52), "at view centre", fontSize: 12);
    }
}
