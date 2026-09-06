using Borough.Shell;
using Godot;

namespace Borough.Renderer.Tests;

public partial class Checks
{
    private static void CheckRoofs()
    {
        // Physical edge counts for gable, short-ridge hip and paired gables.
        // Every crease belongs to two triangles; coplanar triangulation diagonals are excluded.
        int[] expected = [9, 9, 17];
        for (int family = 0; family < 3; family++)
        {
            Mesh mesh = RoofMeshes.Create(family);
            var arrays = mesh.SurfaceGetArrays(0);
            Vector2[] tags = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            Vector2[] bary = arrays[(int)Mesh.ArrayType.TexUV2].AsVector2Array();
            Require(bary.Length == tags.Length && bary.Length > 0, "roof carries triangle coordinates for overlay creases");
            int edges = 0;
            for (int i = 0; i < tags.Length; i += 3)
            {
                Require(tags[i].Y == -17 && tags[i] == tags[i+1] && tags[i] == tags[i+2], "roof edge mask constant over triangle");
                Require(bary[i] == Vector2.Right && bary[i+1] == Vector2.Down && bary[i+2] == Vector2.Zero, "roof triangle coordinates reach each corner");
                int mask = (int)tags[i].X;
                for (int bit = 0; bit < 3; bit++) edges += (mask >> bit) & 1;
            }
            Require(edges == expected[family] * 2, $"roof family {family}: every real crease, no face diagonals ({edges})");
        }
    }
}
