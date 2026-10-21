using System.Collections.Generic;
using Borough.Core.Evidence;
using Borough.Core.Quantities;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    /// <summary>
    /// The ramp <see cref="UtilityColour"/> paints, nothing delivered to fully delivered.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><see cref="TroubleBands"/> read backwards, and the direction is the reading.</b> Trouble
    /// paints a magnitude of failure, so its bright end is bad; this paints a share of what was
    /// asked for, so its bright end is good. Two ramps a line apart in one shell is how a reader
    /// gets a green city the wrong way round.
    /// </remarks>
    private static readonly Color[] UtilityBands =
    [
        new Color("df6355"), new Color("e9c162"), new Color("78cfa0"),
    ];

    /// <summary>A Building with nothing to say about a Utility, under the Utilities wash.</summary>
    private static readonly Color Undrawn = new("6d726f");

    /// <summary>Whether the Utilities wash has a Day's worth of new fractions to paint.</summary>
    private bool _utilityRepaint;

    /// <summary>The Day the Utilities wash was last painted for.</summary>
    private ulong _utilityDay = ulong.MaxValue;

    /// <summary>
    /// What one Building's District network delivers of the first Utility it draws.
    /// </summary>
    /// <remarks>
    /// <b>The first Utility and not all of them</b>, because one body carries one colour. Which one
    /// is first is the Rules' own declaration order, so the picture does not change under a reader
    /// between passes.
    /// </remarks>
    private Color UtilityColour(int building)
    {
        if (!Evidence.TryUtilityOfBuilding(
                _world, _world.Buildings.Rows.At(building), out UtilityEvidence drawn))
        {
            return Undrawn;
        }

        float share = Mathf.Clamp(drawn.Delivered.Raw / 65_536f, 0f, 1f);
        float along = share * (UtilityBands.Length - 1);
        int stop = Mathf.Clamp((int)along, 0, UtilityBands.Length - 2);

        return UtilityBands[stop].Lerp(UtilityBands[stop + 1], along - stop);
    }

    /// <summary>What the Utilities wash is saying, for the readout beside it.</summary>
    /// <remarks>
    /// <b>It names the Day rather than the Tick.</b> One settle governs the whole Day's firings, so
    /// a Tick here would claim a resolution the delivery does not have. The Day is the HUD clock's,
    /// counted from 1, so the legend and the bar name the same Day.
    /// </remarks>
    private string UtilitiesLegend() =>
        "\nUTILITIES — the share of each draw the Building's District network delivers: red nothing, "
        + "amber part, green all of it. Grey draws no Utility at all. "
        + $"One fraction governs the whole of Day {(_world.Tick.Raw / (ulong)Ticks.PerDay) + 1:N0}. "
        + "Select a Building for the units behind it.";

    /// <summary>A delivered fraction as whole per cent, floored the way a firing floors it.</summary>
    /// <remarks>
    /// ⚠ <b>Floored rather than rounded, so 99% never reads as full.</b> A Rule applies
    /// <c>floor(count × delivered)</c> times, and a panel that rounds a brownout up to 100% is
    /// reporting a city the engine is not running.
    /// </remarks>
    private static int DeliveredPercent(Ratio delivered) => delivered.Raw * 100 / 65_536;

    /// <summary>A Utility and its delivered share, for one line of a panel.</summary>
    private string UtilitySentence(UtilityEvidence drawn)
    {
        string name = _names.Resource(drawn.Utility) ?? $"Resource {drawn.Utility.Raw}";

        return $"{name} delivered at {DeliveredPercent(drawn.Delivered)}%";
    }

    /// <summary>
    /// What each Utility the Building draws is delivering, and what its network carried yesterday.
    /// </summary>
    /// <remarks>
    /// <b>Open when something is short.</b> A full delivery is a line a reader can skip; a brownout
    /// is the explanation for every reduced output in the Building, and nothing else in this panel
    /// reports one — a delivered Utility never blocks a Rule.
    /// </remarks>
    private InformationSection UtilitiesSection(BuildingEvidence evidence)
    {
        var rows = new List<InformationRow>();
        bool anyShort = false;

        foreach (UtilityEvidence drawn in evidence.Utilities.Span)
        {
            anyShort |= drawn.Delivered < Ratio.One;

            string text = $"{UtilitySentence(drawn)}\nYesterday the District's network served "
                + $"{drawn.Received:N0} of the {drawn.Asked:N0} units asked of it.";
            if (drawn.HasTariff)
            {
                string name = _names.Resource(drawn.Utility) ?? $"Resource {drawn.Utility.Raw}";
                text += $"\nPaid {drawn.Paid:N0} for {name} yesterday.";
                if (drawn.Unpaid > 0) text += $" {drawn.Unpaid:N0} went unpaid.";
            }

            rows.Add(new(text, Icon: UiIcons.Resource(_names.Resource(drawn.Utility))));
        }

        return new("utilities", $"Utilities · {evidence.Utilities.Length}", anyShort, rows);
    }
}
