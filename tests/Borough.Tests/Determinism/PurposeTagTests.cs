using Borough.Core.Determinism;

namespace Borough.Tests.Determinism;

/// <summary>
/// plans/0005 task 6. <b>This is the stopgap, not the detector.</b>
/// </summary>
/// <remarks>
/// adr/0003 and 02 §10 both require <see cref="PurposeTag"/> uniqueness to be a <em>build-time</em>
/// check, and a unit test is not one: it catches a duplicate when someone runs the suite, which is
/// after the code is written and possibly after values have been drawn under the wrong tag. The real
/// detector is slice 3's analyser (plans/0006). This file exists so the window is not wide open in the
/// meantime, and should be deleted when the analyser lands.
/// </remarks>
public class PurposeTagTests
{
    /// <summary>
    /// A duplicated value makes two mechanisms draw the same sequence at the same coordinates — the
    /// silent correlation 05 §4 names, which has no runtime symptom to find it by.
    /// </summary>
    [Fact]
    public void Every_purpose_tag_has_a_distinct_value()
    {
        string[] names = Enum.GetNames<PurposeTag>();
        ulong[] values = Enum.GetValues<PurposeTag>().Select(tag => (ulong)tag).ToArray();

        // Enum.GetValues deduplicates aliases, so a shorter value list *is* the duplicate.
        Assert.Equal(names.Length, values.Distinct().Count());
    }

    /// <summary>
    /// A zeroed struct field must not silently mean a real purpose, so nothing may share
    /// <see cref="PurposeTag.None"/>'s value.
    /// </summary>
    [Fact]
    public void Zero_is_reserved_for_None() =>
        Assert.Equal(nameof(PurposeTag.None), Enum.GetName((PurposeTag)0));

    /// <summary>
    /// The tag is added into a <c>u64</c> coordinate, so the enum's storage must not be a type that
    /// can carry a negative value into that addition.
    /// </summary>
    [Fact]
    public void The_enum_is_backed_by_an_unsigned_64_bit_integer() =>
        Assert.Equal(typeof(ulong), Enum.GetUnderlyingType(typeof(PurposeTag)));
}
