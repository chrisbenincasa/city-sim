using System;
using Godot;

namespace Borough.Shell;

// Shared shell components: apply the theme to a screen's root, then compose cards and controls.
internal static class InformationUi
{
    public const int MetadataPoints = 14, BodyPoints = 16, TitlePoints = 22;
    public const int RowGap = 8, SectionGap = 16;
    public const int PanelInsetX = 22, PanelInsetY = 18, ConsoleInsetX = 16, ConsoleInsetY = 10;
    public const string Metadata = "InformationMetadata", WarningText = "InformationWarningText";
    private const string Card = "InformationCard", Heading = "InformationHeading";
    private const string WarningHeading = "InformationWarningHeading", LinkStyle = "InformationLink";
    private const string HeadingLabel = "InformationHeadingLabel";

    internal sealed record Palette(Color Paper, Color Surface, Color Ink, Color Muted, Color Line,
        Color Active, Color OnActive, Color Section, Color SectionInk, Color Warning, Color Warn, Color WarnInk);

    public static Palette Apply(Theme theme, bool light)
    {
        var p = light
            ? new Palette(new("f1f4f8"), new("ffffff"), new("243249"), new("5e6d82"), new("c4cfdf"),
                new("2458a3"), new("ffffff"), new("d9e6f7"), new("214e89"), new("fff0d6"), new("a36508"), new("714606"))
            : new Palette(new("202938"), new("273447"), new("eff3fb"), new("a8b8ce"), new("455976"),
                new("82b2ff"), new("172d50"), new("304968"), new("c1dafe"), new("473a25"), new("f2c06b"), new("ffe0a9"));
        theme.SetConstant("separation", "VBoxContainer", RowGap);
        theme.SetConstant("separation", "HBoxContainer", RowGap);
        foreach (string type in new[] { "Label", "Button", "CheckButton", "LineEdit" })
        {
            theme.SetColor("font_color", type, p.Ink);
            theme.SetColor("font_hover_color", type, p.Ink);
            theme.SetColor("font_focus_color", type, p.Ink);
            theme.SetColor("font_disabled_color", type, p.Muted);
        }
        theme.SetTypeVariation(Metadata, "Label");
        theme.SetColor("font_color", Metadata, p.Muted);
        theme.SetTypeVariation(WarningText, "Label");
        theme.SetColor("font_color", WarningText, p.WarnInk);
        theme.SetTypeVariation(HeadingLabel, "Label");
        theme.SetColor("font_color", HeadingLabel, p.SectionInk);
        ButtonStyles(theme, "Button", p.Surface, p.Ink, p.Active, p.OnActive, p.Line);
        theme.SetTypeVariation(Heading, "Button");
        ButtonStyles(theme, Heading, p.Section, p.SectionInk, p.Section, p.SectionInk, p.Section);
        theme.SetTypeVariation(WarningHeading, "Button");
        ButtonStyles(theme, WarningHeading, p.Warning, p.WarnInk, p.Warning, p.WarnInk, p.Warn);
        theme.SetTypeVariation(LinkStyle, "Button");
        ButtonStyles(theme, LinkStyle, p.Surface, p.Active, p.Section, p.SectionInk, p.Surface, horizontal: 0);
        theme.SetTypeVariation(Card, "PanelContainer");
        theme.SetStylebox("panel", Card, Box(p.Surface, p.Line, 1, 1));
        theme.SetStylebox("normal", "LineEdit", Box(p.Surface, p.Line, 10, 6));
        theme.SetStylebox("focus", "LineEdit", Box(Colors.Transparent, p.Active, 10, 6));
        theme.SetColor("caret_color", "LineEdit", p.Ink);
        theme.SetColor("selection_color", "LineEdit", p.Active);
        theme.SetColor("font_selected_color", "LineEdit", p.OnActive);
        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = p.Line, Thickness = 1 });
        theme.SetTypeVariation("InformationWarningPanel", "PanelContainer");
        theme.SetStylebox("panel", "InformationWarningPanel", Box(p.Warning, p.Warn, 12, 10));
        theme.SetTypeVariation("InformationHeadingPanel", "PanelContainer");
        theme.SetStylebox("panel", "InformationHeadingPanel", Box(p.Section, p.Section, 12, 10));
        return p;
    }

    private static void ButtonStyles(Theme theme, string type, Color surface, Color ink,
        Color active, Color onActive, Color line, int horizontal = 10)
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" })
        {
            Color fill = state switch
            {
                "pressed" or "hover_pressed" => active,
                "hover" => surface.Lerp(active, .12f),
                "focus" => Colors.Transparent,
                _ => surface,
            };
            theme.SetStylebox(state, type, Box(fill, state == "focus" ? active : line, horizontal, 6));
        }
        foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color" })
            theme.SetColor(state, type, ink);
        theme.SetColor("font_pressed_color", type, onActive);
        theme.SetColor("font_hover_pressed_color", type, onActive);
    }

    public static StyleBoxFlat Box(Color fill, Color line, int horizontal, int vertical) => new()
    {
        BgColor = fill, BorderColor = line,
        BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
        CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
        ContentMarginLeft = horizontal, ContentMarginRight = horizontal,
        ContentMarginTop = vertical, ContentMarginBottom = vertical,
    };

    public static VBoxContainer Stack(int gap = SectionGap)
    {
        var stack = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        stack.AddThemeConstantOverride("separation", gap);
        return stack;
    }

    public static Label Label(string text, int points = BodyPoints, bool compact = false)
    {
        var label = new Label
        {
            Text = text, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = compact ? TextServer.AutowrapMode.Off : TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = compact ? Control.SizeFlags.ShrinkCenter : Control.SizeFlags.Fill,
        };
        label.SetMeta("type_points", points);
        if (points == MetadataPoints) label.ThemeTypeVariation = Metadata;
        return label;
    }

    public static Button Button(string text, Action action, bool compact = false)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, compact ? 30 : 36),
            SizeFlagsVertical = compact ? Control.SizeFlags.ShrinkCenter : Control.SizeFlags.Fill };
        button.Pressed += action;
        return button;
    }

    public static Button Link(string text, Action action)
    {
        var link = Button(text, action);
        link.ThemeTypeVariation = LinkStyle;
        link.Alignment = HorizontalAlignment.Left;
        return link;
    }

    public static PanelContainer Section(Control header, bool expanded, bool attention, out VBoxContainer rows)
    {
        var card = new PanelContainer { ThemeTypeVariation = Card };
        var stack = Stack(0);
        card.AddChild(stack);
        if (header is Button button)
        {
            button.ThemeTypeVariation = attention ? WarningHeading : Heading;
            stack.AddChild(button);
        }
        else
        {
            header.ThemeTypeVariation = attention ? WarningText : HeadingLabel;
            var band = new PanelContainer { ThemeTypeVariation = attention ? "InformationWarningPanel" : "InformationHeadingPanel" };
            band.AddChild(header);
            stack.AddChild(band);
        }
        rows = Stack(RowGap);
        {
            var padding = new MarginContainer { Visible = expanded };
            foreach (string edge in new[] { "left", "right", "top", "bottom" })
                padding.AddThemeConstantOverride("margin_" + edge, 12);
            padding.AddChild(rows);
            stack.AddChild(padding);
        }
        return card;
    }
}
