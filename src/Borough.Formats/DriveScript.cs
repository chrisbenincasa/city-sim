using System.Globalization;

namespace Borough.Formats;

/// <summary>What a driven shell can be told to do. One entry per line of a drive script.</summary>
/// <remarks>
/// ⚠ <b>Every verb here is an ABSOLUTE and none is a toggle</b>, which is the whole reason a driven
/// run is reproducible where the keyboard is not. <c>g</c> hides the roads if they are shown; a
/// script saying <c>roads off</c> means the same thing whatever ran before it. The keyboard still
/// toggles — it reads the current state and calls the absolute — so there is one applier rather than
/// two behaviours that can part company.
/// </remarks>
public enum DriveVerb
{
    /// <summary>Stop the clock. The rung is remembered, exactly as the space bar remembers it.</summary>
    Pause,

    /// <summary>Start it again at the remembered rung.</summary>
    Resume,

    /// <summary>Set the rung. <c>Amount</c> indexes the shell's ladder, and the shell clamps.</summary>
    Speed,

    /// <summary>Show or hide the carriageway. <c>Amount</c> is 1 or 0.</summary>
    Roads,

    /// <summary>Show or hide the 128 m Cell lattice. <c>Amount</c> is 1 or 0.</summary>
    Cells,

    /// <summary>Turn the eye a quarter. <c>Amount</c> is -1 left or +1 right.</summary>
    Turn,

    /// <summary>Dolly the eye. <c>Amount</c> is notches, positive in.</summary>
    Zoom,

    /// <summary>Write the frame to <c>Path</c>, and the readout beside it.</summary>
    Shoot,

    /// <summary>Write the readout to <c>Path</c>. Needs no display.</summary>
    Readout,

    /// <summary>Write the draw list to <c>Path</c> — every instance on screen, as data.</summary>
    Draw,

    /// <summary>
    /// Choose what the next <see cref="Click"/> means. <c>Path</c> names the tool.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>Amount</c> chooses WHICH one, where a tool has more than one</b> — a Zone Rule by
    /// declaration position, a service by its kind id — which is what makes the verb an absolute
    /// rather than a cycle. ***The keyboard is what cycles***: <c>z</c> reads what is held and asks
    /// for the next one, so a recorded session replays the choice rather than the keypress.
    /// </remarks>
    /// <remarks>
    /// 🔴 <b>The tool is a WORD and not an index, and that is deliberate.</b> Which verbs a shell
    /// offers is the shell's own fact — <c>Borough.Formats</c> knows nothing about looking, zoning or
    /// demolishing, and an ordinal here would be a second copy of an enum in <c>Borough.Godot</c>
    /// that no test can reach. ***The grammar carries the name and the shell resolves it***, so an
    /// unknown tool is a refusal a person reads rather than a click that quietly meant something
    /// else.
    /// </remarks>
    Hold,

    /// <summary>
    /// Act at a Tile with the held tool. <c>East</c> and <c>North</c> are the Tile;
    /// <c>Amount</c> is 1 when shift was held, which is what turns <c>street</c> into a bulldoze.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE FIRST DRIVE VERB THAT CHANGES THE CITY, which is <c>plans/0048</c> <b>D1</b>
    /// arriving.</b> Every verb above it moves the clock, the eye or a file, and D1 was left open
    /// because none of them could reach the world. It is answered by where a click already goes:
    /// the shell turns it into a <c>Command</c> and queues it, and <c>Ordered()</c> — the one drain
    /// — appends every queued Command to the Input Log. ***So a driven click enters the log by the
    /// same door a hand's does***, and neither the tool nor the aim is a second channel.
    /// </remarks>
    Click,

    /// <summary>End the run.</summary>
    Quit,
}

/// <summary>One instruction, and <b>the Tick it lands on rather than the moment it arrives</b>.</summary>
/// <param name="At">The Tick. The shell steps up to it and no further before applying this.</param>
/// <param name="Verb">What to do.</param>
/// <param name="Amount">The verb's quantity, or 0 where it takes none.</param>
/// <param name="Path">Where to write, for the two verbs that produce a file.</param>
/// <param name="East">The Tile a <see cref="DriveVerb.Click"/> lands on, east.</param>
/// <param name="North">And north. Both are zero for every verb that does not aim.</param>
public readonly record struct DriveCommand(
    ulong At, DriveVerb Verb, int Amount, string? Path, int East = 0, int North = 0);

/// <summary>
/// A drive script: <c>&lt;tick&gt; &lt;verb&gt; [argument]</c>, one per line, <c>#</c> comments.
/// </summary>
/// <remarks>
/// <para>
/// <b>It lives here and not in the shell because the shell is not in <c>Borough.slnx</c></b> and
/// nothing written there is covered by a test. The parse is the half that can hold a defect quietly,
/// so the parse is the half that is assertable; what stays in <c>Borough.Godot</c> is the binding
/// from a verb to a Godot call, which fails loudly the first time it is run.
/// </para>
/// <para>
/// ⚠ <b>Ticks must not go backwards and nothing may follow <c>quit</c>.</b> Both are refused rather
/// than sorted or trimmed: a script whose author lost track of the order is a script whose author
/// will misread its output, and <c>adr/0015</c>'s standard for a refusal — a line and a reason — is
/// cheaper here than a silently reordered run.
/// </para>
/// </remarks>
public static class DriveScript
{
    /// <summary>How far one <c>zoom</c> goes when the line does not say. A keyboard notch.</summary>
    private const int DefaultZoomSteps = 4;

    /// <summary>Read a script, or say why there is not one.</summary>
    /// <param name="text">The script's whole text.</param>
    /// <param name="file">What to call it in a refusal.</param>
    /// <returns>The commands in file order, or the refusals.</returns>
    public static DriveScriptResult Parse(string text, string file)
    {
        List<DriveCommand> commands = [];
        List<string> refusals = [];
        ulong last = 0;
        bool quit = false;
        bool stopped = false;

        string[] lines = text.Replace("\r\n", "\n").Split('\n');

        for (int at = 0; at < lines.Length; at++)
        {
            int line = at + 1;
            string source = lines[at];
            int comment = source.IndexOf('#');
            string[] word = (comment < 0 ? source : source[..comment])
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (word.Length == 0)
            {
                continue;
            }

            if (!ulong.TryParse(word[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong tick))
            {
                refusals.Add($"{file}:{line}: '{word[0]}' is not a Tick. A line is "
                    + "'<tick> <verb> [argument]'.");

                continue;
            }

            if (commands.Count > 0 && tick < last)
            {
                refusals.Add($"{file}:{line}: Tick {tick:N0} follows Tick {last:N0}. A script runs "
                    + "forwards, and the shell cannot step backwards to reach it.");

                continue;
            }

            if (quit)
            {
                refusals.Add($"{file}:{line}: nothing runs after 'quit'.");

                continue;
            }

            DriveCommand? command = Command(word, tick, file, line, refusals);

            if (command is null)
            {
                continue;
            }

            // 🔴 A STOPPED CLOCK NEVER REACHES A LATER TICK, AND THE RUN JUST HANGS. Found by
            // running one: a script that paused to look at a flood and asked for a readout three
            // hundred Ticks later sat there until a timeout killed it, having written every file
            // it was going to write. ***The deadlock is visible in the script and therefore
            // belongs here***, where it costs a line number rather than two minutes.
            if (stopped && tick > last)
            {
                refusals.Add($"{file}:{line}: the clock is stopped at Tick {last:N0}, so Tick "
                    + $"{tick:N0} is never reached. Resume before it.");

                continue;
            }

            stopped = Stops(stopped, command.Value);
            last = tick;
            quit |= command.Value.Verb == DriveVerb.Quit;

            commands.Add(command.Value);
        }

        return refusals.Count > 0
            ? DriveScriptResult.Refused(refusals)
            : DriveScriptResult.Read(commands);
    }

    /// <summary>
    /// Read <b>one wire line</b> — a verb and its argument, with no Tick, landing at <c>at</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>It prepends the Tick and calls <see cref="Parse"/> rather than parsing anything itself.</b>
    /// A live command arrives with no Tick because its Tick is <em>now</em>, and that is the only
    /// difference between the two channels — so there is one grammar, and a verb cannot come to mean
    /// two things depending on how it was sent.
    /// </remarks>
    /// <param name="text">The line, as it came off the socket.</param>
    /// <param name="at">The Tick it landed on.</param>
    /// <returns>One command, or the reason there is none.</returns>
    public static DriveScriptResult Line(string text, ulong at)
    {
        int comment = text.IndexOf('#', StringComparison.Ordinal);
        string bare = (comment < 0 ? text : text[..comment]).Trim();

        // 🔴 AN EMPTY LINE IS A POLL AND NOT A MISTAKE, and it had to be said here rather than left
        // to Parse: prepending a Tick to nothing makes a lone Tick, which the grammar refuses by
        // name. ***The doc comment claimed the poll before the code had it*** -- adr/0093 inside one
        // method -- and it was found by sending a blank line at a running shell.
        return bare.Length == 0
            ? DriveScriptResult.Read([])
            : Parse($"{at.ToString(CultureInfo.InvariantCulture)} {bare}", "<socket>");
    }

    /// <summary>
    /// Write a command back out as the script line that would produce it.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THIS IS WHAT MAKES A LIVE SESSION REPRODUCIBLE.</b> A socket is a wall-clock channel
    /// into a simulation whose whole discipline is determinism, and the objection writes itself. The
    /// answer is that every arriving command is stamped with the Tick it landed on and spelled back
    /// out here — ***so the log of an interactive session IS a drive script***, and what somebody
    /// did by hand replays as a batch run. The keyboard is recorded the same way, through the same
    /// applier.
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <returns>A line <see cref="Parse"/> reads back as the same command.</returns>
    public static string Spell(DriveCommand command)
    {
        string at = command.At.ToString(CultureInfo.InvariantCulture);
        string amount = Math.Abs(command.Amount).ToString(CultureInfo.InvariantCulture);

        return command.Verb switch
        {
            DriveVerb.Pause => $"{at} pause",
            DriveVerb.Resume => $"{at} resume",
            DriveVerb.Speed =>
                $"{at} speed {command.Amount.ToString(CultureInfo.InvariantCulture)}",
            DriveVerb.Roads => $"{at} roads {(command.Amount != 0 ? "on" : "off")}",
            DriveVerb.Cells => $"{at} cells {(command.Amount != 0 ? "on" : "off")}",
            DriveVerb.Turn => $"{at} turn {(command.Amount < 0 ? "left" : "right")}",

            // ⚠ The notches are always written, never left to the default. A default that moved
            // would silently re-aim every recorded session that had relied on it.
            DriveVerb.Zoom => $"{at} zoom {(command.Amount < 0 ? "out" : "in")} {amount}",
            DriveVerb.Shoot => $"{at} shoot {command.Path}",
            DriveVerb.Readout => $"{at} readout {command.Path}",
            DriveVerb.Draw => $"{at} draw {command.Path}",
            // ⚠ The choice is always written, on the zoom notch's reasoning: a default that moved
            // would silently re-aim every recorded session that had relied on it.
            DriveVerb.Hold =>
                $"{at} hold {command.Path} {command.Amount.ToString(CultureInfo.InvariantCulture)}",
            DriveVerb.Click =>
                $"{at} click {command.East.ToString(CultureInfo.InvariantCulture)} "
                + $"{command.North.ToString(CultureInfo.InvariantCulture)}"
                + (command.Amount != 0 ? " shift" : string.Empty),
            _ => $"{at} quit",
        };
    }

    /// <summary>Whether the clock is stopped once every one of these has run.</summary>
    /// <remarks>
    /// <b>Exposed because <c>--quit-at</c> is appended after the parse</b> and a run that pauses at
    /// the end and is told to quit later hangs exactly as an in-script one would.
    /// </remarks>
    /// <param name="commands">The commands, in the order they will run.</param>
    /// <returns>True when nothing will advance the world afterwards.</returns>
    public static bool Stopped(IReadOnlyList<DriveCommand> commands)
    {
        bool stopped = false;

        foreach (DriveCommand command in commands)
        {
            stopped = Stops(stopped, command);
        }

        return stopped;
    }

    /// <summary>Whether the clock is stopped after one more command. <c>speed 0</c> is a pause.</summary>
    private static bool Stops(bool stopped, DriveCommand command) => command.Verb switch
    {
        DriveVerb.Pause => true,
        DriveVerb.Resume => false,
        DriveVerb.Speed => command.Amount == 0,
        _ => stopped,
    };

    /// <summary>Read one line's verb and its argument, having already read its Tick.</summary>
    private static DriveCommand? Command(
        string[] word,
        ulong tick,
        string file,
        int line,
        List<string> refusals)
    {
        if (word.Length < 2)
        {
            refusals.Add($"{file}:{line}: Tick {tick:N0} with no verb after it.");

            return null;
        }

        string verb = word[1].ToLowerInvariant();
        string? argument = word.Length > 2 ? word[2] : null;

        switch (verb)
        {
            case "pause":
            case "resume":
            case "quit":
                return Arity(0)
                    ? Made(verb switch
                    {
                        "pause" => DriveVerb.Pause,
                        "resume" => DriveVerb.Resume,
                        _ => DriveVerb.Quit,
                    })
                    : null;

            case "speed":
                if (!Arity(1))
                {
                    return null;
                }

                if (!int.TryParse(argument, NumberStyles.None, CultureInfo.InvariantCulture, out int rung))
                {
                    refusals.Add($"{file}:{line}: 'speed' takes a rung, not '{argument}'.");

                    return null;
                }

                return Made(DriveVerb.Speed, rung);

            case "roads":
            case "cells":
                if (!Arity(1))
                {
                    return null;
                }

                if (argument is not ("on" or "off"))
                {
                    refusals.Add($"{file}:{line}: '{verb}' takes 'on' or 'off', not '{argument}'.");

                    return null;
                }

                return Made(
                    verb == "roads" ? DriveVerb.Roads : DriveVerb.Cells, argument == "on" ? 1 : 0);

            case "turn":
                if (!Arity(1))
                {
                    return null;
                }

                if (argument is not ("left" or "right"))
                {
                    refusals.Add($"{file}:{line}: 'turn' takes 'left' or 'right', not '{argument}'.");

                    return null;
                }

                return Made(DriveVerb.Turn, argument == "left" ? -1 : 1);

            case "zoom":
                if (word.Length is < 3 or > 4)
                {
                    refusals.Add($"{file}:{line}: 'zoom' takes 'in' or 'out', and optionally notches.");

                    return null;
                }

                if (argument is not ("in" or "out"))
                {
                    refusals.Add($"{file}:{line}: 'zoom' takes 'in' or 'out', not '{argument}'.");

                    return null;
                }

                int steps = DefaultZoomSteps;

                if (word.Length == 4
                    && !int.TryParse(word[3], NumberStyles.None, CultureInfo.InvariantCulture, out steps))
                {
                    refusals.Add($"{file}:{line}: 'zoom {argument}' takes notches, not '{word[3]}'.");

                    return null;
                }

                return Made(DriveVerb.Zoom, argument == "in" ? steps : -steps);

            case "hold":
                if (word.Length is < 3 or > 4)
                {
                    refusals.Add($"{file}:{line}: 'hold' takes a tool, and optionally which one of "
                        + "it — 'hold zone 1', 'hold service 2'.");

                    return null;
                }

                int choice = 0;

                if (word.Length == 4
                    && !int.TryParse(word[3], NumberStyles.None, CultureInfo.InvariantCulture, out choice))
                {
                    refusals.Add($"{file}:{line}: 'hold {argument}' takes a number, not '{word[3]}'.");

                    return null;
                }

                return new DriveCommand(tick, DriveVerb.Hold, choice, argument);

            case "click":
                if (word.Length is < 4 or > 5)
                {
                    refusals.Add($"{file}:{line}: 'click' takes an east Tile and a north Tile, and "
                        + "optionally 'shift'.");

                    return null;
                }

                if (!int.TryParse(word[2], NumberStyles.None, CultureInfo.InvariantCulture, out int east)
                    || !int.TryParse(word[3], NumberStyles.None, CultureInfo.InvariantCulture, out int north))
                {
                    refusals.Add($"{file}:{line}: 'click' takes two Tile coordinates, not "
                        + $"'{word[2]} {word[3]}'.");

                    return null;
                }

                if (word.Length == 5 && word[4] != "shift")
                {
                    refusals.Add($"{file}:{line}: 'click' takes 'shift' or nothing, not '{word[4]}'.");

                    return null;
                }

                return new DriveCommand(
                    tick, DriveVerb.Click, word.Length == 5 ? 1 : 0, null, east, north);

            case "shoot":
            case "readout":
            case "draw":
                if (!Arity(1))
                {
                    return null;
                }

                return new DriveCommand(
                    tick,
                    verb switch
                    {
                        "shoot" => DriveVerb.Shoot,
                        "readout" => DriveVerb.Readout,
                        _ => DriveVerb.Draw,
                    },
                    0,
                    argument);

            default:
                refusals.Add($"{file}:{line}: no verb '{verb}'. There is pause, resume, speed, "
                    + "roads, cells, turn, zoom, hold, click, shoot, readout, draw and quit.");

                return null;
        }

        bool Arity(int wanted)
        {
            if (word.Length - 2 == wanted)
            {
                return true;
            }

            refusals.Add($"{file}:{line}: '{verb}' takes {wanted} argument"
                + $"{(wanted == 1 ? string.Empty : "s")}, and was given {word.Length - 2}.");

            return false;
        }

        DriveCommand Made(DriveVerb made, int amount = 0) =>
            new(tick, made, amount, null);
    }
}

/// <summary>What a parse produced: the commands, or the reasons there are none. Never both.</summary>
/// <remarks>
/// <b>The same shape as <see cref="RulesetLoadResult"/> and for the same reason</b> — a run that
/// began on half a script would be a run whose output nobody could attribute. Every refusal is
/// collected rather than the first, so a script is fixed in one pass.
/// </remarks>
public sealed class DriveScriptResult
{
    private DriveScriptResult(IReadOnlyList<DriveCommand>? commands, IReadOnlyList<string> refusals)
    {
        Commands = commands;
        Refusals = refusals;
    }

    /// <summary>The commands in file order, or null when the script was refused.</summary>
    public IReadOnlyList<DriveCommand>? Commands { get; }

    /// <summary>Why there are none. Empty when there are.</summary>
    public IReadOnlyList<string> Refusals { get; }

    /// <summary>A script that parsed.</summary>
    public static DriveScriptResult Read(IReadOnlyList<DriveCommand> commands) =>
        new(commands, []);

    /// <summary>A script that did not.</summary>
    public static DriveScriptResult Refused(IReadOnlyList<string> refusals) =>
        new(null, refusals);

    /// <summary>Every refusal, one per line, for printing at a person.</summary>
    public string Describe() => string.Join('\n', Refusals);
}
