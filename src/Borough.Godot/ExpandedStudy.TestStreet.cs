using Godot;

namespace Borough.Shell;

/// <summary>
/// The procedural-buildings test street: pass 03's five bodies on its 80 x 64 m study site, and the
/// A2 alternative on its own pad east of the side street.
/// </summary>
/// <remarks>
/// Site x runs along the public street and site y runs into the block, so a site point (x, y) is
/// Godot (x, 0, -y). The public street lies at z &gt; 0 and the side street at x &gt; 80.
/// </remarks>
public partial class ExpandedStudy
{
    private static readonly (string Name, Vector3 Eye, Vector3 Target)[] TestStreetViews =
    [
        ("street", new(40, 12, 26), new(40, 5, -8)),
        ("corner", new(104, 16, 22), new(68, 5, -8)),
        ("rears", new(40, 26, -50), new(40, 3, -12)),
        ("workplaces", new(40, 32, -8), new(38, 3, -48)),
        ("receiving", new(40, 16, -82), new(40, 3, -55)),
        ("neighbourhood", new(130, 70, 60), new(40, 0, -30)),
        ("plan", new(40, 170, -30), new(40, 0, -32)),
        ("city-distance", new(230, 165, 175), new(40, 0, -30)),
        .. MatchedViews("a1", new(40, 0, -11)),
        .. MatchedViews("a2", new(120, 0, -9)),
    ];

    private static (string, Vector3, Vector3)[] MatchedViews(string body, Vector3 centre) =>
    [
        ($"{body}-front", centre + new Vector3(18, 14, 30), centre + new Vector3(0, 5, 0)),
        ($"{body}-garden", centre + new Vector3(-18, 16, -26), centre + new Vector3(0, 5, 0)),
    ];

    private void TestStreet()
    {
        void Ground(string name, float x0, float x1, float y0, float y1, string colour, float height = .12f) =>
            GroundBox(name, new Vector3((x0 + x1) / 2, height / 2 - .12f, -(y0 + y1) / 2), new Vector3(x1 - x0, height, y1 - y0), colour);
        void Body(string asset, float x, float y, float width, float depth) =>
            AddAsset(asset, new Vector3(x + width / 2, 0, -(y + depth / 2)), 0, "original");

        Ground("unbuilt site", -12, 92, -12, 76, "d6d5cc", .1f);
        Ground("near pavement", -12, 80, -3, 0, "b9b8b0", .16f);
        Ground("carriageway", -12, 92, -9, -3, "6b6e6f");
        Ground("far pavement", -12, 92, -12, -9, "b9b8b0", .16f);
        Ground("side pavement", 80, 83, -3, 76, "b9b8b0", .16f);
        Ground("side carriageway", 83, 89, -3, 76, "6b6e6f");
        Ground("side far pavement", 89, 92, -3, 76, "b9b8b0", .16f);
        Ground("service lane", 0, 83, 33, 39, "8a8c8b");
        Ground("rear apron", 0, 83, 60, 64, "8a8c8b");
        Ground("workplace forecourt", 0, 36, 39, 40, "a9a9a2", .14f);
        for (int module = 0; module < 4; module++)
            Ground("house path", module * 6 + .65f, module * 6 + 2.15f, 0, 3, "a9a9a2", .14f);
        Ground("apartment path", 38.5f, 41.5f, 0, 3, "a9a9a2", .14f);

        Body("h1-attached-range", 0, 3, 24, 12);
        Body("a1-apartment", 28, 3, 24, 16);
        Body("m1-corner", 56, 0, 24, 16);
        Body("w1-workplace", 0, 40, 36, 20);
        Body("w2-workshop", 44, 43, 32, 16);

        Ground("A2 pad", 98, 142, -12, 30, "d6d5cc", .1f);
        Ground("A2 pavement", 98, 142, -3, 0, "b9b8b0", .16f);
        Ground("A2 carriageway", 98, 142, -9, -3, "6b6e6f");
        Ground("A2 far pavement", 98, 142, -12, -9, "b9b8b0", .16f);
        Ground("A2 gardens", 100, 140, 15, 28, "c4c5b8", .11f);
        foreach (float x in new[] { 111f, 127f })
            Ground("A2 stair path", x, x + 2, 0, 3, "a9a9a2", .14f);
        Body("a2-stair-range", 104, 3, 32, 12);
    }
}
