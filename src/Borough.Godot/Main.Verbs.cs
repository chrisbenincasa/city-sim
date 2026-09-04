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

// ---- the verbs -- what a click means, and the one door a command goes through to reach the city 
//
// Ordered() drains the queue into Step and Record() writes the Input Log, so everything here
// ends at Simulation and nothing here decides anything the city could not decide for itself.
// Sentence() is the refusal vocabulary: the shell's job is to say WHY, in words, on the readout.
//
// Moved out of Main.cs on 2026-09-04, plans/0045 queue row 26. One class across nine files,
// no behaviour changed and no State Hash moved.

public partial class Main
{
    /// <summary>
    /// This Tick's input: whatever the player raised since the last one, and then nothing.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The queue is CLEARED here and not after the step</b>, because a command that threw would
    /// otherwise be re-sent on the next frame for ever. ⚠ <b>The Ruleset hash is zero, which is what
    /// <c>Step(default)</c> already passed</b> — a non-zero one is a reload instruction
    /// (<c>Simulation</c> compares it against what is in force), and the tuner owns reloads by its
    /// own path.
    /// </remarks>
    private TickInput Ordered()
    {
        if (_queued.Count == 0)
        {
            return default;
        }

        Command[] raised = [.. _queued];

        // 🔴 THE ROAD GRAPH IS LAID ONCE AND A PLAYER CAN CHANGE IT, WHICH IS THE WHOLE DEFECT.
        // Pave() ran in _Ready and on a tuner rebuild and nowhere else, so a Street laid by Connect
        // existed in the world, routed Trips, carried Lots -- and was never drawn. ***The verb
        // looked broken while working perfectly***, which is the worst way for a verb to fail.
        // Flagged rather than re-paved here because nothing has stepped yet: the Command applies at
        // the top of the next Tick, so the Segment does not exist until Step returns.
        foreach (Command command in raised)
        {
            if (command.Kind == CommandKind.Connect)
            {
                _repave = true;
            }

            // ⚠ _world.Tick AND NOT _world.Tick + 1. Ordered() is evaluated as the argument to
            // Step, so the Tick it names is the one about to run and the one these Commands apply
            // at. Recording the Tick after would put every verb one Tick late in the replay -- a
            // divergence that appears at the first command and looks like a simulation bug.
            _log.Append(_world.Tick, command);
        }

        _queued.Clear();

        return new TickInput(raised, 0);
    }

    /// <summary>
    /// Writes the session so far to a <c>.borough</c> file, and prints what would reproduce it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS IS THE ROW THAT MAKES THE OTHER THREE WORTH ANYTHING.</b> A verb that is not in
    /// the log is a state change no replay reproduces and no State Hash divergence explains — the
    /// sentence <c>Populate</c>, <c>Trip</c> and <c>Arrive</c> each already carry in their own
    /// remark, and which nothing in the shell honoured until now.
    /// </para>
    /// <para>
    /// ⚠ <b>It USES the codec and never implements one</b> (<c>adr/0039</c>).
    /// <see cref="InputLogCodec"/> lives in <c>Borough.Formats</c>, which both shells reference and
    /// neither may duplicate — a second writer is a second format the day one of them is fixed.
    /// </para>
    /// <para>
    /// ⚠ <b>The State Hash goes to the console beside the path, and it is the whole point of the
    /// line.</b> The round trip is <em>replay this file to this Tick and get this number</em>, so
    /// printing the path alone would leave the operator to find the number themselves — and a
    /// verification nobody can run is not one.
    /// </para>
    /// </remarks>
    private void Record()
    {
        string path = Path.Combine(
            System.Environment.CurrentDirectory,
            $"session-{_world.Tick.Raw}{InputLogCodec.Extension}");

        using (var writer = new StreamWriter(path))
        {
            InputLogCodec.Write(writer, _log.Build());
        }

        // 🔴 THE RULESET IN FORCE IS NOT ALWAYS THE FILE ON DISK, and a reproduce line that assumed
        // it was would be wrong exactly when somebody had been experimenting. Regenerate() rebuilds
        // the world from EDITED toml held in memory, so after a tuner pass the log names a content
        // hash no file has -- and Replay.Start refuses a catalogue whose opening hash differs, which
        // is the refusal working and an operator with no way to satisfy it. So the edited Ruleset is
        // written out beside the log and the line points at THAT.
        string rules = _rulesetPath;

        // ⚠ GLOBALIZED HERE AND NOT AT THE FIELD, because the field is what the reproduce line below
        // prints and a person pastes a RELATIVE path back into a command. Reading it raw threw
        // FileNotFoundException out of Quit for every run given `--ruleset rulesets/x.toml`, which is
        // every run -- and an exception out of Quit does not end the process, it leaves the frame loop
        // turning, so `w` and BOROUGH_LOG looked like a hang rather than a failure. Boot has resolved
        // this same path through Globalize since it was added; this one line did not.
        if (RulesetFile.HashOfContent(System.Text.Encoding.UTF8.GetBytes(_toml))
            != RulesetFile.HashOf(Globalize(_rulesetPath)))
        {
            rules = Path.ChangeExtension(path, ".toml");
            File.WriteAllText(rules, _toml);
            GD.Print($"the Ruleset in force is tuned and is not {_rulesetPath}; wrote {rules}");
        }

        GD.Print(
            $"wrote {path} — {_log.Build().Count} commands over {_world.Tick.Raw} Ticks.\n"
            + $"  State Hash 0x{_world.HashState():X16}\n"
            + "  reproduce: dotnet run --project src/Borough.Headless -- "
            + $"--log {path} --ruleset {rules} --ticks {_world.Tick.Raw} "
            + $"--hash-every {_world.Tick.Raw}");
    }

    /// <summary>What the current verb is called, and for Zone which permission it paints.</summary>
    private string Holding() => _verb switch
    {
        // ⚠ "SUBDIVIDE" AND NOT "ZONE", because the verb is not a brush. LotSubdivider.Face returns
        // zero on a frontage Frontage has already claimed, so this creates Lots on virgin ground and
        // can never repaint an existing block. ***A label that promised a brush would make the
        // commonest misuse -- clicking a block that already has Lots -- look like a bug.***
        Verb.Zone => _world.Rules.ZoneRules.Length > 0
            ? $"SUBDIVIDE 0x{_world.Rules.ZoneRules[_zoneChoice].Admits:X4} (z cycles)"
            : "SUBDIVIDE — no [[zone_rule]] declared",
        Verb.Connect => "STREET (shift-click bulldozes)",
        Verb.Demolish => "DEMOLISH — abandoned only",
        Verb.Service => _serviceKind != 0
            ? $"SERVICE {_names.Kind(_serviceKind) ?? _serviceKind.ToString()} (s cycles)"
            : "SERVICE — no kind declares `serves`",
        _ => "look",
    };

    // ---- the verbs -------------------------------------------------------------------------------

    /// <summary>
    /// Turns the click into a <see cref="Command"/> and queues it. <b>Nothing here touches the
    /// world.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THE SHELL ASKS THE CORE WHETHER A COMMAND WOULD BE REFUSED AND NO LONGER RESTATES A
    /// RULE.</b> It used to guard three refusals by re-implementing them —
    /// <c>plans/0012</c> <b>Cause 1</b> by construction, two places storing one rule and the copy is
    /// the one that drifts — and <b>ten</b> belong to the five verbs it issues.
    /// <see cref="Simulation.Refuses"/> answers off the predicate the applier uses, so ***the shell
    /// declines to SEND what the core would refuse***, on the core's own finding rather than on a
    /// paraphrase of it.
    /// </para>
    /// <para>
    /// ⚠ <b>Why declining rather than catching:</b> commands are applied at the top of a Tick, so an
    /// exception out of <c>Apply</c> aborts <c>Step</c> half way and leaves a world no invariant
    /// covers. ***A crash is not the worst outcome of an unguarded click; a half-stepped world
    /// is.***
    /// </para>
    /// <para>
    /// ⚠ <b>What stays the shell's is AIMING, and it is a different question.</b> <em>No Building
    /// stands in this Cell</em> and <em>that is not on the map</em> are the shell failing to resolve
    /// a cursor into an address; the core is never asked, because there is no command to ask about.
    /// ***A rule belongs to the city and an aim belongs to the hand.***
    /// </para>
    /// <para>
    /// ⚠ <b>Zone has no guard and needs none.</b> A block with no Street on any face yields no Lots,
    /// which is <c>02 §2.2</c>'s third rule and the mechanism by which a bad street layout punishes
    /// the player — ***it is an outcome and not a refusal***, and a shell that greyed it out would be
    /// hiding the lesson. <see cref="Simulation.Refuses"/> says so too.
    /// </para>
    /// </remarks>
    /// <param name="inverted">Whether shift was held, which turns <c>Connect</c> into a bulldoze.</param>
    private void Act(bool inverted)
    {
        // 🔴 CLEARED AFTER THE LOOK CHECK AND NOT BEFORE IT, which was found by driving a script
        // with a misspelt tool in it. `hold plough` disarms the hand and says so; clearing first
        // meant the very next click wiped that sentence before anybody could read it, and the run
        // reported nothing wrong at all. ***A refusal a later click erases is a refusal nobody
        // sees***, and looking is the one verb that changes nothing and should erase nothing.
        if (_verb == Verb.Look)
        {
            return;
        }

        _refused = string.Empty;

        if (Aim() is not { } at)
        {
            _refused = "that is not on the map.";

            return;
        }

        switch (_verb)
        {
            case Verb.Zone when _world.Rules.ZoneRules.Length > 0:
                Send(new Command(
                    CommandKind.Zone,
                    at.East,
                    at.North,
                    _world.Rules.ZoneRules[_zoneChoice].Admits));
                break;

            case Verb.Zone:
                _refused = "this Ruleset declares no [[zone_rule]], so there is no permission to paint.";
                break;

            case Verb.Connect:
                Lay(at, inverted);
                break;

            case Verb.Demolish:
                Clear(at);
                break;

            case Verb.Service:
                Raise(at);
                break;

            default:
                break;
        }
    }

    /// <summary>Choose what the next click means, and which one of the tool.</summary>
    /// <remarks>
    /// <para>
    /// ⚠ <b>The tool is resolved from its NAME here and nowhere else.</b> Which verbs this shell
    /// offers is not something <c>Borough.Formats</c> can know, so the grammar carries the word and
    /// this is the only place that knows what the words are — see <see cref="DriveVerb.Hold"/>.
    /// </para>
    /// <para>
    /// ⚠ <b>An unknown tool is a refusal a person reads rather than a silent <c>look</c>.</b> A
    /// script that misspells its tool would otherwise click five times with nothing held and report
    /// a city that ignored it.
    /// </para>
    /// </remarks>
    private void Hold(string tool, int choice)
    {
        _refused = string.Empty;

        switch (tool)
        {
            case "look":
                _verb = Verb.Look;

                break;

            case "zone":
                _verb = Verb.Zone;
                _zoneChoice = _world.Rules.ZoneRules.Length > 0
                    ? Math.Clamp(choice, 0, _world.Rules.ZoneRules.Length - 1)
                    : 0;

                break;

            case "street":
                _verb = Verb.Connect;

                break;

            case "demolish":
                _verb = Verb.Demolish;

                break;

            case "service":
                _verb = Verb.Service;

                // Zero means "the first kind that serves anything", which is what the s key sends
                // the first time it is pressed. A kind id is 1-based (Ruleset.KindCount).
                _serviceKind = choice > 0 && choice <= byte.MaxValue
                    ? (byte)choice
                    : NextService(0);

                break;

            default:
                // 🔴 DISARMED RATHER THAN LEFT AS IT WAS, and this was found by running it: a
                // misspelt tool left the PREVIOUS one held, so the next click acted with a verb
                // the script had not asked for. ***An unrecognised instruction must not leave a
                // loaded hand*** -- looking is the one verb that cannot do damage.
                _verb = Verb.Look;
                _refused = $"there is no tool called '{tool}'. There is look, zone, street, "
                    + "demolish and service.";

                break;
        }

        ShowTools();
    }

    /// <summary>The keyboard's own <c>hold</c>, so a hand's choice records exactly as a script's.</summary>
    private DriveCommand Held(string tool, int choice) =>
        new(_world.Tick.Raw, DriveVerb.Hold, choice, tool);

    /// <summary>
    /// Queues a command the city would accept, or <b>says in words why it would not.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b><c>plans/0045</c> row 15e, and the whole of it is here.</b> Every verb's refusals were
    /// an <c>InvalidOperationException</c> out of Phase 0 — the right artefact for a log, which must
    /// stop rather than diverge from the session it describes, and the wrong one for a person. This
    /// is the one place a <see cref="Command"/> reaches <see cref="_queued"/> from a click, so it is
    /// the one place that has to ask.
    /// </para>
    /// <para>
    /// ⚠ <b>The answer is good for exactly as long as the world stands still, and it does.</b>
    /// <see cref="Ordered"/> drains the queue as the argument to <c>Step</c>, so nothing runs between
    /// the question and the command applying. ***A shell that asked, stepped, and then sent would be
    /// guarding a city that no longer exists.***
    /// </para>
    /// </remarks>
    private bool Send(Command command)
    {
        Refusal refusal = _simulation.Refuses(command);

        if (refusal == Refusal.None)
        {
            _queued.Add(command);

            return true;
        }

        _refused = Sentence(refusal, command);

        return false;
    }

    /// <summary>
    /// A <see cref="Refusal"/> in the player's words — <b>and the shell owns every one of them.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>The core hands back a number and this is where it becomes a sentence</b>, which is
    /// <c>CLAUDE.md</c>'s leak vector stated as a design rather than as a warning: <em>"the real leak
    /// vector is not <c>using Godot;</c> — it is a method that returns a formatted string because a
    /// panel wanted one."</em> A second front end may word these differently or in another language,
    /// and neither is the city's business.
    /// </para>
    /// <para>
    /// ⚠ <b>They are not the exception messages and must not be.</b>
    /// <c>Simulation.Explain</c> writes for whoever is holding a crash artefact and names the ADR,
    /// the successor mechanism and the Ruleset key; these are for somebody who has just clicked and
    /// wants to know why nothing happened. ***Same rule, two readers, two registers.***
    /// </para>
    /// <para>
    /// ⚠ <b>The unmapped arm names the number rather than saying nothing.</b>
    /// <c>src/Borough.Godot</c> is not in <c>Borough.slnx</c>, so no test can assert this table is
    /// complete — and a silent fallback would make a missing sentence look like a click that
    /// worked. <see cref="Borough.Tests"/> has no reach here; the screen is the only reviewer.
    /// </para>
    /// </remarks>
    private string Sentence(Refusal refusal, Command command) => refusal switch
    {
        Refusal.ConnectRoadKindIsNotStreet =>
            "only a Street can be laid by hand — an Arterial is a route rather than one click.",

        Refusal.ConnectWorldHasNoLattice =>
            "this Ruleset states no [roads] block_tiles, so it has no lattice to edit.",

        Refusal.DemolishNoBuildingOnThatTile =>
            "nothing stands on that plot to clear.",

        Refusal.DemolishBuildingIsOccupied =>
            "somebody still lives there. Clearing occupied ground is a compulsory purchase and its "
            + "price is not built, so only abandoned buildings can be cleared.",

        Refusal.ServiceKindNotDeclared =>
            "this Ruleset declares no such building.",

        Refusal.ServiceKindServesNothing =>
            $"{_names.Kind((byte)command.Zone) ?? "that building"} serves nobody, and only a service "
            + "building is placed by hand — everything else is built by the city on land you zone.",

        Refusal.ServiceNoVacantLotOnThatTile =>
            "that plot is taken. A standing building and an abandoned shell both hold one; demolish "
            + "first.",

        Refusal.GovernNoSuchPolicy or Refusal.GovernPolicyNotInThisWorld =>
            "this city has no such policy to govern.",

        Refusal.GovernPolicyHasNoName =>
            "that [[policy]] states no name, and a governed amount is saved against its name — so "
            + "there would be nothing to restore it to after a reload.",

        Refusal.TripRulesetStatesNoTrips or Refusal.TripWorldHasNoLattice
            or Refusal.TripBlockHoldsNobody or Refusal.TripEndpointsAreOneBuilding
            or Refusal.TripOriginHoldsNoCitizen =>
            "nobody can make that journey.",

        Refusal.ArriveNoGateOnThatTile =>
            "no gate stands there, so nobody can arrive through it.",

        Refusal.PeopleWorldAlreadyHasAPopulation =>
            "this city already has people in it, and there is only one moving-in day.",

        Refusal.PeopleWorldHasNoLots =>
            "there is nowhere for anybody to live yet. Lay some streets, zone the blocks they "
            + "enclose, and then ask again.",

        Refusal.VerbNotApplied =>
            "that verb is not built yet.",

        _ => $"refused for reason {(ushort)refusal}, which this shell has no sentence for.",
    };

    /// <summary>One Street on the lattice edge <b>nearest</b> the cursor.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>THIS SENT THE CURSOR'S OWN TILE AND SPLIT THE BLOCK ON A DIAGONAL, AND THE PLAYER
    /// COULD NOT AIM IT.</b> Two invisible rules composed. <c>Simulation.ApplyConnect</c> FLOORS
    /// whatever Tile it is given, so the edit landed on the south-west corner of the block clicked
    /// in and never on the nearest one; the axis was then <c>alongEast >= alongNorth</c>, a diagonal
    /// split of that same block. ***So a click near a block's top-right corner laid a Street at its
    /// bottom-left.*** ⚠ <b>Face midpoints were right and the interior was not</b>, which is why the
    /// playtest's forty clicks produced forty Segments and no surprise.
    /// </para>
    /// <para>
    /// <b><see cref="StreetGrid.NearestEdge"/> resolves the aim and
    /// <see cref="StreetGrid.IntersectionTile"/> addresses it</b>, so the Tile this sends is the
    /// intersection itself and the core's floor is a no-op on it. ⚠ <b>The floor is not the defect
    /// and is not touched</b>: it is <c>adr/0014</c>'s <em>Streets snap to the grid</em>, and moving
    /// it would change what every already-recorded <c>.borough</c> log replays to. ***A rule belongs
    /// to the city and an aim belongs to the hand***, which is the division
    /// <see cref="Act"/> already states.
    /// </para>
    /// </remarks>
    private void Lay((Tiles East, Tiles North) at, bool bulldoze)
    {
        // ⚠ A world with no lattice has block 0 and NearestEdge answers NoSlot for it. The CORE
        // refuses that world by name (Refusal.ConnectWorldHasNoLattice) and Send is what reports it
        // -- so this is an aim the shell cannot take rather than a rule it is restating.
        StreetGrid streets = _world.Roads.Streets;

        if (streets.BlockTiles <= 0)
        {
            Send(new Command(CommandKind.Connect, at.East, at.North, default));

            return;
        }

        (int column, int row, StreetAxis axis) = streets.NearestEdge(at.East, at.North);
        (Tiles east, Tiles north) = streets.IntersectionTile(column, row);

        var payload = new ConnectPayload(
            axis,
            bulldoze ? ConnectAction.Bulldoze : ConnectAction.Lay,
            RoadKind.Street);

        Send(new Command(CommandKind.Connect, east, north, payload.Encode()));
    }

    /// <summary>Clears the abandoned Building nearest the cursor, at its own Lot's Tile.</summary>
    /// <remarks>
    /// 🔴 <b>The command names the LOT's Tile and never the cursor's, and that is not a convenience.</b>
    /// <c>Simulation.BuildingOn</c> matches a Lot's coordinate exactly and
    /// <c>ApplyDemolish</c> refuses rather than clearing the nearest — <em>"a mistyped command must
    /// not be indistinguishable from the demolition somebody meant"</em>. A cursor lands on a Tile
    /// that is almost never a Lot's own, so the shell resolves the click to a Building, shows which
    /// one in the hover, and then sends <b>that Building's address</b>. ***The refusal stays exact
    /// and the aim becomes possible.***
    /// </remarks>
    private void Clear((Tiles East, Tiles North) at)
    {
        (int building, int lot, _) =
            NearestIn(at, CellGrid.ToCells(at.East), CellGrid.ToCells(at.North));

        if (building == Rows.NoSlot)
        {
            _refused = "no Building stands in this Cell.";

            return;
        }

        Send(new Command(CommandKind.Demolish, _world.Lots.East[lot], _world.Lots.North[lot]));
    }

    /// <summary>Raises the held service kind on the vacant Lot nearest the cursor in its Cell.</summary>
    /// <remarks>
    /// <para>
    /// 🔴 <b>An O(Lots) walk, and it is the shell's second one.</b> There is no Cell index over Lots
    /// — <c>BuildingResidency</c> indexes Buildings, and a Lot is a point on a Segment rather than
    /// ground — so finding a vacant one near a Tile means walking the table. ⚠ <b>It runs on a CLICK
    /// and on a hover in this mode only</b>, never in the ordinary per-frame path, which is the one
    /// thing that keeps it off the same list as <c>Main.Draw</c>'s three whole-table walks
    /// (<c>plans/0013</c>). ***It is still an unpriced walk and the row says so.***
    /// </para>
    /// <para>
    /// ⚠ <b>The command names the LOT's Tile</b>, on <see cref="Clear"/>'s reasoning and
    /// <c>ApplyService</c>'s own: <em>"a school landing on a neighbour's plot because the click
    /// resolved to the first is worse than a refusal."</em>
    /// </para>
    /// </remarks>
    private void Raise((Tiles East, Tiles North) at)
    {
        if (_serviceKind == 0)
        {
            _refused = "this Ruleset declares no kind with a `serves` key, so there is no service to place.";

            return;
        }

        int lot = VacantNear(at);

        if (lot == Rows.NoSlot)
        {
            _refused =
                "no vacant Lot in this Cell. A Lot holding a Building — standing or an abandoned "
                + "shell — is not vacant; demolish first.";

            return;
        }

        // THE FACTORY AND NOT THE CONSTRUCTOR, for the reason Command.Govern's own remark gives:
        // the packing is named in one place rather than spelled at each call site.
        Send(Command.Service(_world.Lots.East[lot], _world.Lots.North[lot], _serviceKind));
    }

    /// <summary>The vacant Lot nearest a Tile within its own Cell, or <see cref="Rows.NoSlot"/>.</summary>
    private int VacantNear((Tiles East, Tiles North) at)
    {
        LotTable lots = _world.Lots;
        Cells east = CellGrid.ToCells(at.East);
        Cells north = CellGrid.ToCells(at.North);
        int nearest = Rows.NoSlot;
        long best = long.MaxValue;

        for (int slot = 0; slot < lots.Rows.SlotCount; slot++)
        {
            if (!lots.Rows.IsLive(slot) || !lots.IsVacant(slot)
                || CellGrid.ToCells(lots.East[slot]) != east
                || CellGrid.ToCells(lots.North[slot]) != north)
            {
                continue;
            }

            long de = lots.East[slot].Raw - at.East.Raw;
            long dn = lots.North[slot].Raw - at.North.Raw;
            long away = (de * de) + (dn * dn);

            if (away < best)
            {
                best = away;
                nearest = slot;
            }
        }

        return nearest;
    }

    /// <summary>The first declared kind that serves a Need, or zero when the file declares none.</summary>
    /// <remarks>
    /// ⚠ <b>Ids run <c>1..KindCount</c></b> (<c>Ruleset.KindCount</c>), so zero is <em>none</em> and
    /// not the first kind — the same encoding <c>ApplyService</c> reads when it refuses an id nothing
    /// declares.
    /// </remarks>
    private byte NextService(byte after)
    {
        int count = _world.Rules.KindCount;

        for (int step = 1; step <= count; step++)
        {
            var kind = (byte)(((after + step - 1) % count) + 1);

            if (_world.Rules.Declares(kind) && _world.Rules.Kind(kind).Serves != Need.None)
            {
                return kind;
            }
        }

        return 0;
    }
}
