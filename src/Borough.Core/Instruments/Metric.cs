namespace Borough.Core.Instruments;

/// <summary>
/// The counters a table exposes to the <see cref="Census"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Three counters rather than one, because they fail differently.</b> A table that is leaking and
/// a table that is merely busy both show a rising <see cref="Live"/>, and nothing about that number
/// alone separates them. The pair that separates them is <see cref="Live"/> against
/// <see cref="Slots"/>: slots are only ever allocated when the free list is empty, so
/// <em>Slots rising while Live is flat</em> is a free list that is not being returned to — which is
/// <c>adr/0006</c>'s failure with the population held constant, and is invisible in a row count.
/// </para>
/// <para>
/// <b><see cref="Capacity"/> is the third because it is the one that costs memory.</b> Slots are a
/// high-water mark and capacity is what was actually allocated to hold them; they diverge because
/// growth is geometric. A run whose capacity climbs while its slots do not has an array being grown
/// by something other than demand.
/// </para>
/// </remarks>
public enum CensusCounter : byte
{
    /// <summary>Rows in use. The city's size, and on its own not evidence of anything.</summary>
    Live,

    /// <summary>
    /// Slots ever allocated — the high-water mark of simultaneous demand.
    /// </summary>
    /// <remarks>
    /// Rises only when a row is created and the free list is empty, so it is flat across any
    /// create-and-destroy cycle that recycles. This is the counter <c>adr/0006</c> is about.
    /// </remarks>
    Slots,

    /// <summary>The length of the backing arrays. What the table costs, as opposed to what it holds.</summary>
    Capacity,
}

/// <summary>
/// What a <see cref="Census"/> series is a series <em>of</em>: one counter of one table.
/// </summary>
/// <remarks>
/// <para>
/// <b>An id and a number, never a name.</b> <c>adr/0002</c> gives the core ids and gives the shell
/// every string a human reads, and a metric identified by a string would have put the vocabulary of
/// the panel inside the simulation. <see cref="Table"/> is the index into <c>World.Tables</c>, which
/// is declaration order — the same order the State Hash folds tables in, so a metric means the same
/// thing to a trace as it does to a hash.
/// </para>
/// <para>
/// <b>It names a table rather than a collection because the intrusive-list pattern makes those the
/// same thing.</b> Every variable-length structure in the core is an intrusive index list whose nodes
/// are rows, so the total length of every list over a table is that table's live row count and there
/// is no separate collection to sample. A list whose nodes are live rows missing from it is not a
/// magnitude problem but a correctness one, and belongs to the invariant tiers rather than here.
/// </para>
/// </remarks>
/// <param name="Table">Index into <c>World.Tables</c>, in declaration order.</param>
/// <param name="Counter">Which of the table's counters.</param>
public readonly record struct Metric(int Table, CensusCounter Counter);
