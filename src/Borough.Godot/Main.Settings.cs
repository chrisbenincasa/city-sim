using System;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private PanelContainer _settingsPanel = null!;
    private bool _edgeScrolling = true;
    private Button _edgeScrollButton = null!;
    private bool _antialiasing = true;
    private Button _antialiasButton = null!;
    private Button? _bodiesButton;

    private void BuildSettings()
    {
        _settingsPanel = InformationPanel();
        var body = InformationUi.Stack();
        var heading = new HBoxContainer();
        var title = InformationLabel("Settings", TitlePoints);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(title);
        heading.AddChild(InformationButton("×", () => Ui("settings off")));
        body.AddChild(heading);
        _themeButton = InformationButton("Light theme", () => Ui(_lightUi ? "theme dark" : "theme light"));
        _themeButton.ToggleMode = true;
        body.AddChild(_themeButton);
        var sizing = new HFlowContainer();
        _textSizeLabel = InformationLabel($"Text size: {_textPercent}%");
        _textSizeLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        sizing.AddChild(_textSizeLabel);
        sizing.AddChild(InformationButton("A−", () => SetTextSize(_textPercent - 10)));
        sizing.AddChild(InformationButton("A+", () => SetTextSize(_textPercent + 10)));
        sizing.AddChild(InformationButton("Reset size", () => SetTextSize(100)));
        body.AddChild(sizing);
        _debugButton = InformationButton("Debug readout", () => Ui(_debugShown ? "debug off" : "debug on"));
        _debugButton.ToggleMode = true;
        body.AddChild(_debugButton);
        _edgeScrollButton = InformationButton("Edge scrolling", () => Ui(_edgeScrolling ? "edge-scroll off" : "edge-scroll on"));
        _edgeScrollButton.ToggleMode = true;
        _edgeScrollButton.TooltipText = "Move the camera when the pointer reaches the window edge";
        body.AddChild(_edgeScrollButton);
        _antialiasButton = InformationButton("Anti-aliasing", () => Ui(_antialiasing ? "antialias off" : "antialias on"));
        _antialiasButton.ToggleMode = true;
        _antialiasButton.ButtonPressed = _antialiasing;
        _antialiasButton.TooltipText = "Smooth thin edges such as kerbs and roof ridges (TAA)";
        body.AddChild(_antialiasButton);
        _bodiesButton = InformationButton("Building bodies", () => Ui(_familyBodies ? "family-bodies off" : "family-bodies on"));
        _bodiesButton.ToggleMode = true;
        _bodiesButton.ButtonPressed = _familyBodies;
        _bodiesButton.TooltipText = "Draw each Building's family body near the camera, in place of its box";
        body.AddChild(_bodiesButton);
        body.AddChild(InformationButton("Help & shortcuts", () => Ui("help on")));
        ScrollAuxiliary(_settingsPanel, body);
        _settingsPanel.Visible = false;
    }

    private void SetAntialiasing(bool on)
    {
        _antialiasing = on;
        GetViewport().UseTaa = on;
        _antialiasButton?.SetPressedNoSignal(on);
    }

    private void LayoutSettings(Vector2 size, float margin)
    {
        if (_settingsPanel is null) return;
        float width = Math.Min(360 * _textPercent / 100f, size.X - 2 * margin);
        float top = margin + _chrome.Size.Y + 8;
        SetPanel(_settingsPanel, size.X - width - margin, top, width,
            Math.Min(500 * _textPercent / 100f, size.Y - top - margin));
        if (_settingsPanel.Visible && !_helpPanel.Visible) _hud.MoveChild(_settingsPanel, -1);
    }
}
