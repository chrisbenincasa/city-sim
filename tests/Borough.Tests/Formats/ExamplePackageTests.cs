using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// `rulesets/split/`, the package the authoring guide walks through.
/// </summary>
/// <remarks>
/// <para>
/// <b>The guide quotes this package's ids, its label and its Rule order, and a reader follows it
/// against a checkout rather than against prose.</b> An edit to the package that contradicts the
/// walkthrough is otherwise invisible until somebody types the commands and gets something else.
/// </para>
/// <para>
/// ⚠ <b>The guide's commands themselves are unheld.</b> These assertions reach the same resolver a
/// runner reaches, so they catch the package moving; nothing here catches a renamed flag or a
/// removed mode, and the guide says so where it names this class.
/// </para>
/// </remarks>
public sealed class ExamplePackageTests
{
    private static RulesetSourceResult Resolved() =>
        RulesetSource.Load(Path.Combine(AppContext.BaseDirectory, "Rulesets", "split", "ruleset.toml"));

    [Fact]
    public void The_example_package_resolves_from_its_manifest()
    {
        RulesetSourceResult result = Resolved();

        Assert.True(
            result.Ok,
            result.Ruleset is null && result.Diagnostics.Count > 0
                ? result.Diagnostics[0].ToString()
                : string.Empty);

        Assert.Equal(
            ["city.toml", "dwelling.toml", "goods.toml", "rules.toml"],
            result.Capture!.Members.Select(member => member.Path));
    }

    [Fact]
    public void The_label_reaches_the_names_and_the_id_does_not()
    {
        RulesetSourceResult result = Resolved();

        Assert.Equal("Terraced house", result.Names.Kind(1));
    }

    /// <summary>
    /// The Rules run in the order their declarations state, not the order the member writes them.
    /// </summary>
    /// <remarks>
    /// <c>rules.toml</c> writes restock, consume and upkeep in that sequence and states an explicit
    /// <c>order</c> on each. The guide's claim is that the order survives a member being split or
    /// moved, which is only true because the dense ids follow <c>(order, id)</c>.
    /// </remarks>
    [Fact]
    public void The_rules_take_the_order_their_declarations_state()
    {
        RulesetNames names = Resolved().Names;

        Assert.Equal(
            ["restock", "consume", "upkeep"],
            Enumerable.Range(1, 3).Select(id => names.Rule(new RuleId((ushort)id))));
    }
}
