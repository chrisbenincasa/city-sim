using System;
using System.Globalization;
using Borough.Core;
using Borough.Core.Instruments;
using Godot;

namespace Borough.Shell;

// ---- the budget -- what the city took in, what it paid out, and what it is holding
//
// plans/0072 Phase D. Borough.Headless's --income answers the same question for a reader with a
// terminal; this answers it for the player who is turning the rates two panels away, and it is the
// clause of amnesty row 33 that says a budget is something a PLAYER reads.
//
// The arithmetic is CityBudget's and is deliberately not here: a Control is invisible to every test
// in the suite, and the rolling, the running totals and the residual are the half that can be wrong
// quietly.

public partial class Main
{
    private PanelContainer _budgetPanel = null!;
    private VBoxContainer _budgetBody = null!;
    private Button _budgetButton = null!;
    private bool _budgetShown;
    private string _budgetSignature = string.Empty;

    /// <summary>
    /// What the panel's SHAPE depends on, as against what its figures do.
    /// </summary>
    /// <remarks>
    /// <b>Two things and neither of them is a number the city produced.</b> A city with no treasury
    /// gets a sentence where a banked one gets a table, and a label is sized at the moment it is
    /// made — so the type scale is the other thing a standing row cannot absorb.
    /// </remarks>
    private string _budgetShape = string.Empty;

    private Label _budgetTreasury = null!, _budgetOpening = null!, _budgetOpeningRow = null!;
    private Label _budgetDayHeading = null!, _budgetRunHeading = null!;
    private Label _budgetExplained = null!, _budgetResidual = null!;

    /// <summary>The eight flows by three columns, row-major. Empty on a city with no treasury.</summary>
    private Label[] _budgetFigures = [];

    /// <summary>
    /// The treasury's account since the shell took hold of this world, or null before it has one.
    /// </summary>
    /// <remarks>
    /// <b>Opened by <c>InstallCity</c> and never by a reload of the panel</b>, because it is a
    /// property of the run rather than of the view. Closing the panel and opening it again must not
    /// restart the running totals, and a world replaced by a tune or a load must.
    /// </remarks>
    private CityBudget? _budget;

    /// <summary>
    /// How wide the three figure columns are. Fixed so the rows line up under their headings
    /// whatever the numbers do.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>These widths are a FLOOR on how narrow the panel can be, not a preference.</b>
    /// <c>ScrollAuxiliary</c> disables horizontal scrolling, so a body wider than the space it is
    /// given makes the panel wider rather than making it scroll — and a right-anchored panel then
    /// runs off the screen, taking the figures with it. Measured at the 1,280 px design minimum with
    /// Government open: it ends at 664, the panel takes <b>582</b> px and starts at 674, and its
    /// right edge lands on the margin at 1,256.
    /// </remarks>
    private const int BudgetFigureWidth = 96;

    private const int BudgetLabelWidth = 140;

    private const int BudgetColumnGap = 10;

    /// <summary>Opens the treasury's account against a world the shell has just installed.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The opening balance is read here and not assumed to be zero.</b> A world loaded from a
    /// save arrives holding whatever it held when it was written, and flows are an instrument's
    /// business and are not saved — so a running total counted from zero would report the save's
    /// entire past as this session's income.
    /// </para>
    /// <para>
    /// 🔴 <b>The discarded drain is not tidiness.</b> <c>CityPreparation</c> runs every Tick up to
    /// <c>--start-at</c> on its own thread and nothing drains the engines while it does, so the
    /// accumulators arrive holding the whole pre-roll. Without this line the first Tick after the
    /// handoff would be credited with every payday, levy and subsidy of however many Days the shell
    /// fast-forwarded through — and the opening balance already accounts for all of them.
    /// </para>
    /// </remarks>
    private void OpenBudget()
    {
        _ownedSimulation.DrainTreasuryFlows();
        _budget = new CityBudget(_ownedWorld.Tick, _ownedWorld.TreasuryBalance()?.Raw ?? 0);
        _budgetSignature = string.Empty;
    }

    /// <summary>
    /// Folds one Tick's treasury movements into the account.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>Called from inside the step batch and therefore from the SIMULATION THREAD.</b> It
    /// touches no Godot object and no shell field the frame reads before <c>TryComplete</c> returns
    /// the world, which is <c>SimulationThread</c>'s own handoff discipline. ⚠ <b>Per Tick and not
    /// per frame</b>: a batch is up to four Ticks and can cross a Day boundary, and an account
    /// folded after the batch would put part of one Day into another silently.
    /// </remarks>
    private void Account()
    {
        TreasuryFlows flows = _ownedSimulation.DrainTreasuryFlows();

        _budget?.Account(flows, _ownedWorld.Tick);
    }

    /// <summary>
    /// Steps one Tick and accounts for what it moved across the treasury's edge.
    /// </summary>
    /// <remarks>
    /// <b>The one step surface the shell hands to <see cref="SimulationThread"/> and to its own
    /// main-thread path</b>, so the account cannot be kept on one and dropped on the other. The
    /// drain is the shell's only one — see <c>Simulation.DrainTreasuryFlows</c>, which a Census
    /// would otherwise be competing with for the same movements.
    /// </remarks>
    /// <param name="input">The Tick's commands, or <c>default</c> after the first of a batch.</param>
    private void StepAccounting(in TickInput input)
    {
        _ownedSimulation.Step(input);
        Account();
    }

    private void BudgetPanel(CanvasLayer layer)
    {
        _budgetPanel = Backed(new Vector2(14f, 108f));
        _budgetPanel.Theme = _type;
        _budgetPanel.Visible = false;

        var box = new VBoxContainer();
        var heading = new HBoxContainer();

        heading.AddChild(new Label
        {
            Text = "The city's budget",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });

        var close = InformationButton("Close Budget", () => Ui("budget off"));

        close.TooltipText = "Close the budget; the account keeps running";
        heading.AddChild(close);
        box.AddChild(heading);

        _budgetBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_budgetBody);
        ScrollAuxiliary(_budgetPanel, box);
        layer.AddChild(_budgetPanel);
    }

    /// <summary>The <c>ui budget …</c> grammar. Returns false when the words are not this panel's.</summary>
    private bool BudgetAction(string[] words)
    {
        if (words.Length != 2 || words[0] != "budget" || words[1] is not ("on" or "off"))
        {
            return false;
        }

        _budgetShown = words[1] == "on";

        // ⚠ It closes NOTHING, and that is the difference between this panel and every other
        // auxiliary one. Government and City Evidence share the left anchor and evict each other;
        // this one sits opposite them so that the rates and what they brought in can be read
        // together.
        if (_budgetShown)
        {
            _hud.MoveChild(_budgetPanel, -1);
        }

        return true;
    }

    /// <summary>
    /// Writes what the account now says into the labels the panel is already holding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>It writes text into standing labels and does not rebuild the body.</b> Tearing thirty
    /// <c>Label</c>s down and making thirty more is affordable once a Day and not once a Tick, and
    /// ***a panel that is too expensive to refresh is a panel that is refreshed too rarely to be
    /// watched.*** The body is built again only when its SHAPE changes, which is a city gaining or
    /// losing a treasury and the type changing size.
    /// </para>
    /// <para>
    /// ⚠ <b>The guard folds the two flow totals and not the balance alone.</b> A Policy that pays
    /// into the treasury and out of it on the same Tick moves four columns and leaves the balance
    /// where it was, so a signature made of levels would hold a stale table with nothing to say so.
    /// Income and expenditure are each a sum of monotone parts, so one of them moves whenever any of
    /// the eight does — which makes the pair exact rather than merely wider.
    /// </para>
    /// </remarks>
    private void RefreshBudget()
    {
        _budgetPanel.Visible = _budgetShown;
        _budgetButton.SetPressedNoSignal(_budgetShown);

        if (!_budgetShown || _budget is not { } budget)
        {
            return;
        }

        long? treasury = _world.TreasuryBalance()?.Raw;
        TreasuryFlows running = budget.Running;
        string signature =
            $"{budget.OpenedAt}|{budget.ClosedAt}|{budget.Days}|{treasury}|{running.Income}"
            + $"|{running.Expenditure}|{_textPercent}";

        if (signature == _budgetSignature)
        {
            return;
        }

        _budgetSignature = signature;

        string shape = $"{treasury is not null}|{_textPercent}";

        if (shape != _budgetShape)
        {
            _budgetShape = shape;
            BuildBudgetBody(treasury is not null);
        }

        if (treasury is { } held)
        {
            WriteBudget(budget, held);
        }
    }

    /// <summary>
    /// Makes the panel's rows once, keeping every label the refresh will write into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Eight flow rows and no ninth that adds any of them up.</b> A net cannot say whether a city
    /// taxed nothing and paid nothing or taxed heavily and paid it all back, and within the income a
    /// withholding, a profit tax, a <c>[[policy]]</c> and a <c>[[rule]]</c> are one arrival through
    /// four levers the player turns separately. ⚠ <b>The treasury is not a ninth row</b>: it is the
    /// running consequence of the eight and it stands above them as a level.
    /// </para>
    /// <para>
    /// <b>Three figure columns, and the first is the one that MOVES.</b> A closed Day stands still
    /// for 2,048 Ticks by construction, so a table of closed Days and running totals has nothing on
    /// it that a player watching the city can see change until a boundary falls. ⚠ <b>The levels
    /// above the flows share the grid and leave their first two cells empty</b>, so a balance lands
    /// under the running column it belongs to rather than between two it does not.
    /// </para>
    /// </remarks>
    /// <param name="banked">Whether this city has a treasury at all.</param>
    private void BuildBudgetBody(bool banked)
    {
        foreach (Node child in _budgetBody.GetChildren())
        {
            _budgetBody.RemoveChild(child);
            child.QueueFree();
        }

        if (!banked)
        {
            // A city with no conserved money Resource has no treasury, and three columns of zeroes
            // would read as a broken payday rather than as a file that banks nothing --
            // IncomeDump.Refuse's polarity, in the one place where refusing outright is not an
            // option because the player opened the panel.
            _budgetBody.AddChild(Wrapped(
                "This city states no money Resource, so it has no treasury and nothing to budget. "
                + "Load a Ruleset that declares one — rulesets/taxing.toml is the demonstration."));
            _budgetFigures = [];

            return;
        }

        var levels = new GridContainer { Columns = 4 };

        levels.AddThemeConstantOverride("h_separation", Typed(BudgetColumnGap));
        levels.AddChild(Named("Treasury now"));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(_budgetTreasury = Figure(string.Empty));
        levels.AddChild(_budgetOpeningRow = Named(string.Empty));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(_budgetOpening = Figure(string.Empty));
        _budgetBody.AddChild(levels);
        _budgetBody.AddChild(new HSeparator());

        var grid = new GridContainer { Columns = 4 };

        // ⚠ A gap between the columns, because the HEADINGS are the widest cells in them: without
        // one "Day to 12,288" and "since 12,288" touch, and a reader takes the pair for one phrase
        // about one column.
        grid.AddThemeConstantOverride("h_separation", Typed(BudgetColumnGap));
        grid.AddChild(Named(string.Empty));
        grid.AddChild(Figure("today so far"));
        grid.AddChild(_budgetDayHeading = Figure(string.Empty));
        grid.AddChild(_budgetRunHeading = Figure(string.Empty));

        _budgetFigures = new Label[BudgetRows.Length * 3];

        for (int row = 0; row < BudgetRows.Length; row++)
        {
            grid.AddChild(Named(BudgetRows[row]));

            for (int column = 0; column < 3; column++)
            {
                grid.AddChild(_budgetFigures[(row * 3) + column] = Figure(string.Empty));
            }
        }

        _budgetBody.AddChild(grid);
        _budgetBody.AddChild(new HSeparator());
        _budgetBody.AddChild(_budgetExplained = Wrapped(string.Empty));
        _budgetBody.AddChild(_budgetResidual = Wrapped(string.Empty));
    }

    /// <summary>The eight flows, in the order the panel states them.</summary>
    /// <remarks>
    /// ⚠ <b><c>placement · out</c> is the one row that pays nobody.</b> A placement's price leaves
    /// the treasury and leaves the money supply with it, because construction money buys imported
    /// Materials and no import path exists. It is expenditure all the same — the balance fell by it
    /// — and without the row the residual below would carry it with nothing to name it.
    /// </remarks>
    private static readonly string[] BudgetRows =
    [
        "withheld · in", "profit tax · in", "policy · in", "rule · in",
        "policy · out", "rule · out", "subsidy · out", "placement · out",
    ];

    /// <summary>
    /// Writes the levels, the eight flows and the sentence that makes the balance checkable.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The residual is the point of the whole panel.</b> <c>plans/0072</c> F11 found 89% of
    /// this city's income arriving through a path no column watched, and the treasury rising against
    /// a reported income of nothing; ***a budget whose balance cannot be explained by the flows
    /// beside it is a table rather than a budget***. F12 is the same defect still open — a dissolving
    /// Household's estate reaches the treasury uncounted — so on a Ruleset declaring
    /// <c>[[life_stage]]</c> and a money Resource this line is how a player finds out, rather than by
    /// arithmetic somebody happens to do.
    /// </remarks>
    private void WriteBudget(CityBudget budget, long held)
    {
        TreasuryFlows today = budget.Today;
        TreasuryFlows day = budget.Yesterday;
        TreasuryFlows running = budget.Running;

        _budgetTreasury.Text = Text($"{held:N0}");
        _budgetOpeningRow.Text = Text($"Opening balance, Tick {budget.OpenedAt:N0}");
        _budgetOpening.Text = Text($"{budget.Opening:N0}");
        _budgetDayHeading.Text =
            budget.Days == 0 ? "no Day yet" : Text($"Day to {budget.ClosedAt:N0}");
        _budgetRunHeading.Text = Text($"since {budget.OpenedAt:N0}");

        Line(0, today.Withheld, day.Withheld, running.Withheld);
        Line(1, today.ProfitTax, day.ProfitTax, running.ProfitTax);
        Line(2, today.PolicyIn, day.PolicyIn, running.PolicyIn);
        Line(3, today.RuleIn, day.RuleIn, running.RuleIn);
        Line(4, today.PolicyOut, day.PolicyOut, running.PolicyOut);
        Line(5, today.RuleOut, day.RuleOut, running.RuleOut);
        Line(6, today.Subsidy, day.Subsidy, running.Subsidy);
        Line(7, today.Placement, day.Placement, running.Placement);

        long residual = budget.Residual(held);
        long explained = budget.Opening + running.Income - running.Expenditure;

        string opening = Text($"{budget.Opening:N0}");
        string income = Text($"{running.Income:N0}");
        string expenditure = Text($"{running.Expenditure:N0}");

        _budgetExplained.Text =
            $"{opening} at the start, {income} in and {expenditure} out, "
            + $"leaves {Text($"{explained:N0}")}.";

        _budgetResidual.Text = residual == 0
            ? "The treasury holds exactly that. Every unit of the balance is explained by the "
              + "flows above it."
            : $"The treasury holds {Text($"{held:N0}")}, which is "
              + $"{Text($"{Math.Abs(residual):N0}")} {(residual > 0 ? "MORE" : "LESS")} than the "
              + "flows explain. Money is crossing the treasury's edge by a path no row above "
              + "watches — a dissolving Household's estate is the one known to do so.";
    }

    private void Line(int row, long today, long day, long running)
    {
        _budgetFigures[row * 3].Text = Text($"{today:N0}");
        _budgetFigures[(row * 3) + 1].Text = Text($"{day:N0}");
        _budgetFigures[(row * 3) + 2].Text = Text($"{running:N0}");
    }

    private Label Figure(string text)
    {
        Label label = Fixed(text);

        label.HorizontalAlignment = HorizontalAlignment.Right;
        label.CustomMinimumSize = new Vector2(Typed(BudgetFigureWidth), 0f);

        return label;
    }

    private Label Named(string text)
    {
        Label label = Fixed(text);

        label.CustomMinimumSize = new Vector2(Typed(BudgetLabelWidth), 0f);

        return label;
    }

    private Label Wrapped(string text)
    {
        Label label = InformationLabel(text, SecondaryPoints);

        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        return label;
    }

    private static string Text(FormattableString value) =>
        value.ToString(CultureInfo.InvariantCulture);
}
