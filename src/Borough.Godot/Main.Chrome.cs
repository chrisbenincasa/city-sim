using System;
using System.Linq;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private PanelContainer _layerPanel = null!, _chrome = null!;
    private ScrollContainer _layerScroll = null!;
    private VBoxContainer _layerBody = null!, _layerChoices = null!;
    private Button _layerButton = null!;
    private VBoxContainer _legend = null!;
    private Label _legendTitle = null!, _legendBody = null!;
    private LegendRamp _legendRamp = null!;
    private MapRuler _ruler = null!;
    private bool _layersShown, _chromeRebuilt = true;

    private PanelContainer _legendPanel = null!, _cameraPanel = null!;
    private HBoxContainer _dataLaunchers = null!;

    private void Chrome()
    {
        _dataLaunchers = new HBoxContainer();
        _dataLaunchers.AddThemeConstantOverride("separation", 8);
        _policiesButton = ConsoleButton("Government", Govern);
        _policiesButton.ToggleMode = true;
        UiIcons.Attach(_policiesButton, "policies");
        _dataLaunchers.AddChild(_policiesButton);
        _dataLaunchers.AddChild(new VSeparator());
        _layerButton = ConsoleButton("Maps", () => Ui(_layersShown ? "layers off" : "layers on"));
        _layerButton.ToggleMode = true;
        UiIcons.Attach(_layerButton, "layers");
        _dataLaunchers.AddChild(_layerButton);
        var reservedCity = ConsoleButton("City", () => { });
        reservedCity.Disabled = true;
        reservedCity.TooltipText = "City view is not available yet";
        _dataLaunchers.AddChild(reservedCity);
        _cityButton = ConsoleButton("Evidence", () => Ui(_cityShown ? "city off" : "city on"));
        _cityButton.ToggleMode = true;
        _cityButton.TooltipText = "Everything in the city that is stopped, grouped by why";
        UiIcons.Attach(_cityButton, "trouble");
        _dataLaunchers.AddChild(_cityButton);
        var reservedPins = ConsoleButton("Pins", () => { });
        reservedPins.Disabled = true;
        reservedPins.TooltipText = "Pins view is not available yet";
        _dataLaunchers.AddChild(reservedPins);
        _pointerRow.AddChild(_dataLaunchers);
        _cameraPanel = InformationPanel();
        _cameraPanel.AddChild(CameraControls());

        _layerPanel = InformationPanel();
        var frame = new VBoxContainer();
        var heading = new HBoxContainer();
        var title = InformationLabel("Maps");
        title.AutowrapMode = TextServer.AutowrapMode.Off;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        var close = InformationButton("Close Maps", () => { });
        close.TooltipText = "Close the layer picker; keep the layer";
        close.Pressed += () => Ui("layers off");
        heading.AddChild(close);
        frame.AddChild(heading);

        _layerBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _layerChoices = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        foreach ((string name, string label) in Washes)
        {
            string want = name;
            var choice = InformationButton(label, () => { });
            choice.Pressed += () => AtBoundary(() => Apply(new DriveCommand(
                _world.Tick.Raw, DriveVerb.Overlay, 0, want)));
            choice.ToggleMode = true;
            choice.Alignment = HorizontalAlignment.Left;
            UiIcons.Attach(choice, name switch
            { "pollution" => "pollution", "value" => "value", "sealing" => "sealing", "health" => "clinic", "off" => "layers", _ => "grid" });
            _layerChoices.AddChild(choice);
        }
        _layerBody.AddChild(_layerChoices);

        _legend = new VBoxContainer();
        var legend = _legend;
        legend.AddThemeConstantOverride("separation", 4);
        _legendTitle = InformationLabel(string.Empty, CaptionPoints);
        _legendRamp = new LegendRamp
        {
            CustomMinimumSize = new Vector2(0, 7),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _legendBody = InformationLabel(string.Empty, SecondaryPoints);
        _legendBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _legendBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        legend.AddChild(new HSeparator());
        legend.AddChild(_legendTitle);
        legend.AddChild(_legendRamp);
        legend.AddChild(_legendBody);
        _legendPanel = InformationPanel();
        _legendPanel.AddChild(legend);

        _layerScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _layerScroll.AddChild(_layerBody);
        frame.AddChild(_layerScroll);
        _layerPanel.AddChild(frame);
        _layerPanel.Visible = false;

        _chrome = InformationPanel();
        var trim = new HBoxContainer();
        trim.AddThemeConstantOverride("separation", 8);
        PerformanceDisplay(trim);
        trim.AddChild(Trim("Menu", "menu", "Open the city menu", () => Ui("menu on")));
        trim.AddChild(Trim("Settings", "settings", "Interface settings",
            () => Ui(_settingsPanel.Visible ? "settings off" : "settings on")));
        _chrome.AddChild(trim);
    }

    /// <summary>
    /// A top-right trim button: <b>the icon alone, with the label kept for a driven script.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <see cref="UiIcons.Glyph"/> and not a bare <see cref="UiIcons.Attach"/>, because the
    /// readout reports a button by <see cref="UiIcons.Label"/> — so an icon-only control still
    /// answers to its name, and <c>ui press</c> keeps finding it.
    /// </remarks>
    private Button Trim(string label, string icon, string hint, Action action)
    {
        var button = InformationUi.Button(label, () => AtBoundary(action), compact: true);
        button.TooltipText = hint;
        UiIcons.Glyph(button, label, icon);
        return button;
    }

    /// <summary>
    /// The layer picker's entries. <b>The two debug washes appear only with the debug overlay.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>rung</c> and <c>age</c> are named DEBUG VIEWS by <see cref="Wash"/>'s own remarks</b>,
    /// and <c>o</c> cycled a player through both of them. They are still reachable — by the key, by
    /// a script, and here once <c>Debug</c> is on — and they are out of the everyday list.
    /// </remarks>
    private static readonly (string Name, string Label)[] Washes =
    [
        ("off", "Off"), ("pollution", "Pollution"), ("value", "Land value"), ("sealing", "Sealing"),
        ("health", "Health"), ("trouble", "Trouble"), ("rung", "Rung"), ("age", "Age"),
    ];

    private static bool DebugWash(string name) => name is "rung" or "age";

    /// <summary>The <see cref="Wash"/>'s name in the drive grammar, which is the picker's key too.</summary>
    private static string WashName(Wash wash) => wash switch
    {
        Wash.None => "off",
        Wash.Pollution => "pollution",
        Wash.Value => "value",
        Wash.Sealed => "sealing",
        Wash.Health => "health",
        Wash.Trouble => "trouble",
        Wash.Rung => "rung",
        _ => "age",
    };

    /// <summary>
    /// The picker and the openers, restated against the world.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The legend's sentence is <see cref="Legend"/>'s, split rather than rewritten.</b> A
    /// second wording of the same three facts is <c>plans/0012</c> <b>Cause 1</b> — the copy that
    /// drifts. The split is at the first em dash, which is where <see cref="Legend"/> already puts
    /// the boundary between the layer's name and what its colours are worth.
    /// </remarks>
    private void RefreshChrome()
    {
        _miniMap.Refresh(_world.Roads, _focus, _camera, MetresPerTile);
        _navigationDial.Yaw = _yaw;
        _navigationDial.QueueRedraw();
        _layerButton.SetPressedNoSignal(_layersShown);
        if (_placementRail is not null)
            foreach (Button button in _placementRail.GetChildren().OfType<Button>())
                button.SetPressedNoSignal(button.Text == "Demolish" ? _verb == Verb.Demolish : _toolsShown && button.Text == _toolCategory);
        int shown = 0;
        foreach (Node node in _layerChoices.GetChildren())
        {
            if (node is not Button choice) continue;
            bool visible = !DebugWash(Washes[shown].Name) || _debugShown;
            if (choice.Visible != visible) { choice.Visible = visible; _chromeRebuilt = true; }
            choice.ButtonPressed = Washes[shown].Name == WashName(_washing);
            shown++;
        }

        string legend = Legend().TrimStart('\n');
        bool washing = legend.Length > 0;
        if (_legend.Visible != washing) _chromeRebuilt = true;
        _legend.Visible = washing;
        _legendPanel.Visible = washing;
        _legendTitle.Visible = washing;
        _legendRamp.Visible = washing && _washing != Wash.Rung;
        _legendBody.Visible = washing;
        if (washing)
        {
            int dash = legend.IndexOf('—');
            string title = (dash < 0 ? legend : legend[..dash]).Trim().ToUpperInvariant();
            string body = dash < 0 ? string.Empty : legend[(dash + 1)..].Trim();
            if (_legendTitle.Text != title || _legendBody.Text != body) _chromeRebuilt = true;
            _legendTitle.Text = title;
            _legendBody.Text = body;
            _legendRamp.Colours = _washing == Wash.Trouble ? TroubleBands : Bands;
            _legendRamp.QueueRedraw();
        }
    }

    private float OpenersBottom(float margin) => margin + (_placementRail?.Size.Y ?? 0) + 8;

    private void LayoutChrome(Vector2 size, float margin)
    {
        if (_placementRail is not null)
        {
            _placementRail.Position = new Vector2(margin, margin);
            _placementRail.Size = _placementRail.GetCombinedMinimumSize();
        }
        _ruler.Position = new Vector2(margin, OpenersBottom(margin));
        _chrome.Size = _chrome.GetCombinedMinimumSize();
        _chrome.Position = new Vector2(size.X - _chrome.Size.X - margin, margin);
    }

    private void LayoutLayerPanel(float margin, float consoleTop, bool narrow)
    {
        if (_placementRail is null) return;
        float railRight = _placementRail.Position.X + _placementRail.Size.X + 8;
        float legendWidth = 240 * _textPercent / 100f;
        _cameraPanel.Size = _cameraPanel.GetCombinedMinimumSize();
        _cameraPanel.Position = new Vector2(margin, consoleTop - _cameraPanel.Size.Y - 8);
        float beside = Math.Max(railRight, _cameraPanel.Position.X + _cameraPanel.Size.X + 8);
        _legendPanel.Size = new Vector2(legendWidth, 0);
        _legendPanel.Position = new Vector2(beside, consoleTop - _legendPanel.Size.Y - 8);
        _ruler.Position = new Vector2(beside,
            (_legendPanel.Visible ? _legendPanel.Position.Y : consoleTop) - _ruler.GetCombinedMinimumSize().Y - 8);
        _layerPanel.Visible = _layersShown;
        if (!_layersShown) return;
        float left = railRight;
        float top = _toolsShown ? _palette.Position.Y + _palette.Size.Y + 8 : margin;
        FitPanel(_layerPanel, _layerScroll, _layerBody, left, top,
            Math.Max(240, _layerBody.GetCombinedMinimumSize().X + 32), 100,
            Math.Max(100, LeftPanelBottom(consoleTop) - top - 8), _chromeRebuilt);
        _chromeRebuilt = false;
        if (!_helpPanel.Visible && !_tuner.Visible && !_governing) _hud.MoveChild(_layerPanel, -1);
    }

    private float LeftPanelBottom(float consoleTop) => consoleTop - _ruler.GetCombinedMinimumSize().Y - 16
        - (_legendPanel.Visible ? _legendPanel.Size.Y + 8 : 0);

    private float LeftColumnEdge(float margin)
    {
        if (_placementRail is null) return margin;
        float edge = _placementRail.Position.X + _placementRail.Size.X;
        if (_toolsShown && _palette is not null) edge = Math.Max(edge, _palette.Position.X + _palette.Size.X);
        if (_layersShown) edge = Math.Max(edge, _layerPanel.Position.X + _layerPanel.Size.X);
        return edge;
    }
}
