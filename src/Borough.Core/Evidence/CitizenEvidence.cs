namespace Borough.Core.Evidence;

using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Tables;

/// <summary>A journey this Citizen is on right now.</summary>
/// <remarks>
/// <b>In flight only.</b> A Trip that has ended has been freed — <c>TripEngine.Release</c> asserts it
/// carries a Fate and gives the row back on the next line — so this is never a finished journey. The
/// <em>last</em> Trip is milestone 6 task 7 and is not here yet; see
/// <see cref="CitizenEvidence.Trip"/>.
/// </remarks>
/// <param name="Trip">Which Trip.</param>
/// <param name="Purpose">What it is for.</param>
/// <param name="Fate">Always <c>TripFate.InFlight</c> while it is reachable this way.</param>
/// <param name="ArrivesAt">When the Traveller expects to reach the end of its current Leg.</param>
public readonly record struct TripEvidence(
    Handle<Trip> Trip, TripPurpose Purpose, TripFate Fate, Ticks ArrivesAt);

/// <summary>How this Citizen's last journey ended, and roughly when.</summary>
/// <remarks>
/// <para>
/// <b>The Trip itself is gone and this is not a handle to it.</b> <c>TripEngine.Release</c> frees the
/// row on the line after asserting it carries a Fate, and the identity is not resurrected here —
/// milestone 6 task 7 kept the <em>outcome</em> and deliberately not the object, because a handle to a
/// freed row is worse than no handle: it resolves to whatever was allocated into the slot next.
/// ***An outcome outliving its subject is a fact; an identity outliving its row is a bug.***
/// </para>
/// <para>
/// ⚠ <b>No <c>TripPurpose</c>, and that is <c>adr/0070</c> rather than an oversight.</b> One Trip
/// generator exists — <c>CommuteEngine</c> — plus <c>TripPurpose.Commanded</c>, which <c>adr/0080</c>
/// demotes to a test affordance. A purpose column would therefore store the same value for every row
/// in every world this project can build, which is a placeholder with a type around it. It arrives as
/// an added member on the Day a second generator does, and adding one changes nothing already
/// returned.
/// </para>
/// </remarks>
/// <param name="Fate">How it ended. Never <c>TripFate.InFlight</c> — that is the absent case.</param>
/// <param name="EndedDay">
/// Which Day it ended on. ⚠ <b>Days and not Ticks</b>, so <em>this morning</em> and <em>this evening</em>
/// are the same reading; <c>CitizenTable.LastTripEndedDay</c> carries the memory argument that bought
/// that. A caller wanting more resolution wants the <em>current</em> Trip, which carries a real Tick.
/// </param>
public readonly record struct PastTripEvidence(TripFate Fate, ushort EndedDay);

/// <summary>
/// <c>02 §9</c>'s Citizen answer: where they live and work, what they are doing, and — where they do
/// not work — why not.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>Two of <c>02 §9</c>'s clauses are absent and each for a different reason</b> — it was three
/// until milestone 6 task 7 built the last Trip's Fate.
/// <b>Need satisfaction</b> has no table anywhere: <c>adr/0103</c> closes the Need set at four and
/// nothing builds it. <b>Household finances</b> is the interesting one —
/// <c>HouseholdTable.Money</c> and <c>Savings</c> are declared, <c>Saved</c>, hashed, in the save file
/// and read by an invariant, and <b>every writer in the repository is a test</b>, so the field would
/// report a legitimate-looking zero for every Household in every world. It is omitted rather than
/// returned, because a Household with no money and a Household in a world with no money read the
/// same. ✅ <b>The <b>last</b> Trip's Fate was the third and is now built</b>
/// (task 7): see <see cref="CitizenEvidence.LastTrip"/>.
/// </para>
/// <para>
/// <b>None of those absences shaped this type.</b> <c>adr/0070</c> makes an unbuilt mechanism evidence
/// of nothing, so there is no per-clause availability flag here and no nullable standing in for one —
/// the rule followed is the narrower <em>do not publish a number that cannot be distinguished from a
/// real one</em>, which would hold just as well if money were built and merely unread. Each arrives as
/// an added member when its mechanism does, and adding one changes nothing already returned.
/// </para>
/// </remarks>
[ColdPath("02 §9's Citizen answer, assembled when a panel asks. No path from step() reaches it.")]
public readonly struct CitizenEvidence
{
    internal CitizenEvidence(
        Handle<Citizen> citizen,
        Handle<Household> household,
        Handle<Building> home,
        Handle<Building> workplace,
        byte activity,
        Ticks plannedCommute,
        ushort reachFailures,
        TripEvidence? trip,
        PastTripEvidence? lastTrip)
    {
        Citizen = citizen;
        Household = household;
        Home = home;
        Workplace = workplace;
        Activity = activity;
        PlannedCommute = plannedCommute;
        ReachFailures = reachFailures;
        Trip = trip;
        LastTrip = lastTrip;
    }

    /// <summary>Which Citizen this is about.</summary>
    public Handle<Citizen> Citizen { get; }

    /// <summary>The Household they belong to.</summary>
    public Handle<Household> Household { get; }

    /// <summary>
    /// Where they live, or the unset handle when their Household is in the Unplaced Pool.
    /// </summary>
    /// <remarks>
    /// <b>A Citizen has no home of their own and that is settled rather than missing</b> — S4 task 2,
    /// and <c>Entities.cs</c> carries the reasoning. This is their Household's dwelling.
    /// </remarks>
    public Handle<Building> Home { get; }

    /// <summary>
    /// Where they work, or the unset handle when they do not.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Unset covers two cases the build genuinely does not separate</b>: never employed, and
    /// employed at a Building since demolished. <c>CitizenTable.Workplace</c> is
    /// <c>Reference.Severable</c>, and a workplace that no longer resolves <em>is</em> the job no
    /// longer existing rather than a break in the handle — so the assembler reports the unset handle
    /// for both, which is what the simulation itself believes.
    /// </remarks>
    public Handle<Building> Workplace { get; }

    /// <summary>What they are doing, as an id the host resolves.</summary>
    public byte Activity { get; }

    /// <summary>
    /// What their commute cost <b>when they took the job</b>, not what it costs now.
    /// </summary>
    /// <remarks>
    /// <c>adr/0101</c>, and it is deliberately not refreshed: somebody still leaving at the old hour
    /// for a journey that has since got worse is late for work, which is a diagnosis the city can show.
    /// Zero for somebody with no job.
    /// </remarks>
    public Ticks PlannedCommute { get; }

    /// <summary>
    /// <b>Where there is no workplace, why</b> — how many job searches ended with the Road Graph
    /// unable to deliver anything inside the Commute Budget, since they last took a job.
    /// </summary>
    /// <remarks>
    /// <c>adr/0097</c> and milestone 6 task 3, and it is the first honest constituent
    /// <c>02 §9</c>'s Citizen row has ever had: <c>jobs beyond budget</c> could report <em>distance
    /// rather than supply separates them</em> and name nobody. <b>Zero distinguishes two things</b> —
    /// somebody employed (it resets on employment) and somebody refused for want of a <em>vacancy</em>
    /// rather than for want of a road, which <c>adr/0097</c> deliberately does not remember because
    /// re-detecting it costs one array read.
    /// </remarks>
    public ushort ReachFailures { get; }

    /// <summary>
    /// The journey they are on, or absent.
    /// </summary>
    /// <remarks>
    /// <b>The <em>current</em> half of <c>02 §9</c>'s <i>"current or last"</i></b>; the other half is
    /// <see cref="LastTrip"/>, and the two are separate members rather than one because they are
    /// different objects — this one is a live Trip with a handle and a Tick, and that one is an
    /// outcome whose Trip no longer exists. ⚠ <b>Both can be absent and both can be present at
    /// once</b>: somebody mid-journey who has travelled before has both, and neither is a fallback
    /// for the other.
    /// </remarks>
    public TripEvidence? Trip { get; }

    /// <summary>
    /// How their last <em>finished</em> journey ended, or absent if none ever has.
    /// </summary>
    /// <remarks>
    /// <b>Milestone 6 task 7, and it discharges the clause this type shipped owing.</b> It was
    /// unrecoverable when task 4 built the rest: <c>TripEngine.Release</c> frees the Trip row on the
    /// line after asserting it carries a Fate, and <c>AdvanceTravellers</c> frees the
    /// <b>Traveller</b> — which holds the only Citizen-to-Trip link there is — earlier in the same
    /// pass, so the Fate and the association with the person who made the journey ceased to exist
    /// together. <c>CitizenTable.LastTripFate</c> now keeps the outcome on the Citizen, who outlives
    /// the journey by design.
    /// </remarks>
    public PastTripEvidence? LastTrip { get; }
}
