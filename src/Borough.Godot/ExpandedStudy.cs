using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;

namespace Borough.Shell;

public partial class ExpandedStudy : Node3D
{
    private static readonly string[] Specimens = { "attached-townhouse", "walk-up-apartment", "courtyard-apartment", "office-tower", "mixed-use-tower", "small-shop", "corner-shop", "factory-with-office", "school", "bus", "walker", "round-tree", "conical-tree" };
    private static readonly string[] Palettes = { "warm-slate", "earth-terracotta" };
    private readonly Camera3D _camera = new();
    private readonly Label _caption = new();
    private readonly List<object> _instances = new();
    private readonly Dictionary<string, PackedScene> _scenes = new();
    private Dictionary<string, Dictionary<string, string>> _colors = new();
    private Node3D _content = null!;
    private Godot.Environment _environment = null!;
    private DirectionalLight3D _sun = null!;
    private string? _output;
    private int _page;
    private int _frame;
    private bool _ready;
    private Vector3 _target;
    private string _subject = "";
    private string _palette = "";
    private string _view = "";
    private int _triangles;
    private int _surfaces;
    private bool _construction;
    private bool _fidelity;
    private bool _neighbourhood;
    private float _brickScale = 1;
    private static readonly string[] ConstructionSpecimens = { "attached-townhouse", "small-shop", "factory-with-office" };
    private int SpecimenPages => _neighbourhood ? 0 : _fidelity ? 4 : _construction ? 12 : 52;
    private int Pages => _neighbourhood ? 6 : SpecimenPages + (_construction ? 0 : 5);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public override void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "--capture-expanded") _output = args[++i];
                else if (args[i] == "--construction") _construction = true;
                else if (args[i] == "--fidelity") { _fidelity = true;_construction = true; }
                else if (args[i] == "--neighbourhood") { _neighbourhood = true;_fidelity = true; }
                else if (args[i] == "--brick-scale") _brickScale = float.Parse(args[++i],System.Globalization.CultureInfo.InvariantCulture);
            if (!float.IsFinite(_brickScale) || _brickScale < .5f || _brickScale > 2) throw new ArgumentOutOfRangeException("brick-scale");
            _colors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(ProjectSettings.GlobalizePath("res://../../art/visual-study/palettes.json")))!;
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = new Vector2I(1440,960);
            GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
            GetViewport().UseTaa = true;
            RenderingServer.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftUltra);
            _environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color("c9d8df"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color("d6e2ed"), AmbientLightEnergy = .55f,
                TonemapMode = Godot.Environment.ToneMapper.Filmic, SsaoEnabled = true, SsaoRadius = .4f, SsaoIntensity = .65f,
                Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial { SkyTopColor = new Color("789ebd"), SkyHorizonColor = new Color("d6dfdf"), GroundBottomColor = new Color("626463"), GroundHorizonColor = new Color("c4c8c3") } },
                ReflectedLightSource = Godot.Environment.ReflectionSource.Sky
            };
            AddChild(new WorldEnvironment { Environment = _environment });
            _sun = new DirectionalLight3D { RotationDegrees = new Vector3(-62,-28,0), LightEnergy = 1.15f, LightAngularDistance = .5f, ShadowEnabled = true, DirectionalShadowMaxDistance = 400 };
            AddChild(_sun);
            _camera.Fov = 40; _camera.Far = 1200; _camera.Near = .03f; AddChild(_camera);
            var canvas = new CanvasLayer();AddChild(canvas);
            var panel = new ColorRect { Color = new Color(.06f,.08f,.1f,.92f), Size = new Vector2(1440,88) };canvas.AddChild(panel);
            panel.AddChild(_caption);_caption.Position = new Vector2(24,14);_caption.AddThemeFontSizeOverride("font_size",20);
            if (_output != null) Directory.CreateDirectory(_output);
            ShowPage();_ready = true;
        }
        catch (Exception ex) { GD.PushError(ex.ToString());GetTree().Quit(1); }
    }

    private Aabb AddAsset(string asset, Vector3 position, float yaw, string palette, bool anchor = false)
    {
        string path = anchor ? $"res://assets/visual-study/kit/models/{asset}.glb" : $"res://assets/visual-study/{(_fidelity ? "fidelity" : _construction ? "construction" : "expanded")}/{asset}.glb";
        if (_neighbourhood && !anchor && asset != "attached-townhouse") path = $"res://assets/visual-study/neighbourhood/{asset}.glb";
        if (!_scenes.TryGetValue(path, out PackedScene? scene)) { scene = GD.Load<PackedScene>(path);_scenes.Add(path,scene); }
        var instance = scene.Instantiate<Node3D>();
        Aabb? bounds = null;
        int triangles = 0, surfaces = 0;
        void Inspect(Node node, Transform3D transform)
        {
            if (node is Node3D spatial) transform *= spatial.Transform;
            if (node is MeshInstance3D mesh)
            {
                Aabb box = transform * mesh.GetAabb();bounds = bounds.HasValue ? bounds.Value.Merge(box) : box;
                for (int surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
                {
                    if (mesh.GetActiveMaterial(surface) is not StandardMaterial3D original) throw new InvalidDataException($"Material missing: {asset}");
                    var material = (StandardMaterial3D)original.Duplicate();
                    string name = System.Text.RegularExpressions.Regex.Replace(original.ResourceName, @"\.\d+$", "");
                    if (_fidelity && name == "brick" && (original.AlbedoTexture == null || original.NormalTexture == null || original.RoughnessTexture == null))
                        throw new InvalidDataException("Brick texture maps did not survive GLB import");
                    if (_fidelity && name == "glass") { material.AlbedoColor = new Color(.5f,.6f,.63f,.14f);material.Metallic = 0;material.MetallicSpecular = .18f; }
                    else if (_fidelity && name.StartsWith("slate-",StringComparison.Ordinal)) material.AlbedoColor = new Color(_colors[palette]["roof"]).Darkened(int.Parse(name[^1..])*.02f);
                    else if (_fidelity && name == "brick") material.AlbedoColor = palette == "warm-slate" ? new Color("e0d5c5") : new Color("e1cbb3");
                    else if (_colors[palette].TryGetValue(name,out string? hex)) { Color color = new Color(hex);color.A = original.AlbedoColor.A;material.AlbedoColor = color; }
                    else if (_construction && name == "roof-secondary") material.AlbedoColor = new Color(_colors[palette]["roof"]).Darkened(.055f);
                    else if (palette != "original" && name is not ("foliage" or "foliage-dark" or "bark" or "skin" or "cloth" or "room" or "blind" or "metal" or "curtain" or "interior" or "paving" or "paving-light")) throw new InvalidDataException($"Unmapped material: {name}");
                    if (_fidelity && name == "brick")
                    {
                        material.Uv1Scale = new Vector3(1/_brickScale,1/_brickScale,1);
                        material.UV2Scale = material.Uv1Scale;
                    }
                    mesh.SetSurfaceOverrideMaterial(surface,material);
                    var arrays = mesh.Mesh.SurfaceGetArrays(surface);
                    triangles += arrays[(int)Mesh.ArrayType.Index].AsInt32Array().Length / 3;
                    foreach (Vector3 normal in arrays[(int)Mesh.ArrayType.Normal].AsVector3Array())
                        if (!normal.IsFinite() || Math.Abs(normal.Length()-1) > .002f) throw new InvalidDataException($"Normal: {asset}");
                    surfaces++;
                }
            }
            foreach (Node child in node.GetChildren()) Inspect(child,transform);
        }
        Inspect(instance,Transform3D.Identity);
        if (bounds is not Aabb b || Math.Abs(b.Position.Y) > .025f || triangles == 0) throw new InvalidDataException($"Ground/mesh: {asset}");
        _content.AddChild(instance);instance.Position = position;instance.RotationDegrees = new Vector3(0,yaw,0);
        _triangles += triangles;_surfaces += surfaces;
        _instances.Add(new { asset, palette, anchor, position = new[] { position.X,position.Y,position.Z }, yaw,
            boundsMin = new[] { b.Position.X,b.Position.Y,b.Position.Z }, boundsSize = new[] { b.Size.X,b.Size.Y,b.Size.Z }, triangles, surfaces });
        return b;
    }

    private void GroundBox(string name, Vector3 position, Vector3 size, string color)
    {
        _content.AddChild(new MeshInstance3D { Name = name, Position = position, Mesh = new BoxMesh { Size = size }, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(color), Roughness = .9f } });
    }

    private void Layout(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ProjectSettings.GlobalizePath("res://../../art/visual-study/expanded-layout.json")));
        var layout = doc.RootElement.GetProperty(name);
        foreach (var box in layout.GetProperty("ground").EnumerateArray())
        {
            var p = box.GetProperty("position").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            var s = box.GetProperty("size").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            GroundBox(box.GetProperty("name").GetString()!,new Vector3(p[0],p[1],p[2]),new Vector3(s[0],s[1],s[2]),box.GetProperty("color").GetString()!);
        }
        foreach (var item in layout.GetProperty("instances").EnumerateArray())
        {
            var p = item.GetProperty("position").EnumerateArray().Select(x=>x.GetSingle()).ToArray();
            AddAsset(item.GetProperty("asset").GetString()!,new Vector3(p[0],p[1],p[2]),item.GetProperty("yaw").GetSingle(),item.GetProperty("palette").GetString()!,item.GetProperty("anchor").GetBoolean());
        }
    }

    private void ShowPage()
    {
        _content?.Free();_content = new Node3D();AddChild(_content);_instances.Clear();_triangles = 0;_surfaces = 0;
        Vector3 eye;
        if (_neighbourhood)
        {
            Neighbourhood();_subject = "neighbourhood";_palette = "mixed-approved";
            (_view,eye,_target) = _page switch
            {
                0 => ("street",new Vector3(18,3.3f,27),new Vector3(-5,6,-5)),
                1 => ("neighbourhood",new Vector3(66,52,79),new Vector3(-10,17,-10)),
                2 => ("city-distance",new Vector3(150,115,170),new Vector3(-10,10,-8)),
                3 => ("shop",new Vector3(13,7,13),new Vector3(-1,4,-4)),
                4 => ("roofs",new Vector3(24,42,10),new Vector3(-9,15,-20)),
                _ => ("tower",new Vector3(29,27,-8),new Vector3(-18,23,-36))
            };
        }
        else if (_page < SpecimenPages)
        {
            _subject = (_construction ? ConstructionSpecimens : Specimens)[_page/4];_palette = Palettes[(_page/2)%2];bool close = _page%2 == 1;_view = close ? "close" : "whole";
            Aabb b = AddAsset(_subject,Vector3.Zero,0,_palette);
            Vector3 center = b.GetCenter();float span = Math.Max(b.Size.X,Math.Max(b.Size.Y,b.Size.Z));
            float ground = Math.Max(b.Size.X,b.Size.Z)*1.6f;
            GroundBox("specimen base",new Vector3(center.X,-.06f,center.Z),new Vector3(ground,.12f,ground),"b5b6aa");
            _target = center;
            eye = center + new Vector3(span*1.5f,span*1.05f,span*1.95f);
            if (close)
            {
                _target = new Vector3(center.X,b.Position.Y+b.Size.Y*.38f,b.End.Z);
                eye = _target + new Vector3(span*.3f,span*.18f,span*.72f);
            }
        }
        else
        {
            int view = _page-SpecimenPages;
            _subject = view < 3 ? "street" : "repetition";_palette = "mixed-approved";
            Layout(_subject);
            (_view,eye,_target) = view switch
            {
                0 => ("overview",new Vector3(142,128,170),new Vector3(0,12,-30)),
                1 => ("street",new Vector3(22,9,4),new Vector3(-24,5,-15)),
                2 => ("courtyard",new Vector3(-20,28,-2),new Vector3(-48,5,-38)),
                3 => ("overview",new Vector3(125,108,140),new Vector3(0,4,0)),
                _ => ("street",new Vector3(15,10,29),new Vector3(-20,5,10))
            };
        }
        if (_construction)
        {
            string path = ProjectSettings.GlobalizePath($"res://../../artifacts/visual-study/expanded/{_subject}-{_palette}-{_view}.json");
            using var reference = JsonDocument.Parse(File.ReadAllText(path));
            static Vector3 Vector(string text)
            {
                float[] xyz = text.Trim('(',')').Split(',').Select(s=>float.Parse(s,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                return new Vector3(xyz[0],xyz[1],xyz[2]);
            }
            eye = Vector(reference.RootElement.GetProperty("camera").GetString()!);
            _target = Vector(reference.RootElement.GetProperty("target").GetString()!);
        }
        _camera.Position = eye;_camera.LookAt(_target);
        _caption.Text = $"0063 / EXPANDED KIT / {_subject.ToUpperInvariant()} / {_palette.ToUpperInvariant()} / {_view.ToUpperInvariant()}\nArt fixture · no simulation state · noon · Left/Right browse · Escape closes";
        if (_construction) _caption.Text = $"0063 / CONSTRUCTION BALANCE / {_subject.ToUpperInvariant()} / {_palette.ToUpperInvariant()} / {_view.ToUpperInvariant()}\nActual openings · finer joinery · constructed roofs · matched first-pass camera and noon light";
        if (_fidelity) _caption.Text = $"0063 / FIDELITY PILOT / {_subject.ToUpperInvariant()} / {_palette.ToUpperInvariant()} / {_view.ToUpperInvariant()}\nBrick material · sash windows and curtains · cornices · entrance courts · matched camera and noon light";
        if (_fidelity && _brickScale != 1) _caption.Text = $"0063 / BRICK SCALE / {_brickScale:0.00}× / {_palette.ToUpperInvariant()} / {_view.ToUpperInvariant()}\nSame geometry and texture images · diffuse, normal and roughness scaled together · matched camera and light";
        if (_neighbourhood) _caption.Text = $"0063 / NEIGHBOURHOOD FIDELITY / {_view.ToUpperInvariant()}\nAuthored street corner · warm material families · static daylight study · Left/Right browse · Escape closes";
        GD.Print($"EXPANDED_IMPORT_OK {_subject} {_palette} {_view} instances={_instances.Count} triangles={_triangles}");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!_ready || @event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.Keycode == Key.Escape) { GetTree().Quit();return; }
        if (_output != null) return;
        if (key.Keycode == Key.Right) _page = (_page+1)%Pages;
        else if (key.Keycode == Key.Left) _page = (_page+Pages-1)%Pages;
        else return;
        ShowPage();
    }

    public override void _Process(double delta)
    {
        if (!_ready || _output == null || ++_frame%40 != 0) return;
        try
        {
            string path = Path.Combine(_output,$"{_subject}-{_palette}-{_view}");
            if (GetViewport().GetTexture().GetImage().SavePng(path+".png") != Error.Ok) throw new IOException("Capture failed");
            File.WriteAllText(path+".json",JsonSerializer.Serialize(new {
                subject = _subject,palette = _palette,view = _view,comparison = _neighbourhood ? "neighbourhood-1" : _fidelity ? "fidelity-1" : _construction ? "construction-study-1" : "expanded-kit-1",instances = _instances,
                brickScale = _brickScale,
                camera = _camera.Position.ToString(),target = _target.ToString(),fov = _camera.Fov,viewport = GetViewport().GetVisibleRect().Size.ToString(),
                sunRotation = _sun.RotationDegrees.ToString(),sunEnergy = _sun.LightEnergy,sunAngularDistance = _sun.LightAngularDistance,
                ambientEnergy = _environment.AmbientLightEnergy,ssao = _environment.SsaoEnabled,taa = GetViewport().UseTaa,
                exposure = 1,light = "noon",triangles = _triangles,surfaces = _surfaces,
                tick = (int?)null,population = (int?)null,godot = Engine.GetVersionInfo()["string"].AsString(),renderer = RenderingServer.GetCurrentRenderingMethod().ToString()
            },JsonOptions));
            if (++_page == Pages) { _ready = false;GetTree().Quit();return; }ShowPage();
        }
        catch (Exception ex) { _ready = false;GD.PushError(ex.ToString());GetTree().Quit(1); }
    }
}
