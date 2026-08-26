using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// Milestone 26 task 3: <c>rulesets/provisioned.toml</c> — <b>it loads and it does not run.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>The asymmetry IS the acceptance test, and it is the only test in the suite whose passing
/// condition includes a throw from production code.</b> <c>RulesetLoader.TryScope</c> accepts
/// <c>pool</c>; <c>RuleEngine.Bin</c> throws <see cref="NotSupportedException"/> on
/// <see cref="Scope.Pool"/>. So a file with a <c>pool</c> term parses, validates and refuses to step
/// — and that is exactly the state milestone 26 task 3 ships, because the Ruleset is written before
/// the mechanism that runs it.
/// </para>
/// <para>
/// <b>What it buys is that the hole is NAMED rather than merely absent.</b> A Ruleset nobody could
/// write would leave *does the loader agree with the engine about what a pool term is* unanswered
/// until task 4, and the two disagreeing by one scope is the only thing standing between this file
/// and a running market. ***When task 4 resolves the scope, this file starts working with no edit to
/// it*** — and <see cref="It_loads_and_does_not_run"/> is the test that must then be rewritten, on
/// purpose. It is a milestone marker as much as an assertion.
/// </para>
/// <para>
/// ⚠ <b>The throw does not come from a Rule firing, which is worth knowing before debugging it.</b>
/// It arrives from the end-of-run invariant — <c>NoWaiterSleepsOnANonBlockingBin</c> →
/// <c>AccumulateClaims</c> → <c>RuleEngine.Bin</c> — because that invariant walks every claim of
/// every waiting Rule Instance whether or not it fired. ***A pool term is unreachable in this build
/// even by a Rule that never runs.***
/// </para>
/// </remarks>
public sealed class ProvisionedRulesetTests
{
    private const string File = "provisioned.toml";

    private static Ruleset Load()
    {
        RulesetLoadResult loaded = RulesetLoader.Load(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", File));

        Assert.True(loaded.Ok, loaded.Describe());

        return loaded.Ruleset!;
    }

    /// <summary>The first half: a Ruleset with a <c>pool</c> term is accepted whole.</summary>
    [Fact]
    public void The_file_loads()
    {
        Ruleset rules = Load();

        // Two premises kinds and two trades, which is what makes this file a Provider Ruleset rather
        // than twinned.toml with a comment on it.
        Assert.Equal(2, rules.KindCount);
        Assert.Equal(2, rules.BusinessKindCount);
    }

    /// <summary>
    /// <b>The split is exclusive, read off the file rather than asserted about the painter.</b>
    /// </summary>
    /// <remarks>
    /// <c>adr/0165</c>'s claim is that a Zone permits Building <em>kinds</em> and the two uses never
    /// contend for a Lot. <see cref="SyntheticCity"/> is the half that paints; this is the half that
    /// <em>reads</em>, and they are separate tests because a generator painting bit 1 and a Ruleset
    /// naming bit 1 are two independent ways to get the pair wrong.
    /// </remarks>
    [Fact]
    public void The_two_zone_rules_admit_disjoint_land()
    {
        Ruleset rules = Load();

        Assert.Equal(2, rules.ZoneRules.Length);

        ushort admitted = 0;

        foreach (ZoneRuleDefinition zone in rules.ZoneRules)
        {
            Assert.Equal(0, admitted & zone.Admits);
            admitted |= zone.Admits;
        }

        // And they are the two bits the generator actually paints, not merely two distinct ones --
        // a Zone Rule naming a bit nothing paints loads clean and builds nothing for ever.
        Assert.Equal(LotTable.Housing | LotTable.Trade, admitted);
    }

    /// <summary>
    /// <b>The seller keeps its stock</b> — <c>adr/0139</c>, read off the file.
    /// </summary>
    /// <remarks>
    /// A District Pool is a market and not a store, so the only <c>pool</c> term in this file is on
    /// the <em>buyer's</em> side. ⚠ <b>A file whose shop pushed stock into the Pool would be
    /// <c>adr/0013</c>'s *pool everything, city-wide* wearing a market's name</b>, and it would load
    /// just as cleanly — which is why this is asserted rather than left to the header.
    /// </remarks>
    [Fact]
    public void Only_the_buyer_reaches_the_pool()
    {
        Ruleset rules = Load();

        int poolInputs = 0;

        for (int id = 0; id < rules.RuleCount; id++)
        {
            var rule = new RuleId((byte)(id + 1));

            foreach (Term output in rules.Outputs(rule))
            {
                Assert.NotEqual(Scope.Pool, output.Bin.Scope);
            }

            foreach (Term input in rules.Inputs(rule))
            {
                if (input.Bin.Scope == Scope.Pool)
                {
                    poolInputs++;
                }
            }
        }

        Assert.Equal(1, poolInputs);
    }

    /// <summary>
    /// The second half, and the one that must be rewritten when task 4 lands: <b>it does not run.</b>
    /// </summary>
    [Fact]
    public void It_loads_and_does_not_run()
    {
        Ruleset rules = Load();

        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(1_000, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key);

        string refusal = Assert.Throws<NotSupportedException>(
            () =>
            {
                for (int tick = 0; tick < 64; tick++)
                {
                    simulation.Step(TickInput.Empty);
                }

                simulation.CheckEndOfRun();
            }).Message;

        // The refusal names the mechanism rather than the symbol, because what a reader needs here is
        // *which milestone owns this hole* and not which line threw.
        Assert.Contains("the District Pool does not exist", refusal);
        Assert.Contains("adr/0050", refusal);
    }
}
