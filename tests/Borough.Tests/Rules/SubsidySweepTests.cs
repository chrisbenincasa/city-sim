using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Borough.Formats;

namespace Borough.Tests.Rules;

/// <summary>
/// <c>plans/0072</c> D12 — <b>a subsidy pays out of the treasury, bounded by a ceiling, and cuts
/// every claimant in proportion when the claims exceed it.</b>
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>The claim worth the file is that a short pot does not decide WHO is paid.</b>
/// <c>PolicyEngine.Move</c> pays each member in full or not at all and abandons the rest of the
/// sweep the moment the treasury cannot cover somebody — so on a transfer, the recipients are
/// whoever the rotating scan reached first. ***A subsidy refuses that***: everybody is cut, nobody
/// is dropped, and the arithmetic that does it is <c>Apportionment</c>.
/// </para>
/// <para>
/// ⚠ <b>The other claim is that the pot is the SMALLER of the ceiling and the treasury.</b> A
/// ceiling is not a reservation — nothing sets Money aside — so a fully funded subsidy in a broke
/// city pays nothing at all.
/// </para>
/// </remarks>
public sealed class SubsidySweepTests
{
    private const int WarmDays = 6;

    [Fact]
    public void A_subsidy_pays_every_eligible_business_its_whole_claim_when_the_pot_covers_it()
    {
        Stand stand = Employed(ceiling: 1_000_000);

        long before = stand.TreasuryLevel;
        SubsidyReading reading = stand.SweepOnce();

        Assert.True(reading.Claimants > 0, "no Business in this city employs anybody.");
        Assert.Equal(reading.Claimed, reading.Paid);
        Assert.Equal(0, reading.Rationed);
        Assert.Equal(before - reading.Paid, stand.TreasuryLevel);
    }

    /// <summary>Every unit of a bound pot is spent, and not one more.</summary>
    [Fact]
    public void A_short_pot_is_spent_to_the_last_unit_and_no_claimant_is_dropped()
    {
        long whole = Employed(ceiling: 1_000_000).SweepOnce().Claimed;

        long ceiling = whole / 2;

        SubsidyReading reading = Employed(ceiling: ceiling).SweepOnce();

        Assert.True(reading.Claimed > ceiling, "the ceiling did not bind.");
        Assert.Equal(ceiling, reading.Paid);
        Assert.True(reading.Rationed > 0, "a bound pot rationed nobody.");
    }

    /// <summary>
    /// ⚠ <b>The treasury is the other bound, and it is read at the payment rather than reserved.</b>
    /// </summary>
    [Fact]
    public void A_subsidy_cannot_pay_more_than_the_treasury_holds()
    {
        Stand stand = Employed(ceiling: 1_000_000);

        stand.EmptyTreasury();

        SubsidyReading reading = stand.SweepOnce();

        Assert.True(reading.Claimed > 0, "nobody claimed.");
        Assert.Equal(0, reading.Paid);
        Assert.Equal(reading.Claimants, reading.Rationed);
    }

    /// <summary>A ceiling of zero is how a subsidy is switched off, and it pays nothing.</summary>
    [Fact]
    public void A_ceiling_of_zero_pays_nothing_and_is_not_an_error()
    {
        Stand stand = Employed(ceiling: 0);

        long before = stand.TreasuryLevel;
        SubsidyReading reading = stand.SweepOnce();

        Assert.Equal(0, reading.Paid);
        Assert.Equal(before, stand.TreasuryLevel);
    }

    /// <summary>Money is moved and never made.</summary>
    [Fact]
    public void A_subsidy_conserves_money()
    {
        Stand stand = Employed(ceiling: 20_000);

        long before = MoneyLedger.Of(stand.World).Total;

        stand.SweepOnce();

        Assert.Equal(before, MoneyLedger.Of(stand.World).Total);
    }

    /// <summary>
    /// The transfer sweep must not also pay it, or every claimant would be paid twice and the
    /// second payment would be the all-or-nothing one.
    /// </summary>
    [Fact]
    public void The_policy_sweep_does_not_also_pay_a_subsidy_as_a_transfer()
    {
        Stand stand = Employed(ceiling: 1_000_000);

        long before = stand.TreasuryLevel;

        SubsidyReading reading = stand.SweepOnce();

        long after = stand.TreasuryLevel;

        stand.PolicySweepOnce();

        Assert.Equal(before - reading.Paid, after);
        Assert.True(reading.Paid > 0, "nothing was paid, so the claim is vacuous.");
    }

    /// <summary>A subsidy naming no trade reaches every Business, which is the catalogue's default.</summary>
    [Fact]
    public void A_subsidy_naming_no_trade_reaches_every_business()
    {
        SubsidyReading untargeted = Employed(ceiling: 1_000_000, trade: null).SweepOnce();
        SubsidyReading grocers = Employed(ceiling: 1_000_000).SweepOnce();

        Assert.True(untargeted.Claimants > 0, "an untargeted subsidy reached nobody.");
        Assert.Equal(untargeted.Claimants, grocers.Claimants);
    }

    /// <summary>
    /// ⚠ <b>A subsidy aimed at a trade nobody practises pays nobody, and is not an error.</b>
    /// </summary>
    /// <remarks>
    /// A Ruleset may declare a trade no Building in this world tenants — the loader checks the name
    /// is declared and cannot know which trades a city ends up with. ***So an empty claim list is a
    /// city fact rather than a defect***, and the sweep says so by paying nothing rather than by
    /// throwing.
    /// </remarks>
    [Fact]
    public void A_subsidy_aimed_at_a_trade_nobody_practises_pays_nobody()
    {
        Stand stand = Employed(ceiling: 1_000_000, trade: "chandler", declareChandler: true);

        long before = stand.TreasuryLevel;
        SubsidyReading reading = stand.SweepOnce();

        Assert.Equal(0, reading.Claimants);
        Assert.Equal(0, reading.Paid);
        Assert.Equal(before, stand.TreasuryLevel);
    }

    /// <summary>The player's ceiling wins over the authored one, through the verb.</summary>
    [Fact]
    public void The_fund_verb_moves_the_ceiling_and_the_next_sweep_obeys_it()
    {
        Stand stand = Employed(ceiling: 1_000_000);

        Assert.Equal(Refusal.None, stand.Sim.Refuses(Command.Fund(stand.Policy, 64)));

        stand.World.Policies.Fund(stand.Policy, 64);

        SubsidyReading reading = stand.SweepOnce();

        Assert.Equal(64, reading.Paid);
    }

    /// <summary>A ceiling against a Policy that pays nobody is refused rather than stored.</summary>
    [Fact]
    public void Funding_a_transfer_is_refused()
    {
        Stand stand = Employed(ceiling: 1_000_000);

        int transfer = Transfer(stand.World);

        Assert.NotEqual(Rows.NoSlot, transfer);
        Assert.Equal(
            Refusal.FundPolicyPaysNobody, stand.Sim.Refuses(Command.Fund(transfer, 500)));
    }

    // ---- the fixture ----------------------------------------------------------------------------

    private sealed record Stand(
        World World, Simulation Sim, SubsidyEngine Engine, PolicyEngine Transfers, int Policy)
    {
        public long TreasuryLevel => World.Bins.LevelAt(Treasury);

        private int Treasury => World.FindTreasuryBin(World.Rules.Policies[Policy].Resource);

        public SubsidyReading SweepOnce() => Engine.Sweep(Sweeping);

        public void PolicySweepOnce() => Transfers.Sweep(Sweeping);

        public void EmptyTreasury() =>
            World.Withdraw(World.Bins.Rows.At(Treasury), TreasuryLevel, Sweeping);

        private static Ticks Sweeping => new((ulong)(Ticks.PerDay * WarmDays));
    }

    /// <summary>The first Policy in the file that is an ordinary transfer.</summary>
    private static int Transfer(World world)
    {
        for (int at = 0; at < world.Rules.Policies.Length; at++)
        {
            if (world.Rules.Policies[at].Tool == PolicyTool.Transfer
                && world.Policies.Key[at] != 0)
            {
                return at;
            }
        }

        return Rows.NoSlot;
    }

    private static Stand Employed(
        long ceiling, string? trade = "grocer", bool declareChandler = false)
    {
        string aimed = trade is null ? string.Empty : $"trade = \"{trade}\"\n";

        // A declared trade that no Building kind tenants, so the world holds none of it.
        string chandler = declareChandler
            ? "\n[[business]]\nname = \"chandler\"\nwork_days = 124\nopen_days = 127\n"
                + "opens_hour = 7\ncloses_hour = 21\n"
                + "shift_start_earliest_hour = 6\nshift_start_latest_hour = 10\n"
            : string.Empty;

        // ⚠ WITHOUT THE CATALOGUE, so the Policy appended below is the only subsidy in the world.
        // The shipped file declares its own (change 10), which would claim against the same
        // treasury on the same Tick -- and `Policies.Length - 1` would then name one of two, with
        // every figure here quietly measuring both. ShippedTaxing carries the argument.
        string text = ShippedTaxing.WithoutCatalogue(ShippedTaxing.Text())
            + chandler
            + "\n[[policy]]\n"
            + "name = \"employment_support\"\n"
            + "sweeps = \"business\"\n"
            + "tool = \"subsidy\"\n"
            + aimed
            + "interval = 2048\n"
            + "apply = { min = 1, max = 1 }\n"
            + $"ceiling = {ceiling}\n"
            + "transfer = { from = \"global\", to = \"local\", resource = \"money\", amount = 8 }\n";

        RulesetLoadResult parsed = RulesetLoader.Parse(text, "taxing.toml");

        Assert.True(parsed.Ok, parsed.Describe());

        var key = WorldKey.FromSeed(1);
        var world = new World(1_000, parsed.Ruleset!, key);
        var sim = new Simulation(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        for (int tick = 0; tick < WarmDays * Ticks.PerDay; tick++)
        {
            sim.Step(default);
        }

        int policy = world.Rules.Policies.Length - 1;

        Assert.Equal(PolicyTool.Subsidy, world.Rules.Policies[policy].Tool);

        return new Stand(world, sim, new SubsidyEngine(world), new PolicyEngine(world, key), policy);
    }
}
