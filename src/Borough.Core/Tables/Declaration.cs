namespace Borough.Core.Tables;

/// <summary>
/// What a column is for: state the simulation owns, or a cache it can rebuild.
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
