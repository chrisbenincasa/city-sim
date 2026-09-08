using System;
using System.IO;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private CityPreparation? _preparation;
    private Action<Simulation>? _prepared;
    private CanvasLayer? _loadingHud;
    private Label? _loadingText;
    private ulong _prepareUntil;
    private int _loadingFrames;
    private string _previousLoadingText = "Preparing city…";
    private bool _sceneReady, _cancellingPreparation, _quitAfterPreparation;

    private void PrepareCity(Ruleset rules, int citizens, ulong seed, ulong until, Action<Simulation> completed)
    {
        RequireWorld();
        _atBoundary.Clear();
        _queued.Clear();
        _owed = 0;
        _repave = false;
        _prepared = completed;
        _prepareUntil = until;
        _cancellingPreparation = false;
        _quitAfterPreparation = false;
        _loadingFrames = 0;
        _loadingHud = new CanvasLayer { Layer = 100 };
        AddChild(_loadingHud);
        var shade = new ColorRect { Color = new Color(0.04f, 0.06f, 0.09f, 0.94f) };
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _loadingHud.AddChild(shade);
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        shade.AddChild(center);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 12);
        center.AddChild(column);
        _loadingText = new Label { Text = "Preparing city…", HorizontalAlignment = HorizontalAlignment.Center };
        _loadingText.AddThemeFontSizeOverride("font_size", 24);
        column.AddChild(_loadingText);
        var cancel = new Button { Text = _sceneReady ? "Cancel" : "Cancel and quit", CustomMinimumSize = new Vector2(280, 44) };
        cancel.AddThemeFontSizeOverride("font_size", 20);
        cancel.Pressed += CancelPreparation;
        column.AddChild(cancel);
        _preparation = new CityPreparation(citizens, rules, WorldKey.FromSeed(seed), _empty, until, _routeWorkers);
    }

    private void CancelPreparation()
    {
        _cancellingPreparation = true;
        _preparation?.Cancel();
    }

    private void PollPreparation()
    {
        var preparation = _preparation!;
        _previousLoadingText = _loadingText!.Text;
        _loadingText.Text = _cancellingPreparation ? "Cancelling after the current Tick…"
            : preparation.Tick == 0 ? "Preparing terrain and population…"
            : $"Preparing city · Tick {preparation.Tick:N0} / {Math.Max(1UL, _prepareUntil):N0}";
        AnswerPreparation();
        if (!preparation.IsCompleted) return;
        try
        {
            Simulation simulation = preparation.Complete();
            if (!_cancellingPreparation) _prepared!(simulation);
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            GD.PrintErr(error.ToString());
            Stop(1);
        }
        finally
        {
            preparation.Dispose();
            _preparation = null;
            _prepared = null;
            _loadingHud!.QueueFree();
            _loadingHud = null;
            _loadingText = null;
        }
        if (_quitAfterPreparation && _sceneReady && !_stopping) Quit();
        else if (!_sceneReady && !_stopping) Stop(0);
    }

    private void AnswerPreparation()
    {
        if (++_loadingFrames < 3 || !_asked.TryTake(out string? line)) return;
        var parsed = DriveScript.Line(line, 0);
        if (parsed.Commands is not { Count: 1 } commands
            || (commands[0].Verb is not (DriveVerb.Readout or DriveVerb.Shoot or DriveVerb.Quit)
                && !(commands[0].Verb == DriveVerb.Ui && commands[0].Path == "key Escape")))
        {
            // The listener admits one outstanding request. Preserve it until a World is ready.
            _asked.Add(line);
            return;
        }
        var command = commands[0];
        string caption = "preparing · " + (command.Verb == DriveVerb.Shoot ? _previousLoadingText : _loadingText!.Text).Replace('\n', ' ');
        try
        {
            if (command.Verb == DriveVerb.Quit)
            {
                _quitAfterPreparation = true;
                CancelPreparation();
            }
            else if (command.Verb == DriveVerb.Ui)
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            else
            {
                string path = Globalize(command.Path!);
                if (command.Verb == DriveVerb.Shoot)
                {
                    if (DisplayServer.GetName() != "headless")
                    {
                        using var picture = GetViewport().GetTexture().GetImage();
                        if (picture.SavePng(path) != Error.Ok) throw new IOException("Could not save loading screen.");
                    }
                    File.WriteAllText(Path.ChangeExtension(path, ".txt"), caption + "\n");
                }
                else File.WriteAllText(path, caption + "\n");
            }
            _answered.Add("ok\t0\t" + caption);
        }
        catch (Exception error) { _answered.Add("refused\t" + error.Message); }
    }

    private void InstallCity(Simulation simulation)
    {
        _simulation = simulation;
        _world = simulation.World;
        _log = new InputLogBuilder(_seed, new WorldConfiguration(_citizens),
            RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml)));
        _log.Append(Ticks.Zero, new Command(_empty ? CommandKind.Ground : CommandKind.Populate, default, default));
        _presentedTick = _world.Tick.Raw;
        _owed = 0;
        _batchTicks = 1;
    }
}
