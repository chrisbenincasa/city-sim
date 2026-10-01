using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Appearance;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private sealed record FarWindowFactoryInput(
        string FamilyId,
        string BodyVariant,
        IReadOnlyDictionary<string, Color> PaintSrgb,
        IReadOnlyList<FamilyBodyFarCell> CellTable);

    private delegate Material FarWindowFactory(FarWindowFactoryInput input);

    // Add one registry line for each far-window option. Its factory and assets live in their own files.
    private static readonly (string Name, FarWindowFactory Factory)[] FarWindowRegistry =
    [
        ("grid", FarWindowsGrid),
    ];

    private string _farWindowName = "grid";

    private Material FarWindowMaterial(FarWindowFactoryInput input) =>
        FarWindowRegistry.Single(entry => entry.Name == _farWindowName).Factory(input);

    private void FarWindowsStudy(string[] words)
    {
        if (words.Length != 2 || !FarWindowRegistry.Any(entry => entry.Name == words[1]))
        {
            _refused = $"far-windows {string.Join('|', FarWindowRegistry.Select(entry => entry.Name))}";
            return;
        }

        if (_farWindowName == words[1]) return;
        _farWindowName = words[1];
        foreach (PlacedBody placed in _placedBodies.Values)
        {
            placed.NearLayer.Multimesh.Replace(placed.Id, []);
            placed.FarLayer?.Multimesh.Replace(placed.Id, []);
        }

        _placedBodies.Clear();
        _bodyNeighbours.Clear();
        foreach (InstanceLayer layer in _bodyLayers.Values) layer.QueueFree();
        _bodyLayers.Clear();
        _familyBodyMeshes.Clear();
        _world.Changes!.Invalidate();
    }
}
