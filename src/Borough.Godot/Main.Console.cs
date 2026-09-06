using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Quantities;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

/// <summary>
/// The sky's arc, with the sun on it by day and the moon by night.
/// </summary>
/// <remarks>
/// <para>
/// <c>01 §7</c>: <em>time of day is an arc with named phases, and never a clock</em>, because a
/// numeric clock makes a claim that can be checked against what the player is watching and under
/// any workable tick rate that claim is false. The arc makes no numeric claim, so it cannot be
/// caught lying.
/// </para>
/// <para>
/// ⚠ <b>What is read off the clock and what is drawing convention.</b> The light's position along
/// the arc and the spent share of daylight are the clock. <b>The two horizon crossings are a
/// CONVENTION</b> — 06:00 and 18:00, not the sun's real height, which
/// <see cref="Main.Daylight"/> computes and this does not ask for. The quarter ticks carry no
/// labels because <c>01 §7</c>'s five phase names have no boundaries anywhere in the build
/// (<c>07 §1.3</c>: a mark either reports a fact or is labelled as invention).
/// </para>
/// <para>
/// 🔴 <b>THE MOON IS FULL BECAUSE THE CITY'S MOON IS FULL.</b> <see cref="Main.Daylight"/> aims one
/// <c>DirectionalLight3D</c> from the sun while it is up and from the sun's <em>antipode</em> while
/// it is down, and its own remark calls that <em>"a full moon every single night, and is a
/// fib"</em>. <see cref="Phase"/> can draw a crescent and <b>nothing sets it</b>: a waxing moon
/// here would be a HUD contradicting the picture behind it, which is the one thing <c>07 §1.3</c>
/// refuses. ***It becomes honest the moment <c>Daylight()</c> scales the moon's energy by the same
/// phase***, and the cycle length is then an authored constant. Filed as its own row rather than
/// taken here.
/// </para>
/// </remarks>
internal sealed partial class SkyArc : Control
{
    /// <summary>Sunrise and sunset as this drawing places them. A convention, not the sun.</summary>
    private const int Sunrise = 6 * 60, Sunset = 18 * 60;

    private const int Samples = 28;

    internal Color Ink = Colors.White, Paper = Colors.Black;

    /// <summary>Minute of the day, 0 to 1,439, from <see cref="Ticks.MinuteOfDay"/>.</summary>
    internal int Minute;

    /// <summary>0 new, .5 full, 1 new again. <b>Negative draws the full moon the sky lights.</b></summary>
    internal float Phase = -1f;

    private Vector2 At(float t)
    {
        float w = Size.X, h = Size.Y, horizon = h * 0.76f;
        Vector2 a = new(w * 0.18f, horizon), b = new(w * 0.5f, -h * 0.26f), c = new(w * 0.82f, horizon);
        float u = 1f - t;
        return (u * u * a) + (2f * u * t * b) + (t * t * c);
    }

    public override void _Draw()
    {
        float w = Size.X, horizon = Size.Y * 0.76f;
        bool daytime = Minute >= Sunrise && Minute <= Sunset;
        float along = daytime
            ? (Minute - Sunrise) / (float)(Sunset - Sunrise)
            : ((Minute - Sunset + 1440) % 1440) / (float)(1440 - (Sunset - Sunrise));

        // Night, either side of the crossings. Dashed while the sun is up, solid while it is not,
        // so the horizon reads as the part of the day you are in.
        var edge = new Color(Ink, daytime ? 0.20f : 0.55f);
        DrawLine(new Vector2(2f, horizon), At(0f), edge, 1.5f, true);
        DrawLine(At(1f), new Vector2(w - 2f, horizon), edge, 1.5f, true);

        var path = new Vector2[Samples + 1];
        for (int i = 0; i <= Samples; i++) path[i] = At(i / (float)Samples);

        if (daytime)
        {
            // The sky under the arc, brightest at noon. Body without a claim.
            for (int i = 0; i < Samples; i++)
            {
                float mid = (i + 0.5f) / Samples;
                float lit = 0.05f + (0.25f * Mathf.Sin(mid * Mathf.Pi));
                DrawColoredPolygon(
                    [path[i], path[i + 1], new Vector2(path[i + 1].X, horizon), new Vector2(path[i].X, horizon)],
                    new Color(Ink, lit));
            }
        }

        DrawPolyline(path, new Color(Ink, daytime ? 0.28f : 0.14f), 1.6f, true);

        if (daytime)
        {
            // The day already spent, drawn solid over the faint whole.
            int spent = Math.Max(2, Mathf.CeilToInt(along * Samples) + 1);
            DrawPolyline(path[..Math.Min(spent, path.Length)], new Color(Ink, 0.80f), 1.6f, true);
        }

        for (int k = 0; k <= 4; k++)
        {
            Vector2 tick = At(k / 4f);
            DrawLine(tick, tick + new Vector2(0f, 4f), new Color(Ink, 0.30f), 1f, true);
        }

        Vector2 light = At(Mathf.Clamp(along, 0f, 1f));
        if (daytime)
        {
            DrawCircle(light, Size.Y * 0.23f, new Color(Ink, 0.14f));
            DrawCircle(light, Size.Y * 0.14f, new Color(Ink, 0.28f));
            DrawCircle(light, Size.Y * 0.085f, Ink);
            return;
        }

        float r = Size.Y * 0.105f;
        DrawCircle(light, Size.Y * 0.20f, new Color(Ink, 0.10f));
        DrawCircle(light, r, Ink);
        if (Phase >= 0f)
        {
            // The shadow is drawn in the panel's own colour, which is what makes a crescent
            // possible without a mask. It only works because the console is opaque.
            float d = 2f * r * (1f - Mathf.Abs((2f * Phase) - 1f));
            DrawCircle(light + new Vector2(Phase < 0.5f ? -d : d, 0f), r, Paper);
        }

        DrawCircle(light, r, new Color(Ink, 0.38f), false, 1f, true);
    }
}

public partial class Main
{
    private PanelContainer _console = null!;
    private VBoxContainer _consoleBody = null!, _layerGroup = null!;
    private HFlowContainer _consoleTop = null!;
    private HBoxContainer _pointerRow = null!;
    private HFlowContainer _layerChoices = null!;
    private Control _toolSlot = null!;
    private SkyArc _skyArc = null!;
    private Label _rungLabel = null!, _dayLengthLabel = null!, _dayLabel = null!;
    private Label _legendTitle = null!, _legendBody = null!, _refusalLabel = null!;
    private Label _pointerHead = null!;
    private LegendRamp _legendRamp = null!;
    private Button _layerButton = null!, _pauseButton = null!, _slowerButton = null!, _fasterButton = null!;
    private PanelContainer _refusalRow = null!;
    private bool _layersShown;

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
    /// 21% of a 960 px window. They are one strip here: time at the left, tools in the centre,
    /// the layer picker and its legend at the right, and the pointer reading and any refusal along
    /// the bottom. ***Idle it is one row***, and the top edge belongs to the picture.
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
        _consoleTop.AddThemeConstantOverride("h_separation", 14);
        _consoleTop.AddThemeConstantOverride("v_separation", 8);

        // ---- time -------------------------------------------------------------------------------
        var pace = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        pace.AddThemeConstantOverride("separation", 6);
        // ⚠ THE STEPPERS ARE DOUBLED ARROWS and the pause button is single. Both were single, and
        // a paused console then read ▶ ◀ paused ▶ -- two different verbs wearing one glyph, side by
        // side, one of which is the only way back to a running city.
        _pauseButton = ConsoleButton("⏸", () => Apply(new DriveCommand(
            _world.Tick.Raw, _rung == 0 ? DriveVerb.Resume : DriveVerb.Pause, 0, null)));
        _pauseButton.TooltipText = "Pause and resume (space)";
        _slowerButton = ConsoleButton("◀◀", () => Apply(new DriveCommand(
            _world.Tick.Raw, DriveVerb.Speed, Math.Max(1, _rung - 1), null)));
        _slowerButton.TooltipText = "Slower ([)";
        _rungLabel = ConsoleLabel(string.Empty, 19);
        _rungLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _rungLabel.CustomMinimumSize = new Vector2(46, 0);
        _fasterButton = ConsoleButton("▶▶", () => Apply(new DriveCommand(
            _world.Tick.Raw, DriveVerb.Speed, Math.Min(Ladder.Length - 1, _rung + 1), null)));
        _fasterButton.TooltipText = "Faster (])";
        _dayLengthLabel = ConsoleLabel(string.Empty, 13);
        pace.AddChild(_pauseButton);
        pace.AddChild(_slowerButton);
        pace.AddChild(_rungLabel);
        pace.AddChild(_fasterButton);
        pace.AddChild(_dayLengthLabel);
        _consoleTop.AddChild(pace);

        // ---- the sky ----------------------------------------------------------------------------
        var sky = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        sky.AddThemeConstantOverride("separation", 8);
        _skyArc = new SkyArc { CustomMinimumSize = new Vector2(124, 36), MouseFilter = Control.MouseFilterEnum.Ignore };
        _dayLabel = ConsoleLabel(string.Empty, 13);
        sky.AddChild(_skyArc);
        sky.AddChild(_dayLabel);
        _consoleTop.AddChild(sky);

        // ---- tools ------------------------------------------------------------------------------
        _toolSlot = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,

            // ⚠ A MINIMUM WIDTH SO THE FLOW WRAPS INSTEAD OF CLIPPING, and NO ExpandFill. Expanding
            // made the tools eat the whole first line's slack, which pushed the chrome onto a second
            // line while the layer picker stayed on the first -- the console's five groups reading
            // in an order nobody chose. Wrapping in order is worth more than a flush right edge.
            CustomMinimumSize = new Vector2(560, 0),
        };
        _consoleTop.AddChild(_toolSlot);

        // ---- layers -----------------------------------------------------------------------------
        // ⚠ THE ONE GROUP THAT EXPANDS, and only once it is open. A FlowContainer breaks its lines
        // off the minimum widths and distributes the slack afterwards, so expanding here cannot
        // change which group lands on which row -- it changes what the picker does with the room
        // its row already had. Without it the four washes came back as two rows beside 900 px of
        // empty console.
        _layerGroup = new VBoxContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _layerGroup.AddThemeConstantOverride("separation", 7);
        var layerHead = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        layerHead.AddThemeConstantOverride("separation", 6);
        _layerButton = ConsoleButton(string.Empty, () => Ui(_layersShown ? "layers off" : "layers on"));
        _layerButton.TooltipText = "Choose a map layer (o cycles)";
        layerHead.AddChild(_layerButton);
        // ⚠ A FLOW AND NOT A BOX, for the same reason the console itself is one: at 480 px the four
        // shipping washes do not fit on a line, and a box would have run them off the edge rather
        // than onto a second row.
        _layerChoices = new HFlowContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,

            // ⚠ EXPAND, or the box beside it hands the flow its MINIMUM width -- which is one
            // button -- and four washes come back as four rows.
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _layerChoices.AddThemeConstantOverride("h_separation", 6);
        _layerChoices.AddThemeConstantOverride("v_separation", 6);
        foreach ((string name, string label) in Washes)
        {
            string want = name;
            var choice = ConsoleButton(label, () => Apply(new DriveCommand(
                _world.Tick.Raw, DriveVerb.Overlay, 0, want)));
            _layerChoices.AddChild(choice);
        }
        layerHead.AddChild(_layerChoices);
        _layerGroup.AddChild(layerHead);
        var legend = new VBoxContainer();
        legend.AddThemeConstantOverride("separation", 4);
        _legendTitle = ConsoleLabel(string.Empty, 11);
        // ⚠ A STATED WIDTH AND NOT AN EXPANDING ONE. A ramp drawn the width of the console reads as
        // a rule between two rows rather than as a scale, which is the opposite of what a legend is
        // for -- it was 1,300 px in the drawings before anybody noticed.
        _legendRamp = new LegendRamp
        {
            CustomMinimumSize = new Vector2(300, 7),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _legendBody = ConsoleLabel(string.Empty, 13);
        _legendBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _legendBody.CustomMinimumSize = new Vector2(300, 0);
        _legendBody.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        legend.AddChild(_legendTitle);
        legend.AddChild(_legendRamp);
        legend.AddChild(_legendBody);
        _layerGroup.AddChild(legend);
        _consoleTop.AddChild(_layerGroup);

        // ---- chrome -----------------------------------------------------------------------------
        var trim = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        trim.AddThemeConstantOverride("separation", 6);
        _themeButton = ConsoleButton("Light", () => Ui(_lightUi ? "theme dark" : "theme light"));
        _debugButton = ConsoleButton("Debug", () => Ui(_debugShown ? "debug off" : "debug on"));
        _toolsButton = ConsoleButton("Tools", () => Ui(_toolsShown ? "tools off" : "tools on"));
        trim.AddChild(_themeButton);
        trim.AddChild(_debugButton);
        trim.AddChild(_toolsButton);
        _consoleTop.AddChild(trim);

        _consoleBody.AddChild(_consoleTop);

        // ---- the pointer reading, and any refusal ------------------------------------------------
        _pointerRow = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _pointerRow.AddThemeConstantOverride("separation", 9);
        _pointerHead = ConsoleLabel("UNDER POINTER", 11);
        _pointerHead.CustomMinimumSize = new Vector2(112, 0);
        _hover.LabelSettings = null;
        _hover.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _pointerRow.AddChild(_pointerHead);
        _pointerRow.AddChild(_hover);
        _consoleBody.AddChild(_pointerRow);

        _refusalRow = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var refusal = new VBoxContainer();
        refusal.AddThemeConstantOverride("separation", 3);
        var refusalHead = ConsoleLabel("REFUSED", 11);
        _refusalLabel = ConsoleLabel(string.Empty, 13);
        _refusalLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        refusal.AddChild(refusalHead);
        refusal.AddChild(_refusalLabel);
        _refusalRow.AddChild(refusal);
        _consoleBody.AddChild(_refusalRow);

        _console.AddChild(_consoleBody);
    }

    /// <summary>The layer picker's entries. <b>The two debug washes appear only with the debug overlay.</b></summary>
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

    /// <summary>A console button: the shell's one button style, at the console's smaller height.</summary>
    /// <remarks>
    /// ⚠ <b><see cref="Control.SizeFlags.ShrinkCenter"/> vertically, and it is not cosmetic.</b> A
    /// <see cref="Control"/> in a box fills the row by default, and the console's rows are as tall
    /// as their tallest member — so a Fill button beside a two-line label became a two-line-tall
    /// button, and beside a wrapped one it became the height of the console.
    /// </remarks>
    private static Button ConsoleButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 30),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        button.Pressed += action;
        return button;
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
    private static Label ConsoleLabel(string text, int size)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    /// <summary>
    /// The two console groups whose widths are stated, restated against the console it is in.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A STATED MINIMUM WIDER THAN THE WINDOW IS AN OVERFLOW AND NOT A WRAP.</b> The tool tray
    /// and the legend both name a width so the flow wraps around them rather than clipping them —
    /// and at 480 px each of those numbers is wider than the console itself, so the group ran off
    /// the right edge and took the pointer reading with it. ***A minimum is a request and the window
    /// is the authority***, so both are capped here every frame rather than chosen once.
    /// </remarks>
    private void SizeConsole(float inner)
    {
        // ⚠ The pointer's fixed caption goes at the minimum window. It costs 112 px of a 416 px
        // row to say what the sentence beside it already reads as, and the sentence wraps to two
        // lines to pay for it.
        _pointerHead.Visible = inner > 440f;
        _toolSlot.CustomMinimumSize = new Vector2(Math.Min(560f, inner), 0f);
        _legendBody.CustomMinimumSize = new Vector2(Math.Min(300f, inner), 0f);
        _legendRamp.CustomMinimumSize = new Vector2(Math.Min(300f, inner), 7f);
        _hover.CustomMinimumSize = new Vector2(Math.Max(120f, inner - (_pointerHead.Visible ? 124f : 0f)), 0f);
    }

    /// <summary>What the current rung is called.</summary>
    private string RungName() => Rungs[_rung];

    /// <summary>
    /// How long a Day is at this rung. <b>Kept beside the rung and not folded into a tooltip.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b><see cref="Pace"/>'s remark is the reason this is on screen.</b> The multiple of real
    /// time was misleading on its own — <em>338× real time</em> invites a person to expect tomorrow
    /// shortly, and the Day counter looked stuck. ***A speed is a rate and a person waiting is
    /// holding a duration***, so the duration is the visible half and the multiple is the tooltip.
    /// </remarks>
    private string DayLength()
    {
        // ⚠ EMPTY AND NOT "paused". The rung beside it already says the word, and the two labels
        // reading it together was one state saying its own name twice.
        if (Ladder[_rung] <= 0.0) return string.Empty;
        int seconds = (int)Math.Round(Ticks.PerDay / Ladder[_rung]);
        return seconds < 60 ? $"a Day in {seconds}s" : $"a Day in {seconds / 60}m{seconds % 60:00}s";
    }

    /// <summary>
    /// The named phase of the Day, which is what <c>01 §7</c> asks for instead of a clock.
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
    /// <para>
    /// ⚠ <b>The legend's sentence is <see cref="Legend"/>'s, split rather than rewritten.</b> A
    /// second wording of the same three facts is <c>plans/0012</c> <b>Cause 1</b> — the copy that
    /// drifts. The split is at the first em dash, which is where <see cref="Legend"/> already puts
    /// the boundary between the layer's name and what its colours are worth.
    /// </para>
    /// </remarks>
    private void RefreshConsole()
    {
        _rungLabel.Text = RungName();
        _rungLabel.TooltipText = Pace(_rung);
        _dayLengthLabel.Text = DayLength();
        _dayLengthLabel.TooltipText = Pace(_rung);
        _pauseButton.Text = _rung == 0 ? "▶" : "⏸";
        _slowerButton.Disabled = _rung <= 1;
        _fasterButton.Disabled = _rung >= Ladder.Length - 1;

        int minute = Ticks.MinuteOfDay(_world.Tick.Raw);
        _skyArc.Minute = minute;
        _skyArc.QueueRedraw();
        _dayLabel.Text = $"Day {_world.Tick.Raw / (ulong)Ticks.PerDay} · {PhaseOfDay(minute)}"
            + $"  {minute / 60:00}:{minute % 60:00}";

        _layerButton.Text = _washing == Wash.None
            ? "Layers  ▾"
            : $"{Washes.First(w => w.Name == WashName(_washing)).Label}  ▾";
        _layerChoices.Visible = _layersShown;
        int shown = 0;
        foreach (Node node in _layerChoices.GetChildren())
        {
            if (node is not Button choice) continue;
            choice.Visible = !DebugWash(Washes[shown].Name) || _debugShown;
            choice.ButtonPressed = Washes[shown].Name == WashName(_washing);
            shown++;
        }

        string legend = Legend().TrimStart('\n');
        bool washing = legend.Length > 0;
        _legendTitle.Visible = washing;
        _legendRamp.Visible = washing && _washing != Wash.Rung;
        _legendBody.Visible = washing;
        if (washing)
        {
            int dash = legend.IndexOf('—');
            _legendTitle.Text = (dash < 0 ? legend : legend[..dash]).Trim().ToUpperInvariant();
            _legendBody.Text = dash < 0 ? string.Empty : legend[(dash + 1)..].Trim();
            _legendRamp.Colours = Bands;
            _legendRamp.QueueRedraw();
        }

        _refusalRow.Visible = _refused.Length > 0;
        if (_refused.Length > 0) _refusalLabel.Text = _refused;
    }

    /// <summary>The <see cref="Wash"/>'s name in the drive grammar, which is the console's key too.</summary>
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
}
