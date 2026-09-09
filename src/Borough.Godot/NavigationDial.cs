using Godot;

namespace Borough.Shell;

internal sealed partial class NavigationDial : Container
{
    internal float Yaw;
    private Color _ink, _line;
    private static readonly float[] Angles = [-150, -30, -90, 90, 30, 150];

    public override Vector2 _GetMinimumSize() => new(136, 136);

    internal void Colours(InformationUi.Palette palette)
    {
        _ink = palette.Active;
        _line = palette.Line;
        foreach (var child in GetChildren())
        {
            if (child is not Button button) continue;
            foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "focus", "disabled" })
            {
                Color fill = state is "pressed" or "hover_pressed" ? palette.Active : palette.Surface;
                button.AddThemeStyleboxOverride(state, InformationUi.Box(
                    state == "focus" ? Colors.Transparent : fill,
                    state == "focus" ? palette.Active : palette.Line, 2, 2, 24));
            }
        }
        QueueRedraw();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        int i = 0;
        foreach (var child in GetChildren())
        {
            if (child is not Control control) continue;
            var centre = Size * .5f + Vector2.FromAngle(Mathf.DegToRad(Angles[i++])) * 48;
            FitChildInRect(control, new Rect2((centre - new Vector2(20, 20)).Round(), new Vector2(40, 40)));
        }
    }

    public override void _Draw()
    {
        Vector2 centre = Size * .5f;
        DrawArc(centre, 48, 0, Mathf.Tau, 64, _line, 1, true);
        DrawCircle(centre, 15, new Color(_line, .3f));
        Vector2 north = new(Mathf.Sin(Yaw), -Mathf.Cos(Yaw));
        Vector2 side = new(-north.Y, north.X);
        DrawColoredPolygon([centre + north * 12, centre - north * 8 + side * 5,
            centre - north * 5, centre - north * 8 - side * 5], _ink);
    }
}
