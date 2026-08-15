using System.Globalization;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>What the runner was asked to do.</summary>
internal enum Mode
{
    /// <summary>Build a synthetic city and print what is in it. Slice 4's artefact.</summary>
    Report,

    /// <summary>Run a session and print its State Hash trace. Slice 5's.</summary>
    Run,

    /// <summary>
    /// Print a Map Layer's Cell grid before and after a source change. Slice 6's.
    /// </summary>
    /// <remarks>
    /// A third mode rather than a flag on the other two, for the reason the first two are separate:
    /// it needs a world with sources in it, and no session can put them there until Rules exist. A
    /// Layer dump taken at the end of a replay would print an empty grid.
    /// </remarks>
    Layer,

    /// <summary>
    /// Print the Lot grid by permission and occupancy, before and after a run. Slice 10's.
    /// </summary>
    /// <remarks>
    /// A fourth mode for <see cref="Layer"/>'s reason turned around. That one builds its own sources
    /// because no session can place any; this one runs a real session because a sweep is a thing that
    /// happens <em>over time</em>, and it refuses without a Ruleset because Zone Rules are content
    /// and there is nothing to invent in their place.
    /// </remarks>
    Zones,

    /// <summary>
    /// Print the Road Graph and its connected components. Slice 5a's.
    /// </summary>
    /// <remarks>
    /// A fifth mode on <see cref="Zones"/>' reasoning, with one difference worth stating: a Zone dump
    /// runs a session because a sweep happens <em>over time</em>, and this one does not, because the
    /// graph is laid at world creation and nothing yet edits it. It still refuses without a Ruleset
    /// for the same reason — a road network is content, and an empty picture would read as a broken
    /// mechanism rather than as a file that declares no <c>[roads]</c>.
    /// </remarks>
    Roads,

    /// <summary>
    /// Print what it costs to walk across this city, and how much further than the grid. 5b's.
    /// </summary>
    /// <remarks>
    /// A sixth mode on <see cref="Roads"/>' reasoning exactly — it does not step the world, because
    /// nothing generates a Trip yet and the graph it walks is laid at world creation. It is the
    /// <em>second</em> Severance instrument rather than the first: <see cref="Roads"/> measures
    /// disconnection and says in its own output that the larger half is <b>detour</b>, which needs a
    /// shortest path and is what milestone 5b built.
    /// </remarks>
    Trips,

    /// <summary>
    /// Print where people work against where they live, before and after the jobs are taken. 5b-bis's.
    /// </summary>
    /// <remarks>
    /// A seventh mode, and it is <see cref="Zones"/>' shape rather than <see cref="Trips"/>': it
    /// <b>steps the world</b>, because employment is a thing that happens over time and a city at
    /// Tick 0 has nobody employed at all — so unlike the two pictures before it, this one has a real
    /// <em>before</em>. It refuses without a Ruleset for every picture's reason, and refuses a
    /// Ruleset with no <c>[jobs]</c> for <see cref="Zones"/>' exactly: employment is content, and a
    /// grid of unemployment would read as a broken pass rather than as a file that grants no work.
    /// </remarks>
    Commute,

    /// <summary>
    /// Print where the traffic is, and what the volume-delay function does to it. 5c's.
    /// </summary>
    /// <remarks>
    /// An eighth mode, and <b>the first whose control is a Ruleset rather than a clock</b>. Every
    /// picture before it takes Tick 0 as its <em>before</em>; volume at Tick 0 is zero on every
    /// Segment of every world, so that panel would be blank on every input. This one steps the same
    /// city <b>twice</b> — identical seed, population and commands, differing only in whether the
    /// Ruleset states <c>[traffic]</c> — so the two panels answer the question the milestone has:
    /// does the function do anything, and in the right direction? It refuses a Ruleset that states no
    /// <c>[traffic]</c> and one that states no <c>[households]</c>, which is <see cref="Zones"/>'
    /// polarity, and ⚠ <b>that means it refuses all three shipped files</b>: neither table is stated
    /// anywhere, which is a recorded coverage hole rather than an accident (<c>adr/0099</c>).
    /// </remarks>
    Traffic,
}

/// <summary>
/// The command line, parsed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two modes, because the runner does two things and one of them cannot see the other's city.</b>
/// The table report needs a populated world, and before slice 7 the only verb a session can apply is
/// Zone — so a report printed at the end of a replay would show four rows and three empty tables.
/// They are kept as separate modes rather than merged into one that degrades.
/// </para>
/// <para>
/// <b>Hand-rolled rather than a parsing library.</b> <c>adr/0018</c> prefers off-the-shelf
/// infrastructure and requires a written exception naming the property no library provides — this is
/// not that. It is below the threshold the ADR is aimed at: nine flags, no subcommands, no
/// completion, and no dependency worth carrying into the one project whose job is to prove it can
/// build with nothing installed. If the surface grows subcommands, take the library.
/// </para>
/// </remarks>
internal sealed class Options
{
    /// <summary>Slice 4's report population, kept as the no-argument behaviour.</summary>
    private const int DefaultPopulation = 10_000;

    private Options()
    {
    }

    public Mode Mode { get; private init; }

    /// <summary>A recorded session to replay, or null to run a fresh one.</summary>
    public string? LogPath { get; private init; }

    /// <summary>
    /// Every Ruleset this run was given, in the order the operator named them.
    /// </summary>
    /// <remarks>
    /// <b><c>--ruleset</c> repeats, because a session that reloaded twice was played against
    /// three.</b> <c>InputLog.cs:131</c> wrote the problem down before anything could reload; slice 8
    /// task 9 is where it stops being hypothetical. A replay resolves each transition's content hash
    /// against this set and <b>refuses an unaccounted one</b> rather than diverging (<c>05 §7</c>).
    /// <b>Order is the operator's convenience and not a contract</b> — the runner puts the Ruleset the
    /// log opens with first, because <see cref="Borough.Core.Rules.RulesetCatalogue"/> takes its
    /// opening entry from position 0 and getting that wrong is a divergence with no symptom.
    /// </remarks>
    public IReadOnlyList<string> RulesetPaths { get; private init; } = [];

    /// <summary>
    /// The first Ruleset named, or null if none was. <b>The opening one, after the runner's sort.</b>
    /// </summary>
    public string? RulesetPath => RulesetPaths.Count == 0 ? null : RulesetPaths[0];

    /// <summary>
    /// The Ticks a fresh session reloads on, one per Ruleset after the first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is what makes <c>adr/0015</c>'s acceptance test runnable, and it is not a
    /// convenience.</b> The ADR's whole claim is that changing a production ratio and seeing the
    /// effect takes seconds; there is no Godot shell, so the loop has to close here or it does not
    /// close. It cannot close through a recorded log, and finding that out is the reason this flag
    /// exists: a log records a transition by <em>content hash</em>, so the first edit to the file
    /// makes the log name a Ruleset that no longer exists and the run is refused. **Editing the file
    /// is what the loop is**, so a log is structurally the wrong instrument for it.
    /// </para>
    /// <para>
    /// <b>It is refused with <c>--log</c> rather than merged into one.</b> A replay's transitions are
    /// what it is reproducing; letting the command line add one would make a run that is neither the
    /// recorded session nor a new one, and no divergence in it could be attributed.
    /// </para>
    /// </remarks>
    public IReadOnlyList<ulong> ReloadTicks { get; private init; } = [];

    /// <summary>Where the trace goes. Null is standard output.</summary>
    public string? OutPath { get; private init; }

    /// <summary>The seed for a fresh session.</summary>
    public ulong Seed { get; private init; }

    /// <summary>Citizen sizing, for a fresh session or for the report.</summary>
    public int Citizens { get; private init; } = DefaultPopulation;

    /// <summary>How many Ticks to run.</summary>
    public ulong Ticks { get; private init; } = 1_024;

    /// <summary>The trace's sampling cadence.</summary>
    public int HashEvery { get; private init; } = 64;

    /// <summary>
    /// Run despite a Ruleset the session was not recorded against.
    /// </summary>
    /// <remarks>
    /// <b>The escape hatch is opt-in and the refusal is the default, which is the opposite polarity
    /// from the flag <c>plans/0008</c> sketched.</b> <c>05 §7</c> is explicit that
    /// <c>Borough.Headless</c> <em>is</em> replay mode and strict — so a <c>--strict</c> opt-in would
    /// have implied a lenient default the corpus denies. There is still a real use for running the
    /// mismatch deliberately, which is asking how far a Ruleset change moves the city; what that must
    /// not do is produce numbers that look comparable. So the trace it writes is stamped
    /// <c>hash-broken</c>, in the spirit of <c>05 §7</c>'s save mark.
    /// </remarks>
    public bool ForceRuleset { get; private init; }

    /// <summary>
    /// Sample every collection's size on the trace cadence and print the series at the end.
    /// </summary>
    /// <remarks>
    /// <b>Opt-in, because it is the one thing here that costs something the run did not ask for.</b>
    /// The readings are cheap but the ring is not free, and a run whose question is <em>did the hash
    /// change</em> has no use for it. It is also the flag that will grow an assertion when slice 7
    /// gives the world churn to reach a steady state with; today it reports and judges nothing.
    /// </remarks>
    public bool Census { get; private init; }

    /// <summary>
    /// Where to write the crash artifact, or null for a name derived from the Tick that panicked.
    /// </summary>
    /// <remarks>
    /// <b>There is no flag to turn the artifact off, and a default path rather than none.</b> The
    /// mechanism exists so that a panic in an unattended run becomes a file somebody can replay
    /// (<c>05 §8</c>); a crash that produced nothing because nobody passed a flag is the mechanism
    /// failing at the only moment it is needed. The flag names the destination, never whether.
    /// </remarks>
    public string? CrashPath { get; private init; }

    /// <summary>
    /// Prove every Tick that Phase 2 wrote nothing (<c>adr/0037</c>). On unless turned off.
    /// </summary>
    /// <remarks>
    /// <b>The switch exists because the guard is <c>O(world)</c> and the runs that need it turned off
    /// are the ones this runner is for.</b> <c>Simulation.VerifyDecideWritesNothing</c> folds every
    /// column of every table twice per Tick, and its own documentation says to turn it off for the
    /// 100,000-Tick test — which was not possible from the command line until spike <c>S0</c> tried to
    /// run one at the 1M target and found the guard was the whole Tick. The polarity is deliberate:
    /// the correctness check is the default and the fast run is the thing you ask for by name.
    /// </remarks>
    public bool DecideGuard { get; private init; } = true;

    /// <summary>Which Map Layer to dump, in <see cref="Mode.Layer"/>.</summary>
    public Layer Layer { get; private init; }

    /// <summary>Dump the Layer as CSV rather than as an ASCII field.</summary>
    /// <remarks>
    /// <b>Both, because they answer different questions.</b> The ASCII form is for judging a
    /// <em>shape</em> — a directional smear or a hard kernel edge is obvious in it and invisible in a
    /// column of numbers — and the CSV form is for anybody comparing values, because the ramp is a
    /// nine-step display quantisation and nothing should read it back.
    /// </remarks>
    public bool Csv { get; private init; }

    /// <summary>
    /// Parses the command line, or explains why it could not.
    /// </summary>
    public static bool TryParse(string[] arguments, out Options options, out string? complaint)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        options = new Options();
        complaint = null;

        string? log = null;
        List<string> rulesets = [];
        List<ulong> reloadAt = [];
        string? output = null;
        string? crash = null;
        ulong seed = 0;
        int citizens = DefaultPopulation;
        ulong ticks = 1_024;
        int hashEvery = 64;
        bool force = false;
        bool census = false;
        bool session = false;

        // Tracked apart from `session` for one reason: a Road dump is laid at world creation from the
        // world key, so a seed is the one session flag that means something to it. See the refusal
        // below, and `RoadDump`'s remarks on why varying it matters.
        bool seeded = false;
        bool citizensGiven = false;
        bool csv = false;
        bool decideGuard = true;
        bool zones = false;
        bool roads = false;
        bool trips = false;
        bool commute = false;
        bool traffic = false;
        Layer? dump = null;

        for (int i = 0; i < arguments.Length; i++)
        {
            string flag = arguments[i];

            switch (flag)
            {
                case "--force-ruleset":
                    force = true;
                    continue;

                // A census is a property of a run, so asking for one is asking for a run — the same
                // reasoning that makes --ticks and --seed imply a session rather than the report.
                case "--census":
                    census = true;
                    session = true;
                    continue;

                case "--csv":
                    csv = true;
                    continue;

                // Deliberately not a session flag, unlike --census. A Zone dump *is* a run, and the
                // mode it selects already implies one; setting `session` too would make it collide
                // with --layer's refusal below for a reason the operator did not cause.
                case "--zones":
                    zones = true;
                    continue;

                // Not a session flag either, and for a stronger reason than --zones': a Road dump does
                // not step the world at all. The graph is laid at world creation and nothing yet edits
                // it, so there is no *after* picture to take.
                case "--roads":
                    roads = true;
                    continue;

                // Not a session flag, for --roads' reason and one more: a Trip dump walks a graph
                // that nothing edits, between Buildings that nothing moves, so stepping the world
                // would change none of its numbers and would only make it slower.
                case "--trips":
                    trips = true;
                    continue;

                // A session flag, and the only picture that is one besides --zones. Employment is a
                // thing that happens over time -- a city at Tick 0 has nobody employed at all -- so
                // this dump has a real *before* where --roads and --trips have none.
                case "--commute":
                    commute = true;
                    session = true;
                    continue;

                // A session flag for --commute's reason, and one more of its own: a road is only busy
                // while somebody is on it, so there is nothing to see in a world that has not stepped.
                case "--traffic":
                    traffic = true;
                    session = true;
                    continue;

                // A run, for the same reason --census is: the guard is a property of stepping a world,
                // and the report never steps one.
                case "--no-decide-guard":
                    decideGuard = false;
                    session = true;
                    continue;

                case "--help" or "-h":
                    complaint = string.Empty;
                    return false;
            }

            if (i + 1 >= arguments.Length)
            {
                complaint = $"{flag} needs a value.";
                return false;
            }

            string value = arguments[++i];

            switch (flag)
            {
                case "--log":
                    log = value;
                    session = true;
                    break;

                case "--ruleset":
                    // Repeats rather than replaces. Naming the same file twice is refused here
                    // instead of at the catalogue, because "you passed it twice" is the operator's
                    // sentence and "two Rulesets carry one content hash" is the format's.
                    if (rulesets.Contains(value, StringComparer.Ordinal))
                    {
                        complaint = $"--ruleset {value} was given twice. Each names one Ruleset the "
                                  + "session was played against, and a duplicate makes a reload "
                                  + "ambiguous.";
                        return false;
                    }

                    rulesets.Add(value);
                    break;

                case "--reload-at":
                    if (!TryNumber(value, out ulong at) || at == 0)
                    {
                        complaint = $"--reload-at {value} is not a Tick after 0. Tick 0 establishes "
                                  + "the opening Ruleset rather than swapping, so a reload there "
                                  + "could never have taken effect.";
                        return false;
                    }

                    reloadAt.Add(at);
                    session = true;
                    break;

                case "--out":
                    output = value;
                    break;

                case "--crash":
                    crash = value;
                    break;

                case "--seed":
                    if (!TryNumber(value, out seed))
                    {
                        complaint = $"--seed {value} is not a number.";
                        return false;
                    }

                    seeded = true;
                    break;

                case "--citizens":
                    if (!TryCount(value, out citizens))
                    {
                        complaint = $"--citizens {value} is not a positive count.";
                        return false;
                    }

                    citizensGiven = true;
                    break;

                case "--ticks":
                    if (!TryNumber(value, out ticks) || ticks == 0)
                    {
                        complaint = $"--ticks {value} is not a positive count.";
                        return false;
                    }

                    session = true;
                    break;

                case "--layer":
                    if (!LayerDump.TryParse(value, out Layer named))
                    {
                        complaint = $"--layer {value} is not a Map Layer. "
                                  + "The Layers are pollution, land-value and sealing. Noise is not "
                                  + "one: it is a line source and a point-of-use query (adr/0034).";
                        return false;
                    }

                    dump = named;
                    break;

                case "--hash-every":
                    if (!TryCount(value, out hashEvery))
                    {
                        complaint = $"--hash-every {value} is not a positive count.";
                        return false;
                    }

                    break;

                default:
                    complaint = $"{flag} is not an option this runner knows.";
                    return false;
            }
        }

        if (log is not null && citizensGiven)
        {
            complaint = "--citizens and --log disagree: a log carries its own configuration, "
                      + "and a replay that took its world size from the command line would be "
                      + "reproducing a different session.";
            return false;
        }

        if (dump is not null && (session || seeded))
        {
            complaint = "--layer and the session flags disagree: a Layer dump builds its own world "
                      + "with sources in it, because no session can place a source until Rules exist.";
            return false;
        }

        if (zones && dump is not null)
        {
            complaint = "--zones and --layer are two pictures of two different things, and each "
                      + "builds its own world. Ask for one.";
            return false;
        }

        // A sweep is a Ruleset's behaviour, so a Zone dump with no Rules would print the same grid
        // twice and read as a broken mechanism rather than an absent one. Refuse instead of degrade.
        if (zones && rulesets.Count == 0)
        {
            complaint = "--zones needs --ruleset PATH. A Zone Rule is content, not a default: "
                      + "without one there is no sweep to show, and an unchanging grid would look "
                      + "like a defect rather than like a Ruleset that declares no [[zone_rule]].";
            return false;
        }

        if (reloadAt.Count > 0 && log is not null)
        {
            complaint = "--reload-at and --log disagree: a recorded session carries its own "
                      + "transitions, and a replay that took one from the command line would be "
                      + "reproducing neither the session it was given nor a new one.";
            return false;
        }

        if (reloadAt.Count != 0 && reloadAt.Count != rulesets.Count - 1)
        {
            complaint = $"--reload-at was given {reloadAt.Count} time(s) against "
                      + $"{rulesets.Count} --ruleset(s). Each reload swaps to the next Ruleset "
                      + "named, so there is exactly one Tick per Ruleset after the first.";
            return false;
        }

        for (int i = 1; i < reloadAt.Count; i++)
        {
            if (reloadAt[i] <= reloadAt[i - 1])
            {
                complaint = $"--reload-at {reloadAt[i]} does not follow {reloadAt[i - 1]}. A session "
                          + "reloads in the order it runs, and a Tick carries exactly one Ruleset.";
                return false;
            }
        }

        if (rulesets.Count > 1 && reloadAt.Count == 0 && log is null)
        {
            complaint = $"{rulesets.Count} --ruleset(s) were given and no --log and no --reload-at, "
                      + "so nothing says when the second one comes into force. A fresh session "
                      + "reloads where the command line says it does.";
            return false;
        }

        if (zones && log is not null)
        {
            complaint = "--zones and --log disagree: a Zone dump runs its own populated session, and "
                      + "a replay's commands would be applied to a world it did not record.";
            return false;
        }

        // Ahead of --commute's for the same reason --commute's is ahead of --roads': --traffic is
        // also a session, so the more general complaint below would fire first and name the wrong
        // mistake. The most specific complaint wins.
        if (traffic && (commute || zones || roads || trips || dump is not null))
        {
            complaint = "--traffic asks for a sixth picture, and each of the six builds its own "
                      + "world. Ask for one.";
            return false;
        }

        // Ahead of every session-flag refusal below, and the ordering is load-bearing rather than
        // tidy. --commute IS a session, so `roads && session` would fire first and complain that the
        // Road Graph has no *after* -- which is true, and is not what the operator got wrong. The
        // most specific complaint wins.
        if (commute && (zones || roads || trips || dump is not null))
        {
            complaint = "--commute asks for a fifth picture, and each of the five builds its own "
                      + "world. Ask for one.";
            return false;
        }

        if (roads && (zones || dump is not null))
        {
            complaint = "--roads asks for a third picture, and each of the three builds its own "
                      + "world. Ask for one.";
            return false;
        }

        // A road network is a Ruleset's content, so a Road dump with no [roads] would print an empty
        // graph and read as a broken mechanism rather than an absent one. --zones' refusal exactly.
        if (roads && rulesets.Count == 0)
        {
            complaint = "--roads needs --ruleset PATH. A road network is content, not a default: "
                      + "without one there is no graph to show, and an empty picture would look "
                      + "like a defect rather than like a Ruleset that declares no [roads].";
            return false;
        }

        // Stronger than --zones' refusal of --log, and stated separately because the reason differs:
        // a Zone dump runs a session and this one does not step the world at all, so every session
        // flag is not merely in conflict with it but inert.
        if (roads && (log is not null || session))
        {
            complaint = "--roads and the session flags disagree: the Road Graph is laid at world "
                      + "creation and nothing edits it yet, so there is no run to take a picture "
                      + "after. Drop the session flags, or ask for --zones instead. (--seed is the "
                      + "exception and is accepted: see below.)";
            return false;
        }

        if (trips && (zones || roads || dump is not null))
        {
            complaint = "--trips asks for a fourth picture, and each of the four builds its own "
                      + "world. Ask for one.";
            return false;
        }

        // --zones' refusal for --zones' reason, and one more of its own. Employment is content twice
        // over: [jobs] states the cadence and [[building]] jobs states the posts, so a Ruleset that
        // declares neither produces a city in which nobody can ever be employed -- and a grid of
        // unemployment would read as a broken assignment pass rather than as a file that grants no
        // work.
        if (commute && rulesets.Count == 0)
        {
            complaint = "--commute needs --ruleset PATH. Employment is content: [jobs] states how "
                      + "often the assignment pass looks and [[building]] jobs states how many "
                      + "posts a Building has, and neither has a default. A city with no jobs is a "
                      + "picture of nothing.";
            return false;
        }

        // Accepted with --log, unlike every other picture, and the asymmetry is the point: this one
        // runs a session, so a recorded one is a legitimate thing to take a picture of.
        if (commute && log is not null)
        {
            complaint = "--commute and --log disagree, though not for the usual reason. The dump "
                      + "populates its own world and steps it, so a recorded session would be "
                      + "replayed and then over-populated. Drop --log, or ask for --census on the "
                      + "replay instead.";
            return false;
        }

        // --zones' refusal, and the state of the world makes it sharper than the others: NO shipped
        // Ruleset states [traffic] or [households], so this is the refusal an operator will actually
        // meet. TrafficDump prints the two tables to add; this only says a file is needed at all.
        if (traffic && rulesets.Count == 0)
        {
            complaint = "--traffic needs --ruleset PATH. Congestion is content: [traffic] states the "
                      + "volume-delay function and [households] states who owns a car, and neither "
                      + "has a default. No shipped Ruleset states either, so this picture needs a "
                      + "file written for it.";
            return false;
        }

        // --commute's refusal for --commute's reason: the dump populates and steps its own world.
        if (traffic && log is not null)
        {
            complaint = "--traffic and --log disagree: the dump populates its own world and steps "
                      + "it twice, so a recorded session would be replayed and then over-populated.";
            return false;
        }

        // --roads' refusal, for --roads' reason: the Streets a walk uses are content. A Trip dump
        // with no [roads] would print a table of dashes, and a table of dashes reads as a broken
        // instrument rather than as a Ruleset that declares no network.
        if (trips && rulesets.Count == 0)
        {
            complaint = "--trips needs --ruleset PATH. The Streets a walk uses are content, not a "
                      + "default: without them there is nothing to walk on, and an empty table "
                      + "would look like a defect rather than like a Ruleset that declares no "
                      + "[roads].";
            return false;
        }

        // --roads' refusal again, and the reason is if anything stronger. A Trip dump walks a graph
        // nothing edits, between Buildings nothing moves, so a session would change none of its
        // numbers. It cannot even be defended as slow-but-honest: a Trip that a Tick produced would
        // be a different measurement, and no Tick produces one (plans/0002 §A).
        if (trips && (log is not null || session))
        {
            complaint = "--trips and the session flags disagree: nothing generates a Trip yet, and "
                      + "the graph a walk uses is laid at world creation, so there is no run to "
                      + "take a picture after. Drop the session flags. (--seed is the exception "
                      + "and is accepted, for --roads' reason: the Arterials are drawn from it.)";
            return false;
        }

        // --seed IS accepted with --roads, and the refusal above deliberately does not catch it.
        //
        // It used to. The Arterial polyline is drawn from the world key, so the whole of Severance is
        // a function of the seed -- and with the seed unreachable, every Severance number in this
        // corpus was ONE DRAW of it, described as though it were a property of the [roads] table.
        // The 2026-08-11 sweep found the same Ruleset stranding 46 of 285 walkable nodes at seed 0
        // and 4 of 289 at another, which is the difference between a demonstration and a rounding
        // error. A generator whose output cannot be varied cannot be characterised.
        session = session || seeded;

        options = new Options
        {
            Mode = traffic ? Mode.Traffic
                 : commute ? Mode.Commute
                 : trips ? Mode.Trips
                 : roads ? Mode.Roads
                 : zones ? Mode.Zones
                 : dump is not null ? Mode.Layer
                 : session ? Mode.Run
                 : Mode.Report,
            Layer = dump ?? default,
            Csv = csv,
            LogPath = log,
            RulesetPaths = rulesets,
            ReloadTicks = reloadAt,
            OutPath = output,
            Seed = seed,
            Citizens = citizens,
            Ticks = ticks,
            HashEvery = hashEvery,
            ForceRuleset = force,
            Census = census,
            CrashPath = crash,
            DecideGuard = decideGuard,
        };

        return true;
    }

    /// <summary>The usage text. Every string a human reads is the shell's (<c>adr/0002</c>).</summary>
    public static string Usage =>
        """
        Borough.Headless -- run a session and print its State Hash trace.

          (no options)          the table report, at 10,000 Citizens

          --log PATH            replay a session recorded in a .borough file
          --seed N              run a fresh session with this seed and no commands
          --citizens N          Citizen sizing, for a fresh session or the report
          --ticks N             how many Ticks to run
          --hash-every N        trace sampling cadence, in Ticks
          --ruleset PATH        the Rules to run under. Loaded and put in force, and
                                the session must name it. Without one, nothing has
                                any Rules and the run simulates an inert city.
                                REPEATABLE: a session that reloaded twice was played
                                against three Rulesets, and every one of them has to be
                                here or the replay is refused. Order does not matter
          --reload-at N         a fresh session puts the next --ruleset in force on Tick N.
                                One per --ruleset after the first, in order. This is
                                adr/0015's iteration loop: edit a Ruleset, re-run, see the
                                city differ -- in seconds and with no rebuild. It is
                                refused with --log, which carries its own transitions
          --force-ruleset       run against a Ruleset the session does not name, and
                                stamp the trace hash-broken
          --out PATH            write the trace to a file instead of standard output
          --census              sample every collection's size and every counter the
                                simulation keeps, on the trace cadence, and print
                                first/last/low/high for each at the end. Seven
                                families: tables, rules, zones, placement, jobs,
                                Trip Fates, and the Trip cost histogram
          --crash PATH          where to write the crash artifact if the run panics.
                                One is always written; this only names where
          --no-decide-guard     stop proving every Tick that Phase 2 wrote nothing.
                                The proof is O(world) per Tick; turn it off for a
                                long run at scale and leave it on everywhere else
          --layer NAME          dump a Map Layer's Cell grid before and after a source
                                change, with the halo that was recomputed. NAME is
                                pollution, land-value or sealing
          --zones               dump the Lot grid by permission and occupancy, before and
                                after --ticks Ticks of sweeping, with what the sweep did.
                                Needs --ruleset, because a sweep is a Ruleset's behaviour
          --roads               dump the Road Graph -- Segments by kind, the Arcs each mode
                                admits, and the connected components of both subgraphs.
                                Needs --ruleset, because a road network is content. Takes
                                no session: the graph is laid at world creation
          --trips               dump what a walk costs between this city's Buildings, by
                                distance, and the DETOUR over the grid ideal -- the half of
                                Severance --roads says it cannot see. Needs --ruleset, takes
                                no session. Compare two Rulesets to read it
          --commute             dump where people work against where they live, by block,
                                before and after the jobs are taken, with what the run's
                                commutes cost. Needs --ruleset with a [jobs] table, and
                                runs a session because employment takes time
          --traffic             dump where the traffic is, by block, and what the volume-
                                delay function does to it -- the SAME city stepped twice,
                                once with [traffic] and once without. Needs --ruleset with
                                a [traffic] and a [households] table; no shipped file has
                                either, so this one needs a Ruleset written for it
          --csv                 dump the Layer, the Lot grid or the Segments as CSV rather
                                than as an ASCII field

        A replay whose Ruleset does not match refuses to run rather than diverging
        silently: a different Ruleset is a different simulation, and the divergence
        would be arithmetic rather than a bug. 05 section 7.
        """;

    private static bool TryNumber(string value, out ulong number) =>
        ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);

    private static bool TryCount(string value, out int count) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count > 0;
}
