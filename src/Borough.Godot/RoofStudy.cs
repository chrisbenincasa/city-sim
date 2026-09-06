using System.IO;
using Godot;

namespace Borough.Shell;

// Same meshes and shaders as the city, at fixed camera and light.
public partial class RoofStudy : Node3D
{
    private readonly InstanceLayer[] _roofs = [new(), new(), new()];
    private readonly ShaderMaterial _overlay = new();
    private readonly Label _caption = new();
    private string _output = "";
    private int _frame, _view;
    private bool _ready, _capturing;
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        if (args.Length != 1) { GetTree().Quit(1); return; }
        _output = args[0]; Directory.CreateDirectory(_output);
        GetWindow().Size = new Vector2I(1500, 720);
        GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
        AddChild(new WorldEnvironment { Environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("7b8586"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("d4e0ed"), AmbientLightEnergy = .5f } });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-45,-25,0), LightEnergy = 1.1f });
        _overlay.Shader = GD.Load<Shader>("res://overlay-buildings.gdshader");
        for (int i = 0; i < 3; i++)
        {
            var roof = _roofs[i]; AddChild(roof);
            roof.Multimesh.Mesh = RoofMeshes.Create(i);
            roof.Multimesh.UseColors = true;
            roof.Multimesh.UseCustomData = true;
            roof.Multimesh.Identity(0, 1);
            Basis basis = Basis.FromScale(new Vector3(12.7f, i == 2 ? 2f : 3.8f, 18.7f));

            roof.Multimesh.SetInstanceTransform(0, new Transform3D(basis, new Vector3((i-1)*24, i == 2 ? 9f : 9.9f,0)));
            roof.Multimesh.SetInstanceColor(0, new Color("947363").SrgbToLinear());
            roof.Multimesh.SetInstanceCustomData(0, new Color(new Color("b6aa98").SrgbToLinear(), 180));
            roof.Multimesh.VisibleInstanceCount = 1; roof.Multimesh.Flush();
            var wall = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(12,8,18) },
                Position = new Vector3((i-1)*24,4,0),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("b6aa98") } };
            AddChild(wall);
            var personMaterial = new StandardMaterial3D { AlbedoColor = new Color("303b43") };
            AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = .17f, BottomRadius = .14f, Height = 1.4f },
                Position = new Vector3((i-1)*24+4, .7f, 10), MaterialOverride = personMaterial });
            AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = .13f, Height = .26f },
                Position = new Vector3((i-1)*24+4, 1.62f, 10), MaterialOverride = personMaterial });
        }
        var camera = new Camera3D { Position = new Vector3(23,43,69), Fov = 53 };
        AddChild(camera); camera.LookAt(new Vector3(0,6,0));
        var canvas = new CanvasLayer(); AddChild(canvas); canvas.AddChild(_caption);
        _caption.Position = new Vector2(24,20); _caption.AddThemeFontSizeOverride("font_size",24);
        Configure(); _ready = true;
    }
    private void Configure()
    {
        string[] names = ["Overlay: faces", "Overlay: edges", "Old grid surface", "Procedural courses",
            "Photographed slates — source scale (3 m repeat)", "Photographed slates — enlarged 2× (6 m repeat)"];
        _caption.Text = names[_view]
            + "\nGable · short-ridge hip · paired gables | walls 12 × 18 m, 8 m tall · people 1.75 m";
        _overlay.SetShaderParameter("treatment", _view + 1);
        foreach (var roof in _roofs)
        {
            var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://surfaces.gdshader") };
            RoofMaterials.Configure(material);
            material.SetShaderParameter("roof_treatment", _view >= 4 ? 2 : _view == 3 ? 1 : 0);
            material.SetShaderParameter("roof_texture_metres", _view == 5 ? 6f : 3f);
            roof.MaterialOverride = _view < 2 ? _overlay : material;
        }
        _frame = 0;
    }
    public override async void _Process(double delta)
    {
        if (!_ready || _capturing || ++_frame < 12) return;
        _capturing = true;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image shot = GetViewport().GetTexture().GetImage();
        if (shot.SavePng(Path.Combine(_output,$"roof-{_view}.png")) != Error.Ok) { GetTree().Quit(1); return; }
        if (++_view == 6) { GetTree().Quit(); return; }
        Configure(); _capturing = false;
    }
}
