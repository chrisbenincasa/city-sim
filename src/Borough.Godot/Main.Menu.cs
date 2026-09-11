using System;
using System.IO;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private Control _menuShade = null!;
    private PanelContainer _menuPanel = null!;
    private VBoxContainer _menuBody = null!;
    private bool _menuLayoutDirty;
    private Button _creditsBack = null!;
    private FileDialog _cityPicker = null!;
    private bool _menuOpen;
    private int _menuPace;
    private string _menuPage = "main";
    private string _menuMessage = "";
    private string? _pendingMenuAction;
    private World? _savedWorld;
    private ulong _savedTick;
    private string? _savePath;
    private bool _resumedFromSave;

    private bool UnsavedCity => _savedWorld != _world || _savedTick != _world.Tick.Raw || _queued.Count != 0;

    private void BuildMenu()
    {
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += () => AtBoundary(() => { OpenMenu(); RequestMenuAction("quit"); });
        _menuShade = new ColorRect
        {
            Color = new Color(0, 0, 0, .45f),
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _hud.AddChild(_menuShade);
        _menuPanel = InformationPanel();
        _menuBody = InformationUi.Stack();
        ScrollAuxiliary(_menuPanel, _menuBody);
        var scroll = (ScrollContainer)_menuBody.GetParent();
        _menuPanel.RemoveChild(scroll);
        var layout = InformationUi.Stack();
        layout.AddChild(scroll);
        _creditsBack = InformationButton("Back to menu", () => Ui("menu cancel"));
        layout.AddChild(_creditsBack);
        _menuPanel.AddChild(layout);
        _menuPanel.Visible = false;
        _cityPicker = new FileDialog
        {
            Title = "Save city",
            Access = FileDialog.AccessEnum.Filesystem,
            UseNativeDialog = false,
            Filters = ["*.borough-city ; Borough city"],
            DisplayMode = FileDialog.DisplayModeEnum.List,
            Exclusive = true,
            Theme = _type,
        };
        _hud.AddChild(_cityPicker);
        _cityPicker.FileSelected += path => AtBoundary(() => PickCityFile(path));
        _cityPicker.Canceled += () => { _pendingMenuAction = null; _menuPage = "main"; RenderMenu(); };
    }

    private void OpenMenu()
    {
        if (_menuOpen) return;
        if (_queued.Count > 0) StepAccounting(Ordered());
        _menuPace = _rung;
        _rung = 0;
        _owed = 0;
        _menuOpen = true;
        _menuPage = "main";
        _menuMessage = "";
        _pendingMenuAction = null;
        _pressed = null;
        _zoneStart = null;
        _zoneMouseDown = false;
        _settingsPanel.Visible = false;
        ShowHelp(false);
        _menuShade.Visible = _menuPanel.Visible = true;
        _hud.MoveChild(_menuShade, -1);
        _hud.MoveChild(_menuPanel, -1);
        RenderMenu();
    }

    private void CloseMenu()
    {
        if (!_menuOpen || _cityPicker.Visible) return;
        _pendingMenuAction = null;
        _menuOpen = false;
        _menuShade.Visible = _menuPanel.Visible = _settingsPanel.Visible = false;
        ShowHelp(false);
        _rung = _menuPace;
        _owed = 0;
    }

    private void RenderMenu()
    {
        _creditsBack.Visible = _menuPage == "credits";
        ((ScrollContainer)_menuBody.GetParent()).ScrollVertical = 0;
        foreach (Node child in _menuBody.GetChildren()) { _menuBody.RemoveChild(child); child.QueueFree(); }
        void Button(string label, string action) => _menuBody.AddChild(InformationButton(label, () => Ui("menu " + action)));
        _menuBody.AddChild(InformationLabel(_menuPage == "credits" ? "Credits" : "Borough", TitlePoints));
        if (_menuPage == "confirm")
        {
            _menuBody.AddChild(InformationLabel("This city has unsaved progress. Save before "
                + (_pendingMenuAction == "quit" ? "quitting?" : "loading another city?")));
            Button("Save and exit", "save-continue");
            Button("Exit without saving", "discard");
            Button("Cancel", "cancel");
        }
        else if (_menuPage == "credits")
        {
            try
            {
                using var credits = JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://assets/credits.json"));
                foreach (var entry in credits.RootElement.EnumerateArray())
                    _menuBody.AddChild(InformationLabel($"{entry.GetProperty("asset").GetString()}\n"
                        + $"{entry.GetProperty("creator").GetString()} · {entry.GetProperty("license").GetString()}\n"
                        + entry.GetProperty("source").GetString()));
            }
            catch (Exception error) { _menuBody.AddChild(InformationLabel("Credits unavailable: " + error.Message)); }
        }
        else
        {
            Button("Resume city", "off");
            Button("Save city…", "save");
            Button("Load city…", "load");
            Button("Settings", "settings");
            Button("Help & shortcuts", "help");
            Button("Credits", "credits");
            Button("Quit game", "quit");
        }
        if (_menuMessage.Length > 0) _menuBody.AddChild(InformationLabel(_menuMessage));
        _menuLayoutDirty = true;
        LayoutInformation();
    }

    private void RequestMenuAction(string action)
    {
        _pendingMenuAction = action;
        if (UnsavedCity) { _menuPage = "confirm"; RenderMenu(); }
        else ContinueMenuAction();
    }

    private void ContinueMenuAction()
    {
        string? action = _pendingMenuAction;
        _pendingMenuAction = null;
        _menuPage = "main";
        if (action == "quit") Quit();
        else if (action == "load") OpenCityPicker(false);
        RenderMenu();
    }

    private void OpenCityPicker(bool save)
    {
        _cityPicker.FileMode = save ? FileDialog.FileModeEnum.SaveFile : FileDialog.FileModeEnum.OpenFile;
        _cityPicker.Title = save ? "Save city" : "Load city";
        string folder = _savePath is null ? ProjectSettings.GlobalizePath("user://saves") : Path.GetDirectoryName(_savePath)!;
        try { Directory.CreateDirectory(folder); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { MenuFailure(error); return; }
        _cityPicker.CurrentDir = folder;
        _cityPicker.CurrentFile = save ? Path.GetFileName(_savePath ?? "My city.borough-city") : "";
        _cityPicker.PopupCentered(new Vector2I(900, 650));
    }

    private void PickCityFile(string path)
    {
        try
        {
            if (_cityPicker.FileMode == FileDialog.FileModeEnum.SaveFile)
            {
                CitySave.Write(path, _world, _toml, _seed);
                _savedWorld = _world;
                _savedTick = _world.Tick.Raw;
                _savePath = path;
                _menuMessage = "Saved " + Path.GetFileName(path) + ".";
                if (_pendingMenuAction is not null) { ContinueMenuAction(); return; }
            }
            else
            {
                SavedCity city = CitySave.Read(path);
                var simulation = new Simulation(city.World, city.Header.Key)
                { VerifyDecideWritesNothing = false, RouteWorkerCount = _routeWorkers };
                simulation.CheckEndOfRun();
                city.World.Changes = new WorldChanges();
                city.World.Changes.Invalidate();
                _rulesetPath = path;
                _toml = city.Toml;
                _names = city.Names;
                _seed = city.Seed;
                _citizens = city.World.Citizens.Rows.LiveCount;
                CloseInspection();
                _healthInspection = false;
                _queued.Clear();
                InstallCity(simulation);
                _resumedFromSave = true;
                _log = new InputLogBuilder(_seed, new WorldConfiguration(_citizens), city.Header.RulesetInForce);
                _savedWorld = _world;
                _savedTick = _world.Tick.Raw;
                _savePath = path;
                _verb = Verb.Look;
                _toolsShown = _governing = _layersShown = _cityShown = _budgetShown = false;
                _cityRead = false;
                _cityGroup = -1;
                _cityCause = null;
                _cityFrom = 0;
                Retrouble();
                _tuner.Visible = false;
                _aimed = null;
                _washing = Wash.None;
                _built = (ulong.MaxValue, Wash.None);
                _synopsisTick = ulong.MaxValue;
                _pickTick = ulong.MaxValue;
                FinishRegenerate();
                _hud.MoveChild(_menuShade, -1);
                _hud.MoveChild(_menuPanel, -1);
                _menuMessage = "Loaded " + Path.GetFileName(path) + ". Resume when ready.";
            }
            _menuPage = "main";
            RenderMenu();
        }
        catch (Exception error) when (error is not OutOfMemoryException) { MenuFailure(error); }
    }

    private void MenuFailure(Exception error)
    {
        _pendingMenuAction = null;
        _menuPage = "main";
        _menuMessage = "Could not " + (_cityPicker.FileMode == FileDialog.FileModeEnum.SaveFile ? "save" : "load")
            + " city: " + error.Message;
        RenderMenu();
    }

    private void MenuAction(string action)
    {
        if (action == "on") { OpenMenu(); return; }
        if (!_menuOpen) return;
        switch (action)
        {
            case "off": CloseMenu(); break;
            case "save": _pendingMenuAction = null; OpenCityPicker(true); break;
            case "load": case "quit": RequestMenuAction(action); break;
            case "save-continue": OpenCityPicker(true); break;
            case "discard": ContinueMenuAction(); break;
            case "cancel": _pendingMenuAction = null; _menuPage = "main"; RenderMenu(); break;
            case "credits": _menuPage = "credits"; RenderMenu(); break;
            case "settings": Ui("settings on"); break;
            case "help": ShowHelp(true); break;
        }
    }

    private void LayoutMenu(Vector2 size, float margin)
    {
        if (_menuPanel is null) return;
        _menuShade.Size = size;
        _menuPanel.Visible = _menuOpen && !_settingsPanel.Visible && !_helpPanel.Visible && !_cityPicker.Visible;
        float width = Math.Min(520 * _textPercent / 100f, size.X - 2 * margin);
        float height = Math.Min((_menuPage == "credits" ? 700 : 510) * _textPercent / 100f, size.Y - 2 * margin);
        var scroll = (ScrollContainer)_menuBody.GetParent();
        FitPanel(_menuPanel, scroll, _menuBody, (size.X - width) / 2, 0, width, 120, height, _menuLayoutDirty);
        _menuPanel.Position = new Vector2((size.X - width) / 2, (size.Y - _menuPanel.Size.Y) / 2);
        _menuLayoutDirty = false;
    }
}
