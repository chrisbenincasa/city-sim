using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The drive script's grammar.
/// </summary>
/// <remarks>
/// <b>This class is the reason the parser is in <c>Borough.Formats</c> at all.</b>
/// <c>plans/0048</c> task 2a: <c>Borough.Godot</c> is not in <c>Borough.slnx</c>, so a script parsed
/// in the shell would be parsed by code no test in this suite can reach — and a driven run exists to
/// produce evidence, which makes a quietly misparsed script the one defect that would corrupt every
/// finding taken through it.
/// </remarks>
public sealed class DriveScriptTests
{
    [Fact]
    public void A_script_reads_every_verb_at_the_Tick_it_names()
    {
        DriveScriptResult read = DriveScript.Parse(
            """
            # the flood, watched from the bank
            4096 pause
            4096 roads off
            4096 turn left
            4096 resume
            4200 speed 3
            4200 zoom in
            4300 zoom out 9
            6101 shoot frames/flood.png
            6101 readout frames/flood.txt
            6101 draw frames/flood.tsv
            6400 resume
            8192 quit
            """,
            "flood.drive");

        Assert.Empty(read.Refusals);
        Assert.NotNull(read.Commands);

        Assert.Equal(
            [
                new DriveCommand(4096, DriveVerb.Pause, 0, null),
                new DriveCommand(4096, DriveVerb.Roads, 0, null),
                new DriveCommand(4096, DriveVerb.Turn, -1, null),
                new DriveCommand(4096, DriveVerb.Resume, 0, null),
                new DriveCommand(4200, DriveVerb.Speed, 3, null),
                new DriveCommand(4200, DriveVerb.Zoom, 4, null),
                new DriveCommand(4300, DriveVerb.Zoom, -9, null),
                new DriveCommand(6101, DriveVerb.Shoot, 0, "frames/flood.png"),
                new DriveCommand(6101, DriveVerb.Readout, 0, "frames/flood.txt"),
                new DriveCommand(6101, DriveVerb.Draw, 0, "frames/flood.tsv"),
                new DriveCommand(6400, DriveVerb.Resume, 0, null),
                new DriveCommand(8192, DriveVerb.Quit, 0, null),
            ],
            read.Commands);
    }

    [Fact]
    public void A_tilt_is_an_angle_in_degrees_and_the_grammar_does_not_clamp_it()
    {
        // 🔴 THE BOUNDS ARE THE SHELL'S AND NOT THE GRAMMAR'S, and that split is the point of this
        // test. Borough.Formats knows nothing about the ground mesh being edge-on at zero or about
        // LookAt's up-vector going degenerate at ninety -- both are facts of Borough.Godot -- so a
        // number outside the band parses here and is clamped there. ***A grammar that refused 200
        // would be a second copy of a constant no test in this assembly can reach.***
        DriveScriptResult read = DriveScript.Parse(
            """
            0 tilt 35
            1 tilt 4
            2 tilt 200
            """,
            "tilt.drive");

        Assert.Empty(read.Refusals);
        Assert.Equal(
            [
                new DriveCommand(0, DriveVerb.Tilt, 35, null),
                new DriveCommand(1, DriveVerb.Tilt, 4, null),
                new DriveCommand(2, DriveVerb.Tilt, 200, null),
            ],
            read.Commands);
    }

    [Theory]
    [InlineData("0 tilt", "takes one angle")]
    [InlineData("0 tilt 20 30", "takes one angle")]
    [InlineData("0 tilt low", "takes one angle")]
    [InlineData("0 tilt -5", "takes one angle")]
    public void A_tilt_that_is_not_one_number_is_refused_by_name(string script, string reason)
    {
        // ⚠ `-5` IS REFUSED BY THE PARSE AND NOT BY A RANGE CHECK: NumberStyles.None admits no
        // sign, which is the same reason every other count in this grammar carries its direction as
        // a word. A negative pitch is the camera underground, so nothing is lost.
        DriveScriptResult read = DriveScript.Parse(script, "x.drive");

        Assert.Null(read.Commands);
        Assert.Contains(reason, Assert.Single(read.Refusals), StringComparison.Ordinal);
    }

    [Fact]
    public void An_on_or_off_is_an_absolute_and_never_a_toggle()
    {
        // The point of the format: 'roads off' twice is 'roads off', where g twice is nothing at
        // all. A script must not depend on what ran before it.
        DriveScriptResult read = DriveScript.Parse("1 roads on\n2 roads off\n3 cells on", "x");

        Assert.Empty(read.Refusals);
        Assert.Equal(1, read.Commands![0].Amount);
        Assert.Equal(0, read.Commands[1].Amount);
        Assert.Equal(DriveVerb.Cells, read.Commands[2].Verb);
        Assert.Equal(1, read.Commands[2].Amount);
    }

    [Fact]
    public void A_comment_a_blank_line_and_a_trailing_comment_are_not_commands()
    {
        DriveScriptResult read = DriveScript.Parse(
            "# a whole line\n\n   \n100 pause   # why we pause here\n", "x");

        Assert.Empty(read.Refusals);
        Assert.Equal(new DriveCommand(100, DriveVerb.Pause, 0, null), Assert.Single(read.Commands!));
    }

    [Fact]
    public void A_Tick_that_goes_backwards_is_refused_and_the_line_is_named()
    {
        // The shell steps forwards only, so a script reaching back is unrunnable rather than merely
        // untidy -- and sorting it silently would run something the author did not write.
        DriveScriptResult read = DriveScript.Parse("500 pause\n400 resume", "back.drive");

        Assert.Null(read.Commands);
        Assert.Contains("back.drive:2", Assert.Single(read.Refusals));
        Assert.Contains("runs forwards", read.Describe());
    }

    [Fact]
    public void Nothing_runs_after_quit()
    {
        DriveScriptResult read = DriveScript.Parse("10 quit\n20 pause", "x");

        Assert.Null(read.Commands);
        Assert.Contains("nothing runs after 'quit'", Assert.Single(read.Refusals));
    }

    [Theory]
    [InlineData("10 dance", "no verb 'dance'")]
    [InlineData("10 focus", "an east Tile and a north Tile")]
    [InlineData("10 focus 4096", "an east Tile and a north Tile")]
    [InlineData("10 focus here 8192", "two Tile coordinates")]
    [InlineData("10 focus 4096 8192 0", "a distance in metres above zero")]
    [InlineData("10", "with no verb after it")]
    [InlineData("tomorrow pause", "is not a Tick")]
    [InlineData("10 pause now", "takes 0 arguments")]
    [InlineData("10 speed", "takes 1 argument")]
    [InlineData("10 speed fast", "takes a rung")]
    [InlineData("10 roads maybe", "takes 'on' or 'off'")]
    [InlineData("10 turn around", "takes 'left' or 'right'")]
    [InlineData("10 release 4096", "takes an east Tile and a north Tile")]
    [InlineData("10 release 4096 8192 shift", "takes an east Tile and a north Tile")]
    [InlineData("10 zoom sideways", "takes 'in' or 'out'")]
    [InlineData("10 zoom in lots", "takes notches")]
    [InlineData("10 shoot", "takes 1 argument")]
    [InlineData("10 draw", "takes 1 argument")]
    [InlineData("10 hold", "takes a tool")]
    [InlineData("10 hold zone one", "takes a number")]
    [InlineData("10 click", "takes an east Tile")]
    [InlineData("10 click 8", "takes an east Tile")]
    [InlineData("10 click here there", "takes two Tile coordinates")]
    [InlineData("10 click 8 9 hard", "takes 'shift' or nothing")]
    public void A_line_that_is_not_a_command_is_refused_by_name(string script, string reason)
    {
        DriveScriptResult read = DriveScript.Parse(script, "x");

        Assert.Null(read.Commands);
        Assert.Contains(reason, Assert.Single(read.Refusals));
    }

    [Fact]
    public void Every_refusal_is_collected_rather_than_the_first()
    {
        // adr/0015's standard, arriving one format along: a script fixed one refusal per run is a
        // script whose author stops writing scripts.
        DriveScriptResult read = DriveScript.Parse("10 dance\n20 speed fast\n30 roads maybe", "x");

        Assert.Null(read.Commands);
        Assert.Equal(3, read.Refusals.Count);
        Assert.Equal(2, read.Describe().Split('\n', StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void A_wire_line_is_the_same_grammar_with_the_Tick_supplied()
    {
        // The only difference between the two channels: a live command's Tick is 'now'.
        DriveScriptResult read = DriveScript.Line("roads off", 6101);

        Assert.Empty(read.Refusals);
        Assert.Equal(
            new DriveCommand(6101, DriveVerb.Roads, 0, null), Assert.Single(read.Commands!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# just looking")]
    public void An_empty_wire_line_is_a_poll_rather_than_a_mistake(string line)
    {
        // A driver watching without touching anything sends nothing and reads the state back. It
        // cannot go through Parse unhelped: a Tick with nothing after it is refused by name.
        DriveScriptResult read = DriveScript.Line(line, 6101);

        Assert.Empty(read.Refusals);
        Assert.Empty(read.Commands!);
    }

    [Fact]
    public void A_wire_line_is_refused_by_the_same_rules()
    {
        DriveScriptResult read = DriveScript.Line("dance", 10);

        Assert.Null(read.Commands);
        Assert.Contains("no verb 'dance'", Assert.Single(read.Refusals));
    }

    [Theory]
    [InlineData("100 pause")]
    [InlineData("100 resume")]
    [InlineData("100 quit")]
    [InlineData("100 speed 7")]
    [InlineData("100 speed 0")]
    [InlineData("100 roads on")]
    [InlineData("100 roads off")]
    [InlineData("100 cells on")]
    [InlineData("100 turn left")]
    [InlineData("100 turn right")]
    [InlineData("100 zoom in 4")]
    [InlineData("100 zoom out 9")]
    [InlineData("100 shoot a/b.png")]
    [InlineData("100 readout a/b.txt")]
    [InlineData("100 draw a/b.tsv")]
    [InlineData("100 hold demolish 0")]
    [InlineData("100 hold zone 2")]
    [InlineData("100 people")]
    [InlineData("100 click 4096 8192")]
    [InlineData("100 click 4096 8192 shift")]
    [InlineData("100 release 4096 8192")]
    [InlineData("100 focus 4096 8192")]
    [InlineData("100 focus 4096 8192 30000")]
    [InlineData("100 tilt 35")]
    [InlineData("100 tilt 5")]
    public void A_command_spells_back_out_as_the_line_that_makes_it(string line)
    {
        // 🔴 THIS ROUND TRIP IS WHAT MAKES A LIVE SESSION REPRODUCIBLE. A socket stamps each
        // arriving command with its Tick and spells it here, so the log of an interactive session
        // IS a drive script -- and what somebody did by hand replays as a batch run.
        DriveCommand read = Assert.Single(DriveScript.Parse(line, "x").Commands!);

        Assert.Equal(line, DriveScript.Spell(read));
        Assert.Equal(
            read, Assert.Single(DriveScript.Parse(DriveScript.Spell(read), "x").Commands!));
    }

    [Fact]
    public void An_empty_script_is_read_rather_than_refused()
    {
        // A run driven by nothing is a run that just plays, which is what --quit-at alone asks for.
        DriveScriptResult read = DriveScript.Parse("# nothing here\n", "x");

        Assert.Empty(read.Refusals);
        Assert.Empty(read.Commands!);
    }

    [Fact]
    public void A_stopped_clock_never_reaches_a_later_Tick_and_the_script_is_refused()
    {
        // Found by running one: the shell wrote every file it was going to write and then sat
        // there until a timeout killed it. A hang is the worst refusal, because it looks like work.
        DriveScriptResult read = DriveScript.Parse(
            "6101 pause\n6400 readout out.txt", "stuck.drive");

        Assert.Null(read.Commands);
        Assert.Contains("stuck.drive:2", Assert.Single(read.Refusals));
        Assert.Contains("the clock is stopped at Tick 6,101", read.Describe());
    }

    [Fact]
    public void A_pause_is_free_to_do_anything_on_the_Tick_it_stopped()
    {
        // The clock is stopped, but the Tick has already been reached, so nothing waits on it.
        DriveScriptResult read = DriveScript.Parse(
            "6101 pause\n6101 roads off\n6101 shoot a.png\n6101 quit", "x");

        Assert.Empty(read.Refusals);
        Assert.Equal(4, read.Commands!.Count);
    }

    [Fact]
    public void A_resume_lets_the_script_go_on()
    {
        DriveScriptResult read = DriveScript.Parse(
            "100 pause\n100 shoot a.png\n100 resume\n200 quit", "x");

        Assert.Empty(read.Refusals);
        Assert.Equal(4, read.Commands!.Count);
    }

    [Fact]
    public void Speed_zero_stops_the_clock_and_a_rung_starts_it()
    {
        // speed 0 is the same rung the space bar reaches, so it must deadlock the same way.
        Assert.True(DriveScript.Stopped(DriveScript.Parse("1 speed 0", "x").Commands!));
        Assert.False(DriveScript.Stopped(DriveScript.Parse("1 pause\n1 speed 4", "x").Commands!));
        Assert.False(DriveScript.Stopped(DriveScript.Parse("1 pause\n1 resume", "x").Commands!));
        Assert.True(DriveScript.Stopped(DriveScript.Parse("1 speed 4\n2 pause", "x").Commands!));
        Assert.False(DriveScript.Stopped(DriveScript.Parse("1 roads off", "x").Commands!));
    }

    [Fact]
    public void Two_commands_may_share_a_Tick_and_they_keep_file_order()
    {
        // The shell applies them in one pass, so 'turn' then 'shoot' photographs the turned city.
        DriveScriptResult read = DriveScript.Parse("77 turn right\n77 shoot a.png", "x");

        Assert.Empty(read.Refusals);
        Assert.Equal(DriveVerb.Turn, read.Commands![0].Verb);
        Assert.Equal(DriveVerb.Shoot, read.Commands[1].Verb);
    }
}
