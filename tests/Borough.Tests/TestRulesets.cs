using Borough.Core.Rules;
using Borough.Core.Tables;

namespace Borough.Tests;

/// <summary>
/// Rulesets built in code, for fixtures that need one property of a Ruleset and none of the rest.
/// </summary>
/// <remarks>
/// <b>It exists because <c>adr/0114</c> made money a property of the file.</b> A balance is a Bin and a
/// Bin exists only for a Resource a Ruleset declares, so every fixture that puts money in a world now
/// has to say so in its Ruleset — and three of them reached for the same nine-line literal within an
/// hour of each other. ***A quantity made conditional on the Ruleset turns every fixture's Ruleset into
/// a statement, and identical statements want one home before they are copies.***
/// </remarks>
internal static class TestRulesets
{
    /// <summary>
    /// <see cref="Ruleset.Empty"/> with a money Resource and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>For fixtures that need a balance and not a city.</b> It declares no kind, so no Building
    /// acquires Bins and no Rule fires; what a world on it gains over <see cref="Ruleset.Empty"/> is
    /// one treasury Bin and one balance per actor, which is exactly <c>World.FitTreasury</c> and
    /// <c>World.FitBalances</c> and nothing else.
    /// </para>
    /// <para>
    /// ⚠ <b>It is not a substitute for a shipped Ruleset and the difference is measurable.</b>
    /// <c>rulesets/minimal.toml</c> states a <c>[roads]</c> table, so a world on it generates a Road
    /// Graph at construction and <c>Invariant.VacantLotHasFrontage</c> stops being vacuous — which the
    /// hand-built golden world cannot satisfy, because its Lots were placed before this project had
    /// roads. ***A Ruleset is not a bag of settings a fixture can take one of***; loading a bigger one
    /// to obtain a small property turns on every mechanism between.
    /// </para>
    /// </remarks>
    internal static readonly Ruleset MoneyOnly = new(
        resources: [ResourceFamily.Money],
        rules: [],
        kinds: [],
        inputs: [],
        outputs: [],
        emissions: [],
        bins: [],
        kindRules: [],
        zoneRules: []);

    /// <summary>
    /// One kind holding <b>two Bins the premises keep</b>, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It exists because <c>adr/0141</c> took the shipped city's only two-Bin Building away.</b>
    /// Every shipped dwelling declares <c>sundries</c> and <c>repairs</c>, and milestone 25 moved
    /// <c>sundries</c> to the tenant — so a Building now holds <b>one</b> Bin in every world the
    /// simulation builds on its own, and <c>bin.bin_next</c> — the link in a Building's own Bin list —
    /// stopped being written anywhere. ⚠ <b><c>DerivedRebuildAuditTests</c> caught it on the first
    /// run</b>, which is exactly what that test is for: the column is still derived, still rebuilt and
    /// still correct, and nothing was left to prove it.
    /// </para>
    /// <para>
    /// <b>A fixture rather than a second Bin on a shipped kind.</b> What a shipped Ruleset declares is
    /// <em>content</em>, and adding a Bin to one so that a test has something to walk would be tuning
    /// the city to suit the instrument. ⚠ <b>Both Bins are the premises'</b> — the default, spelled by
    /// absence — because the column under test is the <em>Building's</em> list and a tenant's Bins hang
    /// off a different one.
    /// </para>
    /// </remarks>
    internal static readonly Ruleset Stocked = new(
        resources: [ResourceFamily.Money, ResourceFamily.Good, ResourceFamily.Good],
        rules: [],
        kinds: [new KindDefinition(BinFirst: 0, BinCount: 2, RuleFirst: 0, RuleCount: 0)],
        inputs: [],
        outputs: [],
        emissions: [],
        bins:
        [
            new BinDeclaration(new ResourceId(2), BinCapacity.Of(48)),
            new BinDeclaration(new ResourceId(3), BinCapacity.Of(4)),
        ],
        kindRules: [],
        zoneRules: []);
}
