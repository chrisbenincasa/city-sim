using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Quantities;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

// A clock diagram: daylight above the horizon, night below, dawn at the left.
internal sealed partial class SkyArc : Control
{
    internal Color Ink = Colors.White, Paper = Colors.Black;
    internal int Minute = -1;
    internal bool Daytime => Minute >= 360 && Minute < 1080;
    internal Vector2 Marker => At(((Minute - 360 + 1440) % 1440) / 1440f);

    private Vector2 At(float progress)
    {
        float angle = Mathf.Pi - progress * Mathf.Tau;
        return new Vector2(Size.X * .5f + Mathf.Cos(angle) * Size.X * .38f,
            Size.Y * .5f - Mathf.Sin(angle) * Size.Y * .34f);
    }

    public override void _Draw()
    {
        const int samples = 48;
        var day = new Vector2[samples / 2 + 1];
        var night = new Vector2[samples / 2 + 1];
        for (int i = 0; i <= samples / 2; i++)
        {
            day[i] = At(i / (float)samples);
            night[i] = At(.5f + i / (float)samples);
        }
        DrawColoredPolygon(day, new Color(Ink, Daytime ? .12f : .04f));
        DrawColoredPolygon(night, new Color(Ink, Daytime ? .03f : .10f));
        DrawPolyline(day, new Color(Ink, Daytime ? .65f : .25f), 1.5f, true);
        DrawPolyline(night, new Color(Ink, Daytime ? .25f : .65f), 1.5f, true);
        DrawLine(new Vector2(2, Size.Y * .5f), new Vector2(Size.X - 2, Size.Y * .5f), new Color(Ink, .3f), 1, true);
        Vector2 at = Marker;
        DrawCircle(at, 6, Paper);
        DrawCircle(at, 3.5f, Ink);
        if (Daytime)
            for (int i = 0; i < 8; i++)
            {
                var ray = Vector2.FromAngle(i * Mathf.Tau / 8);
                DrawLine(at + ray * 5, at + ray * 7, Ink, 1, true);
            }
        else DrawArc(at, 5, 0, Mathf.Tau, 20, Ink, 1, true);
    }
}

public partial class Main
{
    private PanelContainer _console = null!;
    private ScrollContainer _consoleScroll = null!;
    private VBoxContainer _consoleBody = null!;
    private HFlowContainer _consoleTop = null!;
    private HBoxContainer _pointerRow = null!;
    private Control _toolSlot = null!;
    private SkyArc _skyArc = null!;
    private Label _rungLabel = null!;
    private Label _refusalLabel = null!;
    private Button _pauseButton = null!, _slowerButton = null!, _fasterButton = null!;
    private PanelContainer _refusalRow = null!;

    /// <summary>The overlay ramp, drawn from the same <see cref="Bands"/> the map is washed with.</summary>
    internal sealed partial class LegendRamp : Control
    {
        internal Color[] Colours = [];

        public override void _Draw()
        {
            if (Colours.Length == 0) return;
            float step = Size.X / Colours.Length;
            for (int i = 0; i < Colours.Length; i++)
                DrawRect(new Rect2(i * step, 0f, step + 1f, Size.Y), Colours[i]);
        }
    }

    /// <summary>
    /// The everyday controls, as <b>one console along the bottom edge</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Composition C, chosen 2026-09-05</b> (<c>plans/0064</c> row 3). The top bar and the bottom
    /// tool palette were two strips claiming 200 px of every frame before anything had happened —
    /// 21% of a 960 px window. They are one strip here: time and the day/night clock at the left,
    /// the held tool in the centre, the camera at the right, and the pointer reading and any
    /// refusal along the bottom. ***Idle it is one row***, and the top edge belongs to the picture.
    /// </para>
    /// <para>
    /// 🔴 <b>THE POINTER READING IS IN HERE AND NO LONGER A PANEL OF ITS OWN.</b> That supersedes
    /// row 1's <em>hover stays in a fixed lower-left corner</em>. It sits beside the tool's own
    /// hint because they are the same kind of sentence — <em>what a click would do here</em> — and
    /// having them at opposite corners of the screen was the whole of the complaint.
    /// </para>
    /// <para>
    /// ⚠ <b><see cref="HFlowContainer"/> and not an <see cref="HBoxContainer"/></b>, because the
    /// narrow layout is the same console with its groups wrapped rather than a second composition.
    /// A box would have needed the orientation swapped at the breakpoint, which is two layouts to
    /// keep in agreement and <c>plans/0012</c> <b>Cause 1</b> by construction.
    /// </para>
    /// <para>
    /// ⚠ <b>Every control here issues a <see cref="DriveCommand"/></b> and none of them touch state
    /// directly, so the palette's own rule holds for the whole console: the keyboard, a button and
    /// a driven script all reach the world by one path, and a session records and replays whichever
    /// was used.
    /// </para>
    /// </remarks>
    private void Console()
    {
        _console = InformationPanel();
        _consoleBody = new VBoxContainer();
        _consoleBody.AddThemeConstantOverride("separation", 8);

        _consoleTop = new HFlowContainer();
        _consoleTop.AddThemeConstantOverride("h_separation", 16);
        _consoleTop.AddThemeConstantOverride("v_separation", 8);

        // ---- time -------------------------------------------------------------------------------
        var pace = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        pace.AddThemeConstantOverride("separation", 8);
        // ⚠ THE STEPPERS ARE DOUBLED ARROWS and the pause button is single. Both were single, and
        // a paused console then read ▶ ◀ paused ▶ -- two different verbs wearing one glyph, side by
        // side, one of which is the only way back to a running city.
        _pauseButton = ConsoleButton("⏸", () => Apply(new DriveCommand(
            _world.Tick.Raw, _rung == 0 ? DriveVerb.Resume : DriveVerb.Pause, 0, null)));
        _pauseButton.TooltipText = "Pause and resume (space)";
        _pauseButton.ToggleMode = true;
        _slowerButton = ConsoleButton("◀◀", () => Apply(new DriveCommand(
            _world.Tick.Raw, DriveVerb.Speed, Math.Max(1, _rung - 1), null)));
        _slowerButton.TooltipText = "Slower ([)";
        _rungLabel = ConsoleLabel(string.Empty, BodyPoints);
        _rungLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _rungLabel.CustomMinimumSize = new Vector2(46, 0);
        _fasterButton = ConsoleButton("▶▶", () => Apply(new DriveCommand(
            _world.Tick.Raw, DriveVerb.Speed, Math.Min(Ladder.Length - 1, _rung + 1), null)));
        _fasterButton.TooltipText = "Faster (])";
        pace.AddChild(_rungLabel);
        pace.AddChild(_slowerButton);
        pace.AddChild(_pauseButton);
        pace.AddChild(_fasterButton);
        _consoleTop.AddChild(pace);

        // ---- the sky ----------------------------------------------------------------------------
        var sky = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        sky.AddThemeConstantOverride("separation", 8);
        _skyArc = new SkyArc { CustomMinimumSize = new Vector2(112, 44), MouseFilter = Control.MouseFilterEnum.Stop };
        sky.AddChild(_skyArc);
        _consoleTop.AddChild(sky);

        // ---- tools ------------------------------------------------------------------------------
        _toolSlot = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,

            // ⚠ A MINIMUM WIDTH SO THE FLOW WRAPS INSTEAD OF CLIPPING, and NO ExpandFill. Expanding
            // made the tools eat the whole first line's slack, which pushed the chrome onto a second
            // line while the layer picker stayed on the first -- the console's five groups reading
            // in an order nobody chose. Wrapping in order is worth more than a flush right edge.
            CustomMinimumSize = new Vector2(270, 0),
        };
        _consoleTop.AddChild(_toolSlot);

        _consoleTop.AddChild(CameraControls());

        _consoleTop.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _consoleScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Reserve,
        };
        _consoleScroll.AddChild(_consoleTop);
        _consoleBody.AddChild(_consoleScroll);

        // ---- the pointer reading, and any refusal ------------------------------------------------
        _pointerRow = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _pointerRow.AddThemeConstantOverride("separation", 8);
        _hover.LabelSettings = null;
        _hover.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _pointerRow.AddChild(_hover);
        _consoleBody.AddChild(new HSeparator());
        _consoleBody.AddChild(_pointerRow);

        _refusalRow = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var refusal = new VBoxContainer();
        refusal.AddThemeConstantOverride("separation", 3);
        var refusalHead = ConsoleLabel("REFUSED", CaptionPoints);
        refusalHead.ThemeTypeVariation = InformationUi.WarningText;
        _refusalLabel = ConsoleLabel(string.Empty, SecondaryPoints);
        _refusalLabel.ThemeTypeVariation = InformationUi.WarningText;
        _refusalLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        refusal.AddChild(refusalHead);
        refusal.AddChild(_refusalLabel);
        _refusalRow.AddChild(refusal);
        _consoleBody.AddChild(_refusalRow);

        _console.AddChild(_consoleBody);
    }

    /// <summary>A console button: the shell's one button style, at the console's smaller height.</summary>
    /// <remarks>
    /// ⚠ <b><see cref="Control.SizeFlags.ShrinkCenter"/> vertically, and it is not cosmetic.</b> A
    /// <see cref="Control"/> in a box fills the row by default, and the console's rows are as tall
    /// as their tallest member — so a Fill button beside a two-line label became a two-line-tall
    /// button, and beside a wrapped one it became the height of the console.
    /// </remarks>
    private Button ConsoleButton(string text, Action action)
    {
        return InformationUi.Button(text, () => AtBoundary(action), compact: true);
    }

    /// <summary>
    /// A console reading: <b>one line, and it does not wrap.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b><see cref="InformationLabel"/> WRAPS, AND IN A FLOW CONTAINER THAT IS A COLUMN OF
    /// SINGLE LETTERS.</b> Its <see cref="TextServer.AutowrapMode.WordSmart"/> is right in the
    /// inspector, where a Label has a panel's width and a long sentence to fit into it; in the
    /// console every reading is short and the container is asking each child how narrow it can be.
    /// A wrapping Label answers <em>one character</em>, and the console grew to 460 px.
    /// </remarks>
    private Label ConsoleLabel(string text, int size)
    {
        var label = InformationUi.Label(text, size, compact: true);
        SizeLabel(label, size);
        return label;
    }

    /// <summary>
    /// The console groups whose widths are stated, restated against the console they are in.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A STATED MINIMUM WIDER THAN THE WINDOW IS AN OVERFLOW AND NOT A WRAP.</b> The tool tray
    /// names a width so the flow wraps around it rather than clipping it — and at 480 px that number
    /// is wider than the console itself, so the group ran off the right edge and took the pointer
    /// reading with it. ***A minimum is a request and the window is the authority***, so it is
    /// capped here every frame rather than chosen once.
    /// </remarks>
    private void SizeConsole(float inner)
    {
        _toolSlot.CustomMinimumSize = new Vector2(Math.Min(270f, inner), 0f);
        _hover.CustomMinimumSize = new Vector2(Math.Max(120f, inner), 0f);
    }

    /// <summary>What the current rung is called.</summary>
    private string RungName() => Rungs[_rung];

    /// <summary>
    /// The named phase of the Day, which is what <c>01 §7</c> asks for instead of a clock.
    /// <b>It reads in the clock's tooltip and nowhere else.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>THE BOUNDARIES ARE PROVISIONAL and chosen by taste</b> — <c>plans/0045</c> standing
    /// order 4, so no ratifier and no <c>plans/0002</c> §D row. <c>01 §7</c> names the five phases
    /// and states no boundary for any of them, and nothing in the simulation divides a Day this
    /// way. ***What would derive them is a Day with a shape in it***: <c>adr/0101</c> already puts
    /// every Shift start between 06:00 and 10:00, which is the one boundary here that is read off
    /// the build rather than picked.
    /// </remarks>
    private static string PhaseOfDay(int minute) => (minute / 60) switch
    {
        >= 5 and < 7 => "dawn",
        >= 7 and < 10 => "morning peak",
        >= 10 and < 16 => "midday",
        >= 16 and < 20 => "evening peak",
        _ => "night",
    };

    /// <summary>
    /// Every reading in the console, restated against the world as it now is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It writes what the world says and never what the button did.</b> A button issues a
    /// <see cref="DriveCommand"/> and returns; the world steps; this reads the result. So a
    /// refused command shows the old state rather than the state that was asked for, which is the
    /// one behaviour a control that writes its own label cannot have.
    /// </para>
    /// </remarks>
    private void RefreshConsole()
    {
        _rungLabel.Text = Rungs[_rung == 0 ? _resume : _rung];
        _rungLabel.TooltipText = Pace(_rung);
        UiIcons.Glyph(_pauseButton, _rung == 0 ? "▶" : "⏸", _rung == 0 ? "play" : "pause");
        _pauseButton.SetPressedNoSignal(_rung == 0);
        _slowerButton.Disabled = _rung <= 1;
        _fasterButton.Disabled = _rung >= Ladder.Length - 1;

        int minute = Ticks.MinuteOfDay(_world.Tick.Raw);
        if (_skyArc.Minute != minute)
        {
            _skyArc.Minute = minute;
            _skyArc.TooltipText = $"Day {_world.Tick.Raw / (ulong)Ticks.PerDay}, {minute / 60:00}:{minute % 60:00}"
                + $" — {PhaseOfDay(minute)}.\nDawn left, noon above, dusk right, midnight below.";
            _skyArc.QueueRedraw();
        }

        RefreshChrome();

        _refusalRow.Visible = _refused.Length > 0;
        if (_refused.Length > 0) _refusalLabel.Text = _refused;
    }
}
