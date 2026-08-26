using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
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

    /// <summary>
    /// <b>Bankruptcy and starvation are different sentences, and this is the world that produces
    /// both</b> — <c>adr/0137</c>, and milestone 26's Definition of done.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What it asserts is that the three failures are TOLD APART, not that each is common.</b>
    /// Before <c>adr/0137</c>'s field, every one of them surfaced as <c>Blocked = Supply</c> and a
    /// reader had nothing else to go on — so a test that only counted blocked Rules would have passed
    /// against the defect. ***The assertion is on the discriminator and never on the volume.***
    /// </para>
    /// <para>
    /// ⚠ <b>The three are reachable only because this file has a purchase in it.</b> A Rule short of a
    /// Good sleeps on a Bin its own premises or tenant owns; a buyer whose District has no stocked
    /// seller sleeps on the <b>market row</b>, which a District owns (<c>adr/0139</c>,
    /// <c>adr/0167</c>); and a buyer that cannot pay sleeps on its own <b>money</b> Bin. On
    /// <c>minimal.toml</c> only the first exists, which is why no world before this one could have
    /// been the acceptance test.
    /// </para>
    /// <para>
    /// 🔴 <b>The money leg subscribes because the purchase TOUCHES it, and that is the half
    /// <c>adr/0137</c> predicted would be skipped.</b> That record warned that a Pool draw failing for
    /// want of money has *"no term and therefore no Bin to subscribe to"*, and that the cheapest
    /// implementation returns insufficient funds and subscribes to nothing. Milestone 26 task 4 made
    /// it unskippable by shape rather than by discipline: <c>RuleEngine.Buy</c> pushes all three legs
    /// through <c>Touch</c>, so the money leg is walked by the same affordability loop as every
    /// authored term and blames its Bin by the same rule. ***This test is what would notice if that
    /// ever stopped being true.***
    /// </para>
    /// <para>
    /// ⚠ <b>It walks every Building rather than the worst one</b>, because the worst Building is
    /// chosen by pressure and the pressure leader on this file is a <c>consume</c> Rule starving in an
    /// ordinary larder — so the panel a human reads first is exactly the one that shows none of what
    /// is being asserted here.
    /// </para>
    /// </remarks>
    [Fact]
    public void Bankruptcy_starvation_and_a_district_shortage_are_told_apart()
    {
        Ruleset rules = Load();

        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(1_000, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        bool larder = false;
        bool market = false;
        bool broke = false;

        for (int tick = 0; tick < 6_144; tick++)
        {
            simulation.Step(TickInput.Empty);

            for (int slot = 0; slot < world.Buildings.Rows.SlotCount; slot++)
            {
                if (!world.Buildings.Rows.IsLive(slot))
                {
                    continue;
                }

                BuildingEvidence evidence =
                    Core.Evidence.Evidence.OfBuilding(world, world.Buildings.Rows.At(slot));

                foreach (RuleEvidence rule in evidence.Rules.ToArray())
                {
                    if (rule.WaitingOn == BinOwnerKind.None)
                    {
                        // Not asleep, so it names no Bin. The unset pair is the ordinary case and is
                        // asserted below rather than here.
                        continue;
                    }

                    Assert.NotEqual(default, rule.WaitingFor);

                    if (rules.IsConserved(rule.WaitingFor))
                    {
                        broke = true;
                    }
                    else if (rule.WaitingOn == BinOwnerKind.District)
                    {
                        market = true;
                    }
                    else
                    {
                        larder = true;
                    }
                }
            }

            if (larder && market && broke)
            {
                break;
            }
        }

        Assert.True(
            larder,
            "no Rule in this world ever slept on a Good Bin owned by its own premises or tenant, "
            + "which is the ordinary starvation every shipped file produces. If this is the only "
            + "one failing, suspect the walk rather than the field.");

        Assert.True(
            market,
            "no buyer ever slept on a DISTRICT-owned Bin, so either no purchase ever ran short of a "
            + "seller or the wait is landing on the seller's own Bin instead of on the market row. "
            + "The second would be adr/0167 broken -- a buyer parked on one shop sleeps through "
            + "every other shop in the District restocking.");

        Assert.True(
            broke,
            "no buyer ever slept on a MONEY Bin, so the purchase failed for want of funds without "
            + "subscribing to anything -- which is exactly the half adr/0137 said would be skipped. "
            + "Check that RuleEngine.Buy still Touches the purse rather than checking it separately.");
    }
    /// <summary>
    /// <b>A shop goes broke, and going broke is not the same as going hungry</b> — milestone 26
    /// task 7, and the world <c>plans/0037</c> task 10 has been waiting for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>A shop nobody buys from is IMMORTAL, and that is what the <c>rates</c> Rule exists to
    /// fix.</b> Unsold stock fills the seller's Bin, so its <c>stock</c> Rule stops on
    /// <c>Blocking.Space</c> — and <c>RuleEngine.Stop</c> <b>clears</b> the failure-pressure clock for
    /// every blocking reason but <c>Supply</c>, deliberately, because a full Bin is what a
    /// well-supplied Building with nobody to sell to looks like. ***So a trade cannot be made to churn
    /// by failing to SELL; it must go short of something it CONSUMES***, which is money
    /// (<c>adr/0163</c> corrected the day it was written, and <c>adr/0166</c>).
    /// </para>
    /// <para>
    /// <b>The counterparty is the treasury and that is <c>adr/0024</c> rather than a preference.</b>
    /// Money is conserved, so a local money input with no matching output is refused by
    /// <c>RulesetLoader</c>'s refusal 4 — <em>"a cost paid to nobody is a leak, not a cost"</em>, which
    /// is the loader's own message rather than a sentence in that record. <c>adr/0169</c> holds why a
    /// levy and not rent, and names cost of goods to a supplier as the successor.
    /// </para>
    /// <para>
    /// ⚠ <b>The assertion is that the two failures are TOLD APART, not that bankruptcy is common.</b>
    /// It is a tail event by construction: the levy is ~25% of a median shop's revenue, so the weakest
    /// two of twenty fail and the rest trade on. A test that counted blocked Rules would pass with the
    /// levy at any level, including one nothing could fail.
    /// </para>
    /// <para>
    /// ⚠ <b>It must outlast <c>condemn_after</c> firings of a 1,024-Tick Rule</b>, so the horizon is
    /// tens of thousands of Ticks rather than thousands: a shop has to be raised, stand in a District,
    /// trade long enough to be poor rather than new, and then miss four levies.
    /// </para>
    /// </remarks>
    [Fact]
    public void A_shop_that_cannot_pay_its_levy_goes_broke_and_the_treasury_is_paid()
    {
        Ruleset rules = Load();

        var key = WorldKey.FromSeed(0x9A0FEDU);
        var world = new World(2_000, rules, key);

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        var simulation = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        Assert.True(world.TryMoneyResource(out ResourceId money));

        // The levy Rule, by position in the file. Read rather than hard-coded so that inserting a
        // Rule above it moves the test with the Ruleset instead of silently measuring another Rule --
        // which happened while this was being written, and read as "the mechanism does not fire".
        RuleId levy = default;
        long amount = 0;

        for (int id = 0; id < rules.RuleCount; id++)
        {
            var candidate = new RuleId((byte)(id + 1));

            foreach (Term input in rules.Inputs(candidate))
            {
                if (input.Bin.Scope == Scope.Local && rules.IsConserved(input.Bin.Resource))
                {
                    levy = candidate;
                    amount = input.Amount;
                }
            }
        }

        Assert.NotEqual(default, levy);
        Assert.True(amount > 0);

        int declined = Rows.NoSlot;
        ulong pressure = 0;
        int diagTick = -1;

        // Businesses seen, ON AN EARLIER SAMPLE, holding at least one levy's worth. THE WHOLE TEST
        // TURNS ON THIS SET, and on both halves of how it is built.
        //
        // A shopfront opens at a ZERO balance -- adr/0148 instantiates the kind's trade and nothing on
        // this file founds one -- and it cannot sell until the watershed gives it a District, so its
        // first levies fail while it is simply NEW. ***That is poverty at birth and it is not
        // decline.*** Without this set the test passes on the first shopfront ever raised, at Tick
        // 6,144, having proved only that a shop with nothing cannot pay.
        //
        // ⚠ The threshold is the levy's own AMOUNT and not "> 0", and the set is updated AFTER the
        // check rather than before. Held to "> 0" and updated first, a shop that had earned 100 and
        // owed 8,192 would qualify -- which is the same shop, still too new, wearing a stronger word.
        // What is asserted here is that the shop COULD have paid on an earlier Tick and cannot now.
        var earned = new HashSet<int>();

        // till Bin slot -> Business. A Business's Bin leaves BinTable.Owner unset -- that column is a
        // Handle<Building> and cannot hold one (adr/0114) -- so the link is only walkable in this
        // direction.
        var tills = new Dictionary<int, int>();

        for (int tick = 0; tick < 32_768 && declined == Rows.NoSlot; tick++)
        {
            simulation.Step(TickInput.Empty);

            if (tick % 256 != 0)
            {
                continue;
            }

            for (int i = 0; i < world.RuleInstances.Rows.SlotCount; i++)
            {
                if (!world.RuleInstances.Rows.IsLive(i)
                    || world.RuleInstances.Rule[i] != levy
                    || !world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[i], out int on))
                {
                    continue;
                }

                // THE POINT OF task 5, ASSERTED. A levy Rule waits on a MONEY Bin, and every other
                // failure in this world waits on a Good -- so the Resource's family is what tells
                // bankruptcy from starvation, exactly as adr/0137 said it would once Evidence could
                // see the Bin at all.
                Assert.True(
                    rules.IsConserved(world.Bins.Resource[on]),
                    "the levy Rule is waiting on a Bin that does not hold money. Its only input is "
                    + "local money, so there is nothing else it can be short of; a Good here means "
                    + "the subject was resolved to the premises rather than to the Business.");

                if (diagTick < 0)
                {
                    diagTick = tick;
                }

                if (tills.TryGetValue(on, out int shop) && earned.Contains(shop))
                {
                    declined = shop;
                    pressure = (ulong)tick - world.RuleInstances.StarvedSince[i].Raw;
                }
            }

            for (int i = 0; i < world.Businesses.Rows.SlotCount; i++)
            {
                if (world.Businesses.Rows.IsLive(i)
                    && world.Bins.Rows.TryResolve(world.Businesses.Balance[i], out int till))
                {
                    tills[till] = i;

                    if (world.Bins.LevelAt(till) >= amount)
                    {
                        earned.Add(i);
                    }
                }
            }
        }

        Assert.True(
            declined != Rows.NoSlot,
            $"no shop that had earned money ever failed its levy (first broke shop of any kind at Tick "
            + $"{diagTick}), so the decline half is present and unobservable. That is how this Rule was "
            + "first written -- at ~6% of a median shop's revenue nothing could fail. Check [[rule]] "
            + "rates' amount against what a shop on this file actually earns before assuming the "
            + "mechanism is broken.");

        // 🔴 THIS USED TO ASSERT `ended > 0` AND IT WAS TRUE FOR A REASON THAT HAD NOTHING TO DO WITH
        // SHOPS. Zoning.Drain().Ended counts EVERY tenancy end, and on this file the ones it counted
        // were DWELLINGS: the tenant-side clock was condemn_after 4 against `restock`'s rate of 8, so
        // 32 Ticks, while no District -- and therefore no market to buy from -- exists until
        // [districts] revisit_ticks = 2048. Every Household starved by construction and was turned out
        // after 32 Ticks of it. ***The assertion's own failure message, "nothing was actually turned
        // out by going broke", was TRUE ON THE DAY IT PASSED.***
        //
        // 🔴 AND NO RULESET VALUE COULD HAVE MADE IT MEAN WHAT IT SAID. ZoneRuleEngine.Condemn's
        // tenancy loop walks World.Occupants -- the Households in a Building. A Business occupies
        // through World.BuildingBusinesses, "the second Occupant list" (adr/0113), and NOTHING WALKS
        // IT; `Worst` is typed Handle<Household>, so the gap is visible in a signature. A Business's
        // Failure Pressure therefore never reaches any threshold: ***a shop can go broke and cannot be
        // turned out.*** Found 2026-08-26 by the milestone-17 session, measured across four Ruleset
        // variants with every threshold armed on both kinds.
        //
        // ⚠ THE REPAIR IS NOT TO WIDEN WHAT `ended` COUNTS -- that is what was already happening. What
        // a broke shop's eviction should DO is undecided (Unplace sends a Household to the Unplaced
        // Pool; whether a Business goes to UnpremisedTable or is destroyed decides whether its capital
        // survives), and it is plans/0002 §A owned by milestone 26. Until then this asserts the
        // pressure and not the eviction, because the eviction is unbuilt.
        Assert.True(
            pressure > 0,
            "the broke shop's levy Rule carried no failure pressure, so it was short of money for an "
            + "instant rather than persistently. StarvedSince is set by RuleEngine.Stop and cleared "
            + "TOTALLY by Fire (adr/0053), so a clock still running at end of run is a shop that has "
            + "not paid since it started failing.");

        // The counterparty, and the reason the Rule has two terms rather than one. adr/0024: money is
        // conserved, so a cost paid to nobody is a leak. CheckEndOfRun folds every balance against
        // MoneySupplyTable.Issued and would fail if the levy created or destroyed a unit.
        int treasury = world.FindTreasuryBin(money);

        Assert.NotEqual(Rows.NoSlot, treasury);
        Assert.True(world.Bins.LevelAt(treasury) > 0, "the treasury collected nothing, so no levy settled.");

        simulation.CheckEndOfRun();
    }
}
