namespace Borough.Core.Movement;

/// <summary>
/// The recorded outcome of a Trip. <b>The set is closed at four</b> (<c>adr/0076</c>), and a Fate is
/// never silently discarded.
/// </summary>
/// <remarks>
/// <para>
/// <b>The rule that closes the set has two clauses, and every candidate fifth falls to one of
/// them.</b> A Fate names how the <em>journey</em> ended — so anything that fails at the far end is
/// another object's outcome and the Trip <see cref="Completed"/>. And anything that arrives as
/// <em>time</em> is scored by the Commute Budget, which is not a Fate. <b>A fifth Fate is always a
/// proposal to record something twice</b>, somewhere less diagnosable than where it already is.
/// </para>
/// <para>
/// <b>Three authors have reached independently for that argument</b>, which is why the set is worth
/// defending at the write site: <c>adr/0067</c> (<i>"the Trip completed; what failed is the
/// purchase"</i>), <c>CONTEXT.md</c> → Parking Shed (<i>"deliberately no <b>no parking</b> Trip
/// Fate"</i> — scarcity widens the shed and lengthens the walk Leg, arriving as time), and
/// <c>CONTEXT.md</c> → Diversion (<i>"there is no Trip Fate for a lost driver"</i> — that is a
/// <b>Rejoin being abandoned</b>, which is not a Fate).
/// </para>
/// <para>
/// <b>A destination demolished mid-Trip needs no new Fate</b>, and it is the common case rather than
/// a corner under any Ruleset that condemns Buildings. If the <em>Segment</em> is gone the Trip is
/// <see cref="Stranded"/>; if the <em>Building</em> is gone and the Segment is fine the Trip
/// <see cref="Completed"/> and the purpose failed at the far end — because an
/// <see cref="Space.Address"/> outlives its Building, so a Traveller can genuinely arrive at a plot
/// of rubble.
/// </para>
/// </remarks>
public enum TripFate : byte
{
    /// <summary>
    /// <b>The Trip has not ended.</b> Not one of the four — the unset value, so a row that has never
    /// been written cannot read back as an outcome.
    /// </summary>
    /// <remarks>
    /// Zero is <em>in flight</em> rather than <see cref="Completed"/> deliberately. A freshly
    /// allocated row is zero-filled, and a default that meant <i>completed</i> would make
    /// <c>02</c>'s <i>no Trip without a Fate</i> assertion pass on a Trip nothing had ever resolved —
    /// the check would confirm exactly the bug it exists to catch.
    /// </remarks>
    InFlight = 0,

    /// <summary>The Traveller arrived. <b>What happened at the far end is another object's outcome.</b></summary>
    Completed = 1,

    /// <summary>
    /// The network does not connect origin to destination for this mode. <b>What Severance produces.</b>
    /// </summary>
    /// <remarks>
    /// Nobody deletes a pedestrian route — an Arterial's Arcs simply never carried
    /// <see cref="Space.TravelMode.Foot"/>, so the route genuinely does not exist. This is the Fate a
    /// city that is perfectly well connected for cars and broken for people reports, and it is the
    /// clearest payoff of treating walking as real.
    /// </remarks>
    NoRouteFound = 2,

    /// <summary>
    /// A route exists and costs more than the Commute Budget. <b>The line between a Trip that
    /// completes and one that does not.</b>
    /// </summary>
    /// <remarks>
    /// One currency, and it is clock minutes — there is no per-mode weight (<c>adr/0008</c>, session
    /// F). A weight would make the scored quantity differ from the <em>displayed</em> one, which is
    /// the SC4 unlearnability failure the Budget exists to prevent, arriving through the door it
    /// locked.
    /// </remarks>
    ExceededCommuteBudget = 3,

    /// <summary>
    /// The network changed mid-journey and the plan no longer describes a road that exists.
    /// </summary>
    /// <remarks>
    /// <b>This Fate and nothing else.</b> The lost-driver condition that once shared the word is a
    /// <b>Rejoin being abandoned</b>, which is a routing event rather than an outcome of the journey
    /// (<c>adr/0076</c>).
    /// </remarks>
    Stranded = 4,
}
