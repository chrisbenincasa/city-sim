namespace Borough.Core.Movement;

/// <summary>
/// How good a commute is, graded in three bands by the Commute Budget's rungs (<c>adr/0095</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>A rung is not a <see cref="TripFate"/> and adding one here does not open that set.</b>
/// <c>adr/0076</c>'s second clause is that <em>anything that arrives as time is scored by the Commute
/// Budget, which is not a Fate</em>. All three of these are gradations of a cost in time, so all
/// three sit on the Budget's side of that line: a commute on the <see cref="Unsavoury"/> rung
/// <b>completes</b>. Only the ceiling above <see cref="Unsavoury"/> produces a Fate, and it is the
/// one that already existed.
/// </para>
/// <para>
/// <b>It is a vocabulary rather than a scale</b>, which is what fixes the count at three. The
/// question a rung answers is <em>what is this commute like</em>, and the answer has to be a word a
/// player can hold while reading a map. A fourth rung is evidence somebody wants a distribution, and
/// the distribution already exists — <see cref="Instruments.TripCostBucket"/> has seven bands and
/// deliberately does not move with the Budget, because <em>a ruler must not move with the thing it
/// measures</em>.
/// </para>
/// <para>
/// ⚠ <b>A rung grades a cost that today has only one of its two drivers.</b> <c>01 §4</c> names two
/// scarcities that read as long commutes — <b>Congestion</b>, which is road capacity, and
/// <b>Separation</b>, which is distance — and this simulation implements the second. A walk Leg's
/// cost is <c>distance / speed</c> exactly, because pedestrian networks do not saturate
/// (<c>03 §3.7</c>, and <see cref="WalkRouting"/>'s own remarks), and a vehicular Leg carries no
/// congestion term because <c>adr/0075</c> gives a Leg a cost and no path. So these bands report
/// Separation and nothing else until 5c, and <c>adr/0070</c> is why that is stated rather than
/// compensated for.
/// </para>
/// <para>
/// <b>The order is the preference order and the numeric values are load-bearing.</b>
/// <see cref="Rules.EmploymentEngine"/> compares rungs with <c>&lt;</c> to take the best one it drew,
/// so <see cref="Fast"/> must be zero and each rung must be worse than the one before it.
/// </para>
/// </remarks>
public enum CommuteRung : byte
{
    /// <summary>
    /// <b>Unremarkable.</b> The commute is short enough that nobody thinks about it, and nothing in
    /// the simulation reads this except the Census.
    /// </summary>
    Fast,

    /// <summary>
    /// <b>Noticed and tolerated.</b> Mechanically identical to <see cref="Fast"/> — it refuses
    /// nothing and costs nobody anything. <b>It exists to be read</b>: a city whose commutes are
    /// migrating from fast to moderate is deteriorating, and that is the period in which a spatial
    /// fix is still cheap. A single threshold reports zero throughout it and then reports a cliff.
    /// </summary>
    Moderate,

    /// <summary>
    /// <b>Taken rather than accepted.</b> The Trip completes and the Citizen makes it every Day, and
    /// this is where <c>01 §4</c>'s <em>housed</em> Departures come from — a quality failure rather
    /// than a capacity one, which is the distinction that tells a player to fix what they have
    /// instead of building more of it.
    /// </summary>
    /// <remarks>
    /// <b>Nothing consumes it yet and that is deliberate.</b> The Departure channel is unbuilt, so
    /// wiring a consequence here would be inventing one for a mechanism that does not exist
    /// (<c>adr/0070</c>). The rung is named and counted, which is what makes it available on the day
    /// retention is built.
    /// </remarks>
    Unsavoury,
}
