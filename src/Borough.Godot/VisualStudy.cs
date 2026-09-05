using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace Borough.Shell;

public partial class VisualStudy : Node3D
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Camera3D _camera = new();
    private readonly Label _caption = new();
    private int _frame;
    private int _view;
    private string? _capture;
    private bool _validate;
    private bool _sceneReady;
    private string _treatment = "blockout";
    private bool MaterialRich => _treatment == "material-rich";
    private float _expectedWidth = 4;
    private Color _expectedWall = new("b9afa0");
    private static readonly string[] Views = { "block", "street", "facade", "neighbourhood" };
    private static readonly Vector3[] Eyes = { new(75, 70, 105), new(42, 9, 63), new(-18, 5, 29), new(230, 240, 300) };
    private static readonly Vector3[] Targets = { new(0, 0, 0), new(0, 5, 7), new(-22, 5, 17), new(0, 0, 0) };
    private readonly Node3D _repeats = new();
    private PackedScene _facade = null!;

    public override void _Ready()
    {
        try
        {
            AddChild(_camera);
            AddChild(_repeats);
            AddChild(_caption);
            string[] args = OS.GetCmdlineUserArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--capture-study") _capture = args[++i];
                else if (args[i] == "--study-treatment") _treatment = args[++i];
                else if (args[i] == "--validate-study") _validate = true;
                else if (args[i] == "--expected-width") _expectedWidth = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                else if (args[i] == "--expected-wall") _expectedWall = new Color(args[++i]);
            }
            if (_treatment != "blockout" && !MaterialRich) throw new ArgumentException("Unknown study treatment");
            string asset = MaterialRich ? "facade-material-rich" : "facade";
            _facade = GD.Load<PackedScene>($"res://assets/visual-study/{asset}.glb");
            ValidateFacade();
            if (_validate) { GetTree().Quit(); return; }
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1440, 960);
            GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
            var environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("c9d8df"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("d6e2ed"), AmbientLightEnergy = .55f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic
            };
            AddChild(new WorldEnvironment { Environment = environment });
            AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-62, -28, 0),
                LightEnergy = 1.15f, ShadowEnabled = true, DirectionalShadowMaxDistance = 600 });
            _camera.Fov = 48;
            _camera.Far = 2000;
            BuildBlock(this, Vector3.Zero);
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                    if (x != 0 || z != 0) BuildBlock(_repeats, new Vector3(x * 104, 0, z * 90));
            var layer = new CanvasLayer();
            AddChild(layer);
            var panel = new ColorRect { Color = new Color(0.06f, .08f, .1f, .9f), Size = new Vector2(1440, 88), MouseFilter = Control.MouseFilterEnum.Ignore };
            layer.AddChild(panel);
            _caption.Reparent(panel);
            _caption.Position = new Vector2(24, 14);
            _caption.AddThemeFontSizeOverride("font_size", 20);
            SetView(0);
            if (_capture != null) Directory.CreateDirectory(_capture);
            _sceneReady = true;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }

    private void ValidateFacade()
    {
        var root = _facade.Instantiate<Node3D>();
        Aabb? bounds = null;
        bool wallFound = false, trimFound = false, glassFound = false;
        void Walk(Node node, Transform3D transform)
        {
            if (node is Node3D spatial) transform *= spatial.Transform;
            if (node is MeshInstance3D mesh)
            {
                Aabb box = transform * mesh.GetAabb();
                bounds = bounds.HasValue ? bounds.Value.Merge(box) : box;
                for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
                {
                    if (mesh.GetActiveMaterial(i) is not StandardMaterial3D mat)
                        throw new InvalidDataException("Missing portable material");
                    if (mat.ResourceName == "wall")
                    {
                        wallFound = true;
                        if (MaterialRich)
                        {
                            if (mat.NormalTexture == null || mat.RoughnessTexture == null)
                                throw new InvalidDataException("Masonry normal/roughness textures did not import");
                            mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
                            _masonry ??= (StandardMaterial3D)mat.Duplicate();
                            _masonry.Uv1Triplanar = true;
                            _masonry.Uv1WorldTriplanar = true;
                            _masonry.Uv1Scale = new Vector3(.5f, .5f, .5f);
                        }
                        if (Math.Abs(mat.AlbedoColor.R - _expectedWall.R) > .005f ||
                            Math.Abs(mat.AlbedoColor.G - _expectedWall.G) > .005f ||
                            Math.Abs(mat.AlbedoColor.B - _expectedWall.B) > .005f)
                            throw new InvalidDataException($"Wall colour changed: {mat.AlbedoColor}");
                    }
                    trimFound |= mat.ResourceName == "trim";
                    glassFound |= mat.ResourceName == "glazing";
                    var arrays = mesh.Mesh.SurfaceGetArrays(i);
                    foreach (Vector3 normal in arrays[(int)Mesh.ArrayType.Normal].AsVector3Array())
                        if (!normal.IsFinite() || Math.Abs(normal.Length() - 1) > .001f)
                            throw new InvalidDataException("Invalid imported normal");
                }
            }
            foreach (Node child in node.GetChildren()) Walk(child, transform);
        }
        Walk(root, Transform3D.Identity);
        if (bounds is not Aabb b || Math.Abs(b.Size.X - _expectedWidth) > .001f ||
            Math.Abs(b.Size.Y - 3.5f) > .001f || Math.Abs(b.Position.Y) > .001f ||
            b.End.Z < .33f || !wallFound || !trimFound || !glassFound)
            throw new InvalidDataException($"Facade contract failed: {bounds}");
        root.Free();
        GD.Print($"VISUAL_STUDY_IMPORT_OK width={_expectedWidth} height=3.5 ground=0 front=+Z materials=wall,trim,glazing Godot={Engine.GetVersionInfo()["string"]}");
    }

    private StandardMaterial3D Material(string hex)
    {
        if (MaterialRich && hex == "b9afa0") return _masonry!;
        if (_materials.TryGetValue(hex, out StandardMaterial3D? cached)) return cached;
        var material = new StandardMaterial3D { AlbedoColor = new Color(hex), Roughness = .85f };
        if (MaterialRich && (hex == "777a7a" || hex == "7d8989"))
        { material.Metallic = .45f; material.Roughness = .42f; }
        if (MaterialRich && hex == "52686d") material.Roughness = .25f;
        _materials.Add(hex, material);
        return material;
    }

    private void Box(Node parent, string name, Vector3 position, Vector3 size, string color)
    {
        var mesh = new MeshInstance3D { Name = name, Position = position,
            Mesh = new BoxMesh { Size = size }, MaterialOverride = Material(color) };
        if (name == "roof_seam") mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        parent.AddChild(mesh);
    }

    private void Building(Node parent, string name, Vector3 position, int bays, int storeys, bool shop = false)
    {
        var building = new Node3D { Name = name, Position = position };
        parent.AddChild(building);
        float width = bays * 4;
        Box(building, "envelope", new Vector3(0, storeys * 1.75f, -5), new Vector3(width, storeys * 3.5f, 10), "b9afa0");
        for (int floor = 0; floor < storeys; floor++)
            for (int bay = 0; bay < bays; bay++)
            {
                if (floor == 0 && shop) continue;
                if (floor == 0 && bay == 0)
                {
                    float x = (bay - (bays - 1) * .5f) * 4;
                    Box(building, "door_left", new Vector3(x - 1.3f, 1.75f, .16f), new Vector3(1.4f, 3.5f, .3f), "b9afa0");
                    Box(building, "door_right", new Vector3(x + 1.3f, 1.75f, .16f), new Vector3(1.4f, 3.5f, .3f), "b9afa0");
                    Box(building, "door_head", new Vector3(x, 2.95f, .16f), new Vector3(1.2f, 1.1f, .3f), "b9afa0");
                    continue;
                }
                var module = _facade.Instantiate<Node3D>();
                building.AddChild(module);
                module.Position = new Vector3((bay - (bays - 1) * .5f) * 4, floor * 3.5f, .16f);
            }
        Box(building, "roof", new Vector3(0, storeys * 3.5f + .2f, -5), new Vector3(width + .15f, .4f, 10.2f), "777a7a");
        Box(building, "entrance", new Vector3(-width / 2 + 2, 1.2f, .16f), new Vector3(1.2f, 2.4f, .12f), "35454b");
        if (MaterialRich) FinishBuilding(building, bays, storeys, shop);
        if (shop)
        {
            Box(building, "shop_glazing", new Vector3(1, 1.35f, .55f), new Vector3(width - 3, 2.6f, .12f), "52686d");
            Box(building, "shop_canopy", new Vector3(0, 3.0f, 1), new Vector3(width, .18f, 2), "7d8989");
        }
    }

    private void BuildBlock(Node parent, Vector3 offset)
    {
        var block = new Node3D { Position = offset };
        parent.AddChild(block);
        Box(block, "ground", new Vector3(0, -.3f, 0), new Vector3(104, .4f, 90), "9ba491");
        Box(block, "east_west_street", new Vector3(0, -.05f, 29), new Vector3(104, .1f, 10), "777d7e");
        Box(block, "north_south_street", new Vector3(45, -.05f, 0), new Vector3(10, .1f, 90), "777d7e");
        Box(block, "front_pavement", new Vector3(-4, .06f, 21), new Vector3(88, .12f, 6), "c7c3b9");
        Box(block, "side_pavement", new Vector3(37, .06f, -3), new Vector3(6, .12f, 42), "c7c3b9");
        Building(block, "home_1", new Vector3(-34, .12f, 18), 2, 3);
        Building(block, "home_2", new Vector3(-22, .12f, 18), 3, 3);
        Building(block, "home_3", new Vector3(-10, .12f, 18), 2, 2);
        Building(block, "shop_1", new Vector3(2, .12f, 18), 3, 3, true);
        Building(block, "corner_shop", new Vector3(28, .12f, 18), 3, 3, true);
        Box(block, "corner_return_glazing", new Vector3(34.06f, 1.47f, 13), new Vector3(.12f, 2.6f, 8), "52686d");
        Building(block, "home_4", new Vector3(-34, .12f, -12), 2, 2);
        Building(block, "home_5", new Vector3(-22, .12f, -12), 3, 3);
        Building(block, "home_6", new Vector3(-10, .12f, -12), 2, 3);
        Building(block, "school", new Vector3(18, .12f, -14), 6, 2);
        Box(block, "school_entry", new Vector3(18, 1.6f, -13.3f), new Vector3(3.2f, 3, .5f), "52686d");
        Box(block, "school_porch", new Vector3(18, 3.4f, -12), new Vector3(7, .25f, 4), "878c8a");
        Box(block, "school_court", new Vector3(18, .02f, -5), new Vector3(24, .04f, 12), "b5b6a7");
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = new(-35 + i * 17, 0, 0);
            Box(block, $"trunk_{i}", p + new Vector3(0, 2, 0), new Vector3(.4f, 4, .4f), "777466");
            Mesh crown = i % 2 == 0 ? new SphereMesh { Radius = 2.7f, Height = 5.4f } : new CylinderMesh { TopRadius = .25f, BottomRadius = 2.2f, Height = 6, RadialSegments = 10 };
            block.AddChild(new MeshInstance3D { Position = p + new Vector3(0, 5, 0), Mesh = crown, MaterialOverride = Material("748670") });
        }
        Box(block, "car_body", new Vector3(9, .6f, 29), new Vector3(4.4f, .8f, 1.8f), "8e999f");
        Box(block, "car_cabin", new Vector3(9, 1.2f, 29), new Vector3(2.3f, .6f, 1.65f), "52686d");
        Box(block, "walker_body", new Vector3(-18, .9f, 22), new Vector3(.45f, 1.5f, .3f), "52616a");
        block.AddChild(new MeshInstance3D { Position = new Vector3(-18, 1.8f, 22), Mesh = new SphereMesh { Radius = .15f, Height = .3f }, MaterialOverride = Material("a69786") });
    }

    private void SetView(int view)
    {
        _view = view;
        _repeats.Visible = view == 3;
        _camera.Position = Eyes[view];
        _camera.LookAt(Targets[view]);
        _caption.Text = $"0063 / {(MaterialRich ? "MATERIAL-RICH / FIRST FACADE TEST" : "PLAIN BLOCKOUT v1")} / {Views[view].ToUpperInvariant()} / NOON\nArt fixture · no simulated population or occupancy · 1–4 views · Esc closes";
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.Escape) GetTree().Quit();
        if (key.Keycode >= Key.Key1 && key.Keycode <= Key.Key4) SetView((int)(key.Keycode - Key.Key1));
    }

    public override void _Process(double delta)
    {
        if (!_sceneReady || _capture == null || _validate) return;
        _frame++;
        if (_frame % 30 != 0) return;
        string path = Path.Combine(_capture, Views[_view] + ".png");
        Error error = GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok) { GD.PushError($"Capture failed: {error}"); GetTree().Quit(1); return; }
        File.WriteAllText(Path.Combine(_capture, Views[_view] + ".json"), JsonSerializer.Serialize(new
        {
            specimen = "plain-blockout-v1", treatment = _treatment, view = Views[_view], camera = _camera.Position.ToString(),
            target = Targets[_view].ToString(), fov = _camera.Fov, viewport = GetViewport().GetVisibleRect().Size.ToString(),
            light = "noon", exposure = 1, tick = (int?)null, simulatedCitizens = 0,
            buildings = _view == 3 ? 81 : 9, renderer = RenderingServer.GetCurrentRenderingMethod().ToString(),
            godot = Engine.GetVersionInfo()["string"].AsString()
        }, JsonOptions));
        if (_view == 3) GetTree().Quit(); else SetView(_view + 1);
    }
}
