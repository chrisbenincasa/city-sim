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

    // Never worn itself. Each side of the fade wears a copy; see DerelictBody.
    private ShaderMaterial? _derelictBody;
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
            FarSideResidency(layer);
            layer.Multimesh.Repartition();
        }
    }

    /// <summary>
    /// What decides whether a far body layer's batch draws. The fade asks each batch; the chunk
    /// switch hides the far entries of a near chunk, which is what the study option restores.
    /// </summary>
    private void FarSideResidency(InstanceLayer layer)
    {
        layer.Multimesh.Near = _farFade ? null : NearChunk;
        layer.Multimesh.Needed = _farFade ? FarBatchNeeded : null;
    }

    /// <summary>
    /// Whether a far batch still has a Building the fade asks it to draw: <c>false</c> where every
    /// one of them stands nearer than the ring's inner edge and the dither discards it in full.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The farthest corner of the bounds, because the shader measures each instance's own
    /// origin.</b> Distance over a box is greatest at a vertex and every origin lies inside the
    /// box, so a batch is dropped only once no Building in it carries any weight. One mesh is one
    /// layer, so a rare body is a batch of one and goes as soon as the camera reaches it.
    /// </remarks>
    private bool FarBatchNeeded(Aabb bounds)
    {
        Vector3 eye = _camera.GlobalPosition;
        Vector3 farthest = new(
            Corner(eye.X, bounds.Position.X, bounds.End.X),
            Corner(eye.Y, bounds.Position.Y, bounds.End.Y),
            Corner(eye.Z, bounds.Position.Z, bounds.End.Z));
        float inner = Mathf.Max(0f, _bodyNearMetres - FarFadeRingMetres);
        return eye.DistanceSquaredTo(farthest) > inner * inner;

        static float Corner(float eye, float low, float high) =>
            Mathf.Abs(eye - low) >= Mathf.Abs(eye - high) ? low : high;
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

    /// <summary>
    /// The wash override a body layer wears, on its own side of the fade. Without it one override
    /// would replace both sides' materials and draw the near and far bodies on top of each other.
    /// </summary>
    private Material? BodyWash(InstanceLayer layer)
    {
        if (_washing == Wash.None) return null;

        // ArmWash builds both of these before any layer is asked, and sets them from the overlay
        // style alone, so a copy carries settings that do not change after it is taken.
        if ((BuildingWash ? _categorical : _muted) is not { } source) return null;
        return Faded(source, layer);
    }

    /// <summary>The abandonment overlay a body layer wears, on its own side of the fade.</summary>
    private Material DerelictBody(InstanceLayer layer) =>
        Faded(_derelictBody ??= new ShaderMaterial
        {
            ResourceName = "derelict-body",
            Shader = GD.Load<Shader>("res://derelict-body.gdshader"),
        }, layer);

    /// <summary>The copy of a shared material that fades on the same side as the layer it dresses.</summary>
    private Material Faded(Material material, InstanceLayer layer) =>
        layer.Multimesh.NearOnly ? FadingNear(material) : FadeMaterial(material);
}
