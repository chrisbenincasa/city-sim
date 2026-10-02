using System.Text.Json;
using Borough.Appearance;

namespace Borough.Tests.Appearance;

/// <summary>Locks the C# far facade cell ids against the authored `far_cells` table in each
/// family's `bodies.json`, so the Python reference and the port cannot drift apart silently.</summary>
public sealed class FarCellTests
{
    [Fact]
    public void Midrise_far_cells_match_the_authored_table()
    {
        using JsonDocument document = MidriseBodies();
        Dictionary<string, int> expected = document.RootElement.GetProperty("far_cells").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetInt32());

        Dictionary<string, int> actual = new()
        {
            ["mansion-party"] = FamilyBodyBuilder.MidriseFarCell(true, FamilyBodyBuilder.Role.Party, 0),
            ["slab-party"] = FamilyBodyBuilder.MidriseFarCell(false, FamilyBodyBuilder.Role.Party, 0),
            ["mansion-ground"] = FamilyBodyBuilder.MidriseFarCell(true, FamilyBodyBuilder.Role.Street, 0),
            ["mansion-typical"] = FamilyBodyBuilder.MidriseFarCell(true, FamilyBodyBuilder.Role.Street, 1),
            ["mansion-attic"] = FamilyBodyBuilder.MansionAtticFarCell,
            ["mansion-end"] = FamilyBodyBuilder.MidriseFarCell(true, FamilyBodyBuilder.Role.End, 1),
            ["slab-ground"] = FamilyBodyBuilder.MidriseFarCell(false, FamilyBodyBuilder.Role.Street, 0),
            ["slab-typical"] = FamilyBodyBuilder.MidriseFarCell(false, FamilyBodyBuilder.Role.Street, 1),
            ["slab-end"] = FamilyBodyBuilder.MidriseFarCell(false, FamilyBodyBuilder.Role.End, 1),
        };

        Assert.Equal(expected.OrderBy(p => p.Key), actual.OrderBy(p => p.Key));

        // The ground band at storey 0 applies to an End wall as well, matching the near flank.
        Assert.Equal(expected["mansion-ground"], FamilyBodyBuilder.MidriseFarCell(true, FamilyBodyBuilder.Role.End, 0));
        Assert.Equal(expected["slab-ground"], FamilyBodyBuilder.MidriseFarCell(false, FamilyBodyBuilder.Role.End, 0));
    }

    [Fact]
    public void Tower_far_cells_match_the_authored_table()
    {
        using JsonDocument document = TowerBodies();
        Dictionary<string, int> expected = document.RootElement.GetProperty("far_cells").EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetInt32());

        (string Name, TowerVariant Variant)[] variants =
        [
            ("point", TowerVariant.Point),
            ("stepped-point", TowerVariant.SteppedPoint),
            ("l", TowerVariant.L),
            ("h", TowerVariant.H),
        ];

        Dictionary<string, int> actual = new() { ["blank"] = FamilyBodyBuilder.BlankFarCell, ["crown"] = FamilyBodyBuilder.CrownFarCell };
        foreach ((string name, TowerVariant variant) in variants)
        {
            actual[$"{name}-podium-ground"] = FamilyBodyBuilder.TowerCell(variant, 0);
            actual[$"{name}-podium-typical"] = FamilyBodyBuilder.TowerCell(variant, 1);
            actual[$"{name}-shaft"] = FamilyBodyBuilder.TowerCell(variant, 2);
        }

        Assert.Equal(expected.OrderBy(p => p.Key), actual.OrderBy(p => p.Key));
    }

    private static JsonDocument MidriseBodies() => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(RepoRoot(), "art", "midrise-families", "bodies.json")));

    private static JsonDocument TowerBodies() => JsonDocument.Parse(
        File.ReadAllText(Path.Combine(RepoRoot(), "art", "tall-families", "bodies.json")));

    private static string RepoRoot()
    {
        DirectoryInfo? at = new(AppContext.BaseDirectory);
        while (at is not null && !File.Exists(Path.Combine(at.FullName, "CLAUDE.md"))) at = at.Parent;
        return at?.FullName ?? throw new DirectoryNotFoundException("Could not locate repo root from test output directory.");
    }
}
