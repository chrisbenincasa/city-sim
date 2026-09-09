using System;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

internal sealed partial class CityMiniMap : Control
{
    private RoadGraph? _graph;
    private uint _version;
    private ImageTexture? _texture;
    private Vector2 _focus;
    private Camera3D? _camera;
    private float _metresPerTile;
    internal Rect2 Extent { get; private set; }
    internal int RoadCount { get; private set; }
    internal int Rebuilds { get; private set; }
    internal bool Dragging { get; private set; }
    internal Action<int, int>? Navigate;
    private const int Width = 192, Height = 96;

    public override Vector2 _GetMinimumSize() => new(160, 80);

    internal void Refresh(RoadGraph graph, Vector3 focus, Camera3D camera, float metresPerTile)
    {
        if (_graph != graph || _version != graph.Version)
        {
            _graph = graph;
            _version = graph.Version;
            Rebuild();
        }
        _metresPerTile = metresPerTile;
        _focus = new Vector2(focus.X / metresPerTile, -focus.Z / metresPerTile);
        _camera = camera;
        QueueRedraw();
    }

    private void Rebuild()
    {
        var nodes = _graph!.Nodes;
        var roads = _graph.Segments;
        Vector2 low = new(float.MaxValue, float.MaxValue), high = new(float.MinValue, float.MinValue);
        RoadCount = 0;
        for (int i = 0; i < roads.Rows.SlotCount; i++)
        {
            if (!roads.Rows.IsLive(i) || !nodes.Rows.TryResolve(roads.NodeA[i], out int a)
                || !nodes.Rows.TryResolve(roads.NodeB[i], out int b)) continue;
            Vector2 start = new(nodes.East[a].Raw, nodes.North[a].Raw);
            Vector2 end = new(nodes.East[b].Raw, nodes.North[b].Raw);
            low = low.Min(start).Min(end); high = high.Max(start).Max(end);
            RoadCount++;
        }
        if (RoadCount == 0) { low = Vector2.Zero; high = new Vector2(128, 64); }
        Vector2 centre = (low + high) * .5f;
        float span = Math.Max(64, Math.Max(high.X - low.X, (high.Y - low.Y) * 2)) * 1.2f;
        Extent = new Rect2(centre - new Vector2(span, span * .5f) * .5f, new Vector2(span, span * .5f));
        using var raster = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
        raster.Fill(new Color("30483f"));
        for (int i = 0; i < roads.Rows.SlotCount; i++)
        {
            if (!roads.Rows.IsLive(i) || !nodes.Rows.TryResolve(roads.NodeA[i], out int a)
                || !nodes.Rows.TryResolve(roads.NodeB[i], out int b)) continue;
            Vector2 from = Unit(new Vector2(nodes.East[a].Raw, nodes.North[a].Raw)) * new Vector2(Width - 1, Height - 1);
            Vector2 to = Unit(new Vector2(nodes.East[b].Raw, nodes.North[b].Raw)) * new Vector2(Width - 1, Height - 1);
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y))));
            for (int step = 0; step <= steps; step++)
            {
                Vector2 p = from.Lerp(to, step / (float)steps);
                raster.SetPixel(Math.Clamp((int)p.X, 0, Width - 1), Math.Clamp((int)p.Y, 0, Height - 1), new Color("e4dca9"));
            }
        }
        if (_texture is null) _texture = ImageTexture.CreateFromImage(raster);
        else _texture.Update(raster);
        Rebuilds++;
        TooltipText = RoadCount == 0 ? "No roads yet. North is up; click or drag to move the camera."
            : "Road minimap · north is up. Click or drag to move the camera. Cross: camera focus. Outline: ground in view when all corners meet the ground.";
    }

    private Vector2 Unit(Vector2 tile) => new((tile.X - Extent.Position.X) / Extent.Size.X,
        1 - (tile.Y - Extent.Position.Y) / Extent.Size.Y);

    public override void _Draw()
    {
        if (_texture is null) return;
        DrawTextureRect(_texture, new Rect2(Vector2.Zero, Size), false);
        if (_camera is not null)
        {
            var plane = new Plane(Vector3.Up, 0);
            Vector2 viewport = _camera.GetViewport().GetVisibleRect().Size;
            Vector2[] corners = [Vector2.Zero, new(viewport.X, 0), viewport, new(0, viewport.Y)];
            var outline = new Vector2[5];
            bool complete = true;
            for (int i = 0; i < 4; i++)
            {
                Vector3? ground = plane.IntersectsRay(_camera.ProjectRayOrigin(corners[i]), _camera.ProjectRayNormal(corners[i]));
                if (ground is not { } at) { complete = false; break; }
                outline[i] = Unit(new Vector2(at.X / _metresPerTile, -at.Z / _metresPerTile)) * Size;
            }
            if (complete) { outline[4] = outline[0]; DrawPolyline(outline, new Color("8fc9ff"), 1.5f, true); }
        }
        Vector2 mark = Unit(_focus) * Size;
        DrawCircle(mark, 3, Colors.White);
        DrawLine(mark - new Vector2(6, 0), mark + new Vector2(6, 0), Colors.White, 1);
        DrawLine(mark - new Vector2(0, 6), mark + new Vector2(0, 6), Colors.White, 1);
        DrawString(ThemeDB.FallbackFont, new Vector2(5, 15), "N ↑", fontSize: 13, modulate: Colors.White);
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("80998c"), false);
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Godot captures the mouse for this Control until release, including outside its bounds.
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } click)
        {
            Dragging = click.Pressed;
            if (Dragging) NavigateAt(click.Position);
        }
        else if (Dragging && @event is InputEventMouseMotion motion)
            NavigateAt(motion.Position);
        else return;
        AcceptEvent();
    }

    public override void _Input(InputEvent @event)
    {
        if (!Dragging || @event is not InputEventKey { Keycode: Key.Escape, Pressed: true }) return;
        Dragging = false;
        GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMWindowFocusOut
            || (what == NotificationVisibilityChanged && !IsVisibleInTree())) Dragging = false;
    }

    private void NavigateAt(Vector2 position)
    {
        Vector2 unit = (position / Size).Clamp(Vector2.Zero, Vector2.One);
        Vector2 tile = Extent.Position + new Vector2(unit.X, 1 - unit.Y) * Extent.Size;
        Navigate?.Invoke(Math.Clamp((int)Math.Round(tile.X), 0, CellGrid.WorldTiles - 1),
            Math.Clamp((int)Math.Round(tile.Y), 0, CellGrid.WorldTiles - 1));
    }
}
