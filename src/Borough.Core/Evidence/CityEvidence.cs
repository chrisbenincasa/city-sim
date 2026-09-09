namespace Borough.Core.Evidence;

using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>Whose Rule Instance a blocked reading belongs to.</summary>
/// <remarks>
/// <b>The premises and its tenants are different subjects and the distinction is load-bearing.</b> A
/// premises Rule chain in every shipped Ruleset either always succeeds or never succeeds
/// (<c>adr/0168</c>), so a list keyed on premises cannot show a condition clearing. A tenant's can,
/// which is why <see cref="Household"/> and <see cref="Business"/> are separated out rather than
/// folded into the Building they stand in.
/// </remarks>
public enum SubjectKind : byte
{
    /// <summary>The Building itself, running its own Rule.</summary>
    Premises = 0,

    /// <summary>A Household living in it.</summary>
    Household = 1,

    /// <summary>A Business trading in it.</summary>
    Business = 2,
}

/// <summary>
/// Why a set of subjects is stopped: one row of the city's grouping key.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>It never names a quantity of anything.</b> <c>CONTEXT.md</c> → <em>Supply is a property of
/// one named Bin and never of the city</em>, so this carries which Resource a wait is <em>for</em>
/// and whose Bin it is <em>on</em>, and no level, capacity or total. What is counted beside it is
/// subjects.
/// </para>
/// <para>
/// <b><see cref="WaitingOn"/> is in the key and not decoration.</b> A tenant whose own larder is
/// empty and a District in which nobody is selling both report <c>sundries</c>
/// (<see cref="RuleEvidence.WaitingOn"/>), and they are two different problems with two different
/// answers.
/// </para>
/// </remarks>
/// <param name="Kind">Whose Rule it is.</param>
/// <param name="Blocked">Which wait list it is asleep on.</param>
/// <param name="WaitingFor">Which Resource the Bin it is asleep on holds.</param>
/// <param name="WaitingOn">Whose Bin that is.</param>
/// <param name="Explained">
/// False when the Rule is asleep with no wait target recorded. <b>It stays a group of its own rather
/// than merging into any other</b>: missing evidence is a different claim from a known cause, and
/// <c>plans/0064</c> row 13 requires that it never read as healthy.
/// </param>
public readonly record struct CityCause(
    SubjectKind Kind, Blocking Blocked, ResourceId WaitingFor, BinOwnerKind WaitingOn, bool Explained);

/// <summary>One cause, with how many subjects it is stopping and the worst of them.</summary>
/// <param name="Cause">The grouping key.</param>
/// <param name="Subjects">
/// How many distinct subjects it stops. <b>Distinct, not a count of Rule Instances</b> — a Household
/// with two Rules asleep on the same empty Bin is one family in trouble and would otherwise be two.
/// </param>
/// <param name="WorstMissedFirings">The largest missed-firing count among them.</param>
public readonly record struct CityGroup(CityCause Cause, int Subjects, long WorstMissedFirings);

/// <summary>One subject stopped by one cause.</summary>
/// <param name="Group">Which <see cref="CityEvidence.Groups"/> entry it belongs to.</param>
/// <param name="Kind">Whose Rule stopped.</param>
/// <param name="Building">Where it is, which is what a map draws and a click selects.</param>
/// <param name="Household">The Household, when <see cref="Kind"/> is one. Unset otherwise.</param>
/// <param name="Business">The Business, when <see cref="Kind"/> is one. Unset otherwise.</param>
/// <param name="SubjectId">
/// Its row's monotonic never-reused id. <b>The tie-break that makes the order total</b>, and the
/// number a panel prints beside the name.
/// </param>
/// <param name="MissedFirings">Its worst missed-firing count under this cause.</param>
public readonly record struct CitySubject(
    int Group,
    SubjectKind Kind,
    Handle<Building> Building,
    Handle<Household> Household,
    Handle<Business> Business,
    ulong SubjectId,
    long MissedFirings);

/// <summary>
/// Everything in the city that is stopped, grouped by why, assembled when somebody asks.
/// </summary>
/// <remarks>
/// <para>
/// <b>A snapshot, and it carries the Tick it was taken at because it is one.</b> Nothing refreshes
/// this; a reader who wants a later answer asks for a later answer. ***A list that looks live and is
/// not is worse than one that prints its own age***, which is what the removed floating warning layer
/// got wrong in the other direction — it re-read the whole city four times a second so that it could
/// look live, and stalled the shell doing it (<c>plans/0064</c> row 14).
/// </para>
/// <para>
/// <b>Cold and allocating</b>, on <see cref="BuildingEvidence"/>'s terms: it walks every live
/// Building's Rule Instances once. That is the bill, it is paid by a human who clicked, and no path
/// from <c>step()</c> reaches it.
/// </para>
/// <para>
/// ⚠ <b><see cref="Subjects"/> is not capped, and the cap somebody will reach for belongs in the
/// panel instead.</b> Its length is the number of subjects actually in trouble, which is the quantity
/// being asked about; truncating it here would make <see cref="Groups"/>'s counts and the list under
/// them disagree, and reconciling those two is the whole point of grouping.
/// </para>
/// </remarks>
[ColdPath("The city's blocked set, assembled when a panel asks. No path from step() reaches it.")]
public readonly struct CityEvidence
{
    internal CityEvidence(Ticks readAt, int buildingsRead, CityGroup[] groups, CitySubject[] subjects)
    {
        ReadAt = readAt;
        BuildingsRead = buildingsRead;
        Groups = groups;
        Subjects = subjects;
    }

    /// <summary>The Tick this was read at.</summary>
    public Ticks ReadAt { get; }

    /// <summary>
    /// How many live Buildings the walk visited. <b>The instrument's own denominator</b>, on the
    /// ground washes' precedent of printing how many Cells were read beside what they say.
    /// </summary>
    public int BuildingsRead { get; }

    /// <summary>
    /// The causes, worst first: trouble before routine waiting, then by how many subjects each stops.
    /// </summary>
    public ReadOnlyMemory<CityGroup> Groups { get; }

    /// <summary>
    /// Every stopped subject, in <see cref="Groups"/> order and then worst first within a group.
    /// </summary>
    public ReadOnlyMemory<CitySubject> Subjects { get; }
}
