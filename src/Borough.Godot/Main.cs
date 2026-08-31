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

    /// <summary>In-world seconds a Tick is worth, which is what turns a rung into a rate.</summary>
    private const double SecondsPerTick = 86_400.0 / Ticks.PerDay;

    /// <summary>
    /// The speed ladder, in Ticks a second. <b>1× is 16, and the four rungs below 0.5× are this
    /// shell's rather than <c>01 §1</c>'s.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SHIPPED LADDER'S SLOWEST RUNG IS TOO FAST TO WATCH A PERSON, BY A FACTOR OF ABOUT
    /// SIXTEEN.</b> <c>TICKS_PER_DAY = 2048</c> makes a Tick 42.19 s of in-world time, so 1× runs the
    /// world at <b>675× real time</b>: a 20-minute commute is <b>1.8 real seconds</b>, a walker
    /// crosses a 128 m block in <b>0.14 s</b>, and a car crosses one in <b>0.014 s</b> — under a
    /// frame at 60 Hz, at <em>every</em> rung <c>01 §1</c> offers.
    /// </para>
    /// <para>
    /// ⚠ <b><c>01 §1</c> says <em>traffic is visually truthful</em> at 0.5× and that is wrong by
    /// 337×.</b> Truthfulness is by definition 1× <em>real</em> time, which is <b>0.0237 Ticks/s</b>
    /// — 1/675 of the ladder's 1×. <c>§7</c> records a concession that an untouched speed control
    /// shows traffic <em>"roughly twice as fast as its apparent size warrants"</em>; the figure is
    /// 675. ***A two-minute Day and truthful traffic are 675× apart and no one ladder holds both.***
    /// </para>
    /// <para>
    /// <b>1 Tick/s is the rung at which a walker is watchable</b> — a block in 2.2 s, a commute in
    /// 28 s, a Day in 34 minutes. Below it a Day stops being a sitting.
    /// </para>
    /// </remarks>
    private static readonly double[] Ladder = [0.0, 0.25, 1.0, 4.0, 8.0, 16.0, 32.0, 48.0, 64.0];

    /// <summary>What each rung is called, against <c>01 §1</c>'s 1× of 16 Ticks a second.</summary>
    private static readonly string[] Rungs =
        ["paused", "1/64x", "1/16x", "1/4x", "0.5x", "1x", "2x", "3x", "4x"];

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

    public override void _Ready()
    {
        int citizens = (int)(GetMeta("citizens", 1_000));
        string path = ProjectSettings.GlobalizePath("res://../../rulesets/minimal.toml");

        RulesetLoadResult loaded = RulesetLoader.Parse(File.ReadAllText(path), "minimal.toml");

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
        _buildings = Layer(new Color(0.55f, 0.52f, 0.45f), new Vector3(6f, 10f, 6f));
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
        if (System.Environment.GetEnvironmentVariable("BOROUGH_SHOT") is { } shot
            && ++_frame == int.Parse(System.Environment.GetEnvironmentVariable("BOROUGH_SHOT_AT") ?? "120"))
        {
            RenderingServer.ForceDraw();
            GetViewport().GetTexture().GetImage().SavePng(shot);
            GD.Print($"wrote {shot}");
            GetTree().Quit();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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

    /// <summary>Reads the world into the two meshes that change, and writes the readout.</summary>
    private void Draw(Ratio alpha)
    {
        int drawn = Fill(_buildings, Buildings());
        int moving = VisibleAgents.In(_world, CellRect.World, alpha, _agents);

        Fill(_travellers, Travellers(moving));

        ulong tick = _world.Tick.Raw;
        ulong ofDay = tick % (ulong)Ticks.PerDay;

        _readout.Text =
            $"Tick {tick:N0}   Day {tick / (ulong)Ticks.PerDay}   "
            + $"{ofDay * 24 / (ulong)Ticks.PerDay:00}:{ofDay * 1440 / (ulong)Ticks.PerDay % 60:00}\n"
            + $"Citizens {_world.Citizens.Rows.LiveCount:N0}   Buildings {drawn:N0}   "
            + $"travelling {moving:N0}\n"
            + $"speed {Rungs[_rung]} — {Ladder[_rung] * SecondsPerTick:N0}x real time   "
            + "[ ] slower/faster, space pause, 1-4, esc quit";
    }

    /// <summary>Every standing Building, at its Lot.</summary>
    private System.Collections.Generic.IEnumerable<Vector3> Buildings()
    {
        BuildingTable table = _world.Buildings;
        LotTable lots = _world.Lots;

        for (int slot = 0; slot < table.Rows.SlotCount; slot++)
        {
            if (table.Rows.IsLive(slot) && lots.Rows.TryResolve(table.Lot[slot], out int lot))
            {
                yield return new Vector3(
                    lots.East[lot].Raw * MetresPerTile, 0f, -lots.North[lot].Raw * MetresPerTile);
            }
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
                * Basis.FromScale(new Vector3(8f, 1f, from.DistanceTo(to)));

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

        var camera = new Camera3D { Far = 20_000f, Fov = 60f };

        AddChild(camera);
        camera.LookAtFromPosition(
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
