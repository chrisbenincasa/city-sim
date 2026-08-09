using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Core;

/// <summary>
/// <c>step(inputs)</c>: one world, one Tick counter, and the eight phases in order.
/// </summary>
/// <remarks>
/// <para>
/// <b>The simulation is a pure function of its inputs</b> (<c>02 §1</c>). It has no concept of
/// wall-clock time, no camera, no renderer and no filesystem, and the host decides when to advance
/// the Tick. That is what makes fast-forward, headless testing and replay free rather than features
/// somebody has to build.
/// </para>
/// <para>
/// <b>This is separate from <see cref="Entities.World"/> because storage and time are separate
/// things.</b> The World is the tables and the cross-table operations that keep them consistent; the
/// Simulation is what advances them. Keeping the Tick counter out of the World also keeps it out of
/// the State Hash's composition, where it does not belong: two worlds identical in every row are
/// identical worlds, and the Tick they were reached on is a property of the run.
/// </para>
/// <para>
/// <b>Which thread runs this is deliberately not decided here.</b> <c>05 §6</c> never says, and
/// <c>adr/0037</c> made it consequential by leaving one live state. Phase 1 does not need the answer —
/// it is single-threaded and headless — and the shape of this class does not foreclose it: nothing
/// here owns a thread, so the recommendation on the table (the simulation owns a thread, and
/// <c>threads=1</c> means it runs on the caller's) remains available.
/// </para>
/// </remarks>
public sealed class Simulation
{
    private readonly World _world;
    private readonly WorldKey _key;
    private readonly RuleEngine _rules;
    private readonly ZoneRuleEngine _zoning;

    private ulong _tick;
    private TickPhase _phase = TickPhase.Commit;

    /// <param name="world">The tables this advances. Not copied; the Simulation does not own it.</param>
    /// <param name="key">The world seed, already folded. The first coordinate of every draw.</param>
    public Simulation(World world, WorldKey key)
    {
        ArgumentNullException.ThrowIfNull(world);

        _world = world;
        _key = key;
        _rules = new RuleEngine(world, key);
        _zoning = new ZoneRuleEngine(world, key);
    }

    /// <summary>The tables this Simulation advances.</summary>
    public World World => _world;

    /// <summary>The Bin Rule interpreter: Phase 1 collects, Phase 2 evaluates, Phase 3 applies.</summary>
    /// <remarks>
    /// <b>Owned here rather than by the <see cref="Entities.World"/>, unlike the invariant registry.</b>
    /// The registry holds claims about a world and a hand-built test world has the same ones; the
    /// engine holds a Tick's worth of scratch, which only means anything inside a Tick. Putting it on
    /// the World would have made the buffers look like state, which is precisely what they must not be.
    /// </remarks>
    public RuleEngine Rules => _rules;

    /// <summary>The Sweep Rule interpreter: Zone Rules trigger, sample and act, all in phase 6.</summary>
    /// <remarks>
    /// <b>A second engine rather than a mode of the first, which is <c>adr/0033</c> honoured in the
    /// code layout.</b> The two families share the word <em>Rule</em> and nothing else structural: one
    /// is woken by the Event Wheel and settled against contention, the other is polled on an interval
    /// and acts where it runs. A single class that branched on family would make moving a mechanism
    /// between them look like a flag, which is exactly what the ADR says it is not.
    /// </remarks>
    public ZoneRuleEngine Zoning => _zoning;

    /// <summary>The world seed, as <see cref="Randomness.Draw"/>'s first coordinate.</summary>
    public WorldKey Key => _key;

    /// <summary>
    /// The Tick about to run. Starts at zero and advances once per <see cref="Step"/>.
    /// </summary>
    /// <remarks>
    /// An unsigned counter, per <c>CONTEXT.md</c>. At the reference rate of 16 Ticks/s a
    /// <see cref="ulong"/> outlasts any session by a margin with no useful name, so nothing in the
    /// core checks it for overflow.
    /// </remarks>
    public Ticks Tick => new(_tick);

    /// <summary>The phase last entered. For the crash artifact, which reports where a panic landed.</summary>
    public TickPhase Phase => _phase;

    /// <summary>
    /// Whether to prove, every Tick, that <see cref="TickPhase.Decide"/> wrote nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>On by default, and a runtime switch rather than a build configuration.</b> The obvious
    /// implementation is <c>#if DEBUG</c>, and it is backwards for the same reason <c>02 §10</c> gives
    /// about the invariant tiers: the runs that surface this class of bug are headless balance runs,
    /// millions of Ticks long, in <b>release</b>. A guard that compiles out of the build where the bug
    /// appears is a guard aimed at the wrong build.
    /// </para>
    /// <para>
    /// <b>What it costs is why it is a switch at all.</b> The check folds every column of every table
    /// twice per Tick, so it is <c>O(world)</c> against a phase that is meant to be <c>O(woken)</c> —
    /// affordable for a correctness run and not for a long one. Turn it off for the 100,000-Tick test
    /// and leave it on everywhere else.
    /// </para>
    /// </remarks>
    public bool VerifyDecideWritesNothing { get; set; } = true;

    /// <summary>
    /// Advances the world by one Tick, running all eight phases in order.
    /// </summary>
    /// <remarks>
    /// <b>The ordering is the determinism contract</b> (<c>02 §1.1</c>), which is why the calls are
    /// written out straight rather than driven from a table that something could reorder at runtime.
    /// </remarks>
    public void Step(in TickInput input)
    {
        Ticks tick = new(_tick);

        ApplyInput(input, tick);
        Wake(tick);
        Decide(tick);
        Settle(tick);
        Move(tick);
        Layers(tick);
        Growth(tick);
        Commit(tick);

        _tick++;
    }

    /// <summary>Phase 0 — apply the player's commands.</summary>
    /// <remarks>
    /// <b>Serial, and the only door into the simulation.</b> Everything that affects simulation state
    /// enters here, which is what makes the Input Log a complete description of a session. A mechanism
    /// that reaches into the world from outside a Tick is not merely untidy — it is a state change no
    /// replay can reproduce and no State Hash divergence can explain.
    /// </remarks>
    private void ApplyInput(in TickInput input, Ticks tick)
    {
        _phase = TickPhase.Input;

        foreach (Command command in input.Commands)
        {
            Apply(command, tick);
        }
    }

    /// <summary>Applies one command. The verb table of <c>01 §2</c>, as far as it is built.</summary>
    /// <remarks>
    /// <b>An unknown verb throws rather than being skipped.</b> Skipping it would produce a run that
    /// diverges from the log that describes it while reporting success, which is the one outcome this
    /// slice exists to make impossible.
    /// </remarks>
    private void Apply(Command command, Ticks tick)
    {
        _ = tick;

        switch (command.Kind)
        {
            case CommandKind.Zone:
                // The permission set now arrives at full width — slice 10 widened the Lot's column to
                // match this verb, so nothing authored here is lost on the way in.
                //
                // It still paints exactly one Lot, and that is not a shortcut: 02 §2.2 says Lots are
                // **generated, not painted**, by a subdivider that carves zoned land against the
                // Street network and refuses frontage-less land. Streets are milestone 5a, which is
                // Phase 2, so the generator cannot exist yet. Painting a *region* of Lots here would
                // stand in for more of 5a than painting one does, and every Lot it invented would be
                // one the real subdivider would have refused.
                _world.Lots.Create(command.East, command.North, command.Zone);
                break;

            case CommandKind.Populate:
                // Spike S0's verb, and the only one here that is an instrument rather than a player's.
                // It is applied like any other because that is the point of it: a population that
                // arrived any other way would be outside the log that claims to describe the session.
                SyntheticCity.PopulateInto(_world, _key, tick);
                break;

            case CommandKind.None:
            case CommandKind.Connect:
            case CommandKind.Service:
            case CommandKind.Govern:
            default:
                throw new InvalidOperationException(
                    $"command kind {(ushort)command.Kind} is declared but not applied in this slice.");
        }
    }

    /// <summary>Phase 1 — drain the Event Wheel bucket for this Tick.</summary>
    /// <remarks>
    /// <b>Serial, and the only phase that takes rows off the Wheel.</b> Slice 7's wheel carries Rule
    /// Instances; slice 9 generalises it to everything else that sleeps, and every Citizen already
    /// carries the <c>next_event_tick</c> it will be bucketed by, declared in slice 4.
    /// </remarks>
    private void Wake(Ticks tick)
    {
        _phase = TickPhase.Wake;

        _rules.CollectDue(tick);
    }

    /// <summary>
    /// Phase 2 — evaluate Rules and Needs against the Past, emitting intents and writing nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Writing nothing is the load-bearing property</b> (<c>adr/0037</c>): it is what permits every
    /// entity table to be single-buffered, so the Past is <em>the state as of the start of this
    /// Tick</em> rather than a second copy of the world. The guard below is that claim checked instead
    /// of trusted — see <see cref="VerifyDecideWritesNothing"/>.
    /// </para>
    /// <para>
    /// <b>The intents it will emit are not table state</b>, so they are outside what the guard folds.
    /// That is the correct boundary: an intent is a proposal that Phase 3 may reject, and a phase that
    /// could not write its own proposals could not propose anything.
    /// </para>
    /// </remarks>
    private void Decide(Ticks tick)
    {
        _phase = TickPhase.Decide;

        if (!VerifyDecideWritesNothing)
        {
            DecideCore(tick);
            return;
        }

        ulong before = FoldEverything();
        DecideCore(tick);
        ulong after = FoldEverything();

        if (before != after)
        {
            throw new InvalidOperationException(
                $"Decide wrote to the world on Tick {tick.Raw}. Phase 2 is read-only (adr/0037): "
                + "every entity table is single-buffered because of it.");
        }
    }

    /// <summary>The read-only half of Phase 2: evaluate every Rule the Wheel handed us.</summary>
    private void DecideCore(Ticks tick) => _rules.Evaluate(tick);

    /// <summary>Phase 3 — apply intents in shuffle order, re-checking atomicity.</summary>
    /// <remarks>
    /// Serial and sorted. Contested outcomes are settled by a counter-based shuffle rather than by
    /// arrival or by entity id (<c>02 §8</c> rule 5), because ordering by id is <em>biased</em> — the
    /// same Building would win every contested draw for the life of the city.
    /// </remarks>
    private void Settle(Ticks tick)
    {
        _phase = TickPhase.Settle;

        _rules.Apply(tick);
    }

    /// <summary>Phase 4 — Lanes advance Vehicles; Statistical trips check arrival.</summary>
    /// <remarks>
    /// Permitted parallel, and one of <c>adr/0037</c>'s two double-buffered tables when it exists,
    /// because it is a parallel phase that both reads and writes. Empty until Phase 2 of the roadmap.
    /// </remarks>
    private void Move(Ticks tick)
    {
        _ = tick;
        _phase = TickPhase.Move;
    }

    /// <summary>Phase 5 — Map Layer diffusion for whatever is scheduled this Tick.</summary>
    /// <remarks>
    /// <para>
    /// Permitted parallel, and <c>adr/0037</c>'s other double-buffered table — the first one in the
    /// project to declare <c>Buffering.TwoCopies</c>. This build runs it serially, which
    /// <c>Phases.Runs</c> states: permission is an upper bound.
    /// </para>
    /// <para>
    /// <b>It costs nothing on the overwhelming majority of Ticks, by construction.</b> The schedule is
    /// staggered — pollution on <c>tick % 64 == 0</c>, land value every 256 offset by 16 — so 63 Ticks
    /// in 64 do no convolution at all, and the two Layers never land on the same Tick. That is
    /// <c>05 §9</c>'s third multiplier: a spike every 64 Ticks is a visible stutter, and the same work
    /// spread over those 64 Ticks is not.
    /// </para>
    /// <para>
    /// <b>The cadence is hash-bearing, which makes it the designer's number and not the profiler's</b>
    /// (<c>adr/0044</c>). It decides when a source becomes visible to a Rule that reads the Cell, so
    /// two worlds differing only in the period produce two hash traces — <c>05 §4</c>'s test, run
    /// rather than argued, because under <c>adr/0043</c> the claim was <em>measurable</em> and the
    /// machine that settles it is this one. It stays ordinary hot-reloadable Ruleset data, which is
    /// why the schedule arrives as a constructor argument of the <c>World</c> rather than a constant
    /// here: a number embedded in an <c>if</c> has no name, and a number with no name is one nobody
    /// runs the hash rule against.
    /// </para>
    /// </remarks>
    private void Layers(Ticks tick)
    {
        _phase = TickPhase.Layers;

        _world.Layers.Step(tick);
    }

    /// <summary>Phase 6 — Zone Rules sample Lots; Buildings with accumulated failure decline.</summary>
    /// <remarks>
    /// <para>
    /// Serial, and the whole of a Sweep Rule's Tick. <b>That it is one phase rather than three is the
    /// difference <c>adr/0033</c> is about</b>: a Bin Rule's proposal survives phase 2 only to be
    /// re-checked and possibly refused in phase 3, whereas a Zone Rule's effect exists the instant it
    /// acts. Nothing here proposes, so nothing here can be outbid.
    /// </para>
    /// <para>
    /// <b>It runs after phase 5 and that ordering is a decision.</b> A Zone Rule's predicates read
    /// Map Layer values, so growth this Tick sees the diffusion this Tick — a Building placed on a
    /// Cell whose pollution rose in phase 5 is judged against the risen figure and not the stale one.
    /// Reversing the two would make growth lag the environment by a whole Tick on the 1-in-64 Ticks a
    /// Layer moves, which is a difference no readout could explain.
    /// </para>
    /// </remarks>
    private void Growth(Ticks tick)
    {
        _phase = TickPhase.Growth;

        _zoning.Sweep(tick);
    }

    /// <summary>Phase 7 — schedule next events, re-evaluate Stress, emit the State Hash if due.</summary>
    /// <remarks>
    /// <b>This phase no longer swaps Past and Future</b>, which <c>02 §1.1</c>'s table still says it
    /// does. <c>adr/0037</c> deleted the full-world double buffer in favour of one live state, so
    /// there is nothing to swap; the Past is a property of the phase ordering rather than a copy.
    /// Emitting the hash is the caller's business in slice 5 — see <c>Replay</c> — because <em>if
    /// due</em> is a cadence the host chooses.
    /// </remarks>
    private void Commit(Ticks tick)
    {
        _phase = TickPhase.Commit;

        // 02 §10's staggered tier. Last in the Tick because it asserts about a settled world: run
        // before Growth and it would report a city mid-edit, which is a check that fires on correct
        // code and is therefore a check somebody eventually deletes.
        _world.Invariants.RunStaggered(_world, tick);
    }

    /// <summary>
    /// Runs <c>02 §10</c>'s end-of-run tier: the expensive whole-world walks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Called once, after the last Tick, by whoever owns the run.</b> It is <c>O(world)</c> and
    /// affordable precisely because there is one of it per run however long the run was — which is
    /// <c>adr/0033</c>'s shape, quoted by <c>02 §10</c>: <em>unaffordable per Tick and trivial at the
    /// end of a headless run.</em>
    /// </para>
    /// <para>
    /// <b>It is not called from <see cref="Step"/>, and that is the whole point of the tier.</b> A
    /// check that ran every Tick would have to be cheap, and these are the ones that are not.
    /// </para>
    /// </remarks>
    public void CheckEndOfRun() => _world.Invariants.RunEndOfRun(_world, new Ticks(_tick));

    /// <summary>
    /// Folds every column of every table — derived ones included — for the Decide guard.
    /// </summary>
    /// <remarks>
    /// <b>Deliberately not <see cref="Entities.World.HashState"/>.</b> The State Hash folds saved
    /// state only, by declaration, so a Decide that wrote to a derived column would pass a check built
    /// on it. That is exactly the hole slice 4 found in the other direction, where a derived list's
    /// ordering escaped the hash; the same blind spot, read from the other side.
    /// </remarks>
    private ulong FoldEverything()
    {
        ulong hash = 0;

        foreach (Rows table in _world.Tables)
        {
            table.FoldAll(ref hash);
        }

        return hash;
    }
}
