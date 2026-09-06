using Godot;

namespace Borough.Shell;

public static class FacadeMaterials
{
    public static void Configure(ShaderMaterial material, int candidate = 1)
    {
        const string root = "res://assets/city/material-study/";
        string stem = candidate == 2 ? "worn-plaster" : "painted-plaster";
        material.SetShaderParameter("plaster_mix", candidate == 0 ? 0f : 1f);
        material.SetShaderParameter("plaster_albedo", GD.Load<Texture2D>(root + stem + "-albedo.jpg"));
        material.SetShaderParameter("plaster_normal", GD.Load<Texture2D>(root + stem + "-normal.jpg"));
        material.SetShaderParameter("plaster_roughness", GD.Load<Texture2D>(root + stem + "-roughness.jpg"));
        material.SetShaderParameter("plaster_mean", candidate == 2
            ? new Vector3(.18430542f, .15584884f, .11288088f)
            : new Vector3(.40807008f, .36641045f, .35170243f));
        material.SetShaderParameter("wear_height", GD.Load<Texture2D>(root + "damaged-plaster-height.jpg"));
        material.SetShaderParameter("wear_normal", GD.Load<Texture2D>(root + "damaged-plaster-normal.jpg"));
        material.SetShaderParameter("wear_strength", candidate == 0 ? 0f : 1f);
    }
}
