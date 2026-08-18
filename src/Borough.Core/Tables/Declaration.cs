namespace Borough.Core.Tables;

/// <summary>
/// What a column is for: state the simulation owns, a cache it can rebuild, or scratch that means
/// nothing between the phases that use it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is adr/0003's field rule, and it is one enum rather than two flags on purpose.</b> The
/// rule reads <em>every field is declared once as either <c>(saved AND hashed)</c> or
/// <c>(derived AND rebuilt)</c></em>. Two independent booleans would permit the state the rule exists
/// to forbid — saved but not hashed — and that state has <b>no detector at all</b>: two runs diverge
/// on the field, the hashes agree, replay reports success, and the save/reload test passes because
/// the field <em>is</em> saved. The oracle certifies a divergence it cannot see.
/// </para>
/// <para>
/// With the two dispositions welded together, <b>save/reload equivalence transitively proves hash
/// completeness</b>. A field that is saved is hashed by construction, so the save/reload test cannot
/// pass while the hash is incomplete. That is why this is structural rather than a test: a test for
/// hash coverage has the same blind spot as the thing it tests.
/// </para>
/// <para>
/// <b><see cref="Scratch"/> is a third answer and not a third flag, and it does not reopen what the
/// welding closes.</b> The forbidden state is <em>saved but not hashed</em>; scratch is neither, so
/// the pair above stays welded and the enum stays one enum. What made the third value necessary is
/// that <see cref="Derived"/>'s contract is a <em>claim</em> — that the field is a pure function of
/// saved state — and a scratch intermediate makes no such claim, so declaring it
/// <see cref="Derived"/> is not a weaker version of that claim but a false one. The alternative was an
/// exemption list inside the rebuild audit, which is the arrangement <see cref="Reference"/> refused
/// one file below for the reason that outlives both: <b>a list of fields shares its blind spot with
/// the bug it exists to find.</b>
/// </para>
/// </remarks>
public enum Disposition
{
    /// <summary>
    /// State. Written to the save and folded into the State Hash — both, always, by construction.
    /// </summary>
    Saved = 0,

    /// <summary>
    /// A cache, rebuilt from <see cref="Saved"/> state after a load. Neither written nor hashed.
    /// </summary>
    /// <remarks>
    /// Choosing this is a claim that the field is a pure function of saved state, and the claim is
    /// checkable: rebuild it and assert it matches. A derived field that is <em>not</em> a pure
    /// function of saved state is a divergence the hash is now blind to by declaration, which is the
    /// one way to reintroduce the defect this enum closes.
    /// </remarks>
    Derived = 1,

    /// <summary>
    /// Scratch. Neither saved, hashed, nor rebuilt — its content between the phases that use it is
    /// meaningless by declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule this value carries, and it is an obligation rather than an exemption.</b> A
    /// scratch column <em>must be written before it is read within the phase that uses it, and
    /// nothing outside that phase may read it.</em> That is checkable and it is checked: filling
    /// every scratch column with garbage at a phase boundary must not move the State Hash trace. So
    /// the rebuild audit skips these columns by declaration, and the skip is paid for by an assertion
    /// rather than by a name in somebody else's test.
    /// </para>
    /// <para>
    /// <b>Choosing this is a claim too, and it is the stronger one.</b> <see cref="Derived"/> claims
    /// the field can be recovered; this claims nothing ever needs to recover it, because no read of
    /// it outlives the write. A column that is genuinely recoverable and merely happens to have no
    /// rebuild yet is <see cref="Derived"/> with a missing rebuild — a defect — and not this.
    /// </para>
    /// <para>
    /// <b>Why it is storage at all, rather than a local or a bare array.</b> The intermediate has to
    /// live across calls within the phase and be sized with the table. Declaring it here is what
    /// keeps it inside the declaration: <c>BOR0901</c> rejects the bare array, and it is right to —
    /// scratch that escapes the declaration is scratch nobody audits.
    /// </para>
    /// </remarks>
    Scratch = 2,
}

/// <summary>
/// How often a column is read, which is what the hot/cold split of adr/0004 and <c>05 §3</c> decides.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three values rather than two, because S4 task 2 found that "hot" had never been defined and
/// that the two available readings are 4× apart.</b> The corpus's *"on the order of 40 bytes hot"*
/// for a Citizen matches neither: the row is 13 B under the per-Tick reading and 51 B under the
/// working-set one. The two drive different things — the per-Tick figure sizes the Event Wheel drain
/// and the wake gather, the working-set figure sizes the world and the save copy — so a single
/// hot/cold bit would have to pick one and lose the other.
/// </para>
/// <para>
/// Nothing in the simulation branches on this. It is a declaration, read by the footprint report and
/// by whoever is deciding whether a new column belongs in a hot loop.
/// </para>
/// </remarks>
public enum Touch
{
    /// <summary>
    /// Read or written on an ordinary Tick, for every live row. The Event Wheel bucket key and its
    /// intrusive link are the archetype.
    /// </summary>
    PerTick = 0,

    /// <summary>
    /// Read when the entity is woken and acts — a transaction, a decision, a Trip. The bulk of a
    /// row, and untouched on the overwhelming majority of Ticks.
    /// </summary>
    Wake = 1,

    /// <summary>
    /// Read on inspection or on a rare transaction, never inside <c>step()</c>. Household economics
    /// — income, expenses, savings, purchases made and missed — is the case adr/0004 names.
    /// </summary>
    Cold = 2,
}

/// <summary>
/// Whether a table keeps a second copy of its columns for the duration of a parallel phase.
/// </summary>
/// <remarks>
/// <para>
/// adr/0037: <b>a table is double-buffered if and only if a parallel phase both reads and writes
/// it.</b> That is two tables out of a dozen — Lane dynamics, because Phase 4 is parallel and a
/// Vehicle crossing a Junction reads another Lane's queue, and Map Layer cells, which need it anyway
/// for directional smear. Roughly 2 MB against 80–150 MB for everything else.
/// </para>
/// <para>
/// <b>In Phase 1 nothing is parallel, so every table is <see cref="OneCopy"/>.</b> The property is
/// declared anyway, per table, so the rule is stated where a future table is added rather than
/// inferred later. K0 measured what the alternative would have cost: the full-world copy adr/0037
/// deleted is 172.3 MiB, 13.6 ms at this machine's copy rate, against a 15.6 ms Tick budget at 4×
/// speed — 87% of the Tick to move data that was 99% unchanged.
/// </para>
/// <para>
/// <b>Record with it the reason it is safe: the Past is not a second copy.</b> It is <em>the state as
/// of the start of this Tick</em>, and Phase 2 observes it because nothing has written yet. Phase 2's
/// read-only-ness is therefore load-bearing — a future decision to parallelise Decide must not also
/// make it mutating, or every entity table silently reclassifies to <see cref="TwoCopies"/>.
/// </para>
/// </remarks>
public enum Buffering
{
    /// <summary>One live copy. Correct for any table written only by a serial phase.</summary>
    OneCopy = 0,

    /// <summary>
    /// Two copies for the duration of a parallel phase. Not yet used; Map Layer cells in slice 6 are
    /// the first.
    /// </summary>
    TwoCopies = 1,
}

/// <summary>
/// Whether a handle column's target must outlive it, or may be freed underneath it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The exemption <c>02 §10</c>'s every-handle-resolves walk needs, declared at the field rather
/// than listed in the checker.</b> That walk is driven by the columns for a stated reason — a list of
/// fields shares its blind spot with the bug it exists to find — so the one column that is allowed to
/// dangle has to say so where it is declared, in the same place and the same spirit as
/// <see cref="Disposition"/>.
/// </para>
/// <para>
/// <b>Slice 10 is what made this necessary, and only one column needs it.</b> Demolition is the first
/// thing in this project that can free a row another table holds a handle to. A Household's dwelling
/// is repointed on eviction and a Bin's owner dies with its Building — but a Citizen's Workplace
/// belongs to somebody else's Building, and clearing it would need a Building-to-workers reverse index
/// that does not exist and belongs to the labour system rather than to Zone Rules. Leaving it stale is
/// not a concession: <see cref="Rows{T}.Free"/> already promises that every handle to a freed row
/// becomes <em>detectably</em> stale, and a workplace that no longer resolves is exactly the fact that
/// the job no longer exists.
/// </para>
/// </remarks>
public enum Reference
{
    /// <summary>
    /// The target must be live for as long as the row holding the handle is. A dangling one is a
    /// defect, and <c>Invariant.CrossTableHandleResolves</c> reports it.
    /// </summary>
    Required = 0,

    /// <summary>
    /// The target may be freed while this handle still points at it, and the stale handle <em>is</em>
    /// the fact. Read it through <c>TryResolve</c>; a bare <c>Resolve</c> on such a column is a throw
    /// waiting for the first demolition.
    /// </summary>
    Severable = 1,
}
