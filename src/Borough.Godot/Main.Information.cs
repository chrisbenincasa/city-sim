using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    // Shell-only preferences and generational references never enter simulation state.
    private static readonly System.Text.Json.JsonSerializerOptions InformationJson = new() { WriteIndented = true };
    private Handle<Building> _selectedBuilding;
    private Handle<Household> _selectedHousehold;
    private (Tiles East, Tiles North)? _selectedGround;
    private readonly Dictionary<string, bool> _expanded = new();
    private PanelContainer? _inspector;
    private PanelContainer _debugPanel = null!;
    private ScrollContainer _inspectionScroll = null!;
    private VBoxContainer _inspectionBody = null!;
    private TextureRect _inspectionSubjectIcon = null!;
    private Label _inspectionCondition = null!;
    private Label _inspectionTitle = null!, _inspectionIdentity = null!, _debugText = null!;
    private Button _inspectionBack = null!, _themeButton = null!, _debugButton = null!;
    private MeshInstance3D _selectionRing = null!, _hoverRing = null!;
    private bool _lightUi, _debugShown, _toolsShown;
    private string _inspectionCaption = "closed", _inspectionSignature = string.Empty;
    private ulong _inspectionTick = ulong.MaxValue, _inspectionReadAt;
    private int _buildingScroll;

    // Set where the inspector's cards are replaced and cleared by the layout that follows. See
    // FitPanel: a panel whose rows were replaced this frame has to be laid out before it is
    // measured, because nothing has given those rows a width to wrap against yet.
    private bool _inspectionRebuilt;
    private Handle<Building> _synopsisBuilding;
    private ulong _synopsisAt, _synopsisTick;
    private string _synopsisIssue = string.Empty;
    private readonly List<Control> _informationPanels = new();

    /// <summary>The smallest window the interface is designed, reviewed and checked at.</summary>
    private const int DesignWidth = 1280, DesignHeight = 800;

    private sealed record InformationRow(string Text, string? Action = null, string? Icon = null);
    private sealed record InformationSection(string Key, string Title, bool Open, List<InformationRow> Rows);

    private static ulong RowId<T>(Rows<T> rows, Handle<T> handle) where T : unmanaged =>
        rows.TryResolve(handle, out int slot) ? rows.IdAt(slot) : 0;

    private void Information()
    {
        // The window may not be dragged below the size the interface is designed and checked at
        // (plans/0064, the 2026-09-08 desktop viewport decision). Below it nothing verifies the
        // layout, and the console's own minimum already exceeds a 480 px frame.
        GetWindow().MinSize = new Vector2I(DesignWidth, DesignHeight);
        _debugPanel = InformationPanel();
        var debugScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _debugText = new Label
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        SizeLabel(_debugText, SecondaryPoints);
        debugScroll.AddChild(_debugText);
        _debugPanel.AddChild(debugScroll);

        Console();
        Chrome();

        _inspector = InformationPanel();
        _inspector.Size = new Vector2(406, 600);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        var heading = new HBoxContainer();
        var identity = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inspectionBack = InformationButton("‹ Building", () => Ui("back"));
        _inspectionBack.ClipText = true;
        identity.AddChild(_inspectionBack);
        _inspectionIdentity = InformationLabel(string.Empty, CaptionPoints);
        _inspectionIdentity.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionIdentity.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _inspectionIdentity.ClipText = true;
        identity.AddChild(_inspectionIdentity);
        _inspectionTitle = InformationLabel(string.Empty, TitlePoints);
        _inspectionTitle.Size = new Vector2(300, 36);
        _inspectionTitle.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _inspectionTitle.ClipText = true;
        var subject = new HBoxContainer();
        _inspectionSubjectIcon = ReadingIcon("housing");
        _inspectionTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        subject.AddChild(_inspectionSubjectIcon);
        subject.AddChild(_inspectionTitle);
        identity.AddChild(subject);
        _inspectionCondition = InformationLabel(string.Empty, SecondaryPoints);
        _inspectionCondition.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionCondition.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _inspectionCondition.ClipText = true;
        identity.AddChild(_inspectionCondition);
        heading.AddChild(identity);
        var close = InformationButton("×", () => Ui("close"));
        close.CustomMinimumSize = new Vector2(40, 40);
        close.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        close.TooltipText = "Close inspector (Escape)";
        heading.AddChild(close);
        column.AddChild(heading);
        _inspectionScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _inspectionBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inspectionBody.AddThemeConstantOverride("separation", InformationUi.SectionGap);
        _inspectionScroll.AddChild(_inspectionBody);
        column.AddChild(_inspectionScroll);
        _inspector.AddChild(column);
        _inspector.Visible = false;

        _selectionRing = InformationRing(new Color("9ed3c4"));
        _hoverRing = InformationRing(new Color("f5f2e9"));
        var preferences = new ConfigFile();
        if (preferences.Load("user://information.cfg") == Error.Ok)
        {
            _lightUi = (bool)preferences.GetValue("ui", "light", false);
            _debugShown = (bool)preferences.GetValue("ui", "debug", false);
            _edgeScrolling = (bool)preferences.GetValue("ui", "edge_scrolling", true);
            _textPercent = Math.Clamp((int)preferences.GetValue("ui", "text_percent", 100), 100, 150);
        }
        BuildDiscovery();
        BuildSettings();
        BuildMenu();
        Retype();
        ThemeInformation();
        LayoutInformation();
    }

    private PanelContainer InformationPanel()
    {
        var panel = new PanelContainer { Theme = _type, MouseFilter = Control.MouseFilterEnum.Stop };
        _hud.AddChild(panel);
        _informationPanels.Add(panel);
        return panel;
    }

    private Label InformationLabel(string text, int size = BodyPoints)
    {
        var label = InformationUi.Label(text, size);
        SizeLabel(label, size);
        return label;
    }

    private TextureRect ReadingIcon(string id)
    {
        var icon = new TextureRect
        {
            Texture = UiIcons.Texture(id, size: Typed(24)),
            CustomMinimumSize = new Vector2(Typed(24), Typed(24)),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = _type.GetColor("font_color", "Label")
        };
        icon.SetMeta("reading_icon", id);
        return icon;
    }

    private Button InformationButton(string text, Action action)
    {
        var button = InformationUi.Button(text, () => AtBoundary(action));
        button.TooltipText = text;
        return button;
    }

    private MeshInstance3D InformationRing(Color colour)
    {
        var marker = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 2.5f, OuterRadius = 3.2f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = colour,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false,
        };
        AddChild(marker);
        return marker;
    }

    private void ThemeInformation()
    {
        if (_inspector is null) return;
        // Publish one theme change after all colors and styles have been set.
        _type.SetBlockSignals(true);
        var colors = InformationUi.Apply(_type, _lightUi);
        Color paper = colors.Paper, ink = colors.Ink, line = colors.Line;
        _type.SetColor("icon_disabled_color", "Button", colors.Muted);
        _type.SetColor("icon_normal_color", "Button", colors.Active);
        _type.SetColor("icon_hover_color", "Button", colors.Active);
        _type.SetColor("icon_pressed_color", "Button", colors.OnActive);
        _type.SetColor("icon_hover_pressed_color", "Button", colors.OnActive);
        _type.SetBlockSignals(false);
        _type.EmitChanged();
        foreach (var icon in InformationDescendants(_hud).OfType<TextureRect>().Where(t => t.HasMeta("reading_icon")))
            icon.Modulate = ink;
        // Every panel casts a shadow. A flat panel over the city reads as a hole cut in the picture
        // rather than a thing resting above it, and weak clickability signifiers cost real reading
        // time (Nielsen Norman Group, 22% longer and 25% more fixations) — plans/0064 row 18.
        Color shadow = colors.Shadow;
        _palette?.AddThemeStyleboxOverride("panel",
            InformationUi.Box(paper, line, 16, 12, InformationUi.PanelRadius, shadow));
        foreach (Control panel in _informationPanels.Concat(new Control?[] { _policyPanel, _tuner })
            .Where(p => p is not null).Select(p => p!))
        {
            panel.AddThemeStyleboxOverride("panel", InformationUi.Box(paper, line,
                InformationUi.PanelInsetX, InformationUi.PanelInsetY, InformationUi.PanelRadius, shadow));
        }
        _console.AddThemeStyleboxOverride("panel", InformationUi.Box(paper, line,
            InformationUi.ConsoleInsetX, InformationUi.ConsoleInsetY, InformationUi.PanelRadius, shadow));

        _cameraPanel.AddThemeStyleboxOverride("panel", InformationUi.Box(paper, line, 8, 8,
            InformationUi.PanelRadius, shadow));
        _navigationDial.Colours(colors);
        _skyArc.Ink = ink;
        _skyArc.Paper = paper;
        _skyArc.QueueRedraw();

        // Refusals retain their fixed strip below the controls.
        _refusalRow.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = colors.Warning,
            BorderColor = colors.Warn,
            BorderWidthBottom = 1,
            BorderWidthLeft = 3,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            CornerRadiusBottomLeft = InformationUi.CardRadius,
            CornerRadiusBottomRight = InformationUi.CardRadius,
            CornerRadiusTopLeft = InformationUi.CardRadius,
            CornerRadiusTopRight = InformationUi.CardRadius,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        });

        _edgeScrollButton.ButtonPressed = _edgeScrolling;
        _themeButton.ButtonPressed = _lightUi;
        _themeButton.TooltipText = "Switch interface theme";
        _debugButton.ButtonPressed = _debugShown;
        _debugButton.TooltipText = "Toggle technical readout (F3)";
        _debugPanel.Visible = _debugShown;
    }

    /// <summary>
    /// Where every panel sits. <b>One console along the bottom, and the rest of the frame is the
    /// picture.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The console's height is ASKED FOR rather than stated.</b> It carries a legend only
    /// while a layer is on and a refusal only while one stands, so a constant would be wrong in
    /// three of the four states — and wrong in the direction that reserves space nothing is using.
    /// <see cref="Control.GetCombinedMinimumSize"/> is the container's own answer.
    /// </para>
    /// <para>
    /// ⚠ <b>Absolute pixels and no anchors</b>, as before: <see cref="LayoutInformation"/> runs
    /// every frame and an anchor would be a second opinion about the same rectangle.
    /// </para>
    /// </remarks>
    private void LayoutInformation()
    {
        if (_inspector is null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        float margin = size.X < 1000 || size.Y < 800 ? 10 : 24;
        bool narrow = size.X < 900;
        float width = Math.Min(406 * _textPercent / 100f, size.X - margin * 2);
        LayoutDiscovery(size, margin);
        LayoutChrome(size, margin);
        LayoutSettings(size, margin);
        LayoutMenu(size, margin);
        _toolSlot.Visible = true;
        if (_palette is not null) _palette.Visible = _toolsShown;

        // 🔴 THE CONSOLE IS THE ONE PANEL THAT SIZES ITSELF, and it has to be. Its height is its
        // content's -- options appear with a tool and feedback with an action --
        // and a height computed HERE is always one frame behind the change that caused it, which is
        // exactly the frame a driven `shoot` catches. Anchored to the bottom edge and grown upward,
        // the container answers the question itself and there is no second opinion to be stale.
        _console.AnchorLeft = 0f;
        _console.AnchorRight = 0f;
        _console.AnchorTop = 1f;
        _console.AnchorBottom = 1f;
        _console.GrowVertical = Control.GrowDirection.Begin;
        _console.OffsetLeft = margin;
        _console.OffsetRight = margin + ConsoleWidth(size.X - margin * 2);
        _console.OffsetBottom = -margin;
        _console.OffsetTop = 0f;
        float scrollbar = _consoleScroll.GetVScrollBar().GetCombinedMinimumSize().X;
        SizeConsole(Math.Max(160f, size.X - (margin * 2) - 32f - scrollbar));
        float inspectionRoom = _inspector.Visible
            ? Math.Max(_inspector.GetCombinedMinimumSize().Y + 64, 190 * _textPercent / 100f) + (narrow && _debugShown ? 160 + margin : 0) : 0;
        float consoleRoom = Math.Max(100, size.Y - margin * 3 - inspectionRoom - 20);
        float footer = _consoleBody.GetCombinedMinimumSize().Y - _consoleScroll.GetCombinedMinimumSize().Y;
        _consoleScroll.CustomMinimumSize = new Vector2(0,
            Math.Min(_consoleTop.GetCombinedMinimumSize().Y, Math.Max(40, consoleRoom - footer)));
        float consoleTop = Math.Max(margin * 2,
            size.Y - margin - Math.Max(_console.Size.Y, _console.GetCombinedMinimumSize().Y));

        if (_policyPanel is not null && _governing)
        {
            var scroll = _policyPanel.GetChildren().OfType<ScrollContainer>().First();
            var body = scroll.GetChild<Control>(0);
            float policyWidth = Math.Min(640, size.X - width - margin * 3);
            FitPanel(_policyPanel, scroll, body, margin, margin, policyWidth, 100,
                Math.Max(100, consoleTop - _cameraPanel.GetCombinedMinimumSize().Y - 8 - margin * 2), true);
        }
        LayoutToolBrowser(size, margin, consoleTop, narrow, width, OpenersBottom(margin));
        LayoutLayerPanel(margin, consoleTop, narrow);

        // The debug overlay stays independent and stays top-left, below whatever the opener column
        // is showing. On a narrow window it is the one thing above the inspector rather than beside
        // it, because there is no beside.
        bool leftOpen = _toolsShown || _layersShown;
        float debugTop = leftOpen ? margin : OpenersBottom(margin);
        float debugHeight = _debugShown
            ? Math.Min(narrow && _inspector.Visible ? 160 : 720 * _textPercent / 100f, consoleTop - debugTop - margin)
            : 0;
        float left = leftOpen ? LeftColumnEdge(margin) + margin : margin;
        SetPanel(_debugPanel, left, debugTop,
            Math.Min(840 * _textPercent / 100f,
                !narrow && _inspector.Visible ? size.X - width - left - margin * 2 : size.X - left - margin),
            Math.Max(36, debugHeight));

        float chromeBottom = margin + _chrome.Size.Y + 8;
        float top = narrow && _debugShown ? debugTop + debugHeight + margin : chromeBottom;
        FitPanel(_inspector, _inspectionScroll, _inspectionBody,
            narrow ? left : size.X - width - margin, top,
            narrow ? size.X - left - margin : width, 90, Math.Max(90, consoleTop - margin - top),
            _inspectionRebuilt);
        _inspectionRebuilt = false;
    }

    private static void SetPanel(Control panel, float x, float y, float width, float height)
    {
        panel.Position = new Vector2(x, y);
        panel.Size = new Vector2(Math.Max(1, width), Math.Max(1, height));
    }

    /// <summary>
    /// Places a scrolling panel at the height its content actually needs, never taller than the room
    /// it has. <b>A panel stretched to the frame covers the city with nothing</b> — measured at
    /// 1440 × 960 the tool browser held 325 px of content in 728 px and the inspector 519 px, so
    /// 12% of the whole frame was blank opaque panel over the picture (plans/0064 row 18).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scroll container reports a small minimum whatever it holds, so the content height is the
    /// panel's own minimum with the scroll's subtracted and the scrolled body's added back — the
    /// same arithmetic the console already uses for its footer.
    /// </para>
    /// <para>
    /// 🔴 <b>A PANEL REBUILT THIS FRAME MEASURED ITS OWN HEIGHT AS NONSENSE, AND THAT WAS THE
    /// FLICKER.</b> A wrapping <c>Label</c> answers for the width it was last laid out at, and a
    /// row no container has sorted yet has no width to answer for — so every fresh row shaped one
    /// word to a line. Traced on <c>minimal.toml</c> at 1,000 Citizens with a Building selected:
    /// the body's own minimum came back <b>2,813 px</b> against a true <b>533</b>, so
    /// <c>wanted</c> cleared <c>available</c> and the inspector was clamped to the full
    /// <b>1,625 px</b> of the frame. <see cref="RefreshInspection"/> replaces the cards up to four
    /// times a second while the clock runs, so the panel opened to the height of the window and
    /// shut again, four times a second, for as long as anybody watched it.
    /// </para>
    /// <para>
    /// ⚠ <b>So the subtree is laid out before it is measured, and <paramref name="rebuilt"/> is
    /// what says it needs to be.</b> The panel takes the room it might use, <see cref="Settle"/>
    /// walks it, and the arithmetic below then reads a width every row has actually been given.
    /// ***A measurement is taken of a layout that has happened, never of one that is queued.***
    /// </para>
    /// </remarks>
    private static void FitPanel(Control panel, Control scroll, Control body,
        float x, float y, float width, float floor, float available, bool rebuilt)
    {
        panel.Position = new Vector2(x, y);
        panel.Size = new Vector2(Math.Max(1, width), Math.Max(1, available));
        if (rebuilt) Settle(panel);
        float chrome = panel.GetCombinedMinimumSize().Y - scroll.GetCombinedMinimumSize().Y;
        float wanted = chrome + body.GetCombinedMinimumSize().Y;
        // Only the height moves from here, and no row wraps against a height, so the layout the
        // measurement was taken of is still the layout that gets drawn.
        panel.Size = new Vector2(Math.Max(1, width), Math.Clamp(wanted, Math.Min(floor, available), available));
    }

    /// <summary>
    /// Lays a panel's subtree out <b>now</b>, rather than leaving it to the end of the frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>A container sorts on a QUEUED notification, and a rebuilt inspector took TWO frames to
    /// reach its rows.</b> Adding a child queues the parent's sort; the sort hands the child a
    /// width; the child's minimum-size cache is invalidated on a second deferred call behind that.
    /// Traced: one frame after the cards were replaced the first of them still stood at
    /// <b>27 px</b> wide — its own minimum, never the body's 439 — and a frame after <em>that</em>
    /// the cached minimum was still the one shaped against no width at all.
    /// </para>
    /// <para>
    /// ⚠ <b>So both halves are forced, and the second is not redundant.</b> The notification is the
    /// one the engine's own flush would send, parent before child, so this is the same pass a frame
    /// early rather than a second opinion about it; <see cref="Control.UpdateMinimumSize"/> is then
    /// what makes a re-sorted row admit its new height, because a resize alone leaves the cache
    /// standing. ***Sorting a row and believing what it then says are two different things.***
    /// The engine's queued pass still runs afterwards and finds nothing left to do.
    /// </para>
    /// <para>
    /// ⚠ <b>Called once per rebuild rather than once per frame</b>, which is why it may walk the
    /// whole panel: it invalidates every cached minimum it touches, so a per-frame walk would
    /// re-shape every wrapped row in the panel every frame.
    /// </para>
    /// </remarks>
    private static void Settle(Node node)
    {
        if (node is Container container) container.Notification((int)Container.NotificationSortChildren);
        foreach (Node child in node.GetChildren()) Settle(child);
        if (node is Control control) control.UpdateMinimumSize();
    }

    private bool OverInformation(Vector2 at) => _hud.Visible && (
        _menuOpen || _helpShade is not null && _helpShade.Visible
        || _informationPanels.Any(p => p.Visible && p.GetGlobalRect().HasPoint(at))
        || _placementRail.GetGlobalRect().HasPoint(at)
        || _layerButton.GetGlobalRect().HasPoint(at)
        || _palette is not null && _palette.Visible && _palette.GetGlobalRect().HasPoint(at)
        || _tuner.Visible && _tuner.GetGlobalRect().HasPoint(at)
        || _policyPanel is not null && _policyPanel.Visible && _policyPanel.GetGlobalRect().HasPoint(at));

    private void Ui(string action) => AtBoundary(() => Apply(new DriveCommand(_world.Tick.Raw, DriveVerb.Ui, 0, action)));

    private void InformationAction(string action)
    {
        string[] words = action.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return;
        if (words[0] == "menu" && words.Length == 2) { MenuAction(words[1]); return; }
        if (words[0] == "file-path" && words.Length >= 2 && _cityPicker.Visible)
        {
            if (_cityPicker.FileMode == FileDialog.FileModeEnum.OpenFile)
            {
                _cityPicker.CurrentDir = System.IO.Path.GetDirectoryName(action[10..]);
                _cityPicker.CurrentFile = "";
                _cityPicker.DeselectAll();
            }
            else _cityPicker.CurrentPath = action[10..];
            return;
        }
        if (ZoningAction(words)) return;
        switch (words[0])
        {
            case "help" when words.Length == 2 && words[1] is "on" or "off":
                ShowHelp(words[1] == "on"); break;
            case "text-size" when words.Length == 2 && int.TryParse(words[1], out int percent):
                SetTextSize(percent); break;
            case "pointer" when words.Length == 4 && words[1] is "down" or "move" or "up"
                && float.TryParse(words[2], out float pointerX) && float.TryParse(words[3], out float pointerY):
                var pointerPosition = new Vector2(pointerX, pointerY);
                _aimed = null;
                if (words[1] == "move")
                    Input.ParseInputEvent(new InputEventMouseMotion
                    {
                        Position = pointerPosition,
                        GlobalPosition = pointerPosition,
                        ButtonMask = _zoneStart is null ? 0 : MouseButtonMask.Left
                    });
                else
                    Input.ParseInputEvent(new InputEventMouseButton
                    {
                        Position = pointerPosition,
                        GlobalPosition = pointerPosition,
                        ButtonIndex = MouseButton.Left,
                        Pressed = words[1] == "down"
                    });
                break;
            case "key" when words.Length >= 2 && Enum.TryParse(words[1], true, out Key key):
                Input.ParseInputEvent(new InputEventKey
                {
                    Keycode = key,
                    Unicode = (uint)key >= 32 && (uint)key <= 126 ? (uint)key : 0,
                    Pressed = true,
                    ShiftPressed = words.Length == 3 && words[2] == "shift"
                });
                Input.ParseInputEvent(new InputEventKey { Keycode = key, Pressed = false });
                break;
            case "render-probe" when words.Length == 2:
                RenderProbe(words[1]);
                break;
            case "health" when words.Length == 1:
                _selectedBusiness = default;
                _healthInspection = true;
                _selectedGround = (new Tiles(0), new Tiles(0));
                _selectedHousehold = default;
                _selectedBuilding = default;
                _selectedRoad = default;
                _roadParent = default;
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                LayoutInformation();
                break;
            case "close" when words.Length == 1: _healthInspection = false; CloseInspection(); break;
            case "business" when words.Length == 2 && ulong.TryParse(words[1], out ulong businessId):
                var business = InformationHandle(_world.Businesses.Rows, businessId);
                if (business.IsNone) break;
                if (_selectedBusiness.IsNone) _businessScroll = _inspectionScroll.ScrollVertical;
                _selectedBusiness = business;
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                RestoreInspectionScroll(0);
                break;
            case "back" when words.Length == 1:
                if (!_selectedBusiness.IsNone)
                {
                    _selectedBusiness = default;
                    _inspectionSignature = string.Empty;
                    RefreshInspection(true);
                    RestoreInspectionScroll(_businessScroll);
                    break;
                }
                if (_selectedHousehold.IsNone && !_roadParent.IsNone)
                {
                    _selectedRoad = _roadParent;
                    _roadParent = default;
                    _selectedBuilding = default;
                    _inspectionSignature = string.Empty;
                    RefreshInspection(true);
                    RestoreInspectionScroll(_roadScroll);
                    break;
                }
                _selectedHousehold = default;
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                RestoreInspectionScroll(_buildingScroll);
                break;
            case "theme" when words.Length == 2 && words[1] is "light" or "dark":
                _lightUi = words[1] == "light";
                ThemeInformation();
                SaveInformationPreferences();
                break;
            case "settings" when words.Length == 2 && words[1] is "on" or "off":
                _settingsPanel.Visible = words[1] == "on";
                if (_settingsPanel.Visible) { _zoneStart = null; _hud.MoveChild(_settingsPanel, -1); }
                break;
            case "category" when words.Length == 2 && words[1] is "Connections" or "Zoning" or "Municipal" or "Demolish":
                if (words[1] == "Demolish")
                {
                    Apply(Held("demolish", 0));
                    _toolsShown = false;
                    break;
                }
                _toolsShown = !_toolsShown || _toolCategory != words[1];
                _toolCategory = words[1];
                RefreshToolBrowser();
                break;
            case "tools" when words.Length == 2 && words[1] is "on" or "off":
                _toolsShown = words[1] == "on";
                break;
            case "layers" when words.Length == 2 && words[1] is "on" or "off":
                _layersShown = words[1] == "on";
                break;
            case "edge-scroll" when words.Length == 2 && words[1] is "on" or "off":
                _edgeScrolling = words[1] == "on";
                _edgeScrollButton.ButtonPressed = _edgeScrolling;
                SaveInformationPreferences();
                break;
            case "debug" when words.Length == 2 && words[1] is "on" or "off":
                _debugShown = words[1] == "on";
                _debugPanel.Visible = _debugShown;
                _debugButton.ButtonPressed = _debugShown;
                SaveInformationPreferences();
                break;
            case "ground" when words.Length == 3 && int.TryParse(words[1], out int groundEast)
                && int.TryParse(words[2], out int groundNorth) && groundEast >= 0 && groundNorth >= 0
                && groundEast < CellGrid.WorldTiles && groundNorth < CellGrid.WorldTiles:
                SelectInformation(default, default, (new Tiles(groundEast), new Tiles(groundNorth)));
                break;
            case "road" when words.Length == 2 && ulong.TryParse(words[1], out ulong roadId):
                var road = InformationHandle(_world.Roads.Segments.Rows, roadId);
                if (!road.IsNone) SelectInformation(default, road);
                break;
            case "building" when words.Length == 2 && ulong.TryParse(words[1], out ulong buildingId):
                var building = InformationHandle(_world.Buildings.Rows, buildingId);
                if (!building.IsNone) SelectInformation(building, default);
                break;
            case "frontage" when words.Length == 2 && ulong.TryParse(words[1], out ulong frontageId):
                var frontageBuilding = InformationHandle(_world.Buildings.Rows, frontageId);
                if (!_world.Roads.Segments.Rows.TryResolve(_selectedRoad, out int roadSlot)
                    || !_world.Buildings.Rows.TryResolve(frontageBuilding, out int buildingSlot)
                    || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[buildingSlot], out int lotSlot)) break;
                Address frontageAddress = _world.Lots.AddressOf(lotSlot);
                if (!frontageAddress.Exists || frontageAddress.Segment != roadSlot) break;
                _roadParent = _selectedRoad;
                _roadScroll = _inspectionScroll.ScrollVertical;
                _selectedRoad = default;
                _selectedBuilding = frontageBuilding;
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                RestoreInspectionScroll(0);
                break;
            case "household" when words.Length == 2 && ulong.TryParse(words[1], out ulong id):
                if (!_world.Buildings.Rows.IsValid(_selectedBuilding)) break;
                foreach (Handle<Household> household in Evidence.OfBuilding(_world, _selectedBuilding).Occupants.Span)
                {
                    if (RowId(_world.Households.Rows, household) != id) continue;
                    if (_selectedHousehold.IsNone) _buildingScroll = _inspectionScroll.ScrollVertical;
                    _selectedHousehold = household;
                    _inspectionSignature = string.Empty;
                    RefreshInspection(true);
                    RestoreInspectionScroll(0);
                    break;
                }
                break;
            case "stocks" or "finances" when words.Length == 1:
                _expanded[SectionKey(words[0])] = true;
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                Callable.From(() =>
                {
                    if (_inspectionBody.GetNodeOrNull<Control>(words[0]) is { } card)
                        _inspectionScroll.EnsureControlVisible(card);
                }).CallDeferred();
                break;
            case "section" when words.Length == 3 && words[2] is "on" or "off":
                _expanded[SectionKey(words[1])] = words[2] == "on";
                _inspectionSignature = string.Empty;
                RefreshInspection(true);
                break;
            case "point" when words.Length == 3 && int.TryParse(words[1], out int east)
                && int.TryParse(words[2], out int north) && east >= 0 && north >= 0
                && east < CellGrid.WorldTiles && north < CellGrid.WorldTiles:
                _aimed = (new Tiles(east), new Tiles(north));
                break;
            case "read" when words.Length == 2:
                WriteInformationState(words[1]);
                break;
            case "map-press" when words.Length == 3 && int.TryParse(words[1], out int mx)
                && int.TryParse(words[2], out int my):
                var mapPosition = new Vector2(mx, my);
                if (_verb != Verb.Look || OverInformation(mapPosition)
                    || _palette.Visible && _palette.GetGlobalRect().HasPoint(mapPosition)) break;
                _aimed = null;
                Input.ParseInputEvent(new InputEventMouseMotion { Position = mapPosition, GlobalPosition = mapPosition });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = mapPosition,
                    GlobalPosition = mapPosition,
                    ButtonIndex = MouseButton.Left,
                    Pressed = true
                });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = mapPosition,
                    GlobalPosition = mapPosition,
                    ButtonIndex = MouseButton.Left,
                    Pressed = false
                });
                break;
            case "press" when words.Length == 3 && int.TryParse(words[1], out int px)
                && int.TryParse(words[2], out int py):
                var position = new Vector2(px, py);
                if (!OverInformation(position) && !(_palette.Visible && _palette.GetGlobalRect().HasPoint(position)))
                {
                    _refused = "ui press must address a visible panel.";
                    break;
                }
                if (_cityPicker.Visible)
                {
                    GetViewport().PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position }, true);
                    GetViewport().PushInput(new InputEventMouseButton
                    {
                        Position = position,
                        GlobalPosition = position,
                        ButtonIndex = MouseButton.Left,
                        Pressed = true
                    }, true);
                    GetViewport().PushInput(new InputEventMouseButton
                    {
                        Position = position,
                        GlobalPosition = position,
                        ButtonIndex = MouseButton.Left,
                        Pressed = false
                    }, true);
                    break;
                }
                Input.ParseInputEvent(new InputEventMouseMotion { Position = position, GlobalPosition = position });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = position,
                    GlobalPosition = position,
                    ButtonIndex = MouseButton.Left,
                    Pressed = true
                });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = position,
                    GlobalPosition = position,
                    ButtonIndex = MouseButton.Left,
                    Pressed = false
                });
                break;
            case "wheel" when words.Length == 4 && int.TryParse(words[1], out int wx)
                && int.TryParse(words[2], out int wy) && int.TryParse(words[3], out int steps):
                var wheelPosition = new Vector2(wx, wy);
                if (!OverInformation(wheelPosition)) break;
                Input.ParseInputEvent(new InputEventMouseMotion { Position = wheelPosition, GlobalPosition = wheelPosition });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = wheelPosition,
                    GlobalPosition = wheelPosition,
                    ButtonIndex = steps < 0 ? MouseButton.WheelUp : MouseButton.WheelDown,
                    Factor = Math.Max(1, Math.Abs(Math.Clamp(steps, -20, 20))),
                    Pressed = true
                });
                Input.ParseInputEvent(new InputEventMouseButton
                {
                    Position = wheelPosition,
                    GlobalPosition = wheelPosition,
                    ButtonIndex = steps < 0 ? MouseButton.WheelUp : MouseButton.WheelDown,
                    Pressed = false
                });
                break;
            case "scroll" when words.Length == 2 && int.TryParse(words[1], out int scroll):
                _inspectionScroll.ScrollVertical = Math.Max(0, scroll);
                break;
            case "size" when words.Length == 3 && int.TryParse(words[1], out int w)
                && int.TryParse(words[2], out int h) && w >= DesignWidth && h >= DesignHeight:
                GetWindow().Mode = Window.ModeEnum.Windowed;
                GetWindow().Size = new Vector2I(w, h);
                break;
            default: _refused = "ui: use health, close, back, theme light|dark, debug on|off, tools on|off, layers on|off, building ID, road ID, frontage ID, household ID, section KEY on|off, point EAST NORTH, scroll PIXELS, read PATH, press X Y, or size WIDTH HEIGHT (minimum 1280 × 800)."; break;
        }
        LayoutInformation();
    }

    private void SaveInformationPreferences()
    {
        var preferences = new ConfigFile();
        preferences.SetValue("ui", "light", _lightUi);
        preferences.SetValue("ui", "debug", _debugShown);
        preferences.SetValue("ui", "edge_scrolling", _edgeScrolling);
        preferences.SetValue("ui", "text_percent", _textPercent);
        preferences.Save("user://information.cfg");
    }

    private void SelectPointed()
    {
        if (Aim() is not { } at) return;
        var picked = PickInformation(true);
        SelectInformation(picked.Building, picked.Road);
    }

    private void CloseInspection()
    {
        _healthInspection = false;
        _selectedBusiness = default;
        _selectedBuilding = default;
        _selectedHousehold = default;
        _selectedGround = null;
        _pickTick = ulong.MaxValue;
        _pickingFaces.Clear();
        _selectedRoad = _roadParent = default;
        if (_roadSelection is not null) _roadSelection.Visible = false;
        _synopsisBuilding = default;
        _expanded.Clear();
        _inspectionCaption = "closed";
        _inspectionSignature = string.Empty;
        if (_inspector is null) return;
        _inspector.Visible = false;
        _selectionRing.Visible = false;
        LayoutInformation();
    }

    private string InformationGeometry() => _inspector is null ? string.Empty :
        $"ui theme={(_lightUi ? "light" : "dark")} debug={_debugShown} visible={_inspector.Visible} scroll={_inspectionScroll.ScrollVertical}\n"
        + $"inspector {_inspector.GetGlobalRect()} min={_inspector.GetCombinedMinimumSize()}\n"
        + $"body {_inspectionBody.GetGlobalRect()} min={_inspectionBody.GetCombinedMinimumSize()}\n"
        + $"scroll {_inspectionScroll.GetGlobalRect()} min={_inspectionScroll.GetCombinedMinimumSize()}\n"
        + $"hover {_pointerRow.GetGlobalRect()} debug {_debugPanel.GetGlobalRect()} console {_console.GetGlobalRect()}";

    private string SectionKey(string key) => $"business{RowId(_world.Businesses.Rows, _selectedBusiness)}:road{RowId(_world.Roads.Segments.Rows, _selectedRoad)}:building{RowId(_world.Buildings.Rows, _selectedBuilding)}:household{RowId(_world.Households.Rows, _selectedHousehold)}:{key}";

    private void RefreshInformation()
    {
        if (_inspector is null) return;
        RefreshConsole();
        if (_debugShown)
            _debugText.Text = _performanceReading + "\n\nDEBUG · UNDER POINTER\n" + Pointing() + RoadDebug() + "\n\nCITY READOUT\n" + _readout.Text;
        if (_aimed is not null || !OverInformation(GetViewport().GetMousePosition()))
            _hover.Text = Synopsis();
        RefreshInspection(false);
        MarkInformation(_selectionRing, _selectedBuilding);
        if (_selectedBuilding.IsNone) _selectionRing.Visible = false;
        MarkRoad(ref _roadSelection, _selectedRoad, true);
        LayoutInformation();
    }

    private string Synopsis()
    {
        _hoverRing.Visible = false;
        if (_roadHover is not null) _roadHover.Visible = false;
        if (Aim() is not { } at) return "Outside the map";
        if (_verb == Verb.Zone)
            return _zoneStart is not null ? $"{ZoneSelectionCount()} {(_zoneParcels ? "parcels" : "blocks")} · {ZoneName()} · release to apply; Escape cancels"
                : _zoneFeedback.Length > 0 ? _zoneFeedback : $"{ZoneName()} · {(_zoneParcels ? "click a parcel or drag an area" : "drag a rectangle of whole blocks")}";
        var picked = PickInformation();
        if (_world.Roads.Segments.Rows.TryResolve(picked.Road, out int road))
        {
            MarkRoad(ref _roadHover, picked.Road, false);
            return RoadSynopsis(road);
        }
        if (!_world.Buildings.Rows.TryResolve(picked.Building, out int slot))
            return "Open ground · " + _world.Layers.Terrain.At(CellGrid.ToCells(at.East), CellGrid.ToCells(at.North)).ToString().ToLowerInvariant()
                + " — click to inspect";
        Handle<Building> handle = _world.Buildings.Rows.At(slot);
        MarkInformation(_hoverRing, handle);
        int households = _world.Occupants.Length(slot);
        int businesses = _world.BuildingBusinesses.Length(slot);
        string state = _world.Buildings.IsAbandoned(slot) ? "Abandoned"
            : $"{households} {(households == 1 ? "Household" : "Households")} · {businesses} {(businesses == 1 ? "Business" : "Businesses")}";
        ulong now = Time.GetTicksMsec();
        if (_synopsisBuilding != handle || _synopsisTick != _world.Tick.Raw && now - _synopsisAt >= 250)
        {
            _synopsisBuilding = handle;
            _synopsisAt = now;
            _synopsisTick = _world.Tick.Raw;
            _synopsisIssue = SupplySummary(Evidence.SupplyOfBuilding(_world, handle));
        }
        return $"{_names.Kind(_world.Buildings.Kind[slot]) ?? "Building"} · {state} · {_synopsisIssue}";
    }

    private void MarkInformation(MeshInstance3D marker, Handle<Building> handle)
    {
        marker.Visible = !_photographing && _world.Buildings.Rows.TryResolve(handle, out int slot)
            && _world.Lots.Rows.IsValid(_world.Buildings.Lot[slot]);
        if (!marker.Visible || !_world.Buildings.Rows.TryResolve(handle, out slot)) return;
        int lot = _world.Lots.Rows.Resolve(_world.Buildings.Lot[slot]);
        marker.Position = new Vector3(_world.Lots.East[lot].Raw * MetresPerTile, .6f, -_world.Lots.North[lot].Raw * MetresPerTile);
        marker.Scale = Vector3.One * (marker == _selectionRing ? 1.3f : 1f);
    }

    private void RefreshInspection(bool force)
    {
        if (_inspector is null || _selectedGround is null) return;
        ulong now = Time.GetTicksMsec();
        if (!force && (_inspectionTick == _world.Tick.Raw || now - _inspectionReadAt < 250)) return;
        _inspectionTick = _world.Tick.Raw;
        _inspectionReadAt = now;
        var sections = new List<InformationSection>();
        string title, identity;
        _inspectionBack.Visible = !_selectedBusiness.IsNone || !_selectedHousehold.IsNone || !_roadParent.IsNone;
        if (!_selectedBusiness.IsNone)
            BusinessInformation(sections, out title, out identity);
        else if (_healthInspection)
            HealthInformation(sections, out title, out identity);
        else if (!_selectedHousehold.IsNone)
            HouseholdInformation(sections, out title, out identity);
        else if (!_selectedRoad.IsNone)
            RoadInformation(sections, out title, out identity);
        else if (!_selectedBuilding.IsNone)
            BuildingInformation(sections, out title, out identity);
        else
        {
            var ground = _selectedGround.Value;
            title = "Open ground";
            identity = "SELECTED LOCATION";
            var rows = new List<string>();
            Underfoot(rows, CellGrid.ToCells(ground.East), CellGrid.ToCells(ground.North));
            sections.Add(new("ground", "Ground", true, rows.Select(t => new InformationRow(t)).ToList()));
            AddAreaHealth(sections, ground.East.Raw, ground.North.Raw);
        }
        string signature = title + identity + string.Join("\n", sections.Select(s => s.Key + s.Title + string.Join("\n", s.Rows.Select(r => r.Text + r.Action + r.Icon))));
        _inspectionCaption = title + "\n" + identity + "\n" + string.Join("\n", sections.Select(s => s.Title + "\n" + string.Join("\n", s.Rows.Select(r => r.Text))));
        _inspector.Visible = true;
        if (signature == _inspectionSignature) return;
        _inspectionSignature = signature;
        int scroll = _inspectionScroll.ScrollVertical;
        string subjectIcon = !_selectedBusiness.IsNone ? "trade" : !_selectedHousehold.IsNone ? "household"
            : !_selectedRoad.IsNone ? "road" : _selectedBuilding.IsNone ? "layers" : "housing";
        if (_selectedBusiness.IsNone && _selectedHousehold.IsNone
            && _world.Buildings.Rows.TryResolve(_selectedBuilding, out int iconBuilding))
            subjectIcon = _world.Rules.ServedBy(_world.Buildings.Kind[iconBuilding]) switch
            { Need.Health => "clinic", Need.Education => "school", _ => "housing" };
        _inspectionSubjectIcon.Texture = UiIcons.Texture(subjectIcon);
        _inspectionSubjectIcon.SetMeta("reading_icon", subjectIcon);
        _inspectionTitle.Text = title;
        _inspectionTitle.TooltipText = title;
        _inspectionIdentity.Text = identity;
        _inspectionCondition.Text = sections.FirstOrDefault(s => s.Key == "summary")?.Rows.FirstOrDefault()?.Text ?? "";
        _inspectionCondition.TooltipText = _inspectionCondition.Text;
        _inspectionBack.Text = _world.Buildings.Rows.IsValid(_selectedBuilding)
            ? $"‹ Building {RowId(_world.Buildings.Rows, _selectedBuilding)}" : "‹ Former Building";
        if (_selectedBusiness.IsNone && _selectedHousehold.IsNone && !_roadParent.IsNone)
            _inspectionBack.Text = $"‹ Road Segment {RowId(_world.Roads.Segments.Rows, _roadParent)}";
        if (!_selectedBusiness.IsNone && !_selectedHousehold.IsNone)
            _inspectionBack.Text = $"‹ Household {RowId(_world.Households.Rows, _selectedHousehold)}";
        _inspectionRebuilt = true;
        foreach (Node child in _inspectionBody.GetChildren()) { _inspectionBody.RemoveChild(child); child.QueueFree(); }
        foreach (InformationSection section in sections)
        {
            if (section.Key == "summary" && section.Rows.Count == 1) continue;
            bool expanded = _expanded.GetValueOrDefault(SectionKey(section.Key), section.Open);
            bool attention = section.Key == "attention" && section.Open;
            var toggle = InformationButton($"{(expanded ? "▾" : "▸")}  {section.Title}",
                () => Ui($"section {section.Key} {(expanded ? "off" : "on")}"));
            string? sectionIcon = section.Key switch
            {
                "attention" => section.Open ? "trouble" : "clear",
                "activities" => "waiting",
                "households" => "household",
                "businesses" => "trade",
                "stocks" => "resource",
                "health" or "care" => "clinic",
                "citizens" or "workers" => "household",
                _ => null,
            };
            if (sectionIcon is not null) UiIcons.Attach(toggle, sectionIcon);
            toggle.Alignment = HorizontalAlignment.Left;
            var card = InformationUi.Section(toggle, expanded, attention, out var rows);
            card.Name = section.Key;
            if (expanded)
                foreach (InformationRow row in section.Key == "summary" ? section.Rows.Skip(1) : section.Rows)
                {
                    if (row.Action is { } action)
                    {
                        var link = InformationUi.Link(row.Text, () => Ui(action));
                        string? icon = row.Icon ?? (action.StartsWith("household ") ? "household"
                            : action.StartsWith("business ") ? "trade" : action.StartsWith("building ") ? "housing" : null);
                        if (icon is not null) UiIcons.Attach(link, icon);
                        rows.AddChild(link);
                    }
                    else if (row.Icon is { } icon)
                    {
                        var reading = new HBoxContainer();
                        reading.AddChild(ReadingIcon(icon));
                        var label = InformationLabel(row.Text);
                        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        reading.AddChild(label);
                        rows.AddChild(reading);
                    }
                    else rows.AddChild(InformationLabel(row.Text));
                }
            _inspectionBody.AddChild(card);
        }
        RestoreInspectionScroll(scroll);
    }

    private void RestoreInspectionScroll(int position) => Callable.From(() => _inspectionScroll.ScrollVertical = position).CallDeferred();

    private void BuildingInformation(List<InformationSection> sections, out string title, out string identity)
    {
        identity = "SELECTED BUILDING";
        if (!_world.Buildings.Rows.TryResolve(_selectedBuilding, out int slot))
        {
            title = "Building no longer exists";
            return;
        }
        BuildingEvidence evidence = Evidence.OfBuilding(_world, _selectedBuilding);
        title = _names.Kind(evidence.Kind) ?? "Building";
        identity += $" · {_world.Buildings.Rows.IdAt(slot)}";
        int businesses = _world.BuildingBusinesses.Length(slot);
        int occupants = evidence.Occupants.Length + businesses;
        sections.Add(new("summary", "Current condition", true,
        [new(_world.Buildings.IsAbandoned(slot) ? "Abandoned" : evidence.IsDeclared ? "Occupied places: " + occupants + " / " + evidence.DeclaredOccupancy : "Kind no longer declared")]));
        sections[0] = new("summary", "Current condition", true,
            [new(SupplySummary(Evidence.SupplyOfBuilding(_world, _selectedBuilding))), sections[0].Rows[0]]);
        sections.Add(Attention(evidence, default, false));
        sections.Add(Attention(evidence, default, false, true));
        AddFacilityHealth(sections, slot);
        var households = new List<InformationRow>();
        foreach (Handle<Household> household in evidence.Occupants.Span)
        {
            ulong id = RowId(_world.Households.Rows, household);
            households.Add(new($"Household {id} →", $"household {id}"));
        }
        if (households.Count == 0) households.Add(new("No Households live here."));
        sections.Add(new("households", $"Households · {evidence.Occupants.Length}", false, households));
        var trades = new List<InformationRow>();
        foreach (int business in _world.BuildingBusinesses.Walk(slot))
            trades.Add(new($"{_names.BusinessKind(_world.Businesses.Kind[business]) ?? "Business"} · Business {_world.Businesses.Rows.IdAt(business)} →", $"business {_world.Businesses.Rows.IdAt(business)}"));
        if (trades.Count > 0) sections.Add(new("businesses", $"Businesses · {businesses}", false, trades));
        var workers = new List<InformationRow>();
        foreach (Handle<Citizen> worker in evidence.Workers.Span)
            workers.Add(new($"Citizen {RowId(_world.Citizens.Rows, worker)}"));
        if (workers.Count == 0) workers.Add(new("No workers."));
        sections.Add(new("workers", $"Workers · {evidence.Workers.Length}", false, workers));
        sections.Add(Stocks(evidence, default, false));
    }

    private InformationSection Attention(BuildingEvidence evidence, Handle<Household> household, bool onlyHousehold, bool routine = false, Handle<Business> business = default)
    {
        var rows = new List<InformationRow>();
        foreach (RuleEvidence rule in evidence.Rules.Span)
        {
            if (onlyHousehold && rule.Tenant != household) continue;
            if (!business.IsNone && rule.Business != business) continue;
            if (routine ? rule.Blocked == Blocking.Supply : rule.Blocked != Blocking.Supply) continue;
            string owner = !rule.Business.IsNone ? $"Business {RowId(_world.Businesses.Rows, rule.Business)}" : rule.Tenant.IsNone ? "Building activity" : $"Household {RowId(_world.Households.Rows, rule.Tenant)}";
            string resource = rule.Blocked == Blocking.Nothing ? string.Empty : _names.Resource(rule.WaitingFor) ?? $"Resource {rule.WaitingFor.Raw}";
            string target = rule.WaitingOn switch
            {
                BinOwnerKind.District => "the District market",
                BinOwnerKind.Household => "a Household's stock",
                BinOwnerKind.Business => "a Business's stock",
                BinOwnerKind.Building => "the Building's stock",
                _ => "the required stock",
            };
            string waiting = rule.Blocked switch
            {
                Blocking.Nothing => "Scheduled.",
                Blocking.Space => $"Waiting for space for {resource} in {target}.",
                Blocking.Supply => $"Waiting for {resource} in {target}.",
                _ => "Explanation unavailable: unrecognised activity state.",
            };
            if (rule.Blocked != Blocking.Nothing && rule.WaitingBin.IsNone)
                waiting = "Explanation unavailable: the blocking stock cannot be read.";
            rows.Add(new($"{owner} · {_names.Rule(rule.Rule) ?? "Activity"}\n{waiting}"));
            if (!rule.WaitingBin.IsNone)
                rows.Add(new(_world.Rules.IsConserved(rule.WaitingFor)
                    ? $"Available: {rule.WaitingLevel:N0} money units"
                    : $"Blocking stock: {rule.WaitingLevel:N0} / {rule.WaitingCapacity:N0} units"));
            if (!rule.WaitingBin.IsNone && !_world.Rules.IsConserved(rule.WaitingFor)
                && (rule.WaitingOn == BinOwnerKind.Building || rule.WaitingOn == BinOwnerKind.Household))
                rows.Add(new("Inspect stocks →", "stocks"));
            if (!rule.WaitingBin.IsNone && _world.Rules.IsConserved(rule.WaitingFor) && onlyHousehold)
                rows.Add(new("Inspect finances →", "finances"));
            if (!routine && business.IsNone && !rule.Business.IsNone)
                rows.Add(new($"Inspect Business {RowId(_world.Businesses.Rows, rule.Business)} →",
                    $"business {RowId(_world.Businesses.Rows, rule.Business)}"));
            if (rule.Blocked == Blocking.Supply)
                rows.Add(new($"Current shortfall since Tick {rule.StarvedSince.Raw} · {rule.MissedFirings:N0} missed firings"));
            if (!onlyHousehold && !rule.Tenant.IsNone && !routine)
            {
                ulong id = RowId(_world.Households.Rows, rule.Tenant);
                rows.Add(new($"Inspect Household {id} →", $"household {id}"));
            }
        }
        bool blocked = rows.Count > 0;
        if (!blocked) rows.Add(new(routine ? "No other activities reported." : "No supply shortfalls reported."));
        return new(routine ? "activities" : "attention", routine ? "Activities" : "What needs attention", blocked && !routine, rows);
    }

    private InformationSection Stocks(BuildingEvidence evidence, Handle<Household> household, bool onlyHousehold)
    {
        var rows = new List<InformationRow>();
        foreach (BinEvidence bin in evidence.Bins.Span)
        {
            if (onlyHousehold && bin.Tenant != household) continue;
            string owner = bin.Tenant.IsNone ? "Premises" : $"Household {RowId(_world.Households.Rows, bin.Tenant)}";
            rows.Add(new($"{owner} · {_names.Resource(bin.Resource) ?? "Resource"}\n{bin.Level:N0} / {bin.Capacity:N0} units", Icon: UiIcons.Resource(_names.Resource(bin.Resource))));
        }
        if (rows.Count == 0) rows.Add(new("No stocks reported."));
        return new("stocks", onlyHousehold ? "Household stocks" : "Stocks", false, rows);
    }

    private void HouseholdInformation(List<InformationSection> sections, out string title, out string identity)
    {
        identity = "HOUSEHOLD";
        if (!_world.Households.Rows.TryResolve(_selectedHousehold, out int slot))
        {
            title = "Household no longer exists";
            return;
        }
        title = $"Household {_world.Households.Rows.IdAt(slot)}";
        Handle<Building> home = _world.Households.Dwelling[slot];
        bool housed = _world.Buildings.Rows.IsValid(home);
        sections.Add(new("summary", "Current condition", true,
            [new(housed ? $"Home: Building {RowId(_world.Buildings.Rows, home)}" : "No current home")]));
        if (housed)
        {
            sections[0] = new("summary", "Current condition", true,
                [new(SupplySummary(Evidence.SupplyOfBuilding(_world, home, _selectedHousehold))), sections[0].Rows[0]]);
            BuildingEvidence evidence = Evidence.OfBuilding(_world, home);
            sections.Add(Attention(evidence, _selectedHousehold, true));
            sections.Add(Attention(evidence, _selectedHousehold, true, true));
            sections.Add(Stocks(evidence, _selectedHousehold, true));
        }
        if (_world.Rules.Needs.Runs)
        {
            sections.Add(new("needs", "Household Needs", false,
                [new($"Sustenance: {_world.Households.Sustenance[slot]:N0}\n0 is ideal; negative values indicate a deficit.")]));
        }
        AddHouseholdHealth(sections, slot);
        AddHouseholdSchooling(sections, slot);
        var citizens = new List<InformationRow>();
        Money? balance = null;
        foreach (int member in _world.Members.Walk(slot))
        {
            CitizenEvidence who = Evidence.OfCitizen(_world, _world.Citizens.Rows.At(member));
            balance = who.HouseholdBalance;
            string work = who.Workplace.IsNone ? "No Workplace" : $"Workplace: Business {RowId(_world.Businesses.Rows, who.Workplace)}";
            string trip = who.Trip is null ? "No current Trip" : "On a Trip";
            citizens.Add(new($"Citizen {_world.Citizens.Rows.IdAt(member)}\n{trip}"));
            citizens.Add(new(work + (who.Workplace.IsNone ? "" : " →"), who.Workplace.IsNone
                ? null : $"business {RowId(_world.Businesses.Rows, who.Workplace)}"));
        }
        if (citizens.Count == 0) citizens.Add(new("No Citizens in this Household."));
        sections.Add(new("citizens", "Citizens", true, citizens));
        sections.Add(new("finances", "Finances", false,
            [new(balance is { } money ? $"Household balance: {money.Raw:N0} money units" : "No Household balance reported.")]));
    }
    private static IEnumerable<Node> InformationDescendants(Node root, bool includeInternal = false)
    {
        foreach (Node child in root.GetChildren(includeInternal))
        {
            yield return child;
            foreach (Node next in InformationDescendants(child, includeInternal)) yield return next;
        }
    }

    private void WriteInformationState(string path)
    {
        RefreshInspection(true);
        object Rect(Control control)
        {
            Rect2 rect = control.GetGlobalRect();
            if (control.GetWindow() != GetWindow()) rect.Position += control.GetWindow().Position;
            return new { X = rect.Position.X, Y = rect.Position.Y, Width = rect.Size.X, Height = rect.Size.Y };
        }
        object ItemRect(ItemList list, int item)
        {
            Rect2 rect = list.GetItemRect(item);
            rect.Position += list.GlobalPosition;
            rect.Position -= new Vector2(0, (float)list.GetVScrollBar().Value);
            if (list.GetWindow() != GetWindow()) rect.Position += list.GetWindow().Position;
            return new { X = rect.Position.X, Y = rect.Position.Y, Width = rect.Size.X, Height = rect.Size.Y };
        }
        var state = new
        {
            Tick = _world.Tick.Raw,
            Hash = _world.HashState().ToString("X16"),
            Theme = _lightUi ? "light" : "dark",
            Debug = _debugShown,
            MenuVisible = _menuOpen,
            MenuPage = _menuPage,
            MenuMessage = _menuMessage,
            Dialogs = InformationDescendants(_hud, true).OfType<Window>().Where(w => w.Visible)
                .Select(w => new { Name = w.Name.ToString(), w.Title, X = w.Position.X, Y = w.Position.Y, Width = w.Size.X, Height = w.Size.Y, Embedded = w.IsEmbedded() }).ToArray(),
            Unsaved = UnsavedCity,
            FilePickerVisible = _cityPicker.Visible,
            SavePath = _savePath,
            SettingsVisible = _settingsPanel.Visible,
            Settings = Rect(_settingsPanel),
            TextPercent = _textPercent,
            HelpVisible = _helpPanel.Visible,
            Help = Rect(_helpPanel),
            HelpScroll = _helpScroll.ScrollVertical,
            HelpContent = Rect(_helpScroll),
            SpeedLabel = _rungLabel.Text,
            PauseHighlighted = _pauseButton.ButtonPressed,
            Sky = new { _skyArc.Minute, _skyArc.Daytime, X = _skyArc.Marker.X / _skyArc.Size.X, Y = _skyArc.Marker.Y / _skyArc.Size.Y },
            Camera = new
            {
                Yaw = _yaw,
                Pitch = _pitch,
                Distance = _distance,
                Focus = new { _focus.X, _focus.Y, _focus.Z }
            },
            Rendering = new
            {
                Probe = _renderProbe,
                ChunkMetres = InstanceLayer.ChunkMetres,
                Scale = GetViewport().Scaling3DScale,
                FrameLimit = Engine.MaxFps,
                Vsync = DisplayServer.WindowGetVsyncMode().ToString(),
                Shadow16Bits = (bool)ProjectSettings.GetSetting("rendering/lights_and_shadows/directional_shadow/16_bits"),
                Configuration = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyConfigurationAttribute>(typeof(Main).Assembly)?.Configuration
            },
            Fonts = InformationDescendants(_hud, true).OfType<Label>().Where(l => l.IsVisibleInTree())
                .Select(l => new { l.Text, Size = l.GetThemeFontSize("font_size") }).ToArray(),
            Viewport = new { Width = GetViewport().GetVisibleRect().Size.X, Height = GetViewport().GetVisibleRect().Size.Y },
            Selected = RowId(_world.Buildings.Rows, _selectedBuilding),
            Household = RowId(_world.Households.Rows, _selectedHousehold),
            Pointer = new { X = GetViewport().GetMousePosition().X, Y = GetViewport().GetMousePosition().Y },
            MapTargets = InformationMapTargets(),
            Business = RowId(_world.Businesses.Rows, _selectedBusiness),
            Road = RowId(_world.Roads.Segments.Rows, _selectedRoad),
            InspectorVisible = _inspector!.Visible,
            Inspector = Rect(_inspector),
            Hover = Rect(_pointerRow),
            DebugPanel = Rect(_debugPanel),
            ToolsVisible = _toolsShown,
            Tools = Rect(_palette),
            ToolContent = Rect(_toolScroll),
            Console = Rect(_console),
            ConsoleScroll = _consoleScroll.ScrollVertical,
            ConsoleContent = Rect(_consoleScroll),
            Pace = RungName(),
            City = new
            {
                Population = _world.Citizens.Rows.LiveCount,
                Treasury = _world.TreasuryBalance()?.Raw,
                TreasuryShown = _treasuryLabel.Visible,
            },
            Layer = _washing.ToString(),
            Zoning = new
            {
                Dragging = _zoneStart is not null,
                Blocks = _zoneParcels ? 0 : ZoneSelectionCount(),
                Parcels = _zoneParcels ? ZoneSelectionCount() : 0,
                Unit = _zoneParcels ? "parcels" : "blocks",
                Erasing = _zoneErase,
                Feedback = _zoneFeedback
            },
            Tool = _verb.ToString(),
            LayersShown = _layersShown,
            Legend = _legendTitle.Visible ? $"{_legendTitle.Text} {_legendBody.Text}" : string.Empty,
            Refused = _refusalRow.Visible ? _refusalLabel.Text : string.Empty,
            Refusal = Rect(_refusalRow),
            Layers = Rect(_layerPanel),
            LegendPanel = Rect(_legendPanel),
            LegendVisible = _legendPanel.Visible,
            PlacementRail = Rect(_placementRail),
            CameraPanel = Rect(_cameraPanel),
            MiniMap = new
            {
                Rect = Rect(_miniMap),
                _miniMap.RoadCount,
                _miniMap.Rebuilds,
                East = _miniMap.Extent.Position.X,
                North = _miniMap.Extent.Position.Y,
                Width = _miniMap.Extent.Size.X,
                Height = _miniMap.Extent.Size.Y
            },
            CameraShown = _cameraPanel.Visible,
            EdgeScrolling = _edgeScrolling,
            MiniMapDragging = _miniMap.Dragging,
            Ruler = new { X = _ruler.Position.X, Y = _ruler.Position.Y, Width = _ruler.GetCombinedMinimumSize().X, Height = _ruler.GetCombinedMinimumSize().Y },
            Trim = Rect(_chrome),
            Scroll = _inspectionScroll.ScrollVertical,
            Expanded = _expanded,
            Text = _inspectionCaption,
            Synopsis = _hover.Text,
            Inputs = InformationDescendants(_hud, true).OfType<LineEdit>().Where(f => f.IsVisibleInTree())
                .Select(f => new { f.Text, Rect = Rect(f), Focused = f.HasFocus() }).ToArray(),
            FileItems = InformationDescendants(_cityPicker, true).OfType<ItemList>().Where(l => l.IsVisibleInTree())
                .SelectMany(l => Enumerable.Range(0, l.ItemCount).Select(i => new { Text = l.GetItemText(i), Selected = l.IsSelected(i), Disabled = l.IsItemDisabled(i), Rect = ItemRect(l, i) })).ToArray(),
            TunerVisible = _tuner.Visible,
            PoliciesVisible = _governing,
            Policies = Rect(_policyPanel),
            Buttons = InformationDescendants(_hud, true).OfType<Button>().Where(b => b.IsVisibleInTree())
                .Select(b => new { Text = UiIcons.Label(b), Icon = UiIcons.Id(b), Filled = UiIcons.Filled(b), IconWidth = b.GetThemeConstant("icon_max_width"), b.Disabled, Pressed = b.ButtonPressed, Rect = Rect(b) }).ToArray(),
        };
        System.IO.File.WriteAllText(Globalize(path), System.Text.Json.JsonSerializer.Serialize(state,
            InformationJson));
    }

}
