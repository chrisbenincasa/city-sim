namespace Borough.Formats;

/// <summary>What shape of value a reader expected when it asked a table for a key.</summary>
/// <remarks>
/// <para>
/// <b>Recorded at the ask, because the ask is the only place the type is known.</b>
/// <c>RulesetLoader.Find</c> is the single funnel every key lookup goes through and it records the
/// key name; the expected type is one frame up, in whichever reader called it. So the kind travels
/// down as an argument rather than being inferred from what the file happened to contain — ***a
/// type read off a value describes that file, and a type read off the reader describes the
/// format.***
/// </para>
/// <para>
/// ⚠ <b><see cref="Unknown"/> is the default and is not a defect.</b> Most keys are read by
/// <c>TryString</c> or <c>TryInteger</c> and are typed for free; a handful reach <c>Find</c> from a
/// helper that only wants a line number for a refusal. Those record the key and say nothing about
/// its type, which is honest — and a generated schema that omits <c>type</c> still completes the
/// key.
/// </para>
/// </remarks>
public enum RulesetKeyKind
{
    /// <summary>Asked for, with no expectation recorded.</summary>
    Unknown = 0,

    /// <summary>
    /// A whole number — <c>TryInteger</c>, whose own refusal says <em>must be a whole number</em>.
    /// The loader has no float accessor and refuses decimals outright.
    /// </summary>
    Whole = 1,

    /// <summary>
    /// A quoted string — <c>TryString</c>, whose refusal says <em>must be a quoted string</em>.
    /// </summary>
    Quoted = 2,

    /// <summary>An inline table, as <c>apply</c> and <c>fills</c> are.</summary>
    Table = 3,

    /// <summary>An array of inline tables, as <c>inputs</c>, <c>bins</c> and <c>prices</c> are.</summary>
    Array = 4,
}
