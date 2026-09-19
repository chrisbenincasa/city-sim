namespace Borough.Formats;

/// <summary>One typed reference a source v1 declaration states, and the section it resolves into.</summary>
/// <param name="Section">The declaring section, spelled as a member writes it between the brackets.</param>
/// <param name="Key">
/// The key holding the id, from the declaration's table. A dot descends into an inline table and
/// <c>[]</c> descends into each element of an array of inline tables.
/// </param>
/// <param name="Target">The section whose ids the value is matched against.</param>
public readonly record struct RulesetSourceReference(string Section, string Key, string Target);

/// <summary>
/// Every place one source v1 declaration names another, resolved before the reader runs.
/// </summary>
/// <remarks>
/// <para>
/// <b>The resolver owns these because the reader cannot describe them in a package's terms.</b> The
/// single-file reader resolves the same references against the names it registered, and its refusals
/// say <em>name</em>, carry the enclosing inline table's line rather than the key's own, and carry no
/// column at all. A member writes <c>id</c>, so the reader's sentence names a key the author did not
/// write.
/// </para>
/// <para>
/// ⚠ <b>A section here is the table's full name, so a nested declaration such as
/// <c>hinterland.population</c> is its own entry.</b> The resolver walks a declaration's nested
/// tables alongside its own, and the diagnostic still names the owning declaration.
/// </para>
/// <para>
/// <c>RulesetSourceReferenceTests</c> holds this list against <c>RulesetLoader.KeySurface</c>, so a
/// key that stops existing or changes shape cannot leave a reference behind that nothing resolves.
/// </para>
/// </remarks>
public static class RulesetSourceReferences
{
    private static readonly RulesetSourceReference[] Declared =
    [
        new("building", "bins[].reserve.basket", "basket"),
        new("building", "bins[].reserve.profile", "reserve"),
        new("building", "bins[].resource", "resource"),
        new("building", "business", "business"),
        new("hinterland", "prices[].resource", "resource"),
        new("hinterland.population", "stage", "life_stage"),
        new("life_stage", "childless", "life_stage"),
        new("life_stage", "children_become", "life_stage"),
        new("life_stage", "next", "life_stage"),
        new("policy", "trade", "business"),
        new("policy", "transfer.resource", "resource"),
        new("rule", "basket", "basket"),
        new("rule", "fills.resource", "resource"),
        new("rule", "inputs[].resource", "resource"),
        new("rule", "kind", "building"),
        new("rule", "on_fail", "rule"),
        new("rule", "outputs[].resource", "resource"),
        new("water", "carries", "resource"),
        new("zone_rule", "kind", "building"),
    ];

    /// <summary>Every reference this build resolves, ordered by section and key.</summary>
    public static IReadOnlyList<RulesetSourceReference> All => Declared;

    /// <summary>What a declaration of <paramref name="section"/> may refer to.</summary>
    public static IEnumerable<RulesetSourceReference> For(string section)
    {
        foreach (RulesetSourceReference reference in Declared)
        {
            if (string.Equals(reference.Section, section, StringComparison.Ordinal))
            {
                yield return reference;
            }
        }
    }
}
