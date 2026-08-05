using Borough.Analysers;

namespace Borough.Tests.Analysers;

/// <summary>
/// <c>BOR0901</c> — plans/0007's acceptance criterion that <b>adding an undeclared field fails to
/// build</b>.
/// </summary>
/// <remarks>
/// The other half of that criterion needs no test because it needs no check: a column that was never
/// declared has no storage, because declaring it through <c>Rows</c> is what allocates it. What is
/// left is the route around the declaration — a bare array beside the columns — and that is what
/// these probes write.
/// </remarks>
public class TableLintTests
{
    [Theory]
    [InlineData("a bare array", "private readonly int[] _sneaky = new int[8];")]
    [InlineData("a list", "private readonly System.Collections.Generic.List<int> _queue = new();")]
    [InlineData("a scalar beside the columns", "private ulong _epoch;")]
    [InlineData("an auto-property over an array", "public int[] Positions { get; } = new int[8];")]
    public void Storage_that_is_not_a_declared_column_is_reported(string description, string member)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0901", new TableDeclarationAnalyser(), Table(member));
    }

    /// <summary>A table holding its neighbour is a relationship nothing declared. World owns those.</summary>
    [Fact]
    public void A_reference_to_another_table_is_reported() =>
        AnalyserHarness.Fires("BOR0901", new TableDeclarationAnalyser(), """
            using Borough.Core.Tables;

            namespace Probe;

            internal readonly struct Thing;

            [Table]
            internal sealed class ThingTable
            {
                private readonly ThingTable? _neighbour;

                internal ThingTable? Neighbour() => _neighbour;
            }
            """);

    [Fact]
    public void Declared_columns_and_the_tables_own_rows_are_left_alone() =>
        AnalyserHarness.Silent(new TableDeclarationAnalyser(), """
            using Borough.Core.Tables;

            namespace Probe;

            internal readonly struct Thing;
            internal readonly struct Other;

            [Table]
            internal sealed class ThingTable
            {
                private const int Limit = 4;

                private static readonly Rows<Other> Shared = new("shared", Limit);

                private readonly Rows<Thing> _rows;

                internal ThingTable(Rows<Other> others)
                {
                    _rows = new Rows<Thing>("thing", Limit);
                    Count = _rows.Saved<int>("count");
                    Link = _rows.SavedHandle("link", others);
                    Cache = _rows.Derived<int>("cache");
                    _rows.Seal();
                }

                internal Rows<Thing> Rows => _rows;

                internal Column<int> Count { get; }

                internal HandleColumn<Other> Link { get; }

                internal Column<int> Cache { get; }
            }
            """);

    /// <summary>A type that is not a table is not this lint's business.</summary>
    [Fact]
    public void An_unmarked_class_is_left_alone() =>
        AnalyserHarness.Silent(new TableDeclarationAnalyser(), """
            namespace Probe;

            internal sealed class Ordinary
            {
                private readonly int[] _values = new int[8];

                internal int First() => _values[0];
            }
            """);

    private static string Table(string member) => $$"""
        using Borough.Core.Tables;

        namespace Probe;

        internal readonly struct Thing;

        [Table]
        internal sealed class ThingTable
        {
            {{member}}
        }
        """;
}
