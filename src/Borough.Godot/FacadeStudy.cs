using System;
using System.IO;
using Godot;

namespace Borough.Shell;

// A controlled comparison of the live shader. These are labelled samples, not a simulated city.
public partial class FacadeStudy : Node3D
{
    private readonly InstanceLayer _walls = new();
    private readonly ShaderMaterial _material = new();
    private readonly StandardMaterial3D _wash = new() {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true };
    private readonly Label _caption = new();
    private readonly DirectionalLight3D _sun = new();
    private string _output = "";
    private int _view, _frames;
    private bool _capturing, _ready, _materials;
    private readonly Camera3D _camera = new();

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        if (args.Length != 2 || (args[0] != "--facade-capture" && args[0] != "--material-capture"))
        { GD.PrintErr("Use --facade-capture DIRECTORY"); GetTree().Quit(1); return; }
        _materials = args[0] == "--material-capture";
        _output = args[1];
        Directory.CreateDirectory(_output);
        GetWindow().Mode = Window.ModeEnum.Windowed;
        GetWindow().Size = new Vector2I(1600, 720);
        GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
        _material.Shader = GD.Load<Shader>("res://buildings.gdshader");
        _material.SetShaderParameter("masonry_albedo", GD.Load<Texture2D>("res://assets/city/brick-wall-diffuse.jpg"));
        _material.SetShaderParameter("masonry_normal", GD.Load<Texture2D>("res://assets/city/brick-wall-normal.jpg"));
        FacadeMaterials.Configure(_material);
        AddChild(_walls);
        _walls.Multimesh.Mesh = new BoxMesh { Material = _material };
        _walls.Multimesh.UseColors = true;
        _walls.Multimesh.UseCustomData = true;
        var environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("637781"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("c6d5e3"), AmbientLightEnergy = .45f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic };
        AddChild(new WorldEnvironment { Environment = environment });
        _sun.RotationDegrees = new Vector3(-40, -25, 0);
        _sun.ShadowEnabled = true;
        AddChild(_sun);
        var ground = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(100, 60) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("929088"), Roughness = 1 } };
        AddChild(ground);
        _camera.Fov = 48;
        _camera.Position = new Vector3(0, 11, 48);
        AddChild(_camera);
        _camera.LookAt(new Vector3(0, 5, 0));
        var canvas = new CanvasLayer(); AddChild(canvas); canvas.AddChild(_caption);
        _caption.Position = new Vector2(24, 20);
        _caption.AddThemeFontSizeOverride("font_size", 24);
        Configure();
        _ready = true;
    }

    private void Configure()
    {
        if (_materials) { ConfigureMaterials(); return; }
        bool shop = (_view % 4) >= 2, render = (_view % 2) == 1;
        bool night = _view / 4 == 1, wash = _view / 4 == 2;
        _material.SetShaderParameter("night_phase", night ? .25f : 1f);
        _walls.MaterialOverride = wash ? _wash : null;
        _sun.LightEnergy = night ? .05f : 1.1f;
        _caption.Text = $"{(render ? "Plaster" : "Brick")} · {(shop ? "Commercial frontage" : "Residential frontage")} · {(wash ? "Debug wash" : night ? "Night" : "Daylight")}\n"
            + "Left to right: vacant · half occupied · fully occupied · abandoned   |   controlled shader samples";
        for (int i = 0; i < 4; i++)
        {
            _walls.Multimesh.Identity(i, (ulong)i + 1);
            _walls.Multimesh.SetInstanceTransform(i, new Transform3D(Basis.FromScale(new Vector3(12, 10.5f, 8)), new Vector3((i - 1.5f) * 16, 5.25f, 0)));
            Color wall = wash ? new Color("d2a14a") : new Color("bba18c");
            _walls.Multimesh.SetInstanceColor(i, wall.SrgbToLinear());
            _walls.Multimesh.SetInstanceCustomData(i, new Color(.5f, 1f, i == 3 ? 0 : i * .5f,
                FacadeAppearance.Pack(render ? (byte)180 : (byte)40, shop, i == 3)));
        }
        _walls.Multimesh.VisibleInstanceCount = 4;
        _walls.Multimesh.Flush();
        _frames = 0;
    }

    private void ConfigureMaterials()
    {
        int candidate = _view % 3;
        FacadeMaterials.Configure(_material, candidate);
        _camera.Position = _view < 3 ? new Vector3(0, 11, 48) : new Vector3(0, 26, 120);
        _camera.LookAt(new Vector3(0, 5, 0));
        _material.SetShaderParameter("night_phase", 1f);
        string[] names = ["Untreated reference", "Painted plaster + layered wear", "Worn plaster + layered wear"];
        _caption.Text = names[candidate] + (_view < 3 ? " · close" : " · street distance")
            + "\nBrick occupied · brick abandoned · plaster occupied · plaster abandoned | same warm paint and geometry";
        for (int i = 0; i < 4; i++)
        {
            _walls.Multimesh.Identity(i, (ulong)i + 1);
            _walls.Multimesh.SetInstanceTransform(i, new Transform3D(Basis.FromScale(new Vector3(12, 10.5f, 8)), new Vector3((i - 1.5f) * 16, 5.25f, 0)));
            _walls.Multimesh.SetInstanceColor(i, new Color("bba18c").SrgbToLinear());
            _walls.Multimesh.SetInstanceCustomData(i, new Color(.5f, 1f, i % 2 == 0 ? 1 : 0,
                FacadeAppearance.Pack(i < 2 ? (byte)40 : (byte)180, false, i % 2 == 1)));
        }
        _walls.Multimesh.VisibleInstanceCount = 4;
        _walls.Multimesh.Flush();
        _frames = 0;
    }

    public override async void _Process(double delta)
    {
        if (!_ready || _capturing || ++_frames < 12) return;
        _capturing = true;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image shot = GetViewport().GetTexture().GetImage();
        if (shot.SavePng(Path.Combine(_output, $"{(_materials ? "material" : "facade")}-{_view:00}.png")) != Error.Ok)
        { GetTree().Quit(1); return; }
        if (++_view == (_materials ? 6 : 12)) { GetTree().Quit(); return; }
        Configure();
        _capturing = false;
    }
}
