namespace Borough.Core;

/// <summary>
/// The eight phases of a Tick, in the order they run.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ordering is the determinism contract, not an implementation detail</b> (<c>02 §1.1</c>).
/// It is written out as named, ordered, mostly-empty methods before there is anything to put in them
/// so that every later mechanism arrives into a slot that already exists, rather than being appended
/// wherever it fit. A mechanism that lands in the wrong phase is a change to the city — see
/// <c>adr/0033</c>, which is the same argument one level down, about Rule families.
/// </para>
/// <para>
/// <b>The numbering is load-bearing and the names are not.</b> The value is the order; the identifier
/// is for the reader. <c>Borough.Core</c> returns this enum and never a formatted phase name, on
/// <c>adr/0002</c>'s rule that the shell owns every string a human reads.
/// </para>
/// </remarks>
public enum TickPhase : byte
{
    /// <summary>Apply player commands from the Input Log. Nothing about the camera.</summary>
    Input = 0,

    /// <summary>Drain the Event Wheel bucket for this Tick. Slice 9.</summary>
    Wake = 1,

    /// <summary>Evaluate Rules and Needs against the Past. Emits intents, never a mutation.</summary>
    Decide = 2,

    /// <summary>Apply intents in shuffle order. Re-check atomicity. Losers take their fallback.</summary>
    Settle = 3,

    /// <summary>Lanes advance Vehicles; Statistical trips check arrival.</summary>
    Move = 4,

    /// <summary>Map Layer diffusion for whatever is scheduled. Slice 6.</summary>
    Layers = 5,

    /// <summary>Zone Rules sample Lots; Buildings with accumulated failure decline. Slice 10.</summary>
    Growth = 6,

    /// <summary>Schedule next events, re-evaluate Stress, emit the State Hash if due.</summary>
    Commit = 7,
}

/// <summary>
/// What a phase is <em>allowed</em> to do concurrently.
/// </summary>
/// <remarks>
/// <b>This is permission, never implementation.</b> <c>02 §1.1</c> states what may be parallel;
/// <c>05 §6</c> states what is. In Phase 1 those differ — everything runs serially — and the two
/// documents do not currently say that they differ, which is a noted contradiction rather than a
/// settled position. See <see cref="Phases.Runs"/>.
/// </remarks>
public enum PhaseConcurrency : byte
{
    /// <summary>One thread, in a stated order.</summary>
    Serial = 0,

    /// <summary>May be spread across threads, and may write. Requires a double-buffered table.</summary>
    Parallel = 1,

    /// <summary>
    /// May be spread across threads and <b>writes nothing at all</b>.
    /// </summary>
    /// <remarks>
    /// <b>This is the load-bearing one</b> (<c>adr/0037</c>). Writing nothing is what permits every
    /// entity table to be single-buffered, so a later decision to parallelise <see cref="TickPhase.Decide"/>
    /// must not also make it mutating. <see cref="Simulation.VerifyDecideWritesNothing"/> is that claim
    /// checked rather than trusted.
    /// </remarks>
    ParallelReadOnly = 2,
}

/// <summary>
/// The phase table: what each phase may do, and what this Phase 1 build actually does.
/// </summary>
public static class Phases
{
    /// <summary>The number of phases in a Tick.</summary>
    public const int Count = 8;

    /// <summary><c>02 §1.1</c>'s concurrency column, in phase order.</summary>
    private static readonly PhaseConcurrency[] Permission =
    [
        PhaseConcurrency.Serial,            // 0 Input
        PhaseConcurrency.Serial,            // 1 Wake
        PhaseConcurrency.ParallelReadOnly,  // 2 Decide
        PhaseConcurrency.Serial,            // 3 Settle
        PhaseConcurrency.Parallel,          // 4 Move
        PhaseConcurrency.Parallel,          // 5 Layers
        PhaseConcurrency.Serial,            // 6 Growth
        PhaseConcurrency.Serial,            // 7 Commit
    ];

    /// <summary>What <c>02 §1.1</c> permits this phase to do.</summary>
    public static PhaseConcurrency Permits(TickPhase phase) => Permission[(int)phase];

    /// <summary>
    /// What this build actually does, which is everything serially.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Phase 1 is single-threaded and this method is why that is a statement rather than an
    /// accident.</b> A build that runs a phase serially while the design permits parallelism is
    /// correct — permission is an upper bound. A build that ran one in parallel where the design says
    /// serial would be a determinism defect, and the test that catches it compares these two
    /// functions rather than reading the schedule.
    /// </para>
    /// <para>
    /// <b><see cref="TickPhase.Decide"/> is the exception worth naming.</b> Running it serially does
    /// not relax its read-only obligation: the obligation exists so that the tables can stay
    /// single-buffered, and that is true at one thread as much as at eight. Dropping the guard because
    /// nothing is parallel yet would mean discovering in slice 7 that four Rule families had been
    /// quietly writing during Decide the whole time.
    /// </para>
    /// </remarks>
    public static PhaseConcurrency Runs(TickPhase phase) =>
        Permits(phase) == PhaseConcurrency.ParallelReadOnly
            ? PhaseConcurrency.ParallelReadOnly
            : PhaseConcurrency.Serial;
}
