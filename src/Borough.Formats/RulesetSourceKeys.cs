namespace Borough.Formats;

/// <summary>One key a source v1 declaration states, with the shape and the sentence it carries.</summary>
public readonly record struct RulesetSourceKey(string Name, RulesetKeyKind Kind, string Note);

/// <summary>
/// The keys a source v1 declaration states, which the single-file reader never sees.
/// </summary>
/// <remarks>
/// <para>
/// <b><c>RulesetSource</c> lowers <c>id</c>, <c>label</c> and <c>order</c> out of a member before the
/// reader runs</b> — <c>id</c> becomes <c>name</c> and the other two are blanked — so
/// <c>RulesetLoader.KeySurface</c> records what the reader asked for and can never record these. An
/// editor completing a package member against the loader's surface alone offers <c>name</c>, which
/// source v1 refuses, and offers nothing for the key that replaced it.
/// </para>
/// <para>
/// <b>Published here because this is the code that reads them, on <see cref="RulesetKeyNotes"/>'s
/// arrangement one step over.</b> The note belongs to the key rather than to the section, since one
/// <c>id</c> means the same thing in every declaration — so a per-section table would hold forty
/// copies of one sentence and <c>RulesetKeyNoteTests</c> would hold every one of them against a
/// loader that reads none.
/// </para>
/// </remarks>
public static class RulesetSourceKeys
{
    private static readonly string[] OrderedSections = ["policy", "rule", "zone_rule"];

    /// <summary>The sections whose declarations may state an explicit <c>order</c>.</summary>
    /// <remarks>
    /// Every other section orders by id. Execution order is a designer's decision only where the
    /// order changes what the city does, which is the three families the engine runs in sequence.
    /// </remarks>
    public static IReadOnlyList<string> Ordered => OrderedSections;

    /// <summary>Whether a declaration of <paramref name="section"/> may state an <c>order</c>.</summary>
    public static bool Orders(string section) => Array.IndexOf(OrderedSections, section) >= 0;

    private static readonly RulesetSourceKey Id = new(
        "id",
        RulesetKeyKind.Quoted,
        "Identifies the declaration across the package. Other members refer to it by this, and the "
        + "resolver folds it into the identity key; it is matched exactly and never shown to a "
        + "player. A package member states it where a single-file Ruleset states `name`, and stating "
        + "both is refused.");

    private static readonly RulesetSourceKey Label = new(
        "label",
        RulesetKeyKind.Quoted,
        "The display text for this declaration. It reaches the shell's names and nothing else, so "
        + "changing it renames what a player reads without moving any reference. A package member "
        + "states it; omitting it leaves the declaration with no display text of its own.");

    private static readonly RulesetSourceKey Order = new(
        "order",
        RulesetKeyKind.Whole,
        "Places this declaration in execution order ahead of a higher one. A package member states "
        + "it, and declarations sharing an order or stating none fall back to id order. A "
        + "single-file Ruleset orders these by declaration position instead.");

    /// <summary>What a declaration of the array-of-tables <paramref name="section"/> states.</summary>
    /// <remarks>
    /// <paramref name="section"/> is the bare name a file writes between the double brackets, so a
    /// caller holding the loader's context spelling strips them first. Which sections hold
    /// declarations at all is the caller's question: the schema knows them by their repeat marker
    /// and the reference page by its section spelling.
    /// </remarks>
    public static IReadOnlyList<RulesetSourceKey> For(string section) =>
        Orders(section) ? [Id, Label, Order] : [Id, Label];
}
