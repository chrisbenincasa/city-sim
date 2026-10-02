using System;
using Borough.Appearance;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    /// <summary>Cell ids index matching sixteen-slot tables in far-shader.gdshader.</summary>
    private const int FarShaderCells = 16;

    /// <summary>
    /// The widest frame band drawn inside an opening, in metres. It is sub-pixel past about 400 m
    /// and acts from there on through the opening's mean color, which is what stops the glass
    /// reading flat.
    /// </summary>
    private const float FarShaderFrameMetres = .1f;

    /// <summary>Uses the same frame or trim part as the near variant.</summary>
    private static Color FarShaderBandPaint(FarWindowFactoryInput input, Color wall)
    {
        string part = input.BodyVariant switch
        {
            "stepped-point" or "l" or "h" => "frame",
            _ => "trim",
        };
        if (input.PaintSrgb.TryGetValue(part, out Color chosen)) return chosen.SrgbToLinear();
        if (input.PaintSrgb.TryGetValue("frame", out Color frame)) return frame.SrgbToLinear();
        if (input.PaintSrgb.TryGetValue("trim", out Color trim)) return trim.SrgbToLinear();
        return wall.Darkened(.3f);
    }

    /// <summary>A far facade whose openings are drawn procedurally from the bay and storey UVs.</summary>
    // UVs carry fitted bay counts. Opening widths use the nominal bay and may differ by rounding.
    private static ShaderMaterial FarWindowsShader(FarWindowFactoryInput input)
    {
        Color wall = input.PaintSrgb.GetValueOrDefault("wall", new Color(.65f, .65f, .62f)).SrgbToLinear();
        Color glass = input.PaintSrgb.TryGetValue("glass", out Color glazing)
            ? glazing.SrgbToLinear() : wall.Darkened(.55f);
        Color frame = FarShaderBandPaint(input, wall);

        var openings = new Godot.Collections.Array();
        var bands = new Godot.Collections.Array();
        for (int slot = 0; slot < FarShaderCells; slot++)
        {
            openings.Add(Vector4.Zero);
            bands.Add(Vector4.Zero);
        }

        foreach (FamilyBodyFarCell cell in input.CellTable)
        {
            if (cell.Id < 0 || cell.Id >= FarShaderCells) continue;
            if (cell.WindowWidth <= 0f || cell.WindowHeight <= 0f) continue;
            float bay = MathF.Max(cell.BayMetres, .5f);
            float wide = MathF.Min(cell.WindowWidth / bay, .98f);
            float tall = MathF.Min(cell.WindowHeight / ShellBuilder.StoreyMetres, 1f);
            float sill = Math.Clamp(cell.WindowSill / ShellBuilder.StoreyMetres, 0f, 1f - tall);
            openings[cell.Id] = new Vector4(.5f - (wide / 2f), .5f + (wide / 2f), sill, sill + tall);

            // A frame cannot borrow more than the pier or apron beside its opening.
            float pier = MathF.Max(bay - cell.WindowWidth, 0f) / 2f;
            bands[cell.Id] = new Vector4(
                MathF.Min(MathF.Min(FarShaderFrameMetres, pier) / bay, wide * .45f),
                MathF.Min(MathF.Min(FarShaderFrameMetres, cell.WindowSill) / ShellBuilder.StoreyMetres, tall * .45f),
                0f,
                0f);
        }

        var material = new ShaderMaterial
        {
            ResourceName = $"far-shader-{input.FamilyId}-{input.BodyVariant}",
            Shader = GD.Load<Shader>("res://far-shader.gdshader"),
        };
        material.SetShaderParameter("opening", openings);
        material.SetShaderParameter("band", bands);
        material.SetShaderParameter("glass_color", glass);
        material.SetShaderParameter("frame_color", frame);
        return material;
    }
}
