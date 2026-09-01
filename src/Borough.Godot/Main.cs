using System;
using System.Globalization;
using System.IO;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
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

    /// <summary>How near an edge the pointer has to be, in pixels, before the camera follows.</summary>
    /// <remarks>
    /// ⚠ <b>Pixels and not a share of the window</b>, because it is sized by how accurately a hand
    /// parks a cursor rather than by how big the screen is — a share would make the band enormous on
    /// the 1920-wide default and unusable on a small window.
    /// </remarks>
    private const float EdgeMarginPixels = 72f;

    /// <summary>How fast the edge scroll pans, in drag-pixels a second at full push.</summary>
    private const float EdgePixelsPerSecond = 900f;

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
    private MultiMeshInstance3D _plots = null!;
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
    private MultiMeshInstance3D _cursor = null!;

    /// <summary>Every human-readable name in the Ruleset. <b>The shell owns these, not the core.</b></summary>
    private RulesetNames _names = RulesetNames.None;

    /// <summary>What is under the cursor, stacked most specific first.</summary>
    private Label _hover = null!;

    /// <summary>Which verb the next click issues. <b><c>01 §2</c>'s, and never an instrument's.</b></summary>
    private Verb _verb = Verb.Look;

    /// <summary>Which declared <c>[[zone_rule]]</c> the <see cref="Verb.Zone"/> brush paints for.</summary>
    private int _zoneChoice;

    /// <summary>Which <c>serves</c> kind <see cref="Verb.Service"/> would raise.</summary>
    private byte _serviceKind;

    /// <summary>Whether the governing panel is open.</summary>
    private bool _governing;

    /// <summary>The governing panel — one row per declared <c>[[policy]]</c>.</summary>
    private PanelContainer _policyPanel = null!;

    /// <summary>The amount field for each Policy, by declaration position.</summary>
    private LineEdit[] _policyFields = [];

    /// <summary>What the panel says about the last governing act.</summary>
    private Label _policyStatus = null!;

    /// <summary>
    /// Commands raised this frame, drained into the next <c>Step</c>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>A verb NEVER touches the world directly, and this list is what enforces it.</b>
    /// <c>Simulation.Apply</c> is the single door every state change goes through, so a shell that
    /// called <c>World</c> for a click would produce a city no replay reproduces and no State Hash
    /// divergence explains. ***That is the sentence <c>Populate</c> and <c>Arrive</c> each already
    /// carry in their own remark***, arriving on the player's side of it.
    /// </remarks>
    private readonly System.Collections.Generic.List<Command> _queued = [];

    /// <summary>A Street was laid or bulldozed, so the Road Graph has to be re-drawn.</summary>
    private bool _repave;

    /// <summary>
    /// <b>The session, as the file that reproduces it.</b> Every <see cref="Command"/> the shell
    /// issues is appended here at the Tick it applies.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>Populate</c> IS THE FIRST ENTRY, and that is the whole of why this is not a
    /// bolt-on.</b> The shell used to call <c>SyntheticCity.PopulateInto</c> directly, which put an
    /// entire city into the world without going through <see cref="Simulation.Apply"/> — ***state
    /// arriving by a door no log accounts for***, at Tick 0, before a player had touched anything.
    /// <c>Borough.Headless</c> has recorded the population as a Command since slice 6 for exactly
    /// this reason (<c>Session.Load</c>), and the shell simply did not.
    /// </remarks>
    private InputLogBuilder _log = null!;

    /// <summary>Why the last click did nothing, or empty. <b>Shown, never thrown.</b></summary>
    private string _refused = string.Empty;

    /// <summary>
    /// Where a driven run is pointing, or null when the mouse is. <b>A hand's motion clears it.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It PERSISTS after the click rather than being cleared with it</b>, so that the hover, the
    /// cursor marker and any <c>shoot</c> afterwards all describe the Tile the script named. A driven
    /// run has no mouse, and an aim that lasted one call would leave every picture of a driven click
    /// pointing at the corner of the map.
    /// </remarks>
    private (Tiles East, Tiles North)? _aimed;
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

    /// <summary>Which vacant Lot each drawn pad is, in instance order.</summary>
    private readonly List<ulong> _plotIds = [];

    /// <summary>Lines that have arrived over the socket and not yet been applied.</summary>
    private readonly System.Collections.Concurrent.BlockingCollection<string> _asked = [];

    /// <summary>Replies waiting for the socket thread to send. One per line taken.</summary>
    private readonly System.Collections.Concurrent.BlockingCollection<string> _answered = [];

    /// <summary>The listening socket, or null when nobody is driving live.</summary>
    private System.Net.Sockets.Socket? _listener;

    /// <summary>Where every applied command is spelled back out, or null when nothing records.</summary>
    private string? _record;

    /// <summary>The socket's path, kept so it can be unlinked on the way out.</summary>
    private string? _door;
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
        (_rulesetPath, int citizens, ulong startAt, bool govern, string? drive, ulong quitAt,
            string? listen, string? record) = Arguments();

        if (!Driven(drive, quitAt))
        {
            Stop(2);

            return;
        }

        if (record is not null)
        {
            _record = Globalize(record);

            File.WriteAllText(_record, string.Empty);
        }

        if (listen is not null && !Listen(Globalize(listen)))
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

        _names = loaded.Names;
        _citizens = citizens;
        _seed = 0;

        var key = WorldKey.FromSeed(_seed);

        _world = new World(citizens, loaded.Ruleset, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };

        _log = new InputLogBuilder(
            _seed,
            new WorldConfiguration(citizens),
            RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml)));

        // 🔴 THE CITY ARRIVES THROUGH Simulation.Apply AND NOT BESIDE IT. This was
        // SyntheticCity.PopulateInto called on the world directly, which is thousands of rows
        // entering by a door the log does not account for -- and every hand-played session would
        // therefore have replayed against an EMPTY world and diverged at Tick 0, with nothing in
        // the file to explain it. Borough.Headless has recorded the population as a Command since
        // slice 6 (Session.Load); the shell just did not.
        //
        // ⚠ IT COSTS ONE TICK, AND THAT IS THE HEADLESS RUNNER'S BEHAVIOUR RATHER THAN A CHARGE.
        // A Command applies at the top of a Tick, so a world populated by one is populated during
        // Tick 0 and the readout opens at Tick 1. A shell that opened at Tick 0 with a city in it
        // would be one Tick ahead of the runner for the whole session.
        _log.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        _simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

        // FAST-FORWARD BEFORE THE FIRST FRAME, and it is not a rung. The ladder is what a person
        // watches at; this is how they get to the part worth watching. A flood on flooded.toml
        // begins at Tick 4,096, which at the top rung is two and a half minutes of staring at a dry
        // city -- and on a machine with no screen it is the difference between a photograph of a
        // flood and a photograph of the coast.
        //
        // ⚠ IT STEPS THE SIMULATION AND SKIPS NOTHING. Every Tick runs, which is why it is slow and
        // why it is correct: a world jumped to is a different world (adr/0003), and the whole point
        // of a shell is to look at the one the headless runner would produce.
        // FROM ONE, because the Populate Command above already ran Tick 0. A loop from zero would
        // put the shell one Tick past the runner on every --start-at, which is the class of
        // off-by-one a State Hash comparison finds and a photograph never does.
        for (ulong tick = 1; tick < startAt; tick++)
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

        // WHAT THE CURSOR IS OVER, and it is a CELL rather than a Tile on purpose. A Tile is 4 m
        // and vanishes at any zoom that shows a neighbourhood; a Cell is 128 m and is the box the
        // pick actually resolves Buildings against (BuildingResidency.In). ***A marker that is not
        // the thing the query used is a marker that lies at the edges.***
        _cursor = Layer(new Color(0.95f, 0.80f, 0.25f), new Vector3(1f, 1f, 1f));

        // but they sit at almost the same height, and painting the standing water last is what makes
        // a rising tide read as arriving rather than as flickering.
        _water = Layer(new Color(0.10f, 0.22f, 0.42f), new Vector3(1f, 1f, 1f));
        _flood = Layer(new Color(0.20f, 0.48f, 0.78f), new Vector3(1f, 1f, 1f));
        _roads = Layer(new Color(0.30f, 0.30f, 0.33f), new Vector3(1f, 0.1f, 1f));

        // 🔴 A VACANT LOT DREW NOTHING AT ALL, so Zone -- which creates Lots and never a Building --
        // had NO visible result on any world. Buildings() walks the Building table, and a Lot with
        // nothing on it is not in it. ***The subdivision is the thing the verb does***, and until
        // this layer existed the only way to see one was to wait for the simulation to build on it,
        // which on a world with an empty Unplaced Pool never happens.
        _plots = Layer(new Color(0.42f, 0.52f, 0.30f), Vector3.One, perInstance: true);

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

        // ⚠ AFTER Readout(), which is what builds the panel. Opening it from the command line is the
        // same bargain BOROUGH_SHOT strikes -- a machine with no hands cannot press `p`, and a panel
        // nobody can photograph is a panel nobody reviews.
        if (govern)
        {
            _governing = true;
            _policyPanel.Visible = true;
            ShowPolicies();
        }
    }

    public override void _Process(double delta)
    {
        if (_stopping)
        {
            return;
        }

        _owed += delta * Ladder[_rung];

        // 🔴 A QUEUED COMMAND BUYS ONE TICK, EVEN PAUSED, and that is a decision rather than a slip.
        // A Command applies at the top of a Tick because Simulation.Apply is the single door, so a
        // verb pressed at rung 0 would otherwise sit and look broken until somebody started the
        // clock. ***Every reference builder lets you edit while paused***, and the cost is that
        // acting is the one input that moves a paused world -- by exactly one Tick, and the readout
        // says which Tick it is.
        if (_queued.Count > 0 && _owed < 1.0)
        {
            _owed = 1.0;
        }

        // 🔴 THE CLOCK IS CLAMPED AT THE NEXT COMMAND'S TICK, AND THAT IS WHAT MAKES A DRIVEN RUN
        // REPRODUCIBLE. A frame steps as many Ticks as the rung and the frame time between them
        // ask for, so a command drained on "the first frame at or past Tick T" lands on a
        // different Tick on a different machine, at a different rung, or under a different load --
        // which is plans/0048 F4, the defect tier 1 could not fix. Stepping no further than T
        // makes the Tick a command lands on a property of the SCRIPT rather than of the host.
        ulong until = _next < _drive.Length ? _drive[_next].At : ulong.MaxValue;

        while (_owed >= 1.0 && _world.Tick.Raw < until)
        {
            _simulation.Step(Ordered());
            _owed -= 1.0;
        }

        // ⚠ CLAMPED, because holding the clock at a command's Tick lets the debt run past a whole
        // Tick where the loop above used to guarantee it could not. An alpha over 1 would place a
        // Traveller past the Address it is walking to.
        _alpha = new Ratio((int)(Math.Min(_owed, 0.999_99) * 65_536));

        // AFTER the loop and not inside it, because a frame that steps many Ticks may lay many
        // Streets and the Road Graph only has to be correct once a frame. Pave() is O(Segments) and
        // bordered.toml has 535,817 of them, so this is a walk worth doing on the frames that
        // earned it rather than on all of them.
        if (_repave)
        {
            Pave();
            _repave = false;
        }

        // A MACHINE WITH NO HANDS HAS ITS CURSOR AT (0,0), which is the sky or a corner of the map,
        // so every photograph would show the pick refusing rather than the pick working. Warping to
        // the middle of the viewport is BOROUGH_SHOT's own bargain -- the shot exists because there
        // is nobody to press the key -- and it is confined to the same env var, so a person's cursor
        // is never moved out from under them.
        if (System.Environment.GetEnvironmentVariable("BOROUGH_SHOT") is not null)
        {
            Input.WarpMouse(GetViewport().GetVisibleRect().Size * 0.5f);
        }

        Edge(delta);

        Draw(_alpha);
        Drive();
        Answer();
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

            case DriveVerb.Hold:
                Hold(command.Path!, command.Amount);

                break;

            case DriveVerb.Click:
                // 🔴 THE BOUNDS ARE CHECKED HERE BECAUSE THE DRIVEN AIM SKIPS THE RAY THAT USED TO
                // CHECK THEM. Aim() clamps a cursor to the map on its way out of the projection; a
                // script names a Tile and meets none of that, so a click past the edge would have
                // reached Simulation with a coordinate no ground has.
                if (command.East < 0 || command.North < 0
                    || command.East >= CellGrid.WorldTiles || command.North >= CellGrid.WorldTiles)
                {
                    _refused = "that is not on the map.";

                    break;
                }

                _aimed = (new Tiles(command.East), new Tiles(command.North));

                Act(command.Amount != 0);

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

        // ⚠ RECORDED HERE AND NOWHERE ELSE, which is why a keyboard session records as readily as a
        // socket one: every channel arrives at this method, so the log is complete by construction
        // rather than by three call sites remembering.
        if (_record is not null)
        {
            File.AppendAllText(_record, DriveScript.Spell(command) + "\n");
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
    /// Open the door a live driver knocks on. <b>One client, one line, one reply.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A SOCKET IS A WALL-CLOCK CHANNEL INTO A SIMULATION WHOSE WHOLE DISCIPLINE IS
    /// DETERMINISM, AND THE OBJECTION IS THE RIGHT ONE.</b> It is answered rather than accepted:
    /// every arriving line is stamped with the Tick it landed on and spelled back out through
    /// <c>--record</c>, so ***the log of an interactive session IS a drive script*** and what
    /// somebody did by hand replays as a batch run.
    /// </para>
    /// <para>
    /// ⚠ <b>Lines are applied on the main thread at a Tick boundary</b>, never where they arrive.
    /// The socket thread only enqueues and then blocks for its reply, so nothing off the main thread
    /// ever touches the world — which is the same rule the renderer already keeps.
    /// </para>
    /// <para>
    /// ⚠ <b>The path is unlinked before binding.</b> A Unix socket outlives the process that made
    /// it, so a shell that crashed leaves a file the next one cannot bind — and the failure reads as
    /// *address in use* rather than as *the last run died*.
    /// </para>
    /// </remarks>
    private bool Listen(string path)
    {
        try
        {
            File.Delete(path);

            _listener = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.Unix,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Unspecified);

            _listener.Bind(new System.Net.Sockets.UnixDomainSocketEndPoint(path));
            _listener.Listen(1);
        }
        catch (Exception opening)
        {
            GD.PrintErr($"cannot listen on {path}: {opening.Message}");

            return false;
        }

        _door = path;

        new System.Threading.Thread(Serve) { IsBackground = true }.Start();
        GD.Print($"listening on {path}");

        return true;
    }

    /// <summary>Take a line, hand it to the main thread, wait for what it answers.</summary>
    /// <remarks>
    /// ⚠ <b>Strictly one reply per line, and the read blocks until it comes.</b> A driver can
    /// therefore send and read in lock step without a clock of its own — which is the only way a
    /// client can know that what it reads is the state <em>after</em> what it sent.
    /// </remarks>
    private void Serve()
    {
        while (_listener is not null)
        {
            try
            {
                using System.Net.Sockets.Socket client = _listener.Accept();
                using var stream = new System.Net.Sockets.NetworkStream(client);
                using var reader = new StreamReader(stream);
                using var writer = new StreamWriter(stream) { AutoFlush = true };

                while (reader.ReadLine() is { } line)
                {
                    _asked.Add(line);
                    writer.WriteLine(_answered.Take());
                }
            }
            catch (Exception)
            {
                // The listener closed under us, or a client left mid-sentence. Neither is this
                // thread's business to report: the run is either ending or fine without a driver.
                return;
            }
        }
    }

    /// <summary>Apply whatever arrived, then say what the city looks like now.</summary>
    /// <remarks>
    /// ⚠ <b>The readout is recomposed before the reply</b>, for <see cref="Caption"/>'s reason: it
    /// was written by the <c>Draw</c> that ran before these commands did. ⚠ <b>An empty line is a
    /// poll</b> — no commands, and the state comes back anyway, which is how a driver watches
    /// without touching anything.
    /// </remarks>
    private void Answer()
    {
        if (_listener is null)
        {
            return;
        }

        while (_asked.TryTake(out string? line))
        {
            DriveScriptResult read = DriveScript.Line(line, _world.Tick.Raw);

            if (read.Commands is null)
            {
                _answered.Add("refused\t" + read.Describe().Replace('\n', ' '));

                continue;
            }

            foreach (DriveCommand command in read.Commands)
            {
                Apply(command);
            }

            Draw(_alpha);

            _answered.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ok\t{_world.Tick.Raw}\t{_readout.Text.Replace('\n', '\t')}"));
        }
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
        ("plot", _plots, true, _plotIds),
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
        if (@event is InputEventMouseButton button)
        {
            if (button is { Pressed: true, ButtonIndex: MouseButton.WheelUp })
            {
                Dolly(1f);

                return;
            }

            if (button is { Pressed: true, ButtonIndex: MouseButton.WheelDown })
            {
                Dolly(-1f);

                return;
            }

            // 🔴 ON PRESS, AND THE LEFT BUTTON DOES NOTHING ELSE. It used to act on RELEASE, with a
            // four-pixel slop test to tell a click from a drag, because panning was bound to the same
            // button -- and on a macOS trackpad that combination ate the click and panned instead.
            // Tap-to-click and force-click both emit motion while the button is down, so the pointer
            // drifts past four pixels before the release arrives: _mayAct was cleared, Act never ran,
            // and the same drift moved the camera. ***A conditional binding on one button is what
            // made the input feel unreliable***, so the condition is gone rather than widened -- a
            // larger slop radius would have been the same defect with a longer fuse.
            if (button is { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                // ⚠ THROUGH Apply, so --record spells the click out as the Tile it resolved to.
                // A recording holding a screen position would be a recording of a camera, and a
                // camera is not an input (adr/0007). _aimed is null here, so Aim() casts the ray.
                if (Aim() is { } at)
                {
                    Apply(new DriveCommand(
                        _world.Tick.Raw,
                        DriveVerb.Click,
                        button.ShiftPressed ? 1 : 0,
                        null,
                        at.East.Raw,
                        at.North.Raw));
                }
                else
                {
                    _refused = "that is not on the map.";
                }
            }

            return;
        }

        // A trackpad reaches none of that. macOS turns a two-finger scroll into a pan gesture and a
        // pinch into a magnify gesture, so the wheel branch above never sees a finger at all.
        //
        // 🔴 IT PANS, AND IT USED TO ZOOM. A two-finger scroll was wired to Dolly while the pinch
        // gesture below was ALSO wired to Dolly -- so a trackpad had two ways to zoom and no way to
        // move, which is the native mapping of both gestures exactly inverted. ***A gesture named
        // PanGesture doing something other than panning is a mapping nobody can learn***, and for a
        // trackpad this alone is the whole camera.
        if (@event is InputEventPanGesture scroll)
        {
            // Negated on both axes: the platform reports where the CONTENT went, and the camera is
            // the thing that moves here, so following the sign would send the city the wrong way.
            Pan(-scroll.Delta * 12f);

            return;
        }

        if (@event is InputEventMagnifyGesture pinch)
        {
            // Factor is a ratio about 1, and a pinch arrives as many small ones rather than one big.
            Dolly((pinch.Factor - 1f) * 8f);

            return;
        }

        // A hand that moves the mouse is pointing again, and takes the aim back off the script.
        if (@event is InputEventMouseMotion)
        {
            _aimed = null;
        }

        // 🔴 RIGHT OR MIDDLE, NEVER LEFT. Panning shared the left button with every verb until
        // 2026-08-31, which is what made a trackpad click unusable -- see the button branch above.
        // A two-finger click-drag is the native pan on a Mac trackpad and middle-drag is the mouse's,
        // so the two masks here are one gesture on two devices rather than a preference.
        if (@event is InputEventMouseMotion drag
            && (drag.ButtonMask & (MouseButtonMask.Right | MouseButtonMask.Middle)) != 0)
        {
            Pan(drag.Relative);

            return;
        }

        if (@event is not InputEventKey { Pressed: true } key)
        {
            return;
        }

        // The keys with no DriveVerb behind them, which is what puts them ahead of the command
        // switch rather than in it. The tuner's two are an editing UI rather than a view of the
        // city, and a script has no panel to open.
        //
        // 🔴 THE FOUR VERB KEYS USED TO BE AMONG THEM, ON THE GROUND THAT HOLDING A TOOL MOVES
        // NOTHING IN THE WORLD. That was true and it made a recorded session UNREPLAYABLE: a click
        // means whatever is held, so a session recording the click and not the choice replays as a
        // different verb. ***What a recording needs is not everything that changed the city, it is
        // everything a replay has to know*** -- and the tool is the second half of every click.
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

            case Key.V:
                Apply(Held("look", 0));

                return;

            case Key.Z:
                // ⚠ PRESSING IT AGAIN CYCLES THE ZONE rather than doing nothing, which is what makes
                // a Ruleset with two [[zone_rule]]s reachable without a second key. A zone word is a
                // BITMASK of which rules may build (ZoneRuleDefinition.Admits), so what the brush
                // paints is one rule's permission and never a category the city knows about.
                Apply(Held(
                    "zone",
                    _verb == Verb.Zone && _world.Rules.ZoneRules.Length > 0
                        ? (_zoneChoice + 1) % _world.Rules.ZoneRules.Length
                        : 0));

                return;

            case Key.X:
                Apply(Held("street", 0));

                return;

            case Key.B:
                Apply(Held("demolish", 0));

                return;

            case Key.S:
                // ⚠ CYCLES ON REPEAT, exactly as Z does, because a Ruleset may declare more than one
                // `serves` kind and there is no second key to spend on choosing between them.
                Apply(Held(
                    "service", NextService(_verb == Verb.Service ? _serviceKind : (byte)0)));

                return;

            case Key.P:
                // THE GOVERNING PANEL, and it is deliberately NOT the tuner. The tuner regenerates a
                // world from Ruleset text -- world-creation, a NEW city -- while this sets a declared
                // Policy's amount on the city that is running, through a Command, at a Tick, in the
                // order a replay reproduces. ***One panel edits the world's premises and the other
                // plays the game***, and putting them on one key would blur exactly that line.
                _governing = !_governing;
                _policyPanel.Visible = _governing;

                if (_governing)
                {
                    ShowPolicies();
                }

                return;

            case Key.W:
                Record();

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
    /// Slides the eye across the ground by a screen-space delta. <b>The one place panning happens.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>It runs along the EYE's axes and not the world's</b>, because a turn parts the two and a
    /// drag would then go off at an angle. ⚠ <b>Across the ground and never through it</b>, so the
    /// city cannot be lost behind the camera by a careless sweep.
    /// </para>
    /// <para>
    /// <b>Scaled by the standoff rather than by the city</b>, so a close-up pans a street at a time
    /// and a wide shot crosses a district. ***Three callers share it*** — a right-drag, a trackpad
    /// pan gesture and the edge scroll — and they differ only in where the delta comes from.
    /// </para>
    /// </remarks>
    /// <param name="by">Screen-space movement, in pixels, of the kind a drag reports.</param>
    private void Pan(Vector2 by)
    {
        float reach = _distance * 0.002f;
        var right = new Vector3(Mathf.Cos(_yaw), 0f, -Mathf.Sin(_yaw));
        var ahead = new Vector3(-Mathf.Sin(_yaw), 0f, -Mathf.Cos(_yaw));

        _focus += ((right * -by.X) + (ahead * by.Y)) * reach;

        Orbit();
    }

    /// <summary>
    /// Pans while the pointer rests near an edge of the window. <b>The camera control that needs no
    /// button at all.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every verb holds the left button, so a person mid-edit has no hand free to move the
    /// city.</b> Edge scrolling is the affordance that costs no button, which is why every builder
    /// in the reference set has it.
    /// </para>
    /// <para>
    /// ⚠ <b>The speed RAMPS with depth into the margin</b> rather than switching on, so resting a
    /// cursor just inside the boundary creeps and pushing it to the frame's edge moves properly.
    /// ***A binary edge scroll is unusable at both settings***: fast enough to cross the map is too
    /// fast to stop on a street.
    /// </para>
    /// <para>
    /// 🔴 <b>THE TWO PANELS ARE EXCLUDED, AND BY THEIR OWN RECTANGLES RATHER THAN BY A CONSTANT.</b>
    /// The readout is top-left and the hover bottom-left, both inside the margin, so reading either
    /// would otherwise creep the camera the whole time somebody looked at it — ***the one place a
    /// person deliberately parks the cursor is the one place this must not fire.*** ⚠ <b>The first
    /// spelling guessed a 560×200 corner</b> and killed edge-scrolling north-west entirely, which is
    /// a real direction to want; asking the <c>Control</c> for <c>GetGlobalRect</c> makes the dead
    /// zone exactly the size of the text and no larger, and it cannot drift when a panel grows a line.
    /// </para>
    /// <para>
    /// ⚠ <b>It is skipped while a panel is open</b>, because the tuner and the governing panel are
    /// full of fields somebody is aiming at.
    /// </para>
    /// </remarks>
    /// <param name="delta">Seconds since the last frame, so the pan is a rate and not a per-frame step.</param>
    private void Edge(double delta)
    {
        if (_governing || _tuner.Visible)
        {
            return;
        }

        Rect2 frame = GetViewport().GetVisibleRect();
        Vector2 at = GetViewport().GetMousePosition();

        // Outside the window entirely: a pointer that has left is not pointing at an edge, and
        // following it would pan for as long as the cursor was somewhere else.
        if (!frame.HasPoint(at))
        {
            return;
        }

        // Over a panel: reading is not an instruction to move.
        if (_readout.GetGlobalRect().HasPoint(at) || _hover.GetGlobalRect().HasPoint(at))
        {
            return;
        }

        float push = 0f;
        var by = Vector2.Zero;

        Ramp(at.X, frame.Size.X, ref by, ref push, Vector2.Right);
        Ramp(at.Y, frame.Size.Y, ref by, ref push, Vector2.Down);

        if (push > 0f)
        {
            // A rate in pixels a second, converted to the delta Pan wants.
            Pan(by * (float)delta * EdgePixelsPerSecond);
        }
    }

    /// <summary>One axis of the edge ramp: how far into the margin, and which way that points.</summary>
    private static void Ramp(
        float at, float span, ref Vector2 by, ref float push, Vector2 axis)
    {
        if (at < EdgeMarginPixels)
        {
            float depth = (EdgeMarginPixels - at) / EdgeMarginPixels;

            by += axis * depth;
            push += depth;
        }
        else if (at > span - EdgeMarginPixels)
        {
            float depth = (at - (span - EdgeMarginPixels)) / EdgeMarginPixels;

            by -= axis * depth;
            push += depth;
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
    /// <c>--ruleset PATH</c>, <c>--citizens N</c>, <c>--start-at TICK</c>, <c>--govern</c>,
    /// <c>--drive PATH</c>, <c>--quit-at TICK</c>, <c>--listen PATH</c> and <c>--record PATH</c>,
    /// after Godot's <c>--</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A shell reads the command line and the core does not.</b> Every string here is this
    /// project's (<c>adr/0002</c>), and a bad one is reported rather than defaulted, because a
    /// silently-substituted world is a picture of somewhere else.
    /// </remarks>
    private static (string Ruleset, int Citizens, ulong StartAt, bool Govern, string? Drive,
        ulong QuitAt, string? Listen, string? Record) Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;
        ulong startAt = 0;
        string? drive = null;
        ulong quitAt = 0;
        string? listen = null;
        string? record = null;
        string[] given = OS.GetCmdlineUserArgs();

        // ⚠ A FLAG AND NOT A PAIR, so it is read over the whole array rather than inside the loop
        // below, which stops one short to read a value after each name.
        bool govern = Array.IndexOf(given, "--govern") >= 0;

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
            else if (given[at] == "--listen")
            {
                listen = given[at + 1];
            }
            else if (given[at] == "--record")
            {
                record = given[at + 1];
            }
        }

        return (ruleset, citizens, startAt, govern, drive, quitAt, listen, record);
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

    /// <inheritdoc/>
    public override void _ExitTree()
    {
        // The socket file outlives the process, so leaving it behind makes the NEXT run fail to
        // bind and report an address in use -- a message about this run that reads as one about that
        // one.
        _listener?.Dispose();
        _listener = null;

        if (_door is not null)
        {
            File.Delete(_door);
        }
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
        int vacant = Fill(_plots, Plots(), _plotIds);
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);
        int under = Fill(_flood, Anonymous(Inundated()));

        Fill(_travellers, Travellers(moving), _travellerIds);
        Cursor();

        ulong tick = _world.Tick.Raw;
        ulong ofDay = tick % (ulong)Ticks.PerDay;

        _readout.Text =
            $"{System.IO.Path.GetFileName(_rulesetPath)}   Tick {tick:N0}   "
            + $"Day {tick / (ulong)Ticks.PerDay}   "
            + $"{ofDay * 24 / (ulong)Ticks.PerDay:00}:{ofDay * 1440 / (ulong)Ticks.PerDay % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"vacant Lots {vacant:N0}   "
            + $"travelling {moving:N0}{Weather(under)}\n"
            + $"speed {Pace(_rung)}   "
            + $"mode {Holding()}   "
            + "[ ] speed, space pause, v look, z zone, x street, b demolish, s service, "
            + "p policies, w write log, g roads, c cells, tab tune\n"
            + "click acts   right-drag or two-finger scroll pans   edge of screen pans   "
            + "pinch or -/= zooms   q/e turns"
            + (_refused.Length > 0 ? $"\nREFUSED — {_refused}" : string.Empty);

        _hover.Text = Pointing();
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
    /// Every <b>vacant</b> Lot, as a flat pad on the kerb. <b>What <c>Zone</c> actually produces.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>Zone creates Lots and never a Building</b> (<c>adr/0069</c> — construction houses
    /// nobody, and <c>02 §2.2</c> — Lots are generated rather than painted), so before this layer
    /// existed the verb's entire visible result was <em>nothing</em>. On a world whose Unplaced Pool
    /// is empty — which is every shipped file that condemns nothing — the simulation never builds on
    /// a new Lot either, so the nothing was permanent. ***A verb whose success and whose failure
    /// look identical cannot be learned.***
    /// </para>
    /// <para>
    /// ⚠ <b>Vacant only, and that is the informative half.</b> An occupied Lot already has a
    /// Building standing on it; drawing a pad under one would be a marker nobody can see saying
    /// something the Building already says.
    /// </para>
    /// <para>
    /// ⚠ <b>It steps to the kerb exactly as <see cref="Buildings"/> does and for the same reason</b>
    /// — a Lot's coordinate is a point on the Segment (<c>adr/0078</c>), so a pad drawn where the
    /// Lot says it is lies in the carriageway. The setback here is half the carriageway plus half
    /// the pad, so the two layers agree about which side of the road a Lot is on.
    /// </para>
    /// </remarks>
    private System.Collections.Generic.IEnumerable<(ulong Id, Transform3D Where, Color What)>
        Plots()
    {
        LotTable lots = _world.Lots;
        int block = _world.Roads.Streets.BlockTiles;
        float frontage = Frontage(block, _world.Rules.Lots.LotsPerSegment);
        float pad = frontage * FrontageFill;
        float setback = (RoadWidthMetres * 0.5f) + (pad * 0.5f);

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot))
            {
                continue;
            }

            float east = lots.East[slot].Raw * MetresPerTile;
            float north = lots.North[slot].Raw * MetresPerTile;
            var side = (StreetSide)lots.Side[slot];
            bool horizontal = block > 0 && lots.North[slot].Raw % block == 0;

            if (horizontal)
            {
                north += side == StreetSide.Left ? setback : -setback;
            }
            else
            {
                east += side == StreetSide.Right ? setback : -setback;
            }

            // A hand's breadth off the ground -- above the carriageway at 0.1 m so a pad on a kerb
            // is not hidden by the road it hangs on, and far below anything that stands up.
            Vector3 plan = horizontal
                ? new Vector3(pad, 0.4f, pad * 0.5f)
                : new Vector3(pad * 0.5f, 0.4f, pad);

            yield return (
                lots.Rows.IdAt(slot),
                new Transform3D(Basis.FromScale(plan), new Vector3(east, 0.2f, -north)),
                new Color(0.42f, 0.52f, 0.30f).SrgbToLinear());
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
    /// This Tick's input: whatever the player raised since the last one, and then nothing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The queue is CLEARED here and not after the step</b>, because a command that threw would
    /// otherwise be re-sent on the next frame for ever. ⚠ <b>The Ruleset hash is zero, which is what
    /// <c>Step(default)</c> already passed</b> — a non-zero one is a reload instruction
    /// (<c>Simulation</c> compares it against what is in force), and the tuner owns reloads by its
    /// own path.
    /// </remarks>
    private TickInput Ordered()
    {
        if (_queued.Count == 0)
        {
            return default;
        }

        Command[] raised = [.. _queued];

        // 🔴 THE ROAD GRAPH IS LAID ONCE AND A PLAYER CAN CHANGE IT, WHICH IS THE WHOLE DEFECT.
        // Pave() ran in _Ready and on a tuner rebuild and nowhere else, so a Street laid by Connect
        // existed in the world, routed Trips, carried Lots -- and was never drawn. ***The verb
        // looked broken while working perfectly***, which is the worst way for a verb to fail.
        // Flagged rather than re-paved here because nothing has stepped yet: the Command applies at
        // the top of the next Tick, so the Segment does not exist until Step returns.
        foreach (Command command in raised)
        {
            if (command.Kind == CommandKind.Connect)
            {
                _repave = true;
            }

            // ⚠ _world.Tick AND NOT _world.Tick + 1. Ordered() is evaluated as the argument to
            // Step, so the Tick it names is the one about to run and the one these Commands apply
            // at. Recording the Tick after would put every verb one Tick late in the replay -- a
            // divergence that appears at the first command and looks like a simulation bug.
            _log.Append(_world.Tick, command);
        }

        _queued.Clear();

        return new TickInput(raised, 0);
    }

    /// <summary>
    /// Writes the session so far to a <c>.borough</c> file, and prints what would reproduce it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ROW THAT MAKES THE OTHER THREE WORTH ANYTHING.</b> A verb that is not in
    /// the log is a state change no replay reproduces and no State Hash divergence explains — the
    /// sentence <c>Populate</c>, <c>Trip</c> and <c>Arrive</c> each already carry in their own
    /// remark, and which nothing in the shell honoured until now.
    /// </para>
    /// <para>
    /// ⚠ <b>It USES the codec and never implements one</b> (<c>adr/0039</c>).
    /// <see cref="InputLogCodec"/> lives in <c>Borough.Formats</c>, which both shells reference and
    /// neither may duplicate — a second writer is a second format the day one of them is fixed.
    /// </para>
    /// <para>
    /// ⚠ <b>The State Hash goes to the console beside the path, and it is the whole point of the
    /// line.</b> The round trip is <em>replay this file to this Tick and get this number</em>, so
    /// printing the path alone would leave the operator to find the number themselves — and a
    /// verification nobody can run is not one.
    /// </para>
    /// </remarks>
    private void Record()
    {
        string path = Path.Combine(
            System.Environment.CurrentDirectory,
            $"session-{_world.Tick.Raw}{InputLogCodec.Extension}");

        using (var writer = new StreamWriter(path))
        {
            InputLogCodec.Write(writer, _log.Build());
        }

        // 🔴 THE RULESET IN FORCE IS NOT ALWAYS THE FILE ON DISK, and a reproduce line that assumed
        // it was would be wrong exactly when somebody had been experimenting. Regenerate() rebuilds
        // the world from EDITED toml held in memory, so after a tuner pass the log names a content
        // hash no file has -- and Replay.Start refuses a catalogue whose opening hash differs, which
        // is the refusal working and an operator with no way to satisfy it. So the edited Ruleset is
        // written out beside the log and the line points at THAT.
        string rules = _rulesetPath;

        if (RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml))
            != RulesetFile.HashOf(_rulesetPath))
        {
            rules = Path.ChangeExtension(path, ".toml");
            File.WriteAllText(rules, _toml);
            GD.Print($"the Ruleset in force is tuned and is not {_rulesetPath}; wrote {rules}");
        }

        GD.Print(
            $"wrote {path} — {_log.Build().Count} commands over {_world.Tick.Raw} Ticks.\n"
            + $"  State Hash 0x{_world.HashState():X16}\n"
            + "  reproduce: dotnet run --project src/Borough.Headless -- "
            + $"--log {path} --ruleset {rules} --ticks {_world.Tick.Raw} "
            + $"--hash-every {_world.Tick.Raw}");
    }

    /// <summary>What the current verb is called, and for Zone which permission it paints.</summary>
    private string Holding() => _verb switch
    {
        // ⚠ "SUBDIVIDE" AND NOT "ZONE", because the verb is not a brush. LotSubdivider.Face returns
        // zero on a frontage Frontage has already claimed, so this creates Lots on virgin ground and
        // can never repaint an existing block. ***A label that promised a brush would make the
        // commonest misuse -- clicking a block that already has Lots -- look like a bug.***
        Verb.Zone => _world.Rules.ZoneRules.Length > 0
            ? $"SUBDIVIDE 0x{_world.Rules.ZoneRules[_zoneChoice].Admits:X4} (z cycles)"
            : "SUBDIVIDE — no [[zone_rule]] declared",
        Verb.Connect => "STREET (shift-click bulldozes)",
        Verb.Demolish => "DEMOLISH — abandoned only",
        Verb.Service => _serviceKind != 0
            ? $"SERVICE {_names.Kind(_serviceKind) ?? _serviceKind.ToString()} (s cycles)"
            : "SERVICE — no kind declares `serves`",
        _ => "look",
    };

    // ---- the verbs -------------------------------------------------------------------------------

    /// <summary>
    /// Turns the click into a <see cref="Command"/> and queues it. <b>Nothing here touches the
    /// world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SHELL ASKS THE CORE WHETHER A COMMAND WOULD BE REFUSED AND NO LONGER RESTATES A
    /// RULE.</b> It used to guard three refusals by re-implementing them —
    /// <c>plans/0012</c> <b>Cause 1</b> by construction, two places storing one rule and the copy is
    /// the one that drifts — and <b>ten</b> belong to the five verbs it issues.
    /// <see cref="Simulation.Refuses"/> answers off the predicate the applier uses, so ***the shell
    /// declines to SEND what the core would refuse***, on the core's own finding rather than on a
    /// paraphrase of it.
    /// </para>
    /// <para>
    /// ⚠ <b>Why declining rather than catching:</b> commands are applied at the top of a Tick, so an
    /// exception out of <c>Apply</c> aborts <c>Step</c> half way and leaves a world no invariant
    /// covers. ***A crash is not the worst outcome of an unguarded click; a half-stepped world
    /// is.***
    /// </para>
    /// <para>
    /// ⚠ <b>What stays the shell's is AIMING, and it is a different question.</b> <em>No Building
    /// stands in this Cell</em> and <em>that is not on the map</em> are the shell failing to resolve
    /// a cursor into an address; the core is never asked, because there is no command to ask about.
    /// ***A rule belongs to the city and an aim belongs to the hand.***
    /// </para>
    /// <para>
    /// ⚠ <b>Zone has no guard and needs none.</b> A block with no Street on any face yields no Lots,
    /// which is <c>02 §2.2</c>'s third rule and the mechanism by which a bad street layout punishes
    /// the player — ***it is an outcome and not a refusal***, and a shell that greyed it out would be
    /// hiding the lesson. <see cref="Simulation.Refuses"/> says so too.
    /// </para>
    /// </remarks>
    /// <param name="inverted">Whether shift was held, which turns <c>Connect</c> into a bulldoze.</param>
    private void Act(bool inverted)
    {
        // 🔴 CLEARED AFTER THE LOOK CHECK AND NOT BEFORE IT, which was found by driving a script
        // with a misspelt tool in it. `hold plough` disarms the hand and says so; clearing first
        // meant the very next click wiped that sentence before anybody could read it, and the run
        // reported nothing wrong at all. ***A refusal a later click erases is a refusal nobody
        // sees***, and looking is the one verb that changes nothing and should erase nothing.
        if (_verb == Verb.Look)
        {
            return;
        }

        _refused = string.Empty;

        if (Aim() is not { } at)
        {
            _refused = "that is not on the map.";

            return;
        }

        switch (_verb)
        {
            case Verb.Zone when _world.Rules.ZoneRules.Length > 0:
                Send(new Command(
                    CommandKind.Zone,
                    at.East,
                    at.North,
                    _world.Rules.ZoneRules[_zoneChoice].Admits));
                break;

            case Verb.Zone:
                _refused = "this Ruleset declares no [[zone_rule]], so there is no permission to paint.";
                break;

            case Verb.Connect:
                Lay(at, inverted);
                break;

            case Verb.Demolish:
                Clear(at);
                break;

            case Verb.Service:
                Raise(at);
                break;

            default:
                break;
        }
    }

    /// <summary>Choose what the next click means, and which one of the tool.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The tool is resolved from its NAME here and nowhere else.</b> Which verbs this shell
    /// offers is not something <c>Borough.Formats</c> can know, so the grammar carries the word and
    /// this is the only place that knows what the words are — see <see cref="DriveVerb.Hold"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>An unknown tool is a refusal a person reads rather than a silent <c>look</c>.</b> A
    /// script that misspells its tool would otherwise click five times with nothing held and report
    /// a city that ignored it.
    /// </para>
    /// </remarks>
    private void Hold(string tool, int choice)
    {
        _refused = string.Empty;

        switch (tool)
        {
            case "look":
                _verb = Verb.Look;

                break;

            case "zone":
                _verb = Verb.Zone;
                _zoneChoice = _world.Rules.ZoneRules.Length > 0
                    ? Math.Clamp(choice, 0, _world.Rules.ZoneRules.Length - 1)
                    : 0;

                break;

            case "street":
                _verb = Verb.Connect;

                break;

            case "demolish":
                _verb = Verb.Demolish;

                break;

            case "service":
                _verb = Verb.Service;

                // Zero means "the first kind that serves anything", which is what the s key sends
                // the first time it is pressed. A kind id is 1-based (Ruleset.KindCount).
                _serviceKind = choice > 0 && choice <= byte.MaxValue
                    ? (byte)choice
                    : NextService(0);

                break;

            default:
                // 🔴 DISARMED RATHER THAN LEFT AS IT WAS, and this was found by running it: a
                // misspelt tool left the PREVIOUS one held, so the next click acted with a verb
                // the script had not asked for. ***An unrecognised instruction must not leave a
                // loaded hand*** -- looking is the one verb that cannot do damage.
                _verb = Verb.Look;
                _refused = $"there is no tool called '{tool}'. There is look, zone, street, "
                    + "demolish and service.";

                break;
        }
    }

    /// <summary>The keyboard's own <c>hold</c>, so a hand's choice records exactly as a script's.</summary>
    private DriveCommand Held(string tool, int choice) =>
        new(_world.Tick.Raw, DriveVerb.Hold, choice, tool);

    /// <summary>
    /// Queues a command the city would accept, or <b>says in words why it would not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0045</c> row 15e, and the whole of it is here.</b> Every verb's refusals were
    /// an <c>InvalidOperationException</c> out of Phase 0 — the right artefact for a log, which must
    /// stop rather than diverge from the session it describes, and the wrong one for a person. This
    /// is the one place a <see cref="Command"/> reaches <see cref="_queued"/> from a click, so it is
    /// the one place that has to ask.
    /// </para>
    /// <para>
    /// ⚠ <b>The answer is good for exactly as long as the world stands still, and it does.</b>
    /// <see cref="Ordered"/> drains the queue as the argument to <c>Step</c>, so nothing runs between
    /// the question and the command applying. ***A shell that asked, stepped, and then sent would be
    /// guarding a city that no longer exists.***
    /// </para>
    /// </remarks>
    private bool Send(Command command)
    {
        Refusal refusal = _simulation.Refuses(command);

        if (refusal == Refusal.None)
        {
            _queued.Add(command);

            return true;
        }

        _refused = Sentence(refusal, command);

        return false;
    }

    /// <summary>
    /// A <see cref="Refusal"/> in the player's words — <b>and the shell owns every one of them.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The core hands back a number and this is where it becomes a sentence</b>, which is
    /// <c>CLAUDE.md</c>'s leak vector stated as a design rather than as a warning: <em>"the real leak
    /// vector is not <c>using Godot;</c> — it is a method that returns a formatted string because a
    /// panel wanted one."</em> A second front end may word these differently or in another language,
    /// and neither is the city's business.
    /// </para>
    /// <para>
    /// ⚠ <b>They are not the exception messages and must not be.</b>
    /// <c>Simulation.Explain</c> writes for whoever is holding a crash artefact and names the ADR,
    /// the successor mechanism and the Ruleset key; these are for somebody who has just clicked and
    /// wants to know why nothing happened. ***Same rule, two readers, two registers.***
    /// </para>
    /// <para>
    /// ⚠ <b>The unmapped arm names the number rather than saying nothing.</b>
    /// <c>src/Borough.Godot</c> is not in <c>Borough.slnx</c>, so no test can assert this table is
    /// complete — and a silent fallback would make a missing sentence look like a click that
    /// worked. <see cref="Borough.Tests"/> has no reach here; the screen is the only reviewer.
    /// </para>
    /// </remarks>
    private string Sentence(Refusal refusal, Command command) => refusal switch
    {
        Refusal.ConnectRoadKindIsNotStreet =>
            "only a Street can be laid by hand — an Arterial is a route rather than one click.",

        Refusal.ConnectWorldHasNoLattice =>
            "this Ruleset states no [roads] block_tiles, so it has no lattice to edit.",

        Refusal.DemolishNoBuildingOnThatTile =>
            "nothing stands on that plot to clear.",

        Refusal.DemolishBuildingIsOccupied =>
            "somebody still lives there. Clearing occupied ground is a compulsory purchase and its "
            + "price is not built, so only abandoned buildings can be cleared.",

        Refusal.ServiceKindNotDeclared =>
            "this Ruleset declares no such building.",

        Refusal.ServiceKindServesNothing =>
            $"{_names.Kind((byte)command.Zone) ?? "that building"} serves nobody, and only a service "
            + "building is placed by hand — everything else is built by the city on land you zone.",

        Refusal.ServiceNoVacantLotOnThatTile =>
            "that plot is taken. A standing building and an abandoned shell both hold one; demolish "
            + "first.",

        Refusal.GovernNoSuchPolicy or Refusal.GovernPolicyNotInThisWorld =>
            "this city has no such policy to govern.",

        Refusal.GovernPolicyHasNoName =>
            "that [[policy]] states no name, and a governed amount is saved against its name — so "
            + "there would be nothing to restore it to after a reload.",

        Refusal.TripRulesetStatesNoTrips or Refusal.TripWorldHasNoLattice
            or Refusal.TripBlockHoldsNobody or Refusal.TripEndpointsAreOneBuilding
            or Refusal.TripOriginHoldsNoCitizen =>
            "nobody can make that journey.",

        Refusal.ArriveNoGateOnThatTile =>
            "no gate stands there, so nobody can arrive through it.",

        Refusal.VerbNotApplied =>
            "that verb is not built yet.",

        _ => $"refused for reason {(ushort)refusal}, which this shell has no sentence for.",
    };

    /// <summary>One Street on the lattice edge leaving the intersection nearest the cursor.</summary>
    /// <remarks>
    /// ⚠ <b>The AXIS is chosen from where inside the block the cursor sits</b>, which is the only
    /// thing a single click can say about a choice between two edges. <c>adr/0077</c> makes a Connect
    /// exactly one Segment on the lattice, so the question is never <em>which route</em> — it is
    /// which of the two edges leaving one corner, and the cursor's own offset answers it.
    /// </remarks>
    private void Lay((Tiles East, Tiles North) at, bool bulldoze)
    {
        // ⚠ A world with no lattice has block 0, and the axis below would divide by it. The CORE
        // refuses that world by name (Refusal.ConnectWorldHasNoLattice) and Send is what reports it
        // -- so this is arithmetic the shell cannot do rather than a rule it is restating.
        int block = _world.Roads.Streets.BlockTiles;

        if (block <= 0)
        {
            Send(new Command(CommandKind.Connect, at.East, at.North, default));

            return;
        }

        int alongEast = ((at.East.Raw % block) + block) % block;
        int alongNorth = ((at.North.Raw % block) + block) % block;

        var payload = new ConnectPayload(
            alongEast >= alongNorth ? StreetAxis.East : StreetAxis.North,
            bulldoze ? ConnectAction.Bulldoze : ConnectAction.Lay,
            RoadKind.Street);

        Send(new Command(CommandKind.Connect, at.East, at.North, payload.Encode()));
    }

    /// <summary>Clears the abandoned Building nearest the cursor, at its own Lot's Tile.</summary>
    /// <remarks>
    /// 🔴 <b>The command names the LOT's Tile and never the cursor's, and that is not a convenience.</b>
    /// <c>Simulation.BuildingOn</c> matches a Lot's coordinate exactly and
    /// <c>ApplyDemolish</c> refuses rather than clearing the nearest — <em>"a mistyped command must
    /// not be indistinguishable from the demolition somebody meant"</em>. A cursor lands on a Tile
    /// that is almost never a Lot's own, so the shell resolves the click to a Building, shows which
    /// one in the hover, and then sends <b>that Building's address</b>. ***The refusal stays exact
    /// and the aim becomes possible.***
    /// </remarks>
    private void Clear((Tiles East, Tiles North) at)
    {
        (int building, int lot, _) =
            NearestIn(at, CellGrid.ToCells(at.East), CellGrid.ToCells(at.North));

        if (building == Rows.NoSlot)
        {
            _refused = "no Building stands in this Cell.";

            return;
        }

        Send(new Command(CommandKind.Demolish, _world.Lots.East[lot], _world.Lots.North[lot]));
    }

    /// <summary>Raises the held service kind on the vacant Lot nearest the cursor in its Cell.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>An O(Lots) walk, and it is the shell's second one.</b> There is no Cell index over Lots
    /// — <c>BuildingResidency</c> indexes Buildings, and a Lot is a point on a Segment rather than
    /// ground — so finding a vacant one near a Tile means walking the table. ⚠ <b>It runs on a CLICK
    /// and on a hover in this mode only</b>, never in the ordinary per-frame path, which is the one
    /// thing that keeps it off the same list as <c>Main.Draw</c>'s three whole-table walks
    /// (<c>plans/0013</c>). ***It is still an unpriced walk and the row says so.***
    /// </para>
    /// <para>
    /// ⚠ <b>The command names the LOT's Tile</b>, on <see cref="Clear"/>'s reasoning and
    /// <c>ApplyService</c>'s own: <em>"a school landing on a neighbour's plot because the click
    /// resolved to the first is worse than a refusal."</em>
    /// </para>
    /// </remarks>
    private void Raise((Tiles East, Tiles North) at)
    {
        if (_serviceKind == 0)
        {
            _refused = "this Ruleset declares no kind with a `serves` key, so there is no service to place.";

            return;
        }

        int lot = VacantNear(at);

        if (lot == Rows.NoSlot)
        {
            _refused =
                "no vacant Lot in this Cell. A Lot holding a Building — standing or an abandoned "
                + "shell — is not vacant; demolish first.";

            return;
        }

        // THE FACTORY AND NOT THE CONSTRUCTOR, for the reason Command.Govern's own remark gives:
        // the packing is named in one place rather than spelled at each call site.
        Send(Command.Service(_world.Lots.East[lot], _world.Lots.North[lot], _serviceKind));
    }

    /// <summary>The vacant Lot nearest a Tile within its own Cell, or <see cref="Rows.NoSlot"/>.</summary>
    private int VacantNear((Tiles East, Tiles North) at)
    {
        LotTable lots = _world.Lots;
        Cells east = CellGrid.ToCells(at.East);
        Cells north = CellGrid.ToCells(at.North);
        int nearest = Rows.NoSlot;
        long best = long.MaxValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot)
                || CellGrid.ToCells(lots.East[slot]) != east
                || CellGrid.ToCells(lots.North[slot]) != north)
            {
                continue;
            }

            long de = lots.East[slot].Raw - at.East.Raw;
            long dn = lots.North[slot].Raw - at.North.Raw;
            long away = (de * de) + (dn * dn);

            if (away < best)
            {
                best = away;
                nearest = slot;
            }
        }

        return nearest;
    }

    /// <summary>The first declared kind that serves a Need, or zero when the file declares none.</summary>
    /// <remarks>
    /// ⚠ <b>Ids run <c>1..KindCount</c></b> (<c>Ruleset.KindCount</c>), so zero is <em>none</em> and
    /// not the first kind — the same encoding <c>ApplyService</c> reads when it refuses an id nothing
    /// declares.
    /// </remarks>
    private byte NextService(byte after)
    {
        int count = _world.Rules.KindCount;

        for (int step = 1; step <= count; step++)
        {
            var kind = (byte)(((after + step - 1) % count) + 1);

            if (_world.Rules.Declares(kind) && _world.Rules.Kind(kind).Serves != Need.None)
            {
                return kind;
            }
        }

        return 0;
    }

    // ---- picking ---------------------------------------------------------------------------------

    /// <summary>
    /// Where the cursor meets the ground, in Tiles — <b>the shell's first screen-to-world query, and
    /// every player verb is blocked on it.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A ray against the ground plane and nothing more.</b> Godot's physics picking would want
    /// collision shapes on 262,144 MultiMesh instances, which is a body per box for a question that
    /// is one division — and <c>05 §2</c>'s boundary says the shell reads the world rather than
    /// modelling it twice.
    /// </para>
    /// <para>
    /// ⚠ <b>The horizon is REFUSED rather than clamped.</b> A ray that barely descends does meet the
    /// plane, thousands of Tiles away and behind the visible city, so clamping would hand back a
    /// confident answer about somewhere nobody is looking. ***A pick that cannot fail is a pick
    /// nobody can trust***, which is why this returns null and the readout says so.
    /// </para>
    /// <para>
    /// ⚠ <b>It answers in Tiles and the city is what resolves them.</b> The Building boxes on screen
    /// are the renderer's own invention — a frontage and a depth composed here, from a Lot that has
    /// neither (<c>adr/0078</c>) — so picking against the drawn geometry would pick the fiction.
    /// <see cref="Pointing"/> takes the Tile to the Cell and asks
    /// <see cref="BuildingResidency.In"/>, which is the query the simulation already uses.
    /// </para>
    /// </remarks>
    /// <returns>The Tile under the cursor, or <c>null</c> for the sky and for off-map ground.</returns>
    private (Tiles East, Tiles North)? Aim()
    {
        // A driven run names a Tile rather than a pixel, so there is no ray to cast: plans/0048's
        // whole finding is that a wall-clock channel addresses moments and the city is addressed in
        // Ticks, and the same is true of a place -- a screen position is a property of the camera.
        if (_aimed is { } driven)
        {
            return driven;
        }

        Vector2 at = GetViewport().GetMousePosition();
        Vector3 from = _camera.ProjectRayOrigin(at);
        Vector3 along = _camera.ProjectRayNormal(at);

        if (along.Y > -0.001f)
        {
            return null;
        }

        float toGround = -from.Y / along.Y;
        float east = (from.X + (along.X * toGround)) / MetresPerTile;
        float north = -(from.Z + (along.Z * toGround)) / MetresPerTile;

        if (east < 0f || north < 0f
            || east >= CellGrid.WorldTiles || north >= CellGrid.WorldTiles)
        {
            return null;
        }

        return (new Tiles((int)east), new Tiles((int)north));
    }

    /// <summary>
    /// Everything the city knows about the Tile under the cursor, <b>stacked most specific first.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every string here is the shell's</b> (<c>adr/0002</c>): the core hands back a kind id and
    /// this resolves it through <see cref="RulesetNames"/>. ***That is the leak vector
    /// <c>CLAUDE.md</c> actually names*** — not <c>using Godot;</c>, but a core method that returns a
    /// formatted string because a panel wanted one.
    /// </para>
    /// <para>
    /// 🔴 <b>A LINE IS OMITTED WHEN THE THING IT WOULD DESCRIBE HAS NO ROW, AND THAT IS THE WHOLE
    /// DESIGN OF THIS PANEL.</b> Nine of the shipped worlds have no Layer row anywhere, no District
    /// and no water, so a fixed template would print <c>pollution 0 · land value 0 · district 0</c>
    /// over every Tile of every one of them. ***Zero and absent are different answers, and a panel
    /// that renders them identically teaches a city has a quantity when it has no mechanism.***
    /// A Cell with no Layer row says nothing about pollution rather than saying none.
    /// </para>
    /// <para>
    /// ⚠ <b>Terrain is the one exception and it is stated rather than hidden.</b>
    /// <see cref="TerrainCellTable"/> is dense — one byte a Cell, on every world — so a file with no
    /// <c>[[terrain]]</c> reads <c>ordinary</c> everywhere truthfully. It is a uniform answer and not
    /// a missing one.
    /// </para>
    /// <para>
    /// ⚠ <b>Nearest within the Cell, and the Cell is the whole search.</b> A Lot is a point on a
    /// Segment rather than a plot of ground, so *containment* is not a question the city can answer —
    /// what it can answer is which Buildings are resident in a Cell, which is
    /// <see cref="BuildingResidency"/>'s own index. 🔴 <b>A Cell is 128 m and covers about four
    /// frontages</b>, so this is honest for a hover and too coarse for a verb that names one
    /// Building: the distance is printed so a person can see when the answer is a neighbour's.
    /// </para>
    /// </remarks>
    private string Pointing()
    {
        if (Aim() is not { } at)
        {
            return "— pointing off the map —";
        }

        Cells east = CellGrid.ToCells(at.East);
        Cells north = CellGrid.ToCells(at.North);
        var said = new System.Collections.Generic.List<string>
        {
            $"Tile ({at.East.Raw:N0}, {at.North.Raw:N0})    Cell ({east.Raw}, {north.Raw})",
        };

        Built(said, at, east, north);
        Underfoot(said, east, north);

        if (_verb == Verb.Zone)
        {
            Virgin(said, at);
        }

        if (_verb == Verb.Service)
        {
            int lot = VacantNear(at);

            said.Add(lot == Rows.NoSlot
                ? "no vacant Lot in this Cell — a shell is not vacant, demolish first"
                : $"would raise on Lot {_world.Lots.Rows.IdAt(lot):N0} at "
                    + $"({_world.Lots.East[lot].Raw:N0}, {_world.Lots.North[lot].Raw:N0})");
        }

        return string.Join('\n', said);
    }

    /// <summary>
    /// The Building nearest a Tile within its own Cell, with the Lot it stands on and the square of
    /// the distance.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Shared by the hover and by <see cref="Act"/>, and that sharing is the point.</b> A verb
    /// must act on the thing the panel named — <c>Demolish</c>'s own refusal says
    /// <em>"a mistyped command must not be indistinguishable from the demolition somebody meant"</em>
    /// — so the two must not resolve a click twice and risk disagreeing.
    /// </remarks>
    private (int Building, int Lot, long Away) NearestIn(
        (Tiles East, Tiles North) at, Cells east, Cells north)
    {
        Span<int> found = stackalloc int[64];
        int count = _world.BuildingsInCells.In(
            CellRect.At(east, north), _world.Buildings, found);

        LotTable lots = _world.Lots;
        int nearest = Rows.NoSlot;
        int onLot = Rows.NoSlot;
        long best = long.MaxValue;

        for (int seen = 0; seen < count; seen++)
        {
            if (!lots.Rows.TryResolve(_world.Buildings.Lot[found[seen]], out int lot))
            {
                continue;
            }

            long de = lots.East[lot].Raw - at.East.Raw;
            long dn = lots.North[lot].Raw - at.North.Raw;
            long away = (de * de) + (dn * dn);

            if (away < best)
            {
                best = away;
                nearest = found[seen];
                onLot = lot;
            }
        }

        return (nearest, onLot, best);
    }

    /// <summary>The Building nearest the cursor inside its Cell, its tenants and its trades.</summary>
    private void Built(
        System.Collections.Generic.List<string> said,
        (Tiles East, Tiles North) at,
        Cells east,
        Cells north)
    {
        (int nearest, int onLot, long best) = NearestIn(at, east, north);
        LotTable lots = _world.Lots;

        if (nearest == Rows.NoSlot)
        {
            said.Add("open ground — no Building in this Cell");

            return;
        }

        byte kind = _world.Buildings.Kind[nearest];
        string named = _names.Kind(kind) ?? $"kind {kind}";
        int room = _world.Rules.Declares(kind) ? _world.Rules.Kind(kind).Occupants : 0;
        int held = _world.Occupants.Length(nearest);
        int trades = _world.BuildingBusinesses.Length(nearest);

        // ⚠ THE CEILING COUNTS TENANTS OF ANY KIND (adr/0147), so a shop occupies one of the
        // declared occupants and the households line must not be read against the whole number.
        said.Add(_world.Buildings.IsAbandoned(nearest)
            ? $"{named} — ABANDONED, a shell standing on its collapse clock"
            : $"{named} — {held} of {room} occupied, {Math.Sqrt(best):N0} Tiles off");

        for (int business = _world.BuildingBusinesses.PeekFront(nearest);
             business != Rows.NoSlot && trades > 0;
             business = _world.Businesses.BuildingNext[business] - 1)
        {
            byte trade = _world.Businesses.Kind[business];

            said.Add($"    {_names.BusinessKind(trade) ?? $"trade {trade}"}");

            if (--trades == 0)
            {
                break;
            }
        }

        said.Add($"Lot {lots.Rows.IdAt(onLot):N0}, zone 0x{lots.Zone[onLot]:X4}");
    }

    /// <summary>What the ground under the cursor is, and only what it actually has a row for.</summary>
    private void Underfoot(
        System.Collections.Generic.List<string> said, Cells east, Cells north)
    {
        said.Add($"ground: {_world.Layers.Terrain.At(east, north).ToString().ToLowerInvariant()}");

        if (_world.WaterInCells.IsWet(east, north))
        {
            said.Add("under a Water Body");
        }

        int risk = _world.FloodInCells.DepthAt(_world.Flood, east, north);

        if (risk > 0)
        {
            // ⚠ A DEPTH IS THE FLOOD LEVEL MINUS THE GROUND, so a LARGE one is LOW ground. Saying
            // "below the flood line" rather than printing the number keeps the polarity legible.
            // ⚠ NOT THE SAME FACT AS THE TERRAIN LINE ABOVE, however alike they read.
            // TerrainKind.Floodplain is a quantile of a noise field (TerrainGenerator); this is the
            // Hazard Region, which is the water generator's flood level against the height field.
            // ***Two mechanisms that share a word***, so the wording has to separate them.
            said.Add($"AT FLOOD RISK — {risk:N0} below the flood line");
        }

        int layer = _world.Layers.Residency.Slot(east, north);

        if (layer != CellResidency.NotResident)
        {
            LayerCellTable cells = _world.Layers.Cells;

            said.Add(
                $"pollution {cells.Pollution[layer]:N0}    "
                + $"land value {cells.LandValue[layer]:N0}    "
                + $"sealing {cells.Sealing[layer]:N0}");
        }

        int district = _world.DistrictsInCells.Slot(east, north);

        if (district != DistrictResidency.NotResident)
        {
            said.Add($"District {_world.Districts.Rows.IdAt(district):N0}");
        }
    }

    /// <summary>
    /// How much of the block under the cursor is still virgin frontage. <b>Whether a click would do
    /// anything.</b>
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>Zone</c>'s commonest misuse is silent, which is why this line exists.</b> A block
    /// whose four faces are all claimed accepts the command, creates nothing and reports nothing —
    /// so the panel counts the faces that are present <em>and</em> unclaimed, which is exactly what
    /// <c>LotSubdivider.Face</c> tests. ***The alternative was a refusal, and a refusal would be
    /// wrong***: subdividing three of four faces is a real edit, and only the zero case is a no-op.
    /// </remarks>
    private void Virgin(System.Collections.Generic.List<string> said, (Tiles East, Tiles North) at)
    {
        StreetGrid streets = _world.Roads.Streets;

        if (streets.BlockTiles <= 0)
        {
            said.Add("no Street lattice in this world");

            return;
        }

        int column = IntegerMath.FloorDiv(at.East.Raw, streets.BlockTiles);
        int row = IntegerMath.FloorDiv(at.North.Raw, streets.BlockTiles);
        int free = 0;
        int faces = 0;

        // The four faces and the side of each that belongs to THIS block, which is
        // LotSubdivider.SubdivideBlock's own four constants rather than a second derivation.
        ReadOnlySpan<(int Segment, StreetSide Side)> around =
        [
            (streets.Horizontal(column, row), StreetSide.Left),
            (streets.Horizontal(column, row + 1), StreetSide.Right),
            (streets.Vertical(column, row), StreetSide.Right),
            (streets.Vertical(column + 1, row), StreetSide.Left),
        ];

        foreach ((int segment, StreetSide side) in around)
        {
            if (segment == Rows.NoSlot)
            {
                continue;
            }

            faces++;

            if (!_world.Frontage.Claimed(segment, side))
            {
                free++;
            }
        }

        said.Add(free > 0
            ? $"block ({column}, {row}) — {free} of {faces} faces still to subdivide"
            : $"block ({column}, {row}) — nothing to subdivide, a click does nothing");
    }

    /// <summary>The Cell under the cursor, so a person can see what they are aiming at.</summary>
    private void Cursor()
    {
        if (Aim() is not { } at)
        {
            _cursor.Multimesh.VisibleInstanceCount = 0;

            return;
        }

        _cursor.Multimesh.SetInstanceTransform(
            0, Tile(CellGrid.ToCells(at.East), CellGrid.ToCells(at.North), 0.03f));
        _cursor.Multimesh.VisibleInstanceCount = 1;
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

            // ⚠ STATED RATHER THAN INHERITED. A Label defaults to Ignore, so this changes nothing
            // today -- and a verb that stopped working because somebody set a theme's mouse filter
            // would be indistinguishable from the trackpad defect this file just fixed.
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        // BOTTOM LEFT AND GROWING UPWARD, which is what the anchors are for. The stack's LENGTH
        // varies with what is under the cursor -- a built Lot on a floodplain in a District says six
        // lines and open ground says two -- and a top-anchored panel would make every line move
        // whenever the one above it appeared. ***A readout that reflows as you sweep is unreadable
        // even when every line in it is right.***
        _hover = new Label
        {
            AnchorTop = 1f,
            AnchorBottom = 1f,
            GrowVertical = Control.GrowDirection.Begin,

            // ⚠ OFFSETS AND NOT Position, WHICH IS WHAT CLIPPED THE LAST LINE. On an anchored
            // Control, Position writes OffsetLeft/OffsetTop -- so setting it pinned the panel's TOP
            // to the window's bottom edge and every line grew off the screen. The bottom edge is the
            // one that has to be pinned when the stack grows upward.
            OffsetLeft = 16f,
            OffsetBottom = -16f,
            LabelSettings = new LabelSettings { FontSize = 16 },
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

        var layer = new CanvasLayer();

        layer.AddChild(_readout);
        layer.AddChild(_hover);
        AddChild(layer);
        Tuner(layer);
    }

    /// <summary>End the run, <b>writing the session first where <c>BOROUGH_LOG</c> asked</b>.</summary>
    /// <remarks>
    /// ⚠ <b>On <c>BOROUGH_SHOT</c>'s own bargain, and for a stronger reason than the photograph
    /// has.</b> A machine with no hands cannot press <c>w</c>, so without this the round trip — play,
    /// write, replay, compare — could only ever be run by a person, and ***a verification nobody can
    /// run in a script is one nobody runs twice***.
    /// <para>
    /// ⚠ <b>It hangs on the end of the run rather than beside the shutter</b>, which is where it was
    /// written: every ending arrives here, since <c>--quit-at</c> is a <c>quit</c> command on the end
    /// of the script (<see cref="Driven"/>) and <c>esc</c> is the same command from a keyboard. A
    /// driven run that never takes a picture still writes its log.
    /// </para>
    /// </remarks>
    private int Quit()
    {
        if (System.Environment.GetEnvironmentVariable("BOROUGH_LOG") is not null)
        {
            Record();
        }

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
        Governing(layer);
    }

    /// <summary>
    /// The governing panel: every declared <c>[[policy]]</c>, its amount, and a way to set it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>NOT the tuner, and the separation is the decision.</b> The tuner rewrites Ruleset text
    /// and regenerates a <em>new city</em>; this issues a <c>Govern</c> <see cref="Command"/> against
    /// the city that is running, at a Tick, through the door a replay reproduces. ***One edits the
    /// world's premises and the other plays the game.***
    /// </para>
    /// <para>
    /// ⚠ <b>An unnamed <c>[[policy]]</c> is shown and disabled rather than omitted.</b> Its
    /// <c>Ruleset.PolicyKeys</c> entry is zero and <c>ApplyGovern</c> refuses it — a governed amount
    /// is saved state and a name is the only thing that survives a renumbering. ***Omitting the row
    /// would shift every position below it***, and <c>Govern</c> addresses a Policy by exactly that
    /// position.
    /// </para>
    /// <para>
    /// ⚠ <b>The amount is <c>Command.East</c></b>, which is a field whose name says <em>where</em>.
    /// <c>Command.Govern</c> is the factory that says so; the struct is twelve fully-defined bytes
    /// and widening it would re-spell every committed Input Log.
    /// </para>
    /// </remarks>
    private void Governing(CanvasLayer layer)
    {
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        _policyPanel = new PanelContainer { Visible = false, Position = new Vector2(14f, 108f) };

        _policyPanel.AddThemeStyleboxOverride("panel", backing);

        var box = new VBoxContainer();

        box.AddChild(new Label { Text = "GOVERN — p closes. enter sets the amount." });

        // 🔴 THE FIELD IS THE TRANSFER AMOUNT AND NOT THE TAX RATE, and a panel that did not say so
        // would be actively misleading: on levied.toml every row reads `1` while the levy that
        // actually bites is `percent = 10` in the apply rule. Govern writes PolicyTable.Amount and
        // nothing else -- ApplyCount is Ruleset data and is not governable -- so a person turning
        // this dial expecting a rate would change the wrong number and watch nothing happen.
        box.AddChild(new Label
        {
            Text = "what ONE application moves. how many and what share is the [[policy]]'s"
                + " apply rule, which is not governable.",
        });

        PolicyDefinition[] declared = _world.Rules.Policies;

        _policyFields = new LineEdit[declared.Length];

        for (int at = 0; at < declared.Length; at++)
        {
            var row = new HBoxContainer();
            bool governable = _world.Rules.PolicyKey(at) != 0;
            string named = _names.Policy(at) ?? "unnamed";

            row.AddChild(new Label
            {
                Text = governable
                    ? $"{named} — sweeps {declared[at].Subject.ToString().ToLowerInvariant()} "
                        + $"every {declared[at].Interval:N0}"
                    : $"{named} (no name — ungovernable)",
                CustomMinimumSize = new Vector2(340f, 0f),
            });

            var field = new LineEdit
            {
                Text = _world.Policies.AmountOf(at, declared[at]).ToString(),
                Editable = governable,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            int position = at;

            field.TextSubmitted += _ => Govern(position);
            _policyFields[at] = field;
            row.AddChild(field);
            box.AddChild(row);
        }

        if (declared.Length == 0)
        {
            box.AddChild(new Label { Text = "this Ruleset declares no [[policy]]." });
        }

        _policyStatus = new Label { Text = "a governed amount is saved state and survives a reload." };

        box.AddChild(_policyStatus);
        _policyPanel.AddChild(box);
        layer.AddChild(_policyPanel);
    }

    /// <summary>Re-reads every field off the world, so an open panel shows what is in force.</summary>
    private void ShowPolicies()
    {
        PolicyDefinition[] declared = _world.Rules.Policies;

        for (int at = 0; at < _policyFields.Length && at < declared.Length; at++)
        {
            _policyFields[at].Text = _world.Policies.AmountOf(at, declared[at]).ToString();
        }
    }

    /// <summary>Queues a <c>Govern</c> for one Policy, or says why it cannot.</summary>
    private void Govern(int position)
    {
        if (!int.TryParse(_policyFields[position].Text, out int amount))
        {
            _policyStatus.Text = "that is not a whole number.";

            return;
        }

        // ⚠ THE PANEL RESTATED ONE OF Govern'S THREE REFUSALS AND COULD NOT SEE THE OTHER TWO.
        // Simulation.Refuses answers all three off the applier's own predicate, and the panel says
        // whichever one it gave -- so a Policy this world holds no row for is now a sentence rather
        // than a half-stepped Tick.
        if (!Send(Command.Govern(position, amount)))
        {
            _policyStatus.Text = _refused;

            return;
        }

        _policyStatus.Text =
            $"{_names.Policy(position) ?? $"policy {position}"} set to {amount:N0} "
            + $"on Tick {_world.Tick.Raw + 1:N0}.";
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
        _names = loaded.Names;
        _citizens = citizens;
        _seed = seed;

        var key = WorldKey.FromSeed(seed);

        _world = new World(citizens, loaded.Ruleset, key);
        _simulation = new Simulation(_world, key) { VerifyDecideWritesNothing = false };

        _log = new InputLogBuilder(
            _seed,
            new WorldConfiguration(citizens),
            RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml)));

        // 🔴 THE CITY ARRIVES THROUGH Simulation.Apply AND NOT BESIDE IT. This was
        // SyntheticCity.PopulateInto called on the world directly, which is thousands of rows
        // entering by a door the log does not account for -- and every hand-played session would
        // therefore have replayed against an EMPTY world and diverged at Tick 0, with nothing in
        // the file to explain it. Borough.Headless has recorded the population as a Command since
        // slice 6 (Session.Load); the shell just did not.
        //
        // ⚠ IT COSTS ONE TICK, AND THAT IS THE HEADLESS RUNNER'S BEHAVIOUR RATHER THAN A CHARGE.
        // A Command applies at the top of a Tick, so a world populated by one is populated during
        // Tick 0 and the readout opens at Tick 1. A shell that opened at Tick 0 with a city in it
        // would be one Tick ahead of the runner for the whole session.
        _log.Append(Ticks.Zero, new Command(CommandKind.Populate, default, default));
        _simulation.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));

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

/// <summary>
/// What a click does. <b><c>01 §2</c>'s player verbs, and deliberately not the instruments.</b>
/// </summary>
/// <remarks>
/// <c>Populate</c>, <c>Trip</c> and <c>Arrive</c> are <see cref="CommandKind"/> members and are
/// <em>not</em> here: each says in its own remark that it is <b>an instrument rather than one of
/// <c>01 §2</c>'s five</b> and that it is expected to be deleted. ***A verb list assembled from the
/// enum would have handed the player three doors the design intends to close.***
/// </remarks>
internal enum Verb : byte
{
    /// <summary>Read the city and change nothing. The default, and where every mode returns to.</summary>
    Look = 0,

    /// <summary>
    /// Subdivide the block under the cursor into Lots admitting one <c>[[zone_rule]]</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>SUBDIVIDE and not paint, however the verb is named in <c>01 §2</c>.</b>
    /// <c>LotSubdivider.Face</c> returns zero on frontage <c>World.Frontage</c> has already claimed,
    /// so this works on virgin faces and can never repaint a block that has Lots.
    /// <c>PlayerVerbTests.Zoning_ground_that_is_already_subdivided_changes_nothing</c> holds it.
    /// </remarks>
    Zone = 1,

    /// <summary>Lay or bulldoze one Street on the lattice edge leaving the nearest intersection.</summary>
    Connect = 2,

    /// <summary>Clear abandoned stock, and only that.</summary>
    Demolish = 3,

    /// <summary>
    /// Raise one service Building on the vacant Lot under the cursor.
    /// </summary>
    /// <remarks>
    /// 🔴 <b><c>01 §5</c>'s ONE placement exception, and the only verb here that puts a Building on
    /// the ground.</b> Every other kind arrives through a Zone Rule filling in a permission set the
    /// player painted; <c>Simulation.ApplyService</c> refuses a kind declaring no <c>serves</c> by
    /// name, which is what stops the exception becoming a general <em>place anything</em> verb.
    /// </remarks>
    Service = 4,
}
