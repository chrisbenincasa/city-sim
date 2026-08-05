namespace Borough.Core.Tables;

/// <summary>
/// A typed generational reference to a row: <c>{ index: u32, generation: u32 }</c>.
/// </summary>
/// <typeparam name="T">
/// The entity this handle addresses. It is a tag — an empty struct in
/// <c>Borough.Core.Entities</c> — because under structure-of-arrays there is no row struct to name;
/// a Citizen is a set of columns, not a type. The parameter exists so that
/// <c>Handle&lt;Citizen&gt;</c> cannot index the Building table.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Typed as well as generational.</b> adr/0004 makes this move for identities and adr/0003 makes
/// the same move one level down for quantities. An untyped entity id leaves a whole class of bug open
/// by construction, and the class is not hypothetical: every cross-table field in this design is a
/// handle, and half of them are handles to a table one letter away in the source.
/// </para>
/// <para>
/// <b>The generation is what makes a stale handle detectably stale.</b> It is bumped on allocate and
/// again on free, so a live slot always carries an <em>odd</em> generation and a free slot an even
/// one. <see cref="None"/> is <c>default</c> — generation 0, permanently even — which is why a zeroed
/// column reads as "unset" rather than as a reference to row 0. The same reasoning as
/// <c>PurposeTag.None</c>, for the same reason: a zeroed struct field is the normal state of a row
/// that has not been written yet.
/// </para>
/// <para>
/// <b>It is deliberately not comparable, and that is a determinism rule rather than an omission.</b>
/// <c>05 §3</c>: a handle index must never be used as a sort key in simulation logic, because indices
/// are recycled by the free list — so ordering by one means an unrelated demolition on the far side
/// of the city can silently change who wins a contested draw downtown. There is no
/// <c>IComparable</c>, no relational operator, and <see cref="Index"/> is internal. Where a stable
/// per-entity key is genuinely needed it is the monotonic never-reused id on
/// <see cref="Rows{T}.IdOf"/>, which is a separate column and is never recycled.
/// </para>
/// </remarks>
public readonly record struct Handle<T>
    where T : unmanaged
{
    internal Handle(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    /// <summary>The slot this handle addresses. Never a sort key — see the type's remarks.</summary>
    internal uint Index { get; }

    /// <summary>Odd while the slot is live; even once it has been freed. Zero means unset.</summary>
    internal uint Generation { get; }

    /// <summary>
    /// True for the unset handle — a field that has never been pointed at anything.
    /// </summary>
    /// <remarks>
    /// <b>The unset handle is spelled <c>default</c>, and there is deliberately no <c>None</c>
    /// property to spell it with.</b> A static member on a generic type would be a second name for a
    /// value the language already has one for, and the point being made is exactly that the two are
    /// the same thing: a zeroed column is a column of unset handles, so a row that has been allocated
    /// and not yet written reads as pointing at nothing rather than as pointing at row 0.
    /// </remarks>
    public bool IsNone => Generation == 0;
}
