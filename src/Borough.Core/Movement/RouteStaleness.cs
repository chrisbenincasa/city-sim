namespace Borough.Core.Movement;

/// <summary>
/// What a <see cref="RouteCache"/> entry is allowed to be wrong about after the player edits a road.
/// </summary>
/// <remarks>
/// <para>
/// <b>A switch because the choice is measurable and had never been measured on a city</b>
/// (<c>adr/0043</c>). S2 R5 and R5.5.4 settled it on a synthetic lattice under a uniform
/// origin-destination draw — the draw R4 measured as *a different city* — and every figure in this
/// corpus that moved from a fixture to a real world moved in the same direction. 5c task 4 runs the
/// same instrument on real home-to-work pairs and the numbers are in
/// <c>plans/0026</c> → *Task 4*.
/// </para>
/// <para>
/// <b>Every rung is exact about a <em>removal</em> and they differ only about an addition.</b> That
/// asymmetry is <c>adr/0012</c>'s invalidation contract and it is not a policy: the containment test
/// for a bulldozed Segment exists and is cheap, so there is no interval in which a Traveller drives
/// through a road that is not there. What no rung can test exactly is an addition, because a new
/// Segment is on no existing route and containment has nothing to match.
/// </para>
/// </remarks>
public enum RouteStaleness
{
    /// <summary>
    /// <b>Never out of date: the whole cache is discarded whenever the Road Graph changes.</b>
    /// </summary>
    /// <remarks>
    /// <b>The only rung under which a hit and a miss are the same answer</b>, which is what keeps the
    /// cache out of the State Hash entirely — under <c>05 §4</c> it is then an optimisation however it
    /// was motivated, and it needs no saved state, because a reload with an empty cache produces the
    /// city a full one would have. Its cost is what <c>CONTEXT.md</c> → Epoch warns about: a single
    /// counter <em>is</em> a global flush, and a player dragging a road empties the store repeatedly.
    /// </remarks>
    Exact,

    /// <summary>
    /// <b>Out of date about an addition, for ever.</b> Entries survive a new road and nothing
    /// refreshes them.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Measured on the spike and it does not heal.</b> R5.5.4's control fell 38 → 23 wrongly-valid
    /// entries by Tick 64 and then did not move for 960 Ticks, with the resident population constant —
    /// and the mean detour *rose* 16.35% → 19.31% while the count fell, because collision eviction
    /// removes the mild errors and leaves the severe ones. A cache keyed by **pair** makes a hot pair
    /// the least likely to be evicted, so what persists is every driver's route on the busiest pairs at
    /// the largest detours. Kept as a measurable rung, not as a recommendation.
    /// </remarks>
    Keep,

    /// <summary>
    /// <b>Out of date about an addition, until a rotation reaches the entry.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><c>adr/0012</c>'s answer for this store specifically, and the ADR says so in those words</b> —
    /// *"R5.5.4's rotation is retained where it was measured and nowhere else. It rotated the shared
    /// pair-keyed cache, resident population 412 — a store whose size is a design choice and therefore
    /// constant in the city. 0.40 forced refreshes per Tick is affordable there and stays."*
    /// </para>
    /// <para>
    /// ⚠ <b>Do not read <c>adr/0083</c>'s refusal as applying here.</b> That ADR is the **Parking
    /// Shed**'s and it declines to *take* the rotation, which is the opposite direction to carrying it
    /// away from this store. <c>plans/0026</c>'s own task 4 brief had the attribution reversed, and it
    /// is <c>plans/0012</c> <b>Cause 5</b> again — a number read against a store it was never measured
    /// on.
    /// </para>
    /// <para>
    /// <b>A rotation needs traffic to be a teaching mechanism rather than a deletion.</b> Emptying a
    /// slot only makes room; what makes the replacement *correct* is the next request missing on it and
    /// searching the graph as it now is. A window with a rotation and no traffic measures a cache being
    /// deleted and reports it as a cache being taught.
    /// </para>
    /// </remarks>
    KeepAndRotate,
}
