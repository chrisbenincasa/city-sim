using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Entities;
using Borough.Core.Evidence;
using Borough.Core.Rules;
using Borough.Core.Tables;
using Godot;

namespace Borough.Shell;

public partial class Main
{
    private const int CityPage = 6;

    private PanelContainer _cityPanel = null!;
    private VBoxContainer _cityBody = null!;
    private Button _cityButton = null!;
    private bool _cityShown;
    private CityEvidence _cityReading;
    private bool _cityRead;
    private int _cityGroup = -1, _cityFrom;
    private CityCause? _cityCause;
    private string _citySignature = string.Empty;

    /// <summary>
    /// Building row id to its worst missed-firing count, under the filter in force.
    /// </summary>
    /// <remarks>
    /// <b>Keyed by the row id and not the slot, because the reading is a snapshot.</b> A slot
    /// recycles; the id never does. Colouring by slot would repaint a new Building in a dead one's
    /// colour for as long as the snapshot stood.
    /// </remarks>
    private readonly Dictionary<ulong, long> _troubleOf = [];
    private long _troublePeak;
    private int _troubleSubjects;
    private bool _troubleRepaint;

    /// <summary>The ramp <see cref="TroubleColour"/> paints, for the legend beside it.</summary>
    private static readonly Color[] TroubleBands =
    [
        new Color("78cfa0"), new Color("e9c162"), new Color("df6355"),
    ];

    private void CityEvidencePanel(CanvasLayer layer)
    {
        _cityPanel = Backed(new Vector2(14f, 108f));
        _cityPanel.Theme = _type;
        _cityPanel.Visible = false;

        var box = new VBoxContainer();
        var heading = new HBoxContainer();
        heading.AddChild(new Label
        {
            Text = "City Evidence",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        var refresh = InformationButton("Refresh Evidence", () => Ui("city refresh"));
        refresh.TooltipText = "Re-read the city now. The Trouble wash re-reads on its own cadence";
        heading.AddChild(refresh);
        var close = InformationButton("Close Evidence", () => Ui("city off"));
        close.TooltipText = "Close the list; keep the tool, layer and inspection";
        heading.AddChild(close);
        box.AddChild(heading);

        _cityBody = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        box.AddChild(_cityBody);
        ScrollAuxiliary(_cityPanel, box);
        layer.AddChild(_cityPanel);
    }

    /// <summary>
    /// Takes the snapshot, keeps the open cause pointed at the same cause, and repaints the wash.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>The open group is re-found by its cause and not by its row.</b> Groups sort by how many
    /// subjects they stop, so a refresh re-orders them — and a filter that stayed on row 2 would
    /// silently start showing a different problem.
    /// </remarks>
    private void ReadCity()
    {
        _cityReading = Evidence.OfCity(_world);
        _cityRead = true;
        _citySignature = string.Empty;
        _cityGroup = -1;

        if (_cityCause is CityCause open)
        {
            ReadOnlySpan<CityGroup> groups = _cityReading.Groups.Span;

            for (int at = 0; at < groups.Length; at++)
            {
                if (groups[at].Cause != open) continue;

                _cityGroup = at;

                break;
            }

            if (_cityGroup < 0) { _cityCause = null; _cityFrom = 0; }
        }

        Retrouble();
    }

    /// <summary>Rebuilds the wash's lookup from the snapshot and the filter.</summary>
    private void Retrouble()
    {
        _troubleOf.Clear();
        _troublePeak = 0;
        _troubleSubjects = 0;

        if (!_cityRead) return;

        ReadOnlySpan<CityGroup> groups = _cityReading.Groups.Span;

        foreach (CitySubject subject in _cityReading.Subjects.Span)
        {
            if (_cityGroup >= 0 && subject.Group != _cityGroup) continue;
            if (_cityGroup < 0 && groups[subject.Group].Cause.Blocked == Blocking.Space) continue;

            ulong id = RowId(_world.Buildings.Rows, subject.Building);
            if (id == 0) continue;

            _troubleSubjects++;
            long missed = Math.Max(subject.MissedFirings, 1);
            _troubleOf[id] = Math.Max(_troubleOf.TryGetValue(id, out long held) ? held : 0, missed);
            _troublePeak = Math.Max(_troublePeak, missed);
        }

        _troubleRepaint = true;
    }

    /// <summary>
    /// What colour a Building takes under the Trouble wash.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Settled is a colour and not the absence of one.</b> A Building nothing is wrong with
    /// reads green rather than staying grey, because grey is what a Building the snapshot never
    /// visited would be — and <em>nothing is wrong here</em> and <em>this was not read</em> are two
    /// different claims (<c>Main.Ground.Rewash</c>'s <c>Unmeasured</c>, one level out).
    /// </remarks>
    private Color TroubleColour(int slot)
    {
        ulong id = _world.Buildings.Rows.IdAt(slot);

        if (!_cityRead || !_troubleOf.TryGetValue(id, out long missed))
        {
            return new Color("78cfa0");
        }

        float share = _troublePeak <= 0 ? 1f : missed / (float)_troublePeak;

        return new Color("e9c162").Lerp(new Color("df6355"), Mathf.Clamp(share, 0f, 1f));
    }

    private string TroubleLegend()
    {
        if (!_cityRead)
        {
            return "\nTROUBLE — nothing read yet. Open Evidence and Refresh.";
        }

        string scope = _cityGroup >= 0 && _cityGroup < _cityReading.Groups.Length
            ? CauseSentence(_cityReading.Groups.Span[_cityGroup].Cause)
            : "every cause but routine waiting";

        return $"\nTROUBLE — green settled, amber to red by missed firings to {_troublePeak:N0}. "
            + $"{scope}: {_troubleSubjects:N0} subjects in {_troubleOf.Count:N0} Buildings of "
            + $"{_cityReading.BuildingsRead:N0} read, at Tick {_cityReading.ReadAt.Raw:N0}. "
            + "Select a Building for its Evidence.";
    }

    /// <summary>What one cause says, in the shell's words rather than in ids.</summary>
    /// <remarks>
    /// ⚠ <b>It names the Resource a wait is FOR and never a quantity of it.</b> <c>CONTEXT.md</c> —
    /// <em>supply is a property of one named Bin and never of the city</em> — so a city-wide sentence
    /// carrying a level or a total would be the banned aggregate wearing this list's clothes.
    /// </remarks>
    private string CauseSentence(CityCause cause)
    {
        if (!cause.Explained) return "Blocked, with no wait target recorded";

        string named = _names.Resource(cause.WaitingFor) ?? "an unnamed Resource";
        bool money = _world.Rules.IsConserved(cause.WaitingFor);

        return cause.Blocked == Blocking.Space
            ? $"Waiting for room to put {named}"
            : money ? "Waiting for money" : $"Waiting for {named}";
    }

    private static string CauseDetail(CityCause cause) => !cause.Explained
        ? "No Bin recorded · the explanation is unavailable rather than absent"
        : cause.WaitingOn switch
        {
            BinOwnerKind.District => "Asleep on the District market row · nobody is selling",
            BinOwnerKind.Household => "The Household's own Bin",
            BinOwnerKind.Business => "The Business's own Bin",
            BinOwnerKind.Treasury => "The city's own Bin",
            _ => "The premises' Bin",
        };

    private static string Plural(SubjectKind kind, int many) => (kind, many) switch
    {
        (SubjectKind.Business, 1) => "Business",
        (SubjectKind.Business, _) => "Businesses",
        (SubjectKind.Household, 1) => "Household",
        (SubjectKind.Household, _) => "Households",
        (_, 1) => "Building",
        _ => "Buildings",
    };

    private Label Filled(string text, int size = InformationUi.BodyPoints)
    {
        Label label = InformationLabel(text, size);
        ScrollLabel(label);
        return label;
    }

    /// <summary>A reading that states its own width rather than collapsing or wrapping.</summary>
    private Label Fixed(string text, int size = InformationUi.BodyPoints)
    {
        Label label = InformationLabel(text, size);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        return label;
    }

    private void RefreshCityEvidence()
    {
        _cityPanel.Visible = _cityShown;
        _cityButton.SetPressedNoSignal(_cityShown);

        if (!_cityShown) return;

        string signature =
            $"{_cityRead}|{_cityReading.ReadAt.Raw}|{_cityGroup}|{_cityFrom}|{_textPercent}|{_washing}";
        if (signature == _citySignature) return;
        _citySignature = signature;

        foreach (Node child in _cityBody.GetChildren())
        {
            _cityBody.RemoveChild(child);
            child.QueueFree();
        }

        ReadOnlySpan<CityGroup> groups = _cityReading.Groups.Span;
        int attention = 0;

        foreach (CityGroup group in groups)
        {
            if (group.Cause.Blocked != Blocking.Space) attention += group.Subjects;
        }

        var head = new HBoxContainer();
        head.AddChild(Fixed(
            _cityRead
                ? $"{attention:N0} subjects need attention · {_cityReading.BuildingsRead:N0} Buildings read"
                : "Nothing read yet",
            SecondaryPoints));
        head.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        head.AddChild(Fixed(
            !_cityRead ? string.Empty
                : _washing == Wash.Trouble
                    ? $"Tick {_cityReading.ReadAt.Raw:N0} · following the wash"
                    : $"read at Tick {_cityReading.ReadAt.Raw:N0}",
            SecondaryPoints));
        _cityBody.AddChild(head);

        if (_cityRead && groups.Length == 0)
        {
            _cityBody.AddChild(Filled("Nothing in the city is stopped."));
            return;
        }

        for (int at = 0; at < groups.Length; at++)
        {
            CityGroup group = groups[at];
            int index = at;
            bool open = _cityGroup == at;

            var row = new HBoxContainer();
            var opener = InformationButton(
                $"{group.Subjects:N0}  {Plural(group.Cause.Kind, group.Subjects)} · {CauseSentence(group.Cause)}",
                () => Ui($"city group {index}"));
            opener.Alignment = HorizontalAlignment.Left;
            opener.ToggleMode = true;
            opener.SetPressedNoSignal(open);
            opener.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            opener.TooltipText = $"{CauseDetail(group.Cause)} · worst {group.WorstMissedFirings:N0} missed firings";
            UiIcons.Attach(opener, group.Cause.Explained
                ? group.Cause.Blocked == Blocking.Space ? "waiting" : "trouble"
                : "unavailable");
            row.AddChild(opener);
            _cityBody.AddChild(row);

            var detail = Filled(CauseDetail(group.Cause), SecondaryPoints);
            _cityBody.AddChild(detail);

            if (!open) continue;

            int from = Math.Clamp(_cityFrom, 0, Math.Max(0, group.Subjects - 1));
            int seen = 0, shown = 0;

            foreach (CitySubject member in _cityReading.Subjects.Span)
            {
                if (member.Group != index) continue;
                if (seen++ < from) continue;
                if (shown++ == CityPage) break;

                CitySubject held = member;
                ulong buildingId = RowId(_world.Buildings.Rows, member.Building);
                string name = $"{Plural(member.Kind, 1)} {member.SubjectId}";
                var link = InformationButton(
                    $"{name} · Building {buildingId} →", () => OpenCitySubject(held));
                link.Alignment = HorizontalAlignment.Left;
                link.TooltipText = $"{member.MissedFirings:N0} missed firings · select it";
                _cityBody.AddChild(link);
            }

            if (group.Subjects > CityPage)
            {
                var paging = new HBoxContainer();
                var back = InformationButton("Earlier", () => Ui($"city from {Math.Max(0, from - CityPage)}"));
                back.Disabled = from == 0;
                paging.AddChild(back);
                var next = InformationButton("More", () => Ui($"city from {from + CityPage}"));
                next.Disabled = from + CityPage >= group.Subjects;
                paging.AddChild(next);
                paging.AddChild(Filled(
                    $"{from + 1:N0}–{Math.Min(group.Subjects, from + CityPage):N0} of {group.Subjects:N0}",
                    SecondaryPoints));
                _cityBody.AddChild(paging);
            }
        }
    }

    private void OpenCitySubject(CitySubject subject)
    {
        ulong building = RowId(_world.Buildings.Rows, subject.Building);
        if (building == 0) return;

        Ui($"building {building}");

        if (subject.Kind == SubjectKind.Household) Ui($"household {subject.SubjectId}");
        else if (subject.Kind == SubjectKind.Business) Ui($"business {subject.SubjectId}");
    }

    /// <summary>The <c>ui city …</c> grammar. Returns false when the words are not this panel's.</summary>
    private bool CityEvidenceAction(string[] words)
    {
        if (words.Length < 2 || words[0] != "city") return false;

        switch (words[1])
        {
            case "on" or "off":
                _cityShown = words[1] == "on";
                if (_cityShown && _governing) Govern();
                if (_cityShown) _hud.MoveChild(_cityPanel, -1);
                if (_cityShown && !_cityRead) ReadCity();
                return true;
            case "refresh":
                ReadCity();
                return true;
            case "group" when words.Length == 3 && int.TryParse(words[2], out int group):
                bool closing = _cityGroup == group;
                _cityGroup = closing || group < 0 || group >= _cityReading.Groups.Length ? -1 : group;
                _cityCause = _cityGroup < 0 ? null : _cityReading.Groups.Span[_cityGroup].Cause;
                _cityFrom = 0;
                _citySignature = string.Empty;
                Retrouble();
                return true;
            case "from" when words.Length == 3 && int.TryParse(words[2], out int from):
                _cityFrom = Math.Max(0, from);
                _citySignature = string.Empty;
                return true;
            default:
                return false;
        }
    }
}
