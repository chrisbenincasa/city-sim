using System.Numerics;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

public class ShellBuilderTests
{
    [Theory]
    [InlineData(RoofForm.Flat)]
    [InlineData(RoofForm.Parapet)]
    [InlineData(RoofForm.Gable)]
    [InlineData(RoofForm.Hip)]
    public void Every_triangle_is_wound_clockwise_about_its_normal(RoofForm roof)
    {
        var mesh = new ShellMesh();
        ShellBuilder.Append(mesh, new WingShape(14f, 9f, 3, 0, roof, 77), Frame.At(new Vector3(40f, 0f, -12f), 1.1f));

        ReadOnlySpan<Vector3> positions = mesh.Positions;
        ReadOnlySpan<Vector3> normals = mesh.Normals;
        ReadOnlySpan<int> indices = mesh.Indices;
        Assert.Equal(0, indices.Length % 3);

        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 a = positions[indices[i]];
            Vector3 winding = Vector3.Cross(positions[indices[i + 1]] - a, positions[indices[i + 2]] - a);
            Assert.True(Vector3.Dot(winding, normals[indices[i]]) < 0f, $"triangle {i / 3} faces away from its normal");
        }
    }

    [Fact]
    public void A_one_bay_flat_wing_is_forty_nine_quads()
    {
        var mesh = new ShellMesh();
        ShellBuilder.Append(mesh, new WingShape(3.5f, 3.5f, 1, -1, RoofForm.Flat, 0), Frame.Identity);

        // Four faces of one bay: four wall strips, four reveals and a pane each. Twelve cornice
        // quads, and the deck.
        Assert.Equal(49 * 4, mesh.VertexCount);
        Assert.Equal(49 * 6, mesh.IndexCount);
    }
}
