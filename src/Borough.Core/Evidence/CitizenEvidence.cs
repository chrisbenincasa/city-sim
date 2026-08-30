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
/// ✅ <b>The last of <c>02 §9</c>'s three absent clauses is now present.</b> <b>Need
/// satisfaction</b> is <see cref="CitizenEvidence.Sustenance"/> — a Household's, read through the
/// Citizen, like the balance beside it. 🔴 <b>Two of <c>adr/0103</c>'s four Needs exist</b>, and the
/// other two are <em>undesigned</em> rather than unbuilt, so nothing follows from their absence
/// (<c>adr/0070</c>). ✅ <b>The <b>last</b> Trip's Fate was the second</b>
/// (milestone 6 task 7): see <see cref="CitizenEvidence.LastTrip"/>. ✅ <b>Household finances was
/// the third</b> (milestone 10 task 8): see <see cref="CitizenEvidence.HouseholdBalance"/>.
/// </para>
/// <para>
/// ⚠ <b>The finances clause was omitted under a condition with two halves, and only one of them was
/// paid by a writer arriving.</b> The condition read: the columns had no production writer, <em>and</em>
/// a Household with no money and a Household in a world with no money read the same. Milestone 10 task
/// 4c made a balance a Bin and task 5 gave it a writer, which pays the first half. <b>The second half
/// is still true and is paid by the shape instead</b> — <see cref="HouseholdBalance"/> is absent where
/// the world names no money and present where it does, so a zero is now a Household that has spent
/// everything and nothing else. ***A writer arriving does not discharge a condition about what a
/// reader can tell apart.***
/// </para>
/// <para>
/// <b>No absence here shaped this type.</b> <c>adr/0070</c> makes an unbuilt mechanism evidence of
/// nothing, so there is no per-clause availability flag and no nullable standing in for one — the rule
/// followed is the narrower <em>do not publish a number that cannot be distinguished from a real
/// one</em>, which would hold just as well if money were built and merely unread. Each arrives as an
/// added member when its mechanism does, and adding one changes nothing already returned.
/// </para>
/// <para>
/// ⚠ <b><see cref="HouseholdBalance"/> is not a counter-example, and the line is worth stating because
/// the two look alike.</b> A nullable that means <em>nobody has built this</em> is the availability
/// flag <c>adr/0070</c> refuses; a nullable that means <em>this world has no currency</em> is a state
/// of the world, authored in a Ruleset, and true of a world running the finished game. ***An absence
/// the content can produce is a reading; an absence only the roadmap can produce is a flag.***
/// </para>
/// </remarks>
[ColdPath("02 §9's Citizen answer, assembled when a panel asks. No path from step() reaches it.")]
public readonly struct CitizenEvidence
{
    internal CitizenEvidence(
        Handle<Citizen> citizen,
        Handle<Household> household,
        Handle<Building> home,
        Handle<Business> workplace,
        byte activity,
        Ticks plannedCommute,
        ushort reachFailures,
        Money? householdBalance,
        int? sustenance,
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
        HouseholdBalance = householdBalance;
        Sustenance = sustenance;
        Trip = trip;
        LastTrip = lastTrip;
    }

    /// <summary>Which Citizen this is about.</summary>
    public Handle<Citizen> Citizen { get; }

    /// <summary>
    /// How well fed this Citizen's Household is. <b>0 is ideal, negative is deficit; null if
    /// unhoused.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ✅ <b><c>02 §9</c>'s third clause, and the one this type shipped without.</b> It is a Need and
    /// not a stockpile — <c>04 §2</c>'s <em>Goods are absolute; Needs are relative</em> — so it
    /// answers <em>how well is this Household doing</em> and never <em>how much food is in the
    /// cupboard</em>. The cupboard is a Bin and is a different question.
    /// </para>
    /// <para>
    /// ⚠ <b>Sustenance only.</b> Satisfaction is a column no shipped Ruleset feeds; Education and
    /// Health have no column, <c>adr/0103</c> leaving their rule <b>undesigned</b> — and
    /// ***nothing may be reasoned from any of those absences*** (<c>adr/0070</c>).
    /// <b>Null where <see cref="Household"/> is unresolvable</b>, on
    /// <see cref="HouseholdBalance"/>'s shape: a zero would say <em>ideally fed</em> about somebody
    /// nobody is feeding.
    /// </para>
    /// </remarks>
    public int? Sustenance { get; }


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
    /// employed by a Business since departed, or one whose premises were demolished. <c>CitizenTable.Workplace</c> is
    /// <c>Reference.Severable</c>, and a workplace that no longer resolves <em>is</em> the job no
    /// longer existing rather than a break in the handle — so the assembler reports the unset handle
    /// for both, which is what the simulation itself believes.
    /// </remarks>
    public Handle<Business> Workplace { get; }

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
    /// <b><c>02 §9</c>'s household finances</b>: what their Household holds, or absent where the world
    /// names no money.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nullable because a zero and an absence are different facts, and only one of them is about
    /// this Household.</b> A Ruleset that declares no <c>family = "money"</c> Resource opens no money
    /// Bin on anybody (<c>World.FitHousehold</c>), so every reading in such a world would be zero and
    /// would look exactly like destitution. Absent says <em>this world has no currency</em>; present
    /// says <em>this is the balance</em>, and a present zero is somebody who has spent everything.
    /// </para>
    /// <para>
    /// ⚠ <b><c>World.BalanceOf</c> conflates the same two facts, deliberately, and both are
    /// right.</b> That method returns <see cref="Money.Zero"/> for a Household with no money Bin and
    /// says why: <em>"a world with no currency and a Household with none behave identically at every
    /// call site money has."</em> True of a call site that <em>spends</em> — both refuse the purchase —
    /// and false of a reader, which is what this type is. ***Two facts a mechanism may treat as one
    /// are still two facts to somebody reading them***, and the discriminator both use is the same
    /// one: <c>HouseholdTable.Balance</c> is a handle, and it can be unset.
    /// </para>
    /// <para>
    /// ⚠ <b>NO SHIPPED RULESET PRODUCES THE ABSENT READING, and that is worth knowing before
    /// trusting a run.</b> Milestone 10 task 2 put a <c>family = "money"</c> Resource in all seven,
    /// so every Household in every shipped world holds a money Bin and every reading is present. What
    /// six of the seven then show is <b>zero for everybody</b> — money enters a world only through
    /// <c>[households] opening_balance_min/max</c> and only <c>taxed.toml</c> states it — which is
    /// destitution rather than absence, and is exactly the pair this member exists to separate. The
    /// absent branch is exercised by the suite against a Ruleset naming no money, which is a world a
    /// file can describe and no shipped file does. ***A branch no shipped content reaches is still a
    /// branch content can reach, and the test is what says which.***
    /// </para>
    /// <para>
    /// <b>Also absent when the Household handle does not resolve</b>, which is not a second meaning
    /// because it is not a state: <c>Invariant.CitizenIsInExactlyOneHousehold</c> makes a Citizen
    /// without one a defect rather than a condition, so the case is unreachable in a world that
    /// passes its own checks and reporting it as a distinct absence would give a reader a distinction
    /// with nothing on the other side of it.
    /// </para>
    /// <para>
    /// <b>One figure and not two.</b> <c>adr/0024</c>'s reserve is a <em>behaviour</em> — what a
    /// Household will not spend — rather than a second account, which is milestone 10 task 4c's
    /// finding when it deleted <c>HouseholdTable.Savings</c>: one pool, and the reserve arrives as a
    /// policy over it. A second member here would have to be written by something, and nothing writes
    /// one.
    /// </para>
    /// </remarks>
    public Money? HouseholdBalance { get; }

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
