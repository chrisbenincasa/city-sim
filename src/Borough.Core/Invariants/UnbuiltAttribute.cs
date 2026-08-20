namespace Borough.Core.Invariants;

/// <summary>
/// Marks an <see cref="Invariant"/> that the corpus has specified and the build has not implemented,
/// naming what owes it.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists so that an obligation can be <em>declared</em> without being <em>closed</em>, which is
/// the whole of <c>plans/0012</c> check 7.</b> That check was filed because <i>parking occupancy is
/// conserved</i> was specified in four documents and built in none — ***an obligation with no member
/// reads as absent rather than as owed***. A member marked here reads as owed.
/// </para>
/// <para>
/// ⚠ <b>It must not be read as a promise that the invariant is writable yet.</b>
/// <c>adr/0084</c> finds that an invariant over <em>absent</em> state cannot be written at all —
/// <i>zero is a value; undefined is not</i> — so several of these cannot become code until the state
/// they quantify over exists. Marking the gap is the point; closing it is a milestone's.
/// </para>
/// <para>
/// <b>This is <see cref="ObsoleteAttribute"/>'s sibling and not its synonym.</b> <c>[Obsolete]</c>
/// marks an id that has been <em>retired</em> — <see cref="Invariant.HouseholdHomeExists"/>, superseded
/// and never reused, because an id travels in a crash artifact. This marks one that has never been
/// live. Check 7 asserts every member is exactly one of the three: referenced by the build, retired, or
/// marked here.
/// </para>
/// </remarks>
/// <param name="owedBy">
/// What would close it — a milestone and task where one owns it, and otherwise a plain statement that
/// nothing does. ⚠ <b>An honest "nothing owns this" is the more valuable value of the two</b>, because
/// it is the case no board or roadmap is showing anybody.
/// </param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class UnbuiltAttribute(string owedBy) : Attribute
{
    /// <summary>What would close the gap, or a statement that nothing owns it.</summary>
    public string OwedBy { get; } = owedBy;
}
