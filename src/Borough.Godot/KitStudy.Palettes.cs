using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Borough.Shell;

public partial class KitStudy
{
    private bool _palettes;
    private bool _paletteTower;
    private static readonly string[] PaletteNames = { "original", "warm-slate", "earth-terracotta" };
    private Dictionary<string, Dictionary<string, string>> _paletteColors = new();
    private string Palette => PaletteNames[_treatment];

    private void LoadPalettes()
    {
        string path = ProjectSettings.GlobalizePath("res://../../art/visual-study/palettes.json");
        _paletteColors = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Missing palettes");
        foreach (string name in PaletteNames)
            if (!_paletteColors.ContainsKey(name)) throw new InvalidDataException($"Missing palette: {name}");
    }

    private StandardMaterial3D PaletteMaterial(StandardMaterial3D original)
    {
        var copy = (StandardMaterial3D)original.Duplicate();
        if (Palette == "original") return copy;
        string name = System.Text.RegularExpressions.Regex.Replace(original.ResourceName, @"\.\d+$", "");
        if (!_paletteColors[Palette].TryGetValue(name, out string? hex))
            throw new InvalidDataException($"Missing palette material: {original.ResourceName}");
        copy.AlbedoColor = new Color(hex);
        return copy;
    }
}
