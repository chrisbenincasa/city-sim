using Godot;
using System.Collections.Generic;

namespace Borough.Shell;

public partial class VisualStudy
{
    private StandardMaterial3D? _masonry;
    private readonly Dictionary<string, StandardMaterial3D> _materials = new();

    private void FinishBuilding(Node3D building, int bays, int storeys, bool shop)
    {
        float width = bays * 4;
        float roof = storeys * 3.5f;
        const string trim = "bfb8a6";
        const string metal = "515759";
        Box(building, "front_coping", new Vector3(0, roof + .42f, .04f), new Vector3(width + .14f, .08f, .25f), trim);
        Box(building, "rear_coping", new Vector3(0, roof + .42f, -10), new Vector3(width + .14f, .08f, .25f), trim);
        foreach (float x in new[] { -width / 2, width / 2 })
            Box(building, "side_coping", new Vector3(x, roof + .42f, -5), new Vector3(.18f, .08f, 10), trim);
        for (float x = -width / 2 + .6f; x < width / 2; x += 1.2f)
            Box(building, "roof_seam", new Vector3(x, roof + .407f, -5), new Vector3(.035f, .014f, 9.7f), "737779");
        Box(building, "base_course", new Vector3(0, .12f, .335f), new Vector3(width, .24f, .035f), trim);
        float door = -width / 2 + 2;
        foreach (float x in new[] { door - .61f, door + .61f })
            Box(building, "door_jamb", new Vector3(x, 1.2f, .27f), new Vector3(.09f, 2.4f, .16f), trim);
        Box(building, "door_lintel", new Vector3(door, 2.42f, .28f), new Vector3(1.4f, .14f, .18f), trim);
        Box(building, "door_threshold", new Vector3(door, .025f, .3f), new Vector3(1.2f, .05f, .25f), metal);
        Box(building, "door_handle", new Vector3(door + .43f, 1.07f, .26f), new Vector3(.025f, .22f, .035f), metal);
        if (!shop) return;
        for (float x = -width / 2 + 2.5f; x <= width / 2 - .5f; x += 1.6f)
            Box(building, "shop_mullion", new Vector3(x, 1.35f, .63f), new Vector3(.055f, 2.6f, .08f), metal);
        Box(building, "shop_transom", new Vector3(1, 2.28f, .63f), new Vector3(width - 3, .055f, .08f), metal);
        Box(building, "canopy_fascia", new Vector3(0, 2.96f, 2), new Vector3(width, .16f, .035f), metal);
    }
}
