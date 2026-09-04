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

// ---- the channels -- every way a command reaches the shell, and every file it writes back ----
//
// A driven script, a socket line and a hand at the keyboard all arrive at Apply, which is what
// stops a verb meaning two things depending on how it was sent (plans/0048). Shoot, DrawList and
// Write are the other direction: what a run leaves behind for somebody to read.
//
// NOT here: _UnhandledInput, which is the keyboard and stays beside the node it is a method of.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
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

            case DriveVerb.Lens:
                Lens(command.Amount != 0);

                break;

            case DriveVerb.Overlay:
                Washing(command.Path!);

                break;

            case DriveVerb.Tilt:
                // Degrees in, radians held. The clamp lives here rather than in the grammar because
                // the bounds are the SHELL's -- they come from the ground mesh and from LookAt's
                // up-vector, neither of which Borough.Formats knows anything about.
                _pitch = Mathf.Clamp(
                    Mathf.DegToRad(command.Amount), LowestRadians, HighestRadians);

                Orbit();

                break;

            case DriveVerb.Focus:
                // ⚠ THE DISTANCE IS NOT CLAMPED AND A HAND'S ZOOM IS. Nearest/Furthest are derived
                // from the CITY's span, which is the right leash for a mouse wheel and the wrong one
                // for a review: it stops three kilometres out on a 65-kilometre map. A script is not
                // a hand, and this is the one way to look at the other sixty-two.
                _focus = new Vector3(
                    command.East * MetresPerTile, 0f, -command.North * MetresPerTile);

                if (command.Amount > 0)
                {
                    _distance = command.Amount;
                }

                Orbit();

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

            case DriveVerb.Release:
                // The bounds are checked on the same reasoning as Click's above: a driven release
                // names a Tile and meets no ray. ⚠ It CHANGES NOTHING and only speaks, so an
                // off-map one is the same non-event a click's is.
                if (command.East < 0 || command.North < 0
                    || command.East >= CellGrid.WorldTiles || command.North >= CellGrid.WorldTiles)
                {
                    _refused = "that is not on the map.";

                    break;
                }

                _aimed = (new Tiles(command.East), new Tiles(command.North));

                Dragged((new Tiles(command.East), new Tiles(command.North)));

                break;

            case DriveVerb.People:
                // 🔴 plans/0045 ROW 21. --empty gives adr/0090's world -- ground, and every road the
                // player's -- and until this verb existed nothing could ever be built on the Streets
                // they laid: the chain is Households -> the Unplaced Pool -> placement -> Buildings,
                // and an empty world has an empty Pool. Measured on minimal.toml before it landed: 40
                // Street clicks and 16 zone clicks gave 40 Segments and 128 Lots, and five in-world
                // Days later the readout said 0 Buildings and 0 Citizens.
                //
                // ⚠ THROUGH Send LIKE A CLICK, and not onto the world beside it. A population that
                // arrived any other way is a state change no replay reproduces -- Command.cs's
                // argument for why Populate is a verb at all -- and Send is also what keeps the two
                // ways of getting this wrong (populating twice, populating before zoning) a sentence
                // on screen rather than an exception half way through a Tick.
                Send(new Command(CommandKind.People, default, default));

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
        // 🔴 RECOMPOSED BEFORE THE SHUTTER, BECAUSE Draw RAN BEFORE THIS FRAME'S COMMANDS DID.
        // Found by running it: 'pause' then 'shoot' on one Tick wrote a caption reading "speed 1x",
        // which is the rung the frame was composed with and not the one the picture was taken at.
        Draw(_alpha);

        // 🔴 AND THE PICTURE IS TAKEN NEXT FRAME, because recomposing repairs the CAPTION and cannot
        // repair the FRAME: a Control's new text reaches the renderer when the engine draws, which
        // is after _Process returns. Two ForceDraws do not bring it forward. ***The caption said
        // Tick 140 over a picture of Tick 139 for as long as shoot has existed***, and it was
        // invisible because a city one Tick apart looks identical -- it took a tool palette, whose
        // whole content changes on one command, to make the lag legible. See _pendingShot.
        _pendingShot = path;

        // Written now rather than with the picture: this frame's compose is the one the picture will
        // carry, and holding the caption over would be a second reading of a world that has moved.
        Write(Path.ChangeExtension(path, ".txt"));
    }

    /// <summary>Save the frame the engine has just drawn.</summary>
    /// <remarks>
    /// ⚠ <b>THE VIEWPORT HAS NO PICTURE UNDER <c>--headless</c>.</b> <c>GetTexture()</c> is not null
    /// there — what is empty is what the dummy renderer has behind its RID, which surfaces as an
    /// engine error and a null out of <c>GetImage()</c>. ***A guard that checks the handle misses an
    /// emptiness that is in what the handle points at***, so the display server is asked first, where
    /// the capability is actually declared. <b>The caption is already written</b>: a run with no
    /// screen still says what the city was doing.
    /// </remarks>
    private void Capture(string path)
    {
        if (DisplayServer.GetName() == "headless")
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: --headless renders nothing.");
        }
        else if (GetViewport().GetTexture()?.GetImage() is { } picture)
        {
            picture.SavePng(Globalize(path));
            GD.Print($"wrote {path} at Tick {_world.Tick.Raw}");
        }
        else
        {
            GD.Print($"no picture at Tick {_world.Tick.Raw}: the viewport rendered nothing.");
        }
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
                $"ok\t{_world.Tick.Raw}\t{Captioned().Replace('\n', '\t')}"));
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
        ("road", _roads, true, _roadIds),
        ("cell", _cells, false, null),
        ("plot", _plots, true, _plotIds),
        ("building", _buildings, true, _buildingIds),
        ("roof", _roofs, true, _roofIds),
        ("hip", _hips, true, _hipIds),
        ("mansard", _mansards, true, _mansardIds),
        ("yard", _yards, true, _yardIds),
        ("tree", _trees, false, null),
        ("rock", _rocks, false, null),
        ("traveller", _travellers, false, _travellerIds),
    ];

    /// <summary>
    /// A number in a row. <b>Fixed, invariant and rounded</b>, so two runs diff rather than differ.
    /// </summary>
    private static string Figure(float value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Put the readout on disk as it stands. Composed by the caller.</summary>
    /// <remarks>
    /// 🔴 <b>THE HOVER IS ON THE SCREEN AND WAS NOT IN THE CAPTION.</b> Every channel carried
    /// <c>_readout.Text</c> alone — <c>readout</c>, <c>shoot</c> and the socket's reply — so the
    /// panel that says <em>what a click would do here</em> was invisible to every driven run.
    /// ***A caption that omits half the screen cannot be asserted against the half it omits***, and
    /// the hover is where <see cref="Aiming"/>, <see cref="Virgin"/> and <see cref="Crossing"/> all
    /// write. Found by driving <c>plans/0045</c> row 23 and looking for a sentence that was on
    /// screen the whole time.
    /// </remarks>
    private void Write(string path) =>
        File.WriteAllText(Globalize(path), $"tick {_world.Tick.Raw}\n{Captioned()}\n");

    /// <summary>
    /// The readout and the hover, in the order they read — <b>what a driven run is told is on
    /// screen.</b>
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The rule stays <em>a <c>Control</c> is invisible to a channel</em></b>, and this is one
    /// panel joining the caption rather than a general escape from it: the tool palette and the two
    /// editing panels are still checked against a line of the readout, as <see cref="Palette"/>'s
    /// own remark says. ⚠ <b>A separator line rather than a blank one</b>, so a caller greps for
    /// <c>hover</c> and knows which panel a line came from.
    /// </remarks>
    private string Captioned() => $"{_readout.Text}\nhover —\n{_hover.Text}";
}
