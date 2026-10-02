using System;
using System.IO;

namespace Borough.Tests.Appearance;

/// <summary>
/// What the shell has to keep doing for a far body, read off its source.
/// </summary>
/// <remarks>
/// ⚠ <b>Source assertions, because none of this is reachable without Godot.</b> Picking, layer
/// residency, material overrides and overlays are all <c>Borough.Godot</c> types, which this
/// assembly does not reference — <see cref="BodyFadeTests"/> reads the shaders for the same reason.
/// So each one is a tripwire against a line going away silently; the behaviour itself is watched in
/// <c>plans/evidence/far-bodies/fixes</c>.
/// </remarks>
public sealed class FarBodyShellTests
{
    [Fact]
    public void ABodiedBuildingIsPickedAndNamedByItsOwnLayer()
    {
        string source = Shell("Main.RoadInformation.cs");

        // Its massing box is gone, so the ray and the map both have to reach the body layers.
        Assert.Contains("foreach (InstanceLayer layer in _bodyLayers.Values) HitBody(layer);", source);
        Assert.Contains("RayReach(inverse * origin, inverse.Basis * direction, bounds, distance)", source);

        // The far layer only, on both passes: one entry per Building, so a Building is named once.
        Assert.Contains("if (layer.Multimesh.NearOnly) continue;", source);
        Assert.Contains("Add(layer, \"building\", body.GetCenter() with { Y = body.End.Y });", source);
    }

    [Fact]
    public void AWashAndTheAbandonmentOverlayFadeOnTheLayersOwnSide()
    {
        string bodies = Shell("Main.FamilyBodies.cs");
        Assert.Contains("layer.MaterialOverride = BodyWash(layer);", bodies);
        Assert.Contains("layer.MaterialOverlay = DerelictBody(layer);", bodies);
        Assert.Contains("DerelictBody(layer) : null;", bodies);

        // Neither wash material may reach a body layer uncopied, on either path that dresses one.
        Assert.DoesNotContain("_categorical", bodies);
        Assert.DoesNotContain("_muted", bodies);
        Assert.Contains("name == \"family-body\" => BodyWash(over),", Shell("Main.Ground.cs"));

        string fade = Shell("Main.FarFade.cs");
        Assert.Contains("layer.Multimesh.NearOnly ? FadingNear(material) : FadeMaterial(material);", fade);
    }

    [Fact]
    public void AnIncrementalBodyChangeRefreshesTheSpatialPasses()
    {
        // Foliage exclusions and the vacant-Lot drawing follow the geometry flag, and a Building
        // that draws as a body writes no massing instance for ReplaceBuilding to report on.
        string rendering = Shell("Main.Rendering.cs");
        Assert.Contains("geometry |= ReplaceBuilding(old.Id) | RemoveFamilyBody(old.Id);", rendering);
        Assert.Contains("PlaceFamilyBody(slot, out bool bodyChanged);", rendering);
        Assert.Contains("geometry |= bodyChanged;", rendering);
        Assert.Contains("geometry = before.At != after.At || before.NearShape != after.NearShape;",
            Shell("Main.FamilyBodies.cs"));
    }

    [Fact]
    public void ABatchNoFadingBodyDrawsOutOfStopsDrawing()
    {
        // Each side asks its own bounds under the fade and the chunk switch takes over without it.
        string fade = Shell("Main.FarFade.cs");
        Assert.Contains("instances.Near = fades ? null : NearChunk;", fade);
        Assert.Contains(
            "instances.Needed = !fades ? null : instances.NearOnly ? NearBatchNeeded : FarBatchNeeded;",
            fade);

        // Each Building's own origin, which is what the shader measures. No box will do: one
        // around the meshes carries a tower's height and one around the origins invents corners no
        // Building stands on, and either keeps a batch every Building of which has faded.
        Assert.Contains("private bool FarBatchNeeded(InstanceBuffer.Batch batch)", fade);
        Assert.Contains("if (eye.DistanceSquaredTo(instance.Transform.Origin) > inner * inner) return true;",
            fade);
        Assert.Contains("private bool NearBatchNeeded(InstanceBuffer.Batch batch)", fade);
        Assert.Contains(
            "if (eye.DistanceSquaredTo(instance.Transform.Origin) <= _bodyNearMetres * _bodyNearMetres) return true;",
            fade);

        string layers = Shell("InstanceLayer.cs");
        Assert.Contains("public Func<Batch, bool>? Needed { get; set; }", layers);
        Assert.Contains("if (Needed is not null && !Needed(batch)) return false;", layers);
        Assert.Contains("|| Near is not null || Needed is not null;", layers);
    }

    [Fact]
    public void TheGridInkAndTheFallbackMaterialAreBothAuthoredOnce()
    {
        // A `source_color` uniform would convert the ink a second time; the factory hands it over
        // in linear light already, as far-shader.gdshader's glass and frame colours are.
        Assert.Contains("uniform vec4 grid_color = ", Shell("far-grid.gdshader"));
        Assert.DoesNotContain("grid_color : source_color", Shell("far-grid.gdshader"));
        Assert.Contains("SrgbToLinear().Darkened(.45f)", Shell("Main.FarWindows.Grid.cs"));

        // One material for every part a library is missing, double-sided as the kit shader is, so
        // the fade's copies and the drift check hold one entry and nothing warns about it.
        string bodies = Shell("Main.FamilyBodies.cs");
        Assert.Equal(1, Occurrences(bodies, "new StandardMaterial3D"));
        Assert.Contains("CullMode = BaseMaterial3D.CullModeEnum.Disabled,", bodies);
        Assert.Equal(2, Occurrences(bodies, "?? MissingBodyPart"));
    }

    private static int Occurrences(string source, string text)
    {
        int count = 0;
        for (int at = source.IndexOf(text, StringComparison.Ordinal); at >= 0;
            at = source.IndexOf(text, at + text.Length, StringComparison.Ordinal)) count++;
        return count;
    }

    private static string Shell(string file) =>
        File.ReadAllText(Path.Combine(RepoRoot(), "src", "Borough.Godot", file));

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md"))) at = at.Parent;
        Assert.NotNull(at);
        return at!.FullName;
    }
}
