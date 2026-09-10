using Borough.Core.Arithmetic;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Movement;
using Borough.Core.Quantities;
using Borough.Core.Tables;
namespace Borough.Core.Rules;

public enum VisitStage : byte
{
    None, SchoolPlanned, SchoolOutbound, SchoolPresent, Returning,
    Waiting, Booked, ClinicOutbound, Consultation, AwaitingBed, HospitalOutbound, Inpatient, ConsultationQueue,
}
public enum CareEventKind : byte
{
    Ill, Worsened, Requested, Booked, Postponed, Unreachable, NoBed, Admitted,
    Treated, Discharged, Recovered, DiedAwaitingBed, Died, MissedWork, SwitchedClinic,
    SchoolDeparted, SchoolAttended, Returned, FacilityLost, NoAppointment,
}
public readonly record struct CareReading(int Sick, int Serious, int Waiting, int Booked, int Treating,
    int AwaitingBed, int Inpatient, int Beds, int TreatmentPlaces, int DeathsAwaitingBed, long MissedTicks, long LostWages);

public sealed class CivicEngine : IComparer<int>
{
    public RouteWork? RouteWork { get => _walk.Work; set => _walk.Work = value; }
    public Action<CareEventKind>? EventObserved { get; set; }
    private readonly World _world;
    private readonly TripEngine _trips;
    private readonly CommuteEngine _commutes;
    private readonly WalkScratch _walk = new();
    private readonly Dictionary<ulong, int> _people = new();
    private readonly Dictionary<ulong, int> _families = new();
    private readonly Dictionary<int, int> _calendar = new();
    private int[] _calendarSlots = [];
    private int _calendarUsed;
    private long _statsDay = -1;
    private int _statsRow = -1;
    private int _statsWindow;
    private int[] _order = [];
    private long[] _priority = [];
    private ulong[] _oldBookings = [];
    private CivicTable State => _world.Civic;
    private FamilyCareTable Families => _world.FamilyCare;
    private CareRuleset Rules => _world.Rules.Care;
    public CivicEngine(World world, TripEngine trips, CommuteEngine commutes)
    {
        _world = world; _trips = trips; _commutes = commutes;
        for (int i = 0; i < State.Rows.SlotCount; i++)
            if (State.Rows.IsLive(i) && world.Citizens.Rows.TryResolve(State.Citizen[i], out int c))
                _people[world.Citizens.Rows.IdAt(c)] = i;
        for (int i = 0; i < Families.Rows.SlotCount; i++)
            if (Families.Rows.IsLive(i) && world.Households.Rows.TryResolve(Families.Household[i], out int h))
                _families[world.Households.Rows.IdAt(h)] = i;
    }
    public static bool TooIllToWork(World world, int citizen) => world.Rules.Care.Runs
        && world.Citizens.IllnessSeverity[citizen] >= world.Rules.Care.SeriousSeverity;
    public int RowOf(int citizen) => _people.TryGetValue(_world.Citizens.Rows.IdAt(citizen), out int row) ? row : -1;
    private int Ensure(int citizen, Ticks tick)
    {
        ulong id = _world.Citizens.Rows.IdAt(citizen);
        if (_people.TryGetValue(id, out int row)) { return row; }
        row = State.Rows.Resolve(State.Rows.Allocate()); _people[id] = row;
        State.Citizen[row] = _world.Citizens.Rows.At(citizen);
        State.Household[row] = _world.Citizens.HouseholdOf[citizen];
        State.SchoolDay[row] = -1; State.LastMissedDay[row] = -1; State.LastWaitDay[row] = -1;
        State.ProgressAt[row] = tick + new Ticks(Randomness.Draw(_world.Key, id, Ticks.Zero,
            PurposeTag.IllnessCadence) % Ticks.PerDay + 1);
        if (Rules.Runs)
            State.RoutineAt[row] = tick + new Ticks(Randomness.Draw(_world.Key, id, Ticks.Zero,
                PurposeTag.RoutineCare) % ((ulong)Rules.RoutineDays * Ticks.PerDay) + 1);
        return row;
    }
    private int Family(int hh)
    {
        ulong id = _world.Households.Rows.IdAt(hh);
        if (_families.TryGetValue(id, out int row)) { return row; }
        row = Families.Rows.Resolve(Families.Rows.Allocate()); _families[id] = row;
        Families.Household[row] = _world.Households.Rows.At(hh); Families.LastExcessDay[row] = -1;
        return row;
    }
    public void Step(Ticks tick)
    {
        if (Rules.Runs) { DailyStats(tick); }
        if (_world.Rules.Care.Runs && tick.Raw % (ulong)Rules.Interval == 0)
        {
            for (int c = 0; c < _world.Citizens.Rows.SlotCount; c++)
                if (_world.Citizens.Rows.IsLive(c)) { Ensure(c, tick); }
        }
        bool retired = false;
        for (int row = 0; row < State.Rows.SlotCount; row++)
        {
            if (!State.Rows.IsLive(row)) { continue; }
            if (!_world.Citizens.Rows.TryResolve(State.Citizen[row], out int citizen))
            { State.Rows.Free(State.Rows.At(row)); retired = true; continue; }
            State.Household[row] = _world.Citizens.HouseholdOf[citizen];
            if (Rules.Runs && tick >= State.ProgressAt[row])
            {
                State.ProgressAt[row] = tick + new Ticks(Ticks.PerDay);
                Progress(row, citizen, tick);
                if (!_world.Citizens.Rows.IsLive(citizen)) { continue; }
            }
            Continue(row, citizen, tick);
            if (Rules.Runs) { MissWork(row, citizen, tick); }
        }
        if (retired)
        {
            _people.Clear();
            for (int i = 0; i < State.Rows.SlotCount; i++)
                if (State.Rows.IsLive(i) && _world.Citizens.Rows.TryResolve(State.Citizen[i], out int c))
                    _people[_world.Citizens.Rows.IdAt(c)] = i;
        }
        if (Rules.Runs && tick.Raw % (ulong)Rules.Interval == 0)
        {
            Schedule(tick);
            CleanFamilies();
        }
    }
    public bool ScheduleSchool(int citizen, int home, int school, Ticks tick, int day)
    {
        int row = Ensure(citizen, tick);
        if (Stage(row) != VisitStage.None || TooIllToWork(_world, citizen)) { return false; }
        SchoolRuleset rules = _world.Rules.School;
        if (!new WeeklyHours(rules.Days, 0, 24).Includes(WeeklyHours.DayOf((long)tick.Raw))) { return false; }
        ulong id = _world.Buildings.Rows.IdAt(school);
        int bell = DrawMinute(id, rules.BellEarliest, rules.BellLatest, PurposeTag.SchoolBell);
        int dismissal = DrawMinute(id, rules.DismissEarliest, rules.DismissLatest, PurposeTag.SchoolDismissal);
        ulong start = tick.Raw - tick.Raw % Ticks.PerDay;
        TravelTime cost = Cost(home, school, citizen);
        if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost)) { return false; }
        ulong bellAt = start + (ulong)ClockMinute(bell);
        ulong travel = cost.ToTicksFloor().Raw + 1;
        State.NextAt[row] = new Ticks(bellAt > travel ? bellAt - travel : tick.Raw);
        State.EndsAt[row] = new Ticks(start + (ulong)ClockMinute(dismissal));
        State.SchoolDay[row] = day;
        State.Provider[row] = _world.Buildings.Rows.At(school);
        State.Place[row] = _world.Buildings.Rows.At(home);
        State.Stage[row] = (byte)VisitStage.SchoolPlanned;
        return true;
    }
    private int DrawMinute(ulong id, int first, int last, PurposeTag purpose) => first
        + (int)(Randomness.Draw(_world.Key, id, Ticks.Zero, purpose) % (ulong)(last - first + 1));
    public static int ClockMinute(int minute) => Ticks.AtMinute(
        (minute - Ticks.DayBeginsAtHour * 60 + Ticks.MinutesPerDay) % Ticks.MinutesPerDay);
    private VisitStage Stage(int row) => (VisitStage)State.Stage[row];
    private bool Home(int row, out int home) => _world.Households.Rows.TryResolve(State.Household[row], out int hh)
        && _world.Buildings.Rows.TryResolve(_world.Households.Dwelling[hh], out home) ? true : NoHome(out home);
    private static bool NoHome(out int home) { home = -1; return false; }
    private int Location(int row, int citizen)
    {
        if ((CitizenActivity)_world.Citizens.Activity[citizen] == CitizenActivity.AtWork
            && _world.Businesses.Rows.TryResolve(_world.Citizens.Workplace[citizen], out int work)
            && _world.Buildings.Rows.TryResolve(_world.Businesses.Building[work], out int building)) { return building; }
        return Home(row, out int home) ? home : -1;
    }
    private void Continue(int row, int citizen, Ticks tick)
    {
        VisitStage stage = Stage(row);
        if (Rules.Runs && TooIllToWork(_world, citizen)
            && (CitizenActivity)_world.Citizens.Activity[citizen] == CitizenActivity.AtWork
            && stage is VisitStage.None or VisitStage.Waiting or VisitStage.Booked)
        {
            int workplace = Location(row, citizen);
            if (workplace >= 0) { State.Place[row] = _world.Buildings.Rows.At(workplace); Return(row, citizen, tick); return; }
        }
        if (Rules.Runs && stage == VisitStage.None && State.RoutineAt[row].Raw != 0
            && (tick >= State.RoutineAt[row] || _world.Citizens.IllnessSeverity[citizen] > 0 && tick >= State.TreatedUntil[row]))
        { Request(row, tick); stage = Stage(row); }
        CitizenActivity activity = (CitizenActivity)_world.Citizens.Activity[citizen];
        if (!Rules.Runs && stage is VisitStage.Waiting or VisitStage.Booked or VisitStage.AwaitingBed)
        { State.AdmissionWanted[row] = 0; State.Stage[row] = 0; return; }
        if (!Rules.Runs && stage is VisitStage.Consultation or VisitStage.ConsultationQueue or VisitStage.Inpatient)
        { State.AdmissionWanted[row] = 0; Return(row, citizen, tick); return; }
        if (stage is VisitStage.SchoolOutbound or VisitStage.ClinicOutbound or VisitStage.HospitalOutbound or VisitStage.Returning)
        {
            if (activity == CitizenActivity.ServiceTravelling) { return; }
            bool arrived = (TripFate)_world.Citizens.LastTripFate[citizen] == TripFate.Completed;
            if (arrived) { State.Place[row] = State.Destination[row]; }
            if (stage == VisitStage.Returning)
            {
                if (arrived && Home(row, out int home) && State.Place[row] == _world.Buildings.Rows.At(home))
                { Finish(row, citizen, tick); }
                else if (tick >= State.NextAt[row]) { Return(row, citizen, tick); }
                return;
            }
            if (!arrived)
            {
                Trace(row, CareEventKind.Unreachable, tick);
                if (stage == VisitStage.HospitalOutbound) { State.Stage[row] = (byte)VisitStage.AwaitingBed; }
                else { Return(row, citizen, tick); }
                return;
            }
            if (stage == VisitStage.SchoolOutbound)
            {
                State.Stage[row] = (byte)VisitStage.SchoolPresent;
                _world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtSchool;
                Trace(row, CareEventKind.SchoolAttended, tick);
            }
            else if (stage == VisitStage.ClinicOutbound)
            { State.Stage[row] = (byte)VisitStage.ConsultationQueue; }
            else
            {
                State.Stage[row] = (byte)VisitStage.Inpatient;
                _world.Citizens.Activity[citizen] = (byte)CitizenActivity.InHospital;
                Trace(row, CareEventKind.Admitted, tick);
            }
            stage = Stage(row);
        }
        if (stage == VisitStage.SchoolPlanned && tick >= State.NextAt[row])
        {
            if (activity != CitizenActivity.AtHome || TooIllToWork(_world, citizen)
                || !_world.Buildings.Rows.TryResolve(State.Provider[row], out int school) || !Usable(school, Need.Education)
                || !Home(row, out int home))
            { State.Stage[row] = 0; return; }
            State.Stage[row] = (byte)VisitStage.SchoolOutbound;
            Trace(row, CareEventKind.SchoolDeparted, tick);
            Travel(row, citizen, home, school, TripPurpose.School, tick); return;
        }
        if (stage == VisitStage.SchoolPresent && (tick >= State.EndsAt[row]
            || !_world.Buildings.Rows.TryResolve(State.Provider[row], out int standingSchool) || !Usable(standingSchool, Need.Education)))
        {
            if (_world.Households.Rows.TryResolve(State.Household[row], out int hh) && tick >= State.EndsAt[row])
            {
                RuleEngine.Write(_world.Households.Education, hh, _world.Households.Education[hh]
                    + _world.Rules.Needs.EducationRecover, _world.Rules.Needs.Floor);

                // The child sat the whole day out and walked in at the start of it, so this is a Day
                // of attendance in the sense CONTEXT.md -> Schooling means: the constant is a
                // duration, Days of completed attendance per level. Resolved again rather than reusing
                // the binding above, which the short-circuit leaves unassigned on this branch.
                if (_world.Buildings.Rows.TryResolve(State.Provider[row], out int attended))
                {
                    _world.RecordAttendance(citizen, attended);
                }
            }
            Return(row, citizen, tick); return;
        }
        if (stage == VisitStage.Booked)
        {
            if (!_world.Buildings.Rows.TryResolve(State.Provider[row], out int clinic) || !Usable(clinic, Need.Health))
            { Request(row, tick); Trace(row, CareEventKind.FacilityLost, tick); return; }
            int from = Location(row, citizen);
            if (from < 0 || activity is not (CitizenActivity.AtHome or CitizenActivity.AtWork)) { return; }
            TravelTime cost = Cost(from, clinic, citizen);
            if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost))
            { State.Stage[row] = (byte)VisitStage.Waiting; Trace(row, CareEventKind.Unreachable, tick); return; }
            if (tick.Raw + cost.ToTicksFloor().Raw + 1 < State.NextAt[row].Raw) { return; }
            State.Stage[row] = (byte)VisitStage.ClinicOutbound;
            Travel(row, citizen, from, clinic, TripPurpose.Care, tick); return;
        }
        if (stage is VisitStage.ConsultationQueue or VisitStage.Consultation or VisitStage.Inpatient)
        {
            if (!_world.Buildings.Rows.TryResolve(State.Provider[row], out int provider) || !Usable(provider, Need.Health))
            { Trace(row, CareEventKind.FacilityLost, tick); Return(row, citizen, tick); return; }
            if (stage == VisitStage.ConsultationQueue && tick >= State.NextAt[row]
                && _world.Rules.Kind(_world.Buildings.Kind[provider]).CareHours.IsOpen(tick)
                && Occupied(provider, VisitStage.Consultation) < TreatmentPlaces(provider))
            {
                State.Stage[row] = (byte)VisitStage.Consultation;
                State.EndsAt[row] = tick + new Ticks(Rules.VisitTicks);
                _world.Citizens.Activity[citizen] = (byte)CitizenActivity.InTreatment;
            }
            else if (stage == VisitStage.Consultation && tick >= State.EndsAt[row])
            {
                bool admit = NeedsBed(citizen);
                Treat(row, citizen, tick);
                if (admit)
                {
                    State.AdmissionWanted[row] = 1;
                    State.Stage[row] = (byte)VisitStage.AwaitingBed;
                    if (State.RequestedAt[row].Raw == 0) { State.RequestedAt[row] = tick + new Ticks(1); }
                    FindBed(row, citizen, tick);
                }
                else { Return(row, citizen, tick); }
            }
            else if (stage == VisitStage.Inpatient && !NeedsBed(citizen))
            { State.AdmissionWanted[row] = 0; Trace(row, CareEventKind.Discharged, tick); Return(row, citizen, tick); }
        }
        if (Stage(row) == VisitStage.AwaitingBed && tick >= State.NextAt[row]) { FindBed(row, citizen, tick); }
    }
    private bool NeedsBed(int citizen)
    {
        int severity = _world.Citizens.IllnessSeverity[citizen];
        int poor = _world.Households.Rows.TryResolve(_world.Citizens.HouseholdOf[citizen], out int hh)
            ? -_world.Households.Health[hh] : 0;
        return severity >= Rules.AdmissionSeverity || severity >= Rules.SeriousSeverity
            && poor >= Rules.AdmissionSeverity;
    }
    public void Request(int row, Ticks tick)
    {
        if (Stage(row) is VisitStage.Consultation or VisitStage.Inpatient or VisitStage.ClinicOutbound
            or VisitStage.HospitalOutbound or VisitStage.AwaitingBed or VisitStage.ConsultationQueue) { return; }
        State.SchoolDay[row] = -1;
        State.Stage[row] = (byte)VisitStage.Waiting;
        if (State.RequestedAt[row].Raw == 0)
        { State.RequestedAt[row] = tick + new Ticks(1); Trace(row, CareEventKind.Requested, tick); }
    }
    private void Progress(int row, int citizen, Ticks tick)
    {
        int severity = _world.Citizens.IllnessSeverity[citizen];
        ulong id = _world.Citizens.Rows.IdAt(citizen);
        if (severity == 0)
        {
            int poor = _world.Households.Rows.TryResolve(State.Household[row], out int hh) ? -_world.Households.Health[hh] : 0;
            long chance = Rules.IllnessPerThousand + (long)poor * Rules.HealthRiskPerThousand;
            if ((long)(Randomness.Draw(_world.Key, id, tick, PurposeTag.IllnessOnset) % 1000) >= chance) { return; }
            severity = Rules.InitialSeverity; State.IllSince[row] = tick + new Ticks(1);
            _world.Citizens.IllnessSeverity[citizen] = severity;
            Trace(row, CareEventKind.Ill, tick);
        }
        else
        {
            bool treated = Stage(row) == VisitStage.Inpatient || tick < State.TreatedUntil[row];
            bool deteriorates = !treated && Randomness.Draw(_world.Key, id, tick, PurposeTag.IllnessProgress) % 100
                < (ulong)Rules.DeteriorationPercent;
            int next = severity + (deteriorates ? Rules.DeteriorationPerDay
                : -(treated ? Rules.TreatedRecoveryPerDay : Rules.RecoveryPerDay));
            if (next < 0) { next = 0; }
            if (next > Rules.DeathSeverity) { next = Rules.DeathSeverity; }
            _world.Citizens.IllnessSeverity[citizen] = next;
            if (next > severity) { Trace(row, CareEventKind.Worsened, tick); }
            if (next == 0) { State.IllSince[row] = default; Trace(row, CareEventKind.Recovered, tick); }
            severity = next;
            if (!treated && severity >= Rules.DeathSeverity
                && (CitizenActivity)_world.Citizens.Activity[citizen] != CitizenActivity.ServiceTravelling
                && (CitizenActivity)_world.Citizens.Activity[citizen] is CitizenActivity.AtHome or CitizenActivity.InHospital or CitizenActivity.ServiceStopped)
            {
                ulong waitingDays = State.IllSince[row].Raw == 0 ? 0 : (ulong)IntegerMath.FloorDiv(
                    (long)(tick.Raw + 1 - State.IllSince[row].Raw), Ticks.PerDay);
                ulong risk = (ulong)Rules.DeathPerThousand * (waitingDays + 1);
                if (Randomness.Draw(_world.Key, id, tick, PurposeTag.IllnessDeath) % 1000 < risk)
                {
                    Trace(row, State.AdmissionWanted[row] != 0 ? CareEventKind.DiedAwaitingBed : CareEventKind.Died, tick);
                    _people.Remove(id); _world.DestroyCitizen(State.Citizen[row]);
                    State.Rows.Free(State.Rows.At(row)); return;
                }
            }
        }
        if (severity > 0 && Stage(row) == VisitStage.None) { Request(row, tick); }
        if (TooIllToWork(_world, citizen) && (CitizenActivity)_world.Citizens.Activity[citizen] == CitizenActivity.AtWork
            && Stage(row) is VisitStage.None or VisitStage.Waiting or VisitStage.Booked)
        {
            int from = Location(row, citizen); State.Place[row] = _world.Buildings.Rows.At(from);
            Return(row, citizen, tick);
        }
    }
    private void Treat(int row, int citizen, Ticks tick)
    {
        int before = _world.Citizens.IllnessSeverity[citizen];
        int severity = before - Rules.TreatedRecoveryPerDay;
        _world.Citizens.IllnessSeverity[citizen] = severity > 0 ? severity : 0;
        State.TreatedUntil[row] = tick + new Ticks(Ticks.PerDay);
        State.RoutineAt[row] = tick + new Ticks((ulong)Rules.RoutineDays * Ticks.PerDay);
        State.RequestedAt[row] = default;
        if (_world.Households.Rows.TryResolve(State.Household[row], out int hh))
        {
            int family = Family(hh);
            if (Families.Habitual[family].IsNone || Families.ExcessWaits[family] >= Rules.SwitchAfterWaits)
            {
                bool changed = !Families.Habitual[family].IsNone && Families.Habitual[family] != State.Provider[row];
                Families.Habitual[family] = State.Provider[row]; Families.ExcessWaits[family] = 0;
                if (changed) { Trace(row, CareEventKind.SwitchedClinic, tick); }
            }
        }
        Trace(row, CareEventKind.Treated, tick);
        if (before > 0 && severity <= 0)
        { State.IllSince[row] = default; Trace(row, CareEventKind.Recovered, tick); }
    }
    private void FindBed(int row, int citizen, Ticks tick)
    {
        if (!NeedsBed(citizen)) { State.AdmissionWanted[row] = 0; Return(row, citizen, tick); return; }
        int from = _world.Buildings.Rows.TryResolve(State.Place[row], out int place) ? place : Location(row, citizen);
        int chosen = -1; int best = int.MaxValue;
        for (int b = 0; b < _world.Buildings.Rows.SlotCount; b++)
        {
            if (!Usable(b, Need.Health) || Beds(b) <= Occupied(b, VisitStage.Inpatient) + Occupied(b, VisitStage.HospitalOutbound)) { continue; }
            if (b == from) { chosen = b; break; }
            if (from < 0) { continue; }
            TravelTime cost = Cost(from, b, citizen);
            if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost) || cost.Raw >= best) { continue; }
            chosen = b; best = cost.Raw;
        }
        State.NextAt[row] = tick + new Ticks((ulong)Rules.Interval);
        if (chosen >= 0)
        {
            State.Provider[row] = _world.Buildings.Rows.At(chosen);
            if (chosen == from)
            {
                State.Stage[row] = (byte)VisitStage.Inpatient;
                _world.Citizens.Activity[citizen] = (byte)CitizenActivity.InHospital;
                Trace(row, CareEventKind.Admitted, tick);
            }
            else
            {
                State.Stage[row] = (byte)VisitStage.HospitalOutbound;
                Travel(row, citizen, from, chosen, TripPurpose.Care, tick);
            }
            return;
        }
        if (State.Reason[row] != (byte)CareEventKind.NoBed) { Trace(row, CareEventKind.NoBed, tick); }
        if (Home(row, out int home) && from >= 0 && from != home)
        {
            // Retain admission need while returning; Finish restores the waiting state.
            Return(row, citizen, tick);
        }
        else { _world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtHome; }
    }
    private void Return(int row, int citizen, Ticks tick)
    {
        State.Stage[row] = (byte)VisitStage.Returning;
        State.NextAt[row] = tick + new Ticks((ulong)(_world.Rules.School.Runs ? _world.Rules.School.RetryTicks
            : Rules.Runs ? Rules.Interval : Ticks.PerDay));
        if (!Home(row, out int home)) { return; }
        if (!_world.Buildings.Rows.TryResolve(State.Place[row], out int from))
        { Trace(row, CareEventKind.FacilityLost, tick); Finish(row, citizen, tick); return; }
        if (from == home) { Finish(row, citizen, tick); return; }
        Travel(row, citizen, from, home, State.SchoolDay[row] >= 0 ? TripPurpose.School : TripPurpose.Care, tick);
    }
    private void Finish(int row, int citizen, Ticks tick)
    {
        bool bed = Rules.Runs && State.AdmissionWanted[row] != 0 && NeedsBed(citizen);
        if (!bed) { State.AdmissionWanted[row] = 0; }
        State.Stage[row] = bed ? (byte)VisitStage.AwaitingBed : (byte)VisitStage.None;
        _world.Citizens.Activity[citizen] = (byte)CitizenActivity.AtHome;
        if (Home(row, out int home)) { State.Place[row] = _world.Buildings.Rows.At(home); }
        Trace(row, CareEventKind.Returned, tick);
        if (!bed) { _commutes.Resume(citizen, tick); }
    }
    private void Travel(int row, int citizen, int from, int to, TripPurpose purpose, Ticks tick)
    {
        State.Place[row] = _world.Buildings.Rows.At(from); State.Destination[row] = _world.Buildings.Rows.At(to);
        _world.Citizens.Activity[citizen] = (byte)CitizenActivity.ServiceTravelling;
        _world.Citizens.LastTripFate[citizen] = (byte)TripFate.InFlight;
        _trips.Start(citizen, from, to, _world.ModeOf(citizen), purpose, tick);
    }
    private TravelTime Cost(int from, int to, int citizen) => WalkRouting.Cost(_world.Roads, _world.ModeOf(citizen),
        _world.AccessPoint(from, _world.ModeOf(citizen)), _world.AccessPoint(to, _world.ModeOf(citizen)),
        _world.Rules.Trips.CrossingCost, _walk);
    private bool Usable(int b, Need need) => b >= 0 && _world.Buildings.Rows.IsLive(b)
        && !_world.Buildings.IsAbandoned(b) && _world.Rules.ServedBy(_world.Buildings.Kind[b]) == need;
    public int Beds(int b) => !Usable(b, Need.Health) || !Rules.Runs ? 0 : (int)IntegerMath.FloorDiv(
        IntegerMath.FloorDiv((long)_world.FloorTilesOf(b) * _world.Rules.Kind(_world.Buildings.Kind[b]).BedPercent, 100), Rules.FloorTilesPerBed);
    public int TreatmentPlaces(int b) => !Usable(b, Need.Health) || !Rules.Runs ? 0 : (int)IntegerMath.FloorDiv(
        IntegerMath.FloorDiv((long)_world.FloorTilesOf(b) * (100 - _world.Rules.Kind(_world.Buildings.Kind[b]).BedPercent), 100), Rules.FloorTilesPerTreatment);
    public int Occupied(int building, VisitStage stage)
    {
        int n = 0;
        for (int i = 0; i < State.Rows.SlotCount; i++)
            if (State.Rows.IsLive(i) && Stage(i) == stage && State.Provider[i] == _world.Buildings.Rows.At(building)) { n++; }
        return n;
    }
    private void Schedule(Ticks tick)
    {
        int count = State.Rows.SlotCount;
        if (_order.Length < count) { _order = new int[count]; _priority = new long[count]; _oldBookings = new ulong[count]; }
        _calendar.Clear(); _calendarUsed = 0;
        int queued = 0;
        for (int row = 0; row < count; row++)
        {
            if (!State.Rows.IsLive(row)) { continue; }
            VisitStage stage = Stage(row);
            if (stage is VisitStage.ClinicOutbound or VisitStage.Consultation or VisitStage.ConsultationQueue
                && _world.Buildings.Rows.TryResolve(State.Provider[row], out int b))
            {
                ulong start = tick.Raw;
                ulong end = stage == VisitStage.Consultation ? State.EndsAt[row].Raw
                    : Max(State.NextAt[row].Raw, tick.Raw) + Rules.VisitTicks;
                Reserve(b, start, end, tick);
            }
            if (stage is not (VisitStage.Waiting or VisitStage.Booked)
                || !_world.Citizens.Rows.TryResolve(State.Citizen[row], out int citizen)) { continue; }
            _oldBookings[row] = stage == VisitStage.Booked ? State.NextAt[row].Raw : 0;
            State.Stage[row] = (byte)VisitStage.Waiting;
            long wait = (long)(tick.Raw + 1 - State.RequestedAt[row].Raw);
            long age = IntegerMath.FloorDiv(wait, (long)Rules.PriorityEveryDays * Ticks.PerDay);
            long priority = _world.Citizens.IllnessSeverity[citizen] + age;
            _priority[row] = priority;
            _order[queued++] = row;
        }
        Array.Sort(_order, 0, queued, this);
        for (int i = 0; i < queued; i++)
        {
            int row = _order[i];
            if (!_world.Citizens.Rows.TryResolve(State.Citizen[row], out int citizen)
                || !_world.Households.Rows.TryResolve(State.Household[row], out int hh) || !Home(row, out int home)) { continue; }
            int family = Family(hh); Discover(family, home, citizen, tick);
            ulong limit = (ulong)(_world.Citizens.IllnessSeverity[citizen] >= Rules.SeriousSeverity
                ? Rules.UrgentWaitDays : Rules.WaitDays) * Ticks.PerDay;
            int habitual = _world.Buildings.Rows.TryResolve(Families.Habitual[family], out int h) ? h : -1;
            ulong offered = habitual >= 0 ? Offer(habitual, home, citizen, tick) : ulong.MaxValue;
            int chosen = offered != ulong.MaxValue ? habitual : -1;
            int bestCost = int.MaxValue;
            if (chosen < 0 || offered - tick.Raw > limit)
            {
                long day = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
                if (habitual >= 0 && Families.LastExcessDay[family] != day)
                {
                    Families.LastExcessDay[family] = day;
                    if (Families.ExcessWaits[family] < Rules.SwitchAfterWaits) { Families.ExcessWaits[family]++; }
                }
                for (int p = Families.KnownHead[family] - 1; p >= 0; p = _world.KnownClinics.Next[p] - 1)
                {
                    if (!_world.Buildings.Rows.TryResolve(_world.KnownClinics.Building[p], out int clinic)) { continue; }
                    ulong at = Offer(clinic, home, citizen, tick);
                    if (at == ulong.MaxValue || at > offered) { continue; }
                    int cost = Cost(home, clinic, citizen).Raw;
                    if (at == offered && cost >= bestCost) { continue; }
                    offered = at; chosen = clinic; bestCost = cost;
                }
            }
            if (chosen < 0)
            {
                if (_oldBookings[row] != 0) { Trace(row, CareEventKind.Postponed, tick); }
                if (State.Reason[row] != (byte)CareEventKind.NoAppointment) { Trace(row, CareEventKind.NoAppointment, tick); }
                continue;
            }
            State.Provider[row] = _world.Buildings.Rows.At(chosen);
            State.NextAt[row] = new Ticks(offered); State.EndsAt[row] = new Ticks(offered + Rules.VisitTicks);
            State.Stage[row] = (byte)VisitStage.Booked;
            Reserve(chosen, offered, offered + Rules.VisitTicks, tick);
            if (_oldBookings[row] != offered)
                Trace(row, _oldBookings[row] > 0 && offered > _oldBookings[row] ? CareEventKind.Postponed : CareEventKind.Booked,
                    tick, (long)offered);
        }
    }
    public int Compare(int left, int right)
    {
        int urgency = _priority[right].CompareTo(_priority[left]);
        if (urgency != 0) { return urgency; }
        int waiting = State.RequestedAt[left].Raw.CompareTo(State.RequestedAt[right].Raw);
        if (waiting != 0) { return waiting; }
        int l = _world.Citizens.Rows.Resolve(State.Citizen[left]);
        int r = _world.Citizens.Rows.Resolve(State.Citizen[right]);
        return _world.Citizens.Rows.IdAt(l).CompareTo(_world.Citizens.Rows.IdAt(r));
    }
    private ulong Offer(int clinic, int home, int citizen, Ticks tick)
    {
        int capacity = TreatmentPlaces(clinic);
        if (capacity <= 0) { return ulong.MaxValue; }
        TravelTime travel = Cost(home, clinic, citizen);
        if (travel.IsImpassable || !_world.Rules.Trips.WithinBudget(travel)) { return ulong.MaxValue; }
        ulong unit = Rules.VisitTicks;
        ulong earliest = tick.Raw + travel.ToTicksFloor().Raw + 1;
        ulong first = (ulong)IntegerMath.CeilDiv((long)earliest, (long)unit) * unit;
        ulong end = tick.Raw + (ulong)Rules.BookingDays * Ticks.PerDay;
        int calendar = Calendar(clinic);
        WeeklyHours hours = _world.Rules.Kind(_world.Buildings.Kind[clinic]).CareHours;
        ulong origin = (ulong)IntegerMath.FloorDiv((long)tick.Raw, (long)unit);
        for (ulong at = first; at + unit <= end; at += unit)
        {
            int index = (int)((ulong)IntegerMath.FloorDiv((long)at, (long)unit) - origin);
            if (index >= CalendarLength) { break; }
            if (_calendarSlots[calendar + index] < capacity && hours.IsOpen(new Ticks(at)) && hours.IsOpen(new Ticks(at + unit - 1))) { return at; }
        }
        return ulong.MaxValue;
    }
    private int CalendarLength => (int)IntegerMath.CeilDiv((long)Rules.BookingDays * Ticks.PerDay, (long)Rules.VisitTicks) + 2;
    private int Calendar(int clinic)
    {
        if (!_calendar.TryGetValue(clinic, out int offset))
        {
            offset = _calendarUsed; _calendarUsed += CalendarLength;
            if (_calendarSlots.Length < _calendarUsed)
                Array.Resize(ref _calendarSlots, _calendarUsed * 2);
            _calendarSlots.AsSpan(offset, CalendarLength).Clear();
            _calendar[clinic] = offset;
        }
        return offset;
    }
    private void Reserve(int clinic, ulong from, ulong until, Ticks tick)
    {
        int offset = Calendar(clinic);
        long unit = (long)Rules.VisitTicks;
        long origin = IntegerMath.FloorDiv((long)tick.Raw, unit);
        int first = (int)(IntegerMath.FloorDiv((long)from, unit) - origin);
        int last = (int)(IntegerMath.CeilDiv((long)until, unit) - origin);
        if (first < 0) { first = 0; }
        if (last > CalendarLength) { last = CalendarLength; }
        for (int i = first; i < last; i++) { _calendarSlots[offset + i]++; }
    }
    private void Discover(int family, int home, int citizen, Ticks tick)
    {
        int count = 0; int previous = -1;
        KnownClinicTable known = _world.KnownClinics;
        for (int p = Families.KnownHead[family] - 1; p >= 0;)
        {
            int next = known.Next[p] - 1;
            bool reachable = _world.Buildings.Rows.TryResolve(known.Building[p], out int b) && Usable(b, Need.Health);
            if (reachable)
            {
                TravelTime cost = Cost(home, b, citizen);
                reachable = !cost.IsImpassable && _world.Rules.Trips.WithinBudget(cost);
            }
            if (!reachable || count >= Rules.KnownClinics)
            {
                if (previous < 0) { Families.KnownHead[family] = next + 1; } else { known.Next[previous] = next + 1; }
                known.Rows.Free(known.Rows.At(p));
            }
            else { count++; previous = p; }
            p = next;
        }
        if (!_world.Buildings.Rows.TryResolve(Families.Habitual[family], out int habitual) || !Usable(habitual, Need.Health))
            Families.Habitual[family] = default;
        if (count >= Rules.KnownClinics) { return; }
        int total = _world.Buildings.Rows.SlotCount;
        if (total == 0) { return; }
        int offset = (int)(Randomness.Draw(_world.Key, Families.Rows.IdAt(family), tick, PurposeTag.ClinicDiscovery) % (ulong)total);
        for (int attempt = 0; attempt < total && attempt < Rules.SearchCandidates && count < Rules.KnownClinics; attempt++)
        {
            int b = (offset + attempt) % total;
            if (!Usable(b, Need.Health)) { continue; }
            bool exists = false;
            for (int p = Families.KnownHead[family] - 1; p >= 0; p = known.Next[p] - 1)
                if (known.Building[p] == _world.Buildings.Rows.At(b)) { exists = true; break; }
            if (exists) { continue; }
            TravelTime cost = Cost(home, b, citizen);
            if (cost.IsImpassable || !_world.Rules.Trips.WithinBudget(cost)) { continue; }
            int row = known.Rows.Resolve(known.Rows.Allocate());
            known.Building[row] = _world.Buildings.Rows.At(b);
            known.Next[row] = Families.KnownHead[family]; Families.KnownHead[family] = row + 1; count++;
        }
    }
    private void CleanFamilies()
    {
        bool retired = false;
        for (int f = 0; f < Families.Rows.SlotCount; f++)
        {
            if (!Families.Rows.IsLive(f) || _world.Households.Rows.TryResolve(Families.Household[f], out _)) { continue; }
            for (int p = Families.KnownHead[f] - 1; p >= 0;)
            {
                int next = _world.KnownClinics.Next[p] - 1;
                _world.KnownClinics.Rows.Free(_world.KnownClinics.Rows.At(p)); p = next;
            }
            Families.Rows.Free(Families.Rows.At(f)); retired = true;
        }
        if (retired)
        {
            _families.Clear();
            for (int f = 0; f < Families.Rows.SlotCount; f++)
                if (Families.Rows.IsLive(f) && _world.Households.Rows.TryResolve(Families.Household[f], out int h))
                    _families[_world.Households.Rows.IdAt(h)] = f;
        }
    }
    private void MissWork(int row, int citizen, Ticks tick)
    {
        if (!WorkSchedule.OnDuty(_world, citizen, tick)) { return; }
        CitizenActivity activity = (CitizenActivity)_world.Citizens.Activity[citizen];
        if (activity == CitizenActivity.AtWork && !TooIllToWork(_world, citizen)) { return; }
        if (!TooIllToWork(_world, citizen) && Stage(row) is not (VisitStage.Consultation or VisitStage.ClinicOutbound
            or VisitStage.ConsultationQueue or VisitStage.HospitalOutbound or VisitStage.Inpatient or VisitStage.AwaitingBed or VisitStage.Returning)) { return; }
        if (State.MissedTicks[row] < long.MaxValue) { State.MissedTicks[row]++; }
        int stats = DailyStats(tick);
        _world.CareDays.MissedTicks[stats] = Add(_world.CareDays.MissedTicks[stats], 1);
        if (_world.Businesses.Rows.TryResolve(_world.Citizens.Workplace[citizen], out int work))
        {
            BusinessKindDefinition trade = _world.Rules.BusinessKind(_world.Businesses.Kind[work]);
            long length = (long)_world.Rules.Jobs.ShiftLengthOf(_world.Key, _world.Citizens.Rows.IdAt(citizen)).Raw;
            if (length > 0)
            {
                long scaled = State.LostRemainder[row] + trade.WagePerDay;
                long lost = IntegerMath.FloorDiv(scaled, length);
                State.LostRemainder[row] = scaled % length;
                State.LostWages[row] = Add(State.LostWages[row], lost);
                _world.CareDays.LostWages[stats] = Add(_world.CareDays.LostWages[stats], lost);
            }
        }
        long day = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
        if (State.LastMissedDay[row] != day)
        { State.LastMissedDay[row] = day; Trace(row, CareEventKind.MissedWork, tick, State.LostWages[row]); }
    }
    private void Trace(int row, CareEventKind kind, Ticks tick, long value = 0)
    {
        EventObserved?.Invoke(kind);
        State.Reason[row] = (byte)kind;
        if (Rules.Runs && kind is CareEventKind.DiedAwaitingBed or CareEventKind.Died)
        {
            int stats = DailyStats(tick);
            _world.CareDays.Deaths[stats]++;
            if (kind == CareEventKind.DiedAwaitingBed) { _world.CareDays.DeathsAwaitingBed[stats]++; }
        }
        CareHistoryTable history = _world.CareHistory;
        int cap = Rules.Runs ? Rules.HistoryKeeps : _world.Rules.School.HistoryKeeps;
        if (cap <= 0) { return; }
        while (history.Rows.LiveCount >= cap)
        {
            int oldest = -1;
            for (int i = 0; i < history.Rows.SlotCount; i++)
                if (history.Rows.IsLive(i) && (oldest < 0 || history.Rows.IdAt(i) < history.Rows.IdAt(oldest))) { oldest = i; }
            if (oldest < 0) { break; }
            history.Rows.Free(history.Rows.At(oldest));
        }
        int e = history.Rows.Resolve(history.Rows.Allocate());
        history.Tick[e] = tick; history.Event[e] = (byte)kind; history.Value[e] = value;
        if (_world.Citizens.Rows.TryResolve(State.Citizen[row], out int c))
        { history.CitizenId[e] = _world.Citizens.Rows.IdAt(c); history.Severity[e] = _world.Citizens.IllnessSeverity[c]; }
        if (_world.Households.Rows.TryResolve(State.Household[row], out int h)) { history.HouseholdId[e] = _world.Households.Rows.IdAt(h); }
        if (_world.Buildings.Rows.TryResolve(State.Provider[row], out int b)) { history.BuildingId[e] = _world.Buildings.Rows.IdAt(b); }
    }
    public CareReading Read()
    {
        int sick = 0, serious = 0, waiting = 0, booked = 0, treating = 0, bedWait = 0, inpatient = 0, beds = 0, places = 0, deaths = 0;
        long missed = 0, lost = 0;
        for (int c = 0; c < _world.Citizens.Rows.SlotCount; c++)
        {
            if (!_world.Citizens.Rows.IsLive(c)) { continue; }
            int severity = _world.Citizens.IllnessSeverity[c];
            if (severity > 0) { sick++; }
            if (Rules.Runs && severity >= Rules.SeriousSeverity) { serious++; }
        }
        for (int r = 0; r < State.Rows.SlotCount; r++)
        {
            if (!State.Rows.IsLive(r)) { continue; }
            switch (Stage(r))
            {
                case VisitStage.Waiting: waiting++; break;
                case VisitStage.Booked: booked++; break;
                case VisitStage.Consultation: case VisitStage.ConsultationQueue: treating++; break;
                case VisitStage.AwaitingBed: bedWait++; break;
                case VisitStage.Inpatient: inpatient++; break;
            }

        }
        for (int b = 0; b < _world.Buildings.Rows.SlotCount; b++)
        { if (_world.Buildings.Rows.IsLive(b)) { beds += Beds(b); places += TreatmentPlaces(b); } }
        for (int d = 0; d < _world.CareDays.Rows.SlotCount; d++)
            if (_world.CareDays.Rows.IsLive(d))
            {
                deaths += _world.CareDays.DeathsAwaitingBed[d];
                missed = Add(missed, _world.CareDays.MissedTicks[d]); lost = Add(lost, _world.CareDays.LostWages[d]);
            }
        return new(sick, serious, waiting, booked, treating, bedWait, inpatient, beds, places, deaths, missed, lost);
    }
    private int DailyStats(Ticks tick)
    {
        long day = IntegerMath.FloorDiv((long)tick.Raw, Ticks.PerDay);
        if (_statsDay == day && _statsWindow == Rules.ReportDays && _world.CareDays.Rows.IsLive(_statsRow)) { return _statsRow; }
        _statsDay = day; _statsRow = -1; _statsWindow = Rules.ReportDays;
        var stats = _world.CareDays;
        for (int r = 0; r < stats.Rows.SlotCount; r++)
        {
            if (!stats.Rows.IsLive(r)) { continue; }
            if (stats.Day[r] <= day - Rules.ReportDays) { stats.Rows.Free(stats.Rows.At(r)); }
            else if (stats.Day[r] == day) { _statsRow = r; }
        }
        if (_statsRow < 0)
        { _statsRow = stats.Rows.Resolve(stats.Rows.Allocate()); stats.Day[_statsRow] = day; }
        return _statsRow;
    }
    private static long Add(long a, long b) => b > long.MaxValue - a ? long.MaxValue : a + b;
    private static ulong Max(ulong a, ulong b) => a > b ? a : b;
}
