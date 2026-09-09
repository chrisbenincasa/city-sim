using System;
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
    /// </remarks>
    private void Governing(CanvasLayer layer)
    {
        _policyPanel = Backed(new Vector2(14f, 108f));
        _policyPanel.Theme = _type;
        _policyPanel.Visible = _governing;

        var box = new VBoxContainer();

        var heading = new HBoxContainer();
        heading.AddChild(new Label { Text = "Government · Policies", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var close = InformationButton("Close Government", Govern);
        close.TooltipText = "Close Policies";
        heading.AddChild(close);
        box.AddChild(heading);

        // 🔴 THE FIELD IS THE TRANSFER AMOUNT AND NOT THE TAX RATE, and a panel that did not say so
        // would be actively misleading: on levied.toml every row reads `1` while the levy that
        // actually bites is `percent = 10` in the apply rule. Govern writes PolicyTable.Amount and
        // nothing else -- ApplyCount is Ruleset data and is not governable -- so a person turning
        // this dial expecting a rate would change the wrong number and watch nothing happen.
        box.AddChild(new Label
        {
            Text = "what ONE application moves. how many and what share is the [[policy]]'s"
                + " apply rule, which is not governable.",
        });

        PolicyDefinition[] declared = _world.Rules.Policies;

        _policyFields = new LineEdit[declared.Length];

        for (int at = 0; at < declared.Length; at++)
        {
            var row = new HBoxContainer();
            bool governable = _world.Rules.PolicyKey(at) != 0;
            string named = _names.Policy(at) ?? "unnamed";

            row.AddChild(new Label
            {
                Text = governable
                    ? $"{named} — sweeps {declared[at].Subject.ToString().ToLowerInvariant()} "
                        + $"every {declared[at].Interval:N0}"
                    : $"{named} (no name — ungovernable)",
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
            var set = InformationButton("Set", () => Govern(position));
            set.Disabled = !governable;
            row.AddChild(set);
            box.AddChild(row);
        }

        if (declared.Length == 0)
        {
            box.AddChild(new Label { Text = "this Ruleset declares no [[policy]]." });
        }

        _policyStatus = new Label { Text = "a governed amount is saved state and survives a reload." };

        box.AddChild(_policyStatus);
        ScrollAuxiliary(_policyPanel, box);
        layer.AddChild(_policyPanel);
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
        }
    }

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
