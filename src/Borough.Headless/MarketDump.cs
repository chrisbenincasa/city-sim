namespace Borough.Headless;

using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;

/// <summary>
/// The market dump — <b>a Pool with stock, a price, and a Building that could not afford it</b>
/// (<c>plans/0044</c> task 8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Three panels, and the third one is the whole reason this exists.</b> The task entry that asked
/// for this dump says so in its own words: <i>"the third clause is the one that would be dropped, and
/// it is the only one that shows the market having a consequence."</i> A Pool with stock in it and a
/// price beside it is a table; ***a Household that wanted sundries and could not pay for them is a
/// market doing something to somebody***, and it is the only one of the three that can be wrong in a
/// way a reader would notice.
/// </para>
/// <para>
/// ⚠ <b>The stock column is a sum over the SELLERS and not a level in the Pool.</b>
/// <c>adr/0139</c> made a District Pool a **market and not a store** — stock stays in the selling
/// Building's own Bin and the row carries the price, the wake target and the reachable sellers. So
/// the Pool's own Bin is empty in every row of every world, by construction, and a dump that printed
/// <c>Bins.LevelAt</c> for it would print a column of zeroes and call it inventory.
/// </para>
/// <para>
/// 🔴 <b>A FLAT PRICE COLUMN IS NOT A FINDING ON ITS OWN, AND IT WAS ONE UNTIL 2026-08-26.</b> The
/// panel was written to report that no price had ever moved on any world, because
/// <see cref="MarketRuleset.Reprice"/> took the Pool Bin's own <c>level</c> as its cover and that is
/// the Bin <c>adr/0139</c> emptied. <c>adr/0171</c> makes the cover
/// <see cref="Borough.Core.Space.Offered.Held"/> — the sum over the sellers — and the price moves.
/// ⚠ <b>It still does not move on <c>rulesets/provisioned.toml</c>, and there THAT IS THE MECHANISM
/// WORKING</b>: a tier-1 city holds under a Day of cover, and a market with less than a Day of supply
/// prices at its import ceiling because there is nothing to undercut it with. ***The defect and the
/// correct result print the same digits***, so this panel prints <c>stock</c> and <c>rate/Day</c>
/// beside the price and says which of the two a reader is looking at.
/// </para>
/// <para>
/// ⚠ <b>It steps its own world</b>, for <c>--money</c>'s reason one further on: a market row is
/// created by the watershed at <c>[districts] revisit_ticks</c>, a seller is raised by a Zone Rule
/// long after that, and a shortfall needs a Household that has had time to spend. Nothing here is
/// visible at Tick 0 and a run shorter than a few Days prints an empty city truthfully.
/// </para>
/// </remarks>
internal static class MarketDump
{
    private const int Rows = 16;

    /// <summary>Runs the dump and writes it. <c>0</c> ok, <c>2</c> no Ruleset, <c>3</c> no market.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        if (Refuse(rules, output) is int refusal)
        {
            return refusal;
        }

        var cadence = (uint)Ticks.PerDay;
        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules, key);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };
        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        List<Reading> readings = [];

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);

            if (simulation.Tick.Raw % cadence == 0)
            {
                Sample(world, simulation.Tick, readings);
            }
        }

        output.WriteLine(
            "# Borough market dump — a Pool with stock, a price, and who could not afford it");
        string sizing = F($"# {options.Citizens:N0} Citizens, {options.Ticks:N0} Ticks");
        string taken = F($"a reading every {cadence:N0} — the Day [market] reprices on.");
        output.WriteLine($"{sizing}, {taken}");
        output.WriteLine();

        Standing(world, names, output);
        output.WriteLine();
        Moved(world, names, readings, output);
        output.WriteLine();
        Shortfall(world, names, simulation.Tick, output);

        return 0;
    }

    /// <summary>
    /// A Ruleset that cannot show a market, and what to run instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three refusals and they are not the same complaint.</b> Without <c>[districts]</c> there is
    /// no District and therefore no market row at all; without <c>[market]</c> there are rows and the
    /// price is frozen at the ceiling by the file rather than by the build, which would make this
    /// dump's own finding unreadable; and without a kind that <em>sells</em> there is a market with
    /// one side, which is <c>rulesets/twinned.toml</c> exactly. ***The third is the one that would be
    /// missed***, because the first two are Ruleset tables a reader can see and the third is a
    /// property of a Bin declaration three levels down.
    /// </para>
    /// </remarks>
    private static int? Refuse(Ruleset rules, TextWriter output)
    {
        if (!rules.Districts.Runs)
        {
            output.WriteLine(
                "This Ruleset states no [districts], so the city derives no District and there is no "
                + "market row to print. A Pool is opened per (District, Good) and a world with no "
                + "centre in it has none. A District is content.");
            output.WriteLine();
            output.WriteLine("  --market --ruleset rulesets/provisioned.toml --ticks 8192");

            return 3;
        }

        if (!rules.Market.Runs)
        {
            output.WriteLine(
                "This Ruleset states no [market], so every trade clears at the import ceiling for "
                + "ever and the price column would be flat because the FILE says so. This dump "
                + "reports whether the price moves, and a world that forbids it cannot answer.");
            output.WriteLine();
            output.WriteLine("  --market --ruleset rulesets/provisioned.toml --ticks 8192");

            return 3;
        }

        if (!Sells(rules))
        {
            output.WriteLine(
                "This Ruleset declares no Building kind holding a Good in a BUSINESS-owned Bin, so "
                + "nothing in the city can sell. The market rows would exist, hold a price, and have "
                + "no seller behind any of them — a market with one side. Declaring a [[business]] "
                + "trade is not the same test: rulesets/twinned.toml names two and instantiates "
                + "neither.");
            output.WriteLine();
            output.WriteLine("  --market --ruleset rulesets/provisioned.toml --ticks 8192");

            return 3;
        }

        return null;
    }

    /// <summary>Does any declared kind hold a Good in a Bin that belongs to the trade?</summary>
    private static bool Sells(Ruleset rules)
    {
        for (int kind = 1; kind <= rules.KindCount; kind++)
        {
            foreach (BinDeclaration bin in rules.BinsOf((byte)kind))
            {
                if (bin.Tenancy == BinTenancy.Business && !rules.IsConserved(bin.Resource))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Panel 1 — every market row as it stands at the end of the run.</summary>
    private static void Standing(World world, RulesetNames names, TextWriter output)
    {
        output.WriteLine("Where the market is — one row per (District, Good)");
        output.WriteLine();

        string header = Row(
            "district", "good", "price", "ceiling", "sellers", "stock", "in the Pool", "rate/Day");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        int rows = 0;

        for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
        {
            if (!Live(world, row, out int bin, out int district))
            {
                continue;
            }

            ResourceId resource = world.Bins.Resource[bin];
            int sellers = world.Markets.SellerCount(world, row);

            output.WriteLine(Row(
                District(world, district),
                names.Resource(resource) ?? F($"resource {resource.Raw}"),
                Count(world.DistrictPools.Price[row].Raw),
                Count(world.Rules.ImportCeiling(resource).Raw),
                Count(sellers),
                Count(Stock(world, row, sellers)),
                Count(world.Bins.LevelAt(bin)),
                Count(world.DistrictPools.Rate[row])));

            rows++;
        }

        if (rows == 0)
        {
            output.WriteLine("  (no market row — the watershed derived no District in this run)");
        }

        output.WriteLine();
        output.WriteLine(
            "  A Pool is a MARKET and not a store (adr/0139), so the stock column is the sum over the");
        output.WriteLine(
            "  SELLERS' own Bins. The Pool's own Bin holds nothing, in every row of every world, by");
        output.WriteLine(
            "  construction — and the `in the Pool` column is printed to SHOW that rather than to");
        output.WriteLine(
            "  measure anything. It is what MarketRuleset.Reprice took as its cover until adr/0171,");
        output.WriteLine(
            "  which is why no price had ever moved on any world; the cover is now the stock column.");
        output.WriteLine(
            "  The ceiling is the MINIMUM [[hinterland]] price across every declared edge (adr/0135),");
        output.WriteLine(
            "  and it is where a price OPENS rather than a seed: [market] states no starting price and");
        output.WriteLine("  needs none.");
    }

    /// <summary>Panel 2 — what the price did, one reading per reprice.</summary>
    private static void Moved(
        World world, RulesetNames names, List<Reading> readings, TextWriter output)
    {
        output.WriteLine("What the price did — one reading a Day, which is when [market] reprices");
        output.WriteLine();

        string header = Row(
            "district", "good", "opened", "now", "low", "high", "moves", "");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        int printed = 0;
        long moved = 0;

        for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
        {
            if (!Live(world, row, out int bin, out int district))
            {
                continue;
            }

            long opened = 0;
            long now = 0;
            long low = long.MaxValue;
            long high = long.MinValue;
            long moves = 0;
            bool seen = false;

            foreach (Reading reading in readings)
            {
                if (reading.Row != row)
                {
                    continue;
                }

                if (!seen)
                {
                    opened = reading.Price;
                    seen = true;
                }
                else if (reading.Price != now)
                {
                    moves++;
                }

                now = reading.Price;
                low = reading.Price < low ? reading.Price : low;
                high = reading.Price > high ? reading.Price : high;
            }

            ResourceId resource = world.Bins.Resource[bin];

            output.WriteLine(Row(
                District(world, district),
                names.Resource(resource) ?? F($"resource {resource.Raw}"),
                seen ? Count(opened) : "—",
                seen ? Count(now) : "—",
                seen ? Count(low) : "—",
                seen ? Count(high) : "—",
                seen ? Count(moves) : "—",
                string.Empty));

            moved += moves;
            printed++;
        }

        if (printed == 0)
        {
            output.WriteLine("  (no market row — the watershed derived no District in this run)");
        }

        output.WriteLine();

        if (moved > 0)
        {
            output.WriteLine(F($"  {moved:N0} price changes across {printed:N0} rows."));
            output.WriteLine(
                "  The cap is [market] move_cap_percent of the ceiling, so a row cannot cross the");
            output.WriteLine("  table in one Day however far the target is.");

            return;
        }

        // ⚠ THE BRANCH IS THE POINT. A flat price column meant a defect until adr/0171 and means
        // scarcity after it, and the two print identical digits -- so the dump derives which it is
        // looking at rather than asserting one. A row holding MORE than a Day's cover and still
        // sitting at its ceiling is the old defect returning, and nothing else would say so.
        //
        // 🔴 THERE IS A THIRD STATE AND IT WAS BEING COUNTED AS THE FIRST (plans/0053). Cover is
        // stock DIVIDED BY draw, so a row whose draw is ZERO has no cover at all -- and `Held >
        // Rate` reads such a row as glutted the instant it holds one unit. provisioned.toml grew
        // exactly that row when occupancy started dividing the ground: a District with 2 sellers,
        // 188 sundries and nobody buying, which printed a defect report about a defect that had
        // been fixed. A District with no draw is not a glut; it is a shop with no street.
        int glutted = 0;
        int undrawn = 0;

        for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
        {
            if (!Live(world, row, out _, out _))
            {
                continue;
            }

            long held = world.Markets.Stock(world, row).Held;
            long rate = world.DistrictPools.Rate[row];

            if (rate <= 0)
            {
                undrawn += held > 0 ? 1 : 0;
            }
            else if (held > rate)
            {
                glutted++;
            }
        }

        if (glutted > 0)
        {
            output.WriteLine(
                "  🔴 NOT ONE PRICE MOVED AND THAT IS A DEFECT REPORT RATHER THAN A QUIET COLUMN:");
            output.WriteLine(
                F($"  {glutted:N0} of {printed:N0} rows hold MORE than a Day's cover and are still at"));
            output.WriteLine(
                "  the ceiling. A price falls when the sellers' stock outruns the District's daily");
            output.WriteLine(
                "  draw (adr/0171), so a glut priced at the import ceiling is the tâtonnement not");
            output.WriteLine(
                "  running. Check that [market] states move_cap_percent above zero, then check what");
            output.WriteLine(
                "  MarketRuleset.Reprice is being handed as its cover — that is where this last hid.");

            return;
        }

        output.WriteLine(
            "  No price moved, and on this world that is the mechanism rather than a defect.");
        output.WriteLine(
            "  A price OPENS at the import ceiling and falls only where the market holds more than a");
        output.WriteLine(
            "  Day's cover — the sellers' stock against the District's daily draw (adr/0171). Every");
        output.WriteLine(
            "  row above is unsold or under a Day, so the ceiling is the honest price: there is");
        output.WriteLine(
            "  nothing in this city to undercut it with.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ Read the stock and rate/Day columns before reading anything into this one. A flat");
        output.WriteLine(
            "  price was a DEFECT REPORT until 2026-08-26 — the cover was read off the Pool's own");
        output.WriteLine(
            "  Bin, which adr/0139 had emptied — and the defect printed these same digits.");
        output.WriteLine(
            "  For a glut, run rulesets/oversupplied.toml: the same file with two keys deleted.");

        if (undrawn > 0)
        {
            output.WriteLine();
            output.WriteLine(F(
                $"  ⚠ {undrawn:N0} of {printed:N0} rows hold stock against a draw of NOTHING, and those"));
            output.WriteLine(
                "  are neither scarce nor glutted — they have no cover, because cover is stock over a");
            output.WriteLine(
                "  daily draw and this District has no consumer of that Good in it at all. The price");
            output.WriteLine(
                "  sits at the ceiling for want of anything to compare against. Read it as a fact");
            output.WriteLine(
                "  about where the city put its Buildings, never as a fact about the market.");
        }
    }

    /// <summary>Panel 3 — who could not afford it, which is the point of the dump.</summary>
    private static void Shortfall(
        World world, RulesetNames names, Ticks now, TextWriter output)
    {
        output.WriteLine(
            "Who could not afford it — every Rule Instance starving at end of run, by what it waits on");
        output.WriteLine();

        long marketInstances = 0;
        long moneyInstances = 0;
        long localInstances = 0;
        List<int> broke = [];

        for (int slot = 0; slot < world.RuleInstances.Rows.SlotCount; slot++)
        {
            if (!world.RuleInstances.Rows.IsLive(slot)
                || !world.RuleInstances.IsStarving(slot)
                || !world.Bins.Rows.TryResolve(world.RuleInstances.WaitingOn[slot], out int bin))
            {
                continue;
            }

            if (world.Bins.OwnerKind[bin] == BinOwnerKind.District)
            {
                marketInstances++;
            }
            else if (world.Rules.IsConserved(world.Bins.Resource[bin]))
            {
                moneyInstances++;

                if (broke.Count < Rows)
                {
                    broke.Add(slot);
                }
            }
            else
            {
                localInstances++;
            }
        }

        string header = Line("what it waits on", "instances", "share");
        output.WriteLine(header);
        output.WriteLine(new string('-', header.Length));

        long total = marketInstances + moneyInstances + localInstances;

        output.WriteLine(Line(
            "the market — nobody is selling", Count(marketInstances), Share(marketInstances, total)));
        output.WriteLine(Line(
            "money — it could not afford it", Count(moneyInstances), Share(moneyInstances, total)));
        output.WriteLine(Line(
            "a Bin of its own — the larder is empty",
            Count(localInstances),
            Share(localInstances, total)));

        output.WriteLine();
        output.WriteLine(
            "  ⚠ THE MIDDLE ROW IS WHY THIS DUMP EXISTS. The other two are a market with no seller and");
        output.WriteLine(
            "  a Rule with no input, and both are visible without a market at all. A Rule stopped on a");
        output.WriteLine(
            "  MONEY Bin is the only reading in this file that shows a price having a consequence for");
        output.WriteLine("  somebody.");
        output.WriteLine();
        output.WriteLine(
            "  ⚠ Only Blocking.Supply sets StarvedSince, so this counts what is short and never what");
        output.WriteLine(
            "  is FULL: a seller that cannot deposit stops on Space, its clock is cleared, and it does");
        output.WriteLine(
            "  not appear here. A shop nobody buys from is invisible to this panel on purpose");
        output.WriteLine("  (adr/0166).");

        if (broke.Count == 0)
        {
            output.WriteLine();
            output.WriteLine(
                "  Nobody was short of money at end of run. On a world whose Households open with a");
            output.WriteLine(
                "  balance and earn nothing, that means the run was too short to spend it down rather");
            output.WriteLine("  than that the city is rich.");

            return;
        }

        output.WriteLine();
        output.WriteLine(F(
            $"  Named — the first {broke.Count:N0} of {moneyInstances:N0}. A Building may appear twice:"));
        output.WriteLine(
            "  the row is a Rule Instance and a Rule Instance belongs to an OCCUPANT (adr/0166), so two");
        output.WriteLine("  Households under one roof are two rows.");
        output.WriteLine();

        string examples = Line("building", "rule", "starving for");
        output.WriteLine(examples);
        output.WriteLine(new string('-', examples.Length));

        foreach (int slot in broke)
        {
            RuleId rule = world.RuleInstances.Rule[slot];
            bool premised = world.Buildings.Rows.TryResolve(
                world.RuleInstances.Building[slot], out int premises);

            string where = premised
                ? F($"{names.Kind(world.Buildings.Kind[premises]) ?? "kind"} #{premises}")
                : "(no premises)";

            output.WriteLine(Line(
                where,
                names.Rule(rule) ?? F($"rule {rule.Raw}"),
                F($"{Starved(world, now, slot):N0} Ticks")));
        }
    }

    private static ulong Starved(World world, Ticks now, int slot) =>
        now.Raw - world.RuleInstances.StarvedSince[slot].Raw;

    /// <summary>One sample of every live market row.</summary>
    private static void Sample(World world, Ticks tick, List<Reading> readings)
    {
        for (int row = 0; row < world.DistrictPools.Rows.SlotCount; row++)
        {
            if (!Live(world, row, out _, out _))
            {
                continue;
            }

            readings.Add(new Reading(tick, row, world.DistrictPools.Price[row].Raw));
        }
    }

    /// <summary>A live market row, with its Bin and its District resolved.</summary>
    private static bool Live(World world, int row, out int bin, out int district)
    {
        bin = -1;
        district = -1;

        return world.DistrictPools.Rows.IsLive(row)
            && world.Bins.Rows.TryResolve(world.DistrictPools.Bin[row], out bin)
            && world.Districts.Rows.TryResolve(world.DistrictPools.District[row], out district);
    }

    /// <summary>What the sellers reachable from this row are holding, summed.</summary>
    private static long Stock(World world, int row, int sellers)
    {
        long held = 0;

        for (int ordinal = 0; ordinal < sellers; ordinal++)
        {
            Offer offer = world.Markets.Seller(world, row, ordinal);
            held += world.Bins.LevelAt(offer.Bin);
        }

        return held;
    }

    /// <summary>A District has no name, so it is its slot and where its centre stands.</summary>
    private static string District(World world, int district) => F(
        $"{district} at {world.Districts.CentreEast[district].Raw:N0},{world.Districts.CentreNorth[district].Raw:N0}");

    private static string Share(long part, long whole) =>
        whole == 0 ? "—" : F($"{(part * 100) / whole:N0}%");

    private static string Row(
        string a, string b, string c, string d, string e, string f, string g, string h) =>
        F($"{a,-18}  {b,-10}  {c,8}  {d,8}  {e,8}  {f,10}  {g,12}  {h,9}");

    private static string Line(string a, string b, string c) =>
        F($"{a,-42}  {b,12}  {c,10}");

    private static string Count(long value) => F($"{value:N0}");

    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>One market row's price at one reprice boundary.</summary>
    private readonly record struct Reading(Ticks Tick, int Row, long Price);
}
