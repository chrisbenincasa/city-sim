using System.Globalization;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>What the runner was asked to do.</summary>
internal enum Mode
{
    /// <summary>
    /// Follow one Citizen through one Day, Tick by Tick. <c>plans/0045</c>'s queue item 3.
    /// </summary>
    /// <remarks>
    /// The only mode that does not aggregate. Every other one answers <em>what is the city doing</em>;
    /// this answers <em>what is it like to be in it</em>, which is the first pillar and which nothing
    /// in the tree had ever printed.
    /// </remarks>
    Day,

    /// <summary>
    /// Place a handful of schools and report who can get to one. <c>plans/0045</c>'s queue item 10.
    /// </summary>
    /// <remarks>
    /// <b>The only mode that issues a PLAYER'S verb.</b> Every other picture either builds a
    /// synthetic city or steps one; <c>Service</c> is <c>01 §5</c>'s placement exception and there is
    /// no generator that will ever site a school, so a dump of the mechanism has to play the game.
    /// </remarks>
    School,

    /// <summary>
    /// Frames of the city as ASCII, Travellers over Buildings. <c>plans/0045</c>'s queue item 11a.
    /// </summary>
    Watch,

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

    /// <summary>
    /// Print what the city can say about why something happened to it. Milestone 6's.
    /// </summary>
    /// <remarks>
    /// A ninth mode, and the third that <b>steps the world</b>. A trail is a record of things that
    /// have happened, so a city at Tick 0 has an empty one and there is no <em>before</em> panel to
    /// take — <see cref="Traffic"/>'s situation, reached by a different route. ⚠ <b>It refuses a
    /// Ruleset that condemns nothing and does NOT refuse one that names no condition</b>, which is a
    /// departure from every refusal above it: an empty trail is uninterpretable, but a trail with one
    /// column of dashes under a heading that says which file fills it is the <em>legible absence</em>
    /// this milestone exists to produce. See <c>EvidenceDump</c>.
    /// </remarks>
    Evidence,

    /// <summary>
    /// Print where the city's money is and what moved it there. Milestone 10's.
    /// </summary>
    /// <remarks>
    /// A mode that <b>steps the world</b>, for <see cref="Evidence"/>'s reason
    /// turned into a stronger one: a flow is a thing that happens over an interval, so a city at Tick
    /// 0 has a balance sheet with no circuit under it at all. ⚠ <b>It refuses a Ruleset with no
    /// <c>[[policy]]</c> and one whose Policies move nothing conserved</b> — <see cref="Zones"/>'
    /// polarity rather than <see cref="Evidence"/>'s, because the absence here is not legible: a
    /// conservation identity that holds vacuously prints exactly like one that holds, so a city of
    /// paupers would print a balance sheet that says money is conserved and mean nothing by it.
    /// <c>rulesets/taxed.toml</c> is the file written for this picture.
    /// </remarks>
    Money,

    /// <summary>
    /// Print where people parked against where they were going. Milestone 7's.
    /// </summary>
    /// <remarks>
    /// A mode that <b>steps the world</b> — a car is parked somewhere only
    /// after somebody has driven there, so Tick 0 is empty on every input and there is no
    /// <em>before</em> panel to take. ⚠ <b>The quantity is a walk and not an occupancy</b>: capacity
    /// is declared per building <b>kind</b>, so a grid of occupied spaces is a grid of land use and
    /// <see cref="Zones"/> already draws that. It refuses a Ruleset that states no <c>[parking]</c>
    /// and one whose Households keep no car — <see cref="Traffic"/>'s polarity — and ⚠ <b>the second
    /// of those refuses four of the seven shipped files</b>, including <c>minimal.toml</c>, which is
    /// the same recorded coverage hole and the reason this milestone moved no baseline. See
    /// <c>ParkingDump</c>.
    /// </remarks>
    Parking,

    /// <summary>
    /// Print land value against the desirability it is chasing, and the gap. Milestone 9's.
    /// </summary>
    /// <remarks>
    /// A mode that <b>steps the world</b>, because the quantity is a <em>history</em>: land value
    /// moves slowly toward the current desirability rather than tracking it, so a world that has not
    /// run has nothing to show. ⚠ <b>Three grids and not one</b> — the target, the lag, and their
    /// difference — because ***a lag is not a property of a value, it is a property of a pair***. It
    /// refuses a Ruleset that declares no map emission, <see cref="Parking"/>'s polarity, and ⚠ <b>that
    /// refuses eight of the nine shipped files</b>: the only thing that creates a Cell row is a
    /// pollution emission and only <c>fouled.toml</c> emits. See <c>LandValueDump</c>.
    /// </remarks>
    LandValue,

    /// <summary>
    /// Print who came through the gates, who is waiting, who gave up, and what the money did.
    /// Milestone 11's.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The only mode that issues Commands</b>, and it is forced rather than chosen: nothing in
    /// the simulation decides to arrive until milestone 16 (<c>adr/0128</c>), so a dump that stepped
    /// empty Ticks would watch a door nobody knocked on. It asks each gate for <em>more than it can
    /// take</em> every Day, so the rate under observation is the Ruleset's <c>arrivals_per_day</c>
    /// rather than a cadence chosen here. It refuses a Ruleset declaring no gate kind, on
    /// <see cref="LandValue"/>'s polarity, and ⚠ <b>that refuses nine of the eleven shipped
    /// files</b>. See <c>ArrivalDump</c>.
    /// </remarks>
    Arrivals,

    /// <summary>The floods, printed — what the Hazard Region's first consumer did.</summary>
    Flood,

    /// <summary>
    /// The economic actor: how many there are, where they got them, what they hold, who works in
    /// them, and what read one. Milestone 27's.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Five panels, because the milestone's risk has four halves and one of them needs its own
    /// caveat.</b> <c>plans/0041</c>: *the city creates one, funds one, employs through one, and a Rule
    /// can read its balance.* It refuses a Ruleset in which no Business can be created — ***which is
    /// a different test from whether a trade is declared***, since <c>rulesets/tenanted.toml</c>
    /// declares two and builds neither. See <c>BusinessDump</c>.
    /// </remarks>
    Business,

    /// <summary>
    /// The market: what a District Pool's rows hold, what their price did, and who could not afford
    /// what they were selling. Milestone 26's.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Three panels, and the third is the reason the mode exists</b> — <c>plans/0044</c> task 8:
    /// *"the third clause is the one that would be dropped, and it is the only one that shows the
    /// market having a consequence."* It refuses on three separate grounds, of which the one that
    /// would be missed is ***a Ruleset that declares a trade and gives no kind a Good to sell*** —
    /// <c>rulesets/twinned.toml</c>, a market with one side. See <c>MarketDump</c>.
    /// </remarks>
    Market,

    /// <summary>
    /// Print the city's age structure, Day by Day. <c>plans/0046</c> stage 1's artefact.
    /// </summary>
    /// <remarks>
    /// <b>A session mode for <see cref="LandValue"/>'s reason at its most literal.</b> A Life Stage
    /// is a quantity that only exists as a <em>trajectory</em>: a world that has not run holds the
    /// stages it was created with, and a single snapshot of those is a picture of the initialiser
    /// rather than of a mechanism. What this prints is the histogram over time, because the question
    /// <c>plans/0046</c> asks is whether the founding generation's cohort ever blurs.
    /// </remarks>
    Stages,
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
/// <b>Hand-rolled rather than a parsing library.</b> Below the threshold <c>adr/0018</c> is aimed
/// at: a flat list of flags, no subcommands, and no dependency worth carrying into the one project
/// whose job is to prove it builds with nothing installed. If the surface grows subcommands, take
/// the library.
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

    /// <summary>
    /// How many service Buildings <c>--school</c> places before it runs. <b>Zero is the point of the
    /// knob.</b>
    /// </summary>
    /// <remarks>
    /// <b>An instrument setting and not a design number</b> (<c>adr/0164</c>): no Ruleset key hangs
    /// on it and no ratifier is owed. ⚠ <b><c>--schools 0</c> is what shows the failure half.</b> A
    /// city that declares schools and has built none fails every occasion every Day, and that
    /// branch cannot be reached from a Ruleset — a Ruleset says what a school <em>is</em>, and how
    /// many stand is a fact about the city the player built. ***A demonstration of an accumulator
    /// has to be able to fail***, which is <c>rulesets/hungry.toml</c>'s header, and this is where
    /// that half lives.
    /// </remarks>
    public int Schools { get; private init; } = DefaultSchools;

    /// <summary>
    /// What <c>--schools</c> places when nobody says. <b>Deliberately too few to cover a city.</b>
    /// </summary>
    private const int DefaultSchools = 4;

    /// <summary>How many frames <c>--watch</c> prints across its run.</summary>
    public int Frames { get; private init; } = DefaultFrames;

    /// <summary>What <c>--frames</c> prints when nobody says.</summary>
    private const int DefaultFrames = 8;

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
    /// Print the census ring as a time series as well as a summary.
    /// </summary>
    /// <remarks>
    /// <b>A flag rather than a mode of its own, because it builds no world.</b> Every picture mode
    /// populates a city of its own to take a picture of; this one is a second rendering of a run that
    /// is already happening, so it implies <c>--census</c> and rides the same ring. Its own flag
    /// rather than folded into that one because it is hundreds of lines: <c>--census</c> answers
    /// <em>did this trend</em> in four numbers a metric, and this answers <em>when</em> at the cost of
    /// a page a family.
    /// </remarks>
    public bool Series { get; private init; }

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

    /// <summary>
    /// Where to write a save of the world at the end of the run, or null to write none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A flag rather than a mode of its own, on <c>--series</c>' criterion and not on a judgement
    /// call.</b> Every mode in this runner builds a city of its own to photograph; this one rides the
    /// run that is already happening, which is the stated reason <c>--series</c> is a flag while
    /// <c>--traffic</c> is a mode.
    /// </para>
    /// <para>
    /// <b>It prints a round trip rather than only writing a file, and the round trip is the
    /// picture.</b> <c>05 §7</c>'s <em>replay from save</em> is a claim that a resumed city is the
    /// same city, and a file on disk is not evidence of that — so the runner reloads what it just
    /// wrote, runs the saved world and the unbroken one on for the same stretch, and prints the two
    /// hash traces side by side. ***A save that is never loaded demonstrates nothing***, which is the
    /// same reason <c>--traffic</c> steps its city twice.
    /// </para>
    /// </remarks>
    public string? SavePath { get; private init; }

    /// <summary>
    /// A save to resume, or null to start from a log or a fresh world.
    /// </summary>
    /// <remarks>
    /// <b>This is the half that makes a save worth writing</b>, and it is a separate invocation
    /// rather than a phase of one: the file outlives the process, which is the only property a save
    /// has that an in-memory copy does not. The run resumes at the Tick the save was taken at and
    /// steps on with **no commands**, because a save records a world and not the session that made
    /// it — see the refusal of <c>--load</c> beside <c>--log</c>.
    /// </remarks>
    public string? LoadPath { get; private init; }

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
        string? save = null;
        string? load = null;
        ulong seed = 0;
        int citizens = DefaultPopulation;
        ulong ticks = 1_024;
        int hashEvery = 64;
        bool force = false;
        bool census = false;
        bool series = false;
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
        bool day = false;
        bool evidence = false;
        bool money = false;
        bool parking = false;
        bool landValue = false;
        bool arrivals = false;
        bool flood = false;
        bool stages = false;
        bool school = false;
        int schools = DefaultSchools;
        bool watch = false;
        int frames = DefaultFrames;
        bool business = false;
        bool market = false;
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

                // Implies --census rather than refusing without it. The ring is the census, so asking
                // for the series and not the census is asking for a rendering of a thing that was
                // never collected -- a refusal an operator could only satisfy one way, which makes it
                // an obstacle rather than a check.
                case "--series":
                    series = true;
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

                // A session flag for --traffic's reason: a trail records what has happened, so a
                // world that has not stepped has nothing in one.
                case "--evidence":
                    evidence = true;
                    session = true;
                    continue;

                // A session flag for --evidence's reason, and the strongest case of it: employment is
                // assigned on a cadence, so a Day traced from Tick 0 follows somebody with no job.
                case "--day":
                    day = true;
                    session = true;
                    continue;

                // A session flag for --evidence's reason, one step further on: a level needs a world
                // that has been populated and a flow needs one that has been stepped, and this dump
                // prints both.
                case "--money":
                    money = true;
                    session = true;
                    continue;

                // A session flag for the same reason again, and the most literal case of it: a car is
                // parked somewhere only after somebody has driven there, so a world that has not
                // stepped has every space empty and every walk unmeasured.
                case "--parking":
                    parking = true;
                    session = true;
                    continue;

                // A session flag, and the most literal case of it after --parking: land value is a
                // quantity with MEMORY, so a world that has not run holds the zero it was created
                // with and a dump of it would be a picture of the initialiser.
                case "--land-value":
                    landValue = true;
                    session = true;
                    continue;

                // A session flag on --land-value's reasoning and one step further: arrivals are not
                // merely a quantity with memory, they are a quantity nothing produces without being
                // asked. This mode drives the gates itself.
                case "--arrivals":
                    arrivals = true;
                    session = true;
                    continue;

                // A session flag on --arrivals' reasoning: a flood over a world that has not run is
                // a flood over an empty map, and the whole question is what it reaches.
                case "--flood":
                    flood = true;
                    session = true;
                    continue;

                // A session flag on --land-value's reasoning: a Life Stage histogram taken off a
                // world that has not run is a picture of SyntheticCity's own round-robin, which is
                // the initialiser and not the city. What makes it a reading is elapsed Days.
                case "--stages":
                    stages = true;
                    session = true;
                    continue;

                // A session flag on --stages' reasoning and then some: the schools are placed before
                // the run, but the reading is a per-Day FLOW off ServiceEngine, which is zero on
                // every Tick that is not a Day boundary. A snapshot of an unstepped world would find
                // the counters at their initial zero and report a city nobody had asked anything of.
                case "--school":
                    school = true;
                    session = true;
                    continue;

                // A session flag for the plainest of the reasons: a frame of an unstepped world
                // shows the city SyntheticCity laid and nobody moving over it, and the moving is
                // the whole point of a frame.
                case "--watch":
                    watch = true;
                    session = true;
                    continue;

                // A session flag for --money's reason rather than --arrivals': a Business is created
                // by the city on its own and nothing outside has to ask. What it needs is elapsed
                // time, because construction, founding and placement are all paced.
                case "--business":
                    business = true;
                    session = true;
                    continue;

                // A session flag for --business's reason, and it needs MORE elapsed time than any of
                // them: a market row waits on the watershed, a seller waits on a Zone Rule, and a
                // Household short of money has to have had time to spend what it opened with.
                case "--market":
                    market = true;
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

                // Both imply a session for --ticks' reason: a save is taken at a Tick and a load
                // resumes at one, so neither means anything to the report.
                case "--save":
                    save = value;
                    session = true;
                    break;

                case "--load":
                    load = value;
                    session = true;
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

                // A count and not a flag, and zero is admitted where --citizens refuses it: a city
                // with no Citizens has nothing to report, and a city with no schools is the whole
                // failure half of what --school exists to show.
                case "--schools":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture,
                            out schools) || schools < 0)
                    {
                        complaint = $"--schools {value} is not a count of zero or more.";
                        return false;
                    }

                    break;

                case "--frames":
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture,
                            out frames) || frames <= 0)
                    {
                        complaint = $"--frames {value} is not a count of one or more.";
                        return false;
                    }

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
            // ⚠ THE SECOND SENTENCE USED TO READ "because no session can place a source until Rules
            // exist" AND IT IS STALE (adr/0093, and it is wrong about the TRIGGER rather than the
            // mechanism). Rules exist, and rulesets/fouled.toml is a Ruleset whose Rules emit -- so a
            // session CAN place a source, and --land-value is the mode that steps one. What survives
            // is the narrower true thing: this dump wants a field it authored, so that the halo it
            // prints is attributable to the one source it added.
            complaint = "--layer and the session flags disagree: a Layer dump builds its own world "
                      + "with sources it placed itself, so that the halo it prints is attributable "
                      + "to the one source it added. For a Layer on a world that has RUN, ask for "
                      + "--land-value with a Ruleset whose Rules emit.";
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

        // Ahead of --traffic's for the reason --traffic's is ahead of --commute's: the most specific
        // complaint wins, and every flag below is also a picture that builds its own world.
        //
        // ⚠ THIS BLOCK RUNS FIRST AND RETURNS, so it names every other picture and no block below
        // needs editing to admit it. That is why --parking is named here and --money is not named in
        // --parking's block below: the pair is refused here or not at all. A mode added in the
        // middle has to be threaded into every block above it; one added at the top does not.
        //
        // ⚠ --day is named here and still has no block of its own. That is a gap this one does not
        // close: the pair `--day --market` still parses.
        if (school && (stages || day || money || market || business || arrivals || landValue
                       || parking || evidence || traffic || commute || zones || roads || trips
                       || dump is not null))
        {
            complaint = "--school asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (stages && (day || money || market || business || arrivals || landValue || parking
                       || evidence || traffic || commute || zones || roads || trips
                       || dump is not null))
        {
            complaint = "--stages asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (money && (landValue || parking || evidence || traffic || commute || zones || roads || trips
                      || dump is not null))
        {
            complaint = "--money asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        // Ahead of --traffic's on the ordering rule stated below. A picture flag that loses an
        // argument it never announced is worse than a refused one -- the operator reads the other
        // picture's output as the one they asked for. plans/0012.
        //
        // ⚠ THE COMPLAINTS BELOW COUNT THE PICTURES AND THE ONES HERE DO NOT: a count in prose is a
        // fact that drifts. The old ones stay because a test asserts one of the strings.
        // Above --parking's on the ordering rule stated above: this block runs first and returns, so
        // `--land-value --parking` is refused here or not at all.
        // Above --land-value's on the ordering rule stated above: this block runs first and returns.
        if (arrivals && (landValue || parking || evidence || traffic || commute || zones || roads
                         || trips || dump is not null))
        {
            complaint = "--arrivals asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (arrivals && rulesets.Count == 0)
        {
            complaint = "--arrivals needs --ruleset PATH. Nothing arrives unless a kind declares "
                      + "arrivals_per_day, so a world with no Ruleset has no door and every panel "
                      + "would be blank. rulesets/crowded.toml is the file this mode was written "
                      + "for, and rulesets/bordered.toml is the same world at the designer's own "
                      + "numbers.";
            return false;
        }

        if (arrivals && log is not null)
        {
            complaint = "--arrivals and --log disagree: this mode issues its own arrive commands, so "
                      + "a recorded session would be replayed and then driven on top of. If you want "
                      + "a scripted arrival, write the verb into the log and use --log alone.";
            return false;
        }

        if (market && (business || arrivals || landValue || parking || evidence || traffic || commute
                       || zones || roads || trips || dump is not null))
        {
            complaint = "--market asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (market && rulesets.Count == 0)
        {
            complaint = "--market needs --ruleset PATH. A market is content three times over: a "
                      + "[districts] table for the rows to hang on, a [market] table for the price "
                      + "to be allowed to move, and a kind holding a Good in a business-owned Bin so "
                      + "that somebody is selling. rulesets/provisioned.toml is the file this mode "
                      + "was written for -- it is the only shipped world in which a Building sells.";
            return false;
        }

        // --money's refusal for --money's reason: this dump populates and steps a world of its own.
        if (market && log is not null)
        {
            complaint = "--market and --log disagree: the dump populates its own world and steps it, "
                      + "so a recorded session would be replayed and then over-populated.";
            return false;
        }

        if (business && (arrivals || landValue || parking || evidence || traffic || commute || zones
                         || roads || trips || dump is not null))
        {
            complaint = "--business asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (business && rulesets.Count == 0)
        {
            complaint = "--business needs --ruleset PATH. Nothing creates a Business unless a kind "
                      + "declares a trade or the file states a [founding] channel, so a world with "
                      + "no Ruleset has no economic actor and every panel would be blank. "
                      + "rulesets/levied.toml is the file this mode was written for -- it is the "
                      + "only shipped world in which a Business is created, funded, staffed and "
                      + "read by a Rule.";
            return false;
        }

        if (landValue && (parking || evidence || traffic || commute || zones || roads || trips
                          || dump is not null))
        {
            complaint = "--land-value asks for another picture, and each picture builds its own "
                      + "world. Ask for one.";
            return false;
        }

        if (parking && (evidence || traffic || commute || zones || roads || trips || dump is not null))
        {
            complaint = "--parking asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
            return false;
        }

        if (evidence && (traffic || commute || zones || roads || trips || dump is not null))
        {
            complaint = "--evidence asks for another picture, and each picture builds its own world. "
                      + "Ask for one.";
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

        // --zones' refusal, and the polarity is the same one: decline is content. A file with no
        // [[zone_rule]] never looks at a Lot and a file whose kinds set no condemn_after never
        // condemns one, so the trail would be empty -- and an empty accumulator reads as a broken
        // mechanism rather than as a Ruleset that declines nothing. The two Ruleset-level checks are
        // EvidenceDump's, because they need the file loaded; this only says a file is needed at all.
        if (evidence && rulesets.Count == 0)
        {
            complaint = "--evidence needs --ruleset PATH. Decline is content: without a "
                      + "[[zone_rule]] and a condemn_after nothing is ever condemned, and an empty "
                      + "trail would look like a defect rather than like a Ruleset that declines "
                      + "nothing. rulesets/diagnosed.toml is the file written for this picture.";
            return false;
        }

        // The polarity is --zones' rather than --evidence's, and the difference is that this absence
        // is not legible. An empty trail under a heading naming the file that fills it is milestone
        // 6's legible absence; a balance sheet of six zeroes says "supply == held" and is TRUE, so
        // the reader learns that money is conserved in a city that has none. The Ruleset-level check
        // that the Policies move something conserved is MoneyDump's, because it needs the file
        // loaded; this only says a file is needed at all.
        if (money && rulesets.Count == 0)
        {
            complaint = "--money needs --ruleset PATH. A circuit is content: without a [[policy]] "
                      + "moving a family = \"money\" Resource nothing ever moves, and a balance "
                      + "sheet over a city of paupers holds vacuously and prints as though it held. "
                      + "rulesets/taxed.toml is the file written for this picture.";
            return false;
        }

        // --evidence's refusal for --evidence's reason.
        if (money && log is not null)
        {
            complaint = "--money and --log disagree: the dump populates its own world and steps it, "
                      + "so a recorded session would be replayed and then over-populated.";
            return false;
        }

        // --commute's and --traffic's refusal, for their reason.
        if (evidence && log is not null)
        {
            complaint = "--evidence and --log disagree: the dump populates its own world and steps "
                      + "it, so a recorded session would be replayed and then over-populated.";
            return false;
        }

        // --traffic's refusal and its polarity: a picture of parking needs content, and BOTH halves
        // are content -- supply nobody can find and drivers nobody has are different empty pictures.
        // ParkingDump makes the two Ruleset-level checks because they need the file loaded; this only
        // says a file is needed at all.
        // --parking's refusal and its polarity: a picture of a field needs a field. LandValueDump
        // makes the sharper check -- that some Rule actually emits -- because that needs the file
        // loaded; this only says a file is needed at all.
        if (landValue && rulesets.Count == 0)
        {
            complaint = "--land-value needs --ruleset PATH. The only thing in the build that creates "
                      + "a Cell row is a pollution emission, so a world with no Rules has no Cells "
                      + "and every panel would be blank. rulesets/fouled.toml is the only shipped "
                      + "file whose Rules emit.";
            return false;
        }

        if (landValue && log is not null)
        {
            complaint = "--land-value and --log disagree: the dump populates its own world and steps "
                      + "it, so a recorded session would be replayed and then over-populated.";
            return false;
        }

        if (parking && rulesets.Count == 0)
        {
            complaint = "--parking needs --ruleset PATH. Parking is content twice over: [parking] "
                      + "states the shed a driver queries and [households] states whether anybody "
                      + "keeps a car, and a file missing either parks nobody. "
                      + "rulesets/congested.toml is the only shipped file that states both.";
            return false;
        }

        if (parking && log is not null)
        {
            complaint = "--parking and --log disagree: the dump populates its own world and steps "
                      + "it, so a recorded session would be replayed and then over-populated.";
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

        // A census rides a run, and the pictures are not runs even when they step a world: each of
        // them populates its own city and never reaches Session, so these two flags were ACCEPTED AND
        // SILENTLY IGNORED under --zones, --commute and --traffic. A flag that does nothing is worse
        // than one that is refused, because the operator reads the absence of a census as a census
        // with nothing in it. Found while adding --series; the hole was --census's since slice 10.
        if ((census || series)
            && (zones || commute || traffic || evidence || money || parking || roads || trips
                || market || dump is not null))
        {
            string asked = series ? "--series" : "--census";

            complaint = $"{asked} and the picture modes disagree: each picture populates a world of "
                      + "its own and prints it, and none of them keeps a census — so this flag would "
                      + "have been accepted and then ignored. Ask for a run, or ask for the picture.";
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

        // A save is the run's output and a load is its input, and one invocation that did both would
        // produce a trace that is neither -- it would save a world it had itself resumed, which is a
        // round trip written the long way round and with no control to compare against. --save
        // already runs the round trip internally, which is the thing somebody asking for both wants.
        if (save is not null && load is not null)
        {
            complaint = "--save and --load disagree: one is what a run produces and the other is "
                      + "what it starts from. --save already reloads what it wrote and prints both "
                      + "traces, so ask for that; --load is for resuming a save in a later run.";
            return false;
        }

        // A save has no schema of its own -- it is the field declaration, dumped (adr/0086) -- so the
        // Rules are not in the file and cannot be guessed. This is Zones' polarity rather than a
        // convenience: a world loaded under no Rules is inert, which reads as a broken save.
        if (load is not null && rulesets.Count == 0)
        {
            complaint = "--load needs --ruleset PATH. A save carries no Rules: adr/0086 makes the "
                      + "file the field declaration dumped, with no schema of its own, so the Rules "
                      + "a loaded city runs under are the ones you name here.";
            return false;
        }

        // A save records the world and not the session that produced it, so there is nothing in it a
        // log could be checked against. Permitting both would mean replaying one session's commands
        // into another session's world from the Tick the save happens to sit at, and no divergence
        // in that run could be attributed to either.
        if (load is not null && log is not null)
        {
            complaint = "--load and --log disagree: a save is a world at a Tick and a log is the "
                      + "session that made one, and a save records nothing a log could be matched "
                      + "against. Resume the save, or replay the log.";
            return false;
        }

        // The picture is a round trip, so it needs a city with something in it to round-trip. This is
        // the same refusal --zones, --roads, --trips, --commute and --traffic each make: the content
        // is the Ruleset's, and a save of an inert world agrees with itself and demonstrates nothing.
        if (save is not null && rulesets.Count == 0)
        {
            complaint = "--save needs --ruleset PATH. The round trip it prints is only evidence if "
                      + "the city changed between the two traces, and a world with no Rules does "
                      + "nothing between Ticks -- so the two would agree on an empty city.";
            return false;
        }

        options = new Options
        {
            Mode = day ? Mode.Day
                 : watch ? Mode.Watch
                 : school ? Mode.School
                 : stages ? Mode.Stages
                 : market ? Mode.Market
                 : business ? Mode.Business
                 : flood ? Mode.Flood
                 : arrivals ? Mode.Arrivals
                 : money ? Mode.Money
                 : landValue ? Mode.LandValue
                 : parking ? Mode.Parking
                 : evidence ? Mode.Evidence
                 : traffic ? Mode.Traffic
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
            Schools = schools,
            Frames = frames,
            Ticks = ticks,
            HashEvery = hashEvery,
            ForceRuleset = force,
            Census = census,
            Series = series,
            CrashPath = crash,
            DecideGuard = decideGuard,
            SavePath = save,
            LoadPath = load,
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
          --series              print the same readings AGAINST THE TICK -- one block
                                per family, one row per reading -- as well as the
                                summary. Implies --census. A column that never moves
                                becomes a footnote naming it and its value. Hundreds
                                of lines: --census says whether something trended,
                                this says when
          --crash PATH          where to write the crash artifact if the run panics.
                                One is always written; this only names where
          --no-decide-guard     stop proving every Tick that Phase 2 wrote nothing.
                                The proof is O(world) per Tick; turn it off for a
                                long run at scale and leave it on everywhere else
          --save PATH           save the world at the end of the run, then RELOAD it
                                and run both on, printing the two hash traces side
                                by side. A save that is never loaded demonstrates
                                nothing, so the round trip is what this prints.
                                Needs --ruleset
          --load PATH           resume a save written by --save and run --ticks more.
                                Starts at the Tick the save was taken at, with no
                                commands: a save is a world, not a session. Needs
                                --ruleset, and refuses --log
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
          --money               dump the circular flow: where the city's money is and what
                                moved it there. Two blocks -- the balance sheet, with the
                                money supply and the treasury on separate rows because
                                01 section 5.1 makes them different bills, and the circuit,
                                one row per sweep round with both directions unnetted.
                                Steps its own world. Needs --ruleset, and refuses one whose
                                Policies move nothing conserved: a balance sheet over a city
                                with no money says "conserved" and means nothing by it.
                                rulesets/taxed.toml is the file written for it
          --day                 follow ONE Citizen through one Day, Tick by Tick --
                                where they went, when, and what it cost them. Every
                                other mode aggregates; this one is a person. Needs
                                --ruleset with a [jobs] table, and runs --ticks Ticks
                                first so somebody has a job to go to. Read the footer:
                                it names the mechanisms that do not exist yet, and a
                                thin Day is the finding rather than a broken dump
          --evidence            dump what the city can say about why something happened to
                                it: the condemnation trail with its aggregate expanded,
                                one Building's answer assembled from live state, and why
                                the vacant Lots are vacant. Needs --ruleset that declines
                                something, and runs a session because a trail records what
                                has happened. Run it past 2048 Ticks or the trail has not
                                filled and there is no aggregate to expand
          --parking             dump where people parked against where they were going: the
                                walk from the car and the walk to it, as distributions, and
                                the supply those walks were spent on. NOT a grid -- capacity
                                is per building kind, so a map of occupied spaces is the map
                                --zones already draws. Needs --ruleset with a [parking] and
                                a [households] table, and runs a session because a car is
                                parked somewhere only after somebody has driven there
          --land-value          dump land value against the desirability it is chasing, and
                                the gap: three grids, because a lag is a property of a pair
                                and not of a value. Needs --ruleset whose Rules EMIT -- the
                                only thing that creates a Cell row is a pollution emission,
                                and rulesets/fouled.toml is the only shipped file that does.
                                Prints the hour, because desirability's noise term reads a
                                Segment's volume at the instant it is asked
          --arrivals            dump who came through the gates, who is waiting, who gave
                                up and what the money did. Needs --ruleset declaring a
                                kind with arrivals_per_day -- rulesets/crowded.toml is
                                the file it was written for. THE ONLY MODE THAT ISSUES
                                COMMANDS: nothing decides to arrive until milestone 16,
                                so it knocks on every gate once a Day, asking for more
                                than the door can take, and what is admitted is the
                                file's ceiling rather than a rate chosen by the runner
          --flood               dump the floods: where the world seeded each one, how far it
                                got, and how many Buildings it ruined and swept. Needs
                                --ruleset stating [disasters], which rulesets/flooded.toml
                                is the only shipped file to do. A flood that touched
                                nothing is printed like one that took a district -- 01
                                §5.3, and it is the game telling a player that a siting
                                decision was right. Prints where the floodplain actually
                                is, because a city that is not sited on one never meets a
                                flood and the run looks like a broken mechanism
          --business            dump the economic actor: how many Businesses there are and
                                where, what created them, what they hold, who works in
                                them, and what read one. Needs --ruleset in which a
                                Business can be CREATED -- a kind declaring a trade, or a
                                [founding] channel -- which is a different test from one
                                that merely declares a [[business]]. rulesets/levied.toml
                                is the only shipped world with all four quarters in it
          --market              dump the District Pool: what each (District, Good) row holds,
                                what its price did over the run, and WHO COULD NOT AFFORD IT,
                                which is the only one of the three that shows the market
                                having a consequence for somebody. The stock column sums the
                                SELLERS' Bins, because a Pool is a market and not a store.
                                Needs --ruleset stating [districts] and [market] AND giving
                                some kind a Good in a business-owned Bin -- declaring a
                                [[business]] trade is a different test, and twinned.toml
                                passes it while selling nothing. rulesets/provisioned.toml
                                is the file this mode was written for
          --stages              dump the city's age structure Day by Day: who is in which
                                Life Stage, how many moved, and whether the founding
                                generation's cohort ever blurs. The population column is
                                expected to be FLAT -- plans/0046 stage 1 advances a stage
                                and does nothing else, so dissolution and generation are
                                later stages and a city that grew here would be a defect.
                                Needs --ruleset stating [[life_stage]]; rulesets/aged.toml
                                is the only shipped file that does
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
