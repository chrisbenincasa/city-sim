using System;
using Godot;

namespace Borough.Shell;

// Shared shell components: apply the theme to a screen's root, then compose cards and controls.
internal static class InformationUi
{
    public const int MetadataPoints = 13, BodyPoints = 16, TitlePoints = 24;
    public const int RowGap = 8, SectionGap = 16;
    public const int PanelInsetX = 20, PanelInsetY = 16, ConsoleInsetX = 16, ConsoleInsetY = 12;

    // Nested radii step down by the padding between them; equal radii leave an uneven gap between
    // the two curves. A control inside a card inside a panel is 4 inside 6 inside 10.
    public const int PanelRadius = 10, CardRadius = 6, ControlRadius = 4;
    public const int PanelShadow = 14;

    public const string Metadata = "InformationMetadata", WarningText = "InformationWarningText";
    public const string Reading = "InformationReading";
    private const string Card = "InformationCard", Heading = "InformationHeading";
    private const string WarningHeading = "InformationWarningHeading", LinkStyle = "InformationLink";
    private const string HeadingLabel = "InformationHeadingLabel";

    internal sealed record Palette(Color Paper, Color Surface, Color Ink, Color Muted, Color Line,
        Color Active, Color OnActive, Color Section, Color SectionInk, Color Warning, Color Warn, Color WarnInk,
        Color Shadow);

    public static Palette Apply(Theme theme, bool light)
    {
        var p = light
            ? new Palette(new("f1f4f8"), new("ffffff"), new("243249"), new("5e6d82"), new("c4cfdf"),
                new("2458a3"), new("ffffff"), new("d9e6f7"), new("214e89"), new("fff0d6"), new("a36508"), new("714606"),
                new(0.06f, 0.11f, 0.20f, 0.20f))
            : new Palette(new("202938"), new("273447"), new("eff3fb"), new("a8b8ce"), new("455976"),
                new("82b2ff"), new("172d50"), new("304968"), new("c1dafe"), new("473a25"), new("f2c06b"), new("ffe0a9"),
                new(0f, 0f, 0f, 0.45f));
        theme.SetTypeVariation(Reading, "Label");
        theme.SetFont("font", Reading, TabularFont());
        theme.SetColor("font_color", Reading, p.Muted);
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

        // A section heading sits inside a card that already has a border, so it draws neither its
        // own border nor its own fill: an accent carried by every section marks none of them, and
        // the amber attention band is left as the one filled heading in the inspector.
        theme.SetTypeVariation("SupplyMark", "Button");
        ButtonStyles(theme, "SupplyMark", p.Warning, p.WarnInk, p.Warn, p.WarnInk, p.Warn,
            horizontal: 4, vertical: 2, radius: 18);
        theme.SetTypeVariation(Heading, "Button");
        ButtonStyles(theme, Heading, p.Surface, p.SectionInk, p.Section, p.SectionInk, p.Surface);
        theme.SetTypeVariation(WarningHeading, "Button");
        ButtonStyles(theme, WarningHeading, p.Warning, p.WarnInk, p.Warning, p.WarnInk, p.Warn);
        theme.SetTypeVariation(LinkStyle, "Button");
        ButtonStyles(theme, LinkStyle, p.Surface, p.Active, p.Section, p.SectionInk, p.Surface, horizontal: 0);
        theme.SetTypeVariation(Card, "PanelContainer");
        theme.SetStylebox("panel", Card, Box(p.Surface, p.Line, 1, 1, CardRadius));
        theme.SetStylebox("normal", "LineEdit", Box(p.Surface, p.Line, 12, 8));
        theme.SetStylebox("focus", "LineEdit", Box(Colors.Transparent, p.Active, 12, 8));
        theme.SetColor("caret_color", "LineEdit", p.Ink);
        theme.SetColor("selection_color", "LineEdit", p.Active);
        theme.SetColor("font_selected_color", "LineEdit", p.OnActive);
        theme.SetStylebox("panel", "AcceptDialog", Box(p.Paper, p.Line, 16, 16, PanelRadius));
        theme.SetStylebox("panel", "ItemList", Box(p.Surface, p.Line, 4, 4));
        theme.SetColor("font_color", "ItemList", p.Ink);
        theme.SetColor("font_selected_color", "ItemList", p.OnActive);
        theme.SetStylebox("selected", "ItemList", Box(p.Active, p.Active, 0, 0));
        theme.SetStylebox("selected_focus", "ItemList", Box(p.Active, p.Active, 0, 0));
        theme.SetColor("title_color", "Window", p.Ink);
        theme.SetIcon("close", "Window", CloseIcon(p.Ink));
        theme.SetIcon("close_pressed", "Window", CloseIcon(p.Active));
        theme.SetColor("file_icon_color", "FileDialog", p.Ink);
        theme.SetColor("folder_icon_color", "FileDialog", p.Muted);
        var frame = Box(p.Paper, p.Line, 8, 8, PanelRadius, p.Shadow);
        frame.ExpandMarginTop = 32;
        theme.SetStylebox("embedded_border", "Window", frame);
        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = p.Line, Thickness = 1 });
        theme.SetTypeVariation("InformationWarningPanel", "PanelContainer");
        theme.SetStylebox("panel", "InformationWarningPanel", Box(p.Warning, p.Warn, 12, 10, CardRadius));
        theme.SetTypeVariation("InformationHeadingPanel", "PanelContainer");
        theme.SetStylebox("panel", "InformationHeadingPanel", Box(p.Surface, p.Surface, 12, 10, CardRadius));
        return p;
    }

    private static ImageTexture CloseIcon(Color ink)
    {
        using var image = new Image();
        image.LoadSvgFromString($"<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16'><path d='M4 4L12 12M12 4L4 12' stroke='#{ink.ToHtml(false)}' stroke-width='2' stroke-linecap='round'/></svg>");
        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>
    /// The face used for readings that change while the player watches. Proportional digits have
    /// different widths, so a clock or a frame counter shifts the text beside it every time a digit
    /// turns over; <c>tnum</c> gives every digit one advance width and the row stops moving.
    /// </summary>
    private static FontVariation TabularFont()
    {
        var features = new Godot.Collections.Dictionary
        {
            { TextServerManager.GetPrimaryInterface().NameToTag("tnum"), 1 },
        };
        return new FontVariation { BaseFont = ThemeDB.FallbackFont, OpentypeFeatures = features };
    }

    private static void ButtonStyles(Theme theme, string type, Color surface, Color ink,
        Color active, Color onActive, Color line, int horizontal = 10, int vertical = 8, int radius = 4)
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
            theme.SetStylebox(state, type, Box(fill, state == "focus" ? active : line, horizontal, vertical, radius));
        }
        foreach (string state in new[] { "font_color", "font_hover_color", "font_focus_color" })
            theme.SetColor(state, type, ink);
        theme.SetColor("font_pressed_color", type, onActive);
        theme.SetColor("font_hover_pressed_color", type, onActive);
        theme.SetColor("icon_normal_color", type, ink);
        theme.SetColor("icon_hover_color", type, ink);
        theme.SetColor("icon_pressed_color", type, onActive);
        theme.SetColor("icon_hover_pressed_color", type, onActive);
        theme.SetColor("icon_disabled_color", type, ink.Darkened(.35f));
    }

    public static StyleBoxFlat Box(Color fill, Color line, int horizontal, int vertical,
        int radius = ControlRadius, Color? shadow = null) => new()
    {
        BgColor = fill, BorderColor = line,
        BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        ContentMarginLeft = horizontal, ContentMarginRight = horizontal,
        ContentMarginTop = vertical, ContentMarginBottom = vertical,
        ShadowSize = shadow is null ? 0 : PanelShadow,
        ShadowColor = shadow ?? Colors.Transparent,
        ShadowOffset = shadow is null ? Vector2.Zero : new Vector2(0, PanelShadow / 3f),
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
        var button = new Button { Text = text, TooltipText = text, CustomMinimumSize = new Vector2(0, compact ? 30 : 36),
            SizeFlagsVertical = compact ? Control.SizeFlags.ShrinkCenter : Control.SizeFlags.Fill };
        UiIcons.Common(button);
        button.Pressed += action;
        return button;
    }

    public static Button Link(string text, Action action)
    {
        var link = Button(text, action);
        link.ClipText = true;
        link.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
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
            button.ClipText = true;
            button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
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
