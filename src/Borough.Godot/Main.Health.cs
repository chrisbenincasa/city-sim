using System;
using System.Collections.Generic;
using System.Linq;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Godot;
namespace Borough.Shell;

public partial class Main
{
    private readonly Dictionary<ulong, Label3D> _healthMarkers = new();
    private PanelContainer? _healthSummary;
    private Label? _healthSummaryText;
    private bool _healthInspection;
    private Color HealthColour(int building)
    {
        if (_world.Rules.ServedBy(_world.Buildings.Kind[building]) == Need.Health) return new Color("65b5d9");
        int burden = 0;
        foreach (int household in _world.Occupants.Walk(building))
        {
            burden = Math.Max(burden, -_world.Households.Health[household]);
            foreach (int citizen in _world.Members.Walk(household))
                burden = Math.Max(burden, _world.Citizens.IllnessSeverity[citizen]);
        }
        float scale = _world.Rules.Care.Runs ? _world.Rules.Care.DeathSeverity : 100;
        return new Color("78cfa0").Lerp(new Color("df6355"), Mathf.Clamp(burden / scale, 0f, 1f));
    }
    private string HealthCaption()
    {
        if (!_world.Rules.Care.Runs) return string.Empty;
        CareReading r = _simulation.Civic.Read();
        return $"\nHealth: sick {r.Sick} · waiting {r.Waiting} · appointments {r.Booked} · beds {r.Inpatient}/{r.Beds} · awaiting admission {r.AwaitingBed}"
            + $"\nDeaths while awaiting admission {r.DeathsAwaitingBed} (last {_world.Rules.Care.ReportDays} Days) · missed work {r.MissedTicks} Ticks · lost earnings {r.LostWages}";
    }
    private void RefreshHealthMarkers()
    {
        if (_healthSummary is null)
        {
            _healthSummary = InformationPanel();
            var column = new VBoxContainer();
            _healthSummaryText = InformationLabel("", 15); column.AddChild(_healthSummaryText);
            column.AddChild(InformationButton("Care history →", () => Ui("health")));
            _healthSummary.AddChild(column);
            ThemeInformation();
        }
        _healthSummary.Visible = _washing == Wash.Health;
        _healthSummary.Position = new Vector2(16, 16);
        _healthSummary.Size = new Vector2(Math.Min(400, GetViewport().GetVisibleRect().Size.X - 32), 0);
        if (_healthSummary.Visible && _healthSummaryText is not null)
        {
            CareReading reading = _simulation.Civic.Read();
            _healthSummaryText.Text = $"HEALTH\nSick {reading.Sick} · serious {reading.Serious}\nAwaiting care {reading.Waiting} · admission {reading.AwaitingBed}\nBeds {reading.Inpatient} / {reading.Beds}\nDeaths awaiting admission: {reading.DeathsAwaitingBed} in {_world.Rules.Care.ReportDays} Days\nGreen healthy → red illness/Health deficit\nBlue: care facilities";
        }
        foreach (Label3D marker in _healthMarkers.Values) marker.Visible = false;
        if (_washing != Wash.Health || !_world.Rules.Care.Runs) return;
        var live = new HashSet<ulong>();
        for (int b = 0; b < _world.Buildings.Rows.SlotCount; b++)
        {
            if (!_world.Buildings.Rows.IsLive(b) || _world.Rules.ServedBy(_world.Buildings.Kind[b]) != Need.Health
                || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[b], out int lot)) continue;
            ulong id = _world.Buildings.Rows.IdAt(b); live.Add(id);
            if (!_healthMarkers.TryGetValue(id, out Label3D? label))
            {
                label = new Label3D { Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                    NoDepthTest = true, FixedSize = true, FontSize = 22, PixelSize = .0005f, Modulate = new Color("ecf8ff") };
                _healthMarkers[id] = label; AddChild(label);
            }
            label.Visible = true;
            label.Position = new Vector3(_world.Lots.East[lot].Raw * MetresPerTile, 26f, -_world.Lots.North[lot].Raw * MetresPerTile);
            var civic = _simulation.Civic;
            label.Text = $"{_names.Kind(_world.Buildings.Kind[b])} {id}\nTreating {civic.Occupied(b, VisitStage.Consultation)}/{civic.TreatmentPlaces(b)}"
                + $" · beds {civic.Occupied(b, VisitStage.Inpatient)}/{civic.Beds(b)}\nAwaiting admission {civic.Occupied(b, VisitStage.AwaitingBed)}";
        }
        foreach (ulong id in _healthMarkers.Keys.Where(id => !live.Contains(id)).ToArray())
        { _healthMarkers[id].QueueFree(); _healthMarkers.Remove(id); }
    }
    private void AddFacilityHealth(List<InformationSection> sections, int building)
    {
        if (!_world.Rules.Care.Runs || _world.Rules.ServedBy(_world.Buildings.Kind[building]) != Need.Health) return;
        var care = _simulation.Civic;
        var rows = new List<InformationRow>
        {
            new($"Treatments: {care.Occupied(building, VisitStage.Consultation)} / {care.TreatmentPlaces(building)} simultaneous places"),
            new($"Beds: {care.Occupied(building, VisitStage.Inpatient)} occupied, {care.Occupied(building, VisitStage.HospitalOutbound)} reserved, {care.Beds(building)} total"),
            new($"Appointments: {care.Occupied(building, VisitStage.Booked)} · waiting for consultation: {care.Occupied(building, VisitStage.ConsultationQueue)}"),
            new($"Awaiting admission after consulting here: {care.Occupied(building, VisitStage.AwaitingBed)}"),
        };
        sections.Add(new("care", "Care capacity", true, rows));
    }
    private void AddHouseholdHealth(List<InformationSection> sections, int household)
    {
        if (!_world.Rules.Care.Runs) return;
        var rows = new List<InformationRow> { new($"General Health: {_world.Households.Health[household]} (0 ideal)") };
        var family = _world.FamilyCare;
        for (int f = 0; f < family.Rows.SlotCount; f++)
            if (family.Rows.IsLive(f) && family.Household[f] == _world.Households.Rows.At(household))
                rows.Add(new(family.Habitual[f].IsNone ? "Family doctor: not established"
                    : $"Family doctor: Building {RowId(_world.Buildings.Rows, family.Habitual[f])}"));
        foreach (int c in _world.Members.Walk(household))
        {
            int row = _simulation.Civic.RowOf(c);
            string details = $"Citizen {_world.Citizens.Rows.IdAt(c)} · severity {_world.Citizens.IllnessSeverity[c]} · {ActivitySentence((CitizenActivity)_world.Citizens.Activity[c])}";
            if (row >= 0)
            {
                var s = _world.Civic;
                details += $"\n{VisitSentence((VisitStage)s.Stage[row])} · missed work {s.MissedTicks[row] * 24.0 / Ticks.PerDay:F1} h · lost earnings {s.LostWages[row]}";
                if (s.RequestedAt[row].Raw > 0) details += $"\nWaiting since {CareMoment(s.RequestedAt[row].Raw - 1)}";
                if ((VisitStage)s.Stage[row] == VisitStage.Booked) details += $" · appointment {CareMoment(s.NextAt[row].Raw)}";
            }
            rows.Add(new(details));
        }
        sections.Add(new("health", "Health and care", true, rows));
        ulong id = _world.Households.Rows.IdAt(household);
        var history = _world.CareHistory;
        var events = Enumerable.Range(0, history.Rows.SlotCount)
            .Where(e => history.Rows.IsLive(e) && history.HouseholdId[e] == id)
            .OrderByDescending(e => history.Rows.IdAt(e)).Select(e => new InformationRow(
                $"{CareMoment(history.Tick[e].Raw)} · Citizen {history.CitizenId[e]} · Building {history.BuildingId[e]}\n{CareEventText(history, e)} · severity {history.Severity[e]}"
)).ToList();
        if (events.Count == 0) events.Add(new("No retained care events."));
        sections.Add(new("care-history", "Care history (bounded)", false, events));
    }
    private void AddAreaHealth(List<InformationSection> sections, int east, int north)
    {
        if (!_world.Rules.Care.Runs) return;
        int sick = 0, waiting = 0, households = 0; long missed = 0, earnings = 0;
        for (int h = 0; h < _world.Households.Rows.SlotCount; h++)
        {
            if (!_world.Households.Rows.IsLive(h) || !_world.Buildings.Rows.TryResolve(_world.Households.Dwelling[h], out int b)
                || !_world.Lots.Rows.TryResolve(_world.Buildings.Lot[b], out int lot)
                || _world.Lots.East[lot].Raw / 32 != east / 32 || _world.Lots.North[lot].Raw / 32 != north / 32) continue;
            households++;
            foreach (int c in _world.Members.Walk(h))
            {
                if (_world.Citizens.IllnessSeverity[c] > 0) sick++;
                int r = _simulation.Civic.RowOf(c); if (r < 0) continue;
                if ((VisitStage)_world.Civic.Stage[r] is VisitStage.Waiting or VisitStage.AwaitingBed) waiting++;
                missed += _world.Civic.MissedTicks[r]; earnings += _world.Civic.LostWages[r];
            }
        }
        sections.Add(new("area-health", "Health in this Cell", true,
            [new($"Households {households} · sick Citizens {sick} · unmet care {waiting}\nMissed work {missed} Ticks · lost earnings {earnings}")]));
    }
    private void HealthInformation(List<InformationSection> sections, out string title, out string identity)
    {
        title = "City health"; identity = $"LAST {_world.Rules.Care.ReportDays} DAYS";
        CareReading r = _simulation.Civic.Read();
        sections.Add(new("summary", "Current condition", true,
            [new($"{r.Sick} sick · {r.Serious} seriously ill · {r.AwaitingBed} awaiting admission")]));
        sections.Add(new("health-outcomes", "Care outcomes", true,
            [new($"Deaths while awaiting admission: {r.DeathsAwaitingBed}\nMissed work: {r.MissedTicks * 24.0 / Ticks.PerDay:F1} h\nLost earnings: {r.LostWages}"),
             new("A death while waiting does not establish that treatment would have prevented it.")]));
        var history = _world.CareHistory;
        var rows = Enumerable.Range(0, history.Rows.SlotCount).Where(history.Rows.IsLive)
            .OrderByDescending(e => history.Rows.IdAt(e)).Take(100).Select(e => new InformationRow(
                $"{CareMoment(history.Tick[e].Raw)} · Citizen {history.CitizenId[e]} · Household {history.HouseholdId[e]}\n{CareEventText(history, e)} · facility {history.BuildingId[e]} · severity {history.Severity[e]}"))
            .ToList();
        sections.Add(new("city-care-history", "Recent care events", true, rows));
    }
    private static string CareEventText(CareHistoryTable history, int row)
    {
        CareEventKind kind = (CareEventKind)history.Event[row];
        string sentence = CareSentence(kind);
        if (kind is CareEventKind.Booked or CareEventKind.Postponed)
            return sentence + (history.Value[row] > 0 ? $"; appointment {CareMoment((ulong)history.Value[row])}" : "; awaiting a new appointment");
        if (kind == CareEventKind.MissedWork) return sentence + $": {history.Value[row]}";
        return sentence;
    }
    private static string CareMoment(ulong tick)
    {
        int minute = Ticks.MinuteOfDay(tick);
        return $"Day {tick / Ticks.PerDay} {minute / 60:00}:{minute % 60:00}";
    }
    private static string ActivitySentence(CitizenActivity activity) => activity switch
    {
        CitizenActivity.AtSchool => "at school", CitizenActivity.InTreatment => "receiving treatment",
        CitizenActivity.InHospital => "in hospital", CitizenActivity.ServiceTravelling => "travelling for school or care",
        CitizenActivity.AtHome => "at home", CitizenActivity.AtWork => "at work", _ => "travelling or waiting",
    };
    private static string VisitSentence(VisitStage stage) => stage switch
    {
        VisitStage.Waiting => "Awaiting an appointment", VisitStage.Booked => "Appointment booked",
        VisitStage.AwaitingBed => "Awaiting admission—no reachable bed available",
        VisitStage.Inpatient => "Receiving inpatient care", VisitStage.Consultation => "Consultation underway",
        VisitStage.ConsultationQueue => "Waiting at the clinic", VisitStage.Returning => "Returning home",
        VisitStage.None => "No active visit", _ => "School or care journey scheduled or underway",
    };
    private static string CareSentence(CareEventKind kind) => kind switch
    {
        CareEventKind.Ill => "Illness began", CareEventKind.Worsened => "Illness worsened",
        CareEventKind.Requested => "Care requested", CareEventKind.Booked => "Appointment booked",
        CareEventKind.Postponed => "Appointment postponed; original waiting time retained",
        CareEventKind.NoAppointment => "No reachable clinic could offer an appointment",
        CareEventKind.Unreachable => "Journey could not reach the provider", CareEventKind.NoBed => "No reachable bed available",
        CareEventKind.Admitted => "Admitted to hospital", CareEventKind.Treated => "Consultation completed",
        CareEventKind.Discharged => "Discharged; recovery may continue at home", CareEventKind.Recovered => "Recovered",
        CareEventKind.DiedAwaitingBed => "Died while awaiting admission", CareEventKind.Died => "Died during illness",
        CareEventKind.MissedWork => "Missed work because of illness or care; cumulative lost earnings",
        CareEventKind.SwitchedClinic => "Changed family doctor after excessive waits",
        CareEventKind.SchoolDeparted => "Left for school", CareEventKind.SchoolAttended => "Arrived at school",
        CareEventKind.Returned => "Returned home", CareEventKind.FacilityLost => "Care facility no longer available",
        _ => "Care event",
    };
}
