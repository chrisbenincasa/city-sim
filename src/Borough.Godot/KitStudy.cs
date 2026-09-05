using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace Borough.Shell;

public partial class KitStudy : Node3D
{
    private static readonly string[] Specimens = { "detached-house", "residential-tower", "car" };
    private static readonly string[] Treatments = { "material-rich", "sculpted", "middle" };
    private static readonly Vector3[] Eyes = { new(17, 12, 22), new(66, 49, 85), new(6.4f, 3.4f, 7.7f) };
    private static readonly Vector3[] Targets = { new(0, 3.3f, 0), new(0, 21, 0), new(0, .7f, 0) };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Camera3D _camera = new();
    private readonly Label _caption = new();
    private Node3D? _asset;
    private MeshInstance3D _ground = null!;
    private int _specimen;
    private int _treatment;
    private int _frame;
    private bool _ready;
    private bool _extremes;
    private bool _models;
    private static readonly string[] ModelStyles = { "realistic", "city-builder", "voxel", "low-poly" };
    private bool _detail;
    private string Treatment => _palettes ? (_paletteTower ? "city-builder" : "realistic") : _models ? ModelStyles[_treatment] : _styles ? ArtStyles[_treatment] : !_extremes ? Treatments[_treatment] : _treatment switch
    {
        0 => _specimen == 2 ? "middle" : "material-rich",
        1 => "detailed-extreme",
        _ => "sculpted-extreme"
    };
    private string? _capture;
    private int _triangles;
    private int _surfaces;
    private Aabb _bounds;
    private Vector3 _target;

    public override void _Ready()
    {
        try
        {
            AddChild(_camera);
            string[] args = OS.GetCmdlineUserArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--capture-kit") _capture = args[++i];
                else if (args[i] == "--kit-extremes") _extremes = true;
                else if (args[i] == "--kit-styles") _styles = true;
                else if (args[i] == "--kit-models") _models = true;
                else if (args[i] == "--kit-palettes") { _palettes = true; _models = true; }
                else if (args[i] == "--palette-tower-city-builder") _paletteTower = true;
            if ((_styles ? 1 : 0) + (_extremes ? 1 : 0) + (_models ? 1 : 0) > 1) throw new ArgumentException("Choose styles or extremes");
            if (_paletteTower && !_palettes) throw new ArgumentException("Tower palette requires --kit-palettes");
            if (_paletteTower) _specimen = 1;
            if (_palettes) LoadPalettes();
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1440, 960);
            GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
            if (_styles || _models)
            {
                GetViewport().UseTaa = true;
                RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftUltra);
            }
            _environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("c9d8df"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("d6e2ed"), AmbientLightEnergy = .55f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic
            };
            AddChild(new WorldEnvironment { Environment = _environment });
            _sun = new DirectionalLight3D { RotationDegrees = new Vector3(-62,-28,0), LightEnergy = 1.15f,
                ShadowEnabled = true, DirectionalShadowMaxDistance = 180 };
            AddChild(_sun);
            _ground = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1,.12f,1) },
                Position = new Vector3(0,-.06f,0), MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("b5b6aa"), Roughness = .85f } };
            AddChild(_ground);
            _camera.Fov = 40;
            _camera.Far = 600;
            var layer = new CanvasLayer();
            AddChild(layer);
            var panel = new ColorRect { Color = new Color(.06f,.08f,.1f,.92f), Size = new Vector2(1440,88) };
            layer.AddChild(panel);
            panel.AddChild(_caption);
            _caption.Position = new Vector2(24,14);
            _caption.AddThemeFontSizeOverride("font_size",20);
            ShowSpecimen();
            if (_capture != null) Directory.CreateDirectory(_capture);
            _ready = true;
        }
        catch (Exception ex) { GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }

    private void ShowSpecimen()
    {
        _asset?.Free();
        StyleEnvironment();
        string stem = $"{Specimens[_specimen]}-{Treatment}";
        string folder = _models ? "models/" : _styles ? "styles/" : _extremes && _treatment > 0 ? "extremes/" : "";
        _asset = GD.Load<PackedScene>($"res://assets/visual-study/kit/{folder}{stem}.glb").Instantiate<Node3D>();
        AddChild(_asset);
        _triangles = 0;
        _surfaces = 0;
        Aabb? bounds = null;
        void Inspect(Node node, Transform3D transform)
        {
            if (node is Node3D spatial) transform *= spatial.Transform;
            if (node is MeshInstance3D mesh)
            {
                Aabb box = transform * mesh.GetAabb();
                bounds = bounds.HasValue ? bounds.Value.Merge(box) : box;
                for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                {
                    _surfaces++;
                    if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D mat) throw new InvalidDataException("Material import failed");
                    mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
                    if (mat.ResourceName == "wall" && (Treatment == "material-rich" || Treatment == "detailed-extreme" || Treatment == "naturalistic") && (mat.NormalTexture == null || mat.RoughnessTexture == null))
                        throw new InvalidDataException("Masonry maps did not import");
                    if (Treatment == "sculpted-extreme")
                    {
                        var sculpted = new ShaderMaterial { Shader = GD.Load<Shader>("res://kit-sculpted.gdshader") };
                        sculpted.SetShaderParameter("base_color", mat.AlbedoColor);
                        mesh.SetSurfaceOverrideMaterial(surface, sculpted);
                    }
                    if (_styles) mesh.SetSurfaceOverrideMaterial(surface, StyleMaterial(mat));
                    if (_palettes) mesh.SetSurfaceOverrideMaterial(surface, PaletteMaterial(mat));
                    var arrays = mesh.Mesh.SurfaceGetArrays(surface);
                    _triangles += arrays[(int)Mesh.ArrayType.Index].AsInt32Array().Length / 3;
                    foreach (Vector3 normal in arrays[(int)Mesh.ArrayType.Normal].AsVector3Array())
                        if (!normal.IsFinite() || Math.Abs(normal.Length()-1) > .002f) throw new InvalidDataException("Invalid normal");
                }
            }
            foreach (Node child in node.GetChildren()) Inspect(child, transform);
        }
        Inspect(_asset, Transform3D.Identity);
        if (bounds is not Aabb b || Math.Abs(b.Position.Y) > .025f || _triangles == 0)
            throw new InvalidDataException($"Ground/geometry contract failed: {stem}: {bounds}");
        _bounds = b;
        Vector3 target = Targets[_specimen];
        Vector3 eye = Eyes[_specimen];
        if (_detail)
        {
            target = _specimen == 0 ? new Vector3(2.3f, 4.7f, 5.3f) : _specimen == 1 ? new Vector3(2, 22, 8) : new Vector3(.65f,.75f,1);
            eye = target + (_specimen == 0 ? new Vector3(4,1.5f,6) : _specimen == 1 ? new Vector3(8,3,12) : new Vector3(2,1,2.5f));
        }
        _camera.Position = eye;
        _camera.LookAt(target);
        _target = target;
        float size = _specimen == 0 ? 22 : _specimen == 1 ? 45 : 8;
        _ground.Scale = new Vector3(size,1,size);
        _caption.Text = $"0063 / {Specimens[_specimen].ToUpperInvariant()} / {Treatment.ToUpperInvariant()} / NOON\nArt fixture · {(_models ? "MODEL ROUND 1 / geometry varies, warm palette shared" : _styles ? "ART STYLES / palette and shading vary" : _extremes ? "EXTREMES EXPLORATION" : "common composition")} · 1–3 specimens · Q/W/E/R treatments · D detail · Esc closes";
        if (_palettes) _caption.Text = $"0063 / {Specimens[_specimen].ToUpperInvariant()} / {Treatment.ToUpperInvariant()} / {Palette.ToUpperInvariant()}\nPalette study · identical geometry and noon light · Q/W/E palettes · D close · Esc closes";
        GD.Print($"KIT_IMPORT_OK {stem} triangles={_triangles} surfaces={_surfaces} bounds={_bounds}");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!_ready || @event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.Escape) { GetTree().Quit(); return; }
        if (_capture != null) return;
        if (!_paletteTower && key.Keycode >= Key.Key1 && key.Keycode <= Key.Key3) _specimen = (int)(key.Keycode-Key.Key1);
        else if (key.Keycode == Key.Q) _treatment = 0;
        else if (key.Keycode == Key.W) _treatment = 1;
        else if (key.Keycode == Key.E) _treatment = 2;
        else if (key.Keycode == Key.R && !_palettes && (_styles || _models)) _treatment = 3;
        else if (key.Keycode == Key.D) _detail = !_detail;
        else return;
        ShowSpecimen();
    }

    public override void _Process(double delta)
    {
        if (!_ready || _capture == null || ++_frame % 40 != 0) return;
        try
        {
            string stem = $"{Specimens[_specimen]}-{Treatment}";
            string view = _detail ? "detail" : "whole";
            if (_palettes) stem += "-" + Palette;
            string path = Path.Combine(_capture, stem + (_extremes || _styles || _models ? "-" + view : ""));
            Error result = GetViewport().GetTexture().GetImage().SavePng(path+".png");
            if (result != Error.Ok) throw new IOException($"PNG failed: {result}");
            File.WriteAllText(path+".json", JsonSerializer.Serialize(new
            {
                specimen = Specimens[_specimen], treatment = Treatment,
                camera = _camera.Position.ToString(), target = _target.ToString(), fov = _camera.Fov,
                viewport = GetViewport().GetVisibleRect().Size.ToString(), taa = GetViewport().UseTaa, light = "noon", exposure = 1,
                triangles = _triangles, surfaces = _surfaces, bounds = _bounds.ToString(),
                palette = _palettes ? Palette : null,
                comparison = _palettes ? "palette-round-1" : _models ? "model-round-1" : _styles ? "art-styles" : _extremes ? "extremes" : "kit-v1", view = _detail ? "detail" : "whole",
                shading = StyleDescription,
                sunRotation = _sun.RotationDegrees.ToString(), sunEnergy = _sun.LightEnergy, sunAngularDistance = _sun.LightAngularDistance,
                ambientEnergy = _environment.AmbientLightEnergy, ssao = _environment.SsaoEnabled,
                tick = (int?)null, population = (int?)null, godot = Engine.GetVersionInfo()["string"].AsString(),
                renderer = RenderingServer.GetCurrentRenderingMethod().ToString()
            }, JsonOptions));
            if ((_extremes || _styles || _models) && !_detail) { _detail = true; ShowSpecimen(); return; }
            _detail = false;
            if (++_treatment == (!_palettes && (_styles || _models) ? 4 : 3)) { _treatment = 0; _specimen++; }
            if (_specimen == 3 || (_paletteTower && _specimen == 2)) { _ready = false; GetTree().Quit(); return; }
            ShowSpecimen();
        }
        catch (Exception ex) { _ready = false; GD.PushError(ex.ToString()); GetTree().Quit(1); }
    }
}
