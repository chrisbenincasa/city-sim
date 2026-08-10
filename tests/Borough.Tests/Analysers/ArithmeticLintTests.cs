using Borough.Analysers;

namespace Borough.Tests.Analysers;

/// <summary>
/// <c>05 §4</c> lint 2 — the deliberate violations, and the constructs that must survive.
/// </summary>
public class ArithmeticLintTests
{
    // ---- BOR0201, floating point ---------------------------------------------------------------

    [Theory]
    [InlineData("a double local", "        double x = 1;\n        _ = x;")]
    [InlineData("a var local inferred as double", "        var x = 2.5;\n        _ = x;")]
    [InlineData("a float temporary cast away", "        int r = (int)(3 * 1.5f);\n        _ = r;")]
    [InlineData("a decimal local", "        decimal m = 1;\n        _ = m;")]
    public void Floating_point_expressions_are_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0201", new FloatingPointAnalyser(),
            AnalyserHarness.InMethod(statements));
    }

    /// <summary>
    /// The case the reflection test in <see cref="BoundaryTests"/> structurally cannot see, and the
    /// case that motivated widening the rule from "in simulation state" to "in any arithmetic".
    /// </summary>
    [Fact]
    public void A_float_temporary_is_reported_even_though_nothing_stores_it() =>
        AnalyserHarness.Fires("BOR0201", new FloatingPointAnalyser(), """
            namespace Probe;

            internal static class Subject
            {
                internal static int Scale(int a) => (int)(a * 1.5f);
            }
            """);

    /// <summary>
    /// <b>The four doors a <c>SpecialType</c>-shaped check leaves open</b>, each easier to walk
    /// through than writing <c>double</c>. The lambda is the sharpest: no symbol action visits it,
    /// and its body holds no literal and no conversion, so nothing in the assembly saw it at all.
    /// <c>Vector2</c> is the likeliest in practice — it is what somebody writing a position reaches
    /// for, and it is <c>unmanaged</c>, so lint 7 waves it through too.
    /// </summary>
    [Theory]
    [InlineData("a generic type argument", "internal static List<double>? Samples;")]
    [InlineData("a nullable", "internal static double? Rate;")]
    [InlineData("a delegate signature", "internal static Func<double, double>? Curve;")]
    [InlineData("System.Numerics.Vector2", "internal static System.Numerics.Vector2 Position;")]
    [InlineData("System.Half", "internal static System.Half Weight;")]
    [InlineData("System.Numerics.Complex", "internal static System.Numerics.Complex Phase;")]
    public void Floating_point_hidden_inside_another_type_is_reported(string description, string member)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0201", new FloatingPointAnalyser(), $$"""
            using System;
            using System.Collections.Generic;

            namespace Probe;

            #pragma warning disable CS0649
            internal static class Subject
            {
                {{member}}
            }
            #pragma warning restore CS0649
            """);
    }

    [Theory]
    [InlineData("a lambda", "internal static Func<double, double> Curve() => v => v;")]
    [InlineData("a local function",
        "internal static int Run(int a) { double Scale(double v) => v; return (int)Scale(a); }")]
    public void Floating_point_in_a_nested_function_is_reported(string description, string member)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0201", new FloatingPointAnalyser(), $$"""
            using System;

            namespace Probe;

            internal static class Subject
            {
                {{member}}
            }
            """);
    }

    /// <summary>
    /// The structural check walks the fields of a struct. It must stop there — following a class's
    /// fields reaches floating point from nearly every type in .NET, and a rule that reports
    /// everything reports nothing.
    /// </summary>
    [Fact]
    public void Integer_quantities_and_handles_are_left_alone() =>
        AnalyserHarness.Silent(new FloatingPointAnalyser(), """
            using System;
            using System.Collections.Generic;

            namespace Probe;

            #pragma warning disable CS0649
            internal readonly record struct Handle<T>(int Index);

            internal static class Subject
            {
                internal static Handle<object> Owner;
                internal static List<int>? Counts;
                internal static Func<int, int>? Curve;
                internal static Borough.Core.Quantities.Ticks Tick;
                internal static Borough.Core.Quantities.Money Balance;
                internal static int? MaybeCount;
            }
            #pragma warning restore CS0649
            """);

    /// <summary>A lifted built-in operator has result type <c>int?</c> and no OperatorMethod.</summary>
    [Fact]
    public void A_lifted_division_is_reported() =>
        AnalyserHarness.Fires("BOR0203", new BannedArithmeticAnalyser(), """
            namespace Probe;

            internal static class Subject
            {
                internal static int? Half(int? a) => a / 2;
            }
            """);

    /// <summary>
    /// Modulo is not on `05 §4`'s banned list and must not be swept in with division: `x % n` has
    /// no rounding choice to state.
    /// </summary>
    [Fact]
    public void Modulo_is_left_alone() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), """
            namespace Probe;

            internal static class Subject
            {
                internal static int Wrap(int x, int n) => x % n;
            }
            """);

    /// <summary>
    /// A child namespace must not inherit the substrate exemption — inventing one is exactly how a
    /// single visible exemption spreads by copy.
    /// </summary>
    [Fact]
    public void A_namespace_below_the_substrate_does_not_inherit_the_exemption() =>
        AnalyserHarness.Fires("BOR0203", new BannedArithmeticAnalyser(), """
            namespace Borough.Core.Arithmetic.Nested;

            internal static class Sneaky
            {
                internal static int Divide(int a, int b) => a / b;
            }
            """);

    [Theory]
    [InlineData("a string hash", "internal static int Run(string s) => s.GetHashCode();")]
    [InlineData("HashCode.Combine",
        "internal static int Run(int a, int b) => System.HashCode.Combine(a, b);")]
    public void Process_seeded_hashes_are_reported(string description, string member)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0206", new NondeterministicApiAnalyser(), $$"""
            namespace Probe;

            internal static class Subject
            {
                {{member}}
            }
            """);
    }

    [Theory]
    [InlineData("a double field", "internal static double Rate;")]
    [InlineData("a float return type", "internal static float Rate() => 0f;")]
    [InlineData("a double parameter", "internal static void Take(double rate) { _ = rate; }")]
    [InlineData("a double array", "internal static double[]? Rates;")]
    public void Floating_point_declarations_are_reported(string description, string member)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0201", new FloatingPointAnalyser(), $$"""
            namespace Probe;

            internal static class Subject
            {
                {{member}}
            }
            """);
    }

    [Fact]
    public void Integer_and_fixed_point_arithmetic_is_left_alone() =>
        AnalyserHarness.Silent(new FloatingPointAnalyser(), AnalyserHarness.InMethod("""
                    int half = Borough.Core.Arithmetic.Fixed.One >> 1;
                    int scaled = Borough.Core.Arithmetic.Fixed.Mul(half, half);
                    _ = scaled;
            """));

    // ---- BOR0202, System.Math -----------------------------------------------------------------

    [Theory]
    [InlineData("Math.Exp", "        _ = Math.Exp(1);")]
    [InlineData("Math.Log", "        _ = Math.Log(1);")]
    [InlineData("Math.Abs, which is exact and banned anyway", "        _ = Math.Abs(-1);")]
    [InlineData("MathF", "        _ = MathF.Sqrt(1f);")]
    [InlineData("Math.PI", "        _ = Math.PI;")]
    public void Math_members_are_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0202", new BannedArithmeticAnalyser(),
            AnalyserHarness.InMethod(statements));
    }

    /// <summary>
    /// adr/0038's tables are the stated replacement, so reaching for them must not trip the lint
    /// that sent you there.
    /// </summary>
    [Fact]
    public void The_tabulated_replacement_is_left_alone() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), AnalyserHarness.InMethod("""
                    _ = Borough.Core.Arithmetic.Transcendental.Exp(Borough.Core.Arithmetic.Fixed.One);
                    _ = Borough.Core.Arithmetic.Transcendental.Log(Borough.Core.Arithmetic.Fixed.One);
            """));

    // ---- BOR0203, raw division ----------------------------------------------------------------

    [Theory]
    [InlineData("a raw quotient", "        int q = 7 / a;\n        _ = q;")]
    [InlineData("a compound divide", "        a /= 2;\n        _ = a;")]
    public void Raw_integer_division_is_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0203", new BannedArithmeticAnalyser(), $$"""
            namespace Probe;

            internal static class Subject
            {
                internal static void Run(int a)
                {
            {{statements}}
                }
            }
            """);
    }

    [Fact]
    public void The_stated_rounding_helpers_are_left_alone() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), AnalyserHarness.InMethod("""
                    _ = Borough.Core.Arithmetic.IntegerMath.FloorDiv(-7, 2);
                    _ = Borough.Core.Arithmetic.IntegerMath.CeilDiv(7, 2);
                    _ = Borough.Core.Arithmetic.Fixed.Div(1, 2);
            """));

    /// <summary>
    /// A user-defined <c>/</c> is somebody else's decision, already made under these rules —
    /// <c>Ratio</c>'s routes through <c>Fixed.Div</c>. The lint's target is the built-in operator.
    /// </summary>
    [Fact]
    public void A_user_defined_division_operator_is_left_alone() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), AnalyserHarness.InMethod("""
                    _ = Borough.Core.Quantities.Ratio.One / Borough.Core.Quantities.Ratio.One;
            """));

    /// <summary>
    /// The exemption that makes the rule satisfiable: a helper with stated rounding has to be built
    /// out of the operator it replaces.
    /// </summary>
    [Fact]
    public void The_arithmetic_substrate_may_divide_and_shift_freely() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), """
            namespace Borough.Core.Arithmetic;

            internal static class Substrate
            {
                internal static int Divide(int a, int b) => a / b;
                internal static int Shift(int a, int n) => a << n;
            }
            """);

    // ---- BOR0204, masked shift counts ---------------------------------------------------------

    [Theory]
    [InlineData("a computed left shift", "        _ = a << n;")]
    [InlineData("a computed right shift", "        _ = a >> n;")]
    [InlineData("a compound shift", "        a <<= n;\n        _ = a;")]
    public void Shifting_by_a_computed_count_is_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0204", new BannedArithmeticAnalyser(), $$"""
            namespace Probe;

            internal static class Subject
            {
                internal static void Run(int a, int n)
                {
            {{statements}}
                }
            }
            """);
    }

    /// <summary>
    /// A constant count is visible in the source and cannot vary, which is the whole of the hazard.
    /// <c>Randomness.Mix</c> is built out of these.
    /// </summary>
    [Fact]
    public void A_constant_shift_count_is_left_alone() =>
        AnalyserHarness.Silent(new BannedArithmeticAnalyser(), """
            namespace Probe;

            internal static class Subject
            {
                private const int Bits = 16;

                internal static int Run(int a) => (a << 3) ^ (a >> Bits);
            }
            """);

    // ---- BOR0205 and BOR0206, inputs the Input Log does not carry ------------------------------

    [Theory]
    [InlineData("BOR0205", "        _ = DateTime.UtcNow;")]
    [InlineData("BOR0205", "        _ = Environment.TickCount64;")]
    [InlineData("BOR0205", "        _ = System.Diagnostics.Stopwatch.GetTimestamp();")]
    [InlineData("BOR0206", "        _ = Guid.NewGuid();")]
    public void Process_local_inputs_are_reported(string expected, string statements) =>
        AnalyserHarness.Fires(expected, new NondeterministicApiAnalyser(),
            AnalyserHarness.InMethod(statements));

    /// <summary>
    /// The subtle half of BOR0206: this compiles, is spelled like a hash, and returns a different
    /// number for the same logical value in the next process.
    /// </summary>
    [Fact]
    public void The_default_GetHashCode_is_reported() =>
        AnalyserHarness.Fires("BOR0206", new NondeterministicApiAnalyser(), """
            namespace Probe;

            internal sealed class Node
            {
                internal int Key;
            }

            internal static class Subject
            {
                internal static int Run(Node node) => node.GetHashCode();
            }
            """);

    /// <summary>An override is a hash somebody wrote, which is exactly what the rule asks for.</summary>
    [Fact]
    public void An_overridden_GetHashCode_is_left_alone() =>
        AnalyserHarness.Silent(new NondeterministicApiAnalyser(), """
            namespace Probe;

            internal sealed class Node
            {
                internal int Key;

                public override int GetHashCode() => Key;

                public override bool Equals(object? other) => other is Node n && n.Key == Key;
            }

            internal static class Subject
            {
                internal static int Run(Node node) => node.GetHashCode();
            }
            """);

    /// <summary>The Tick counter is the clock, and reading it must not look like reading a clock.</summary>
    [Fact]
    public void The_tick_counter_is_left_alone() =>
        AnalyserHarness.Silent(new NondeterministicApiAnalyser(), AnalyserHarness.InMethod("""
                    var tick = new Borough.Core.Quantities.Ticks(8192);
                    _ = tick.Raw;
            """));

    // ---- BOR0207, a ratio pre-scaled in 32 bits ------------------------------------------------

    /// <summary>
    /// The shape S2's routing spike shipped in a published finding: a Q16.16 cost difference scaled
    /// to hundredths of a percent before dividing, entirely in <c>int</c>. A uniform journey is
    /// about 4.7 million in Q16.16, so the product wraps once the improvement passes ~215,000 —
    /// about 4.6% of the journey — and it wraps <i>negative</i>, deleting the largest improvements
    /// from the mean rather than reporting them.
    /// </summary>
    [Theory]
    [InlineData("the literal on the right", "        int r = ((before - after) * 10_000) / before;\n        _ = r;")]
    [InlineData("the literal on the left", "        int r = (10_000 * (before - after)) / before;\n        _ = r;")]
    [InlineData("a named constant", "        const int Scale = 10_000;\n        int r = ((before - after) * Scale) / before;\n        _ = r;")]
    [InlineData("scaled to per-mille", "        int r = ((before - after) * 1_000) / before;\n        _ = r;")]
    [InlineData("the product cast to long afterwards", "        long r = (long)((before - after) * 10_000) / before;\n        _ = r;")]
    public void A_ratio_pre_scaled_in_32_bits_is_reported(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0207", new ScaledRatioAnalyser(),
            AnalyserHarness.InMethod("        int before = 1; int after = 0;\n" + statements));
    }

    /// <summary>
    /// <b>The route in that looks like compliance with BOR0203.</b> That lint sends every division
    /// to <c>IntegerMath</c>, whose <c>FloorDiv</c>, <c>CeilDiv</c> and <c>RoundDiv</c> all have
    /// <c>int</c> overloads — so the call site reads as though the widening had been handled while
    /// the multiplication has already overflowed inside the argument. Following one rule leads
    /// directly into the other, which is the whole argument for the second rule existing.
    /// </summary>
    [Theory]
    [InlineData("FloorDiv", "IntegerMath.FloorDiv((before - after) * 10_000, before)")]
    [InlineData("CeilDiv", "IntegerMath.CeilDiv((before - after) * 10_000, before)")]
    [InlineData("RoundDiv", "IntegerMath.RoundDiv((before - after) * 10_000, before)")]
    [InlineData("Fixed.Div", "Fixed.Div((before - after) * 10_000, before)")]
    public void A_scaled_numerator_handed_to_a_rounding_helper_is_reported(
        string description, string call)
    {
        _ = description;
        AnalyserHarness.Fires("BOR0207", new ScaledRatioAnalyser(), $$"""
            using Borough.Core.Arithmetic;

            namespace Probe;

            internal static class Subject
            {
                internal static int Run(int before, int after) => {{call}};
            }
            """);
    }

    /// <summary>
    /// The fix, in both of its spellings, and the two shapes that were never the target. Widening
    /// either operand gives the product 64 bits of headroom, which is what <c>Fixed.Mul</c> already
    /// does with <c>(long)a * b</c>. A small scale factor is ordinary unit conversion rather than
    /// the pre-scaled-ratio idiom, and a division with no scaling multiplication in its numerator
    /// has nothing to overflow.
    /// </summary>
    [Theory]
    [InlineData("a long scale literal", "        long r = ((before - after) * 10_000L) / before;")]
    [InlineData("an explicit widening cast on the operand", "        long r = ((long)(before - after) * 10_000) / before;")]
    [InlineData("a long numerator", "        long wide = before - after;\n        long r = (wide * 10_000) / before;")]
    [InlineData("a scale below the threshold", "        int r = ((before - after) * 100) / before;")]
    [InlineData("no scaling at all", "        int r = (before - after) / before;")]
    [InlineData("a non-constant multiplier", "        int r = ((before - after) * after) / before;")]
    public void The_widened_and_unscaled_forms_are_left_alone(string description, string statements)
    {
        _ = description;
        AnalyserHarness.Silent(new ScaledRatioAnalyser(),
            AnalyserHarness.InMethod("        int before = 1; int after = 0;\n" + statements + "\n        _ = r;"));
    }

    /// <summary>
    /// <b>The arithmetic substrate is deliberately not exempt</b>, unlike BOR0203's. Nothing in
    /// <c>Borough.Core.Arithmetic</c> needs to overflow — it is where the widening is written — so
    /// correct substrate code does not trip this, and incorrect substrate code is precisely what no
    /// other rule can catch.
    /// </summary>
    [Fact]
    public void The_arithmetic_substrate_is_not_exempt() =>
        AnalyserHarness.Fires("BOR0207", new ScaledRatioAnalyser(), """
            namespace Borough.Core.Arithmetic;

            internal static class Probe
            {
                internal static int Scale(int part, int whole) => (part * 10_000) / whole;
            }
            """);
}
