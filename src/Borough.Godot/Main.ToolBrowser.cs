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
    private VBoxContainer _placementRail = null!, _toolOptions = null!;
    private Label _chooserTitle = null!;
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
        _toolOptions = new VBoxContainer();
        _toolSlot.AddChild(_toolOptions);
        _palette = new PanelContainer { Theme = _type, Visible = _toolsShown };
        _browserBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var heading = new HBoxContainer();
        var title = _chooserTitle = InformationLabel(_toolCategory);
        title.AutowrapMode = TextServer.AutowrapMode.Off;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        var close = InformationButton("Close", () => { });
        close.TooltipText = "Close tools; keep the selected tool";
        close.Pressed += () => Ui("tools off");
        heading.AddChild(close);
        var frame = new VBoxContainer();
        frame.AddChild(heading);
        _placementRail = new VBoxContainer { Theme = _type };
        _hud.AddChild(_placementRail);
        _tools = _placementRail;
        foreach (string category in new[] { "Connections", "Zoning", "Municipal", "Utilities", "Demolish" })
        {
            string captured = category;
            var button = InformationButton(category, () => { });
            button.ToggleMode = true;
            UiIcons.Attach(button, ToolIconName(category));
            button.Alignment = HorizontalAlignment.Left;
            button.TooltipText = category == "Utilities" ? "No utility placement tools are available yet" : $"Open {category.ToLowerInvariant()} tools";
            button.Disabled = category == "Utilities";
            button.Pressed += () => Ui("category " + captured);
            _tools.AddChild(button);
        }
        _choices = new VBoxContainer();
        _browserBody.AddChild(_choices);
        _emptyTools = InformationLabel("No tools available");
        _browserBody.AddChild(_emptyTools);
        _toolScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
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
            button.ButtonPressed = _toolsShown && button.Text == _toolCategory;
        _chooserTitle.Text = _toolCategory;
        _browserRebuilt = true;
        foreach (Node node in _choices.GetChildren()) { _choices.RemoveChild(node); node.QueueFree(); }
        foreach (ToolDefinition tool in ToolDefinitions().Where(t => t.Category == _toolCategory && t.Id != "zone-size"))
            foreach (ToolOption option in tool.Options)
            {
                bool selected = tool.Id switch
                {
                    "zone-size" => _zoneParcels == (option.Choice == 0),
                    "zone" => _verb == Verb.Zone && !_zoneErase && option.Choice == _zoneChoice,
                    "erase" => _verb == Verb.Zone && _zoneErase,
                    "street" => _verb == Verb.Connect,
                    "demolish" => _verb == Verb.Demolish,
                    "service" => _verb == Verb.Service && option.Choice == _serviceKind,
                    _ => false,
                };
                var button = new Button
                {
                    CustomMinimumSize = new Vector2(0, 36),
                    ClipText = true,
                    Text = ToolLabel(option.Label),
                    ToggleMode = true,
                    ButtonPressed = selected,
                    Disabled = !tool.Available,
                    TooltipText = option.Label + "\n" + tool.Hint + (tool.Key == Key.None ? "" : $" ({tool.Key})"),
                    Alignment = HorizontalAlignment.Left,
                    TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
                };
                UiIcons.Attach(button, ToolIconName(OptionIcon(tool, option)));
                button.Pressed += () => AtBoundary(() => { tool.Select(option.Choice); Ui("tools off"); });
                _choices.AddChild(button);
            }
        _emptyTools.Text = "No placement tools available";
        _emptyTools.Visible = _choices.GetChildCount() == 0;
        foreach (Node node in _toolOptions.GetChildren()) { _toolOptions.RemoveChild(node); node.QueueFree(); }
        if (_verb == Verb.Zone)
        {
            var options = new HBoxContainer();
            foreach (var option in ToolDefinitions().First(t => t.Id == "zone-size").Options)
            {
                var button = ConsoleButton(option.Label, () => Ui(option.Choice == 0 ? "zone-size parcels" : "zone-size blocks"));
                button.ToggleMode = true;
                button.ButtonPressed = _zoneParcels == (option.Choice == 0);
                options.AddChild(button);
            }
            var value = ConsoleButton("Land value ↗", () => Apply(new Borough.Formats.DriveCommand(_world.Tick.Raw, Borough.Formats.DriveVerb.Overlay, 0, "value")));
            value.TooltipText = "Show land value; keep the zoning tool";
            options.AddChild(value);
            _toolOptions.AddChild(options);
        }
    }

    private void LayoutToolBrowser(Vector2 size, float margin, float consoleTop, bool narrow,
        float inspectorWidth, float top)
    {
        if (_palette is null) return;
        float width = Math.Max(240, Math.Max(_palette.GetCombinedMinimumSize().X, _choices.GetCombinedMinimumSize().X + 32));
        float left = _placementRail.Position.X + _placementRail.Size.X + 8;
        FitPanel(_palette, _toolScroll, _browserBody, left, margin, width,
            100, Math.Max(100, (LeftPanelBottom(consoleTop) - margin - 8) / (_layersShown ? 2 : 1)), _browserRebuilt);
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

    private static string ToolIconName(string category) => category switch
    {
        "Connections" or "street" => "road",
        "Municipal" => "civic",
        "health" => "clinic",
        "house" => "housing",
        "shop" => "trade",
        "Policies" => "policies",
        "Demolish" => "demolish",
        "school" or "mixed" or "erase" or "demolish" => category,
        _ => "grid",
    };
}
