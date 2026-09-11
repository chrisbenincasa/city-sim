using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Borough.Core;
using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Core.Space;
using Borough.Core.Tables;
using Borough.Formats;
using Godot;

namespace Borough.Shell;

// ---- the panels -- the tool palette, the Policy panel, and the tuner that reloads the Ruleset 
//
// All three are a READING OF THE RULESET and are rebuilt when it is tuned, which is why Panels()
// exists: it is the one place that knows what has to be built again after a reload.
//
// Stated/Names/Turned edit the TOML as text rather than round-tripping it through the loader, so
// a tune changes the one key it was asked to change and leaves every comment in the file alone.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    /// <summary>
    /// Builds the two panels that are <b>a reading of the Ruleset</b>, and rebuilds them on a tune.
    /// </summary>
    /// <remarks>
    /// 🔴 <b>THE GOVERNING PANEL WAS BUILT ONCE AND A TUNE MAKES A NEW CITY.</b> Its rows are one per
    /// declared <c>[[policy]]</c>, read at <c>_Ready</c>; <see cref="Regenerate"/> replaces the World
    /// and the Ruleset and left the rows where they were, so a tune that changed the policy set left
    /// a panel addressing positions the new city does not have. ⚠ <b>Since row 15e that is a sentence
    /// rather than a half-stepped Tick</b> — <c>Simulation.Refuses</c> catches it — ***but a panel
    /// showing a Policy the city has not got is still a panel lying about the city.*** The palette
    /// would have acquired the same defect the moment it listed a Zone Rule, so both are built here.
    /// </remarks>
    private void Panels()
    {
        _zoneStart = null;
        _zoneFeedback = string.Empty;
        _policyPanel?.QueueFree();
        _cityPanel?.QueueFree();
        _palette?.QueueFree();
        _placementRail?.QueueFree();

        Governing(_hud);
        CityEvidencePanel(_hud);
        _cityRead = false;
        _cityGroup = -1;
        _cityCause = null;
        _cityFrom = 0;
        Retrouble();
        Palette();

        // ⚠ The rebuild re-enters the theme. A Ruleset reload rebuilds both panels, and before this
        // line they came back in the DEFAULT Godot colours until the next theme toggle -- a bug that
        // only showed after a reload, which is why it survived so long.
        ThemeInformation();
    }

    /// <summary>A backing panel in the shell's one panel style.</summary>
    private static PanelContainer Backed(Vector2 at)
    {
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        var panel = new PanelContainer { Position = at };

        panel.AddThemeStyleboxOverride("panel", backing);

        return panel;
    }

    // ---- the tuner ----------------------------------------------------------------------------

    /// <summary>
    /// One number the tuner can turn, named by the table it lives in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b><see cref="Table"/> is what makes this safe, and it is not decoration.</b>
    /// <c>candidates</c> is a key in <c>[placement]</c> <em>and</em> in <c>[jobs]</c>, and
    /// <c>interval</c> is in both plus <c>[[zone_rule]]</c> — so a rewriter matching on the key
    /// alone would turn two dials when the viewer asked for one, and the second would be invisible.
    /// </para>
    /// <para>
    /// <b>An empty <see cref="Table"/> means the field is the shell's own</b> — population and seed
    /// are arguments to <c>World</c> rather than anything a Ruleset states, and they are on the
    /// panel because they are the two biggest levers on what you are looking at.
    /// </para>
    /// </remarks>
    private readonly record struct Dial(string Table, string Key, string Label);

    /// <summary>
    /// What the tuner exposes. <b>Eight, chosen because each one changes what you SEE.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS A TUNER AND NOT AN EDITOR, AND THE BOUNDARY IS THAT IT ONLY TURNS KEYS THE
    /// FILE ALREADY STATES.</b> A key the Ruleset does not mention is shown greyed and is never
    /// written, because inserting one means knowing which table to put it in, whether that table
    /// exists, and what else becomes required when it does — <c>[districts]</c> alone makes four
    /// keys and every Good's price mandatory. ***An editor is a different program and it is the one
    /// this would grow into.***
    /// </para>
    /// <para>
    /// ⚠ <b><c>occupants</c> is written to EVERY <c>[[building]]</c> in the file</b>, which is the
    /// one field here that is not one-to-one. It is coherent — <em>every kind holds N</em> — and
    /// every shipped file already declares the same number for every kind, but a file that varied
    /// them would be flattened by a single turn of this dial.
    /// </para>
    /// </remarks>
    private static readonly Dial[] Dials =
    [
        new("", "citizens", "citizens"),
        new("", "seed", "world seed"),
        new("[roads]", "block_tiles", "block_tiles"),
        new("[lots]", "lots_per_segment", "lots_per_segment"),
        new("[roads]", "arterial_count", "arterial_count"),
        new("[placement]", "candidates", "placement candidates"),
        new("[[building]]", "occupants", "occupants"),
        new("[households]", "car_ownership_percent", "car_ownership_percent"),
    ];

    /// <summary>
    /// Reads the value a key currently carries, or <c>null</c> when the file does not state it.
    /// </summary>
    private static string? Stated(string toml, string table, string key)
    {
        string? here = null;

        foreach (string line in toml.Split('\n'))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out string value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Whether a line assigns <paramref name="key"/>, and what it assigns.</summary>
    private static bool Names(string line, string key, out string value)
    {
        value = string.Empty;

        int equals = line.IndexOf('=');

        if (equals < 0 || line.StartsWith('#') || line[..equals].Trim() != key)
        {
            return false;
        }

        value = line[(equals + 1)..].Trim();

        return true;
    }

    /// <summary>
    /// The Ruleset text with one key rewritten <b>inside its own table and nowhere else</b>.
    /// </summary>
    private static string Turned(string toml, string table, string key, string to)
    {
        string[] lines = toml.Split('\n');
        string? here = null;

        for (int at = 0; at < lines.Length; at++)
        {
            string trimmed = lines[at].Trim();

            if (trimmed.StartsWith('['))
            {
                here = trimmed;

                continue;
            }

            if (here == table && Names(trimmed, key, out _))
            {
                // The original indentation is kept because these files are column-aligned by hand
                // and a rewriter that reflowed them would make every diff unreadable.
                int equals = lines[at].IndexOf('=');

                lines[at] = string.Concat(lines[at].AsSpan(0, equals + 1), " ", to);
            }
        }

        return string.Join('\n', lines);
    }

    /// <summary>Builds the panel. One row per <see cref="Dials"/> entry, hidden until asked for.</summary>
    private void Tuner(CanvasLayer layer)
    {
        var box = new VBoxContainer();

        // A panel the text can be read against. The readout above is white on whatever the city
        // happens to be, which is legible for three lines and not for twelve rows of numbers.
        var backing = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.06f, 0.08f, 0.92f),
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 10f,
            ContentMarginBottom = 10f,
        };

        _tuner = new PanelContainer
        {
            Visible = false,
            Position = new Vector2(14f, 108f),
            Theme = _type,
        };

        _tuner.AddThemeStyleboxOverride("panel", backing);

        _fields = new LineEdit[Dials.Length];

        for (int at = 0; at < Dials.Length; at++)
        {
            var row = new HBoxContainer();
            var name = new Label
            {
                Text = Dials[at].Label,
                CustomMinimumSize = new Vector2(210f, 0f),
            };

            string? stated = Dials[at].Table.Length == 0
                ? Own(Dials[at].Key)
                : Stated(_toml, Dials[at].Table, Dials[at].Key);

            var field = new LineEdit
            {
                Text = stated ?? "—",
                Editable = stated is not null,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            _fields[at] = field;

            row.AddChild(name);
            row.AddChild(field);
            box.AddChild(row);
        }

        var apply = new Button { Text = "regenerate  (enter)" };

        apply.Pressed += () => AtBoundary(Regenerate);
        box.AddChild(apply);

        _tunerStatus = new Label { Text = "tab closes. a regenerate is a NEW city, not a reload." };
        box.AddChild(_tunerStatus);

        ScrollAuxiliary(_tuner, box);
        layer.AddChild(_tuner);
    }

    /// <summary>
    /// The governing panel: every declared <c>[[policy]]</c>, its amount, and a way to set it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>NOT the tuner, and the separation is the decision.</b> The tuner rewrites Ruleset text
    /// and regenerates a <em>new city</em>; this issues a <c>Govern</c> <see cref="Command"/> against
    /// the city that is running, at a Tick, through the door a replay reproduces. ***One edits the
    /// world's premises and the other plays the game.***
    /// </para>
    /// <para>
    /// ⚠ <b>An unnamed <c>[[policy]]</c> is shown and disabled rather than omitted.</b> Its
    /// <c>Ruleset.PolicyKeys</c> entry is zero and <c>ApplyGovern</c> refuses it — a governed amount
    /// is saved state and a name is the only thing that survives a renumbering. ***Omitting the row
    /// would shift every position below it***, and <c>Govern</c> addresses a Policy by exactly that
    /// position.
    /// </para>
    /// <para>
    /// ⚠ <b>The amount is <c>Command.East</c></b>, which is a field whose name says <em>where</em>.
    /// <c>Command.Govern</c> is the factory that says so; the struct is twelve fully-defined bytes
    /// and widening it would re-spell every committed Input Log.
    /// </para>
    /// <para>
    /// 🔴 <b>FOUR TOOLS RENDERED IDENTICALLY AND THREE OF THE FOUR READINGS WERE WRONG.</b>
    /// <c>plans/0072</c> D10 turned one transfer into four things a Policy can be, and the row said
    /// <em>sweeps X every N</em> for all of them. ***A relief's number is a percentage and read as
    /// money***, so a player typing 25 into one believed they had set a charge of 25; a subsidy's
    /// second number had no field at all, so <c>Command.Fund</c> was unreachable from the panel; and
    /// a Policy aimed at one trade looked exactly like one that levied every Business in the city.
    /// <b>So the row now names its tool, names its trade, and grows a second field where there is a
    /// second decision.</b>
    /// </para>
    /// <para>
    /// ⚠ <b>The trade is resolved through <see cref="RulesetNames"/> and never through the core.</b>
    /// <c>PolicyDefinition.Trade</c> is a kind id; <c>_names.BusinessKind</c> is the same path
    /// <see cref="Sentence"/> and the Business readout already take. <b>The shell owns every string
    /// a human reads</b>, so a Policy naming a trade nothing can name falls back to the id.
    /// </para>
    /// </remarks>
    private void Governing(CanvasLayer layer)
    {
        _policyPanel = Backed(new Vector2(14f, 108f));
        _policyPanel.Theme = _type;
        _policyPanel.Visible = _governing;

        var box = new VBoxContainer();

        var heading = new HBoxContainer();
        heading.AddChild(new Label { Text = "Government · Policies and Income Tax", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var close = InformationButton("Close Government", Govern);
        close.TooltipText = "Close Policies";
        heading.AddChild(close);
        box.AddChild(heading);

        // 🔴 THE FIELD IS THE TRANSFER AMOUNT AND NOT THE TAX RATE, and a panel that did not say so
        // would be actively misleading: on levied.toml every row reads `1` while the levy that
        // actually bites is `percent = 10` in the apply rule. Govern writes PolicyTable.Amount and
        // nothing else -- ApplyCount is Ruleset data and is not governable -- so a person turning
        // this dial expecting a rate would change the wrong number and watch nothing happen.
        // ⚠ ADDED HERE AND WORDED AFTER THE LOOP, because what the field means depends on which
        // tools this city declares and the sentence has to sit ABOVE the rows it qualifies. A world
        // of plain transfers gets the sentence it always had; naming a relief's unit to somebody who
        // has no relief is a line of panel spent on a control that is not there.
        var meaning = new Label();

        box.AddChild(meaning);

        PolicyDefinition[] declared = _world.Rules.Policies;

        _policyFields = new LineEdit[declared.Length];
        _policyCeilings = new LineEdit?[declared.Length];

        var units = new List<Label>();
        bool anyRelief = false;
        bool anySubsidy = false;

        for (int at = 0; at < declared.Length; at++)
        {
            var row = new HBoxContainer();
            bool governable = _world.Rules.PolicyKey(at) != 0;
            string named = _names.Policy(at) ?? "unnamed";

            anyRelief |= declared[at].Tool == PolicyTool.Relief;
            anySubsidy |= declared[at].Tool == PolicyTool.Subsidy;

            row.AddChild(new Label
            {
                Text = governable
                    ? $"{named} · {Described(declared[at])}"
                    : $"{named} · {Described(declared[at])} (no name — ungovernable)",
                CustomMinimumSize = new Vector2(340f, 0f),
            });

            var field = new LineEdit
            {
                Text = _world.Policies.AmountOf(at, declared[at]).ToString(),
                Editable = governable,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            int position = at;

            field.TextSubmitted += _ => AtBoundary(() => Govern(position));
            _policyFields[at] = field;
            row.AddChild(field);

            var unit = new Label { Text = Unit(declared[at].Tool) };

            units.Add(unit);
            row.AddChild(unit);

            var set = InformationButton("Set", () => Govern(position));
            set.Disabled = !governable;
            row.AddChild(set);
            box.AddChild(row);

            if (declared[at].Tool != PolicyTool.Subsidy)
            {
                continue;
            }

            // ⚠ A SECOND ROW AND NOT A SECOND COLUMN. Two numbers and two buttons on one line put
            // the rate's Set beside the ceiling's field, and the two verbs are separately refusable
            // -- a mis-hit would report a refusal about a number the player had not touched.
            var funding = new HBoxContainer();

            funding.AddChild(new Label
            {
                Text = "    ↳ funding ceiling — the most it may pay out in one Day",
                CustomMinimumSize = new Vector2(340f, 0f),
            });

            var ceiling = new LineEdit
            {
                Text = _world.Policies.CeilingOf(at, declared[at])
                    .ToString(CultureInfo.InvariantCulture),
                Editable = governable,
                CustomMinimumSize = new Vector2(110f, 0f),
            };

            ceiling.TextSubmitted += _ => AtBoundary(() => Fund(position));
            _policyCeilings[at] = ceiling;
            funding.AddChild(ceiling);

            var perDay = new Label { Text = "a Day" };

            units.Add(perDay);
            funding.AddChild(perDay);

            var fund = InformationButton("Fund", () => Fund(position));

            fund.Disabled = !governable;
            funding.AddChild(fund);
            box.AddChild(funding);
        }

        meaning.Text = "what ONE application moves"
            + (anyRelief && anySubsidy
                ? ", EXCEPT where the row says otherwise: a relief's field is a PERCENTAGE of a tax"
                    + " bill and a subsidy's is what ONE WORKER is worth"
                : anyRelief ? ", EXCEPT a relief's field, which is a PERCENTAGE of a tax bill"
                : anySubsidy ? ", EXCEPT a subsidy's field, which is what ONE WORKER is worth"
                : string.Empty)
            + ". how many and what share is the [[policy]]'s apply rule, which is not governable.";

        if (declared.Length == 0)
        {
            box.AddChild(new Label { Text = "this Ruleset declares no [[policy]]." });
        }

        // 🔴 TWO THINGS A PLAYER CANNOT SEE FROM THESE CONTROLS, AND EACH IS SAID ONLY WHERE THE
        // CONTROL EXISTS. A relief's field looks like money and its effect is invisible unless a
        // Business owed tax anyway; a ceiling looks like a reservation and is not one. ⚠ Both sit
        // BELOW the rows, which is Taxing's own finding: a banner above the controls it qualifies
        // pushes them under the fold, and a caveat that hides its control is worse than none.
        if (anyRelief)
        {
            box.AddChild(new Label
            {
                Text = "a RELIEF MOVES NO MONEY. it takes its percentage off a profit tax bill that"
                    + " has already been worked out, so a Business owing no profit tax gets nothing"
                    + " from it however high you set it — raising a relief can never pay anybody."
                    + " overlapping reliefs on one trade add together, and the sum stops at the"
                    + " whole bill.",
            });
        }

        if (anySubsidy)
        {
            box.AddChild(new Label
            {
                Text = "a CEILING IS NOT A RESERVATION. it bounds what one subsidy may pay on a Day;"
                    + " what the treasury actually holds is asked at the moment of payment, so a"
                    + " fully funded subsidy in a broke city pays nothing and nothing was set aside"
                    + " for it. when the claims come to more than the pot every claimant is cut in"
                    + " proportion, and the shortfall creates no debt.",
            });
        }

        _policyStatus = new Label { Text = "a governed amount is saved state and survives a reload." };

        box.AddChild(_policyStatus);
        Taxing(box);
        ScrollAuxiliary(_policyPanel, box);

        // ⚠ AFTER ScrollAuxiliary AND NOT BEFORE, because that is what undoes this. It walks every
        // Label in the body and makes it a wrapping, expanding paragraph -- right for a note and
        // wrong for a two-character unit, which would then take half the row's spare width and sit
        // a field's length away from the number it names. ***A unit that is not touching its field
        // is not a unit.***
        foreach (Label unit in units)
        {
            unit.AutowrapMode = TextServer.AutowrapMode.Off;
            unit.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
            unit.CustomMinimumSize = new Vector2(72f, 0f);
        }

        layer.AddChild(_policyPanel);
    }

    /// <summary>
    /// One Policy in the player's words: <b>which of the four tools it is, and who it is aimed at.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The four do not share a sentence, which is why this is a switch and not a format
    /// string.</b> A relief has an <c>interval</c> in its table and <c>TaxRelief.PercentFor</c>
    /// never looks at it — the relief is taken inside the profit assessment, on whatever Day that
    /// Business is assessed — so a relief row saying <em>every 2,048</em> would be quoting a number
    /// that decides nothing. ***A row that names a cadence the mechanism does not have is the same
    /// defect as a field that names the wrong unit.***
    /// </para>
    /// <para>
    /// <b>The tool's word is the player's and never the enum's.</b> <c>PolicyTool.Transfer</c> is a
    /// name for <em>what this was before the catalogue</em>, which is a fact about the build.
    /// </para>
    /// </remarks>
    private string Described(in PolicyDefinition policy) => policy.Tool switch
    {
        PolicyTool.Charge =>
            $"charge — charges {Aimed(policy)} every {policy.Interval:N0}, into the treasury",

        PolicyTool.Relief =>
            $"relief — takes a share off the profit tax of {Aimed(policy)}",

        PolicyTool.Subsidy =>
            $"subsidy — pays {Aimed(policy)} every {policy.Interval:N0}, out of the treasury",

        _ => $"transfer — sweeps {Aimed(policy)} every {policy.Interval:N0}",
    };

    /// <summary>Who a Policy reaches, <b>naming the trade when it names one.</b></summary>
    /// <remarks>
    /// ⚠ <b>Absent means everybody and the row has to say so out loud</b> (<c>plans/0072</c> D28,
    /// <c>TradeKind.Any</c>). A charge aimed at one trade and a charge aimed at every Business in
    /// the city are the same six controls and the same number; ***the only place that difference
    /// can be seen is this sentence.***
    /// </remarks>
    private string Aimed(in PolicyDefinition policy) => policy.Subject switch
    {
        PolicySubject.Household => "all households",
        PolicySubject.Building => "all buildings",
        PolicySubject.Business when policy.Trade == TradeKind.Any => "all businesses",
        PolicySubject.Business =>
            $"{_names.BusinessKind(policy.Trade) ?? $"trade {policy.Trade}"} businesses ONLY",
        _ => "nobody",
    };

    /// <summary>What the number beside a Policy is counted in. <b>Empty where it is plain Money.</b></summary>
    private static string Unit(PolicyTool tool) => tool switch
    {
        PolicyTool.Relief => "% of the bill",
        PolicyTool.Subsidy => "per worker",
        _ => string.Empty,
    };

    /// <summary>
    /// The income-tax block: the four marginal-band controls, and what they will not accept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It belongs to the governing panel and is not a window of its own.</b> A rate is governed
    /// exactly the way a Policy amount is — a <see cref="Command"/> against the standing city, at a
    /// Tick, through the door a replay reproduces — so putting it anywhere else would say the two
    /// are different kinds of act.
    /// </para>
    /// <para>
    /// 🔴 <b>Every field shows TOMORROW's schedule and never today's.</b> A change applies from the
    /// start of the next Day (<c>plans/0072</c> D6), and <c>Simulation.RefuseTax</c> composes its
    /// refusal on that same next-Day schedule — so a panel reading today's would show a player one
    /// set of numbers and have their next command judged against another. ***The panel shows what
    /// is being built rather than what is being earned against.***
    /// </para>
    /// <para>
    /// ⚠ <b>The block states the two ordering constraints rather than enforcing them.</b> The core
    /// owns both, and a shell that greyed a field out would be a second copy of a rule that is free
    /// to drift — <c>plans/0012</c> <b>Cause 1</b>. What it does instead is say which end to move
    /// first, because from a city that levies nothing all four controls sit at zero and the naive
    /// order is the refused one.
    /// </para>
    /// <para>
    /// ⚠ <b>The note sits BELOW the four rows, where the Policy block's sits above them.</b> It runs
    /// to six lines at the panel's width, and the panel is bounded by the console — driven at 1600 ×
    /// 1000 against <c>taxed.toml</c>, a banner above the rows left three of the four controls under
    /// the fold. ***A caveat that pushes the control it qualifies off the screen is worse than no
    /// caveat.***
    /// </para>
    /// </remarks>
    private void Taxing(VBoxContainer box)
    {
        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "Income tax — what one Day's earnings are taxed at" });

        // ⚠ SIZED BY THE ENUM AND NOT BY A COUNT. Levy indexes this by (int)control, so a control
        // added to TaxControl and forgotten here is an IndexOutOfRange on a click rather than a
        // compile error -- and the profit half arrived exactly that way.
        _taxFields = new LineEdit[(int)TaxControl.ProfitUpperRate + 1];

        Levy(box, TaxControl.Allowance, "tax-free allowance — earned in a Day before any tax");
        Levy(box, TaxControl.UpperThreshold, "upper band opens at — a Day's earnings");
        Levy(box, TaxControl.MiddleRate, "middle rate — % between the allowance and that");
        Levy(box, TaxControl.UpperRate, "upper rate — % above that");

        // 🔴 THE FOUR CONTROLS CONSTRAIN EACH OTHER AND A PLAYER CANNOT SEE THAT FROM THE FIELDS.
        // From a city that levies nothing every one of them is zero, so raising the middle rate
        // first is refused (the upper would sit below it) and raising the allowance first is
        // refused (the upper band would open below it). Saying so is the difference between a
        // control that teaches its order and one that just says no.
        box.AddChild(new Label
        {
            Text = "levied from the START OF THE NEXT DAY, so these fields are tomorrow's and no"
                + " change ever reprices a Day already being earned. each rate is MARGINAL — it"
                + " bites only on the earnings inside its own band, so crossing a threshold never"
                + " reprices what was earned below it. the upper rate can never sit below the middle"
                + " one, and the upper band can never open below the allowance: raise the upper rate"
                + " before the middle one, and the threshold before the allowance.",
        });

        _taxStatus = new Label
        {
            Text = "a rate the player has set is saved state and survives a reload; a Ruleset's"
                + " [income_tax] is what a city that has never been governed levies.",
        };

        box.AddChild(_taxStatus);
        Profiting(box);
    }

    /// <summary>
    /// The Business profit block: the three marginal-band controls, and what they will not accept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>ITS OWN BLOCK, ITS OWN TITLE AND ITS OWN NOTE, because it is its own schedule.</b> A
    /// Citizen pays on what they <em>earned</em> in a Day and a Business on what it <em>made</em>;
    /// the two share the <see cref="TaxControl"/> selector and the effective-Day rule and nothing
    /// else. ***Seven fields under one heading would read as one tax with seven dials***, and a
    /// player would reasonably expect the allowance above to shelter a Business's first pound.
    /// </para>
    /// <para>
    /// ⚠ <b>The missing field is the thing to say out loud.</b> There is no tax-free band on this
    /// schedule and no control for one — <c>plans/0072</c> D8 — so a player who has just read the
    /// earnings block above arrives here looking for an allowance that is deliberately absent. The
    /// note says how to build one out of the two controls that do exist.
    /// </para>
    /// <para>
    /// ⚠ <b>Three rows and a note, on a panel that was already full.</b> The governing panel is a
    /// <c>ScrollContainer</c> bounded by the console, so this block sits below the fold at
    /// 1600 × 1400 and is reached by scrolling. ***That is a reachable control and not a hidden
    /// one***, and shortening the notes to buy it a screen would cost the sentences that stop a
    /// player mis-reading the dial.
    /// </para>
    /// </remarks>
    private void Profiting(VBoxContainer box)
    {
        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "Business profit tax — what one Day's profit is taxed at" });

        Levy(box, TaxControl.ProfitThreshold, "upper band opens at — a Day's profit");
        Levy(box, TaxControl.ProfitLowerRate, "lower rate — % on profit below that");
        Levy(box, TaxControl.ProfitUpperRate, "upper rate — % on profit above that");

        // 🔴 TWO CONTROLS AND THREE THINGS A PLAYER CANNOT SEE FROM THEM. The ordering constraint is
        // the earnings block's again. The absent allowance is D8 and is the field a reader will look
        // for and not find. The loss rule is D26 -- nothing carries forward, so a Business is not
        // spared a good Day by a bad one, and that is the opposite of what most tax systems teach.
        box.AddChild(new Label
        {
            Text = "levied from the START OF THE NEXT DAY, the same as earnings above, and no change"
                + " ever reprices a Day already traded. each rate is MARGINAL — it bites only on the"
                + " profit inside its own band. the upper rate can never sit below the lower one, so"
                + " from a city that taxes nothing, raise the upper rate first."
                + " THERE IS NO TAX-FREE BAND here and no control for one: set the lower rate to 0"
                + " and the threshold becomes the allowance."
                + " a Day that made a LOSS is untaxed and nothing carries forward, so a Business that"
                + " loses money one Day and profits the next pays in full on the profitable Day.",
        });

        _profitStatus = new Label
        {
            Text = "a rate the player has set is saved state and survives a reload; a Ruleset's"
                + " [business_tax] is what a city that has never been governed levies.",
        };

        box.AddChild(_profitStatus);
    }

    /// <summary>One tax control: what it is, what it is set to for tomorrow, and a way to move it.</summary>
    private void Levy(VBoxContainer box, TaxControl control, string described)
    {
        var row = new HBoxContainer();

        row.AddChild(new Label { Text = described, CustomMinimumSize = new Vector2(340f, 0f) });

        var field = new LineEdit
        {
            Text = Levied(control).ToString(CultureInfo.InvariantCulture),
            CustomMinimumSize = new Vector2(110f, 0f),
        };

        field.TextSubmitted += _ => AtBoundary(() => Tax(control));
        _taxFields[(int)control] = field;
        row.AddChild(field);
        row.AddChild(InformationButton("Set", () => Tax(control)));
        box.AddChild(row);
    }

    private void Palette() => BuildToolBrowser();

    /// <summary>Open or close the governing panel. <b>The key and the palette button share it.</b></summary>
    private void Govern()
    {
        if (!_governing && _cityShown)
        {
            _cityShown = false;
            RefreshCityEvidence();
        }

        _governing = !_governing;
        _policyPanel.Visible = _governing;
        _policiesButton.ButtonPressed = _governing;

        if (_governing)
        {
            _hud.MoveChild(_policyPanel, -1);
            ShowPolicies();
        }
    }

    private void ShowTools() => RefreshToolBrowser();

    /// <summary>Re-reads every field off the world, so an open panel shows what is in force.</summary>
    private void ShowPolicies()
    {
        PolicyDefinition[] declared = _world.Rules.Policies;

        for (int at = 0; at < _policyFields.Length && at < declared.Length; at++)
        {
            _policyFields[at].Text = _world.Policies.AmountOf(at, declared[at]).ToString();

            // A null entry is a Policy with no second number rather than a field not yet built, so
            // there is nothing to re-read and nothing to report -- Governing builds one only where
            // Fund can be sent.
            if (_policyCeilings.Length > at && _policyCeilings[at] is { } ceiling)
            {
                ceiling.Text = _world.Policies.CeilingOf(at, declared[at])
                    .ToString(CultureInfo.InvariantCulture);
            }
        }

        for (int at = 0; at < _taxFields.Length; at++)
        {
            _taxFields[at].Text = Levied((TaxControl)at).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>The Day a command issued now would take effect from.</summary>
    /// <remarks>
    /// ⚠ <b><c>today + 1</c>, and the <c>+ 1</c> is the whole point.</b> It is
    /// <c>Simulation.RefuseTax</c>'s own arithmetic, so what the panel shows and what the core
    /// judges the next command against are the same schedule.
    /// </remarks>
    private long Tomorrow() => IntegerMath.FloorDiv((long)_world.Tick.Raw, Ticks.PerDay) + 1;

    /// <summary>Whether a control belongs to the profit schedule rather than the earnings one.</summary>
    private static bool OnProfit(TaxControl control) => control >= TaxControl.ProfitThreshold;

    /// <summary>What one control is set to for tomorrow, player-set or Ruleset-authored.</summary>
    /// <remarks>
    /// ⚠ <b>Two schedules and two fall-throughs.</b> Each half of a Day's row stamps on its own, so
    /// a world that has governed earnings and never touched profit reads the player's earnings back
    /// and the Ruleset's <c>[business_tax]</c> beside it.
    /// </remarks>
    private long Levied(TaxControl control)
    {
        if (OnProfit(control))
        {
            BusinessTaxSchedule profit =
                _world.IncomeTaxRates.ProfitScheduleFor(Tomorrow(), _world.Rules.BusinessTax);

            return control switch
            {
                TaxControl.ProfitThreshold => profit.ThresholdPerDay,
                TaxControl.ProfitLowerRate => profit.LowerRatePercent,
                _ => profit.UpperRatePercent,
            };
        }

        IncomeTaxSchedule schedule =
            _world.IncomeTaxRates.ScheduleFor(Tomorrow(), _world.Rules.IncomeTax);

        return control switch
        {
            TaxControl.Allowance => schedule.AllowancePerDay,
            TaxControl.UpperThreshold => schedule.UpperThresholdPerDay,
            TaxControl.MiddleRate => schedule.MiddleRatePercent,
            _ => schedule.UpperRatePercent,
        };
    }

    /// <summary>Queues a <c>Tax</c> for one control, or says why it cannot.</summary>
    /// <remarks>
    /// ⚠ <b>The refusal is <see cref="Send"/>'s and not a restatement of it.</b> All five of
    /// <c>RefuseTax</c>'s codes reach the player through <c>Sentence</c>, the same table every
    /// other verb's refusals go through — the panel's banner says what the constraints are so a
    /// player is not surprised by one, and never decides whether they hold.
    /// </remarks>
    private void Tax(TaxControl control)
    {
        // ⚠ THE ANSWER GOES TO THE BLOCK THE CONTROL BELONGS TO. One status line under two blocks
        // would report a profit refusal beneath the earnings rows, several lines above the dial
        // that was actually turned, and on a panel this tall that is off the screen.
        Label status = OnProfit(control) ? _profitStatus : _taxStatus;

        if (!int.TryParse(_taxFields[(int)control].Text, out int value))
        {
            status.Text = "that is not a whole number.";

            return;
        }

        if (!Send(Command.Tax(control, value)))
        {
            status.Text = _refused;

            return;
        }

        status.Text = $"{Named(control)} set to {value:N0}, from the start of Day {Tomorrow():N0}.";
    }

    /// <summary>A <see cref="TaxControl"/> in the player's words. <b>The shell owns every one.</b></summary>
    private static string Named(TaxControl control) => control switch
    {
        TaxControl.Allowance => "the tax-free allowance",
        TaxControl.UpperThreshold => "the upper band's opening",
        TaxControl.MiddleRate => "the middle rate",
        TaxControl.UpperRate => "the upper rate",
        TaxControl.ProfitThreshold => "the profit band's opening",
        TaxControl.ProfitLowerRate => "the lower rate on profit",
        _ => "the upper rate on profit",
    };

    /// <summary>Queues a <c>Govern</c> for one Policy, or says why it cannot.</summary>
    private void Govern(int position)
    {
        if (!int.TryParse(_policyFields[position].Text, out int amount))
        {
            _policyStatus.Text = "that is not a whole number.";

            return;
        }

        // ⚠ THE PANEL RESTATED ONE OF Govern'S THREE REFUSALS AND COULD NOT SEE THE OTHER TWO.
        // Simulation.Refuses answers all three off the applier's own predicate, and the panel says
        // whichever one it gave -- so a Policy this world holds no row for is now a sentence rather
        // than a half-stepped Tick.
        if (!Send(Command.Govern(position, amount)))
        {
            _policyStatus.Text = _refused;

            return;
        }

        _policyStatus.Text =
            $"{_names.Policy(position) ?? $"policy {position}"} set to {amount:N0} "
            + $"on Tick {_world.Tick.Raw + 1:N0}.";
    }

    /// <summary>Queues a <c>Fund</c> for one subsidy's daily ceiling, or says why it cannot.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>Two verbs against one Policy, and the rate is not the ceiling.</b> Raising what a claim
    /// is worth while leaving the pot alone pays the same Money to fewer claimants
    /// (<c>plans/0072</c> D12), so a panel that set both from one button would make that unreachable
    /// — which is <c>Simulation.ApplyFund</c>'s own sentence.
    /// </para>
    /// <para>
    /// ⚠ <b>The button exists only on a subsidy, so <c>FundPolicyPaysNobody</c> is unreachable from
    /// this panel — and it still has a sentence.</b> The console channel sends the same verb, and a
    /// refusal with no sentence reads on the readout as a click that worked.
    /// </para>
    /// <para>
    /// ⚠ <b>The panel states no order between the two refusals and must not.</b>
    /// <c>Simulation.RefuseFund</c> names the Policy first, then the sign of the number, then the
    /// tool; <see cref="Send"/> reports whichever it gave. ***A shell that ranked them would be a
    /// second copy of an order that is free to drift.***
    /// </para>
    /// </remarks>
    private void Fund(int position)
    {
        if (_policyCeilings.Length <= position || _policyCeilings[position] is not { } field)
        {
            return;
        }

        if (!int.TryParse(field.Text, out int ceiling))
        {
            _policyStatus.Text = "that is not a whole number.";

            return;
        }

        if (!Send(Command.Fund(position, ceiling)))
        {
            _policyStatus.Text = _refused;

            return;
        }

        _policyStatus.Text =
            $"{_names.Policy(position) ?? $"policy {position}"} may pay out at most "
            + $"{ceiling:N0} a Day, from Tick {_world.Tick.Raw + 1:N0}. what the treasury holds "
            + "on the Day is asked separately.";
    }

    /// <summary>The current value of a field the shell owns rather than the Ruleset.</summary>
    private string Own(string key) => key switch
    {
        "citizens" => _citizens.ToString(),
        "seed" => _seed.ToString(),
        _ => "—",
    };

    /// <summary>
    /// Rewrites the Ruleset text from the panel, re-parses it, and builds a new city from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>IT GOES BACK THROUGH THE LOADER AND NEVER POKES THE <see cref="Ruleset"/>.</b>
    /// <c>adr/0048</c> puts every one of the loader's refusals at the parse site, so a panel that
    /// set fields on a parsed Ruleset would bypass all of them — and a bad number would surface as
    /// a crash somewhere unrelated, several minutes later, rather than as the sentence naming the
    /// key and the line. ***The round trip through text is what keeps the tuner honest.***
    /// </para>
    /// <para>
    /// ⚠ <b>A REGENERATE IS A NEW CITY AND NOT A RELOAD, and the panel says so.</b> Most of what is
    /// on it is <em>world-creation</em> under <c>CLAUDE.md</c>'s Kind column — the Road Graph is
    /// laid once — so there is no sense in which the standing city could absorb a new
    /// <c>block_tiles</c>. Everything starts again at Tick 0: the State Hash restarts, and nothing
    /// that happened is carried over. <c>adr/0015</c>'s hot reload is a different mechanism for the
    /// <em>tuning</em> half, and it is not this.
    /// </para>
    /// <para>
    /// ⚠ <b>The eye is re-framed and not rebuilt</b>, so the yaw and zoom you were looking with
    /// survive the regenerate. Comparing two cities is the whole point of the panel, and a camera
    /// that jumped home between them would make the comparison useless.
    /// </para>
    /// </remarks>
    private void Regenerate()
    {
        string toml = _toml;
        int citizens = _citizens;
        ulong seed = _seed;

        for (int at = 0; at < Dials.Length; at++)
        {
            if (!_fields[at].Editable)
            {
                continue;
            }

            string typed = _fields[at].Text.Trim();

            if (Dials[at].Table.Length == 0)
            {
                if (Dials[at].Key == "citizens" && int.TryParse(typed, out int wanted))
                {
                    citizens = wanted;
                }
                else if (Dials[at].Key == "seed" && ulong.TryParse(typed, out ulong drawn))
                {
                    seed = drawn;
                }

                continue;
            }

            toml = Turned(toml, Dials[at].Table, Dials[at].Key, typed);
        }

        RulesetLoadResult loaded = RulesetLoader.Parse(toml, Path.GetFileName(_rulesetPath));

        if (loaded.Ruleset is null)
        {
            // The loader's own sentence, verbatim. It names the key and the line, which is the
            // whole reason the rewrite goes through text rather than around it.
            _tunerStatus.Text = loaded.Describe();

            return;
        }

        PrepareCity(loaded.Ruleset, citizens, seed, 1, simulation =>
        {
            _toml = toml;
            _names = loaded.Names;
            _citizens = citizens;
            _seed = seed;
            CloseInspection();
            InstallCity(simulation);
            FinishRegenerate();
        });
    }

    private void FinishRegenerate()
    {
        // The four layers laid once rather than per frame. Ground is fixed to the map and would
        // survive, but it is re-laid with the others so that "what a rebuild redoes" is one list.
        Ground();
        Skin();
        Scatter();
        Hazard();
        Flood();
        Pave();
        Frame();
        Cells();
        Orbit();

        // 🔴 REBUILT, BECAUSE BOTH PANELS ARE A READING OF THE RULESET AND THIS IS A NEW ONE. The
        // governing panel had one row per declared [[policy]] from _Ready and was never redone, so a
        // tune that changed the policy set left it addressing positions the new city has not got.
        Panels();

        _owed = 0d;
        _tunerStatus.Text =
            $"new city: {_citizens:N0} Citizens, seed {_seed}, {_world.Lots.Rows.LiveCount:N0} Lots.";
    }
}
