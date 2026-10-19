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
/// A tenant living off a wage paid every seven Days — <c>plans/money-circulation.md</c> L16's
/// reason for refusing a deadline shorter than the longest pay period.
/// </summary>
/// <remarks>
/// <para>
/// <b>Rent is charged daily and a wage arrives weekly, so a tenant on a wage is in arrears most of
/// the week by construction.</b> That is not a defect and it is why the debt exists: the arrears
/// clock measures the Days since the shortfall began, the payday clears it, and the clock starts
/// again. ***What must never happen is the clock reaching the deadline between two paydays***, which
/// the loader's refusal makes unauthorable and this file watches hold at run time.
/// </para>
/// <para>
/// <b>Two fixtures, because they answer different halves.</b>
/// <see cref="A_tenant_paid_every_seven_days_clears_its_arrears_at_every_payday"/> deposits the week
/// on its exact cadence, so the Days and the figures are the test's own and nothing else moves.
/// <see cref="A_waged_city_collects_its_rent_and_conserves_its_money"/> runs a populated city off
/// <c>insolvent.toml</c>, so the charge meets real wages, real employment and every invariant at
/// once.
/// </para>
/// </remarks>
public sealed class RentPaydayTests
{
    private static readonly WorldKey Key = WorldKey.FromSeed(0x2026_10_17UL);

    /// <summary>Money per floor Tile per Day.</summary>
    private const int Rate = 10;

    /// <summary>Floor Tiles one tenancy holds, which is also the capacity rate.</summary>
    private const int Share = 4;

    /// <summary>Days between paydays, and the eviction deadline the loader therefore requires.</summary>
    private const int Period = 7;

    /// <summary>What one tenancy pays a Day on ground worth nothing.</summary>
    private const int Daily = Share * Rate;

    private static readonly ResourceId Cash = new(1);

    /// <summary>
    /// A tenant paid a week's rent every seventh Day is behind for six Days in seven and is never
    /// evicted, because every payday clears the debt and resets the clock.
    /// </summary>
    [Fact]
    public void A_tenant_paid_every_seven_days_clears_its_arrears_at_every_payday()
    {
        Ruleset rules = new(
            resources: [ResourceFamily.Money],
            rules: [],
            kinds: [new KindDefinition(0, 0, 0, 0)
            {
                Houses = true,
                RentPerFloorTile = new Money(Rate),
            }],
            inputs: [],
            outputs: [],
            emissions: [],
            bins: [],
            kindRules: [],
            zoneRules: [])
        {
            Placement = new PlacementRuleset(0, 0, 0, 20, 0),
            Capacity = new CapacityRuleset(Share, 0, 0),

            // Exactly the pay period, which is the shortest deadline the loader permits beside a
            // weekly wage -- so this is the tightest case the mechanism has to survive.
            Rent = new RentRuleset(Period),
        };

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        Handle<Lot> lot = world.Lots.Create(
            Tiles.Zero, Tiles.Zero, zone: 0, wide: new Tiles(Share), deep: new Tiles(1));
        Handle<Household> tenant = world.CreateHousehold(
            world.CreateBuilding(lot, 1, Ticks.Zero, Key), lifeStage: 0);
        int slot = world.Households.Rows.Resolve(tenant);

        int paydays = 0;
        int behind = 0;

        for (int day = 0; day < 6 * Period; day++)
        {
            // Paid in arrears, on the last Day of the week worked -- and before that Day's charge,
            // as payroll runs ahead of the rent sweep in phase 6.
            bool payday = day % Period == Period - 1;

            if (payday)
            {
                world.Endow(tenant, new Money(Period * Daily));
                paydays++;
            }

            while (world.Tick.Raw < (ulong)(day + 1) * (ulong)Ticks.PerDay)
            {
                simulation.Step(TickInput.Empty);
            }

            if (payday)
            {
                // The claim in one line. The week's wage settles the week's rent, so the tenant owes
                // nothing at every payday and the clock starts again from zero.
                Assert.False(world.Households.IsBehindOnRent(slot));
                Assert.Equal(0, world.Households.RentDebtSinceDay[slot]);
            }
            else
            {
                behind++;

                // Six Days short of the deadline at the worst, which is what the loader's refusal
                // buys and what makes a weekly wage survivable at all.
                Assert.True(world.Households.IsBehindOnRent(slot));
                Assert.InRange(day - world.Households.RentDebtSinceDay[slot], 0, Period - 2);
            }

            Assert.False(world.Households.IsUnplaced(slot));
        }

        Assert.Equal(6, paydays);

        // Six Days in seven, so arrears are the ordinary state of a waged tenant rather than a rare
        // one -- and the test would pass vacuously if they were not.
        Assert.Equal(6 * (Period - 1), behind);

        Assert.Equal(6 * Period * Daily, world.Bins.LevelAt(world.FindTreasuryBin(Cash)));

        simulation.CheckEndOfRun();
    }

    /// <summary>
    /// A populated waged city charges its rent into the treasury, keeps money conserved and trips no
    /// invariant.
    /// </summary>
    /// <remarks>
    /// <b><c>insolvent.toml</c> patched rather than a new shipped file</b>, on
    /// <c>PayrollAccrualTests</c>' idiom. It is the one shipped Ruleset that pays a weekly wage and
    /// declares no decline, so a Household leaving its dwelling here can only have been evicted for
    /// arrears. Three keys are added: the rate, the deadline, and the give-up rule the deadline is
    /// refused without.
    /// </remarks>
    [Fact]
    public void A_waged_city_collects_its_rent_and_conserves_its_money()
    {
        string text = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "insolvent.toml"));

        RulesetLoadResult loaded = RulesetLoader.Parse(
            text
                .Replace(
                    "houses   = true",
                    "houses   = true\nrent_per_floor_tile = 8",
                    StringComparison.Ordinal)
                .Replace(
                    "[placement]\ninterval      = 32",
                    "[placement]\ngives_up_after_days = 60\ninterval      = 32",
                    StringComparison.Ordinal)
                + "\n[rent]\nevicts_after_days = 7\n",
            "renting-insolvent.toml");

        Assert.True(loaded.Ok, loaded.Describe());

        Ruleset rules = loaded.Ruleset!;

        Assert.Equal(7, rules.Rent.EvictsAfterDays);

        var world = new World(1_000, rules, Key);
        var simulation = new Simulation(world, Key);

        SyntheticCity.PopulateInto(world, Key, Ticks.Zero);

        int housed = Housed(world);

        Assert.True(housed > 0, "the populated city housed nobody.");

        RentReading sweep = default;

        (long Owed, int Tenancies) arrears = Arrears(world);
        bool settledMidDay = false;

        for (int tick = 0; tick < 10 * Ticks.PerDay; tick++)
        {
            simulation.Step(TickInput.Empty);

            // The reading is overwritten every Tick and only a Day boundary writes one (adr/0006),
            // so it is read where it is written rather than at the end of the run.
            if (simulation.LastRent.Tenancies > 0)
            {
                sweep = simulation.LastRent;
            }

            (long Owed, int Tenancies) now = Arrears(world);

            // L16 at city scale: total arrears fall on a Tick the rent sweep did not run on, which
            // can only be a wage, a sale or a draw landing and being taken for rent where it
            // arrived. Two things are excluded rather than argued away -- the Day sweep, which is
            // the Tick after a boundary, and a tenancy ENDING, which writes its debt off without
            // paying it and would otherwise read as a payment.
            if (now.Owed < arrears.Owed
                && now.Tenancies == arrears.Tenancies
                && world.Tick.Raw % (ulong)Ticks.PerDay != 1UL)
            {
                settledMidDay = true;
            }

            arrears = now;
        }

        Assert.True(
            settledMidDay,
            "no tenancy's arrears ever fell between Day boundaries, so income is not settling rent "
            + "where it lands.");

        Assert.True(
            sweep.Collected > 0, $"the sweep collected nothing from {housed} housed Households.");

        Assert.InRange(sweep.Behind, 0, sweep.Tenancies);
        Assert.True(sweep.Tenancies > 0, "the sweep walked no tenancy.");

        // ⚠ Collected is NOT bounded by Charged, and the reason is the mechanism this test is
        // about: income settles arrears where it lands, so a debt paid off between two boundaries
        // is discharged but never asked for again. Every Lot here is city-held, so no ring of
        // Landowners exists and nothing can have cancelled.
        Assert.Equal(0, sweep.Cancelled);

        // Every Lot in a generated city is city-held (L14), so the rent is treasury income. The
        // money Resource is the file's third here, so it is asked for rather than assumed.
        Assert.True(world.TryMoneyResource(out ResourceId money));
        Assert.True(world.Bins.LevelAt(world.FindTreasuryBin(money)) > 0, "the treasury is empty.");

        // The invariants, MoneyIsConserved among them. A debt is not money (adr/0003), so the
        // arrears this city runs up must leave the supply exactly where it was.
        simulation.CheckEndOfRun();
    }

    private static int Housed(World world)
    {
        int count = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// What the city's tenancies owe in rent, and how many tenancies there are to owe it.
    /// </summary>
    /// <remarks>
    /// <b>The count travels with the total because a tenancy ending also reduces the total.</b>
    /// Eviction, condemnation and bankruptcy all write a debt off without anyone paying it, so a
    /// fall in the total only means a payment while the count holds still.
    /// </remarks>
    private static (long Owed, int Tenancies) Arrears(World world)
    {
        long owed = 0;
        int tenancies = 0;

        for (int slot = 0; slot < world.Households.Rows.SlotCount; slot++)
        {
            if (world.Households.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Households.Dwelling[slot]))
            {
                owed += world.Households.RentDebt[slot].Raw;
                tenancies++;
            }
        }

        for (int slot = 0; slot < world.Businesses.Rows.SlotCount; slot++)
        {
            if (world.Businesses.Rows.IsLive(slot)
                && world.Buildings.Rows.IsValid(world.Businesses.Building[slot]))
            {
                owed += world.Businesses.RentDebt[slot].Raw;
                tenancies++;
            }
        }

        return (owed, tenancies);
    }
}
