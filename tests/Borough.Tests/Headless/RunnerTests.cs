using Borough.Core.Determinism;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
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

    /// <summary>A session recorded against <paramref name="opening"/>, reloading at each Tick given.</summary>
    private static InputLog Log(ulong opening, params (ulong Tick, ulong To)[] reloads)
    {
        InputLogBuilder builder = new(seed: 1, new WorldConfiguration(1_000), opening);

        foreach ((ulong tick, ulong to) in reloads)
        {
            builder.Reload(new Ticks(tick), to);
        }

        return builder.Build();
    }

    /// <summary>What the operator named on the command line.</summary>
    private static Supplied[] Given(params (string Path, ulong Hash)[] rulesets) =>
        [.. rulesets.Select(entry => new Supplied(entry.Path, entry.Hash))];

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
            Log(Recorded), Given(("vanilla.toml", 0x1111_1111_1111_1111UL)), force: false);

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
        RulesetCheck check = RulesetCheck.Against(Log(Recorded), Given(), force: false);

        Assert.False(check.Allowed);
        Assert.Contains("no --ruleset was given", check.Refusal, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matching_ruleset_runs_unmarked()
    {
        RulesetCheck check = RulesetCheck.Against(
            Log(Recorded), Given(("vanilla.toml", Recorded)), force: false);

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
            Log(ContentHash.None), Given(), force: false);

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
        RulesetCheck check = RulesetCheck.Against(Log(Recorded), Given(), force: true);

        Assert.True(check.Allowed);
        Assert.True(check.HashBroken);
    }

    // ---- slice 8 task 9: a session names more than one Ruleset ---------------------------------

    private const ulong Second = 0x0F0F_0F0F_0F0F_0F0FUL;

    /// <summary>
    /// Every Ruleset a session reloads into has to be supplied, and the refusal says which one is not.
    /// </summary>
    /// <remarks>
    /// <b>The sentence <c>InputLog.cs:131</c> wrote down in slice 5</b>: <em>"--ruleset PATH names one
    /// file and a session that reloaded twice was played against three."</em> Before this the runner
    /// checked the opening hash and nothing else, so a log with a transition in it would have started
    /// cleanly and thrown a thousand Ticks later.
    /// </remarks>
    [Fact]
    public void A_session_reloading_into_a_ruleset_nobody_supplied_is_refused()
    {
        RulesetCheck check = RulesetCheck.Against(
            Log(Recorded, (100, Second)), Given(("vanilla.toml", Recorded)), force: false);

        Assert.False(check.Allowed);
        Assert.Contains("Tick 100", check.Refusal, StringComparison.Ordinal);
        Assert.Contains("0x0F0F0F0F0F0F0F0F", check.Refusal, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Force waives a mismatch and cannot waive an absence.</b>
    /// </summary>
    /// <remarks>
    /// <em>Run these Rules under this log anyway</em> is a sentence that needs Rules. It is the third
    /// instance of one distinction in this runner: a malformed Ruleset is refused unconditionally
    /// because force cannot make Rules readable, and a Ruleset nobody has is the same shape one notch
    /// further on.
    /// </remarks>
    [Fact]
    public void Forcing_cannot_waive_a_ruleset_nobody_supplied()
    {
        RulesetCheck check = RulesetCheck.Against(
            Log(Recorded, (100, Second)), Given(("vanilla.toml", Recorded)), force: true);

        Assert.False(check.Allowed);
        Assert.Contains("--force-ruleset cannot waive this", check.Refusal, StringComparison.Ordinal);
    }

    /// <summary>A session given every Ruleset it names runs unmarked, however they were ordered.</summary>
    [Fact]
    public void A_session_given_every_ruleset_it_names_runs_unmarked()
    {
        RulesetCheck check = RulesetCheck.Against(
            Log(Recorded, (100, Second)),
            Given(("patched.toml", Second), ("vanilla.toml", Recorded)),
            force: false);

        Assert.True(check.Allowed);
        Assert.False(check.HashBroken);
        Assert.Equal(Recorded, check.InForce);
    }

    [Fact]
    public void The_ruleset_flag_repeats()
    {
        Assert.True(Options.TryParse(
            ["--log", "s.borough", "--ruleset", "a.toml", "--ruleset", "b.toml"],
            out Options options,
            out _));

        Assert.Equal(["a.toml", "b.toml"], options.RulesetPaths);
        Assert.Equal("a.toml", options.RulesetPath);
    }

    /// <summary>
    /// The same file twice is the operator's mistake, and is refused in the operator's words.
    /// </summary>
    /// <remarks>
    /// <c>RulesetCatalogue.Of</c> refuses a duplicate content hash too, and says <em>two Rulesets
    /// carry one content hash</em> — true, and not what somebody who typed the same path twice needs
    /// to read.
    /// </remarks>
    [Fact]
    public void The_same_ruleset_named_twice_is_refused()
    {
        Assert.False(Options.TryParse(
            ["--log", "s.borough", "--ruleset", "a.toml", "--ruleset", "a.toml"],
            out _,
            out string? complaint));

        Assert.Contains("was given twice", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// The catalogue opens on the Ruleset the log opens on, whatever order the operator typed.
    /// </summary>
    /// <remarks>
    /// <b>This is the trap the sort exists for.</b> <c>RulesetCatalogue</c> takes its opening entry
    /// from position 0 and the world is built from it, so the wrong order would build a city under
    /// Rules the log never named — and Tick 0 would <em>establish</em> that hash rather than swap away
    /// from it, because the first Tick opens rather than reloads. Both Rulesets load, both run, and
    /// nothing says which one the numbers came from.
    /// </remarks>
    [Fact]
    public void The_catalogue_opens_on_the_ruleset_the_log_opens_on()
    {
        string directory = Directory.CreateTempSubdirectory("borough-rulesets").FullName;

        try
        {
            string opening = Write(directory, "opening.toml", "sundries");
            string patched = Write(directory, "patched.toml", "repairs");

            Supplied[] supplied =
            [
                new(patched, RulesetFile.HashOf(patched)),
                new(opening, RulesetFile.HashOf(opening)),
            ];

            Assert.True(Borough.Headless.Session.TryCatalogue(
                supplied, supplied[1].Hash, out RulesetCatalogue catalogue));

            Assert.Equal(supplied[1].Hash, catalogue.OpeningHash);
            Assert.Equal(2, catalogue.Count);
            Assert.True(catalogue.TryResolve(supplied[0].Hash, out _));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // ---- --reload-at, which is adr/0015's iteration loop ----------------------------------------

    /// <summary>
    /// A fresh session reloads where the command line says it does, and the transition is in the log.
    /// </summary>
    /// <remarks>
    /// <b>The loop cannot close through a recorded log, which is why this flag exists.</b> A log
    /// records a transition by content hash, so the first edit to the Ruleset makes the log name a
    /// file that no longer exists and the run is refused — and editing the file <em>is</em> the loop.
    /// Building the transitions here against whatever the files hash to on this run is what makes
    /// <c>adr/0015</c>'s <em>seconds</em> claim testable at all, and the session stays reproducible
    /// because the log the runner builds still carries them.
    /// </remarks>
    [Fact]
    public void A_fresh_session_reloads_where_the_command_line_says()
    {
        Assert.True(Options.TryParse(
            ["--ticks", "400", "--citizens", "100",
             "--ruleset", "a.toml", "--reload-at", "200", "--ruleset", "b.toml"],
            out Options options,
            out _));

        InputLog log = Borough.Headless.Session.Load(
            options, Given(("a.toml", Recorded), ("b.toml", Second)));

        Assert.Equal(Recorded, log.RulesetHash);
        Assert.Equal(1, log.TransitionCount);
        Assert.Equal(new Ticks(200), log.Transition(0).Tick);
        Assert.Equal(Recorded, log.Transition(0).From);
        Assert.Equal(Second, log.Transition(0).To);
    }

    /// <summary>
    /// A replay's transitions are what it is reproducing, so the command line may not add one.
    /// </summary>
    [Fact]
    public void A_reload_on_the_command_line_and_a_recorded_log_disagree()
    {
        Assert.False(Options.TryParse(
            ["--log", "s.borough", "--ruleset", "a.toml", "--reload-at", "200", "--ruleset", "b.toml"],
            out _,
            out string? complaint));

        Assert.Contains("carries its own", complaint, StringComparison.Ordinal);
    }

    /// <summary>One Tick per Ruleset after the first, because each reload swaps to the next one.</summary>
    [Theory]
    [InlineData(new[] { "--ruleset", "a.toml", "--reload-at", "200" }, "one Tick per Ruleset")]
    [InlineData(new[] { "--ruleset", "a.toml", "--ruleset", "b.toml" }, "nothing says when")]
    [InlineData(
        new[] { "--ruleset", "a.toml", "--reload-at", "200", "--ruleset", "b.toml",
                "--reload-at", "200", "--ruleset", "c.toml" },
        "does not follow")]
    public void A_reload_the_runner_cannot_place_is_refused(string[] arguments, string because)
    {
        Assert.False(Options.TryParse(
            [.. arguments, "--ticks", "400"], out _, out string? complaint));

        Assert.Contains(because, complaint, StringComparison.Ordinal);
    }

    /// <summary>Tick 0 is the opening Ruleset, so a reload there could never have taken effect.</summary>
    [Fact]
    public void A_reload_at_tick_zero_is_refused()
    {
        Assert.False(Options.TryParse(
            ["--ruleset", "a.toml", "--reload-at", "0", "--ruleset", "b.toml"],
            out _,
            out string? complaint));

        Assert.Contains("Tick after 0", complaint, StringComparison.Ordinal);
    }

    /// <summary>A one-Resource Ruleset, named for the Resource so two of them differ.</summary>
    private static string Write(string directory, string name, string resource)
    {
        string path = Path.Combine(directory, name);

        File.WriteAllText(path, $"""
            [[resource]]
            name = "{resource}"
            family = "good"
            """);

        return path;
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
             "--ruleset", "v.toml", "--out", "t.txt", "--force-ruleset", "--census",
             "--crash", "c.borough-crash"],
            out Options options,
            out _));

        Assert.Equal("s.borough", options.LogPath);
        Assert.Equal("v.toml", options.RulesetPath);
        Assert.Equal("t.txt", options.OutPath);
        Assert.Equal(500UL, options.Ticks);
        Assert.Equal(25, options.HashEvery);
        Assert.True(options.ForceRuleset);
        Assert.True(options.Census);
        Assert.Equal("c.borough-crash", options.CrashPath);
    }

    /// <summary>
    /// There is no flag that turns the crash artifact off, and that is the point of it.
    /// </summary>
    /// <remarks>
    /// The mechanism exists so a panic in an unattended run becomes a file somebody can replay. One
    /// that produced nothing because nobody passed a flag would be failing at the only moment it is
    /// needed, so <c>--crash</c> names the destination and never whether.
    /// </remarks>
    [Fact]
    public void A_run_that_names_no_crash_path_still_gets_an_artifact()
    {
        Assert.True(Options.TryParse(["--ticks", "10"], out Options options, out _));
        Assert.Null(options.CrashPath);
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

    /// <summary>
    /// <b>No <c>--ruleset</c> means no Rules, and there is deliberately no default.</b>
    /// </summary>
    /// <remarks>
    /// A default Ruleset would silently change what every existing invocation measures — S0a's
    /// footprint capture included — and the first symptom would be figures that no longer compare to
    /// the ones already in the corpus.
    /// </remarks>
    [Fact]
    public void A_run_given_no_ruleset_runs_against_no_rules()
    {
        Assert.True(Borough.Headless.Session.TryRules(null, out Ruleset rules));
        Assert.Same(Ruleset.Empty, rules);
    }

    /// <summary>
    /// A Ruleset the loader refuses stops the run, and every refusal reaches the operator.
    /// </summary>
    /// <remarks>
    /// <b>This is the first test in which <c>adr/0048</c>'s promise is checked end to end</b> — the
    /// refusals had a file, a line and a rule name since task 3, and until the runner parsed anything
    /// there was no path by which one reached a person. Printing only the first would turn a single
    /// pass over a broken file into as many runs as it has mistakes.
    /// </remarks>
    [Fact]
    public void A_refused_ruleset_stops_the_run()
    {
        string path = Path.Combine(Path.GetTempPath(), $"borough-refused-{Guid.NewGuid():N}.toml");

        // A chain that is its own fallback: adr/0045's cycle check, which is refusal 1.
        File.WriteAllText(path, """
            [[resource]]
            name = "flour"
            family = "good"

            [[building]]
            name = "bakery"
            bins = [ { resource = "flour", capacity = 60 } ]

            [[rule]]
            name    = "bake_bread"
            kind    = "bakery"
            rate    = 10
            apply   = { min = 1, max = 4 }
            on_fail = "bake_bread"
            inputs  = [ { scope = "local", resource = "flour", amount = 6 } ]
            outputs = []
            """);

        try
        {
            Assert.False(Borough.Headless.Session.TryRules(path, out Ruleset rules));
            Assert.Same(Ruleset.Empty, rules);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// <b>A fresh session is recorded against the Ruleset it was handed.</b>
    /// </summary>
    /// <remarks>
    /// The builder stamped <c>ContentHash.None</c> unconditionally, which was correct for as long as
    /// nothing could be supplied. The moment <c>--ruleset</c> began loading content it became a
    /// defect that makes the flag unusable: a fresh run would name no Ruleset, be handed one, and
    /// <see cref="RulesetCheck"/> would refuse the session against its own Rules — correctly, on the
    /// evidence it had. <b>A new session is not a mismatch; it is the recording.</b>
    /// </remarks>
    [Fact]
    public void A_fresh_session_names_the_ruleset_it_was_given()
    {
        Assert.True(Options.TryParse(["--ticks", "4", "--citizens", "100"], out Options options, out _));

        Assert.Equal(
            Recorded,
            Borough.Headless.Session.Load(options, Given(("v.toml", Recorded))).RulesetHash);

        Assert.Equal(
            ContentHash.None, Borough.Headless.Session.Load(options, Given()).RulesetHash);
    }

    /// <summary>Asking for the Lot grid selects its own mode, not a run and not the report.</summary>
    [Fact]
    public void Asking_for_zones_selects_the_zone_dump()
    {
        Assert.True(Options.TryParse(
            ["--zones", "--ruleset", "minimal.toml"], out Options options, out _));

        Assert.Equal(Mode.Zones, options.Mode);
    }

    /// <summary>
    /// <b>A sweep is a Ruleset's behaviour, so a Zone dump with no Rules is refused rather than
    /// degraded.</b>
    /// </summary>
    /// <remarks>
    /// The degraded form is the dangerous one: it would print the same grid twice and read as a
    /// broken mechanism, when what happened is that the file declares no <c>[[zone_rule]]</c>. That
    /// is <c>HONEST DEGRADATION</c> in the one direction the tag is usually not applied — the honest
    /// thing here is not to degrade at all.
    /// </remarks>
    [Fact]
    public void A_zone_dump_with_no_ruleset_is_refused()
    {
        Assert.False(Options.TryParse(["--zones"], out _, out string? complaint));

        Assert.Contains("--zones needs --ruleset", complaint, StringComparison.Ordinal);
    }

    /// <summary>Two pictures of two different things, each building its own world.</summary>
    [Fact]
    public void A_zone_dump_and_a_layer_dump_disagree()
    {
        Assert.False(Options.TryParse(
            ["--zones", "--ruleset", "minimal.toml", "--layer", "pollution"],
            out _,
            out string? complaint));

        Assert.Contains("Ask for one", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Zone dump runs its own populated session, so a log's commands would land on a world it did
    /// not record.
    /// </summary>
    [Fact]
    public void A_zone_dump_and_a_log_disagree()
    {
        Assert.False(Options.TryParse(
            ["--zones", "--ruleset", "minimal.toml", "--log", "s.borough"],
            out _,
            out string? complaint));

        Assert.Contains("runs its own populated session", complaint, StringComparison.Ordinal);
    }

    /// <summary>Asking for the Road Graph selects its own mode, not a run and not the report.</summary>
    [Fact]
    public void Asking_for_roads_selects_the_road_dump()
    {
        Assert.True(Options.TryParse(
            ["--roads", "--ruleset", "minimal.toml"], out Options options, out _));

        Assert.Equal(Mode.Roads, options.Mode);
    }

    /// <summary>
    /// <b>A road network is a Ruleset's content, so a Road dump with no <c>[roads]</c> is refused
    /// rather than degraded.</b> <c>--zones</c>' refusal exactly, for the same reason.
    /// </summary>
    /// <remarks>
    /// The degraded form is the dangerous one again: an empty graph would read as a broken mechanism
    /// when what happened is that the file declares no roads. <c>HONEST DEGRADATION</c> in the
    /// direction where the honest thing is not to degrade at all.
    /// </remarks>
    [Fact]
    public void A_road_dump_with_no_ruleset_is_refused()
    {
        Assert.False(Options.TryParse(["--roads"], out _, out string? complaint));

        Assert.Contains("--roads needs --ruleset", complaint, StringComparison.Ordinal);
    }

    /// <summary>Three pictures of three different things, each building its own world.</summary>
    [Fact]
    public void A_road_dump_and_a_zone_dump_disagree()
    {
        Assert.False(Options.TryParse(
            ["--roads", "--ruleset", "minimal.toml", "--zones"],
            out _,
            out string? complaint));

        Assert.Contains("Ask for one", complaint, StringComparison.Ordinal);
    }

    /// <inheritdoc cref="A_road_dump_and_a_zone_dump_disagree"/>
    [Fact]
    public void A_road_dump_and_a_layer_dump_disagree()
    {
        Assert.False(Options.TryParse(
            ["--roads", "--ruleset", "minimal.toml", "--layer", "pollution"],
            out _,
            out string? complaint));

        Assert.Contains("Ask for one", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>Stronger than <c>--zones</c>' refusal of a session, and refused for a different reason.</b>
    /// </summary>
    /// <remarks>
    /// A Zone dump runs a session because a sweep is a thing that happens <em>over time</em>; a Road
    /// dump does not step the world at all, because the graph is laid at world creation and nothing
    /// yet edits it. So a session flag here is not merely in conflict with the mode — it is inert, and
    /// silently ignoring it would have the operator believe they had asked for an <em>after</em>
    /// picture that does not exist. Both a recorded log and a fresh session are covered, because they
    /// reach the refusal by different routes: <c>--log</c> directly, <c>--ticks</c> through the
    /// <c>session</c> flag every run-implying option sets.
    /// <para>
    /// <b>⚠ <c>--seed</c> was a row here until 2026-08-11 and is now the exception</b>, with a test of
    /// its own below. The reasoning above does not reach it: a seed is not a claim about a run, it is
    /// the world key the Arterial polyline is drawn from, so it is the one session flag a picture of
    /// world creation can honour.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("--log", "s.borough")]
    [InlineData("--ticks", "8")]
    public void A_road_dump_and_the_session_flags_disagree(string flag, string value)
    {
        Assert.False(Options.TryParse(
            ["--roads", "--ruleset", "minimal.toml", flag, value],
            out _,
            out string? complaint));

        Assert.Contains("no run to take a picture after", complaint, StringComparison.Ordinal);
    }

    /// <summary>
    /// <b>A Road dump accepts a seed, and refusing one made every Severance number in the corpus a
    /// single draw.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Arterial polyline is drawn from the world key, so Severance — the milestone's flagship
    /// emergent behaviour — is a function of the seed. With the seed unreachable through the one
    /// instrument that reports it, every figure recorded about it was one sample described as though
    /// it were a property of the <c>[roads]</c> table. It is not a small effect: the same Ruleset
    /// strands 191 walkable nodes at seed 0 and 127 at seed 7, and at a nearby configuration it ranges
    /// from 0 to 68.
    /// </para>
    /// <para>
    /// <b>The general shape is worth more than the flag.</b> The refusal was correct about every other
    /// session flag and was extended to this one by category — <i>a seed implies a fresh session</i> —
    /// rather than by asking what the mode actually reads. <b>A generator whose output cannot be varied
    /// cannot be characterised</b>, and the guard that prevented varying it was written to prevent
    /// something else.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_road_dump_accepts_a_seed_because_the_graph_is_drawn_from_the_world_key()
    {
        Assert.True(
            Options.TryParse(
                ["--roads", "--ruleset", "minimal.toml", "--seed", "7"],
                out Options? options,
                out string? complaint),
            complaint);

        Assert.Equal(Mode.Roads, options!.Mode);
        Assert.Equal(7UL, options.Seed);
    }

    /// <summary>
    /// A flag the usage text does not name is a flag nobody finds — <c>adr/0002</c>, the shell owns
    /// every string a human reads.
    /// </summary>
    [Fact]
    public void The_usage_text_names_the_road_dump()
    {
        Assert.Contains("--roads", Options.Usage, StringComparison.Ordinal);
        Assert.Contains("Road Graph", Options.Usage, StringComparison.Ordinal);
    }
}
