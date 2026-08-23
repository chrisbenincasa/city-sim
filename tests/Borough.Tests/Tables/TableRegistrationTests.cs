using System.Reflection;

using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Tests.Tables;

/// <summary>
/// Every <c>[Table]</c> that holds saved state is in the array the State Hash walks.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>It exists because milestone 25 task 5 shipped a saved table that was not, and the whole
/// assertion tier passed.</b> <c>UnpremisedTable</c> declared two <c>Rows.Saved</c> columns and was
/// left out of <c>World._tables</c>; 2,074 tests were green, no golden hash moved, and nothing could
/// have noticed — ***a fact nothing folds cannot disagree with anything.***
/// </para>
/// <para>
/// <b>The gap is one level up from the one the corpus already guards.</b> <c>CLAUDE.md</c>'s rule is
/// that declaring a field through <c>Rows.Saved</c> is what allocates it, <em>so the State Hash cannot
/// have a coverage hole</em>. That is true per column and only per column: it guarantees a saved
/// column is folded <b>if its table is walked</b>, and says nothing about whether it is.
/// <see cref="DerivedRebuildAuditTests"/> is the same shape for the rebuild side —
/// ***a structure outside the world is not derived state however it is declared*** — and this is its
/// sibling for the hash side.
/// </para>
/// <para>
/// ⚠ <b>The check is by row type rather than by name</b>, because the name a table passes to
/// <c>Rows&lt;T&gt;</c> is a string nobody validates, and matching on it would pass for a table that
/// registered a <em>different</em> table's rows.
/// </para>
/// </remarks>
public sealed class TableRegistrationTests
{
    /// <summary>
    /// Tables that are deliberately absent, each with the reason it is.
    /// </summary>
    /// <remarks>
    /// <b><c>TreasuryTable</c> is the only one and milestone 10 task 1 argued it</b>: both its
    /// <em>declared</em> columns are <c>Derived</c>, and <c>Rows.Fold</c> folds the allocator's
    /// scalars <em>before</em> consulting any column's disposition — so a wholly-derived table in the
    /// array would hash its own allocation history rather than any state. It is safe outside the array
    /// only because its one row is allocated in the constructor and never freed, which makes that
    /// contribution a constant. ⚠ <b>An entry here is a claim that the table declares no saved
    /// column</b>, and the test below checks the claim rather than trusting it.
    /// </remarks>
    private static readonly string[] Excused = ["TreasuryTable"];

    /// <summary>
    /// The allocator's own columns, which <c>Rows</c> declares on <b>every</b> table as <c>Saved</c>.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Excluding them is the difference between this test working and it being vacuous, and the
    /// first draft got it wrong.</b> <c>Rows.Columns</c> includes <c>id</c>, <c>generation</c> and
    /// <c>free_next</c> — allocator bookkeeping, saved on every table in the project — so a check for
    /// <em>no saved columns at all</em> fails for every table including the ones that are correct.
    /// ⚠ <b>They are exactly the scalars <c>Rows.Fold</c> folds BEFORE consulting any column's
    /// disposition</b>, which is the mechanism <c>TreasuryTable</c>'s exclusion argument turns on:
    /// ***a wholly-derived table in <c>_tables</c> would hash its own allocation history rather than
    /// any state.*** So the names here are not an inconvenience being worked around; they are the
    /// thing that argument is about.
    /// </remarks>
    private static readonly string[] Allocator = ["id", "generation", "free_next"];

    [Fact]
    public void Every_saved_table_is_in_the_array_the_state_hash_walks()
    {
        var world = new World(1_000, Golden.GoldenFixtures.Rules());

        HashSet<Type> registered = [];
        foreach (Rows rows in world.Tables)
        {
            registered.Add(rows.GetType());
        }

        List<string> missing = [];

        foreach (Type table in Declared())
        {
            PropertyInfo? property = table.GetProperty("Rows");

            if (property is null || !typeof(Rows).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }

            if (Excused.Contains(table.Name) || registered.Contains(property.PropertyType))
            {
                continue;
            }

            missing.Add(table.Name);
        }

        Assert.True(
            missing.Count == 0,
            $"these [Table] types are not in World._tables: {string.Join(", ", missing)}. A table "
            + "outside that array is state the State Hash has agreed not to look at -- it is saved, it "
            + "is reloaded, and no replay, golden baseline or save/reload test can see it disagree. "
            + "Append it to _tables with a comment saying what it carries, or add it to Excused here "
            + "with the argument that it holds no saved state.");
    }

    /// <summary>
    /// An excused table really does hold no saved state, so the excuse cannot rot.
    /// </summary>
    /// <remarks>
    /// <b>Without this the allow-list is a way to silence the check above.</b> A table excused as
    /// wholly derived that later gains one <c>Rows.Saved</c> column would keep its excuse and lose its
    /// justification, ***which is the failure mode of every allow-list that is not itself checked.***
    /// </remarks>
    [Fact]
    public void An_excused_table_holds_no_saved_state()
    {
        var world = new World(1_000, Golden.GoldenFixtures.Rules());

        foreach (string name in Excused)
        {
            Type table = Declared().Single(candidate => candidate.Name == name);
            PropertyInfo property = table.GetProperty("Rows")!;

            object? owner = Owner(world, table);
            Assert.NotNull(owner);

            var rows = (Rows)property.GetValue(owner)!;

            List<string> saved = [];
            foreach (Column column in rows.Columns)
            {
                if (column.Disposition == Disposition.Saved && !Allocator.Contains(column.Name))
                {
                    saved.Add(column.Name);
                }
            }

            Assert.True(
                saved.Count == 0,
                $"{name} is excused from World._tables on the grounds that it declares no saved "
                + $"column, and it now declares: {string.Join(", ", saved)}. Either those are "
                + "genuinely derived and should say so, or this table has become saved state and must "
                + "join _tables.");
        }
    }

    /// <summary>Every <c>[Table]</c> type in the simulation assembly.</summary>
    private static IEnumerable<Type> Declared() =>
        typeof(World).Assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<TableAttribute>() is not null)
            .OrderBy(type => type.Name, StringComparer.Ordinal);

    /// <summary>The instance of <paramref name="table"/> this World holds, found by walking it.</summary>
    private static object? Owner(World world, Type table)
    {
        foreach (PropertyInfo property in typeof(World).GetProperties())
        {
            if (property.PropertyType == table)
            {
                return property.GetValue(world);
            }
        }

        return null;
    }
}
