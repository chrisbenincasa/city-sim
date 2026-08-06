using Borough.Core.Determinism;
using Borough.Headless;

namespace Borough.Tests.Headless;

/// <summary>
/// The runner's command line and its Ruleset policy.
/// </summary>
/// <remarks>
/// <b><c>Borough.Headless</c> is the primary interface for the whole of Phase 1 and most of Phase
/// 2</b>, and it is the project most likely to be dismissed as a nicety and the one that decides
/// whether this simulation ever gets balanced. These two units are where a mistake in it is silent:
/// a misparsed flag runs a different session than the one asked for, and a lenient check produces
/// numbers that look comparable and are not.
/// </remarks>
public sealed class RunnerTests
{
    private const ulong Recorded = 0xABCD_EF01_2345_6789UL;

    /// <summary>
    /// The acceptance criterion, as a test: a run whose Ruleset does not match refuses to start.
    /// </summary>
    /// <remarks>
    /// <c>05 §7</c>: a different Ruleset is a different simulation and the State Hash will diverge.
    /// That is arithmetic rather than a bug, and a runner that reported it as a divergence would be
    /// blaming the simulation for something the command line did.
    /// </remarks>
    [Fact]
    public void A_session_whose_ruleset_does_not_match_refuses_to_run()
    {
        RulesetCheck check = RulesetCheck.Against(
            Recorded, supplied: 0x1111_1111_1111_1111UL, path: "vanilla.toml", force: false);

        Assert.False(check.Allowed);
        Assert.Contains("vanilla.toml", check.Refusal, StringComparison.Ordinal);
        Assert.Contains("--force-ruleset", check.Refusal, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A session naming a Ruleset nobody supplied is refused too.</b> It is not a mismatch — it
    /// is a match nothing can confirm, which from the far side is the same thing: the run either was
    /// or was not against the right Rules and the runner cannot say which. This is the shape
    /// <c>05 §7</c>'s <em>unaccounted</em> takes first.
    /// </summary>
    [Fact]
    public void A_session_whose_ruleset_cannot_be_checked_refuses_to_run()
    {
        RulesetCheck check = RulesetCheck.Against(
            Recorded, ContentHash.None, path: null, force: false);

        Assert.False(check.Allowed);
        Assert.Contains("no --ruleset was given", check.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matching_ruleset_runs_unmarked()
    {
        RulesetCheck check = RulesetCheck.Against(Recorded, Recorded, "vanilla.toml", force: false);

        Assert.True(check.Allowed);
        Assert.False(check.HashBroken);
        Assert.Null(check.Refusal);
    }

    /// <summary>
    /// Nothing before slice 8 names a Ruleset, so every log in the repository runs unmarked with no
    /// <c>--ruleset</c> at all. The policy is built now and starts firing then.
    /// </summary>
    [Fact]
    public void A_session_naming_no_ruleset_runs_unmarked()
    {
        RulesetCheck check = RulesetCheck.Against(
            ContentHash.None, ContentHash.None, path: null, force: false);

        Assert.True(check.Allowed);
        Assert.False(check.HashBroken);
    }

    /// <summary>
    /// Forcing runs, and marks. <c>05 §7</c> marks a save loaded across an unaccounted mismatch
    /// permanently hash-broken; a trace earns the same for the same reason, or a divergence report
    /// eventually arrives for numbers that were never comparable.
    /// </summary>
    [Fact]
    public void Forcing_runs_but_marks_the_trace_hash_broken()
    {
        RulesetCheck check = RulesetCheck.Against(Recorded, ContentHash.None, null, force: true);

        Assert.True(check.Allowed);
        Assert.True(check.HashBroken);
    }

    [Fact]
    public void No_arguments_is_the_table_report()
    {
        Assert.True(Options.TryParse([], out Options options, out _));
        Assert.Equal(Mode.Report, options.Mode);
        Assert.Equal(10_000, options.Citizens);
    }

    [Theory]
    [InlineData("--log")]
    [InlineData("--seed")]
    [InlineData("--ticks")]
    public void Any_session_flag_selects_a_run(string flag)
    {
        Assert.True(Options.TryParse([flag, "4"], out Options options, out _));
        Assert.Equal(Mode.Run, options.Mode);
    }

    [Fact]
    public void The_flags_parse_to_what_they_say()
    {
        Assert.True(Options.TryParse(
            ["--log", "s.borough", "--ticks", "500", "--hash-every", "25",
             "--ruleset", "v.toml", "--out", "t.txt", "--force-ruleset", "--census"],
            out Options options,
            out _));

        Assert.Equal("s.borough", options.LogPath);
        Assert.Equal("v.toml", options.RulesetPath);
        Assert.Equal("t.txt", options.OutPath);
        Assert.Equal(500UL, options.Ticks);
        Assert.Equal(25, options.HashEvery);
        Assert.True(options.ForceRuleset);
        Assert.True(options.Census);
    }

    /// <summary>
    /// A census is a property of a run, so asking for one asks for a run.
    /// </summary>
    /// <remarks>
    /// The table report is a constructed world at one moment and has no history to take a series
    /// over; <c>--census</c> alone selecting it would have produced an empty report and no complaint.
    /// </remarks>
    [Fact]
    public void Asking_for_a_census_selects_a_run()
    {
        Assert.True(Options.TryParse(["--census"], out Options options, out _));
        Assert.Equal(Mode.Run, options.Mode);
        Assert.True(options.Census);
    }

    /// <summary>
    /// <b>A log carries its own configuration</b>, so a replay that took its world size from the
    /// command line would be reproducing a different session while claiming to reproduce this one.
    /// Refusing the combination is cheaper than explaining the divergence it would cause.
    /// </summary>
    [Fact]
    public void A_log_and_an_explicit_citizen_count_disagree()
    {
        Assert.False(Options.TryParse(
            ["--log", "s.borough", "--citizens", "64"], out _, out string? complaint));

        Assert.Contains("carries its own configuration", complaint, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("--ticks", "0")]
    [InlineData("--ticks", "-1")]
    [InlineData("--hash-every", "0")]
    [InlineData("--citizens", "0")]
    [InlineData("--seed", "banana")]
    public void A_value_that_is_not_a_count_is_refused(string flag, string value)
    {
        Assert.False(Options.TryParse([flag, value], out _, out string? complaint));
        Assert.False(string.IsNullOrEmpty(complaint));
    }

    [Fact]
    public void An_unknown_flag_is_refused_rather_than_ignored()
    {
        Assert.False(Options.TryParse(["--verbose", "1"], out _, out string? complaint));
        Assert.Contains("--verbose", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// A flag with no value is refused rather than silently consuming the next flag as its value,
    /// which is the parsing bug that runs a plausible-looking wrong session.
    /// </summary>
    [Fact]
    public void A_flag_with_no_value_is_refused()
    {
        Assert.False(Options.TryParse(["--ticks"], out _, out string? complaint));
        Assert.Contains("needs a value", complaint, StringComparison.Ordinal);
    }
}
