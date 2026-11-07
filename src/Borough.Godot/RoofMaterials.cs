using Godot;

namespace Borough.Shell;

public static class RoofMaterials
{
    private const string Shingles = "res://assets/city/roofing/asphalt-shingles";

    public static void Configure(ShaderMaterial material)
    {
        material.SetShaderParameter("surface_kind", 2);
        material.SetShaderParameter("roof_albedo", Textures.Load(Shingles + "-albedo.png"));
        material.SetShaderParameter("roof_normal", Textures.Load(Shingles + "-normal.png"));
        material.SetShaderParameter("roof_roughness", Textures.Load(Shingles + "-roughness.png"));

        var manifest = Json.ParseString(Godot.FileAccess.GetFileAsString(Shingles + ".json")).AsGodotDictionary();
        var tile = manifest["tile"].AsGodotDictionary();
        material.SetShaderParameter("shingle_tile_metres", tile["metres"].AsSingle());
        material.SetShaderParameter("shingle_courses", tile["courses"].AsInt32());
        material.SetShaderParameter("shingle_mean_luminance", manifest["albedo_linear_mean_luminance"].AsSingle());

        material.SetShaderParameter("roof_wall_brick", Textures.Load("res://assets/city/brick-wall-diffuse.jpg"));
        material.SetShaderParameter("roof_wall_plaster", Textures.Load("res://assets/city/material-study/painted-plaster-albedo.jpg"));
    }
}
