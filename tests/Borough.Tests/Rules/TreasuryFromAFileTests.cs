using Borough.Core.Entities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 10 task 2: <c>rulesets/monetised.toml</c>, the first shipped Ruleset in the project's
/// life to declare a conserved Resource.
/// </summary>
/// <remarks>
/// <para>
/// <b>A shipped file has to be held to something or it is a literal nothing held to a file.</b> That
/// is 5a-bis's finding about <c>minimal-tuned.toml</c>, whose stated content hash guarded a file no
/// test ever opened, and the repair takes the same shape here: the claims the file makes in its own
/// header are the assertions in this class.
/// </para>
/// <para>
/// <b>The negative half is the load-bearing one.</b> A treasury with a Bin in it is evidence that
/// <c>World.FitTreasury</c> read the Ruleset only if a treasury with <em>none</em> is what the other
/// five files produce. So it is asserted over every shipped Ruleset rather than over the new one —
/// a guard that covers one of two near-identical files is worse than no guard, because the file it
/// covers is evidence somebody thought about it.
/// </para>
/// </remarks>
public sealed class TreasuryFromAFileTests
{
    /// <summary>The file this task added.</summary>
    private const string Moneyed = "monetised.toml";

    /// <summary>Every shipped Ruleset that names no money, which is all of the others.</summary>
    public static TheoryData<string> Moneyless =>
        ["minimal.toml", "minimal-tuned.toml", "severance.toml", "congested.toml", "diagnosed.toml"];

    /// <summary>
    /// The moneyed Ruleset is <c>minimal.toml</c> with one <c>[[resource]]</c> block added, and the
    /// content is verbatim either side of it.
    /// </summary>
    /// <remarks>
    /// <b>The same guard the two golden Rulesets carry, for the same reason</b>: a Ruleset is a whole
    /// file to the loader, so <em>minimal.toml plus a patch</em> is not a thing this format can
    /// express — the file is a copy, and a copy drifts. Comments are excluded, because the two files
    /// explain themselves differently on purpose and the claim is about the <em>city</em> they
    /// describe.
    /// </remarks>
    [Fact]
    public void The_moneyed_ruleset_is_minimal_toml_plus_one_resource_and_nothing_else()
    {
        string[] plain = Content("minimal.toml");
        string[] moneyed = Content(Moneyed);

        string[] added = [.. moneyed.Except(plain, StringComparer.Ordinal)];
        string[] lost = [.. plain.Except(moneyed, StringComparer.Ordinal)];

        Assert.True(
            lost.Length == 0,
            $"{Moneyed} drops {lost.Length} content lines minimal.toml has:\n  "
            + string.Join("\n  ", lost));

        Assert.Equal<string[]>(["family = \"money\"", "name = \"money\""], [.. added.Order(StringComparer.Ordinal)]);

        Assert.Equal(plain.Length + 3, moneyed.Length);
    }

    /// <summary>
    /// A world on the moneyed Ruleset opens with one treasury Bin, empty and unbounded.
    /// </summary>
    /// <remarks>
    /// <b>This is the whole of what the file buys, and no key in it says so.</b>
    /// <c>World.FitTreasury</c> walks the Ruleset's Resources at world creation and gives the treasury
    /// one Bin per conserved one (<c>adr/0116</c>), so a three-line <c>[[resource]]</c> block is the
    /// entire vocabulary a Ruleset has for making the treasury real — there is no table to author, no
    /// kind to declare it on, and nothing to tune.
    /// </remarks>
    [Fact]
    public void A_world_on_the_moneyed_ruleset_opens_with_one_empty_unbounded_treasury_bin()
    {
        (Ruleset rules, RulesetNames names) = Load(Moneyed);
        var world = new World(1_000, rules);

        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        Assert.Single(bins);
        Assert.True(rules.IsConserved(world.Bins.Resource[bins[0]]));
        Assert.Equal("money", names.Resource(world.Bins.Resource[bins[0]]));
        Assert.Equal(0, world.Bins.LevelAt(bins[0]));
        Assert.Equal(long.MaxValue, world.Bins.Capacity[bins[0]]);
        Assert.Equal(BinOwnerKind.Treasury, world.Bins.OwnerKind[bins[0]]);
    }

    /// <summary>
    /// Every other shipped Ruleset gives the treasury no Bins at all, because none of them names
    /// money.
    /// </summary>
    /// <remarks>
    /// <b>The negative that makes the positive mean something.</b> It is also the survey milestone 10
    /// opened on, asserted rather than remembered: every <c>[[resource]]</c> in the tree declared
    /// <c>family = "good"</c>, and the only <c>family = "money"</c> anywhere was inside a loader
    /// test. If a sixth file starts naming money this fails and says which — which is the right
    /// failure, because a treasury quietly acquiring a Bin is a change to a city's conservation sum.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Moneyless))]
    public void Every_other_shipped_ruleset_gives_the_treasury_no_bins_at_all(string file)
    {
        (Ruleset rules, _) = Load(file);
        var world = new World(1_000, rules);

        int[] bins = [.. world.TreasuryBins.Walk(TreasuryTable.Slot)];

        Assert.True(
            bins.Length == 0,
            $"{file} gives the treasury {bins.Length} Bins. It declares no money Resource, so it "
            + "should give it none — and if it now does declare one, this file's own header has to "
            + "say why.");
    }

    /// <summary>A shipped Ruleset, loaded from beside the test assembly as the runner loads it.</summary>
    private static (Ruleset Rules, RulesetNames Names) Load(string file)
    {
        RulesetLoadResult result = RulesetLoader.Load(Path(file));

        Assert.True(
            result.Ok,
            $"rulesets/{file} was refused:\n  "
            + result.Describe());

        return (result.Ruleset!, result.Names);
    }

    /// <summary>A Ruleset's lines with the comments and the blanks taken out.</summary>
    private static string[] Content(string file) =>
    [
        .. File.ReadAllLines(Path(file))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#')),
    ];

    private static string Path(string file) =>
        System.IO.Path.Combine(AppContext.BaseDirectory, "Rulesets", file);
}
