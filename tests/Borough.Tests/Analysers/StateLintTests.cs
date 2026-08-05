using Borough.Analysers;

namespace Borough.Tests.Analysers;

/// <summary>
/// <c>05 §4</c> lints 3 and 7, and the <c>purpose_tag</c> row — the deliberate violations, and the
/// constructs each rule must leave alone.
/// </summary>
public class StateLintTests
{
    // ---- BOR0301, walking a hash map ----------------------------------------------------------

    [Theory]
    [InlineData("foreach over a Dictionary", "        foreach (var pair in map) { _ = pair; }")]
    [InlineData("foreach over the keys", "        foreach (var key in map.Keys) { _ = key; }")]
    [InlineData("LINQ over a Dictionary", "        _ = map.Select(p => p.Value).ToArray();")]
    [InlineData("GetEnumerator by hand", "        _ = map.GetEnumerator();")]
    public void Enumerating_a_dictionary_is_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0301", new HashMapOrderAnalyser(), Subject(statements));
    }

    [Fact]
    public void Enumerating_a_hash_set_is_reported() =>
        AnalyserHarness.Fires("BOR0301", new HashMapOrderAnalyser(), """
            using System.Collections.Generic;

            namespace Probe;

            internal static class Subject
            {
                internal static int Run(HashSet<int> set)
                {
                    int total = 0;
                    foreach (int value in set) { total += value; }
                    return total;
                }
            }
            """);

    /// <summary>
    /// <b>The acceptance criterion that keeps the rule usable.</b> A dictionary keyed by a Ruleset
    /// name and read at lookup is deterministic; only its <em>order</em> is not. A lint that banned
    /// the type would be worked around rather than obeyed.
    /// </summary>
    [Fact]
    public void Building_and_looking_up_a_dictionary_is_left_alone() =>
        AnalyserHarness.Silent(new HashMapOrderAnalyser(), """
            using System.Collections.Generic;

            namespace Probe;

            internal static class Subject
            {
                internal static int Run()
                {
                    var map = new Dictionary<string, int> { ["a"] = 1 };
                    map["b"] = 2;
                    return map.TryGetValue("a", out int found) && map.ContainsKey("b") ? found : 0;
                }
            }
            """);

    /// <summary>
    /// Order from a comparer is reproducible; order from a hash is not. The rule is about the
    /// second, and banning the first would cost a legitimate structure for nothing.
    /// </summary>
    [Fact]
    public void Walking_a_sorted_dictionary_is_left_alone() =>
        AnalyserHarness.Silent(new HashMapOrderAnalyser(), """
            using System.Collections.Generic;

            namespace Probe;

            internal static class Subject
            {
                internal static int Run(SortedDictionary<int, int> map)
                {
                    int total = 0;
                    foreach (var pair in map) { total += pair.Value; }
                    return total;
                }
            }
            """);

    /// <summary>
    /// <c>FrozenDictionary</c> is what .NET 8+ guidance points you at for a read-mostly lookup
    /// table — which is exactly the use this diagnostic blesses — so it is exactly the type someone
    /// obeying the rule would reach for, and then walk.
    /// </summary>
    [Fact]
    public void Enumerating_a_frozen_dictionary_is_reported() =>
        AnalyserHarness.Fires("BOR0301", new HashMapOrderAnalyser(), """
            using System.Collections.Frozen;

            namespace Probe;

            internal static class Subject
            {
                internal static int Run(FrozenDictionary<string, int> map)
                {
                    int total = 0;
                    foreach (var pair in map) { total += pair.Value; }
                    return total;
                }
            }
            """);

    /// <summary>A subclass must not launder the ban.</summary>
    [Fact]
    public void Enumerating_a_derived_dictionary_is_reported() =>
        AnalyserHarness.Fires("BOR0301", new HashMapOrderAnalyser(), """
            using System.Collections.Generic;

            namespace Probe;

            internal sealed class Index : Dictionary<string, int>;

            internal static class Subject
            {
                internal static int Run(Index map)
                {
                    int total = 0;
                    foreach (var pair in map) { total += pair.Value; }
                    return total;
                }
            }
            """);

    // ---- BOR0302, System.Random ---------------------------------------------------------------

    [Theory]
    [InlineData("a construction", "        var rng = new Random(1);\n        _ = rng.Next();")]
    [InlineData("the shared instance", "        _ = Random.Shared.Next();")]
    public void System_random_is_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0302", new NondeterministicApiAnalyser(),
            AnalyserHarness.InMethod(statements));
    }

    [Fact]
    public void A_field_typed_as_random_is_reported() =>
        AnalyserHarness.Fires("BOR0302", new NondeterministicApiAnalyser(), """
            using System;

            namespace Probe;

            internal sealed class Subject
            {
                private Random? _rng;

                internal void Set(Random rng) => _rng = rng;
            }
            """);

    [Fact]
    public void The_counter_based_draw_is_left_alone() =>
        AnalyserHarness.Silent(new NondeterministicApiAnalyser(), AnalyserHarness.InMethod("""
                    _ = Borough.Core.Determinism.Randomness.Draw(
                        Borough.Core.Determinism.WorldKey.FromSeed(1),
                        entityId: 7,
                        tick: new Borough.Core.Quantities.Ticks(3),
                        purpose: Borough.Core.Determinism.PurposeTag.None);
            """));

    // ---- BOR0701, reference types in simulation state -----------------------------------------

    [Theory]
    [InlineData("a List field", "private List<int> _waiting;")]
    [InlineData("an array field", "private int[] _waiting;")]
    [InlineData("a string field", "private string _name;")]
    [InlineData("an interface field", "private System.IComparable _key;")]
    public void A_reference_field_on_a_struct_is_reported(string description, string field)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0701", new UnmanagedStateAnalyser(), $$"""
            using System.Collections.Generic;

            namespace Probe;

            #pragma warning disable CS0169
            internal struct BinRow
            {
                {{field}}
            }
            #pragma warning restore CS0169
            """);
    }

    /// <summary>
    /// The companion rule that makes lint 7 satisfiable: a head index on the owner and a
    /// <c>next</c> index on the element, both in flat arrays, never a per-entity collection.
    /// </summary>
    [Fact]
    public void An_intrusive_index_list_is_left_alone() =>
        AnalyserHarness.Silent(new UnmanagedStateAnalyser(), """
            namespace Probe;

            internal struct BinRow
            {
                internal int FirstWaiting;
                internal int WaitingCount;
            }

            internal struct CitizenRow
            {
                internal int NextWaiting;
            }
            """);

    /// <summary>
    /// <b>The rule-7 exception, exercised.</b> The axis is hot/cold, not a list of type names: the
    /// hot path runs inside <c>step()</c> every Tick and holds no references; the cold path runs on
    /// a click and may. adr/0036 owed this enumeration and named these two candidates.
    /// </summary>
    [Theory]
    [InlineData("the Ruleset interpreter", "loaded from data and reloaded per adr/0015; no path from step() reaches it")]
    [InlineData("the Evidence surface", "assembled when a panel asks; 02 §9, cold and UI-facing")]
    public void A_documented_cold_path_exception_is_left_alone(string description, string reason)
    {
        _ = description;
        AnalyserHarness.Silent(new UnmanagedStateAnalyser(), $$"""
            using Borough.Core;

            namespace Probe;

            #pragma warning disable CS0169
            [ColdPath("{{reason}}")]
            internal struct Node
            {
                private string _name;
            }
            #pragma warning restore CS0169
            """);
    }

    /// <summary>
    /// <b>Containment does not inherit the exception.</b> <c>ColdPathAttribute</c> declares
    /// <c>Inherited = false</c> and requires a written reason at the point of use; a struct nested
    /// inside a cold one taking the exemption for free contradicts both. Nesting is a plausible way
    /// to organise Ruleset and Evidence types, so this is reachable rather than hypothetical.
    /// </summary>
    [Fact]
    public void A_struct_nested_inside_a_cold_path_type_must_argue_for_itself() =>
        AnalyserHarness.Fires("BOR0701", new UnmanagedStateAnalyser(), """
            using Borough.Core;

            namespace Probe;

            #pragma warning disable CS0169
            [ColdPath("assembled when a panel asks; 02 §9")]
            internal struct Surface
            {
                private string _caption;

                internal struct Row
                {
                    private string _cell;
                }
            }
            #pragma warning restore CS0169
            """);

    /// <summary>
    /// The shapes slice 4 is built out of: a phantom typed handle, a positional row, and an
    /// intrusive index list. None may fire, or the analyser condemns the code it was written to
    /// shape.
    /// </summary>
    [Fact]
    public void The_typed_table_shapes_are_left_alone() =>
        AnalyserHarness.Silent(new UnmanagedStateAnalyser(), """
            using Borough.Core.Quantities;

            namespace Probe;

            internal readonly record struct Handle<T>(int Index);

            internal sealed class Citizen;

            internal readonly record struct CitizenRow(
                Handle<Citizen> Self, Money Balance, Ticks WakesAt, int NextWaiting);
            """);

    /// <summary>
    /// A <c>ref struct</c> cannot be a field, an array element or a generic argument, so it cannot
    /// be state. Banning it would ban <c>Span&lt;T&gt;</c>, which adr/0036 names as the systems
    /// dialect the core is written in.
    /// </summary>
    [Fact]
    public void A_ref_struct_is_left_alone() =>
        AnalyserHarness.Silent(new UnmanagedStateAnalyser(), """
            using System;

            namespace Probe;

            internal ref struct Window
            {
                internal Span<int> Values;
            }
            """);

    // ---- BOR0801 to BOR0803, the purpose_tag row ----------------------------------------------

    [Fact]
    public void A_duplicated_purpose_tag_value_is_reported() =>
        AnalyserHarness.Fires("BOR0801", new PurposeTagAnalyser(), PurposeTag("""
                None = 0,
                JobChoice = 1,
                ShopChoice = 1,
            """), referenceCore: false);

    [Fact]
    public void Claiming_zero_for_something_other_than_None_is_reported() =>
        AnalyserHarness.Fires("BOR0802", new PurposeTagAnalyser(), PurposeTag("""
                JobChoice = 0,
            """), referenceCore: false);

    [Fact]
    public void A_narrower_backing_type_is_reported() =>
        AnalyserHarness.Fires("BOR0803", new PurposeTagAnalyser(), """
            namespace Borough.Core.Determinism;

            public enum PurposeTag : int
            {
                None = 0,
            }
            """, referenceCore: false);

    /// <summary>
    /// Implicit numbering is the case a careless reading would miss: nothing here writes a value at
    /// all, and every value is still distinct.
    /// </summary>
    [Fact]
    public void Distinct_tags_are_left_alone() =>
        AnalyserHarness.Silent(new PurposeTagAnalyser(), PurposeTag("""
                None = 0,
                JobChoice,
                ShopChoice,
                MoveOut,
            """), referenceCore: false);

    private static string PurposeTag(string members) => $$"""
        namespace Borough.Core.Determinism;

        public enum PurposeTag : ulong
        {
        {{members}}
        }
        """;

    private static string Subject(string statements) => $$"""
        using System.Collections.Generic;
        using System.Linq;

        namespace Probe;

        internal static class Subject
        {
            internal static void Run(Dictionary<string, int> map)
            {
        {{statements}}
            }
        }
        """;
}
