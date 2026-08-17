namespace Borough.Formats;

using Borough.Core.Rules;
using Borough.Core.Tables;

/// <summary>
/// What the Ruleset's authored names were, kept so a shell can put words back on the ids
/// <c>Borough.Core</c> returns.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>05 §1</c>'s rule is that <c>Core</c> returns ids and numbers and <em>"the shell owns every
/// string a human reads, resolved through the Ruleset"</em> — and until milestone 6 task 5 there was
/// nothing to resolve them through.</b> The loader builds four name-to-id maps while reading a file
/// and dropped all four on the floor; <c>Ruleset</c> could not keep them, because a Ruleset is
/// <c>Core</c> and <c>Core</c> holds no strings. So the resolution path the architecture assumes
/// simply did not exist, and no reader of that sentence would have guessed it — the eight runner modes
/// shipped before this one all get away with it because <b>they print quantities rather than names</b>.
/// A grid of v/c ratios needs no vocabulary. A condemnation reason is nothing but one.
/// </para>
/// <para>
/// ⚠ <b>The corpus already had the other half of this and the two are easy to mistake for one.</b>
/// <c>Ruleset.ResourceKeys</c> and <c>KindKeys</c> are <em>content hashes</em> of the same authored
/// names, carried into <c>Core</c> under <c>adr/0048</c>'s rule that a number may cross where the name
/// it was made from may not — and that ADR says in as many words that <em>"the core never renders it
/// and never resolves a name with it; it only asks whether two declarations are the same thing."</em>
/// <b>A key answers <em>are these two the same</em>; a name answers <em>what do I call it</em>.</b> The
/// build had the first and not the second, which is why the gap survived: a reader checking whether
/// names were kept finds a table that looks like the answer and is a one-way hash.
/// </para>
/// <para>
/// <b>Here rather than in <c>Core</c>, and it is not a near miss.</b> <c>Borough.Formats</c> is the
/// project described as <em>"the artefacts that spell things in words"</em>; putting these on the
/// <c>Ruleset</c> would put <c>string</c> into simulation state, which lint 7 refuses and
/// <c>adr/0002</c> refuses for the reason that matters — a method returning a formatted string because
/// a panel wanted one is the real leak vector, not <c>using Godot;</c>. Nothing here reaches the
/// simulation: it is a by-product of parsing, it does not enter the State Hash, and a world loaded
/// from a save has none of it.
/// </para>
/// <para>
/// ⚠ <b>Every lookup can return null and that is not a defensive habit.</b> An id can outlive the name
/// that introduced it — a Building standing under a Ruleset that no longer declares its kind is
/// <c>adr/0068</c>'s <em>derelict</em>, and is a state the design has on purpose. A caller that wants
/// a word either way spells the fallback itself, because what to show for a kind nobody named is a
/// presentation decision and this is not the presentation layer.
/// </para>
/// </remarks>
public sealed class RulesetNames
{
    private static readonly string[] Nothing = [];

    private readonly string[] _kinds;
    private readonly string[] _conditions;
    private readonly string[] _resources;
    private readonly string[] _rules;

    internal RulesetNames(
        IReadOnlyDictionary<string, byte> kinds,
        IReadOnlyDictionary<string, ushort> conditions,
        IReadOnlyDictionary<string, ushort> resources,
        IReadOnlyDictionary<string, ushort> rules)
    {
        _kinds = Invert(kinds);
        _conditions = Invert(conditions);
        _resources = Invert(resources);
        _rules = Invert(rules);
    }

    private RulesetNames()
    {
        _kinds = Nothing;
        _conditions = Nothing;
        _resources = Nothing;
        _rules = Nothing;
    }

    /// <summary>A Ruleset whose names were not kept. Every lookup returns null.</summary>
    /// <remarks>
    /// <b>The honest state for a world that did not come from a file</b> — a save, or a
    /// <c>Ruleset</c> a test built by hand. Those have ids and never had names, so an empty table is
    /// the fact rather than a placeholder for one.
    /// </remarks>
    public static RulesetNames None { get; } = new();

    /// <summary>What the file called a Building kind, or null.</summary>
    public string? Kind(byte id) => At(_kinds, id);

    /// <summary>What the file called a condition, or null.</summary>
    /// <remarks>
    /// <c>ConditionId.None</c> returns null, which is correct rather than incidental: a Rule reporting
    /// nothing has no condition to name, and <em>a demolition with no sentence behind it</em> is the
    /// state every Ruleset without an <c>on_fail</c> chain is in.
    /// </remarks>
    public string? Condition(ConditionId id) => At(_conditions, id.Raw);

    /// <summary>What the file called a Resource, or null.</summary>
    public string? Resource(ResourceId id) => At(_resources, id.Raw);

    /// <summary>What the file called a Rule, or null.</summary>
    public string? Rule(RuleId id) => id.IsNone ? null : At(_rules, id.Raw);

    private static string? At(string[] table, int id) =>
        id > 0 && id < table.Length ? table[id] : null;

    /// <summary>
    /// Turns a name-to-id map into an id-indexed table.
    /// </summary>
    /// <remarks>
    /// <b>A dense array rather than the dictionary turned round</b>, because <c>05 §4</c>'s lint 3
    /// bans enumerating a hash map in simulation code and this is the pattern that stops the habit
    /// leaking across the boundary — the ids are small, dense and assigned in file order, so the array
    /// is the natural shape anyway. Index 0 is unused throughout: every id family here reserves it for
    /// <em>none</em>.
    /// </remarks>
    private static string[] Invert(IReadOnlyDictionary<string, byte> map)
    {
        int highest = 0;

        foreach (KeyValuePair<string, byte> entry in map)
        {
            if (entry.Value > highest)
            {
                highest = entry.Value;
            }
        }

        string[] table = new string[highest + 1];

        foreach (KeyValuePair<string, byte> entry in map)
        {
            table[entry.Value] = entry.Key;
        }

        return table;
    }

    private static string[] Invert(IReadOnlyDictionary<string, ushort> map)
    {
        int highest = 0;

        foreach (KeyValuePair<string, ushort> entry in map)
        {
            if (entry.Value > highest)
            {
                highest = entry.Value;
            }
        }

        string[] table = new string[highest + 1];

        foreach (KeyValuePair<string, ushort> entry in map)
        {
            table[entry.Value] = entry.Key;
        }

        return table;
    }
}
