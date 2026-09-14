using System;
using System.Collections.Generic;
using Borough.Core.Entities;
using Borough.Core.Instruments;
using Borough.Core.Quantities;
using Borough.Core.Space;
using Godot;

namespace Borough.Shell;

// ---- the Outside -- who is out there, who is waiting at a door, and who got in
//
// plans/0073 task 7. Borough.Headless's --arrivals answers the same question for a reader with a
// terminal; this answers it for the player who has just raised a door and wants to know whether
// anybody came through it.
//
// Every figure here is read through Core's instruments and none is computed in the shell. A panel
// that did its own arithmetic over the tables would be a second opinion about the city, and the one
// that is invisible to every test in the suite.

public partial class Main
{
    private PanelContainer _outsidePanel = null!;
    private VBoxContainer _outsideBody = null!;
    private Button _outsideButton = null!;
    private bool _outsideShown;

    /// <summary>Which edge's detail is open, or <c>None</c> when the player has opened none.</summary>
    private MapEdge _outsideEdge = MapEdge.None;

    private string _outsideSignature = string.Empty;

    /// <summary>
    /// What the panel's SHAPE depends on, as against what its figures do.
    /// </summary>
    /// <remarks>
    /// <b>The selected edge and what stands behind it</b>, because a group retiring or a door coming
    /// down changes how many rows there are rather than what any row says. The type scale is the
    /// third, as everywhere: a label is sized at the moment it is made.
    /// </remarks>
    private string _outsideShape = string.Empty;

    /// <summary>The four edges in the order every reader of this city states them.</summary>
    private static readonly MapEdge[] OutsideEdges =
        [MapEdge.West, MapEdge.East, MapEdge.South, MapEdge.North];

    /// <summary>
    /// Read buffers, held so a refresh allocates nothing.
    /// </summary>
    /// <remarks>
    /// <b>The instruments fill a caller's Span for exactly this reason.</b> A panel refreshed on
    /// every collected batch that allocated two arrays a frame would be a garbage collector attached
    /// to a readout. A city with more doors or more compositions than these fills what fits and the
    /// panel says so rather than growing without a bound the player can see.
    /// </remarks>
    private readonly HinterlandGateReading[] _outsideGates = new HinterlandGateReading[32];

    private readonly HinterlandGroupReading[] _outsideGroups = new HinterlandGroupReading[64];

    private Button[] _outsideEdgeButtons = [];
    private Label[] _outsideEdgeFigures = [];
    private Label[] _outsideAccount = [];
    private Label[] _outsideGateFigures = [];
    private Label[] _outsideGroupFigures = [];
    private Label[] _outsideFlowFigures = [];
    private Label _outsideDetail = null!, _outsideQueue = null!;

    /// <summary>Households per figure column, and the widest a name column may be.</summary>
    private const int OutsideFigureWidth = 74;

    private const int OutsideLabelWidth = 96;

    private const int OutsideColumnGap = 8;

    /// <summary>The six figures each edge row states, after the edge's own name.</summary>
    private const int OutsideEdgeColumns = 6;

    /// <summary>The flow counters the detail states: five, five and three, in three grids.</summary>
    private static readonly int[] OutsideFlowGrids = [5, 5, 3];

    private void HinterlandPanel(CanvasLayer layer)
    {
        _outsidePanel = Backed(new Vector2(14f, 108f));
        _outsidePanel.Theme = _type;
        _outsidePanel.Visible = false;

        var box = new VBoxContainer();
        var heading = new HBoxContainer();

        heading.AddChild(new Label
        {
            Text = "The Outside",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var close = InformationButton("Close Outside", () => Ui("outside off"));

        close.TooltipText = "Close the panel; keep the tool, layer and inspection";
        heading.AddChild(close);
        box.AddChild(heading);

        _outsideBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_outsideBody);
        ScrollAuxiliary(_outsidePanel, box);
        layer.AddChild(_outsidePanel);
    }

    /// <summary>The <c>ui outside …</c> grammar. Returns false when the words are not this panel's.</summary>
    private bool HinterlandAction(string[] words)
    {
        if (words.Length < 2 || words[0] != "outside") return false;

        switch (words[1])
        {
            case "on" or "off":
                _outsideShown = words[1] == "on";

                // The left anchor holds one panel at a time, as Government and City Evidence
                // already agree between themselves.
                if (_outsideShown && _governing) Govern();
                if (_outsideShown && _cityShown) { _cityShown = false; RefreshCityEvidence(); }
                if (_outsideShown) _hud.MoveChild(_outsidePanel, -1);

                return true;
            case "edge" when words.Length == 3:
                _outsideEdge = EdgeNamed(words[2]);
                _outsideSignature = string.Empty;

                if (!_outsideShown) HinterlandAction(["outside", "on"]);

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Writes what the Outside now says into the labels the panel is already holding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Reading the Outside changes nothing about it</b>, which is the contract
    /// <c>HinterlandReading</c> is written to and <c>A_reading_moves_no_state</c> asserts. No draw is
    /// consumed and no meter is reset, so a panel open all session and a panel opened once see the
    /// same city.
    /// </para>
    /// <para>
    /// ⚠ <b>The rollover is <c>Simulation</c>'s and never this panel's</b> (plans/0073 D10). Yesterday
    /// is whatever today was when the Day turned, whether or not anybody was looking, and a Day
    /// nobody watched still ends.
    /// </para>
    /// </remarks>
    private void RefreshHinterlands()
    {
        bool stated = _world.Rules.Immigration.Stated;

        _outsideButton.Visible = stated;
        _outsidePanel.Visible = _outsideShown;
        _outsideButton.SetPressedNoSignal(_outsideShown);

        if (!_outsideShown) return;

        PopulationReading account = PopulationReading.Of(_world);
        int gates = 0, groups = 0;

        if (_outsideEdge != MapEdge.None)
        {
            gates = HinterlandGateReading.Of(_world, _outsideEdge, _outsideGates);
            groups = HinterlandGroupReading.Of(_world, _outsideEdge, _outsideGroups);
        }

        string shape = $"{stated}|{_outsideEdge}|{gates}|{groups}|{_textPercent}";

        if (shape != _outsideShape)
        {
            _outsideShape = shape;
            _outsideSignature = string.Empty;
            BuildHinterlandBody(stated, gates, groups);
        }

        if (!stated) return;

        string signature = $"{account.Day}|{account.People}|{account.Households}"
            + $"|{account.PoolHouseholds}|{account.PoolPeople}|{account.Residual}"
            + $"|{account.Ever.Births}|{account.Ever.Admissions}";

        foreach (MapEdge edge in OutsideEdges)
        {
            HinterlandReading reading = HinterlandReading.Of(_world, edge);

            signature += $"|{reading.StockHouseholds},{reading.AvailableHouseholds}"
                + $",{reading.StockPeople},{reading.QueueHouseholds},{reading.Gates}"
                + $",{reading.RestingHouseholds}";

            if (edge == _outsideEdge) signature += $"|{reading.Today}|{reading.Yesterday}";
        }

        if (signature == _outsideSignature) return;

        _outsideSignature = signature;
        WriteHinterlands(account, gates, groups);
    }

    /// <summary>
    /// Makes the panel's rows once, keeping every label the refresh will write into.
    /// </summary>
    /// <remarks>
    /// <b>Four edge rows whatever the city has done</b>, so an edge with no door and no stock is
    /// visible as an edge with no door rather than absent from a list of four. ***A panel that omits
    /// what is empty cannot be read for what is missing.***
    /// </remarks>
    /// <param name="stated">Whether this Ruleset states an <c>[immigration]</c> table at all.</param>
    /// <param name="gates">How many doors stand on the open edge.</param>
    /// <param name="groups">How many compositions stand behind it.</param>
    private void BuildHinterlandBody(bool stated, int gates, int groups)
    {
        foreach (Node child in _outsideBody.GetChildren())
        {
            _outsideBody.RemoveChild(child);
            child.QueueFree();
        }

        _outsideEdgeButtons = [];
        _outsideEdgeFigures = [];
        _outsideAccount = [];
        _outsideGateFigures = [];
        _outsideGroupFigures = [];
        _outsideFlowFigures = [];

        if (!stated)
        {
            // A file with no counted Outside has no Households standing anywhere to report, and four
            // rows of zeroes would read as an empty world rather than as a Ruleset that states no
            // Outside -- ArrivalDump.Refuse's polarity, where refusing outright is not an option
            // because the player opened the panel.
            _outsideBody.AddChild(Wrapped(
                "This city states no [immigration] table, so nobody stands outside it and nobody "
                + "decides to come. Arrivals here are presented by an explicit command. Load a "
                + "Ruleset with a counted Outside — rulesets/attracted.toml is the demonstration."));

            return;
        }

        var edges = new GridContainer { Columns = OutsideEdgeColumns + 1 };

        edges.AddThemeConstantOverride("h_separation", Typed(OutsideColumnGap));
        edges.AddChild(Named("edge"));

        foreach (string column in
            (string[])["doors", "Households", "free", "resting", "people", "waiting"])
        {
            edges.AddChild(Cell(column));
        }

        _outsideEdgeButtons = new Button[OutsideEdges.Length];
        _outsideEdgeFigures = new Label[OutsideEdges.Length * OutsideEdgeColumns];

        for (int row = 0; row < OutsideEdges.Length; row++)
        {
            MapEdge edge = OutsideEdges[row];
            Button open = InformationButton(EdgeName(edge), () => Ui($"outside edge {Spelt(edge)}"));

            open.TooltipText = $"Show what stands behind the {Spelt(edge)} edge";
            open.ToggleMode = true;
            open.Alignment = HorizontalAlignment.Left;
            open.CustomMinimumSize = new Vector2(Typed(OutsideLabelWidth), 0f);
            _outsideEdgeButtons[row] = open;
            edges.AddChild(open);

            for (int column = 0; column < OutsideEdgeColumns; column++)
            {
                Label figure = Cell(string.Empty);

                _outsideEdgeFigures[(row * OutsideEdgeColumns) + column] = figure;
                edges.AddChild(figure);
            }
        }

        _outsideBody.AddChild(edges);
        _outsideBody.AddChild(Wrapped(
            "`free` is stock nobody has promised to a waiting Household. `resting` is the count the "
            + "Outside recovers towards."));
        _outsideBody.AddChild(new HSeparator());

        var city = new GridContainer { Columns = 4 };

        city.AddThemeConstantOverride("h_separation", Typed(OutsideColumnGap));
        _outsideAccount = new Label[8];

        AccountRow(city, 0, "In the city");
        AccountRow(city, 2, "Since the founding");
        AccountRow(city, 4, "Waiting outside");
        AccountRow(city, 6, "Admitted, looking");
        _outsideBody.AddChild(city);
        _outsideBody.AddChild(_outsideQueue = Wrapped(string.Empty));
        _outsideBody.AddChild(new HSeparator());

        if (_outsideEdge == MapEdge.None)
        {
            _outsideBody.AddChild(_outsideDetail = Wrapped(
                "Choose an edge above to see the compositions standing behind it, its doors, and "
                + "every outcome an occasion had today and over the last complete Day."));

            return;
        }

        _outsideBody.AddChild(_outsideDetail = Wrapped(string.Empty));
        BuildOutsideGates(gates);
        BuildOutsideGroups(groups);
        BuildOutsideFlows();
    }

    private void BuildOutsideGates(int gates)
    {
        _outsideBody.AddChild(Heading($"Doors · {gates}"));

        if (gates == 0)
        {
            _outsideBody.AddChild(Wrapped(
                "No door stands on this edge, so nobody can cross here however many are willing. "
                + "The stock behind it is untouched and is not lost."));

            return;
        }

        var grid = new GridContainer { Columns = 4 };

        grid.AddThemeConstantOverride("h_separation", Typed(OutsideColumnGap));
        grid.AddChild(Named("door"));
        grid.AddChild(Cell("a Day"));
        grid.AddChild(Cell("admitted"));
        grid.AddChild(Cell("left"));
        _outsideGateFigures = new Label[gates * 4];

        for (int row = 0; row < gates; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                Label figure = column == 0 ? Named(string.Empty) : Cell(string.Empty);

                _outsideGateFigures[(row * 4) + column] = figure;
                grid.AddChild(figure);
            }
        }

        _outsideBody.AddChild(grid);
        _outsideBody.AddChild(Wrapped(
            "A door has a quota and not a market: every door on this edge draws on the one stock "
            + "above."));
    }

    private void BuildOutsideGroups(int groups)
    {
        _outsideBody.AddChild(Heading($"Who is out there · {groups}"));

        if (groups == 0)
        {
            _outsideBody.AddChild(Wrapped(
                "Nobody stands behind this edge. Either the Ruleset states no [[hinterland]] table "
                + "for it, or every group it stated has been admitted and has not yet recovered."));

            return;
        }

        var grid = new GridContainer { Columns = 6 };

        grid.AddThemeConstantOverride("h_separation", Typed(OutsideColumnGap));
        grid.AddChild(Named("stage"));

        foreach (string column in (string[])["a home", "Households", "free", "admitted", "returned"])
        {
            grid.AddChild(Cell(column));
        }

        _outsideGroupFigures = new Label[groups * 6];

        for (int row = 0; row < groups; row++)
        {
            for (int column = 0; column < 6; column++)
            {
                Label figure = column == 0 ? Named(string.Empty) : Cell(string.Empty);

                _outsideGroupFigures[(row * 6) + column] = figure;
                grid.AddChild(figure);
            }
        }

        _outsideBody.AddChild(grid);
        _outsideBody.AddChild(Wrapped(
            "`a home` is people per Household. A stage marked · was made by an emigration coming "
            + "back rather than stated, and it decays rather than recovers."));
    }

    /// <summary>
    /// The three flow grids: who considered coming, what became of them, and the Outside's own
    /// arithmetic.
    /// </summary>
    /// <remarks>
    /// ⚠ <b><c>reviewed</c> stands apart from the four fresh outcomes and is never added to them.</b>
    /// It counts second thoughts by families already waiting, so a panel that summed it with the
    /// fresh interest would report the same family twice.
    /// </remarks>
    private void BuildOutsideFlows()
    {
        string[][] columns =
        [
            ["occasions", "no door", "no sample", "stayed", "willing"],
            ["admitted", "queued", "reviewed", "gave up", "went home"],
            ["replenished", "turnover", "door lost"],
        ];

        string[] titles =
        [
            "Who considered coming",
            "What became of them",
            "The Outside's own arithmetic",
        ];

        int figures = 0;

        foreach (int width in OutsideFlowGrids) figures += width * 2;

        _outsideFlowFigures = new Label[figures];

        int written = 0;

        for (int table = 0; table < OutsideFlowGrids.Length; table++)
        {
            _outsideBody.AddChild(Heading(titles[table]));

            var grid = new GridContainer { Columns = OutsideFlowGrids[table] + 1 };

            grid.AddThemeConstantOverride("h_separation", Typed(OutsideColumnGap));
            grid.AddChild(Named(string.Empty));

            foreach (string column in columns[table]) grid.AddChild(Cell(column));

            foreach (string when in (string[])["today", "yesterday"])
            {
                grid.AddChild(Named(when));

                for (int column = 0; column < OutsideFlowGrids[table]; column++)
                {
                    Label figure = Cell(string.Empty);

                    _outsideFlowFigures[written++] = figure;
                    grid.AddChild(figure);
                }
            }

            _outsideBody.AddChild(grid);
        }

        _outsideBody.AddChild(Wrapped(
            "The four outcomes are exclusive and add up to `occasions`. `no sample` found no home "
            + "worth weighing the Outside against, which is a different city from `stayed`, who "
            + "compared and preferred where they live."));
    }

    private void WriteHinterlands(PopulationReading account, int gates, int groups)
    {
        int waiting = 0;
        long waitingPeople = 0;
        ulong oldest = 0;

        for (int row = 0; row < OutsideEdges.Length; row++)
        {
            MapEdge edge = OutsideEdges[row];
            HinterlandReading reading = HinterlandReading.Of(_world, edge);
            int at = row * OutsideEdgeColumns;

            _outsideEdgeButtons[row].SetPressedNoSignal(edge == _outsideEdge);
            _outsideEdgeFigures[at].Text = Text($"{reading.Gates:N0}");
            _outsideEdgeFigures[at + 1].Text = Text($"{reading.StockHouseholds:N0}");
            _outsideEdgeFigures[at + 2].Text = Text($"{reading.AvailableHouseholds:N0}");
            _outsideEdgeFigures[at + 3].Text = Text($"{reading.RestingHouseholds:N0}");
            _outsideEdgeFigures[at + 4].Text = Text($"{reading.StockPeople:N0}");
            _outsideEdgeFigures[at + 5].Text = Text($"{reading.QueueHouseholds:N0}");

            waiting += reading.QueueHouseholds;
            waitingPeople += reading.QueuePeople;

            if (reading.OldestWait > oldest) oldest = reading.OldestWait;
        }

        _outsideAccount[0].Text = Text($"{account.People:N0} people");
        _outsideAccount[1].Text = Text($"{account.Households:N0} Households");
        _outsideAccount[2].Text = Text($"{account.Ever.Births:N0} born here");
        _outsideAccount[3].Text = Text($"{account.Ever.Admissions:N0} came in");
        _outsideAccount[4].Text = Text($"{waiting:N0} Households");
        _outsideAccount[5].Text = Text($"{waitingPeople:N0} people");
        _outsideAccount[6].Text = Text($"{account.PoolHouseholds:N0} Households");
        _outsideAccount[7].Text = Text($"{account.PoolPeople:N0} people");

        _outsideQueue.Text =
            $"Waiting outside is at a full door and still in the stock above, with no Citizen row "
            + $"anywhere; the longest has waited {Days(oldest)}. Admitted and looking is inside the "
            + $"city, searching. Residual {account.Residual:N0}: an account check, and not a reading "
            + "of how the city is doing.";

        if (_outsideEdge == MapEdge.None) return;

        HinterlandReading open = HinterlandReading.Of(_world, _outsideEdge);

        _outsideDetail.Text =
            $"{EdgeName(_outsideEdge)} · Day {open.Day} · {open.StockHouseholds:N0} Households "
            + $"({open.StockPeople:N0} people) behind it, {open.ReservedHouseholds:N0} promised to "
            + $"somebody waiting. {open.QueueHouseholds:N0} wait at a full door, the longest for "
            + $"{Days(open.OldestWait)}. Its doors have taken {open.AdmittedToday:N0} today and can "
            + $"take {open.RemainingToday:N0} more.";

        for (int row = 0; row < gates; row++)
        {
            HinterlandGateReading door = _outsideGates[row];
            int at = row * 4;

            _outsideGateFigures[at].Text = _names.Kind(door.Kind) ?? "Outside Connection";
            _outsideGateFigures[at + 1].Text = Text($"{door.Ceiling:N0}");
            _outsideGateFigures[at + 2].Text = Text($"{door.AdmittedToday:N0}");
            _outsideGateFigures[at + 3].Text = Text($"{door.RemainingToday:N0}");
        }

        for (int row = 0; row < groups; row++)
        {
            HinterlandGroupReading group = _outsideGroups[row];
            int at = row * 6;

            _outsideGroupFigures[at].Text = _names.LifeStage(group.Composition.Stage)
                ?? Text($"stage {group.Composition.Stage}");
            _outsideGroupFigures[at + 1].Text = Text($"{group.Composition.Members:N0}");
            _outsideGroupFigures[at + 2].Text = Text($"{group.Stock:N0}");
            _outsideGroupFigures[at + 3].Text = Text($"{group.Free:N0}");
            _outsideGroupFigures[at + 4].Text = Text($"{group.Admitted:N0}");
            _outsideGroupFigures[at + 5].Text = Text($"{group.Returned:N0}");

            if (!group.Authored) _outsideGroupFigures[at].Text += " ·";
        }

        WriteOutsideFlows(open);
    }

    private void WriteOutsideFlows(HinterlandReading reading)
    {
        int written = 0;

        for (int table = 0; table < OutsideFlowGrids.Length; table++)
        {
            foreach (HinterlandFlows flows in (HinterlandFlows[])[reading.Today, reading.Yesterday])
            {
                long[] figures = table switch
                {
                    0 =>
                    [
                        flows.Occasions, flows.NoConnection, flows.NoSample,
                        flows.StayedOutside, flows.Willing,
                    ],
                    1 =>
                    [
                        flows.Admitted, flows.Queued, flows.Reviewed,
                        flows.Expired, flows.ChangedMind,
                    ],
                    _ => [flows.Replenished, flows.Turnover, flows.ConnectionLost],
                };

                foreach (long figure in figures)
                {
                    _outsideFlowFigures[written++].Text = Text($"{figure:N0}");
                }
            }
        }
    }

    /// <summary>
    /// The gate's own quota, on the Building the player is inspecting.
    /// </summary>
    /// <remarks>
    /// <b>The quota is this door's and the stock is the edge's</b>, which is why the section states
    /// the one and links to the other. A reader shown an edge's Households under one door's heading
    /// would take the market for the door's own.
    /// </remarks>
    private void AddGateOutside(List<InformationSection> sections, int slot)
    {
        byte kind = _world.Buildings.Kind[slot];

        if (!_world.IsOutsideConnection(kind)) return;
        if (!_world.Lots.Rows.TryResolve(_world.Buildings.Lot[slot], out int lot)) return;

        MapEdge edge = _world.EdgeOf(lot);

        if (edge == MapEdge.None) return;

        HinterlandReading reading = HinterlandReading.Of(_world, edge);
        int gates = HinterlandGateReading.Of(_world, edge, _outsideGates);
        ulong id = _world.Buildings.Rows.IdAt(slot);
        var rows = new List<InformationRow>();

        for (int row = 0; row < gates; row++)
        {
            if (_outsideGates[row].Building != id) continue;

            rows.Add(new($"Admitted today · {_outsideGates[row].AdmittedToday:N0}"
                + $" of {_outsideGates[row].Ceiling:N0}"));
            rows.Add(new($"Room left today · {_outsideGates[row].RemainingToday:N0}"));
        }

        if (!_world.Rules.Immigration.Stated)
        {
            rows.Add(new("Nobody decides to come in this city: arrivals here are presented by an "
                + "explicit command, and this door only sets a ceiling on them."));
            sections.Add(new("outside", $"Outside Connection · {EdgeName(edge)}", true, rows));

            return;
        }

        rows.Add(new(reading.Gates > 1
            ? $"Behind this edge · {reading.StockHouseholds:N0} Households, shared with the "
                + $"{reading.Gates - 1:N0} other doors on it"
            : $"Behind this edge · {reading.StockHouseholds:N0} Households, and this is the only "
                + "door drawing on them"));
        rows.Add(new($"Waiting at a full door · {reading.QueueHouseholds:N0} Households"));
        rows.Add(new($"The {Spelt(edge)} Outside →", $"outside edge {Spelt(edge)}"));
        sections.Add(new("outside", $"Outside Connection · {EdgeName(edge)}", true, rows));
    }

    /// <summary>Every door on every edge, for a driven check to hold the panel against.</summary>
    /// <remarks>
    /// <b>Off the reading and not off the panel's labels</b>, for the reason the Budget's own
    /// published figures state: a scrape reads what the panel formatted, and what a check wants is
    /// what the panel read.
    /// </remarks>
    private HinterlandGateReading[] OutsideDoors()
    {
        var doors = new List<HinterlandGateReading>();
        var buffer = new HinterlandGateReading[32];

        foreach (MapEdge edge in OutsideEdges)
        {
            int written = HinterlandGateReading.Of(_world, edge, buffer);

            for (int door = 0; door < written; door++) doors.Add(buffer[door]);
        }

        return [.. doors];
    }

    private void AccountRow(GridContainer grid, int at, string name)
    {
        grid.AddChild(Named(name));
        grid.AddChild(_outsideAccount[at] = Cell(string.Empty));
        grid.AddChild(_outsideAccount[at + 1] = Cell(string.Empty));
        grid.AddChild(Cell(string.Empty));
    }

    private Label Heading(string text)
    {
        Label label = InformationLabel(text, SecondaryPoints);

        label.AutowrapMode = TextServer.AutowrapMode.Off;

        return label;
    }

    /// <summary>A figure column, stating its own width so the rows line up under their headings.</summary>
    private Label Cell(string text)
    {
        Label label = Fixed(text, SecondaryPoints);

        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.CustomMinimumSize = new Vector2(Typed(OutsideFigureWidth), 0f);

        return label;
    }

    private static string EdgeName(MapEdge edge) => edge switch
    {
        MapEdge.West => "West",
        MapEdge.East => "East",
        MapEdge.South => "South",
        MapEdge.North => "North",
        _ => "Nowhere",
    };

    private static string Spelt(MapEdge edge) => EdgeName(edge).ToLowerInvariant();

    private static MapEdge EdgeNamed(string word) => word switch
    {
        "west" => MapEdge.West,
        "east" => MapEdge.East,
        "south" => MapEdge.South,
        "north" => MapEdge.North,
        _ => MapEdge.None,
    };

    /// <summary>A wait in Days, because nobody watching a city thinks in Ticks.</summary>
    private static string Days(ulong ticks) => Text($"{ticks / (ulong)Ticks.PerDay:N0}d");
}
