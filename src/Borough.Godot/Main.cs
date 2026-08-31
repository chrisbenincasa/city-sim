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

    /// <summary>How wide the carriageway is drawn. A drawing width, and no Segment states one.</summary>
    private const float RoadWidthMetres = 8f;

    /// <summary>How deep and wide a Building's box is drawn.</summary>
    private const float BuildingFootprintMetres = 6f;

    /// <summary>How tall a Building's box is drawn.</summary>
    private const float BuildingHeightMetres = 10f;

    /// <summary>
    /// How far off the Segment a Building stands — <b>derived, so the two boxes cannot overlap</b>.
    /// </summary>
    /// <remarks>
    /// Half the carriageway plus half the footprint puts the Building's near face on the kerb. It is
    /// not a Ruleset number and must not become one: <b>a Lot has no depth</b> and there is no depth
    /// key (<c>adr/0078</c>), so this is the renderer inventing a thickness the city does not have.
    /// </remarks>
    private const float SetbackMetres = (RoadWidthMetres + BuildingFootprintMetres) * 0.5f;

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
    private Label _readout = null!;
    private VisibleAgent[] _agents = new VisibleAgent[8192];
    private double _owed;
    private int _rung = DesignSpeed;
    private int _resume = DesignSpeed;
    private int _frame;
    private string _rulesetPath = "rulesets/minimal.toml";
    private Camera3D _camera = null!;
    private float _span = 512f;

    public override void _Ready()
    {
        (_rulesetPath, int citizens) = Arguments();

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

        _roads = Layer(new Color(0.30f, 0.30f, 0.33f), new Vector3(1f, 0.1f, 1f));
        _buildings = Layer(
            new Color(0.55f, 0.52f, 0.45f),
            new Vector3(BuildingFootprintMetres, BuildingHeightMetres, BuildingFootprintMetres));
        _travellers = Layer(new Color(1.0f, 0.45f, 0.15f), new Vector3(3f, 3f, 3f));

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
        if (System.Environment.GetEnvironmentVariable("BOROUGH_SHOT") is { } shot
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
            // Dollying along the camera's own forward axis rather than changing the field of view:
            // a zoom that moves the eye keeps the perspective the picture was framed with.
            if (click.ButtonIndex == MouseButton.WheelUp)
            {
                _camera.Position += _camera.Basis.Z * -_span * 0.08f;
            }
            else if (click.ButtonIndex == MouseButton.WheelDown)
            {
                _camera.Position += _camera.Basis.Z * _span * 0.08f;
            }

            return;
        }

        // Dragging with any button held pans across the ground, never through it, so the city
        // cannot be lost behind the camera by a careless drag.
        if (@event is InputEventMouseMotion drag && drag.ButtonMask != 0)
        {
            float reach = _span * 0.0016f;

            _camera.Position += new Vector3(
                -drag.Relative.X * reach, 0f, -drag.Relative.Y * reach);

            return;
        }

        if (@event is not InputEventKey { Pressed: true } key)
        {
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
    /// <c>--ruleset PATH</c> and <c>--citizens N</c>, after Godot's own <c>--</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A shell reads the command line and the core does not.</b> Every string here is this
    /// project's (<c>adr/0002</c>), and a bad one is reported rather than defaulted, because a
    /// silently-substituted world is a picture of somewhere else.
    /// </remarks>
    private static (string Ruleset, int Citizens) Arguments()
    {
        string ruleset = "rulesets/minimal.toml";
        int citizens = 1_000;
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
        }

        return (ruleset, citizens);
    }

    /// <summary>Reads the world into the two meshes that change, and writes the readout.</summary>
    private void Draw(Ratio alpha)
    {
        int drawn = Fill(_buildings, Buildings());
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);

        Fill(_travellers, Travellers(moving));

        ulong tick = _world.Tick.Raw;
        ulong ofDay = tick % (ulong)Ticks.PerDay;

        _readout.Text =
            $"{System.IO.Path.GetFileName(_rulesetPath)}   Tick {tick:N0}   "
            + $"Day {tick / (ulong)Ticks.PerDay}   "
            + $"{ofDay * 24 / (ulong)Ticks.PerDay:00}:{ofDay * 1440 / (ulong)Ticks.PerDay % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"travelling {moving:N0}\n"
            + $"speed {Pace(_rung)}   "
            + "[ ] speed, space pause, 1-4, drag pan, wheel zoom, esc quit";
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

    /// <summary>Every standing Building, at its Lot.</summary>
    private System.Collections.Generic.IEnumerable<Vector3> Buildings()
    {
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;
        int block = _world.Roads.Streets.BlockTiles;

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
            if (block > 0 && lots.North[lot].Raw % block == 0)
            {
                north += side == StreetSide.Left ? SetbackMetres : -SetbackMetres;
            }
            else
            {
                east += side == StreetSide.Right ? SetbackMetres : -SetbackMetres;
            }

            yield return new Vector3(east, BuildingHeightMetres * 0.5f, -north);
        }
    }

    /// <summary>Every Traveller the last query placed.</summary>
    private System.Collections.Generic.IEnumerable<Vector3> Travellers(int found)
    {
        for (int agent = 0; agent < found; agent++)
        {
            yield return new Vector3(
                _agents[agent].East.Raw * MetresPerTile / 65_536f,
                4f,
                -_agents[agent].North.Raw * MetresPerTile / 65_536f);
        }
    }

    /// <summary>Writes positions into a MultiMesh and returns how many there were.</summary>
    private static int Fill(
        MultiMeshInstance3D into, System.Collections.Generic.IEnumerable<Vector3> places)
    {
        int count = 0;

        foreach (Vector3 place in places)
        {
            if (count >= into.Multimesh.InstanceCount)
            {
                break;
            }

            into.Multimesh.SetInstanceTransform(count++, new Transform3D(Basis.Identity, place));
        }

        into.Multimesh.VisibleInstanceCount = count;

        return count;
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
    private MultiMeshInstance3D Layer(Color colour, Vector3 size)
    {
        var mesh = new BoxMesh { Size = size };
        var material = new StandardMaterial3D
        {
            AlbedoColor = colour,
            Roughness = 0.9f,
        };

        mesh.Material = material;

        var multi = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = 65_536,
            VisibleInstanceCount = 0,
        };

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
        _camera = new Camera3D { Far = 200_000f, Fov = 60f };

        AddChild(_camera);
        _camera.LookAtFromPosition(
            centre + new Vector3(0f, span * 0.62f, span * 0.52f), centre, Vector3.Up);
    }

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
