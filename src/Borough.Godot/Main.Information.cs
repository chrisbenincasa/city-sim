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
    private PanelContainer _synopsis = null!, _debugPanel = null!, _informationBar = null!;
    private ScrollContainer _inspectionScroll = null!;
    private VBoxContainer _inspectionBody = null!;
    private Label _inspectionCondition = null!;
    private Label _inspectionTitle = null!, _inspectionIdentity = null!, _debugText = null!, _cityStatus = null!;
    private Button _inspectionBack = null!, _themeButton = null!, _debugButton = null!;
    private MeshInstance3D _selectionRing = null!, _hoverRing = null!;
    private bool _lightUi, _debugShown, _toolsShown;
    private Button _toolsButton = null!;
    private string _inspectionCaption = "closed", _inspectionSignature = string.Empty;
    private ulong _inspectionTick = ulong.MaxValue, _inspectionReadAt;
    private int _buildingScroll;
    private Handle<Building> _synopsisBuilding;
    private ulong _synopsisAt, _synopsisTick;
    private string _synopsisIssue = string.Empty;
    private readonly List<Control> _informationPanels = new();

    private sealed record InformationRow(string Text, string? Action = null);
    private sealed record InformationSection(string Key, string Title, bool Open, List<InformationRow> Rows);

    private static ulong RowId<T>(Rows<T> rows, Handle<T> handle) where T : unmanaged =>
        rows.TryResolve(handle, out int slot) ? rows.IdAt(slot) : 0;

    private void Information()
    {
        GetWindow().MinSize = new Vector2I(480, 640);
        _synopsis = InformationPanel();
        _hover.LabelSettings = null;
        _synopsis.AddChild(_hover);
        _debugPanel = InformationPanel();
        var debugScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Auto };
        _debugText = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        _debugText.AddThemeFontSizeOverride("font_size", 13);
        debugScroll.AddChild(_debugText);
        _debugPanel.AddChild(debugScroll);

        _informationBar = InformationPanel();
        var bar = new HBoxContainer();
        _cityStatus = InformationLabel(string.Empty);
        _cityStatus.MaxLinesVisible = 2;
        _cityStatus.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        _cityStatus.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bar.AddChild(_cityStatus);
        _themeButton = InformationButton("Light", () => Ui(_lightUi ? "theme dark" : "theme light"));
        _debugButton = InformationButton("Debug", () => Ui(_debugShown ? "debug off" : "debug on"));
        bar.AddChild(_themeButton);
        bar.AddChild(_debugButton);
        _toolsButton = InformationButton("Tools", () => Ui(_toolsShown ? "tools off" : "tools on"));
        bar.AddChild(_toolsButton);
        _informationBar.AddChild(bar);

        _inspector = InformationPanel();
        _inspector.Size = new Vector2(406, 600);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        var heading = new HBoxContainer();
        var identity = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inspectionBack = InformationButton("‹ Building", () => Ui("back"));
        identity.AddChild(_inspectionBack);
        _inspectionIdentity = InformationLabel(string.Empty, 12);
        _inspectionIdentity.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionIdentity.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        identity.AddChild(_inspectionIdentity);
        _inspectionTitle = InformationLabel(string.Empty, 26);
        _inspectionTitle.Size = new Vector2(300, 36);
        _inspectionTitle.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionTitle.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        identity.AddChild(_inspectionTitle);
        _inspectionCondition = InformationLabel(string.Empty, 14);
        _inspectionCondition.AutowrapMode = TextServer.AutowrapMode.Off;
        _inspectionCondition.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
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
        _inspectionBody.AddThemeConstantOverride("separation", 16);
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
        }
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

    private static Label InformationLabel(string text, int size = 16)
    {
        var label = new Label
        {
            Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static Button InformationButton(string text, Action action)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 36) };
        button.Pressed += action;
        return button;
    }

    private MeshInstance3D InformationRing(Color colour)
    {
        var marker = new MeshInstance3D
        {
            Mesh = new TorusMesh { InnerRadius = 2.5f, OuterRadius = 3.2f },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = colour, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
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
        Color paper = new(_lightUi ? "f5f2e9" : "222c30");
        Color ink = new(_lightUi ? "273632" : "f1f1e9");
        Color line = new(_lightUi ? "d4d8cd" : "435052");
        Color accent = new(_lightUi ? "31695b" : "9ed3c4");
        foreach (string type in new[] { "Label", "Button", "CheckButton", "LineEdit" })
        {
            _type.SetColor("font_color", type, ink);
            _type.SetColor("font_hover_color", type, ink);
            _type.SetColor("font_pressed_color", type, accent);
            _type.SetColor("font_focus_color", type, ink);
            _type.SetColor("font_disabled_color", type, new Color(ink, 0.5f));
        }
        foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
        {
            var box = new StyleBoxFlat
            {
                BgColor = state == "hover" || state == "pressed" ? paper.Lerp(accent, .14f) : paper,
                BorderColor = state == "focus" ? accent : line,
                BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
            };
            _type.SetStylebox(state, "Button", box);
        }
        foreach (Control panel in _informationPanels.Concat(new Control[] { _palette, _policyPanel, _tuner }))
        {
            panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = paper, BorderColor = line,
                BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1,
                CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7,
                CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7,
                ContentMarginLeft = 22, ContentMarginRight = 22,
                ContentMarginTop = 18, ContentMarginBottom = 18,
            });
        }
        _themeButton.Text = _lightUi ? "Dark" : "Light";
        _themeButton.TooltipText = "Switch interface theme";
        _debugButton.Text = _debugShown ? "Debug ✓" : "Debug";
        _debugButton.TooltipText = "Toggle technical readout (F3)";
        _debugPanel.Visible = _debugShown;
    }

    private void LayoutInformation()
    {
        if (_inspector is null) return;
        Vector2 size = GetViewport().GetVisibleRect().Size;
        float margin = size.X < 1000 || size.Y < 800 ? 10 : 24;
        bool narrow = size.X < 900;
        float width = Math.Min(406, size.X - margin * 2);
        _toolsButton.Visible = narrow;
        _palette.Visible = !narrow || _toolsShown;
        SetPanel(_informationBar, margin, margin, size.X - margin * 2, 84);
        // Existing tools remain available in a horizontal scroller on a narrow window.
        _palette.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        SetPanel(_palette, margin, size.Y - 116 - margin, size.X - margin * 2, 116);
        float hoverHeight = 142;
        float contentBottom = _palette.Visible ? _palette.Position.Y - margin : size.Y - margin;
        float hoverY = contentBottom - hoverHeight;
        SetPanel(_synopsis, margin, hoverY, Math.Min(336, size.X - margin * 2), hoverHeight);
        float bottom = narrow ? hoverY - margin : contentBottom;
        float top = narrow ? Math.Max(160, bottom - size.Y * .43f) : 96 + margin;
        SetPanel(_inspector, narrow ? margin : size.X - width - margin, top,
            narrow ? size.X - margin * 2 : width, Math.Max(90, bottom - top));
        float debugBottom = narrow && _inspector.Visible ? top - margin : hoverY - margin;
        SetPanel(_debugPanel, margin, 96 + margin,
            Math.Min(400, narrow ? Math.Min(300, size.X - margin * 2) : size.X - width - margin * 3),
            Math.Max(36, Math.Min(230, debugBottom - 96 - margin)));
    }

    private static void SetPanel(Control panel, float x, float y, float width, float height)
    {
        panel.Position = new Vector2(x, y);
        panel.Size = new Vector2(Math.Max(1, width), Math.Max(1, height));
    }

    private bool OverInformation(Vector2 at) => _hud.Visible && _informationPanels.Any(p => p.Visible && p.GetGlobalRect().HasPoint(at));

    private void Ui(string action) => Apply(new DriveCommand(_world.Tick.Raw, DriveVerb.Ui, 0, action));

    private void InformationAction(string action)
    {
        string[] words = action.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return;
        switch (words[0])
        {
            case "close" when words.Length == 1: CloseInspection(); break;
            case "back" when words.Length == 1:
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
            case "tools" when words.Length == 2 && words[1] is "on" or "off":
                _toolsShown = words[1] == "on";
                break;
            case "debug" when words.Length == 2 && words[1] is "on" or "off":
                _debugShown = words[1] == "on";
                ThemeInformation();
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
                Input.ParseInputEvent(new InputEventMouseButton { Position = mapPosition, GlobalPosition = mapPosition,
                    ButtonIndex = MouseButton.Left, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = mapPosition, GlobalPosition = mapPosition,
                    ButtonIndex = MouseButton.Left, Pressed = false });
                break;
            case "press" when words.Length == 3 && int.TryParse(words[1], out int px)
                && int.TryParse(words[2], out int py):
                var position = new Vector2(px, py);
                if (!OverInformation(position) && !(_palette.Visible && _palette.GetGlobalRect().HasPoint(position)))
                {
                    _refused = "ui press must address a visible panel.";
                    break;
                }
                Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position,
                    ButtonIndex = MouseButton.Left, Pressed = true });
                Input.ParseInputEvent(new InputEventMouseButton { Position = position, GlobalPosition = position,
                    ButtonIndex = MouseButton.Left, Pressed = false });
                break;
            case "scroll" when words.Length == 2 && int.TryParse(words[1], out int scroll):
                _inspectionScroll.ScrollVertical = Math.Max(0, scroll);
                break;
            case "size" when words.Length == 3 && int.TryParse(words[1], out int w)
                && int.TryParse(words[2], out int h) && w >= 480 && h >= 640:
                GetWindow().Mode = Window.ModeEnum.Windowed;
                GetWindow().Size = new Vector2I(w, h);
                break;
            default: _refused = "ui: use close, back, theme light|dark, debug on|off, tools on|off, building ID, road ID, frontage ID, household ID, section KEY on|off, point EAST NORTH, scroll PIXELS, read PATH, press X Y, or size WIDTH HEIGHT (minimum 480 × 640)."; break;
        }
        LayoutInformation();
    }

    private void SaveInformationPreferences()
    {
        var preferences = new ConfigFile();
        preferences.SetValue("ui", "light", _lightUi);
        preferences.SetValue("ui", "debug", _debugShown);
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
        + $"hover {_synopsis.GetGlobalRect()} debug {_debugPanel.GetGlobalRect()} tools {_palette.GetGlobalRect()}";

    private string SectionKey(string key) => $"road{RowId(_world.Roads.Segments.Rows, _selectedRoad)}:building{RowId(_world.Buildings.Rows, _selectedBuilding)}:household{RowId(_world.Households.Rows, _selectedHousehold)}:{key}";

    private void RefreshInformation()
    {
        if (_inspector is null) return;
        string status = _refused.Length > 0 ? "Action refused · hover here for details"
            : $"{_world.Citizens.Rows.LiveCount:N0} Citizens";
        _cityStatus.Text = $"Day {_world.Tick.Raw / (ulong)Ticks.PerDay} · {Pace(_rung)}\n{status}";
        _informationBar.TooltipText = _refused.Length > 0 ? _refused : Legend();
        _debugText.Text = "DEBUG · UNDER POINTER\n" + Pointing() + RoadDebug() + "\n\nCITY READOUT\n" + _readout.Text;
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
        if (Aim() is not { } at) return "UNDER POINTER\nOutside the map";
        var picked = PickInformation();
        if (_world.Roads.Segments.Rows.TryResolve(picked.Road, out int road))
        {
            MarkRoad(ref _roadHover, picked.Road, false);
            return RoadSynopsis(road);
        }
        if (!_world.Buildings.Rows.TryResolve(picked.Building, out int slot))
            return "UNDER POINTER\nOpen ground · " + _world.Layers.Terrain.At(CellGrid.ToCells(at.East), CellGrid.ToCells(at.North)).ToString().ToLowerInvariant()
                + "\nClick to inspect";
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
            _synopsisIssue = "No supply shortfalls reported";
            foreach (RuleEvidence rule in Evidence.OfBuilding(_world, handle).Rules.Span)
            {
                if (rule.Blocked != Blocking.Supply) continue;
                string resource = _names.Resource(rule.WaitingFor) ?? "a Resource";
                _synopsisIssue = $"Waiting for {resource}";
                break;
            }
        }
        return $"UNDER POINTER · Building\n{_names.Kind(_world.Buildings.Kind[slot]) ?? "Building"}\n{state}\n{_synopsisIssue}";
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
        _inspectionBack.Visible = !_selectedHousehold.IsNone || !_roadParent.IsNone;
        if (!_selectedHousehold.IsNone)
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
        }
        string signature = title + identity + string.Join("\n", sections.Select(s => s.Key + s.Title + string.Join("\n", s.Rows.Select(r => r.Text + r.Action))));
        _inspectionCaption = title + "\n" + identity + "\n" + string.Join("\n", sections.Select(s => s.Title + "\n" + string.Join("\n", s.Rows.Select(r => r.Text))));
        _inspector.Visible = true;
        if (signature == _inspectionSignature) return;
        _inspectionSignature = signature;
        int scroll = _inspectionScroll.ScrollVertical;
        _inspectionTitle.Text = title;
        _inspectionTitle.TooltipText = title;
        _inspectionIdentity.Text = identity;
        _inspectionCondition.Text = sections.FirstOrDefault(s => s.Key == "summary")?.Rows.FirstOrDefault()?.Text ?? "";
        _inspectionCondition.TooltipText = _inspectionCondition.Text;
        _inspectionBack.Text = _world.Buildings.Rows.IsValid(_selectedBuilding)
            ? $"‹ Building {RowId(_world.Buildings.Rows, _selectedBuilding)}" : "‹ Former Building";
        if (_selectedHousehold.IsNone && !_roadParent.IsNone)
            _inspectionBack.Text = $"‹ Road Segment {RowId(_world.Roads.Segments.Rows, _roadParent)}";
        foreach (Node child in _inspectionBody.GetChildren()) { _inspectionBody.RemoveChild(child); child.QueueFree(); }
        foreach (InformationSection section in sections)
        {
            if (section.Key == "summary" && section.Rows.Count == 1) continue;
            bool expanded = _expanded.GetValueOrDefault(SectionKey(section.Key), section.Open);
            var group = new VBoxContainer();
            group.AddThemeConstantOverride("separation", 12);
            var toggle = InformationButton($"{(expanded ? "⌄" : "›")}  {section.Title}",
                () => Ui($"section {section.Key} {(expanded ? "off" : "on")}"));
            toggle.Alignment = HorizontalAlignment.Left;
            group.AddChild(toggle);
            if (expanded)
                foreach (InformationRow row in section.Rows)
                {
                    if (row.Action is { } action)
                    {
                        var link = InformationButton(row.Text, () => Ui(action));
                        link.Alignment = HorizontalAlignment.Left;
                        group.AddChild(link);
                    }
                    else group.AddChild(InformationLabel(row.Text));
                }
            _inspectionBody.AddChild(group);
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
        sections.Add(Attention(evidence, default, false));
        sections.Add(Attention(evidence, default, false, true));
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
            trades.Add(new($"{_names.BusinessKind(_world.Businesses.Kind[business]) ?? "Business"} · Business {_world.Businesses.Rows.IdAt(business)}"));
        if (trades.Count > 0) sections.Add(new("businesses", $"Businesses · {businesses}", false, trades));
        var workers = new List<InformationRow>();
        foreach (Handle<Citizen> worker in evidence.Workers.Span)
            workers.Add(new($"Citizen {RowId(_world.Citizens.Rows, worker)}"));
        if (workers.Count == 0) workers.Add(new("No workers."));
        sections.Add(new("workers", $"Workers · {evidence.Workers.Length}", false, workers));
        sections.Add(Stocks(evidence, default, false));
    }

    private InformationSection Attention(BuildingEvidence evidence, Handle<Household> household, bool onlyHousehold, bool routine = false)
    {
        var rows = new List<InformationRow>();
        foreach (RuleEvidence rule in evidence.Rules.Span)
        {
            if (onlyHousehold && rule.Tenant != household) continue;
            if (routine ? rule.Blocked == Blocking.Supply : rule.Blocked != Blocking.Supply) continue;
            string owner = rule.Tenant.IsNone ? "Building activity" : $"Household {RowId(_world.Households.Rows, rule.Tenant)}";
            string resource = rule.Blocked == Blocking.Nothing ? string.Empty : _names.Resource(rule.WaitingFor) ?? $"Resource {rule.WaitingFor.Raw}";
            string target = rule.WaitingOn switch
            {
                BinOwnerKind.District => "the District market", BinOwnerKind.Household => "a Household's stock",
                BinOwnerKind.Business => "a Business's stock", BinOwnerKind.Building => "the Building's stock",
                _ => "the required stock",
            };
            string waiting = rule.Blocked switch
            {
                Blocking.Nothing => "Scheduled.",
                Blocking.Space => $"Waiting for space for {resource} in {target}.",
                _ => $"Waiting for {resource} in {target}.",
            };
            rows.Add(new($"{owner} · {_names.Rule(rule.Rule) ?? "Activity"}\n{waiting}"));
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
            rows.Add(new($"{owner} · {_names.Resource(bin.Resource) ?? "Resource"}\n{bin.Level:N0} / {bin.Capacity:N0} units"));
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
        var citizens = new List<InformationRow>();
        Money? balance = null;
        foreach (int member in _world.Members.Walk(slot))
        {
            CitizenEvidence who = Evidence.OfCitizen(_world, _world.Citizens.Rows.At(member));
            balance = who.HouseholdBalance;
            string work = who.Workplace.IsNone ? "No Workplace" : $"Workplace: Business {RowId(_world.Businesses.Rows, who.Workplace)}";
            string trip = who.Trip is null ? "No current Trip" : "On a Trip";
            citizens.Add(new($"Citizen {_world.Citizens.Rows.IdAt(member)}\n{work}\n{trip}"));
        }
        if (citizens.Count == 0) citizens.Add(new("No Citizens in this Household."));
        sections.Add(new("citizens", "Citizens", true, citizens));
        sections.Add(new("finances", "Finances", false,
            [new(balance is { } money ? $"Household balance: {money.Raw:N0} money units" : "No Household balance reported.")]));
    }
    private static IEnumerable<Node> InformationDescendants(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node next in InformationDescendants(child)) yield return next;
        }
    }

    private void WriteInformationState(string path)
    {
        RefreshInspection(true);
        static object Rect(Control control)
        {
            Rect2 rect = control.GetGlobalRect();
            return new { X = rect.Position.X, Y = rect.Position.Y, Width = rect.Size.X, Height = rect.Size.Y };
        }
        var state = new
        {
            Tick = _world.Tick.Raw, Hash = _world.HashState().ToString("X16"),
            Theme = _lightUi ? "light" : "dark", Debug = _debugShown,
            Viewport = new { Width = GetViewport().GetVisibleRect().Size.X, Height = GetViewport().GetVisibleRect().Size.Y },
            Selected = RowId(_world.Buildings.Rows, _selectedBuilding), Household = RowId(_world.Households.Rows, _selectedHousehold),
            Pointer = new { X = GetViewport().GetMousePosition().X, Y = GetViewport().GetMousePosition().Y },
            MapTargets = InformationMapTargets(),
            Road = RowId(_world.Roads.Segments.Rows, _selectedRoad),
            InspectorVisible = _inspector!.Visible, Inspector = Rect(_inspector),
            Hover = Rect(_synopsis), DebugPanel = Rect(_debugPanel), ToolsVisible = _palette.Visible, Tools = Rect(_palette),
            Scroll = _inspectionScroll.ScrollVertical, Expanded = _expanded,
            Text = _inspectionCaption, Synopsis = _hover.Text,
            Buttons = InformationDescendants(_hud).OfType<Button>().Where(b => b.IsVisibleInTree())
                .Select(b => new { b.Text, Rect = Rect(b) }).ToArray(),
        };
        System.IO.File.WriteAllText(Globalize(path), System.Text.Json.JsonSerializer.Serialize(state,
            InformationJson));
    }

}
