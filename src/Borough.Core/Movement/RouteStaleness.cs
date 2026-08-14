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

/// <summary>
/// Which entry a full set gives up when a new route arrives.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>A switch because the access pattern this store actually sees is the one replacement policy
/// LRU is provably worst at.</b> A commute is a <b>once-per-Day cyclic scan</b>: every employed
/// Citizen departs once per Day, and <see cref="CommuteRoster"/> puts each in a fixed bucket, so the
/// order is stable across Days. Over a working set larger than the store, LRU evicts precisely the
/// entry that is needed next and converges toward zero — measured at <b>2.83%</b> where a policy that
/// simply holds a fixed subset would give <c>store ÷ working set</c>.
/// </para>
/// <para>
/// <b>And <c>store ÷ working set</c> is the ceiling, not a target.</b> On a uniform cyclic scan every
/// key is equally likely to be needed next, so no policy can retain a better subset than an arbitrary
/// one — the information a smarter policy would need does not exist in the access stream. The rungs
/// below therefore split into <em>LRU, which is uniquely bad</em> and <em>everything else, which is at
/// the ceiling</em>. Choosing between them buys the gap and nothing beyond it.
/// </para>
/// <para>
/// ⚠ <b>R6's four-way measurement is not evidence about this axis.</b> It measured <em>conflict</em>
/// misses — how a key maps to a set — across 1, 2, 4 and 8 ways, which is a property of the hash and
/// the associativity. Which entry a full set gives up is a different question, and the draw R6 used
/// was not a cyclic scan.
/// </para>
/// <para>
/// <b>Measured on a real commute draw, 5c task 4</b> (16,000 Citizens, 4,808 distinct pairs, store
/// 1,024, ceiling 21.30%): <b>LRU 2.83%, Random 3.79%, MRU 19.54%, None 22.41%</b>.
/// ⚠ <b>Random fails here and it succeeds in the textbook, because the textbook cache is fully
/// associative.</b> Inside a four-way set a random victim still churns the set out over one cycle —
/// a resident entry survives each colliding arrival with probability ¾, and a set takes many arrivals
/// per Day. <b>Only a policy that refuses to displace, or one that inverts recency, holds a stable
/// subset.</b>
/// </para>
/// </remarks>
public enum RouteEviction
{
    /// <summary>
    /// Least recently used. <c>adr/0012</c>'s stated policy, and ⚠ the worst possible one for a scan.
    /// </summary>
    Lru,

    /// <summary>
    /// Most recently used — <b>evict what was just served, because a cyclic scan will not want it
    /// again until the cycle comes round.</b>
    /// </summary>
    /// <remarks>
    /// The textbook inversion for this access pattern: LRU's rule is exactly wrong when recency
    /// predicts a <em>long</em> wait rather than a short one. It protects whatever subset it happens to
    /// hold, which is the ceiling.
    /// </remarks>
    Mru,

    /// <summary>
    /// A deterministic pseudo-random victim, drawn from the key rather than from a stream.
    /// </summary>
    /// <remarks>
    /// <b>Counter-based and not a stream</b>, per <c>CLAUDE.md</c>'s randomness rule — the victim is a
    /// function of the arriving key and the set, so two identical cities evict identically and
    /// <c>System.Random</c> never enters <c>Borough.Core</c>. Scan-resistant by having no memory to
    /// mislead.
    /// </remarks>
    Random,

    /// <summary>
    /// Nothing is ever displaced: a full set refuses new routes and serves what it holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The control, and it is a real policy rather than a degenerate one.</b> On a uniform scan it
    /// reaches the same ceiling as the others at zero bookkeeping and zero churn — which is the fact
    /// that says the gap between LRU and the rest is the whole of what a policy can buy here. It
    /// scored highest of the four.
    /// </para>
    /// <para>
    /// ⚠ <b>And it is not the default, because its edge is a property of a draw that never changes.</b>
    /// The measurement re-ran one commute set; a city changes jobs, and a full set under this policy can
    /// <em>never</em> admit the new pair. <see cref="Mru"/> is within three points on a static draw and
    /// adapts, so the default is chosen on a structural property rather than on the three points.
    /// ***A policy that wins on a frozen input has not been shown to win.***
    /// </para>
    /// </remarks>
    None,
}
