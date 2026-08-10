using System.Globalization;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;

namespace Borough.Headless;

/// <summary>
/// Prints the Lot grid by permission and occupancy, before and after a run, with what the sweep did
/// in between.
/// </summary>
/// <remarks>
/// <para>
/// <b>Slice 10's <em>something to look at</em></b>, and the second thing this project prints that is
/// not a number. The Map Layer dump showed a field; this shows a <em>city changing</em>, which is the
/// first defect class whose symptom is temporal rather than spatial. A Zone Rule that condemns
/// everything, or nothing, or the wrong Lot, produces a State Hash that moves and a census that looks
/// plausible — and is obvious the moment two grids sit next to each other.
/// </para>
/// <para>
/// <b>It requires a Ruleset and says so rather than degrading.</b> Sweeping is a Ruleset's
/// behaviour: with no <c>[[zone_rule]]</c> there is nothing to demonstrate, and a dump that printed
/// an unchanging grid would read as a broken mechanism instead of as an absent one. That is
/// <c>Mode.Layer</c>'s refusal in the other direction — it builds its own sources because no session
/// can place any; this one cannot invent its own Rules, because Rules are content.
/// </para>
/// <para>
/// <b>Every string here belongs to the shell</b> (<c>adr/0002</c>). <c>Borough.Core</c> hands over
/// Lot coordinates, permission bits and counters; the glyphs, the legend and the headings are this
/// file's.
/// </para>
/// </remarks>
internal static class ZoneDump
{
    /// <summary>
    /// Occupied, vacant-and-paintable, vacant-and-unzoned. Three states, because those are the three
    /// a reader has to be able to tell apart to judge a sweep.
    /// </summary>
    /// <remarks>
    /// A glyph rather than a permission digit: the question a picture answers is <em>is the city
    /// filling in or thinning out</em>, and a grid of bit patterns answers a different one. The CSV
    /// form beside it carries the permission set for anybody who wants it.
    /// </remarks>
    private const char Built = '#';
    private const char Vacant = '.';
    private const char Unzoned = ' ';

    /// <summary>Runs the demonstration and writes it to <paramref name="output"/>.</summary>
    internal static int Run(Options options, TextWriter output)
    {
        if (!Session.TryRules(options.RulesetPath, out Ruleset rules))
        {
            return 2;
        }

        var key = WorldKey.FromSeed(options.Seed);
        World world = new(options.Citizens, LayerRuleset.Default, rules);
        Simulation simulation = new(world, key) { VerifyDecideWritesNothing = false };

        SyntheticCity.PopulateInto(world, key, new Ticks(0));

        output.WriteLine("# Borough Zone dump");
        output.WriteLine(
            $"# {world.Lots.Rows.LiveCount} Lots, {world.Buildings.Rows.LiveCount} Buildings, "
            + $"{world.Households.Rows.LiveCount} Households, {rules.ZoneRules.Length} Zone Rules.");

        for (int i = 0; i < rules.ZoneRules.Length; i++)
        {
            ZoneRuleDefinition rule = rules.ZoneRules[i];
            output.WriteLine(
                $"#   rule {i}: kind {rule.Kind}, admits bit {rule.Zone}, every {rule.Interval} "
                + $"Ticks, {rule.Sample} Lots a trigger.");
        }

        output.WriteLine();
        output.WriteLine("## Before — the populator's city, one Building on every Lot");
        Write(output, world, options.Csv);

        simulation.Zoning.Drain();

        for (ulong tick = 0; tick < options.Ticks; tick++)
        {
            simulation.Step(default);
        }

        ZoneActivity activity = simulation.Zoning.Drain();

        output.WriteLine();
        output.WriteLine($"## After — {options.Ticks} Ticks of sweeping");
        Write(output, world, options.Csv);

        output.WriteLine();
        output.WriteLine("## What the sweep did");
        output.WriteLine(
            $"{activity.Triggers.Sum} triggers, {activity.Vacant.Sum} vacant Lots evaluated, "
            + $"{activity.Occupied.Sum} occupied. {activity.Created.Sum} Buildings raised, "
            + $"{activity.Demolished.Sum} condemned.");
        output.WriteLine(
            $"{world.UnplacedPool.Count} Households are in the Unplaced Pool, waiting for somewhere "
            + "to live. A demolition evicts a Building's whole occupancy; a Zone Rule rehouses one "
            + "Household per Building it raises, because a Building has no declared occupancy yet "
            + "(plans/0014 task 10).");

        return 0;
    }

    /// <summary>
    /// One row per North coordinate, one glyph per Lot.
    /// </summary>
    /// <remarks>
    /// <b>The grid is the Lots that exist rather than a window on the map</b>, which is the opposite
    /// choice from the Layer dump and is forced by the same reasoning. A Layer is dense — every Cell
    /// has a value, so a window is the only way to fit one on a terminal. Lots are sparse and painted
    /// wherever the populator put them, so a fixed window would print mostly nothing and would hide
    /// exactly the Lots a sweep had been busy on.
    /// </remarks>
    private static void Write(TextWriter output, World world, bool csv)
    {
        LotTable lots = world.Lots;
        int slots = lots.Rows.SlotCount;

        if (csv)
        {
            output.WriteLine("east,north,zone,building");

            for (int slot = 0; slot < slots; slot++)
            {
                if (!lots.Rows.IsLive(slot))
                {
                    continue;
                }

                int building = lots.BuildingOn(slot);

                output.WriteLine(string.Create(
                    CultureInfo.InvariantCulture,
                    $"{lots.East[slot].Raw},{lots.North[slot].Raw},{lots.Zone[slot]},{building}"));
            }

            return;
        }

        int east = 0;
        int north = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            if (lots.East[slot].Raw > east)
            {
                east = lots.East[slot].Raw;
            }

            if (lots.North[slot].Raw > north)
            {
                north = lots.North[slot].Raw;
            }
        }

        // Indexed by position rather than walked per row, because the Lot table is in creation order
        // and nothing guarantees that is scan order — CONTEXT's Lot is a parcel, not a grid cell.
        char[] grid = new char[(east + 1) * (north + 1)];
        Array.Fill(grid, Unzoned);

        int built = 0;
        int vacant = 0;

        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            bool occupied = !lots.IsVacant(slot);
            grid[(lots.North[slot].Raw * (east + 1)) + lots.East[slot].Raw] =
                occupied ? Built : Vacant;

            if (occupied)
            {
                built++;
            }
            else
            {
                vacant++;
            }
        }

        output.WriteLine(
            $"{built} built, {vacant} vacant. '{Built}' holds a Building, '{Vacant}' is a Lot with "
            + "none, blank is no Lot at all.");

        for (int row = north; row >= 0; row--)
        {
            output.WriteLine(new string(grid, row * (east + 1), east + 1).TrimEnd());
        }
    }
}
