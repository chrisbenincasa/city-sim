using Godot;

namespace Borough.Shell;

public static class RoofMaterials
{
    public static void Configure(ShaderMaterial material)
    {
        material.SetShaderParameter("surface_kind", 2);
        const string root = "res://assets/city/material-study/roof-slates-";
        material.SetShaderParameter("roof_albedo", GD.Load<Texture2D>(root + "albedo.jpg"));
        material.SetShaderParameter("roof_normal", GD.Load<Texture2D>(root + "normal.jpg"));
        material.SetShaderParameter("roof_roughness", GD.Load<Texture2D>(root + "roughness.jpg"));
        material.SetShaderParameter("roof_wall_brick", GD.Load<Texture2D>("res://assets/city/brick-wall-diffuse.jpg"));
        material.SetShaderParameter("roof_wall_plaster", GD.Load<Texture2D>("res://assets/city/material-study/painted-plaster-albedo.jpg"));
    }
}
