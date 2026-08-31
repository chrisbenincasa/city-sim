using System;
using System.Globalization;
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
    /// as one so nobody promotes it to a Ruleset key. ⚠ <b>It is metres and NOT a share of the
    /// frontage</b>, which is what it was until it was found to make every density look the same —
    /// see <see cref="Depth"/>.
    /// </remarks>
    private const float PlotDepthMetres = 12f;

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
    private PanelContainer _tuner = null!;

    private LineEdit[] _fields = [];

    private Label _tunerStatus = null!;

    /// <summary>The Ruleset's TEXT, kept because the tuner rewrites text and re-parses it.</summary>
    private string _toml = string.Empty;

    private int _citizens;

    private ulong _seed;

    private MultiMeshInstance3D _cells = null!;

    /// <summary>The ground the city actually stands on, in metres, from the last framing.</summary>
    private Rect2 _laid;

    private MultiMeshInstance3D _ground = null!;

    private MultiMeshInstance3D _water = null!;
    private MultiMeshInstance3D _flood = null!;
    private MultiMeshInstance3D _hazard = null!;
    private Label _readout = null!;
    private VisibleAgent[] _agents = new VisibleAgent[8192];
    private double _owed;
    private int _rung = DesignSpeed;
    private int _resume = DesignSpeed;
    /// <summary>Frames drawn since the shell opened. Read only by the screenshot trigger.</summary>
    private int _frame;
    private string _rulesetPath = "rulesets/minimal.toml";

    /// <summary>The script this run is driven by, in Tick order. Empty when nobody is driving.</summary>
    private DriveCommand[] _drive = [];

    /// <summary>How far into <see cref="_drive"/> the run has got.</summary>
    private int _next;

    /// <summary>This frame's interpolation, kept so a written caption can be recomposed.</summary>
    private Ratio _alpha;

    /// <summary>Set once the run has been refused, so no frame draws a world nobody built.</summary>
    private bool _stopping;

    /// <summary>Which Building each drawn instance is, in instance order. See <see cref="DrawList"/>.</summary>
    private readonly List<ulong> _buildingIds = [];

    /// <summary>Which Citizen each drawn Traveller is, in instance order.</summary>
    private readonly List<ulong> _travellerIds = [];
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
        (_rulesetPath, int citizens, ulong startAt, string? drive, ulong quitAt) = Arguments();

        if (!Driven(drive, quitAt))
        {
            Stop(2);

            return;
        }

        string path = Globalize(_rulesetPath);

        if (!File.Exists(path))
        {
            GD.PrintErr($"no Ruleset at {path}. Pass one after Godot's own --, as in:");
            GD.PrintErr("  godot --path src/Borough.Godot -- --ruleset rulesets/congested.toml "
                + "--citizens 4000");
            Stop(2);

            return;
        }

        _toml = File.ReadAllText(path);

        RulesetLoadResult loaded = RulesetLoader.Parse(_toml, Path.GetFileName(path));

        if (loaded.Ruleset is null)
        {
            GD.PrintErr(loaded.Describe());
            Stop(2);

            return;
        }

        _citizens = citizens;
        _seed = 0;

        var key = WorldKey.FromSeed(_seed);

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

        // 01 §5.3'S POSTED PRICE, and it is drawn UNDER everything the city puts on top of it --
        // under the roads at a tenth of a metre and under the sea at WaterMetres. That is the whole
        // point of the height: the risk is a property of the GROUND, so a Street laid across a
        // floodplain must read as a Street on a floodplain and not as a floodplain interrupted.
        _hazard = Layer(new Color(0.34f, 0.25f, 0.17f), new Vector3(1f, 1f, 1f));

        // but they sit at almost the same height, and painting the standing water last is what makes
        // a rising tide read as arriving rather than as flickering.
        _water = Layer(new Color(0.10f, 0.22f, 0.42f), new Vector3(1f, 1f, 1f));
        _flood = Layer(new Color(0.20f, 0.48f, 0.78f), new Vector3(1f, 1f, 1f));
        _roads = Layer(new Color(0.30f, 0.30f, 0.33f), new Vector3(1f, 0.1f, 1f));

        // OFF by default. It is an instrument rather than scenery -- it answers "how big is a Cell
        // against this Building", and a person who has not asked that question does not want a
        // 128-metre lattice drawn over their city.
        _cells = Layer(new Color(0.55f, 0.45f, 0.25f), new Vector3(1f, 0.1f, 1f));
        _cells.Visible = false;

        // A UNIT BOX, with the size composed per instance rather than baked into the mesh, which is
        // what lets one draw call hold Buildings of different shapes.
        _buildings = Layer(Standing, Vector3.One, perInstance: true);
        _travellers = Layer(new Color(1.0f, 0.45f, 0.15f), new Vector3(3f, 3f, 3f));

        Ground();
        Hazard();
        Flood();
        Pave();
        Sun();
        Look();
        Cells();
        Readout();
    }

    public override void _Process(double delta)
    {
        if (_stopping)
        {
            return;
        }

        _owed += delta * Ladder[_rung];

        // 🔴 THE CLOCK IS CLAMPED AT THE NEXT COMMAND'S TICK, AND THAT IS WHAT MAKES A DRIVEN RUN
        // REPRODUCIBLE. A frame steps as many Ticks as the rung and the frame time between them
        // ask for, so a command drained on "the first frame at or past Tick T" lands on a
        // different Tick on a different machine, at a different rung, or under a different load --
        // which is plans/0048 F4, the defect tier 1 could not fix. Stepping no further than T
        // makes the Tick a command lands on a property of the SCRIPT rather than of the host.
        ulong until = _next < _drive.Length ? _drive[_next].At : ulong.MaxValue;

        while (_owed >= 1.0 && _world.Tick.Raw < until)
        {
            _simulation.Step(default);
            _owed -= 1.0;
        }

        // ⚠ CLAMPED, because holding the clock at a command's Tick lets the debt run past a whole
        // Tick where the loop above used to guarantee it could not. An alpha over 1 would place a
        // Traveller past the Address it is walking to.
        _alpha = new Ratio((int)(Math.Min(_owed, 0.999_99) * 65_536));

        Draw(_alpha);
        Drive();
    }

    /// <summary>Apply every command the world has reached, in file order.</summary>
    /// <remarks>
    /// ⚠ <b>Nothing runs before the third frame, and that is not pedantry.</b> A <c>Control</c>
    /// added to a <c>CanvasLayer</c> has not been laid out in the frame it was added, so the
    /// readout is absent from a picture taken too early -- and with <c>--start-at</c> the world is
    /// already past the first command when the first frame draws. ***The one thing in a frame that
    /// says which Tick it is, is the thing an early capture drops.*** Two photographs of a flood
    /// were taken with no caption before anybody noticed the panel was missing rather than empty.
    /// <b>Nothing is lost by waiting</b>: the clock is held at the command's Tick until it runs.
    /// </remarks>
    private void Drive()
    {
        if (_frame++ < 2)
        {
            return;
        }

        while (_next < _drive.Length && _world.Tick.Raw >= _drive[_next].At)
        {
            Apply(_drive[_next++]);
        }
    }

    /// <summary>
    /// Read the drive script, and hang <c>--quit-at</c> off the end of it as one more command.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>--quit-at</c> is a command and not a second mechanism</b>, which answers
    /// <c>plans/0048</c> <b>D3</b>: <c>--drive</c> does NOT imply an end, because a script a person
    /// is watching should keep running when it runs out of instructions. Wanting an end means
    /// writing <c>quit</c> or passing the flag, and both arrive at the same applier.
    /// </remarks>
    private bool Driven(string? script, ulong quitAt)
    {
        List<DriveCommand> commands = [];

        if (script is not null)
        {
            string path = Globalize(script);

            if (!File.Exists(path))
            {
                GD.PrintErr($"no drive script at {path}.");

                return false;
            }

            DriveScriptResult read = DriveScript.Parse(
                File.ReadAllText(path), Path.GetFileName(path));

            if (read.Commands is null)
            {
                GD.PrintErr(read.Describe());

                return false;
            }

            commands.AddRange(read.Commands);
        }

        if (quitAt > 0)
        {
            if (commands.Count > 0 && commands[^1].At > quitAt)
            {
                GD.PrintErr($"--quit-at {quitAt:N0} is before the script's last command at Tick "
                    + $"{commands[^1].At:N0}, so that command could never run.");

                return false;
            }

            if (DriveScript.Stopped(commands) && commands.Count > 0 && quitAt > commands[^1].At)
            {
                GD.PrintErr($"the script stops the clock at Tick {commands[^1].At:N0}, so "
                    + $"--quit-at {quitAt:N0} is never reached. Resume before the end.");

                return false;
            }

            commands.Add(new DriveCommand(quitAt, DriveVerb.Quit, 0, null));
        }

        _drive = [.. commands];

        return true;
    }

    /// <summary>
    /// <b>The one control surface.</b> The keyboard and the script both arrive here.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Every verb is an ABSOLUTE and the keyboard is what toggles.</b> <c>g</c> reads the
    /// carriageway's current visibility and asks for the opposite; a script says <c>roads off</c>
    /// and means it whatever ran before. Two appliers would drift, and the one that drifted would
    /// be the one nobody presses.
    /// </remarks>
    private void Apply(DriveCommand command)
    {
        switch (command.Verb)
        {
            case DriveVerb.Pause:
                _rung = 0;

                break;

            case DriveVerb.Resume:
                _rung = _resume;

                break;

            case DriveVerb.Speed:
                // Clamped rather than refused: the ladder's length is the shell's own fact and the
                // format cannot know it, so the parser checks the shape and this checks the range.
                _rung = Math.Clamp(command.Amount, 0, Ladder.Length - 1);

                break;

            case DriveVerb.Roads:
                _roads.Visible = command.Amount != 0;

                break;

            case DriveVerb.Cells:
                _cells.Visible = command.Amount != 0;

                break;

            case DriveVerb.Turn:
                // Modulo rather than accumulation: a session is long and a yaw is an angle.
                _yaw = (_yaw + (command.Amount * YawStepRadians)) % (Mathf.Pi * 2f);

                Orbit();

                break;

            case DriveVerb.Zoom:
                Dolly(command.Amount);

                break;

            case DriveVerb.Shoot:
                Shoot(command.Path!);

                break;

            case DriveVerb.Readout:
                Caption(command.Path!);

                break;

            case DriveVerb.Draw:
                DrawList(command.Path!);

                break;

            case DriveVerb.Quit:
                Quit();

                break;
        }

        // Pause is a rung rather than a separate state (01 §1), so it remembers what it left.
        if (_rung != 0)
        {
            _resume = _rung;
        }
    }

    /// <summary>Write the frame, and the readout beside it under the same name.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The picture and the caption are written in ONE act</b>, so they cannot disagree about
    /// which Tick they are of. A frame filed beside a readout taken a moment later is a frame whose
    /// numbers belong to a different city.
    /// </para>
    /// <para>
    /// ⚠ <b>THE VIEWPORT HAS NO PICTURE UNDER <c>--headless</c>.</b> <c>GetTexture()</c> is not null
    /// there -- what is empty is what the dummy renderer has behind its RID, which surfaces as an
    /// engine error and a null out of <c>GetImage()</c>. ***A guard that checks the handle misses an
    /// emptiness that is in what the handle points at***, so the display server is asked first,
    /// where the capability is actually declared. <b>The caption is still written</b>: a run with no
    /// screen can still say what the city was doing.
    /// </para>
    /// </remarks>
    private void Shoot(string path)
    {
        string full = Globalize(path);

        // 🔴 REDRAWN BEFORE THE SHUTTER, BECAUSE Draw RAN BEFORE THIS FRAME'S COMMANDS DID. Found
        // by running it: 'pause' then 'shoot' on one Tick wrote a caption reading "speed 1x", which
        // is the rung the frame was composed with and not the one the picture was taken at.
        // ***A caption written from a stale compose is a caption that disagrees with its own
        // picture***, which is the one thing writing them in a single act was supposed to prevent.
        Draw(_alpha);
        RenderingServer.ForceDraw();

        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: --headless renders nothing.");
        }
        else if (GetViewport().GetTexture()?.GetImage() is { } picture)
        {
            picture.SavePng(full);
            GD.Print($"wrote {path} at Tick {_world.Tick.Raw}");
        }
        else
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: the viewport rendered nothing.");
        }

        // Written rather than captioned: Draw has already run above, and a second compose would
        // be a second reading of a world that has not moved.
        Write(Path.ChangeExtension(path, ".txt"));
    }

    /// <summary>
    /// Write the readout, which is <b>the whole of what a driven run returns without a picture</b>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The Tick is on its own first line rather than only inside the readout's prose.</b> The
    /// readout is written to be read by a person at 18pt; this file is read by whatever is driving,
    /// and a caller should not have to parse an em-dash to find out which Tick it got.
    /// </remarks>
    private void Caption(string path)
    {
        Draw(_alpha);
        Write(path);
    }

    /// <summary>
    /// Write <b>everything on screen, as data</b> — one row per drawn instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ONLY QUESTION THE SHELL CAN ANSWER THAT THE HEADLESS RUNNER CANNOT.</b>
    /// Every number in the readout comes from the core — <c>VisibleAgents.In</c> and the tables
    /// under it — so a driven shell reporting world state reports what <c>Borough.Headless</c>
    /// already reports. ***What only exists here is the DERIVATION***: the transform and the colour
    /// this shell decided on. <c>plans/0045</c> holds the defect that makes the case:
    /// <c>Basis.Scaled</c> scales in the parent frame, so every east–west Segment drew 8 m long and
    /// its own length wide, and <b>half the road network was missing in a picture nobody could
    /// assert on</b>. A row saying a Segment is 8 long and 128 wide fails a test.
    /// </para>
    /// <para>
    /// ⚠ <b>The geometry is read back off the <c>MultiMesh</c> and not from the iterator that filled
    /// it.</b> A dump built by walking the tables a second time would be a second derivation, free
    /// to agree with the world while disagreeing with the picture — which is the entire class of
    /// defect this exists to catch. ***The mesh is what the GPU is handed, so the mesh is what is
    /// asked.***
    /// </para>
    /// <para>
    /// ⚠ <b>Identity comes the other way, from the fill</b>, because a <c>MultiMesh</c> knows only
    /// an instance index and an index is a recycled slot rather than a name. Two layers know an
    /// entity; the rest draw ground and honestly say <c>-</c>.
    /// </para>
    /// <para>
    /// ⚠ <b><c>instances</c> against <c>capacity</c> is not bookkeeping.</b> A layer at capacity has
    /// silently dropped whatever did not fit, and the picture shows a smaller city with nothing to
    /// say so — the one failure a screenshot renders as success.
    /// </para>
    /// </remarks>
    private void DrawList(string path)
    {
        // Recomposed for Caption's reason: Draw ran before this frame's commands did.
        Draw(_alpha);

        var text = new System.Text.StringBuilder();

        text.Append(CultureInfo.InvariantCulture, $"tick\t{_world.Tick.Raw}\n");
        text.Append(
            CultureInfo.InvariantCulture,
            $"ruleset\t{Path.GetFileName(_rulesetPath)}\n");
        text.Append("# layer\tname\tinstances\tcapacity\tvisible\n");

        foreach ((string name, MultiMeshInstance3D layer, bool _, List<ulong>? _) in Layers())
        {
            text.Append(
                CultureInfo.InvariantCulture,
                $"layer\t{name}\t{layer.Multimesh.VisibleInstanceCount}\t"
                    + $"{layer.Multimesh.InstanceCount}\t{(layer.Visible ? 1 : 0)}\n");
        }

        // 🔴 UNDER --headless EVERY TRANSFORM READS BACK AS THE IDENTITY, AND THE FILE STILL LOOKS
        // RIGHT. The counts above are CPU-side and stay true, so a headless dump came out with the
        // correct number of rows, the correct layers and 21,504 lines of zeros -- ***structurally
        // valid and silently wrong***, which is the one failure that survives being eyeballed.
        // ⚠ It is the same shape as the screenshot's headless defect that plans/0045 records twice:
        // the handle is fine and the emptiness is in what the handle points at. So the rows are
        // WITHHELD rather than written, because a file that exists is a file somebody parses.
        if (DisplayServer.GetName() == "headless")
        {
            text.Append(
                "# rows withheld: --headless renders nothing, and a MultiMesh under the dummy "
                    + "renderer returns the identity for every instance. The counts above are "
                    + "CPU-side and are true. A draw list needs a real display.\n");

            File.WriteAllText(Globalize(path), text.ToString());
            GD.Print($"wrote {path} at Tick {_world.Tick.Raw}, counts only: --headless has no geometry.");

            return;
        }

        text.Append("# row\tlayer\tindex\tid\tx\ty\tz\tsx\tsy\tsz\tyaw\tr\tg\tb\n");

        foreach ((string name, MultiMeshInstance3D layer, bool colours, List<ulong>? ids)
            in Layers())
        {
            MultiMesh mesh = layer.Multimesh;

            for (int at = 0; at < mesh.VisibleInstanceCount; at++)
            {
                Transform3D where = mesh.GetInstanceTransform(at);
                Vector3 size = where.Basis.Scale;
                Color paint = colours ? mesh.GetInstanceColor(at) : default;

                text.Append(
                    CultureInfo.InvariantCulture,
                    $"row\t{name}\t{at}\t{(ids is not null && at < ids.Count ? ids[at].ToString(CultureInfo.InvariantCulture) : "-")}\t"
                        + $"{Figure(where.Origin.X)}\t{Figure(where.Origin.Y)}\t{Figure(where.Origin.Z)}\t"
                        + $"{Figure(size.X)}\t{Figure(size.Y)}\t{Figure(size.Z)}\t"
                        + $"{Figure(where.Basis.GetEuler().Y)}\t"
                        + $"{(colours ? $"{Figure(paint.R)}\t{Figure(paint.G)}\t{Figure(paint.B)}" : "-\t-\t-")}\n");
            }
        }

        File.WriteAllText(Globalize(path), text.ToString());
        GD.Print($"wrote {path} at Tick {_world.Tick.Raw}");
    }

    /// <summary>Every layer, in draw order, with whether it paints per instance and who it is.</summary>
    private (string Name, MultiMeshInstance3D Layer, bool Colours, List<ulong>? Ids)[] Layers() =>
    [
        ("ground", _ground, false, null),
        ("hazard", _hazard, false, null),
        ("water", _water, false, null),
        ("flood", _flood, false, null),
        ("road", _roads, false, null),
        ("cell", _cells, false, null),
        ("building", _buildings, true, _buildingIds),
        ("traveller", _travellers, false, _travellerIds),
    ];

    /// <summary>
    /// A number in a row. <b>Fixed, invariant and rounded</b>, so two runs diff rather than differ.
    /// </summary>
    private static string Figure(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Put the readout on disk as it stands. Composed by the caller.</summary>
    private void Write(string path) =>
        File.WriteAllText(Globalize(path), $"tick {_world.Tick.Raw}\n{_readout.Text}\n");

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

        // The tuner's two keys, which are an editing UI rather than a view of the city and are
        // therefore the only keys with no verb behind them. A script has no panel to open.
        switch (key.Keycode)
        {
            case Key.Tab:
                _tuner.Visible = !_tuner.Visible;

                return;

            case Key.Enter:
            case Key.KpEnter:
                // Only while the panel is open, so the key is free everywhere else.
                if (_tuner.Visible)
                {
                    Regenerate();
                }

                return;
        }

        // 🔴 A KEY TOGGLES AND A COMMAND IS ABSOLUTE, SO THE CURRENT STATE IS READ HERE AND NEVER
        // IN Apply. g asks for the opposite of what the carriageway is doing; space asks to resume
        // if the clock is stopped. That reading is the whole difference between the two surfaces,
        // and keeping it on this side is what leaves exactly one applier to go wrong.
        //
        // ⚠ The Tick is the world's rather than zero, and it is unused today. It is what tier 4
        // needs: a socket that stamps each arriving command with the Tick it landed on writes a
        // drive script, which is what makes an interactive session replayable as a batch one.
        DriveCommand? command = key.Keycode switch
        {
            Key.Q => Made(DriveVerb.Turn, -1),
            Key.E => Made(DriveVerb.Turn, 1),
            Key.G => Made(DriveVerb.Roads, _roads.Visible ? 0 : 1),
            Key.C => Made(DriveVerb.Cells, _cells.Visible ? 0 : 1),
            Key.Equal or Key.KpAdd => Made(DriveVerb.Zoom, 4),
            Key.Minus or Key.KpSubtract => Made(DriveVerb.Zoom, -4),
            Key.Space => Made(_rung == 0 ? DriveVerb.Resume : DriveVerb.Pause),
            Key.Bracketleft => Made(DriveVerb.Speed, Math.Max(1, _rung - 1)),
            Key.Bracketright => Made(DriveVerb.Speed, Math.Min(Ladder.Length - 1, _rung + 1)),
            Key.Key1 => Made(DriveVerb.Speed, DesignSpeed),
            Key.Key2 => Made(DriveVerb.Speed, DesignSpeed + 1),
            Key.Key3 => Made(DriveVerb.Speed, DesignSpeed + 2),
            Key.Key4 => Made(DriveVerb.Speed, DesignSpeed + 3),
            Key.Escape => Made(DriveVerb.Quit),
            _ => null,
        };

        if (command is not null)
        {
            Apply(command.Value);
        }

        DriveCommand Made(DriveVerb verb, int amount = 0) =>
            new(_world.Tick.Raw, verb, amount, null);
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
    private static (string Ruleset, int Citizens, ulong StartAt, string? Drive, ulong QuitAt)
        Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;
        ulong startAt = 0;
        string? drive = null;
        ulong quitAt = 0;
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
            else if (given[at] == "--drive")
            {
                drive = given[at + 1];
            }
            else if (given[at] == "--quit-at"
                && ulong.TryParse(given[at + 1], out ulong until))
            {
                quitAt = until;
            }
        }

        return (ruleset, citizens, startAt, drive, quitAt);
    }

    /// <summary>
    /// Refuse to run, <b>and stop this frame as well as the next one</b>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>GetTree().Quit()</c> IS DEFERRED TO THE END OF THE FRAME, so <c>_Process</c> still
    /// runs once against a world <c>_Ready</c> never built</b> — and the refusal a person is meant
    /// to read scrolls past under a <c>NullReferenceException</c> and forty lines of Godot
    /// backtrace. ⚠ <b>This was already true of both Ruleset refusals</b>; the drive script only
    /// made it happen often enough to notice. ***A message printed above a stack trace is a message
    /// nobody reads***, which is <c>adr/0015</c>'s standard for a refusal failing at the last step.
    /// </remarks>
    private void Stop(int code)
    {
        _stopping = true;

        GetTree().Quit(code);
    }

    /// <summary>A path as the operator typed it, resolved against the repository root.</summary>
    /// <remarks>
    /// ⚠ <b>The shell's working directory is the Godot project and the operator's is the
    /// repository</b>, which is why <c>--ruleset rulesets/minimal.toml</c> works at all. A written
    /// file resolves the same way, so a script and its output land where they were asked for.
    /// </remarks>
    private static string Globalize(string path) =>
        Path.IsPathRooted(path) ? path : ProjectSettings.GlobalizePath($"res://../../{path}");

    /// <summary>Reads the world into the two meshes that change, and writes the readout.</summary>
    private void Draw(Ratio alpha)
    {
        int drawn = Fill(_buildings, Buildings(), _buildingIds);
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);
        int under = Fill(_flood, Anonymous(Inundated()));

        Fill(_travellers, Travellers(moving), _travellerIds);

        ulong tick = _world.Tick.Raw;
        ulong ofDay = tick % (ulong)Ticks.PerDay;

        _readout.Text =
            $"{System.IO.Path.GetFileName(_rulesetPath)}   Tick {tick:N0}   "
            + $"Day {tick / (ulong)Ticks.PerDay}   "
            + $"{ofDay * 24 / (ulong)Ticks.PerDay:00}:{ofDay * 1440 / (ulong)Ticks.PerDay % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"travelling {moving:N0}{Weather(under)}\n"
            + $"speed {Pace(_rung)}   "
            + "[ ] speed, space pause, 1-4, drag pan, q/e turn, -/= zoom, g roads, c cells, tab tune, esc quit";
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

    /// <summary>
    /// How much kerb one Lot commands, in metres, <b>on its own side of the Street</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CONTEXT.md</c> → Address: <em>five Buildings share a Segment</em>. A Segment is one block
    /// edge, so its length is <c>[roads] block_tiles</c> and the Buildings on it are
    /// <c>[lots] lots_per_segment</c>. 🔴 ⚠ <b>BUT THEY DO NOT ALL STAND ON THE SAME SIDE, AND
    /// DIVIDING BY ALL OF THEM IS WHAT MADE THE CITY LOOK SILLY.</b> <c>Frontage.SideOf</c> sends
    /// them to <i>strictly</i> alternating kerbs — <c>(index &amp; 1)</c> — so consecutive Lots are
    /// never neighbours, and along either kerb the spacing is <b>twice</b>
    /// <c>block_tiles ÷ lots_per_segment</c>. A Building drawn to fill one of those instead of two
    /// left a gap exactly as wide as itself beside every house.
    /// </para>
    /// <para>
    /// ⚠ <b>It was not a tuning error and no constant could have fixed it.</b> The gap was one
    /// frontage wide at every <c>block_tiles</c> and every <c>lots_per_segment</c>, because both
    /// cancel out of the ratio — which is why the city looked equally sparse at every density it
    /// was asked for, and why turning the dials in the tuner never helped.
    /// </para>
    /// <para>
    /// 🔴 ⚠ <b>THE PREVIOUS VERSION OF THIS COMMENT ARGUED AGAINST THE DOUBLING AND ITS SECOND
    /// REASON WAS CORRECT.</b> <c>Frontage.OffsetOf</c> puts the outermost Lots
    /// <c>block ÷ (2 × lots)</c> from each end — 12.8 m at the shipped numbers — so a Building
    /// filling the doubled width would run <em>through</em> the junction and into the cross street.
    /// That is a reason to clamp against the Segment's end and not a reason to halve every Building
    /// in the city: see <see cref="Buildings"/>, where the width is the lesser of this and the room
    /// actually left. ***The corner plots come out narrow, which is what corner plots are.***
    /// </para>
    /// <para>
    /// ⚠ <b>Falls back to one block's width when a Ruleset states no Lots</b>, which is a world with
    /// no Buildings in it — so the value is never actually used, and a division by zero would be the
    /// only thing anybody saw.
    /// </para>
    /// </remarks>
    private static float Frontage(int blockTiles, int lotsPerSegment) =>
        lotsPerSegment > 0
            ? 2f * blockTiles * MetresPerTile / lotsPerSegment
            : blockTiles * MetresPerTile;

    /// <summary>How far back from the kerb a Building reaches, in metres.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 ⚠ <b>THIS USED TO BE A SHARE OF THE FRONTAGE AND THAT WAS THE DEFECT.</b> Tying depth to
    /// width fixes a Building's <i>shape</i>, so narrowing the Lots shrank every building instead
    /// of terracing them — a street of ten narrow houses came out as ten small sheds with the same
    /// gaps between them, and the city looked identical at every density it was asked for. ***A
    /// real terrace house is 6 m wide and 12 m deep***; the two are independent and the renderer
    /// now says so.
    /// </para>
    /// <para>
    /// ⚠ <b>It is the shell's invention and the city holds no such number</b> — a Lot has no depth
    /// and there is no depth key (<c>adr/0078</c>). What it must not do is reach the Building on
    /// the block's far face, so it is capped against the block rather than trusted.
    /// </para>
    /// </remarks>
    private static float Depth(int blockTiles, ulong shape)
    {
        float wanted = PlotDepthMetres * (0.8f + (((shape >> 16) & 0xFFu) / 255f * 0.6f));
        float room = ((blockTiles * MetresPerTile) - RoadWidthMetres) * 0.5f;

        return room > 4f ? Mathf.Min(wanted, room * 0.8f) : wanted;
    }

    /// <summary>Every standing Building, at its Lot, at the size its kind implies.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>THE HEIGHT IS DERIVED FROM THE KIND AND THE JITTER IS THE RENDERER'S.</b>
    /// <c>[[building]] occupants</c> is how many Households a Building of that kind holds
    /// (<c>adr/0068</c>), so it is the one thing the city already says about how big a Building is.
    /// 🔴 ⚠ <b>THIS SAID "every shipped kind declares 3" AND THAT WAS WRONG</b> — the tuner read the
    /// files and found <b>4</b> in 28 declarations, 3 in three and 1 in three, so the derivation
    /// already varies the picture and <c>CLAUDE.md</c>'s constants table is wrong by the same 3.
    /// ***A number quoted from memory about a file nobody re-read***, which is what the panel was
    /// built to stop. The derivation stands either way: the day a Ruleset declares a kind that holds
    /// thirty, the shell draws a tower without being told to.
    /// </para>
    /// <para>
    /// <b>The jitter is <see cref="PlotDepthMetres"/>' class of thing and is labelled as one</b> — a
    /// thickness the city does not have, invented so the picture reads as a city rather than as a
    /// bar chart. It is keyed on the Building's monotonic row id, so a Building keeps its shape for
    /// as long as it stands and a rebuilt one on the same Lot is visibly a different building.
    /// ⚠ <b>It draws on no <c>purpose_tag</c> and must not</b>: the simulation's stream is for
    /// decisions, and a shape nobody in the city can perceive is not one.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)>
        Buildings()
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

            ulong id = table.Rows.IdAt(slot);
            ulong shape = Scramble(id);
            float deep = Depth(block, shape);

            // Half the carriageway plus half the building's own depth, so the near face clears the
            // road by construction rather than by a constant that happened to be big enough.
            float setback = (RoadWidthMetres * 0.5f) + (deep * 0.5f);

            if (horizontal)
            {
                north += side == StreetSide.Left ? setback : -setback;
            }
            else
            {
                east += side == StreetSide.Right ? setback : -setback;
            }

            byte kind = table.Kind[slot];
            int storeys = _world.Rules.Declares(kind)
                ? Math.Max(1, _world.Rules.Kind(kind).Occupants)
                : 1;

            // 0.55x to 1.85x on the height, 0.85x to 1.15x on the frontage. Each draw takes its
            // own bit range off the one scramble, so a tall Building is not also a fat one.
            float tall = storeys * StoreyMetres * (0.55f + ((shape & 0xFFu) / 255f * 1.3f));
            // ⚠ CLAMPED AGAINST THE SEGMENT'S OWN END, WHICH IS WHAT LETS THE FRONTAGE DOUBLE.
            // Frontage.OffsetOf leaves the outermost Lots half a spacing from the junction, so a
            // Building filling its whole kerb would run into the cross street. Taking the lesser of
            // the two gives wide plots mid-block and narrow ones on the corners.
            int alongTiles = (horizontal ? lots.East[lot].Raw : lots.North[lot].Raw) % block;
            float toEnd = Mathf.Min(alongTiles, block - alongTiles) * MetresPerTile;
            float room = 2f * Mathf.Max(2f, toEnd - (RoadWidthMetres * 0.5f));
            float along = Mathf.Min(
                frontage * FrontageFill * (0.85f + (((shape >> 8) & 0xFFu) / 255f * 0.3f)), room);

            // The long side runs ALONG the Street, which is what makes a row of them read as a
            // street rather than as a field of blocks -- so the plan is swapped with the axis the
            // setback above already had to know about.
            Vector3 plan = horizontal
                ? new Vector3(along, tall, deep)
                : new Vector3(deep, tall, along);

            yield return (
                id,
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
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> Travellers(
        int found)
    {
        for (int agent = 0; agent < found; agent++)
        {
            // ⚠ RESOLVED RATHER THAN READ. A Handle's index is internal to the core on purpose --
            // it is a recycled slot, and 05 §4 folds the monotonic id precisely because the slot
            // is not identity. A draw list keyed on a slot would name a different Citizen after a
            // collection, which is the one thing a list meant for diffing must never do.
            ulong id = _world.Citizens.Rows.TryResolve(_agents[agent].Citizen, out int slot)
                ? _world.Citizens.Rows.IdAt(slot)
                : 0UL;

            yield return (
                id,
                new Transform3D(
                Basis.Identity,
                new Vector3(
                    _agents[agent].East.Raw * MetresPerTile / 65_536f,
                    4f,
                    -_agents[agent].North.Raw * MetresPerTile / 65_536f)));
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
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)> places,
        List<ulong>? ids = null)
    {
        int painted = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D where, Color what) in places)
        {
            if (painted >= into.Multimesh.InstanceCount)
            {
                break;
            }

            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(painted, where);
            into.Multimesh.SetInstanceColor(painted++, what);
        }

        into.Multimesh.VisibleInstanceCount = painted;

        return painted;
    }

    /// <inheritdoc cref="Fill(MultiMeshInstance3D, System.Collections.Generic.IEnumerable{ValueTuple{Transform3D, Color}})"/>
    /// <summary>The same, for a layer whose colour belongs to the layer rather than the box.</summary>
    private static int Fill(
        MultiMeshInstance3D into,
        System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> places,
        List<ulong>? ids = null)
    {
        int count = 0;

        ids?.Clear();

        foreach ((ulong id, Transform3D place) in places)
        {
            if (count >= into.Multimesh.InstanceCount)
            {
                break;
            }

            ids?.Add(id);
            into.Multimesh.SetInstanceTransform(count++, place);
        }

        into.Multimesh.VisibleInstanceCount = count;

        return count;
    }

    /// <summary>
    /// The Hazard Region, laid once — <b><c>01 §5.3</c>'s posted price, and the shell's first
    /// overlay of a thing that has not happened.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Everything else this shell draws is a thing that exists; this is a thing that MIGHT.</b>
    /// <c>01 §5.3</c> asks for the floodplain to be visible from the first Tick, so that riverside
    /// land is a decision the player made rather than an ambush the world sprang — ***a price
    /// somebody paid without being shown it is not a price, it is a surprise.*** The paragraph on
    /// <see cref="Flood"/> called this a gap for as long as it was one.
    /// </para>
    /// <para>
    /// <b>Laid once, for <see cref="Flood"/>'s reason and the same one.</b>
    /// <see cref="FloodCellTable"/> is generator output under <c>adr/0157</c> — written before the
    /// first Tick from a height field that is then thrown away — so a Cell's depth never moves and a
    /// per-frame refill would be the same rows every frame for ever. ⚠ <b>Re-laid on a rebuild</b>
    /// (<c>n</c>, in the tuner), because a new seed is a new coastline.
    /// </para>
    /// <para>
    /// ⚠ <b>The DEPTH is not drawn and that is a decision rather than an omission.</b> A
    /// <see cref="FloodCellTable"/> row's depth is <em>the flood level minus the ground</em>, so a
    /// large one is LOW ground — the polarity that made <c>flooded.toml</c>'s worst-looking seed the
    /// one that ruined nothing. ***A shade ramp on a quantity that reads backwards teaches the wrong
    /// thing faster than no ramp at all***, so this is one flat colour saying <em>at risk</em>, and
    /// <c>--flood</c> keeps the numbers.
    /// </para>
    /// <para>
    /// ⚠ <b>Only <c>coastal.toml</c> and <c>flooded.toml</c> have any</b>, and on every other shipped
    /// world this lays nothing. ⚠ <b>It is bounded by <see cref="Layer"/>'s buffer</b> — 65,536 Cells
    /// against a 262,144-Cell map, so a world with more than a quarter of its ground at risk would
    /// truncate. <see cref="Fill(MultiMeshInstance3D, System.Collections.Generic.IEnumerable{Transform3D})"/>
    /// stops rather than throwing, which is <see cref="MapLayers.LayerCells"/>' disposition, and the
    /// measured worlds sit at 3–9%.
    /// </para>
    /// </remarks>
    private void Hazard() => Fill(_hazard, Anonymous(AtRisk()));

    /// <summary>
    /// Give a run of placements no identity, for the layers that draw <b>ground</b> rather than
    /// entities.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A flooded Cell and a Hazard Region Cell are not rows in a table</b> — they are painted
    /// from a grid, and the honest key for one is where it is, which the row already carries. So
    /// the id column is <c>-</c> for them rather than a number invented to fill it.
    /// </remarks>
    private static System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where)> Anonymous(
        System.Collections.Generic.IEnumerable<Transform3D> places)
    {
        foreach (Transform3D place in places)
        {
            yield return (0UL, place);
        }
    }

    /// <summary>Every Cell a flood could reach, whether or not one ever does.</summary>
    private System.Collections.Generic.IEnumerable<Transform3D> AtRisk()
    {
        FloodCellTable plain = _world.Flood;

        for (int slot = 0; slot < plain.Rows.SlotCount; slot++)
        {
            if (plain.Rows.IsLive(slot))
            {
                // A FIFTH of a road's height, so the overlay sits on the ground rather than on top of
                // the city. Anything at or above 0.1f swallows the carriageway and the floodplain
                // reads as a hole in the road network.
                yield return Tile(plain.East[slot], plain.North[slot], 0.02f);
            }
        }
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
    /// ⚠ <b>This is the water and not the risk.</b> The floodplain is <see cref="Hazard"/>'s, laid
    /// on the same occasion and under this — the two are separate layers because they are separate
    /// claims: ground that <em>is</em> wet, and ground that <em>can be</em>.
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

    /// <summary>The Cell lattice, drawn over the ground the city stands on.</summary>
    /// <remarks>
    /// <para>
    /// <b>A Cell is 32×32 Tiles — 128 m — and it is the unit nearly every derived quantity in the
    /// simulation is denominated in</b>: a Map Layer holds one value per Cell, Stress is per Cell,
    /// the residency index buckets per Cell. So this is the answer to <i>how much of the city does
    /// one number cover?</i>, which is a question you cannot ask a street.
    /// </para>
    /// <para>
    /// ⚠ <b>It is deliberately NOT the street lattice</b>, and the two are easy to confuse because
    /// at the shipped <c>block_tiles = 32</c> they coincide exactly. Turn <c>block_tiles</c> to 16
    /// in the tuner and they part company — four blocks to a Cell — which is the clearest available
    /// demonstration that the road generator and the Cell grid have nothing to do with each other.
    /// </para>
    /// <para>
    /// ⚠ <b>It spans the Lots and not the map.</b> The map is 512 Cells a side, so a full lattice
    /// would be 1,026 lines for a city that occupies eleven of them.
    /// </para>
    /// </remarks>
    private void Cells()
    {
        const float width = 2f;
        const float above = 0.2f;

        int first = Mathf.FloorToInt(_laid.Position.X / CellMetres);
        int last = Mathf.CeilToInt(_laid.End.X / CellMetres);
        int lowest = Mathf.FloorToInt(_laid.Position.Y / CellMetres);
        int highest = Mathf.CeilToInt(_laid.End.Y / CellMetres);

        float left = first * CellMetres;
        float right = last * CellMetres;
        float bottom = lowest * CellMetres;
        float top = highest * CellMetres;

        int drawn = 0;

        for (int column = first; column <= last && drawn + 1 < _cells.Multimesh.InstanceCount;
             column++)
        {
            _cells.Multimesh.SetInstanceTransform(
                drawn++,
                new Transform3D(
                    Basis.FromScale(new Vector3(width, 1f, top - bottom)),
                    new Vector3(column * CellMetres, above, -(bottom + top) * 0.5f)));
        }

        for (int row = lowest; row <= highest && drawn + 1 < _cells.Multimesh.InstanceCount; row++)
        {
            _cells.Multimesh.SetInstanceTransform(
                drawn++,
                new Transform3D(
                    Basis.FromScale(new Vector3(right - left, 1f, width)),
                    new Vector3((left + right) * 0.5f, above, -row * CellMetres)));
        }

        _cells.Multimesh.VisibleInstanceCount = drawn;
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

    /// <summary>Fits the eye's orbit to the city that is actually standing.</summary>
    /// <remarks>
    /// ⚠ <b>Framed on the EXTENT and not on the far corner.</b> A lattice may sit at the map's
    /// origin, and a camera pulled back by the largest coordinate then frames the empty map rather
    /// than the city standing in one corner of it.
    /// </remarks>
    private void Frame()
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
        _laid = new Rect2(east, north, eastEnd - east, northEnd - north);
    }

    /// <summary>
    /// Frames the city and creates the eye. <b>Called once</b> — a regenerate re-frames with
    /// <see cref="Frame"/> and keeps the camera, so a rebuilt world does not arrive with a second
    /// <see cref="Camera3D"/> in the tree and the viewer's own yaw survives the rebuild.
    /// </summary>
    private void Look()
    {
        Frame();

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
        Tuner(layer);
    }

    private int Quit()
    {
        GetTree().Quit();

        return _rung;
    }
    // ---- the tuner ----------------------------------------------------------------------------

    /// <summary>
    /// One number the tuner can turn, named by the table it lives in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><see cref="Table"/> is what makes this safe, and it is not decoration.</b>
    /// <c>candidates</c> is a key in <c>[placement]</c> <em>and</em> in <c>[jobs]</c>, and
    /// <c>interval</c> is in both plus <c>[[zone_rule]]</c> — so a rewriter matching on the key
    /// alone would turn two dials when the viewer asked for one, and the second would be invisible.
    /// </para>
    /// <para>
    /// <b>An empty <see cref="Table"/> means the field is the shell's own</b> — population and seed
    /// are arguments to <c>World</c> rather than anything a Ruleset states, and they are on the
    /// panel because they are the two biggest levers on what you are looking at.
    /// </para>
    /// </remarks>
    private readonly record struct Dial(string Table, string Key, string Label);

    /// <summary>
    /// What the tuner exposes. <b>Eight, chosen because each one changes what you SEE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS A TUNER AND NOT AN EDITOR, AND THE BOUNDARY IS THAT IT ONLY TURNS KEYS THE
    /// FILE ALREADY STATES.</b> A key the Ruleset does not mention is shown greyed and is never
    /// written, because inserting one means knowing which table to put it in, whether that table
    /// exists, and what else becomes required when it does — <c>[districts]</c> alone makes four
    /// keys and every Good's price mandatory. ***An editor is a different program and it is the one
    /// this would grow into.***
    /// </para>
    /// <para>
    /// ⚠ <b><c>occupants</c> is written to EVERY <c>[[building]]</c> in the file</b>, which is the
    /// one field here that is not one-to-one. It is coherent — <em>every kind holds N</em> — and
    /// every shipped file already declares the same number for every kind, but a file that varied
    /// them would be flattened by a single turn of this dial.
    /// </para>
    /// </remarks>
    private static readonly Dial[] Dials =
    [
        new("", "citizens", "citizens"),
        new("", "seed", "world seed"),
        new("[roads]", "block_tiles", "block_tiles"),
        new("[lots]", "lots_per_segment", "lots_per_segment"),
        new("[roads]", "arterial_count", "arterial_count"),
        new("[placement]", "candidates", "placement candidates"),
        new("[[building]]", "occupants", "occupants"),
        new("[households]", "car_ownership_percent", "car_ownership_percent"),
    ];

    /// <summary>
    /// Reads the value a key currently carries, or <c>null</c> when the file does not state it.
    /// </summary>
    private static string? Stated(string toml, string table, string key)
    {
        string? here = null;

        foreach (string line in toml.Split('\n'))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out string value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Whether a line assigns <paramref name="key"/>, and what it assigns.</summary>
    private static bool Names(string line, string key, out string value)
    {
        value = string.Empty;

        int equals = line.IndexOf('=');

        if (equals < 0 || line.StartsWith('#') || line[..equals].Trim() != key)
        {
            return false;
        }

        value = line[(equals + 1)..].Trim();

        return true;
    }

    /// <summary>
    /// The Ruleset text with one key rewritten <b>inside its own table and nowhere else</b>.
    /// </summary>
    private static string Turned(string toml, string table, string key, string to)
    {
        string[] lines = toml.Split('\n');
        string? here = null;

        for (int at = 0; at < lines.Length; at++)
        {
            string trimmed = lines[at].Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out _))
            {
                // The original indentation is kept because these files are column-aligned by hand
                // and a rewriter that reflowed them would make every diff unreadable.
                int equals = lines[at].IndexOf('=');

                lines[at] = string.Concat(lines[at].AsSpan(0, equals + 1), " ", to);
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>Builds the panel. One row per <see cref="Dials"/> entry, hidden until asked for.</summary>
    private void Tuner(CanvasLayer layer)
    {
        var box = new VBoxContainer();

        // A panel the text can be read against. The readout above is white on whatever the city
        // happens to be, which is legible for three lines and not for twelve rows of numbers.
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        _tuner = new PanelContainer { Visible = false, Position = new Vector2(14f, 108f) };

        _tuner.AddThemeStyleboxOverride("panel", backing);

        _fields = new LineEdit[Dials.Length];

        for (int at = 0; at < Dials.Length; at++)
        {
            var row = new HBoxContainer();
            var name = new Label
            {
                Text = Dials[at].Label,
                CustomMinimumSize = new Vector2(210f, 0f),
            };

            string? stated = Dials[at].Table.Length == 0
                ? Own(Dials[at].Key)
                : Stated(_toml, Dials[at].Table, Dials[at].Key);

            var field = new LineEdit
            {
                Text = stated ?? "—",
                Editable = stated is not null,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            _fields[at] = field;

            row.AddChild(name);
            row.AddChild(field);
            box.AddChild(row);
        }

        var apply = new Button { Text = "regenerate  (enter)" };

        apply.Pressed += Regenerate;
        box.AddChild(apply);

        _tunerStatus = new Label { Text = "tab closes. a regenerate is a NEW city, not a reload." };
        box.AddChild(_tunerStatus);

        _tuner.AddChild(box);
        layer.AddChild(_tuner);
    }

    /// <summary>The current value of a field the shell owns rather than the Ruleset.</summary>
    private string Own(string key) => key switch
    {
        "citizens" => _citizens.ToString(),
        "seed" => _seed.ToString(),
        _ => "—",
    };

    /// <summary>
    /// Rewrites the Ruleset text from the panel, re-parses it, and builds a new city from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT GOES BACK THROUGH THE LOADER AND NEVER POKES THE <see cref="Ruleset"/>.</b>
    /// <c>adr/0048</c> puts every one of the loader's refusals at the parse site, so a panel that
    /// set fields on a parsed Ruleset would bypass all of them — and a bad number would surface as
    /// a crash somewhere unrelated, several minutes later, rather than as the sentence naming the
    /// key and the line. ***The round trip through text is what keeps the tuner honest.***
    /// </para>
    /// <para>
    /// ⚠ <b>A REGENERATE IS A NEW CITY AND NOT A RELOAD, and the panel says so.</b> Most of what is
    /// on it is <em>world-creation</em> under <c>CLAUDE.md</c>'s Kind column — the Road Graph is
    /// laid once — so there is no sense in which the standing city could absorb a new
    /// <c>block_tiles</c>. Everything starts again at Tick 0: the State Hash restarts, and nothing
    /// that happened is carried over. <c>adr/0015</c>'s hot reload is a different mechanism for the
    /// <em>tuning</em> half, and it is not this.
    /// </para>
    /// <para>
    /// ⚠ <b>The eye is re-framed and not rebuilt</b>, so the yaw and zoom you were looking with
    /// survive the regenerate. Comparing two cities is the whole point of the panel, and a camera
    /// that jumped home between them would make the comparison useless.
    /// </para>
    /// </remarks>
    private void Regenerate()
    {
        string toml = _toml;
        int citizens = _citizens;
        ulong seed = _seed;

        for (int at = 0; at < Dials.Length; at++)
        {
            if (!_fields[at].Editable)
            {
                continue;
            }

            string typed = _fields[at].Text.Trim();

            if (Dials[at].Table.Length == 0)
            {
                if (Dials[at].Key == "citizens" && int.TryParse(typed, out int wanted))
                {
                    citizens = wanted;
                }
                else if (Dials[at].Key == "seed" && ulong.TryParse(typed, out ulong drawn))
                {
                    seed = drawn;
                }

                continue;
            }

            toml = Turned(toml, Dials[at].Table, Dials[at].Key, typed);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(toml, Path.GetFileName(_rulesetPath));

        if (loaded.Ruleset is null)
        {
            // The loader's own sentence, verbatim. It names the key and the line, which is the
            // whole reason the rewrite goes through text rather than around it.
            _tunerStatus.Text = loaded.Describe();

            return;
        }

        _toml = toml;
        _citizens = citizens;
        _seed = seed;

        var key = WorldKey.FromSeed(seed);

        _world = new World(citizens, loaded.Ruleset, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(_world, key, new Ticks(0));

        // The four layers laid once rather than per frame. Ground is fixed to the map and would
        // survive, but it is re-laid with the others so that "what a rebuild redoes" is one list.
        Ground();
        Hazard();
        Flood();
        Pave();
        Frame();
        Cells();
        Orbit();

        _owed = 0d;
        _tunerStatus.Text =
            $"new city: {citizens:N0} Citizens, seed {seed}, {_world.Lots.Rows.LiveCount:N0} Lots.";
    }

}
