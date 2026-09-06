using System;
using System.Linq;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private const int CaptionPoints = InformationUi.MetadataPoints;
    private const int SecondaryPoints = InformationUi.MetadataPoints;
    private const int BodyPoints = InformationUi.BodyPoints;
    private const int HeadingPoints = InformationUi.BodyPoints;
    private const int TitlePoints = InformationUi.TitlePoints;
    private ScrollContainer _helpScroll = null!;
    private int _textPercent = 118; // PROVISIONAL readability default.
    private PanelContainer _helpPanel = null!;
    private Control _helpShade = null!;
    private Label _textSizeLabel = null!;
    private sealed record Shortcut(string Group, string Label, string Binding, Key[] Keys, Action Action);

    private void SizeLabel(Label label, int points)
    {
        label.SetMeta("type_points", points);
        if (points == InformationUi.MetadataPoints) label.ThemeTypeVariation = InformationUi.Metadata;
        label.AddThemeFontSizeOverride("font_size", Typed(points));
    }

    private void SetTextSize(int percent)
    {
        _textPercent = Math.Clamp(percent, 100, 150);
        _textSizeLabel.Text = $"Text size: {_textPercent}%";
        Retype();
        SaveInformationPreferences();
    }

    private void ViewCommand(DriveVerb verb, int amount = 0) =>
        Apply(new DriveCommand(_world.Tick.Raw, verb, amount, null));

    private Shortcut[] Shortcuts() =>
    [
        new("Camera", "Rotate left", "Q", [Key.Q], () => ViewCommand(DriveVerb.Turn, -1)),
        new("Camera", "Rotate right", "E", [Key.E], () => ViewCommand(DriveVerb.Turn, 1)),
        new("Camera", "Tilt up", "R", [Key.R], () => ViewCommand(DriveVerb.Tilt, Tipped(5))),
        new("Camera", "Tilt down", "F", [Key.F], () => ViewCommand(DriveVerb.Tilt, Tipped(-5))),
        new("Camera", "Zoom in", "+ / =", [Key.Equal, Key.KpAdd], () => ViewCommand(DriveVerb.Zoom, 4)),
        new("Camera", "Zoom out", "−", [Key.Minus, Key.KpSubtract], () => ViewCommand(DriveVerb.Zoom, -4)),
        new("Time", "Pause / resume", "Space", [Key.Space], () => ViewCommand(_rung == 0 ? DriveVerb.Resume : DriveVerb.Pause)),
        new("Time", "Slower", "[", [Key.Bracketleft], () => ViewCommand(DriveVerb.Speed, Math.Max(1, _rung - 1))),
        new("Time", "Faster", "]", [Key.Bracketright], () => ViewCommand(DriveVerb.Speed, Math.Min(Ladder.Length - 1, _rung + 1))),
        new("Time", "1×", "1", [Key.Key1], () => ViewCommand(DriveVerb.Speed, DesignSpeed)),
        new("Time", "2×", "2", [Key.Key2], () => ViewCommand(DriveVerb.Speed, DesignSpeed + 1)),
        new("Time", "3×", "3", [Key.Key3], () => ViewCommand(DriveVerb.Speed, DesignSpeed + 2)),
        new("Time", "4×", "4", [Key.Key4], () => ViewCommand(DriveVerb.Speed, DesignSpeed + 3)),
        new("Tools", "Look / cancel tool", "V", [Key.V], () => Apply(Held("look", 0))),
        new("Tools", "Subdivide / next permission", "Z", [Key.Z], () => Apply(Held("zone", _verb == Verb.Zone && _world.Rules.ZoneRules.Length > 0 ? (_zoneChoice + 1) % _world.Rules.ZoneRules.Length : 0))),
        new("Tools", "Street", "X", [Key.X], () => Apply(Held("street", 0))),
        new("Tools", "Demolish", "B", [Key.B], () => Apply(Held("demolish", 0))),
        new("Tools", "Service / next kind", "S", [Key.S], () => Apply(Held("service", NextService(_verb == Verb.Service ? _serviceKind : (byte)0)))),
        new("Tools", "Policies", "P", [Key.P], Govern),
        new("Views", "Next map layer", "O", [Key.O], () => Apply(new DriveCommand(_world.Tick.Raw, DriveVerb.Overlay, 0, _washing switch { Wash.None => "pollution", Wash.Pollution => "value", Wash.Value => "sealing", Wash.Sealed => "rung", Wash.Rung => "age", _ => "off" }))),
        new("Views", "Photograph view", "L", [Key.L], () => ViewCommand(DriveVerb.Lens, _photographing ? 0 : 1)),
        new("Views", "Road drawing", "G", [Key.G], () => ViewCommand(DriveVerb.Roads, _roads.Visible ? 0 : 1)),
        new("Developer", "Cell grid", "C", [Key.C], () => ViewCommand(DriveVerb.Cells, _cells.Visible ? 0 : 1)),
        new("Developer", "Debug readout", "F3", [Key.F3], () => Ui(_debugShown ? "debug off" : "debug on")),
        new("Developer", "Ruleset tuner", "Tab", [Key.Tab], ToggleTuner),
        new("Developer", "Regenerate from tuner", "Enter", [Key.Enter, Key.KpEnter], () => { if (_tuner.Visible) Regenerate(); }),
        new("Developer", "Write Input Log", "W", [Key.W], Record),
    ];

    private HBoxContainer CameraControls()
    {
        var group = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        string[] labels = ["↶", "↷", "↑", "↓", "+", "−"];
        group.AddChild(ConsoleLabel("Camera", CaptionPoints));
        int i = 0;
        foreach (var shortcut in Shortcuts().Where(s => s.Group == "Camera"))
        {
            var button = ConsoleButton(labels[i++], shortcut.Action);
            button.TooltipText = $"{shortcut.Label} ({shortcut.Binding})";
            group.AddChild(button);
        }
        return group;
    }

    private void BuildDiscovery()
    {
        _helpShade = new Control { MouseFilter = Control.MouseFilterEnum.Stop };
        _hud.AddChild(_helpShade);
        _helpPanel = InformationPanel();
        var body = InformationUi.Stack();
        var heading = new HBoxContainer();
        var title = InformationLabel("Help", TitlePoints);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        heading.AddChild(InformationButton("Close Help ×", () => Ui("help off")));
        body.AddChild(heading);
        var sizing = new HFlowContainer();
        _textSizeLabel = InformationLabel($"Text size: {_textPercent}%");
        _textSizeLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        sizing.AddChild(_textSizeLabel);
        sizing.AddChild(InformationButton("A−", () => SetTextSize(_textPercent - 10)));
        sizing.AddChild(InformationButton("A+", () => SetTextSize(_textPercent + 10)));
        sizing.AddChild(InformationButton("Reset size", () => SetTextSize(118)));
        body.AddChild(sizing);
        _helpScroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", InformationUi.SectionGap);
        void Section(string title, params string[] lines)
        {
            var card = InformationUi.Section(InformationLabel(title), true, false, out var rows);
            foreach (string line in lines) rows.AddChild(InformationLabel(line));
            content.AddChild(card);
        }
        Section("Mouse & trackpad", "Click to inspect or use the selected tool. Right or middle drag to pan. Two-finger scroll pans; pinch or the mouse wheel zooms. The window edges pan the camera. Shift-click with Street removes a Street.");
        Section("Windows", "? (Shift+/) opens Help. Escape closes the topmost window, then the inspector, then cancels the selected tool. Help leaves time running at your chosen pace.");
        foreach (var section in Shortcuts().GroupBy(s => s.Group))
            Section(section.Key, section.Select(s => $"{s.Binding} — {s.Label}").ToArray());
        _helpScroll.AddChild(content);
        body.AddChild(_helpScroll);
        _helpPanel.AddChild(body);
        ShowHelp(false);
    }

    private void ToggleTuner()
    {
        _tuner.Visible = !_tuner.Visible;
        if (_tuner.Visible) _hud.MoveChild(_tuner, -1);
    }

    private void ShowHelp(bool shown)
    {
        _helpShade.Visible = shown;
        _helpPanel.Visible = shown;
        if (shown)
        {
            _hud.MoveChild(_helpShade, -1);
            _hud.MoveChild(_helpPanel, -1);
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
            _pressed = null;
        }
    }

    private static void ScrollAuxiliary(PanelContainer panel, VBoxContainer body)
    {
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        foreach (Node node in InformationDescendants(body))
            if (node is Label label)
            {
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                label.CustomMinimumSize = Vector2.Zero;
            }
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(body);
        panel.AddChild(scroll);
    }

    private void LayoutDiscovery(Vector2 size, float margin)
    {
        if (_helpPanel is null) return;
        foreach (var panel in new[] { _tuner, _policyPanel })
            if (panel is not null)
                SetPanel(panel, margin, margin, Math.Min(720 * _textPercent / 100f, size.X - 2 * margin), size.Y - 2 * margin);
        _helpShade.Size = size;
        float width = Math.Min(720 * _textPercent / 100f, size.X - 2 * margin);
        SetPanel(_helpPanel, (size.X - width) / 2, margin, width, size.Y - 2 * margin);
    }

    public override void _Input(InputEvent @event)
    {
        if (_helpPanel is null) return;
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape)
        {
            if (_helpPanel.Visible) Ui("help off");
            else if (_tuner.Visible && (!_governing || _tuner.GetIndex() > _policyPanel.GetIndex())) _tuner.Visible = false;
            else if (_governing) Govern();
            else if (_inspector!.Visible) Ui("close");
            else Apply(Held("look", 0));
            GetViewport().GuiGetFocusOwner()?.ReleaseFocus();
            GetViewport().SetInputAsHandled();
            return;
        }
        if (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        if (!key.CtrlPressed && !key.AltPressed && !key.MetaPressed
            && (key.Keycode == Key.Question || key.Keycode == Key.Slash && key.ShiftPressed))
        {
            Ui(_helpPanel.Visible ? "help off" : "help on");
            GetViewport().SetInputAsHandled();
        }
    }
}
