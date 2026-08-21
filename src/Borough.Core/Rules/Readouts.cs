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

    /// <summary>
    /// What a Household holds, in the smallest money unit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The first Household-scoped Readout, and this file predicted it would need a second entry
    /// point rather than a wider switch.</b> Every Readout before it hangs off a Building, because
    /// the only consumer was a Bin Rule and a Bin Rule is attached to one. A Policy sweeps Households
    /// (<c>02 §4.2</c>), so the entity a Readout hangs off is now part of its declaration —
    /// <see cref="Readouts.ScopeOf"/> — and the loader refuses a Rule that names one of the wrong
    /// shape.
    /// </para>
    /// <para>
    /// ⚠ <b>It reads a Bin, which is the one line this enum's own summary draws</b> — <em>Bins are
    /// what a Rule spends; Readouts are what a Rule consults.</em> A balance is both, and the
    /// consequence is worth stating rather than smoothing over: a derived apply count taken off the
    /// same Bin the term then draws from <b>can never overdraw</b>, because <c>n</c> is a fraction of
    /// what is there. ***A Rule whose apply count is read off the Bin it spends is unfailable by
    /// construction.*** So a levy on holdings never joins a wait list and never reports bankruptcy,
    /// and it is the transfer in the other direction — paid out of a treasury that can empty — that
    /// exercises the failure surface <c>adr/0114</c> built.
    /// </para>
    /// <para>
    /// <b>It is not gross income and must not be quoted as it.</b> <c>CONTEXT</c> → Policy's worked
    /// example is <em>"15% of gross income"</em>, and income is a <em>flow</em> that arrives with
    /// wages in milestone 15. This is a <em>stock</em>. A percentage of a stock and a percentage of a
    /// flow are different instruments with different incidence, and the only reason this one is here
    /// is that it is the only money magnitude the build has (<c>adr/0070</c>: income is
    /// <b>unbuilt</b>, so nothing is being approximated — a different thing is being measured).
    /// </para>
    /// <para>
    /// <b>Zero when the Ruleset in force names no money</b>, for <c>World.BalanceOf</c>'s reason: a
    /// world with no currency and a Household with none behave identically at every call site money
    /// has, and a Rule reading zero applies zero times, which is a success that does nothing rather
    /// than the silent non-event <c>02 §4.1</c> bans.
    /// </para>
    /// </remarks>
    Balance = 2,
}

/// <summary>
/// Which entity a <see cref="Readout"/> hangs off, and therefore which Rule family may name it.
/// </summary>
/// <remarks>
/// <b>Part of a Readout's declaration rather than a property of its consumer.</b>
/// <c>02 §4.1</c> attaches a Bin Rule to a Building and <c>02 §4.2</c> attaches a Policy to a
/// population, so <em>occupancy</em> and <em>balance</em> are not two values of one thing — they are
/// scalars of different entities, and a Rule naming the wrong one has no row to read it from. The
/// loader refuses the mismatch by name, which is <c>adr/0048</c>'s division of labour: the loader
/// refuses an unusable <em>name</em>, the interpreter refuses an unusable <em>id</em>.
/// </remarks>
public enum ReadoutScope : byte
{
    /// <summary>Read against a Building row. Every Readout declared before milestone 10 task 5.</summary>
    Building = 0,

    /// <summary>Read against a Household row.</summary>
    Household = 1,
}

/// <summary>
/// Reads a declared <see cref="Readout"/>, and enumerates the declared set.
/// </summary>
/// <remarks>
/// <b>The entity a Readout hangs off is part of its declaration</b> — <see cref="ScopeOf"/> — and the
/// two scopes have <b>two entry points</b> rather than one switch, which is what this class predicted
/// it would need before there was a second scope to need it. <see cref="Read"/> takes a Building row
/// and <see cref="ReadHousehold"/> a Household row; a single method taking an <c>(entity kind, slot)</c>
/// pair would be two switches wearing one signature and would let a Building slot be read as a
/// Household. The scalars <c>CONTEXT</c> still names and this build still lacks — gross income, time
/// unemployed, experience — are Household-scoped and arrive with the mechanisms that produce them.
/// </remarks>
public static class Readouts
{
    private static readonly Readout[] DeclaredSet = [Readout.Occupancy, Readout.Balance];

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

    /// <summary>Which entity <paramref name="id"/> is read against.</summary>
    /// <remarks>
    /// <b>An undeclared id is <see cref="ReadoutScope.Building"/> rather than a throw</b>, and the
    /// asymmetry with <see cref="Read"/> is deliberate: this is asked by the loader, which is deciding
    /// whether to <em>refuse</em>, and a validator that throws on the input it exists to reject turns
    /// a refusal with a file and a line into a crash. The unreadable id is refused a line later, by
    /// the check that already exists for it.
    /// </remarks>
    public static ReadoutScope ScopeOf(ReadoutId id) =>
        (Readout)id.Raw == Readout.Balance ? ReadoutScope.Household : ReadoutScope.Building;

    /// <summary>Reads a Household-scoped Readout.</summary>
    /// <remarks>
    /// <b>A second entry point rather than a wider switch</b>, which this class's own summary called
    /// for before there was anything to put in it. The two take different rows and share no case, so
    /// one method taking an <c>(entity kind, slot)</c> pair would be two switches wearing one
    /// signature and would let a Building slot be read as a Household.
    /// </remarks>
    /// <param name="world">The tables to read.</param>
    /// <param name="household">The Household row the Policy is sweeping.</param>
    /// <param name="id">A declared Household-scoped Readout.</param>
    public static long ReadHousehold(World world, int household, ReadoutId id)
    {
        ArgumentNullException.ThrowIfNull(world);

        switch ((Readout)id.Raw)
        {
            case Readout.Balance:
                return world.BalanceOf(world.Households.Rows.At(household)).Raw;

            case Readout.Occupancy:
            case Readout.None:
            default:
                throw new InvalidOperationException(
                    $"readout id {id.Raw} is not a Household-scoped Readout. The scope is part of a "
                    + "Readout's declaration (Readouts.ScopeOf) and the loader refuses a Rule that "
                    + "names one of the wrong shape, so reaching here means a Ruleset was built in "
                    + "code rather than loaded.");
        }
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
    public static long Read(World world, int building, ReadoutId id)
    {
        ArgumentNullException.ThrowIfNull(world);

        switch ((Readout)id.Raw)
        {
            case Readout.Occupancy:
                return Count(world.Occupants, building);

            case Readout.Balance:
                throw new InvalidOperationException(
                    $"readout id {id.Raw} is declared and is Household-scoped, so it has no value "
                    + "against a Building row. Use ReadHousehold. The loader refuses a [[rule]] that "
                    + "names it, so reaching here means a Ruleset was built in code rather than "
                    + "loaded.");

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
    /// A walk rather than a stored counter — see <see cref="IndexList.Length"/>, which is where the
    /// argument lives now that a second caller wanted the same walk. This method existed first and
    /// held its own copy of the loop; <c>adr/0068</c>'s occupancy guard is what made one of them a
    /// duplicate.
    /// </remarks>
    private static int Count(IndexList list, int owner) => list.Length(owner);
}
