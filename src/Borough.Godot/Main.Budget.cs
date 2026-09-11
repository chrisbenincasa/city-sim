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
    /// The treasury's account since the shell took hold of this world, or null before it has one.
    /// </summary>
    /// <remarks>
    /// <b>Opened by <c>InstallCity</c> and never by a reload of the panel</b>, because it is a
    /// property of the run rather than of the view. Closing the panel and opening it again must not
    /// restart the running totals, and a world replaced by a tune or a load must.
    /// </remarks>
    private CityBudget? _budget;

    /// <summary>
    /// How wide the two figure columns are. Fixed so the rows line up under their headings whatever
    /// the numbers do.
    /// </summary>
    private const int BudgetFigureWidth = 120;

    private const int BudgetLabelWidth = 168;

    private const int BudgetColumnGap = 12;

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

    private void RefreshBudget()
    {
        _budgetPanel.Visible = _budgetShown;
        _budgetButton.SetPressedNoSignal(_budgetShown);

        if (!_budgetShown || _budget is not { } budget)
        {
            return;
        }

        long? treasury = _world.TreasuryBalance()?.Raw;
        string signature = $"{budget.ClosedAt}|{budget.Days}|{treasury}|{_textPercent}";

        if (signature == _budgetSignature)
        {
            return;
        }

        _budgetSignature = signature;

        foreach (Node child in _budgetBody.GetChildren())
        {
            _budgetBody.RemoveChild(child);
            child.QueueFree();
        }

        if (treasury is not { } held)
        {
            // A city with no conserved money Resource has no treasury, and three columns of zeroes
            // would read as a broken payday rather than as a file that banks nothing --
            // IncomeDump.Refuse's polarity, in the one place where refusing outright is not an
            // option because the player opened the panel.
            _budgetBody.AddChild(Wrapped(
                "This city states no money Resource, so it has no treasury and nothing to budget. "
                + "Load a Ruleset that declares one — rulesets/taxing.toml is the demonstration."));

            return;
        }

        Levels(budget, held);
        Flows(budget);
        Identity(budget, held);
    }

    private void Levels(CityBudget budget, long held)
    {
        // Three columns and an empty middle one, so a level lands under the running column rather
        // than between the two. A figure that does not line up with the column it belongs to reads
        // as belonging to the other one.
        var levels = new GridContainer { Columns = 3 };

        levels.AddThemeConstantOverride("h_separation", Typed(BudgetColumnGap));
        levels.AddChild(Named("Treasury now"));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(Figure(Text($"{held:N0}")));
        levels.AddChild(Named(Text($"Opening balance, Tick {budget.OpenedAt:N0}")));
        levels.AddChild(Figure(string.Empty));
        levels.AddChild(Figure(Text($"{budget.Opening:N0}")));
        _budgetBody.AddChild(levels);
        _budgetBody.AddChild(new HSeparator());
    }

    /// <summary>
    /// The seven flows, each over the Day just closed and over the whole account.
    /// </summary>
    /// <remarks>
    /// <b>Seven rows and no eighth that adds any of them up.</b> A net cannot say whether a city
    /// taxed nothing and paid nothing or taxed heavily and paid it all back, and within the income
    /// a withholding, a profit tax, a <c>[[policy]]</c> and a <c>[[rule]]</c> are one arrival
    /// through four levers the player turns separately. ⚠ <b>The treasury is not an eighth row</b>:
    /// it is the running consequence of the seven and it is printed above them as a level.
    /// </remarks>
    private void Flows(CityBudget budget)
    {
        TreasuryFlows day = budget.Yesterday;
        TreasuryFlows running = budget.Running;

        var grid = new GridContainer { Columns = 3 };

        // ⚠ A gap between the columns, because the two HEADINGS are the widest cells in them: at
        // the design minimum "Day to 12,288" and "since Tick 12,288" touch, and a reader takes the
        // pair for one phrase about one column.
        grid.AddThemeConstantOverride("h_separation", Typed(BudgetColumnGap));
        grid.AddChild(Named(string.Empty));
        // ⚠ Short enough to stay inside its column. A heading that overflows runs into its
        // neighbour with no separator and the two read as one phrase.
        grid.AddChild(Figure(budget.Days == 0
            ? "no Day yet"
            : Text($"Day to {budget.ClosedAt:N0}")));
        grid.AddChild(Figure(Text($"since Tick {budget.OpenedAt:N0}")));

        Line(grid, "withheld · in", day.Withheld, running.Withheld);
        Line(grid, "profit tax · in", day.ProfitTax, running.ProfitTax);
        Line(grid, "policy · in", day.PolicyIn, running.PolicyIn);
        Line(grid, "rule · in", day.RuleIn, running.RuleIn);
        Line(grid, "policy · out", day.PolicyOut, running.PolicyOut);
        Line(grid, "rule · out", day.RuleOut, running.RuleOut);
        Line(grid, "subsidy · out", day.Subsidy, running.Subsidy);

        _budgetBody.AddChild(grid);
        _budgetBody.AddChild(new HSeparator());
    }

    /// <summary>
    /// The sentence that makes the balance checkable, and the residual where it is not.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>The residual is the point of the whole panel.</b> <c>plans/0072</c> F11 found 89% of
    /// this city's income arriving through a path no column watched, and the treasury rising against
    /// a reported income of nothing; ***a budget whose balance cannot be explained by the flows
    /// beside it is a table rather than a budget***. F12 is the same defect still open — a
    /// dissolving Household's estate reaches the treasury uncounted — so on a Ruleset declaring
    /// <c>[[life_stage]]</c> and a money Resource this line is how a player finds out, rather than
    /// by arithmetic somebody happens to do.
    /// </remarks>
    private void Identity(CityBudget budget, long held)
    {
        TreasuryFlows running = budget.Running;
        long residual = budget.Residual(held);
        long explained = budget.Opening + running.Income - running.Expenditure;

        string opening = Text($"{budget.Opening:N0}");
        string income = Text($"{running.Income:N0}");
        string expenditure = Text($"{running.Expenditure:N0}");

        _budgetBody.AddChild(Wrapped(
            $"{opening} at the start, {income} in and {expenditure} out, "
            + $"leaves {Text($"{explained:N0}")}."));

        _budgetBody.AddChild(Wrapped(residual == 0
            ? "The treasury holds exactly that. Every unit of the balance is explained by the "
              + "flows above it."
            : $"The treasury holds {Text($"{held:N0}")}, which is "
              + $"{Text($"{Math.Abs(residual):N0}")} {(residual > 0 ? "MORE" : "LESS")} than the "
              + "flows explain. Money is crossing the treasury's edge by a path no row above "
              + "watches — a dissolving Household's estate is the one known to do so."));
    }

    private void Line(GridContainer grid, string named, long day, long running)
    {
        grid.AddChild(Named(named));
        grid.AddChild(Figure(Text($"{day:N0}")));
        grid.AddChild(Figure(Text($"{running:N0}")));
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
