using System;
using System.Linq;
using Borough.Core.Rules;
using Borough.Core.Entities;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private Label _heldTool = null!;
    private Button _cancelTool = null!;
    private VBoxContainer _browserBody = null!;
    private ScrollContainer _toolScroll = null!;
    private Label _emptyTools = null!;
    private string _toolCategory = "Zoning";

    // The tool browser's own half of FitPanel's settle gate. Its rows are buttons and answer for
    // their height whatever width they are given, so this has never been the flickering one -- it
    // is here because the panel is fitted by the same arithmetic and the exemption would be silent.
    private bool _browserRebuilt;

    private void BuildToolBrowser()
    {
        foreach (Node node in _toolSlot.GetChildren()) { _toolSlot.RemoveChild(node); node.QueueFree(); }
        var held = new HBoxContainer();
        _heldTool = InformationLabel("Look");
        _heldTool.AutowrapMode = TextServer.AutowrapMode.Off;
        _heldTool.ClipText = true;
        _heldTool.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _cancelTool = InformationButton("Cancel", () => { });
        _cancelTool.Pressed += () => AtBoundary(() => Apply(Held("look", 0)));
        held.AddChild(_heldTool);
        held.AddChild(_cancelTool);
        _toolSlot.AddChild(held);
        _palette = new PanelContainer { Theme = _type, Visible = _toolsShown };
        _browserBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var heading = new HBoxContainer();
        var title = InformationLabel("Tools");
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        var close = InformationButton("Close", () => { });
        close.TooltipText = "Close tools; keep the selected tool";
        close.Pressed += () => Ui("tools off");
        heading.AddChild(close);
        var frame = new VBoxContainer();
        frame.AddChild(heading);
        _tools = new VBoxContainer();
        foreach (string category in new[] { "Roads", "Zoning", "Services", "Policies" })
        {
            string captured = category;
            var button = InformationButton(category, () => { });
            button.ToggleMode = true;
            button.Icon = ToolIcon(category);
            button.Alignment = HorizontalAlignment.Left;
            button.AddThemeConstantOverride("icon_max_width", 26);
            button.Pressed += () => AtBoundary(() =>
            {
                if (captured == "Policies")
                {
                    if (!_governing) Govern();
                    _toolsShown = false;
                    return;
                }
                _toolCategory = captured;
                RefreshToolBrowser();
            });
            _tools.AddChild(button);
            if (category == "Policies")
            {
                _policiesButton = button;
                button.Disabled = _world.Rules.Policies.Length == 0;
                button.TooltipText = button.Disabled ? "This city has no Policies available" : "Open city Policies (P)";
            }
        }
        _browserBody.AddChild(_tools);
        _browserBody.AddChild(new HSeparator());
        _choices = new VBoxContainer();
        _browserBody.AddChild(_choices);
        _emptyTools = InformationLabel("No tools available");
        _browserBody.AddChild(_emptyTools);
        _toolScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        _toolScroll.AddChild(_browserBody);
        frame.AddChild(_toolScroll);
        _palette.AddChild(frame);
        _hud.AddChild(_palette);
        RefreshToolBrowser();
    }

    private void RefreshToolBrowser()
    {
        if (_heldTool is null) return;
        _heldTool.Text = _verb == Verb.Zone ? ZoneName() : _verb switch
        { Verb.Look => "Look", Verb.Connect => "Street", Verb.Demolish => "Demolish", _ => _names.Kind(_serviceKind) ?? "Service" };
        _heldTool.Text = ToolLabel(_heldTool.Text);
        _heldTool.TooltipText = _heldTool.Text;
        _cancelTool.Visible = _verb != Verb.Look;
        foreach (Button button in _tools.GetChildren().OfType<Button>())
            button.ButtonPressed = button.Text == _toolCategory;
        _browserRebuilt = true;
        foreach (Node node in _choices.GetChildren()) { _choices.RemoveChild(node); node.QueueFree(); }
        foreach (ToolDefinition tool in ToolDefinitions().Where(t => t.Category == _toolCategory))
            foreach (ToolOption option in tool.Options)
            {
                bool selected = tool.Id switch
                {
                    "zone" => _verb == Verb.Zone && !_zoneErase && option.Choice == _zoneChoice,
                    "erase" => _verb == Verb.Zone && _zoneErase,
                    "street" => _verb == Verb.Connect,
                    "demolish" => _verb == Verb.Demolish,
                    "service" => _verb == Verb.Service && option.Choice == _serviceKind,
                    _ => false,
                };
                var button = new Button { CustomMinimumSize = new Vector2(0, 36), ClipText = true, Text = ToolLabel(option.Label), ToggleMode = true, ButtonPressed = selected,
                    Disabled = !tool.Available, TooltipText = option.Label + "\n" + tool.Hint + (tool.Key == Key.None ? "" : $" ({tool.Key})"),
                    Icon = ToolIcon(OptionIcon(tool, option)), Alignment = HorizontalAlignment.Left,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
                button.AddThemeConstantOverride("icon_max_width", 26);
                button.Pressed += () => AtBoundary(() => tool.Select(option.Choice));
                _choices.AddChild(button);
            }
        _emptyTools.Visible = _choices.GetChildCount() == 0;
    }

    private void LayoutToolBrowser(Vector2 size, float margin, float consoleTop, bool narrow, float inspectorWidth)
    {
        if (_palette is null) return;
        float width = narrow ? 176 : Math.Min(240, 190 * _textPercent / 100f);
        FitPanel(_palette, _toolScroll, _browserBody, margin, margin, width,
            100, Math.Max(100, consoleTop - margin * 2), _browserRebuilt);
        _browserRebuilt = false;
        _palette.Visible = _toolsShown;
        if (_toolsShown && !_helpPanel.Visible && !_tuner.Visible && !_governing) _hud.MoveChild(_palette, -1);
    }

    private static string ToolLabel(string label) => string.IsNullOrEmpty(label) ? label : char.ToUpperInvariant(label[0]) + label[1..];

    private string OptionIcon(ToolDefinition tool, ToolOption option) => tool.Id switch
    {
        "zone" => (_world.Rules.ZoneRules[option.Choice].Admits & (LotTable.Housing | LotTable.Trade)) switch
        {
            LotTable.Housing => "house",
            LotTable.Trade => "shop",
            LotTable.Housing | LotTable.Trade => "mixed",
            _ => "Zoning",
        },
        "service" => _world.Rules.Kind((byte)option.Choice).Serves switch
        {
            Need.Education => "school",
            Need.Health => "health",
            _ => "Services",
        },
        _ => tool.Id,
    };

    private static ImageTexture ToolIcon(string category)
    {
        string path = category switch
        {
            "Roads" or "street" => "M5 2v20M19 2v20M12 2v4m0 4v4m0 4v4",
            "Services" => "M3 9l9-6 9 6H3zm2 3v7m7-7v7m7-7v7M3 21h18",
            "health" => "M9 3h6v6h6v6h-6v6H9v-6H3V9h6z",
            "school" => "M2 8l10-5 10 5-10 5-10-5zm4 4v5l6 3 6-3v-5m4-4v9",
            "house" => "M3 11l9-8 9 8M5 10v11h14V10M10 21v-7h4v7",
            "shop" => "M4 10v11h16V10M2 10l3-7h14l3 7H2zm6-7-1 7m9-7 1 7M8 21v-7h8v7",
            "mixed" => "M2 10l6-6 6 6M4 9v12h8V9m2 12V3h7v18m-4-14h1m-1 4h1m-1 4h1",
            "erase" => "M3 14L14 3l7 7-11 11H8l-5-5v-2zm5-5 8 8m-6 4h12",
            "demolish" => "M3 20h18M5 17l4-9h6l4 9H5zm4-9V4h6v4M2 13h4m12 0h4",
            "Policies" => "M4 5h16M4 12h16M4 19h16M8 2v6m8 1v6m-6 1v6",
            _ => "M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z",
        };
        var image = new Image();
        image.LoadSvgFromString($"<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32' viewBox='0 0 24 24'><path d='{path}' fill='none' stroke='white' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'/></svg>");
        return ImageTexture.CreateFromImage(image);
    }
}
