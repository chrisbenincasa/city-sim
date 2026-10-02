using Godot;

namespace Borough.Shell;

public partial class Main
{
    private static ShaderMaterial FarWindowsGrid(FarWindowFactoryInput input)
    {
        Color wall = input.PaintSrgb.GetValueOrDefault("wall", new Color(.65f, .65f, .62f));
        var material = new ShaderMaterial
        {
            ResourceName = $"far-grid-{input.FamilyId}-{input.BodyVariant}",
            Shader = GD.Load<Shader>("res://far-grid.gdshader"),
        };
        material.SetShaderParameter("grid_color", wall.SrgbToLinear().Darkened(.45f));
        return material;
    }
}
