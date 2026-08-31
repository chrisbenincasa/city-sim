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
                new DriveCommand(6400, DriveVerb.Resume, 0, null),
                new DriveCommand(8192, DriveVerb.Quit, 0, null),
            ],
            read.Commands);
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
    [InlineData("10", "with no verb after it")]
    [InlineData("tomorrow pause", "is not a Tick")]
    [InlineData("10 pause now", "takes 0 arguments")]
    [InlineData("10 speed", "takes 1 argument")]
    [InlineData("10 speed fast", "takes a rung")]
    [InlineData("10 roads maybe", "takes 'on' or 'off'")]
    [InlineData("10 turn around", "takes 'left' or 'right'")]
    [InlineData("10 zoom sideways", "takes 'in' or 'out'")]
    [InlineData("10 zoom in lots", "takes notches")]
    [InlineData("10 shoot", "takes 1 argument")]
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
