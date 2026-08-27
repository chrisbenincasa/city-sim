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
/// <b>Four values, and they are the Day the build actually has.</b> <c>adr/0101</c> makes a commute
/// two journeys anchored on a Shift, and the commute is the only Trip generator that exists — so
/// there is nothing else a Citizen can be doing yet. ⚠ <b>The set grows when a second generator
/// does</b>, and a reader must not take four as a claim about how full a life is.
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
}
