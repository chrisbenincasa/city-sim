using System;
using System.IO;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

/// <summary>
/// The shell: a camera over the city, and the simulation stepping under it.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>The scene is built in code and the <c>.tscn</c> holds one node.</b> Everything drawn here
/// is derived from the world every frame, so an authored scene would be a second description of the
/// city that could disagree with the first.
/// </para>
/// <para>
/// ⚠ <b>One <see cref="MultiMeshInstance3D"/> per kind of thing, not per Chunk.</b> <c>05 §2</c>
/// asks for per-Chunk and that is a claim about a million Citizens; at the sizes this shell has ever
/// been run at it would be structure with nothing behind it.
/// </para>
/// <para>
/// <b>The clock is the host's and the Tick is not.</b> <c>05</c>'s fixed-Tick/interpolated-render
/// split lives in <see cref="_Process"/>: wall time accumulates, whole Ticks are stepped from it,
/// and the leftover is the <c>alpha</c> handed to <see cref="VisibleAgents"/>.
/// </para>
/// </remarks>
public partial class Main : Node3D
{
    /// <summary>Metres per Tile, so the camera can be placed in something a human reads.</summary>
    private const float MetresPerTile = 4f;

    /// <summary>
    /// How far the eye is tipped above the ground — <b>the isometric angle, and it never moves.</b>
    /// </summary>
    /// <remarks>
    /// <c>atan(1/sqrt(2))</c>, which is the angle at which a cube's three visible faces are drawn
    /// equal. The camera is a perspective one, so this is the framing rather than the projection.
    /// </remarks>
    private const float PitchRadians = 0.61547971f;

    /// <summary>A quarter turn of the compass, which is what one press of the rotate key is.</summary>
    private const float YawStepRadians = Mathf.Pi * 0.25f;

    /// <summary>What one notch of zoom in multiplies the eye's standoff by.</summary>
    private const float DollyPerStep = 0.92f;

    /// <summary>How wide the carriageway is drawn. A drawing width, and no Segment states one.</summary>
    private const float RoadWidthMetres = 8f;

    /// <summary>
    /// How much of its own frontage a Building fills, leaving the rest as the gap to its neighbour.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THIS REPLACED A FLAT 6 METRES, AND THE 6 WAS WRONG BY A FACTOR OF FOUR.</b> The shell
    /// invented a footprint instead of deriving one, and the result was a Building NARROWER than the
    /// 8-metre carriageway beside it — so a city of them read as a wide road network with specks
    /// along it. ***The simulation had said how wide a Building is all along***:
    /// <c>[roads] block_tiles</c> is how long a Segment is and <c>[lots] lots_per_segment</c> is how
    /// many Buildings share it, which at the shipped 32 and 5 is <b>25.6 m of frontage each</b>
    /// against a road three times narrower.
    /// </remarks>
    private const float FrontageFill = 0.85f;

    /// <summary>
    /// How deep a Building is drawn, as a share of its frontage. <b>Invented, and it has to be.</b>
    /// </summary>
    /// <remarks>
    /// <b>A Lot has no depth and there is no depth key</b> (<c>adr/0078</c>) — a Lot is an address
    /// point on a Segment, and how far back the building goes is a fact the city does not hold. So
    /// this is the renderer inventing a thickness, exactly as the setback does, and it is labelled
    /// as one so nobody promotes it to a Ruleset key. ⚠ <b>It must stay below 1</b>, or a Building
    /// is deeper than it is wide and a row of them stops reading as a street.
    /// </remarks>
    private const float PlotDepthShare = 0.55f;

    /// <summary>How tall one storey is drawn. A drawing height, and no kind states one.</summary>
    private const float StoreyMetres = 3.5f;

    /// <summary>
    /// The tallest a Building is drawn before jitter — <b>what the setback is derived against.</b>
    /// </summary>
    /// <remarks>
    /// A shipped kind declares <c>occupants = 3</c>, so this is the height the shell was framed and
    /// lit for. It is not a cap: a kind declaring more is drawn taller.
    /// </remarks>
    private const float BuildingHeightMetres = 3f * StoreyMetres;

    /// <summary>What a Building in use is drawn as. Warm stone, and the shell's original.</summary>
    private static readonly Color Standing = new(0.55f, 0.52f, 0.45f);

    /// <summary>
    /// What an <b>abandoned</b> Building is drawn as — <b>the state the picture could not show.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A ruin looked exactly like a house until this landed, and a flood is what made that
    /// unbearable.</b> In the frame at Tick 6,101 on <c>flooded.toml</c> the water is unmistakable
    /// and the <b>235 ruined Buildings standing in it are indistinguishable from the dry ones on the
    /// bank</b>. The readout said the number; the picture did not. ***That is the same shape as the
    /// hexadecimal <c>CLAUDE.md</c>'s Definition of done was amended over*** — a state the city knows
    /// and the eye cannot find.
    /// </para>
    /// <para>
    /// ⚠ <b>It is <see cref="BuildingTable.IsAbandoned"/> and not <em>flooded</em>, which is wider
    /// than the thing that prompted it and deliberately so.</b> A Building abandoned by
    /// <c>adr/0053</c>'s failure pressure and one ruined by a flood are the same state — <c>02
    /// §4.3</c>'s derelict — and the renderer has no business knowing which verb put it there. The
    /// visible consequence is that <c>declining.toml</c> now greys out as it decays, which nobody
    /// asked for and is the point: ***one colour, every mechanism that reaches the state.***
    /// </para>
    /// <para>
    /// ⚠ <b>Desaturated as well as darkened.</b> Darker alone reads as shadow at this sun angle, and
    /// the boxes are already lit from one side.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>Both colours are converted with <c>SrgbToLinear</c> at the write site, and the first
    /// spelling was not.</b> A MultiMesh instance colour is multiplied into albedo in <b>linear</b>
    /// space, so an sRGB value written straight through renders far brighter than it reads —
    /// <see cref="Standing"/>'s warm stone came out near white and the contrast this exists to
    /// create was most of the way washed out. ***The colours were right and the space they were
    /// written in was not***, which looks in a screenshot exactly like a badly chosen palette.
    /// </para>
    /// </remarks>
    private static readonly Color Derelict = new(0.20f, 0.19f, 0.18f);

    /// <summary>How thick the water is drawn. Flat, and floating just clear of the ground.</summary>
    private const float WaterMetres = 0.6f;

    /// <summary>A Cell's side in the shell's metres, which is what one water quad covers.</summary>
    private const float CellMetres = CellGrid.TilesPerCell * MetresPerTile;


    /// <summary>In-world seconds a Tick is worth, which is what turns a rung into a rate.</summary>
    private const double SecondsPerTick = 86_400.0 / Ticks.PerDay;

    /// <summary>
    /// The speed ladder, in Ticks a second. 🔴 <b>A PLAYTEST INSTRUMENT AND NOT A RATIFIED LADDER.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>This disagrees with <c>01 §1</c> on purpose, and <c>01 §1</c> is still the design.</b>
    /// Every rung here is <b>half</b> that table's Ticks/s, so 1× is 8 rather than 16 and a Day is
    /// 4m16s rather than 2m08s. The tick rate is host-side and runtime-only (<c>CLAUDE.md</c> →
    /// Constants), so nothing here moves a hash or settles anything. ***It is a dial to play with,
    /// and what it is for is producing the playtest <c>adr/0094</c>'s revisit trigger asks for.***
    /// </para>
    /// <para>
    /// 🔴 <b>THE LADDER'S SLOWEST SHIPPED RUNG IS ~340× TOO FAST TO SEE A PERSON MOVE.</b> A Tick is
    /// 42.19 s of in-world time, so <c>01 §1</c>'s 1× runs the world at <b>675× real time</b>: a
    /// 20-minute commute is <b>1.8 real seconds</b> and a car crosses a 128 m block in <b>0.014 s</b>,
    /// under a frame at 60 Hz. Visual truthfulness is 1× <em>real</em> time — <b>0.0237 Ticks/s</b>.
    /// Filed against <c>01 §1</c> and <c>§7</c> in <c>plans/0012</c>.
    /// </para>
    /// <para>
    /// <b>1 Tick/s is the rung at which a walker is watchable</b> — a block in 2.2 s, a commute in
    /// 28 s, a Day in 34 minutes — and it is kept as <c>1/8×</c> because it is the one a person
    /// actually looked at and liked.
    /// </para>
    /// </remarks>
    private static readonly double[] Ladder = [0.0, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0, 24.0, 32.0];

    /// <summary>What each rung is called. <c>1×</c> is this shell's, at half <c>01 §1</c>'s rate.</summary>
    private static readonly string[] Rungs =
        ["paused", "1/16x", "1/8x", "1/4x", "0.5x", "1x", "2x", "3x", "4x"];

    /// <summary>The rung <c>space</c> returns to, and the one a fresh shell opens at.</summary>
    private const int DesignSpeed = 5;

    private Simulation _simulation = null!;
    private World _world = null!;
    private MultiMeshInstance3D _buildings = null!;
    private MultiMeshInstance3D _travellers = null!;
    private MultiMeshInstance3D _roads = null!;
    private MultiMeshInstance3D _ground = null!;

    private MultiMeshInstance3D _water = null!;
    private MultiMeshInstance3D _flood = null!;
    private Label _readout = null!;
    private VisibleAgent[] _agents = new VisibleAgent[8192];
    private double _owed;
    private int _rung = DesignSpeed;
    private int _resume = DesignSpeed;
    /// <summary>Frames drawn since the shell opened. Read only by the screenshot trigger.</summary>
    private int _frame;
    private string _rulesetPath = "rulesets/minimal.toml";
    private Camera3D _camera = null!;
    private float _span = 512f;

    /// <summary>The ground point the eye orbits. A drag moves this and nothing else does.</summary>
    private Vector3 _focus;

    /// <summary>How far the eye stands off the focus, bounded by <see cref="Dolly"/>.</summary>
    private float _distance = 512f;

    /// <summary>Which corner the city is seen from, clockwise from due south in radians.</summary>
    private float _yaw = YawStepRadians;

    public override void _Ready()
    {
        (_rulesetPath, int citizens, ulong startAt) = Arguments();

        string path = Path.IsPathRooted(_rulesetPath)
            ? _rulesetPath
            : ProjectSettings.GlobalizePath($"res://../../{_rulesetPath}");

        if (!File.Exists(path))
        {
            GD.PrintErr($"no Ruleset at {path}. Pass one after Godot's own --, as in:");
            GD.PrintErr("  godot --path src/Borough.Godot -- --ruleset rulesets/congested.toml "
                + "--citizens 4000");
            GetTree().Quit(2);

            return;
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(
            File.ReadAllText(path), Path.GetFileName(path));

        if (loaded.Ruleset is null)
        {
            GD.PrintErr(loaded.Describe());
            GetTree().Quit(2);

            return;
        }

        var key = WorldKey.FromSeed(0);

        _world = new World(citizens, loaded.Ruleset, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(_world, key, new Ticks(0));

        // FAST-FORWARD BEFORE THE FIRST FRAME, and it is not a rung. The ladder is what a person
        // watches at; this is how they get to the part worth watching. A flood on flooded.toml
        // begins at Tick 4,096, which at the top rung is two and a half minutes of staring at a dry
        // city -- and on a machine with no screen it is the difference between a photograph of a
        // flood and a photograph of the coast.
        //
        // ⚠ IT STEPS THE SIMULATION AND SKIPS NOTHING. Every Tick runs, which is why it is slow and
        // why it is correct: a world jumped to is a different world (adr/0003), and the whole point
        // of a shell is to look at the one the headless runner would produce.
        for (ulong tick = 0; tick < startAt; tick++)
        {
            _simulation.Step(default);
        }

        // THE SEA FIRST AND THE FLOOD ON TOP OF IT, and the order is the draw order. A Hazard
        // Region Cell is dry ground that a flood reaches, so the two never cover the same Cell --
        // 🔴 THE GROUND IS PAINTED FIRST AND IT IS NOT DECORATION. Without it dry land is the
        // BACKGROUND -- the viewport's clear colour -- so a block's interior reads as a hole rather
        // than as land, the only thing on screen with a surface is the carriageway, and an 8-metre
        // Street beside a 128-metre block looks like the widest thing in the city. ***Two separate
        // readings of this shell as broken traced back to the same absence***: a flood covering the
        // frame read as the camera drifting out to sea, and a correctly-scaled Street read as huge.
        _ground = Layer(new Color(0.16f, 0.17f, 0.14f), new Vector3(1f, 1f, 1f));

        // but they sit at almost the same height, and painting the standing water last is what makes
        // a rising tide read as arriving rather than as flickering.
        _water = Layer(new Color(0.10f, 0.22f, 0.42f), new Vector3(1f, 1f, 1f));
        _flood = Layer(new Color(0.20f, 0.48f, 0.78f), new Vector3(1f, 1f, 1f));
        _roads = Layer(new Color(0.30f, 0.30f, 0.33f), new Vector3(1f, 0.1f, 1f));

        // A UNIT BOX, with the size composed per instance rather than baked into the mesh, which is
        // what lets one draw call hold Buildings of different shapes.
        _buildings = Layer(Standing, Vector3.One, perInstance: true);
        _travellers = Layer(new Color(1.0f, 0.45f, 0.15f), new Vector3(3f, 3f, 3f));

        Ground();
        Flood();
        Pave();
        Sun();
        Look();
        Readout();
    }

    public override void _Process(double delta)
    {
        _owed += delta * Ladder[_rung];

        while (_owed >= 1.0)
        {
            _simulation.Step(default);
            _owed -= 1.0;
        }

        Draw(new Ratio((int)(_owed * 65_536)));

        // A picture of the frame, for a machine with no screen. Nothing reads it back.
        //
        // ⚠ TRIGGERED ON THE WORLD'S TICK AND NOT ON A FRAME COUNT, so two Rulesets photographed at
        // the same number are photographed at the same moment in the city rather than after the same
        // amount of the operator's patience.
        // ⚠ AND ON THE THIRD FRAME AT THE EARLIEST, WHICH IS NOT PEDANTRY. --start-at does its
        // fast-forwarding in _Ready, so with it the world is already past the trigger when the
        // FIRST frame is drawn -- and a Control added to a CanvasLayer this frame has not been laid
        // out yet, so the readout is absent from the picture. Two photographs of a flood were taken
        // with no caption on them before anybody noticed the panel was missing rather than empty.
        // ***The one thing in the frame that says which Tick it is, is the thing a first-frame
        // capture drops.***
        if (System.Environment.GetEnvironmentVariable("BOROUGH_SHOT") is { } shot
            && _frame++ >= 2
            && _world.Tick.Raw >= ulong.Parse(
                System.Environment.GetEnvironmentVariable("BOROUGH_SHOT_AT") ?? "750"))
        {
            RenderingServer.ForceDraw();

            // ⚠ THE VIEWPORT HAS NO TEXTURE UNDER --headless, and this block is the one thing in
            // the shell written for a machine with no screen. Reaching through the null threw
            // before the Print and before the Quit, so the run neither said it had arrived nor
            // stopped -- it spewed a stack trace every frame until something killed it, and a
            // timing run against it read the killer's timeout back as the answer.
            if (GetViewport().GetTexture() is { } texture)
            {
                texture.GetImage().SavePng(shot);
                GD.Print($"wrote {shot} at Tick {_world.Tick.Raw}");
            }
            else
            {
                GD.Print($"no viewport texture; no picture written at Tick {_world.Tick.Raw}");
            }

            GetTree().Quit();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } click)
        {
            if (click.ButtonIndex == MouseButton.WheelUp)
            {
                Dolly(1f);
            }
            else if (click.ButtonIndex == MouseButton.WheelDown)
            {
                Dolly(-1f);
            }

            return;
        }

        // A trackpad reaches none of that. macOS turns a two-finger scroll into a pan gesture and a
        // pinch into a magnify gesture, so the wheel branch above never sees a finger at all.
        if (@event is InputEventPanGesture scroll)
        {
            // Delta is the wheel's own quantity negated by the platform, so up is negative here.
            Dolly(-scroll.Delta.Y * 0.5f);

            return;
        }

        if (@event is InputEventMagnifyGesture pinch)
        {
            // Factor is a ratio about 1, and a pinch arrives as many small ones rather than one big.
            Dolly((pinch.Factor - 1f) * 8f);

            return;
        }

        // Dragging with any button held pans across the ground, never through it, so the city
        // cannot be lost behind the camera by a careless drag. ⚠ It runs along the EYE's axes and
        // not the world's, because a turn parts the two and a drag would then go off at an angle.
        if (@event is InputEventMouseMotion drag && drag.ButtonMask != 0)
        {
            // Scaled by the standoff rather than the city, so a close-up pans a street at a time.
            float reach = _distance * 0.002f;
            var right = new Vector3(Mathf.Cos(_yaw), 0f, -Mathf.Sin(_yaw));
            var ahead = new Vector3(-Mathf.Sin(_yaw), 0f, -Mathf.Cos(_yaw));

            _focus += (right * -drag.Relative.X + ahead * drag.Relative.Y) * reach;

            Orbit();

            return;
        }

        if (@event is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        // The camera's keys, which move the eye and never the clock, so they leave before the rung.
        switch (key.Keycode)
        {
            case Key.Q:
            case Key.E:
                // Modulo rather than accumulation: a session is long and a yaw is an angle.
                _yaw = (_yaw + (key.Keycode == Key.Q ? -YawStepRadians : YawStepRadians))
                    % (Mathf.Pi * 2f);

                Orbit();

                return;

            case Key.Equal:
            case Key.KpAdd:
                Dolly(4f);

                return;

            case Key.Minus:
            case Key.KpSubtract:
                Dolly(-4f);

                return;
        }

        // Pause is a rung rather than a separate state (01 §1), so it remembers what it left.
        _rung = key.Keycode switch
        {
            Key.Space => _rung == 0 ? _resume : 0,
            Key.Bracketleft => Math.Max(1, _rung - 1),
            Key.Bracketright => Math.Min(Ladder.Length - 1, _rung + 1),
            Key.Key1 => DesignSpeed,
            Key.Key2 => DesignSpeed + 1,
            Key.Key3 => DesignSpeed + 2,
            Key.Key4 => DesignSpeed + 3,
            Key.Escape => Quit(),
            _ => _rung,
        };

        if (_rung != 0)
        {
            _resume = _rung;
        }
    }

    /// <summary>
    /// Change the eye's standoff rather than the field of view: a zoom that moves the eye keeps
    /// the perspective the picture was framed with. One step is a wheel notch, and a gesture
    /// arrives as a stream of fractions of one.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A step is a RATIO and not a distance.</b> A fixed distance a notch is the same nudge
    /// over the whole city as it is over one street, so the last notches in cross the ground and
    /// come out underneath it — which is what the bounds here are for.
    /// </remarks>
    private void Dolly(float steps)
    {
        _distance = Mathf.Clamp(
            _distance * Mathf.Pow(DollyPerStep, steps), Nearest(), Furthest());

        Orbit();
    }

    /// <summary>
    /// <c>--ruleset PATH</c>, <c>--citizens N</c> and <c>--start-at TICK</c>, after Godot's <c>--</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A shell reads the command line and the core does not.</b> Every string here is this
    /// project's (<c>adr/0002</c>), and a bad one is reported rather than defaulted, because a
    /// silently-substituted world is a picture of somewhere else.
    /// </remarks>
    private static (string Ruleset, int Citizens, ulong StartAt) Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;
        ulong startAt = 0;
        string[] given = OS.GetCmdlineUserArgs();

        for (int at = 0; at + 1 < given.Length; at++)
        {
            if (given[at] == "--ruleset")
            {
                ruleset = given[at + 1];
            }
            else if (given[at] == "--citizens"
                && int.TryParse(given[at + 1], out int asked) && asked > 0)
            {
                citizens = asked;
            }
            else if (given[at] == "--start-at"
                && ulong.TryParse(given[at + 1], out ulong from))
            {
                startAt = from;
            }
        }

        return (ruleset, citizens, startAt);
    }

    /// <summary>Reads the world into the two meshes that change, and writes the readout.</summary>
    private void Draw(Ratio alpha)
    {
        int drawn = Fill(_buildings, Buildings());
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);
        int under = Fill(_flood, Inundated());

        Fill(_travellers, Travellers(moving));

        ulong tick = _world.Tick.Raw;
        ulong ofDay = tick % (ulong)Ticks.PerDay;

        _readout.Text =
            $"{System.IO.Path.GetFileName(_rulesetPath)}   Tick {tick:N0}   "
            + $"Day {tick / (ulong)Ticks.PerDay}   "
            + $"{ofDay * 24 / (ulong)Ticks.PerDay:00}:{ofDay * 1440 / (ulong)Ticks.PerDay % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"travelling {moving:N0}{Weather(under)}\n"
            + $"speed {Pace(_rung)}   "
            + "[ ] speed, space pause, 1-4, drag pan, q/e turn, -/= or wheel zoom, esc quit";
    }

    /// <summary>
    /// What a rung is called, <b>how long it makes a Day</b>, and its multiple of real time.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>THE DAY'S LENGTH IS HERE BECAUSE THE MULTIPLE OF REAL TIME WAS MISLEADING ON ITS OWN.</b>
    /// <c>1×</c> is 8 Ticks/s and reads as <em>338× real time</em>, which is correct and invites a
    /// person to expect tomorrow shortly; a Day is 2048 Ticks and therefore <b>4m16s</b>, and the
    /// reassuring number is the one that was on screen. It was read as the <c>Day</c> counter being
    /// stuck. ***A speed is a rate and a person waiting is holding a duration***, which is
    /// <c>adr/0059</c>'s *state the duration, derive the rate* arriving in a readout — and
    /// <c>adr/0094</c>'s reason for a Day being a sampling rate rather than a length of life.
    /// </remarks>
    private static string Pace(int rung)
    {
        if (Ladder[rung] <= 0.0)
        {
            return Rungs[rung];
        }

        int seconds = (int)Math.Round(Ticks.PerDay / Ladder[rung]);
        string day = seconds < 60 ? $"{seconds}s" : $"{seconds / 60}m{seconds % 60:00}s";

        return $"{Rungs[rung]} — a Day in {day}, {Ladder[rung] * SecondsPerTick:N0}x real time";
    }

    /// <summary>Every standing Building, at its Lot, at the size its kind implies.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>THE HEIGHT IS DERIVED FROM THE KIND AND THE JITTER IS THE RENDERER'S.</b>
    /// <c>[[building]] occupants</c> is how many Households a Building of that kind holds
    /// (<c>adr/0068</c>), so it is the one thing the city already says about how big a Building is —
    /// and every shipped kind declares <b>3</b>, so today the derivation buys nothing visible and
    /// the variation you see is all jitter. ***That is the point of deriving it anyway***: the day a
    /// Ruleset declares a kind that holds thirty, the shell draws a tower without being told to.
    /// </para>
    /// <para>
    /// <b>The jitter is <see cref="PlotDepthShare"/>' class of thing and is labelled as one</b> — a
    /// thickness the city does not have, invented so the picture reads as a city rather than as a
    /// bar chart. It is keyed on the Building's monotonic row id, so a Building keeps its shape for
    /// as long as it stands and a rebuilt one on the same Lot is visibly a different building.
    /// ⚠ <b>It draws on no <c>purpose_tag</c> and must not</b>: the simulation's stream is for
    /// decisions, and a shape nobody in the city can perceive is not one.
    /// </para>
    /// </remarks>
    /// <summary>
    /// How much of a Segment one Building gets, in metres — <b>derived from the Ruleset and never
    /// chosen here</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Address: <em>five Buildings share a Segment</em>. A Segment is one block
    /// edge, so its length is <c>[roads] block_tiles</c> and the Buildings on it are
    /// <c>[lots] lots_per_segment</c> — and the frontage each one gets is the first divided by the
    /// second. At the shipped 32 and 5 that is <b>25.6 m</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>Divided by ALL the Lots on the Segment and not by the ones on this side of it.</b> Lots
    /// alternate kerbs (<c>Frontage.SideOf</c> — odd and even house numbering), so consecutive Lots
    /// on one side sit twice this far apart and a Building could be drawn twice this wide. It is
    /// not, for two reasons: the design's sentence is <em>five Buildings share a Segment</em> and
    /// says nothing about sides, and Lots near a block's end are close to the next block's Lots, so
    /// the doubled width overlaps across the junction. ***The narrower reading is both the honest
    /// one and the one that does not intersect.***
    /// </para>
    /// <para>
    /// ⚠ <b>Falls back to one block's width when a Ruleset states no Lots</b>, which is a world with
    /// no Buildings in it — so the value is never actually used, and a division by zero would be the
    /// only thing anybody saw.
    /// </para>
    /// </remarks>
    private static float Frontage(int blockTiles, int lotsPerSegment) =>
        lotsPerSegment > 0
            ? blockTiles * MetresPerTile / lotsPerSegment
            : blockTiles * MetresPerTile;

    private System.Collections.Generic.IEnumerable<(Transform3D Where, Color What)> Buildings()
    {
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;
        int block = _world.Roads.Streets.BlockTiles;
        float frontage = Frontage(block, _world.Rules.Lots.LotsPerSegment);

        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (!table.Rows.IsLive(slot) || !lots.Rows.TryResolve(table.Lot[slot], out int lot))
            {
                continue;
            }

            float east = lots.East[lot].Raw * MetresPerTile;
            float north = lots.North[lot].Raw * MetresPerTile;
            var side = (StreetSide)lots.Side[lot];

            // ⚠ A LOT'S COORDINATE IS A POINT ON THE SEGMENT, NOT A PLOT OF GROUND BESIDE IT.
            // Lots hang on Segments and have no depth (adr/0078), so drawing one where it says it
            // is puts the Building in the carriageway. Which kerb to step to is Side, read the way
            // LotSubdivider.BlockOf writes it: on a horizontal Street Left is the north side, on a
            // vertical one Right is the east side.
            bool horizontal = block > 0 && lots.North[lot].Raw % block == 0;

            // Half the carriageway plus half the building's own depth, so the near face clears the
            // road by construction rather than by a constant that happened to be big enough.
            float setback = (RoadWidthMetres * 0.5f) + (frontage * PlotDepthShare * 0.5f);

            if (horizontal)
            {
                north += side == StreetSide.Left ? setback : -setback;
            }
            else
            {
                east += side == StreetSide.Right ? setback : -setback;
            }

            ulong shape = Scramble(table.Rows.IdAt(slot));
            byte kind = table.Kind[slot];
            int storeys = _world.Rules.Declares(kind)
                ? Math.Max(1, _world.Rules.Kind(kind).Occupants)
                : 1;

            // 0.55x to 1.85x on the height, 0.85x to 1.15x on the plan. Three draws off one
            // scramble, taken from different bit ranges so a tall Building is not also a fat one.
            float tall = storeys * StoreyMetres * (0.55f + ((shape & 0xFFu) / 255f * 1.3f));
            float along = frontage * FrontageFill
                * (0.85f + (((shape >> 8) & 0xFFu) / 255f * 0.3f));
            float deep = frontage * PlotDepthShare
                * (0.85f + (((shape >> 16) & 0xFFu) / 255f * 0.3f));

            // The long side runs ALONG the Street, which is what makes a row of them read as a
            // street rather than as a field of blocks -- so the plan is swapped with the axis the
            // setback above already had to know about.
            Vector3 plan = horizontal
                ? new Vector3(along, tall, deep)
                : new Vector3(deep, tall, along);

            yield return (
                new Transform3D(Basis.FromScale(plan), new Vector3(east, tall * 0.5f, -north)),
                (table.IsAbandoned(slot) ? Derelict : Standing).SrgbToLinear());
        }
    }

    /// <summary>
    /// One slab under the whole map, so that dry land is a surface rather than the absence of one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One instance, laid once, and it never changes</b> — the map is a fixed size
    /// (<c>CellGrid.WorldTiles</c>), so this is a single box rather than a Cell grid. A per-Cell
    /// ground would be 262,144 instances to say one thing.
    /// </para>
    /// <para>
    /// ⚠ <b>It sits BELOW the water and below the carriageway</b>, at a depth chosen to clear both
    /// without z-fighting: the sea is drawn at <see cref="WaterMetres"/> from zero and the roads at a
    /// tenth of a metre, so the ground's top face is under the lower of the two.
    /// </para>
    /// <para>
    /// ⚠ <b>It carries no terrain and must not be read as any.</b> <c>rulesets/varied.toml</c> is the
    /// only shipped file that states <c>[[terrain]]</c>, and nothing here asks it — this is one flat
    /// colour, and the day the ground means something it becomes a per-Cell layer that reads it.
    /// </para>
    /// </remarks>
    private void Ground()
    {
        const float depth = 4f;
        float side = CellGrid.WorldTiles * MetresPerTile;

        _ground.Multimesh.InstanceCount = 1;
        _ground.Multimesh.SetInstanceTransform(
            0,
            new Transform3D(
                Basis.FromScale(new Vector3(side, depth, side)),
                new Vector3(side * 0.5f, -depth * 0.5f, -side * 0.5f)));

        // ⚠ WITHOUT THIS THE SLAB IS PLACED AND NOT DRAWN. A MultiMesh has a buffer size and a
        // visible count, Layer leaves the second at zero, and Fill is what normally raises it -- so
        // a layer written by hand rather than filled from an enumeration silently renders nothing.
        _ground.Multimesh.VisibleInstanceCount = 1;
    }

    /// <summary>Every Cell a Disaster has under water right now.</summary>
    /// <remarks>
    /// <b>Refilled every frame, unlike <see cref="Flood"/>'s sea</b> — these rows are created as a
    /// surge rises and freed as it recedes, which is the whole of what there is to watch.
    /// </remarks>
    private System.Collections.Generic.IEnumerable<Transform3D> Inundated()
    {
        InundationTable wet = _world.Inundations;

        for (int slot = 0; slot < wet.Rows.SlotCount; slot++)
        {
            if (wet.Rows.IsLive(slot))
            {
                yield return Tile(wet.East[slot], wet.North[slot], WaterMetres * 1.4f);
            }
        }
    }

    /// <summary>One Cell-sized flat slab, centred on the Cell.</summary>
    private static Transform3D Tile(Cells east, Cells north, float height) =>
        new(
            Basis.FromScale(new Vector3(CellMetres, height, CellMetres)),
            new Vector3(
                (east.Raw + 0.5f) * CellMetres,
                height * 0.5f,
                -(north.Raw + 0.5f) * CellMetres));

    /// <summary>
    /// A 64-bit mix, so that neighbouring row ids do not produce neighbouring shapes.
    /// </summary>
    /// <remarks>
    /// <b>splitmix64's finaliser.</b> Row ids are allocated in sequence, and the low bits of a
    /// counter are a terrible source of variety — a street of Buildings created one after another
    /// would step through the jitter range in order and read as a ramp rather than as a city.
    /// </remarks>
    private static ulong Scramble(ulong id)
    {
        ulong mixed = id + 0x9E3779B97F4A7C15UL;

        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;

        return mixed ^ (mixed >> 31);
    }

    /// <summary>Every Traveller the last query placed.</summary>
    private System.Collections.Generic.IEnumerable<Transform3D> Travellers(int found)
    {
        for (int agent = 0; agent < found; agent++)
        {
            yield return new Transform3D(
                Basis.Identity,
                new Vector3(
                    _agents[agent].East.Raw * MetresPerTile / 65_536f,
                    4f,
                    -_agents[agent].North.Raw * MetresPerTile / 65_536f));
        }
    }

    /// <summary>Writes transforms into a MultiMesh and returns how many there were.</summary>
    /// <remarks>
    /// ⚠ <b>A whole transform and not a position, since Buildings vary in size.</b> The scale is
    /// composed with <see cref="Basis.FromScale"/> in the instance's own frame — <c>Basis.Scaled</c>
    /// scales in the PARENT frame, which is what drew the first road network as north–south lines
    /// with no cross-streets.
    /// </remarks>
    private static int Fill(
        MultiMeshInstance3D into,
        System.Collections.Generic.IEnumerable<(Transform3D Where, Color What)> places)
    {
        int painted = 0;

        foreach ((Transform3D where, Color what) in places)
        {
            if (painted >= into.Multimesh.InstanceCount)
            {
                break;
            }

            into.Multimesh.SetInstanceTransform(painted, where);
            into.Multimesh.SetInstanceColor(painted++, what);
        }

        into.Multimesh.VisibleInstanceCount = painted;

        return painted;
    }

    /// <inheritdoc cref="Fill(MultiMeshInstance3D, System.Collections.Generic.IEnumerable{ValueTuple{Transform3D, Color}})"/>
    /// <summary>The same, for a layer whose colour belongs to the layer rather than the box.</summary>
    private static int Fill(
        MultiMeshInstance3D into, System.Collections.Generic.IEnumerable<Transform3D> places)
    {
        int count = 0;

        foreach (Transform3D place in places)
        {
            if (count >= into.Multimesh.InstanceCount)
            {
                break;
            }

            into.Multimesh.SetInstanceTransform(count++, place);
        }

        into.Multimesh.VisibleInstanceCount = count;

        return count;
    }

    /// <summary>
    /// The sea, laid once — <b>the first thing the shell has ever drawn that is not the city.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>adr/0157</c> makes the water graph generator output, computed from a height field that is
    /// then thrown away, so a Water Body's Cells are written before the first Tick and never move.
    /// ⚠ <b>Only <c>coastal.toml</c> and <c>flooded.toml</c> have any</b>; every other shipped world
    /// is inland and this lays nothing.
    /// </para>
    /// <para>
    /// ⚠ <b>The Hazard Region is NOT drawn, and that is a gap rather than a decision.</b>
    /// <c>01 §5.3</c> wants the floodplain shown as an ordinary overlay from the first Tick — it is
    /// the posted price that makes riverside land a decision rather than an ambush — and the shell
    /// has no overlay machinery at all. So what you can see here is the water and the flood, and
    /// <b>not the risk</b>. <c>--flood</c> prints the exposure as a number in the meantime.
    /// </para>
    /// </remarks>
    private void Flood()
    {
        WaterCellTable cells = _world.WaterCells;
        int drawn = 0;

        for (int slot = 0;
             slot < cells.Rows.SlotCount && drawn < _water.Multimesh.InstanceCount;
             slot++)
        {
            if (cells.Rows.IsLive(slot))
            {
                _water.Multimesh.SetInstanceTransform(
                    drawn++, Tile(cells.East[slot], cells.North[slot], WaterMetres));
            }
        }

        _water.Multimesh.VisibleInstanceCount = drawn;
    }

    /// <summary>What the weather is doing, or nothing at all when it is doing nothing.</summary>
    /// <remarks>
    /// ⚠ <b>Silent on a world with no <c>[disasters]</c></b>, which is every shipped Ruleset but
    /// <c>flooded.toml</c> — a readout that said <c>floods 0</c> for ever would be a permanent line
    /// about an absent mechanism.
    /// </remarks>
    private string Weather(int under)
    {
        if (!_world.Rules.Disasters.Stated)
        {
            return string.Empty;
        }

        int live = _world.Disasters.Rows.LiveCount;
        int ruined = 0;
        int swept = 0;

        for (int slot = 0; slot < _world.Disasters.Rows.SlotCount; slot++)
        {
            if (_world.Disasters.Rows.IsLive(slot))
            {
                ruined += _world.Disasters.Ruined[slot];
                swept += _world.Disasters.Swept[slot];
            }
        }

        return live == 0
            ? "   no flood"
            : $"   FLOOD — {under:N0} Cells under water, {ruined:N0} ruined, {swept:N0} swept";
    }

    /// <summary>The Road Graph, laid once — the lattice is generated before the first frame.</summary>
    private void Pave()
    {
        RoadSegmentTable segments = _world.Roads.Segments;
        RoadNodeTable nodes = _world.Roads.Nodes;
        int drawn = 0;

        for (int slot = 0; slot < segments.Rows.SlotCount && drawn < _roads.Multimesh.InstanceCount;
             slot++)
        {
            if (!segments.Rows.IsLive(slot)
                || !nodes.Rows.TryResolve(segments.NodeA[slot], out int a)
                || !nodes.Rows.TryResolve(segments.NodeB[slot], out int b))
            {
                continue;
            }

            var from = new Vector3(
                nodes.East[a].Raw * MetresPerTile, 0f, -nodes.North[a].Raw * MetresPerTile);
            var to = new Vector3(
                nodes.East[b].Raw * MetresPerTile, 0f, -nodes.North[b].Raw * MetresPerTile);

            if (from.IsEqualApprox(to))
            {
                continue;
            }

            // Scaled along its own length and turned to face the other end: one unit cube per
            // Segment, which is why the Road Graph costs one draw call however large the city is.
            //
            // ⚠ THE SCALE IS COMPOSED IN THE SEGMENT'S OWN FRAME AND NOT THE WORLD'S. Basis.Scaled
            // scales the basis in the PARENT frame, so the first spelling gave every east-west
            // Segment 8 m of length and its whole length of width -- and the lattice rendered as
            // north-south lines with the cross-streets missing. Invisible in the ASCII dump, which
            // rasterises the line itself and never asks for a transform.
            var basis = new Basis(Quaternion.FromEuler(
                    new Vector3(0f, Mathf.Atan2(to.X - from.X, to.Z - from.Z), 0f)))
                * Basis.FromScale(
                    new Vector3(RoadWidthMetres, 1f, from.DistanceTo(to)));

            _roads.Multimesh.SetInstanceTransform(
                drawn++, new Transform3D(basis, from.Lerp(to, 0.5f)));
        }

        _roads.Multimesh.VisibleInstanceCount = drawn;
    }

    /// <summary>One MultiMesh of boxes, coloured, sized, and ready to be filled per frame.</summary>
    /// <remarks>
    /// ⚠ <b><paramref name="perInstance"/> makes the layer's colour a property of each box rather
    /// than of the layer</b>, which needs three things set together: the mesh's albedo goes to white
    /// so the instance colour is not tinted by it, the material reads the instance colour as albedo,
    /// and the MultiMesh is told to carry one. ⚠ <b><c>UseColors</c> is set BEFORE
    /// <c>InstanceCount</c></b> — Godot allocates the instance buffer on the count, and a format
    /// changed afterwards is a resize the engine declines to do.
    /// </remarks>
    private MultiMeshInstance3D Layer(Color colour, Vector3 size, bool perInstance = false)
    {
        var mesh = new BoxMesh { Size = size };
        var material = new StandardMaterial3D
        {
            AlbedoColor = perInstance ? Colors.White : colour,
            VertexColorUseAsAlbedo = perInstance,
            Roughness = 0.9f,
        };

        mesh.Material = material;

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = perInstance,
        };

        multi.Mesh = mesh;
        multi.InstanceCount = 65_536;
        multi.VisibleInstanceCount = 0;

        var node = new MultiMeshInstance3D { Multimesh = multi };

        AddChild(node);

        return node;
    }

    /// <summary>A light, so the boxes have faces rather than silhouettes.</summary>
    private void Sun()
    {
        var light = new DirectionalLight3D { LightEnergy = 1.1f };

        light.RotateX(-1.1f);
        light.RotateY(-0.6f);
        AddChild(light);

        var sky = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.09f, 0.10f, 0.13f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.35f, 0.38f, 0.45f),
                AmbientLightEnergy = 0.7f,
            },
        };

        AddChild(sky);
    }

    /// <summary>A camera over the middle of what the city actually laid.</summary>
    /// <remarks>
    /// ⚠ <b>Framed on the EXTENT and not on the far corner.</b> A lattice may sit at the map's
    /// origin, and a camera pulled back by the largest coordinate then frames the empty map rather
    /// than the city standing in one corner of it.
    /// </remarks>
    private void Look()
    {
        LotTable lots = _world.Lots;
        float east = float.MaxValue;
        float north = float.MaxValue;
        float eastEnd = float.MinValue;
        float northEnd = float.MinValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            east = Mathf.Min(east, lots.East[slot].Raw * MetresPerTile);
            north = Mathf.Min(north, lots.North[slot].Raw * MetresPerTile);
            eastEnd = Mathf.Max(eastEnd, lots.East[slot].Raw * MetresPerTile);
            northEnd = Mathf.Max(northEnd, lots.North[slot].Raw * MetresPerTile);
        }

        if (east > eastEnd)
        {
            east = north = 0f;
            eastEnd = northEnd = 512f;
        }

        float span = Mathf.Max(256f, Mathf.Max(eastEnd - east, northEnd - north));
        var centre = new Vector3((east + eastEnd) * 0.5f, 0f, -(north + northEnd) * 0.5f);

        _span = span;
        _focus = centre;
        _distance = span * 0.95f;
        _camera = new Camera3D { Far = 200_000f, Fov = 60f };

        AddChild(_camera);
        Orbit();
    }

    /// <summary>
    /// Put the eye where the yaw, the pitch and the distance say, looking at the focus.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The eye is derived from the orbit and never read back out of it.</b> Every verb moves
    /// one of the three fields and calls this, so a rotation cannot drift the framing and a zoom
    /// cannot leave the camera pointing somewhere a pan did not put it.
    /// </remarks>
    private void Orbit()
    {
        float flat = Mathf.Cos(PitchRadians) * _distance;

        _camera.LookAtFromPosition(
            _focus + new Vector3(
                Mathf.Sin(_yaw) * flat, Mathf.Sin(PitchRadians) * _distance, Mathf.Cos(_yaw) * flat),
            _focus,
            Vector3.Up);
    }

    /// <summary>The nearest the eye may stand, which is close enough to read one Building.</summary>
    private float Nearest() => Mathf.Max(32f, _span * 0.02f);

    /// <summary>The furthest, which is the whole city and a margin, never the whole map.</summary>
    private float Furthest() => _span * 3f;

    /// <summary>The panel, which is every string a human reads (<c>adr/0002</c>).</summary>
    private void Readout()
    {
        _readout = new Label
        {
            Position = new Vector2(16f, 12f),
            LabelSettings = new LabelSettings { FontSize = 18 },
        };

        var layer = new CanvasLayer();

        layer.AddChild(_readout);
        AddChild(layer);
    }

    private int Quit()
    {
        GetTree().Quit();

        return _rung;
    }
}
