using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Headless;

/// <summary>
/// The standing city counted by Building kind — what a Ruleset declared, and what of it stands.
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>NOTHING IN THIS PROJECT COULD REPORT A MIXED CITY, AND THAT IS WHY THIS EXISTS.</b> Every
/// other picture aggregates over Buildings and drops the kind on the way: <c>--census</c> has
/// <c>building live</c>, <c>slots</c> and <c>capacity</c> and no fourth row, <c>--zones</c> draws a
/// Lot grid in which every standing Building is the same <c>#</c>, and the Zone dump's own header
/// prints <c>kind 1</c>, <c>kind 2</c> as integers because no dump had ever needed the names. ***A
/// city whose mix cannot be counted cannot be balanced***, and the first question a second Building
/// kind raises is *how many of each*.
/// </para>
/// <para>
/// <b>It counts the same city twice, before and after</b>, which is <see cref="ZoneDump"/>'s shape
/// and its reason: the populator's city and the swept city are different cities, and the interesting
/// number is the difference. ⚠ <b><see cref="SyntheticCity"/> raises ONE kind</b> — its own
/// <c>DwellingKind</c> is a hardcoded <c>1</c> and its remark says *"the kind this populator raises,
/// and the only one it knows"* — so the <em>before</em> column of every world this runner builds is
/// one kind holding everything. Every other kind in the table arrives through a Zone Rule, a
/// <c>service</c> command, or not at all.
/// </para>
/// <para>
/// 🔴 <b>THE ZERO ROWS ARE THE POINT AND NOT THE PADDING.</b> A kind that stands nowhere is declared
/// content the city never built, and there are three unlike reasons for it that the table cannot tell
/// apart on the count alone — so the footer separates them. No Zone Rule names the kind, which is a
/// service awaiting a command (<c>adr/0032</c>). A Zone Rule names it and admits a bit no Lot in this
/// world carries, which is a <b>dead rule</b>: it draws its sample for ever and builds nothing.
/// Or a Rule names it and the Lots it wants are taken. ⚠ <b>The second is not refused at load</b> —
/// <c>RulesetLoader</c> checks the bit is inside <see cref="LotTable.ZoneBits"/> and cannot check
/// that anything paints it — so a Ruleset can declare an industrial zone, load clean, and raise
/// nothing for ever with no diagnostic anywhere. ***This dump is where that becomes visible.***
/// </para>
/// <para>
/// ⚠ <b>The painted set is read off the world rather than off the generator.</b> It is the union of
/// every live Lot's permission set, so it reports what <em>this</em> world carries rather than what
/// <see cref="SyntheticCity"/> promises — which keeps the finding true when a command has painted
/// something the generator does not (<c>adr/0093</c>: a description of the build is where to look).
/// </para>
/// <para>
/// <b>Ceiling against holds is the second reading.</b> Occupancy derives from the ground since
/// <c>plans/0053</c> step 3, so a kind's ceiling is a property of the Lots it happened to land on and
/// two kinds with identical declarations can carry different ceilings. ***The gap between ceiling and
/// holds is the room the city has and is not using***, and a kind whose ceiling is large and whose
/// holds is zero is either brand new or somewhere nobody wants to live.
/// </para>
/// <para>
/// ⚠ <b>It refuses without a Ruleset rather than degrading</b>, on <c>--zones</c>' precedent: a
/// Ruleset is what declares a kind, and a table of nothing would read as a broken mechanism rather
/// than as a runner that was not told what to count.
/// </para>
/// </remarks>
internal static class KindDump
{
    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!Session.TryRules(options.RulesetPath, out Ruleset rules, out RulesetNames names))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, Ticks.Zero);

        output.WriteLine("# Borough Building kind dump");
        output.WriteLine(
            $"# {rules.KindCount} kind(s) declared, {world.Lots.Rows.LiveCount} Lots, "
            + $"{world.Buildings.Rows.LiveCount} Buildings, {world.Households.Rows.LiveCount} "
            + $"Households, {rules.ZoneRules.Length} Zone Rule(s).");

        output.WriteLine();
        output.WriteLine("## What each kind declares");
        Declared(output, rules, names);

        output.WriteLine();
        output.WriteLine("## The standing city — the populator's, before any sweep");
        Standing(output, world, rules, names);

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        output.WriteLine();
        output.WriteLine($"## The standing city — after {options.Ticks} Ticks");
        Standing(output, world, rules, names);

        output.WriteLine();
        output.WriteLine("## Zone Rules that can never build");
        Dead(output, world, rules, names);

        output.WriteLine();
        output.WriteLine("## What stands nowhere, and why");
        Absent(output, world, rules, names);

        return 0;
    }

    /// <summary>The Ruleset's own declarations, one row per kind.</summary>
    /// <remarks>
    /// <b>Printed beside the counts because a count alone cannot be read.</b> Zero standing means one
    /// thing for a kind no Zone Rule raises and another for a kind five of them fight over, and the
    /// reader should not have to open the TOML to tell which.
    /// </remarks>
    private static void Declared(TextWriter output, Ruleset rules, RulesetNames names)
    {
        output.WriteLine("  id  kind                  tenanted  parked  trade            serves      bins  rules");

        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            KindDefinition definition = rules.Kind(kind);

            string trade = definition.Business == 0
                ? "—"
                : names.BusinessKind(definition.Business) ?? $"#{definition.Business}";

            string serves = definition.IsService ? definition.Serves.ToString() : "—";

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {kind,2}  {Name(names, kind),-20}  {Truth(definition.Tenanted),-8}  "
                + $"{Truth(definition.Parked),-6}  {trade,-15}  {serves,-10}  "
                + $"{rules.BinsOf(kind).Length,4}  {rules.RulesOf(kind).Length,5}"));
        }
    }

    /// <summary>The counts, one row per kind, over the world as it stands now.</summary>
    private static void Standing(TextWriter output, World world, Ruleset rules, RulesetNames names)
    {
        int kinds = rules.KindCount;
        int[] standing = new int[kinds + 1];
        int[] shells = new int[kinds + 1];
        int[] floor = new int[kinds + 1];
        int[] ceiling = new int[kinds + 1];
        int[] holds = new int[kinds + 1];
        int[] trades = new int[kinds + 1];

        BuildingTable buildings = world.Buildings;
        LotTable lots = world.Lots;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (!buildings.Rows.IsLive(slot))
            {
                continue;
            }

            byte kind = buildings.Kind[slot];

            // A kind outside the Ruleset in force is not a defect to swallow: a reload can retire a
            // kind under a Building that is still standing, and this dump would rather drop it out of
            // the table than index past the end of one.
            if (kind == 0 || kind > kinds)
            {
                continue;
            }

            standing[kind]++;

            if (buildings.IsAbandoned(slot))
            {
                shells[kind]++;
            }

            if (lots.Rows.TryResolve(buildings.Lot[slot], out int lot))
            {
                floor[kind] += lots.FloorTiles(lot);
            }

            ceiling[kind] += world.DeclaredOccupancy(slot);

            foreach (int _ in world.Occupants.Walk(slot))
            {
                holds[kind]++;
            }

            foreach (int _ in world.BuildingBusinesses.Walk(slot))
            {
                trades[kind]++;
            }
        }

        output.WriteLine("  id  kind                  standing  shells    floor  ceiling    holds  trades  used");

        for (byte kind = 1; kind <= kinds; kind++)
        {
            // 🔴 BOTH KINDS OF TENANT, and the first cut of this column counted only Households --
            // which printed `used 0%` against a row of 24 shops each holding a Business, because one
            // ceiling counts both (adr/0147) and only half of it was in the numerator. A share whose
            // halves are denominated differently is plans/0012 Cause 5 inside one expression.
            //
            // Integer percent, and only where a denominator exists. A kind with no ceiling is not
            // 0% full, it is a kind nothing can live in -- adr/0003's rule about a ratio arriving as
            // two numbers rather than one, at the one place this dump would have needed a double.
            string used = ceiling[kind] == 0
                ? "—"
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"{((holds[kind] + trades[kind]) * 100) / ceiling[kind]}%");

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {kind,2}  {Name(names, kind),-20}  {standing[kind],8}  {shells[kind],6}  "
                + $"{floor[kind],7}  {ceiling[kind],7}  {holds[kind],7}  {trades[kind],6}  {used,5}"));
        }

        output.WriteLine(
            "`holds` counts TENANCIES and not people — a Household is one and so is a Business, "
            + "which is why `trades` is beside it rather than inside it (adr/0147: one ceiling "
            + "counts both kinds of tenant, so `used` is holds PLUS trades over the ceiling). "
            + "`floor` is Tiles across every storey, and `ceiling` is that over [capacity] "
            + "floor_tiles_per_occupant, so two kinds declaring the same thing differ here by the "
            + "ground they landed on and by nothing else. ⚠ A shell keeps its floor and its ceiling "
            + "and holds nobody, so a kind with shells reads low on `used` for a reason the column "
            + "beside it names.");
    }

    /// <summary>
    /// Names every kind that stands nowhere, and separates the three reasons it can happen.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The dead-rule case is the one no other instrument can report.</b> A Zone Rule admitting a
    /// bit no Lot carries is accepted at load, sweeps for ever and raises nothing, and every counter
    /// in <c>--census</c> reads exactly as it would for a rule that simply found no vacant Lot. The
    /// two are told apart here by the <em>painted</em> set, which is the union of the permission sets
    /// this world actually carries.
    /// </remarks>
    private static void Dead(TextWriter output, World world, Ruleset rules, RulesetNames names)
    {
        ushort painted = Painted(world);

        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"This world's Lots carry permission bits {Bits(painted)} between them, out of "
            + $"{LotTable.ZoneBits} a Lot can hold. A Zone Rule admitting any other bit is a rule "
            + $"that samples for ever and builds nothing — and it is not refused at load, because "
            + $"RulesetLoader can check a bit is in range and cannot check that anything paints it."));
        output.WriteLine();

        bool quiet = true;

        for (int position = 0; position < rules.ZoneRules.Length; position++)
        {
            ZoneRuleDefinition rule = rules.ZoneRules[position];

            if ((rule.Admits & painted) != 0)
            {
                continue;
            }

            quiet = false;

            output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  DEAD RULE — '{names.ZoneRule(position) ?? $"rule {position}"}' raises "
                + $"'{Name(names, rule.Kind)}' on bit {rule.Zone}, which no Lot in this world "
                + $"carries. It draws its sample every {rule.Interval} Ticks, for ever, and builds "
                + $"nothing."));
        }

        if (quiet)
        {
            output.WriteLine("  None. Every Zone Rule admits a bit this world paints.");
        }
    }

    /// <summary>
    /// Names every kind that stands nowhere, and separates the reasons it can happen.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THIS SECTION IS NOT WHERE A DEAD RULE IS REPORTED, AND THAT WAS THE FIRST CUT'S
    /// BUG.</b> It read the dead-rule case off a kind standing nowhere, which hid every dead rule
    /// naming a kind <see cref="SyntheticCity"/> also builds — and the populator builds kind 1, so
    /// moving <c>minimal.toml</c>'s only Zone Rule to an unpaintable bit produced a clean report.
    /// ***A rule that can never fire is dead whether or not something else raises its kind***, so
    /// <see cref="Dead"/> asks about rules and this asks about kinds.
    /// </remarks>
    private static void Absent(TextWriter output, World world, Ruleset rules, RulesetNames names)
    {
        ushort painted = Painted(world);
        bool quiet = true;

        for (byte kind = 1; kind <= rules.KindCount; kind++)
        {
            if (Stands(world, kind))
            {
                continue;
            }

            quiet = false;
            output.WriteLine($"  {Name(names, kind)}: {Why(rules, kind, painted)}");
        }

        if (quiet)
        {
            output.WriteLine(
                "  Every declared kind stands somewhere. That is the interesting outcome and the "
                + "rare one — it means no declaration in this file is inert.");
        }

    }

    /// <summary>Why a kind stands nowhere, in the terms the reader can act on.</summary>
    private static string Why(Ruleset rules, byte kind, ushort painted)
    {
        int raising = 0;
        int dead = 0;

        foreach (ZoneRuleDefinition rule in rules.ZoneRules)
        {
            if (rule.Kind != kind)
            {
                continue;
            }

            raising++;

            if ((rule.Admits & painted) == 0)
            {
                dead++;
            }
        }

        if (raising == 0)
        {
            return rules.Kind(kind).IsService
                ? "no Zone Rule raises it, which is correct for a service — the city does not site "
                + "its own schools (adr/0032). It stands when a `service` command places one."
                : "NO ZONE RULE RAISES IT and it is not a service, so nothing in this world can "
                + "ever build one. It is declared content with no way in.";
        }

        if (dead == raising)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"DEAD RULE — all {raising} Zone Rule(s) naming it admit a bit no Lot in this world "
                + $"carries. They sample every trigger and build nothing, for ever, with no refusal "
                + $"at load and no counter anywhere that reads differently.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{raising} Zone Rule(s) raise it on a bit this world paints, and none has yet won a "
            + $"Lot. Either the run is too short or the Lots it wants are occupied.");
    }

    /// <summary>Whether any live Building carries this kind.</summary>
    private static bool Stands(World world, byte kind)
    {
        BuildingTable buildings = world.Buildings;

        for (int slot = 0; slot < buildings.Rows.SlotCount; slot++)
        {
            if (buildings.Rows.IsLive(slot) && buildings.Kind[slot] == kind)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The union of every live Lot's permission set.</summary>
    private static ushort Painted(World world)
    {
        LotTable lots = world.Lots;
        ushort painted = 0;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (lots.Rows.IsLive(slot))
            {
                painted |= lots.Zone[slot];
            }
        }

        return painted;
    }

    /// <summary>A permission set as the bit indices it holds, which is how a Ruleset states one.</summary>
    private static string Bits(ushort set)
    {
        if (set == 0)
        {
            return "none at all";
        }

        List<string> bits = [];

        for (int bit = 0; bit < LotTable.ZoneBits; bit++)
        {
            if ((set & (1 << bit)) != 0)
            {
                bits.Add(bit.ToString(CultureInfo.InvariantCulture));
            }
        }

        return string.Join(", ", bits);
    }

    /// <summary>
    /// A kind's name, or its id when the Ruleset came from somewhere that kept no names.
    /// </summary>
    /// <remarks>
    /// <b>The shell owns every string a human reads</b> (<c>adr/0002</c>), and
    /// <see cref="RulesetNames.None"/> is a real case rather than a defensive one — a world loaded
    /// from a save has a Ruleset and no names table.
    /// </remarks>
    private static string Name(RulesetNames names, byte kind) =>
        names.Kind(kind) ?? $"kind {kind}";

    private static string Truth(bool value) => value ? "yes" : "no";
}
