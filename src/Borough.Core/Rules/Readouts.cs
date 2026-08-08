namespace Borough.Core.Rules;

using Borough.Core.Entities;
using Borough.Core.Tables;

/// <summary>
/// The declared Readout set: every named scalar a Rule is permitted to consult.
/// </summary>
/// <remarks>
/// <para>
/// <b>The set is declared in the simulation, not in the Ruleset</b> (<c>02 §4.1</c>, inverted by
/// session B). A Readout is <em>read-only</em> — never consumed, never conserved, never subscribed to
/// — which is the whole line between it and a Bin: <em>Bins are what a Rule spends; Readouts are what
/// a Rule consults.</em>
/// </para>
/// <para>
/// <b>Declaring one here is what makes it inspectable, by construction rather than by reference.</b>
/// <see cref="Readouts.Read"/> is the only way to obtain a Readout's value, and a panel calls it with
/// the same id a Rule does — so a quantity a Rule can act on is a quantity a player can be shown, and
/// there is no second path that could drift from this one. The converse does not hold: the inspectable
/// surface is much larger than this enum, and most of it is display-only.
/// </para>
/// <para>
/// <b>The set is small because the test for admitting one is narrow</b> — <em>does a Rule read it, or
/// is it only displayed?</em> Gross income, experience, time unemployed and composed fertility are all
/// named as Readouts by <c>CONTEXT</c> and none of them exists yet, so none is declared. A name the
/// loader cannot resolve is <b>refused</b> rather than defaulted, which is what keeps this honest.
/// </para>
/// </remarks>
public enum Readout : ushort
{
    /// <summary>Not a Readout. The value <see cref="ReadoutId.None"/> carries.</summary>
    None = 0,

    /// <summary>
    /// How many Households occupy this Building.
    /// </summary>
    /// <remarks>
    /// <b>Households, not Citizens</b>, because the Occupant of a Building is a Household
    /// (<c>CONTEXT</c> → Building) and a Household is what holds Needs and money (<c>04 §2</c>). A
    /// resident count is a different Readout and is not declared until a Rule reads one.
    /// </remarks>
    Occupancy = 1,
}

/// <summary>
/// Reads a declared <see cref="Readout"/>, and enumerates the declared set.
/// </summary>
/// <remarks>
/// <b>Every Readout reachable today is scoped to a Building</b>, because the only consumer that exists
/// is a Bin Rule's derived apply count and a Bin Rule is attached to a Building. The scalars
/// <c>CONTEXT</c> names — gross income, time unemployed — are Household-scoped and have no consumer
/// yet; the entity a Readout hangs off is part of its declaration and this class will need a second
/// entry point rather than a wider switch when one arrives.
/// </remarks>
public static class Readouts
{
    private static readonly Readout[] DeclaredSet = [Readout.Occupancy];

    /// <summary>
    /// Every declared Readout, which is the set a shell may enumerate to build an inspector.
    /// </summary>
    /// <remarks>
    /// <see cref="Readout.None"/> is deliberately absent: it is the absence of a Readout rather than a
    /// member of the set, and an inspector listing it would offer the player a row that reads nothing.
    /// </remarks>
    public static ReadOnlySpan<Readout> Declared => DeclaredSet;

    /// <summary>Whether <paramref name="id"/> names a declared Readout.</summary>
    public static bool IsDeclared(ReadoutId id)
    {
        foreach (Readout readout in DeclaredSet)
        {
            if ((ushort)readout == id.Raw)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads a Building-scoped Readout.</summary>
    /// <remarks>
    /// <b>An undeclared id throws rather than returning zero</b>, which is slice 6's named-hole rule
    /// and matters more here than usual: a Readout's value is an apply count, so a silent zero would
    /// be a Rule that <em>succeeds</em> doing nothing, re-arms on its rate, and produces neither a
    /// failure nor a word of Evidence for ever. That is the silent non-event <c>02 §4.1</c> bans.
    /// <c>adr/0048</c> puts this check here rather than in a second validator: the loader refuses an
    /// unknown <em>name</em>, and the interpreter refuses an unknown <em>id</em>.
    /// </remarks>
    /// <param name="world">The tables to read.</param>
    /// <param name="building">The Building row the Rule is attached to.</param>
    /// <param name="id">A declared Readout.</param>
    public static int Read(World world, int building, ReadoutId id)
    {
        ArgumentNullException.ThrowIfNull(world);

        switch ((Readout)id.Raw)
        {
            case Readout.Occupancy:
                return Count(world.Occupants, building);

            case Readout.None:
            default:
                throw new InvalidOperationException(
                    $"readout id {id.Raw} is not declared. The readable set is declared in the "
                    + "simulation and the loader refuses an unresolvable name, so reaching here means "
                    + "a Ruleset was built in code rather than loaded, or the two sets have drifted.");
        }
    }

    /// <summary>Length of an intrusive list, which is what a count Readout is.</summary>
    /// <remarks>
    /// A walk rather than a stored counter. The list is a Building's occupants, which is a handful,
    /// and a counter column would be a second place for the same fact to be wrong — <c>adr/0006</c>'s
    /// question of what keeps it true has no answer that a walk needs.
    /// </remarks>
    private static int Count(IndexList list, int owner)
    {
        int count = 0;

        foreach (int _ in list.Walk(owner))
        {
            count++;
        }

        return count;
    }
}
