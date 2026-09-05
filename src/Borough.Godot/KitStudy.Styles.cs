using Godot;

namespace Borough.Shell;

public partial class KitStudy
{
    private static readonly string[] ArtStyles = { "painterly", "graphic", "miniature", "naturalistic" };
    private bool _styles;
    private Godot.Environment _environment = null!;
    private DirectionalLight3D _sun = null!;

    private string StyleDescription => Treatment switch
    {
        "realistic" => "articulated construction, shaped vehicle body, warm PBR palette",
        "city-builder" => "exaggerated rooflines, projecting masses, chunky vehicle proportions",
        "voxel" => "crisp grid masses, stepped silhouettes, square wheels",
        "low-poly" => "angular profiles, polygonal tower, planar vehicle facets",
        "painterly" => "directional brush strokes, warm/cool washes, matte shading",
        "graphic" => "flat colours, hard shading bands, selective ink outlines",
        "miniature" => "rounded geometry, matte pastel surfaces, wrapped diffuse, soft sun",
        "naturalistic" => "PBR masonry, metal, opaque reflective glazing, sky reflections",
        _ => Treatment == "sculpted-extreme" ? "three-band diffuse; specular disabled" : "standard PBR"
    };

    private Color StyleColor(string material, Color original)
    {
        string? hex = (Treatment, material) switch
        {
            ("painterly", "wall") => "c9af91", ("painterly", "stone") => "ead6b8",
            ("painterly", "roof") => "666c86", ("painterly", "glass") => "4f727c",
            ("painterly", "paint") => "759783", ("painterly", "frame") => "8e8479",
            ("graphic", "wall") => "dfbd77", ("graphic", "stone") => "f0dfb3",
            ("graphic", "roof") => "37485a", ("graphic", "glass") => "2d5969",
            ("graphic", "paint") => "da795a", ("graphic", "frame") => "435866",
            ("miniature", "wall") => "d4c6b8", ("miniature", "stone") => "ece0cb",
            ("miniature", "roof") => "9d9aaa", ("miniature", "glass") => "788a9c",
            ("miniature", "paint") => "a4c3b4", ("miniature", "rubber") => "61616b",
            ("miniature", "frame") => "ada9b0",
            _ => null
        };
        return hex == null ? original : new Color(hex);
    }

    private Material StyleMaterial(StandardMaterial3D original)
    {
        string name = original.ResourceName;
        Color color = StyleColor(name, original.AlbedoColor);
        if (Treatment == "naturalistic")
        {
            if (name == "wall")
            {
                var wall = new ShaderMaterial { Shader = GD.Load<Shader>("res://kit-natural-wall.gdshader") };
                wall.SetShaderParameter("base_color", color);
                wall.SetShaderParameter("masonry_normal", original.NormalTexture);
                wall.SetShaderParameter("masonry_roughness", original.RoughnessTexture);
                return wall;
            }
            var material = (StandardMaterial3D)original.Duplicate();
            if (name == "glass") { material.Roughness = .075f; material.Metallic = .12f; material.AlbedoColor = new Color("283c47"); }
            if (name == "paint") { material.Metallic = .35f; material.Roughness = .22f; material.ClearcoatEnabled = true; material.Clearcoat = .7f; material.ClearcoatRoughness = .12f; }
            if (name == "frame" || name == "roof") { material.Metallic = .7f; material.Roughness = .3f; }
            return material;
        }
        var shader = new ShaderMaterial { Shader = GD.Load<Shader>($"res://kit-{Treatment}.gdshader") };
        shader.SetShaderParameter("base_color", color);
        if (Treatment == "painterly") shader.SetShaderParameter("stroke_scale", _specimen == 2 ? 8f : _specimen == 1 ? 1.1f : 3f);
        if (Treatment == "graphic" && (name == "wall" || name == "roof" || name == "paint" || name == "rubber"))
        {
            var outline = new ShaderMaterial { Shader = GD.Load<Shader>("res://kit-outline.gdshader") };
            outline.SetShaderParameter("width", _specimen == 2 ? .013f : _specimen == 1 ? .065f : .035f);
            shader.NextPass = outline;
        }
        return shader;
    }

    private void StyleEnvironment()
    {
        if (!_styles && !_models) return;
        _environment.Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color("789ebd"), SkyHorizonColor = new Color("d6dfdf"),
            GroundBottomColor = new Color("626463"), GroundHorizonColor = new Color("c4c8c3")
        } };
        _environment.ReflectedLightSource = Godot.Environment.ReflectionSource.Sky;
        _environment.SsaoEnabled = _models || Treatment == "miniature" || Treatment == "naturalistic";
        _environment.SsaoRadius = Treatment == "miniature" ? .65f : .4f;
        _environment.SsaoIntensity = .65f;
        _sun.LightAngularDistance = Treatment == "miniature" ? 4f : .5f;
    }
}
