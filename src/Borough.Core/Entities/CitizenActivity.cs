namespace Borough.Core.Entities;

/// <summary>
/// What a Citizen is doing, as the id <see cref="CitizenTable.Activity"/> stores.
/// </summary>
/// <remarks>
/// <para>
/// <b><see cref="CitizenTable.Activity"/> was declared, saved, hashed and <c>Touch.PerTick</c> with
/// no writer at all until 2026-08-26</b> — one reference in the whole tree, and it was a read in
/// <c>Evidence.OfCitizen</c>. So <c>02 §9</c>'s <em>what are they doing</em> answered <b>0</b> for
/// every Citizen in every world. This names the values and <c>CommuteEngine</c> and
/// <c>World.RecordTripFate</c> write them.
/// </para>
/// <para>
/// <b>Five values, and the fifth arrived exactly as this remark said it would.</b> It read <i>four
/// values, and they are the Day the build actually has … the set grows when a second generator
/// does</i> — and by 2026-09-03 there were four generators: the commute,
/// <c>TripPurpose.Immigration</c>, <c>Shopping</c> and <c>School</c>. Only the commute wrote a
/// travelling value, so ***a Citizen on any other journey read as standing still.***
/// </para>
/// <para>
/// 🔴 <b>THAT WAS NOT A COSMETIC GAP, BECAUSE THIS COLUMN IS A GUARD.</b>
/// <c>CommuteEngine.Travel</c> refuses a Citizen who is not <see cref="AtHome"/> or
/// <see cref="AtWork"/>, and its own remark says why — <i>a roster phase arriving while somebody is
/// still walking would otherwise start a second Trip under the first.</i> An immigrant walking in
/// from a Gate read as <see cref="AtHome"/>, so the roster started one: measured on
/// <c>plans/0055</c>'s tree, citizen 1678 of <c>ArrivalLongRunTests</c> began a Commute at Tick 252
/// with an Immigration Trip still in flight, and the two journeys' parking releases and takes
/// interleaved until one space was held by nobody — <c>Invariant.ParkingOccupancyIsConserved</c>,
/// <b>1,160 against 1,159</b>. ⚠ <b>It could not happen while the city sat in the map's origin
/// corner</b>, because two of the four Gates were then zero minutes from it and an Immigration Trip
/// completed on the Tick it started.
/// </para>
/// <para>
/// ⚠ <b>What is closed is the COMMUTE's door and not every door.</b> Nothing refuses a second Trip
/// structurally; what prevents one is that its generator asks this column first, and only the
/// commute asks. <b>Shopping and School do not</b>, so a Trip started under one of those is
/// <em>unbuilt</em> rather than <em>refused</em> (<c>adr/0070</c>) — named here rather than fixed,
/// because a refusal needs a <c>TripFate</c> to report it and that is a decision.
/// </para>
/// <para>
/// <b><see cref="AtHome"/> is zero deliberately.</b> A freshly allocated row is zero-filled, and a
/// Citizen who has never moved is at home — so the default reads as the truth rather than as a
/// state nothing has written, which is <c>TripFate.InFlight</c>'s argument one table across.
/// </para>
/// </remarks>
public enum CitizenActivity : byte
{
    /// <summary>In their Household's dwelling, or nowhere if it is unplaced.</summary>
    AtHome = 0,

    /// <summary>On the outbound journey of <c>adr/0101</c>'s two.</summary>
    TravellingToWork = 1,

    /// <summary>Arrived at their Business's premises.</summary>
    AtWork = 2,

    /// <summary>On the homeward journey.</summary>
    TravellingHome = 3,

    /// <summary>On a journey that is not the commute — an arrival, a shop, a school run.</summary>
    /// <remarks>
    /// <b>One value for every other purpose rather than one apiece</b>, because what reads this is a
    /// guard asking <em>are they on the road</em> and not a report asking <em>where are they
    /// going</em>. ⚠ <b>It resolves to <see cref="AtHome"/> whatever the Fate</b>: unlike the
    /// commute's two it carries no direction, so there is no second place a failed journey could
    /// leave somebody.
    /// </remarks>
    Travelling = 4,
}
