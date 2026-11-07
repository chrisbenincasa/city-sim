using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Instruments;
using Borough.Core.Movement;
using Godot;

namespace Borough.Shell;

// The Needs panel words NeedsReading's counts: who wants a home, a job or a shop, and why vacant
// Lots stay empty.

public partial class Main
{
    private PanelContainer _needsPanel = null!;
    private VBoxContainer _needsBody = null!;
    private Button _needsButton = null!;
    private bool _needsShown;

    /// <summary>The Tick the sections were last read at, or -1 before the first read.</summary>
    private long _needsReadAt = -1;

    private List<(string Heading, List<string> Lines)> _needs = [];
    private string _needsSignature = string.Empty;

    private readonly ShoppingNeed[] _needsShopping = new ShoppingNeed[32];

    /// <summary>Ticks between reads. The Lot read is cold and allocating, so it is not taken every frame.</summary>
    private const int NeedsReadTicks = 32;

    private const int NeedsShoppingShown = 6;

    private void NeedsPanel(CanvasLayer layer)
    {
        _needsPanel = Backed(new Vector2(14f, 108f));
        _needsPanel.Theme = _type;
        _needsPanel.Visible = false;

        var box = new VBoxContainer();
        var heading = new HBoxContainer();

        heading.AddChild(new Label
        {
            Text = "What the city needs",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var close = InformationButton("Close Needs", () => Ui("needs off"));

        close.TooltipText = "Close the panel; keep the tool, layer and inspection";
        heading.AddChild(close);
        box.AddChild(heading);

        _needsBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_needsBody);
        ScrollAuxiliary(_needsPanel, box);
        layer.AddChild(_needsPanel);
    }

    /// <summary>The <c>ui needs on|off|refresh</c> grammar. Returns false when the words are not this panel's.</summary>
    private bool NeedsAction(string[] words)
    {
        if (words.Length != 2 || words[0] != "needs") return false;

        switch (words[1])
        {
            case "on" or "off":
                _needsShown = words[1] == "on";

                if (_needsShown)
                {
                    CloseLeftPanels();
                    _hud.MoveChild(_needsPanel, -1);
                    _needsReadAt = -1;
                }

                return true;
            case "refresh":
                _needsReadAt = -1;
                return true;
            default:
                return false;
        }
    }

    /// <summary>Closes every other panel on the left anchor, which holds one at a time.</summary>
    private void CloseLeftPanels()
    {
        if (_governing) Govern();
        if (_cityShown) { _cityShown = false; RefreshCityEvidence(); }
        if (_outsideShown) { _outsideShown = false; RefreshHinterlands(); }
    }

    private void RefreshNeeds()
    {
        _needsPanel.Visible = _needsShown;
        _needsButton.SetPressedNoSignal(_needsShown);

        if (!_needsShown) return;

        long now = (long)_world.Tick.Raw;

        if (_needsReadAt >= 0 && now < _needsReadAt + NeedsReadTicks && now >= _needsReadAt) return;

        _needsReadAt = now;
        _needs = ReadNeeds();

        string signature = $"{_textPercent}|" + string.Join("|", _needs.Select(s => $"{s.Heading}:{string.Join(";", s.Lines)}"));

        if (signature == _needsSignature) return;

        _needsSignature = signature;

        foreach (Node child in _needsBody.GetChildren())
        {
            _needsBody.RemoveChild(child);
            child.QueueFree();
        }

        bool first = true;

        foreach ((string title, List<string> lines) in _needs)
        {
            if (!first) _needsBody.AddChild(new HSeparator());

            first = false;
            _needsBody.AddChild(Heading(title));

            foreach (string line in lines)
            {
                Label label = InformationLabel(line, InformationUi.BodyPoints);

                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                _needsBody.AddChild(label);
            }
        }
    }

    /// <summary>The panel's text for a driven readout, or nothing while it is closed.</summary>
    private string NeedsCaption() => !_needsShown
        ? string.Empty
        : "\nneeds —\n" + string.Join("\n", _needs.Select(s => $"{s.Heading}\n  " + string.Join("\n  ", s.Lines)));

    private List<(string, List<string>)> ReadNeeds()
    {
        HomeNeeds homes = NeedsReading.Homes(_world);
        JobNeeds jobs = NeedsReading.Jobs(_world);
        int shopping = NeedsReading.Shopping(_world, _needsShopping);
        LotNeeds lots = NeedsReading.Lots(_world);

        return
        [
            ("Homes", HomeLines(homes)),
            ("Jobs", JobLines(jobs)),
            ("Shops", ShoppingLines(shopping)),
            ("Land", LotLines(lots)),
        ];
    }

    private static List<string> HomeLines(HomeNeeds homes)
    {
        var lines = new List<string>();

        if (homes.Looking == 0)
        {
            lines.Add("Nobody is looking for a home.");
        }
        else
        {
            lines.Add($"{Many(homes.Looking, "Household is", "Households are")} looking for a home.");

            if (homes.NoRoom > 0) lines.Add("No home in the city has room for them.");
            if (homes.CannotAfford > 0)
                lines.Add($"{Many(homes.CannotAfford, "can't", "can't")} afford the cheapest vacant home.");
            if (homes.CanAfford > 0)
                lines.Add($"{Many(homes.CanAfford, "can", "can")} afford a vacant home and {(homes.CanAfford == 1 ? "is" : "are")} waiting to move in.");
        }

        if (homes.VacantHomes > 0)
            lines.Add($"{Many(homes.VacantHomes, "Building has", "Buildings have")} room for another Household.");
        if (homes.LeftYesterday > 0)
            lines.Add($"{Many(homes.LeftYesterday, "Household", "Households")} gave up looking and left the city yesterday.");

        return lines;
    }

    private static List<string> JobLines(JobNeeds jobs)
    {
        var lines = new List<string>();

        if (jobs.NoVacancy > 0)
            lines.Add($"{Many(jobs.NoVacancy, "Citizen", "Citizens")} can't find a job, because every job near home is taken.");
        if (jobs.BeyondReach > 0)
            lines.Add($"{Many(jobs.BeyondReach, "Citizen", "Citizens")} can only find jobs too far to commute to.");
        if (jobs.BelowCredential > 0)
            lines.Add($"{Many(jobs.BelowCredential, "Citizen lacks", "Citizens lack")} the schooling the open jobs ask for.");
        if (lines.Count == 0)
            lines.Add("Nobody who is looking for work is going without.");

        lines.Add(jobs.OpenPosts > 0
            ? $"{Many(jobs.OpenPosts, "job is", "jobs are")} open."
            : "No business is hiring.");

        if (jobs.BusinessesLooking > 0)
            lines.Add($"{Many(jobs.BusinessesLooking, "Business is", "Businesses are")} waiting for premises to open in.");

        return lines;
    }

    private List<string> ShoppingLines(int written)
    {
        var lines = new List<string>();

        if (!_world.Rules.Shopping.Runs)
        {
            lines.Add("Nobody in this city goes shopping.");

            return lines;
        }

        for (int at = 0; at < Math.Min(written, NeedsShoppingShown); at++)
        {
            ShoppingNeed need = _needsShopping[at];
            string good = _names.Resource(need.Good) ?? "an unnamed Good";

            lines.Add($"{Many(need.Households, "Household", "Households")} can't buy {good}, because {ShoppingReason(need.Reason, need.Households)}.");
        }

        if (written > NeedsShoppingShown)
        {
            int rest = 0;

            for (int at = NeedsShoppingShown; at < written; at++) rest += _needsShopping[at].Households;

            lines.Add($"{Many(rest, "more Household", "more Households")} can't buy something else.");
        }

        if (lines.Count == 0) lines.Add("Every Household that went shopping found what it wanted.");

        return lines;
    }

    private static string ShoppingReason(ShoppingFailure reason, int many) => reason switch
    {
        ShoppingFailure.Empty => "the shops they know have sold out",
        ShoppingFailure.Unaffordable => many == 1 ? "it can't afford it" : "they can't afford it",
        ShoppingFailure.Closed => "the shops were closed",
        ShoppingFailure.Unreachable => "no shop selling it is within reach",
        ShoppingFailure.NoKnownShop => "they know no shop that sells it",
        ShoppingFailure.LostCargo => "what they bought was lost on the way home",
        _ => "of a reason the shell does not name",
    };

    private static List<string> LotLines(LotNeeds lots)
    {
        var lines = new List<string>();

        if (lots.Vacant == 0)
        {
            lines.Add("Every Lot is built on.");

            return lines;
        }

        lines.Add($"{Many(lots.Vacant, "Lot is", "Lots are")} empty.");

        if (lots.NobodySeeking > 0)
            lines.Add($"{Many(lots.NobodySeeking, "stays", "stay")} empty because nobody is looking for a home.");
        if (lots.NoCapital > 0)
            lines.Add($"{Many(lots.NoCapital, "stays", "stay")} empty because no Household can afford to build there.");
        if (lots.NotZoned > 0)
            lines.Add($"{Many(lots.NotZoned, "is", "are")} not zoned for anything that fits.");
        if (lots.NoFrontage > 0)
            lines.Add($"{Many(lots.NoFrontage, "touches", "touch")} no Street.");
        if (lots.Unexplained > 0)
            lines.Add($"{Many(lots.Unexplained, "hasn't", "haven't")} been considered for building yet.");

        return lines;
    }

    /// <summary>A count with the singular or plural words after it.</summary>
    private static string Many(long count, string one, string many) =>
        Text($"{count:N0} {(count == 1 ? one : many)}");
}
