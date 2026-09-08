using System;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private PanelContainer _settingsPanel = null!;

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
        body.AddChild(InformationButton("Help & shortcuts", () => Ui("help on")));
        ScrollAuxiliary(_settingsPanel, body);
        _settingsPanel.Visible = false;
    }

    private void LayoutSettings(Vector2 size, float margin)
    {
        if (_settingsPanel is null) return;
        float width = Math.Min(360 * _textPercent / 100f, size.X - 2 * margin);
        SetPanel(_settingsPanel, size.X - width - margin, margin, width,
            Math.Min(380 * _textPercent / 100f, size.Y - 2 * margin));
        if (_settingsPanel.Visible && !_helpPanel.Visible) _hud.MoveChild(_settingsPanel, -1);
    }
}
