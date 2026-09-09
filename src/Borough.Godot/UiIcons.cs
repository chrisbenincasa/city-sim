using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Borough.Shell;

internal static class UiIcons
{
    private static readonly Dictionary<(string, bool, int, bool), ImageTexture> Textures = new();
    public static string Id(Button button) => button.HasMeta("icon_id") ? (string)button.GetMeta("icon_id") : "";
    public static string Label(Button button) => button.HasMeta("icon_label") ? (string)button.GetMeta("icon_label") : button.Text;
    public static bool Filled(Button button) => Textures.TryGetValue((Id(button), true, button.GetThemeConstant("icon_max_width"), button.HasMeta("center_icon")), out var texture)
        && button.Icon == texture;

    public static ImageTexture Texture(string id, bool filled = false, int size = 24, bool centered = false)
    {
        var key = (id, filled, size, centered);
        if (Textures.TryGetValue(key, out var texture)) return texture;
        string path = $"Borough.Shell.assets.icons.{(filled ? "solid" : "outline")}.{id}.svg";
        using var stream = typeof(UiIcons).Assembly.GetManifestResourceStream(path)
            ?? throw new InvalidOperationException($"Missing icon: {path}");
        using var reader = new StreamReader(stream);
        string svg = reader.ReadToEnd();
        using var image = new Image();
        if (image.LoadSvgFromString(svg.Replace("currentColor", "white"), size / 24f) != Error.Ok)
            throw new InvalidOperationException($"Invalid icon: {path}");
        if (centered)
        {
            Rect2I ink = image.GetUsedRect();
            using var canvas = Image.CreateEmpty(image.GetWidth(), image.GetHeight(), false, Image.Format.Rgba8);
            canvas.BlitRect(image, ink, (new Vector2I(image.GetWidth(), image.GetHeight()) - ink.Size) / 2);
            texture = ImageTexture.CreateFromImage(canvas);
        }
        else texture = ImageTexture.CreateFromImage(image);
        Textures.Add(key, texture);
        return texture;
    }

    public static void Attach(Button button, string id)
    {
        if (!button.HasMeta("icon_id"))
        {
            button.ExpandIcon = false;
            button.MouseEntered += () => Refresh(button);
            button.MouseExited += () => Refresh(button);
            button.Draw += () => Refresh(button);
        }
        button.SetMeta("icon_id", id);
        Refresh(button);
    }

    public static void Glyph(Button button, string label, string id)
    {
        button.SetMeta("icon_label", label);
        button.Text = "";
        Attach(button, id);
    }

    public static void Center(Button button)
    {
        button.IconAlignment = HorizontalAlignment.Center;
        button.VerticalIconAlignment = VerticalAlignment.Center;
        button.SetMeta("center_icon", true);
        Refresh(button);
    }

    public static void Refresh(Button button)
    {
        if (Id(button) is not { Length: > 0 } id) return;
        int size = Math.Max(24, button.GetThemeFontSize("font_size") * 3 / 2);
        var texture = Texture(id, !button.Disabled && button.IsHovered(), size, centered: button.HasMeta("center_icon"));
        if (button.Icon != texture) button.Icon = texture;
        if (button.GetThemeConstant("icon_max_width") != size)
            button.AddThemeConstantOverride("icon_max_width", size);
    }

    public static void Common(Button button)
    {
        string? id = button.Text switch
        {
            "×" => "close",
            "◀◀" => "slower",
            "▶▶" => "faster",
            "⏸" => "pause",
            "▶" => "play",
            "↶" => "rotate-left",
            "↷" => "rotate-right",
            "↑" => "up",
            "↓" => "down",
            "+" => "plus",
            "−" => "minus",
            "Close" or "Cancel" => "close",
            "Tools" => "grid",
            "Menu" => "menu",
            "Settings" => "settings",
            "Resume city" => "play",
            "Help & shortcuts" or "Credits" => "info",
            "Quit game" => "quit",
            "Save city…" or "Save and continue" => "save",
            "Load city…" => "load",
            "Back to menu" => "back",
            _ => null,
        };
        if (id is null) return;
        if (button.Text is "×" or "◀◀" or "▶▶" or "⏸" or "▶" or "↶" or "↷" or "↑" or "↓" or "+" or "−")
            Glyph(button, button.Text, id);
        else Attach(button, id);
    }

    public static string Resource(string? name) => name?.ToLowerInvariant().Replace('_', '-').Replace(' ', '-') switch
    {
        "produce" => "produce",
        "food" => "food",
        "timber" => "timber",
        "materials" => "materials",
        "consumer-goods" => "consumer-goods",
        _ => "resource",
    };
}
