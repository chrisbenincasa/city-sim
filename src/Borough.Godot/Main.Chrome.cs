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

    /// <summary>The height of the opener column, and where the panels below it begin.</summary>
    private const float OpenerHeight = 40f, OpenerWidth = 110f, OpenerGap = 8f;

    /// <summary>
    /// The two openers at the top left, the layer picker they open, and the top-right trim.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A layer is a thing you are looking at, not a thing you are doing</b>, and the picker sat
    /// in the bottom console beside the tools because that is where the fold put it. It opens the
    /// same way the tool browser does now: an opener on the left edge, a panel beneath it, and the
    /// console keeps only what a person watches while the city runs.
    /// </para>
    /// <para>
    /// ⚠ <b>The openers stay visible while their panels are open</b>, unlike the Tools button
    /// before them. Two openers stacked cannot take turns hiding — the lower one would move every
    /// time the upper one's panel appeared, and a control that moves when you open something else
    /// is a control you have to look for.
    /// </para>
    /// </remarks>
    private void Chrome()
    {
        _toolsButton = Opener("grid", "Tools", "Open and close the tool browser",
            () => Ui(_toolsShown ? "tools off" : "tools on"));
        _layerButton = Opener("layers", "Layers", "Choose a map layer (o cycles)",
            () => Ui(_layersShown ? "layers off" : "layers on"));

        _layerPanel = InformationPanel();
        var frame = new VBoxContainer();
        var heading = new HBoxContainer();
        var title = InformationLabel("Layers");
        title.AutowrapMode = TextServer.AutowrapMode.Off;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        var close = InformationButton("Close", () => { });
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
        _layerBody.AddChild(legend);

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

    /// <summary>An opener on the left edge: a labelled button that toggles the panel beneath it.</summary>
    private Button Opener(string icon, string label, string hint, Action action)
    {
        var button = InformationUi.Button(label, () => AtBoundary(action), compact: true);
        button.Theme = _type;
        button.ToggleMode = true;
        button.TooltipText = hint;
        UiIcons.Attach(button, icon);
        _hud.AddChild(button);
        return button;
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
        ("health", "Health"), ("rung", "Rung"), ("age", "Age"),
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
        _toolsButton.SetPressedNoSignal(_toolsShown);
        _layerButton.SetPressedNoSignal(_layersShown);
        _layerButton.Text = _washing == Wash.None
            ? "Layers"
            : Washes.First(w => w.Name == WashName(_washing)).Label;

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
            _legendRamp.Colours = Bands;
            _legendRamp.QueueRedraw();
        }
    }

    /// <summary>Where the left column's panels begin, which is under both openers.</summary>
    private static float OpenersBottom(float margin) => margin + (OpenerHeight + OpenerGap) * 2;

    /// <summary>
    /// The openers on the left edge, the scale bar under them, and the trim in the top-right.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Both openers take the wider one's width</b>, and it is not tidiness. The layer opener
    /// wears the layer's own name once one is on — <em>Land value</em> is wider than <em>Layers</em>
    /// — so a stated width either clips the label or leaves the column ragged as the wash changes.
    /// </remarks>
    private void LayoutChrome(Vector2 size, float margin)
    {
        float width = Math.Max(OpenerWidth,
            Math.Max(_toolsButton.GetCombinedMinimumSize().X, _layerButton.GetCombinedMinimumSize().X));
        _toolsButton.Position = new Vector2(margin, margin);
        _toolsButton.Size = new Vector2(width, OpenerHeight);
        _layerButton.Position = new Vector2(margin, margin + OpenerHeight + OpenerGap);
        _layerButton.Size = new Vector2(width, OpenerHeight);
        _ruler.Position = new Vector2(margin, OpenersBottom(margin));
        _chrome.Size = _chrome.GetCombinedMinimumSize();
        _chrome.Position = new Vector2(size.X - _chrome.Size.X - margin, margin);
    }

    /// <summary>
    /// The layer picker, <b>under the tool browser when both are open</b> rather than over it.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Laid out after the tool browser</b>, because where it starts is where the browser
    /// ended and the browser's height is its content's. Two openers that can both be pressed are
    /// two panels that have to agree about one column.
    /// </remarks>
    private void LayoutLayerPanel(float margin, float consoleTop, bool narrow)
    {
        _layerPanel.Visible = _layersShown;
        if (!_layersShown) return;
        float width = narrow ? 176 : Math.Min(240, 190 * _textPercent / 100f);
        float top = _toolsShown && _palette is not null
            ? _palette.Position.Y + _palette.Size.Y + OpenerGap
            : OpenersBottom(margin);
        FitPanel(_layerPanel, _layerScroll, _layerBody, margin, top, width,
            100, Math.Max(100, consoleTop - top - margin), _chromeRebuilt);
        _chromeRebuilt = false;
        if (!_helpPanel.Visible && !_tuner.Visible && !_governing) _hud.MoveChild(_layerPanel, -1);
    }

    /// <summary>The right edge of whatever the left column is showing, which the debug panel clears.</summary>
    private float LeftColumnEdge(float margin)
    {
        float edge = margin;
        if (_toolsShown && _palette is not null) edge = Math.Max(edge, _palette.Position.X + _palette.Size.X);
        if (_layersShown) edge = Math.Max(edge, _layerPanel.Position.X + _layerPanel.Size.X);
        return edge;
    }
}
