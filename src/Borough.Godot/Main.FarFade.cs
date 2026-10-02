using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

/// <summary>Cross-fades mid-rise and tower bodies by each Building's distance from the main camera.</summary>
public partial class Main
{
    private const float FarFadeRingMetres = 50f;
    private const float FarFadeDepthMetres = 1f;

    // Copies keep the fade off low-rise bodies that share the same library materials.
    private readonly Dictionary<Material, Material> _fadeMaterials = [];
    private readonly Dictionary<Material, Material> _fadeNearMaterials = [];
    private readonly List<ShaderMaterial> _fadeFacades = [];
    private readonly HashSet<BaseMaterial3D> _checkedBodyKitMaterials = [];
    private bool _farFade = true;
    private bool _bodyKitCheckedAtStartup;

    private void CheckBodyKitAtStartup()
    {
        if (_bodyKitCheckedAtStartup) return;
        _bodyKitCheckedAtStartup = true;
        foreach (var family in Preset()?.Families ?? [])
        {
            if (family.Body is not { } body || body.Midrise is null && body.Tower is null) continue;
            foreach (Material material in BodyLibrary(body.Library).Values)
            {
                if (material is BaseMaterial3D authored) CheckBodyKitMaterial(authored);
            }
        }
    }

    private void FarFadeStudy(string[] words)
    {
        if (words.Length != 2 || words[1] is not ("on" or "off"))
        {
            _refused = "far-fade on|off";
            return;
        }

        _farFade = words[1] == "on";
        ApplyFarFade();
        GD.Print($"far_fade\t{(_farFade ? "on" : "off")}\tring {FarFadeRingMetres:F0} m\tband {_bodyNearMetres:F0} m");
    }

    private Material FadeMaterial(Material material)
    {
        if (_fadeMaterials.TryGetValue(material, out Material? copy)) return copy;

        copy = Copy(material, "far-fade", nearSide: false);
        _fadeMaterials[material] = copy;
        FadePart(copy, nearSide: false);
        return copy;
    }

    private Material FadingNear(Material material)
    {
        if (_fadeNearMaterials.TryGetValue(material, out Material? copy)) return copy;

        copy = Copy(material, "near-fade", nearSide: true);
        _fadeNearMaterials[material] = copy;
        FadePart(copy, nearSide: true);
        return copy;
    }

    private Material Copy(Material material, string suffix, bool nearSide)
    {
        Material copy = material is BaseMaterial3D authored
            ? Rebuilt(authored)
            : (Material)material.Duplicate();
        copy.ResourceName = $"{material.ResourceName}-{suffix}";
        if (copy is ShaderMaterial shader) shader.SetShaderParameter("fade_near_side", nearSide);
        return copy;
    }

    private ShaderMaterial Rebuilt(BaseMaterial3D authored)
    {
        CheckBodyKitMaterial(authored);
        var copy = new ShaderMaterial { Shader = GD.Load<Shader>("res://body-kit.gdshader") };
        copy.SetShaderParameter("albedo", authored.AlbedoColor);
        copy.SetShaderParameter("roughness", authored.Roughness);
        copy.SetShaderParameter("metallic", authored.Metallic);
        copy.SetShaderParameter("specular", authored.MetallicSpecular);
        copy.SetShaderParameter("tint_by_vertex_colour", authored.VertexColorUseAsAlbedo);
        return copy;
    }

    /// <summary>Warns once when a loaded kit material has a setting the rebuild cannot reproduce.</summary>
    private int CheckBodyKitMaterial(BaseMaterial3D authored)
    {
        if (!_checkedBodyKitMaterials.Add(authored)) return 0;
        int warnings = 0;

        // The kit shader is double-sided. All other uncarried settings must retain Godot's defaults.
        using var expected = new StandardMaterial3D { CullMode = BaseMaterial3D.CullModeEnum.Disabled };
        foreach (Godot.Collections.Dictionary property in authored.GetPropertyList())
        {
            if ((property["usage"].AsInt64() & (long)PropertyUsageFlags.Storage) == 0) continue;
            string name = property["name"].AsString();
            if (name is "albedo_color" or "roughness" or "metallic" or "metallic_specular"
                or "vertex_color_use_as_albedo" or "resource_name" or "resource_local_to_scene"
                or "resource_path") continue;
            Variant value = authored.Get(name);
            Variant baseline = expected.Get(name);
            // Godot's Variant equality does not treat two null Object variants as equal.
            if (property["type"].AsInt64() == (long)Variant.Type.Object
                && value.AsGodotObject() is null && baseline.AsGodotObject() is null) continue;
            if (value.Equals(baseline)) continue;

            GD.PushWarning($"Body kit material '{authored.ResourceName}' property '{name}' is not carried by body-kit.gdshader.");
            warnings++;
        }

        return warnings;
    }

    private Material FadingFacade(Material material)
    {
        if (material is ShaderMaterial facade)
        {
            _fadeFacades.Add(facade);
            FadePart(facade, nearSide: false);
        }

        return material;
    }

    private void ApplyFarFade()
    {
        foreach (Material copy in _fadeMaterials.Values) FadePart(copy, nearSide: false);
        foreach (Material copy in _fadeNearMaterials.Values) FadePart(copy, nearSide: true);
        foreach (ShaderMaterial facade in _fadeFacades) FadePart(facade, nearSide: false);
        foreach (InstanceLayer layer in _bodyLayers.Values)
        {
            if (layer.Multimesh.NearOnly) continue;
            layer.Multimesh.Near = _farFade ? NoChunkNear : NearChunk;
            layer.Multimesh.Repartition();
        }
    }

    private void FadePart(Material material, bool nearSide)
    {
        float outer = _farFade ? _bodyNearMetres : 0f;
        float inner = _farFade ? Mathf.Max(0f, outer - FarFadeRingMetres) : 0f;
        if (material is not ShaderMaterial shader) return;

        shader.SetShaderParameter("fade_inner_metres", inner);
        shader.SetShaderParameter("fade_outer_metres", outer);
        shader.SetShaderParameter("fade_depth_metres", _farFade && !nearSide ? FarFadeDepthMetres : 0f);
    }

    // Far instances remain resident inside near chunks so the shared dither can draw both sides.
    private static bool NoChunkNear(Vector2I key) => false;
}
