using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>rulesets/provisioned.toml</c> — <b>it loaded and did not run at task 3, and it trades at
/// task 4.</b>
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
/// it*** — and that is what happened: the second half is now
/// <see cref="It_runs_and_the_market_trades"/>, rewritten on the day rather than deleted, so the
/// marker records which milestone moved it.
/// </para>
/// <para>
/// 🔴 <b>⚠ THE FILE DID NEED ONE EDIT, AND IT WAS NOT THE SCOPE.</b> It states no
/// <c>[households]</c> table, so every Household opened at a zero balance and nothing in the build
/// ever issued one a penny — no wage, no gate, no policy. The first run after <c>Scope.Pool</c>
/// resolved therefore failed every purchase on the MONEY leg, at Tick 0, for ever: the shops filled
/// and nobody bought. ***A world that loads, runs and demonstrates nothing is the failure this class
/// was written to make visible, arriving one leg over from where it was looked for.***
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
    /// The second half, rewritten at task 4 as its own doc said it would be: <b>it runs, and it
    /// trades.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A Business holding money is the assertion, and nothing else in this world could have put it
    /// there.</b> A Business opens at a zero balance — <c>adr/0144</c> for a founded one and
    /// <c>adr/0148</c> for an instantiated one, and this file founds none — there is no wage
    /// (<c>adr/0026</c>, milestone 15), no <c>[[policy]]</c>, and no gate. ***So the only door money
    /// can reach a grocer through is a sale***, which makes one line stand for the whole of
    /// <c>adr/0050</c>: the Good moved one way and the money the other, settled atomically.
    /// </para>
    /// <para>
    /// <b>Conservation is asserted by <c>CheckEndOfRun</c> and not restated here.</b>
    /// <c>Invariant.MoneyIsConserved</c> folds every balance against
    /// <c>MoneySupplyTable.Issued</c>, which only <c>World.Endow</c> moves — so a purchase that
    /// created or destroyed a unit fails there, on a walk this test already performs. Restating it
    /// would be a second spelling that has to agree for ever.
    /// </para>
    /// <para>
    /// ⚠ <b>The run must outlast the watershed's cadence, and that is why it is Ticks in the
    /// thousands rather than 64.</b> A shop has to be raised on trade land, take a tenancy, stock its
    /// Bin, and stand in a District — and a District arrives on <c>[districts] revisit_ticks</c>,
    /// which is 2,048 here. A shorter run tests that nothing throws and nothing more.
    /// </para>
    /// </remarks>
    [Fact]
    public void It_runs_and_the_market_trades()
    {
        Ruleset rules = Load();

        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(1_000, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        // Off for the long-run tests' reason, and it matters more on this file than on most: the
        // guard folds the whole world twice a Tick, and this world is twinned.toml's TWO paved
        // lattices. It asks whether Phase 2 wrote a column; what a purchase writes in Phase 2 is
        // engine scratch and a derived index, neither of which is a column, and ReplayTests and the
        // golden baseline hold the property for the build as a whole.
        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        for (int tick = 0; tick < 6_144; tick++)
        {
            simulation.Step(TickInput.Empty);

            // ⚠ THE POOL'S OWN BINS STAY EMPTY FOR EVER, and that is what a market rather than a
            // store MEANS (adr/0139). It is the assertion that would catch the one failure no other
            // test in this repository can: plans/0044's "must not implement Scope.Pool as a wider Bin
            // lookup", which ships an unconserved economy, and whose tell is stock appearing in a Bin
            // nobody sells from. Asserted every Tick and inside this run rather than in a second one,
            // because a second 6,144-Tick run costs the assertion tier the same again for one
            // predicate.
            for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
            {
                if (world.DistrictPools.Rows.IsLive(row)
                    && world.Bins.Rows.TryResolve(world.DistrictPools.Bin[row], out int pool))
                {
                    Assert.Equal(0, world.Bins.LevelAt(pool));
                }
            }
        }

        // Every invariant, including MoneyIsConserved across every purchase the run settled.
        simulation.CheckEndOfRun();

        long earned = 0;
        int sellers = 0;

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (!world.Businesses.Rows.IsLive(slot)
                || !world.Bins.Rows.TryResolve(world.Businesses.Balance[slot], out int balance))
            {
                continue;
            }

            long held = world.Bins.LevelAt(balance);

            if (held > 0)
            {
                earned += held;
                sellers++;
            }
        }

        Assert.True(
            sellers > 0,
            "no Business in this world holds a penny, so no purchase ever settled. A grocer opens at "
            + "zero and this file has no wage, no policy and no gate, so a sale is the only way money "
            + "reaches one. Check that Households were endowed -- [households] opening_balance_max -- "
            + "before looking at Scope.Pool.");

        Assert.True(earned > 0);
    }
}
