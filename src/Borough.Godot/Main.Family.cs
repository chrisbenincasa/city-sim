using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Borough.Appearance;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private const string DefaultStylePreset = "appearance/test-street";
    private const string TextureLibraryDirectory = "res://assets/city/library";
    private const string TextureLibraryIndex = TextureLibraryDirectory + "/materials.json";

    private static readonly Color FallbackFamily = new(0.55f, 0.55f, 0.55f);
    private static readonly Color MissingFamily = new(1f, 0f, 1f);

    private static readonly Color[] FamilyHues =
    [
        new(0.90f, 0.34f, 0.24f),
        new(0.28f, 0.55f, 0.72f),
        new(0.88f, 0.74f, 0.28f),
        new(0.42f, 0.62f, 0.35f),
        new(0.82f, 0.42f, 0.62f),
        new(0.36f, 0.80f, 0.78f),
        new(0.96f, 0.60f, 0.20f),
        new(0.58f, 0.44f, 0.86f),
    ];

    private static readonly string[] FamilyHueNames = ["red", "blue", "yellow", "green", "pink", "teal", "orange", "violet"];

    private StylePreset? _stylePreset;
    private string? _stylePresetRefusal;

    /// <summary>The Style Preset the family wash draws with, from <c>--appearance DIR</c>.</summary>
    /// <remarks>Read on first use, so a city that never shows the wash never reads the files.</remarks>
    private StylePreset? Preset()
    {
        if (_stylePreset is not null || _stylePresetRefusal is not null) return _stylePreset;

        string[] given = OS.GetCmdlineUserArgs();
        int at = Array.IndexOf(given, "--appearance");
        string directory = Globalize(at >= 0 && at + 1 < given.Length ? given[at + 1] : DefaultStylePreset);

        StylePresetResult read = StylePresetReader.Read(directory);
        if (read.Preset is not null) read = Library().Dress(read.Preset);
        if (read.Preset is null)
        {
            _stylePresetRefusal = string.Join("; ", read.Errors.Select(e => e.ToString()));
            GD.PrintErr($"appearance preset refused: {_stylePresetRefusal}");
        }

        _stylePreset = read.Preset;
        return _stylePreset;
    }

    private FamilyPick FamilyOf(int slot)
    {
        StylePreset? preset = Preset();
        if (preset is null || !BuildingFacts.TryOf(_world, _names, slot, out BuildingFacts facts))
        {
            return new FamilyPick(null, FamilyChoice.Missing);
        }

        return FamilyPicker.Pick(preset, _world.Key, facts);
    }

    private Color FamilyColour(int slot)
    {
        FamilyPick pick = FamilyOf(slot);
        return pick.Choice switch
        {
            FamilyChoice.Fallback => FallbackFamily,
            FamilyChoice.Missing => MissingFamily,
            _ => FamilyHues[FamilyIndex(pick.Family!) % FamilyHues.Length],
        };
    }

    private int FamilyIndex(AppearanceFamily family) =>
        _stylePreset!.Families.Where(f => !f.Fallback).ToList().IndexOf(family);

    private string FamilyLegend()
    {
        StylePreset? preset = Preset();
        if (preset is null)
        {
            return $"\nOVERLAY family — DEBUG. The Style Preset was refused: {_stylePresetRefusal}";
        }

        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        for (int slot = 0; slot < _world.Buildings.Rows.SlotCount; slot++)
        {
            if (!_world.Buildings.Rows.IsLive(slot)) continue;
            FamilyPick pick = FamilyOf(slot);
            string name = pick.Choice switch
            {
                FamilyChoice.Missing => "(missing, magenta)",
                FamilyChoice.Fallback => $"{pick.Family!.Id} (fallback, grey)",
                _ => $"{pick.Family!.Id} ({FamilyHueNames[FamilyIndex(pick.Family) % FamilyHueNames.Length]})",
            };
            counts[name] = counts.GetValueOrDefault(name) + 1;
        }

        string drawn = string.Join(", ", counts.Select(c => string.Create(CultureInfo.InvariantCulture, $"{c.Key} {c.Value:N0}")));
        return $"\nOVERLAY family — DEBUG. The Appearance Family each Building draws under preset "
            + $"{preset.Name}; one hue per family. Drawn: {drawn}";
    }
}
