using System;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private string _renderProbe = "baseline";

    private void RenderProbe(string name)
    {
        if (System.Environment.GetEnvironmentVariable("BOROUGH_RENDER_PROFILE") is null)
        {
            _refused = "Rendering comparisons require BOROUGH_RENDER_PROFILE=1.";
            return;
        }
        if (name is not ("baseline" or "scale75" or "scale50" or "shadow4096" or "shadows-off"
            or "shadow-low" or "ssao-off" or "foliage-off" or "foliage3000" or "foliage4000"
            or "foliage-shadows-off" or "scale85" or "no-3d"))
        {
            _refused = "Unknown rendering comparison.";
            return;
        }
        _renderProbe = name;
        GetViewport().Disable3D = name == "no-3d";
        Input.WarpMouse(GetViewport().GetVisibleRect().Size * .5f);
        GetViewport().Scaling3DScale = name == "scale75" ? .75f : name == "scale50" ? .5f : name == "scale85" ? .85f : 1f;
        _light.ShadowEnabled = name != "shadows-off";
        RenderingServer.DirectionalShadowAtlasSetSize(name == "shadow4096" ? 4096 : 8192,
            (bool)ProjectSettings.GetSetting("rendering/lights_and_shadows/directional_shadow/16_bits"));
        RenderingServer.DirectionalSoftShadowFilterSetQuality(name == "shadow-low"
            ? RenderingServer.ShadowQuality.SoftLow : RenderingServer.ShadowQuality.SoftMedium);
        _air.SsaoEnabled = name != "ssao-off";
        _trees.Visible = name != "foliage-off";
        _rocks.Visible = name != "foliage-off";
        _trees.DetailDistance = name == "foliage3000" ? 3000f : name == "foliage4000" ? 4000f : 8000f;
        _trees.CastShadow = name == "foliage-shadows-off"
            ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On;
    }
}
