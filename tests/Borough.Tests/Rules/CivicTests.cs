using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Movement;
using Borough.Core.Persistence;
using Borough.Core.Quantities;
using Borough.Core.Rules;
using Borough.Formats;
using Borough.Tests.Persistence;
namespace Borough.Tests.Rules;

public sealed class CivicTests
{
    internal static string Content => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "care.toml"));
    internal static (World World, Simulation Sim) Start(string? text = null)
    {
        var loaded = RulesetLoader.Parse(text ?? Content, "care-test.toml");
        Assert.True(loaded.Ok, loaded.Describe());
        var key = WorldKey.FromSeed(0);
        var world = new World(400, loaded.Ruleset!, key);
        var sim = new Simulation(world, key);
        SyntheticCity.PopulateInto(world, key, Ticks.Zero);
        return (world, sim);
    }
    internal static int Place(World world, byte kind)
    {
        for (int lot = 0; lot < world.Lots.Rows.SlotCount; lot++)
            if (world.Lots.Rows.IsLive(lot) && world.Lots.IsVacant(lot))
                return world.Buildings.Rows.Resolve(world.CreateBuilding(world.Lots.Rows.At(lot), kind, world.Tick, world.Key));
        throw new InvalidOperationException("No vacant Lot");
    }
    private static int FirstCitizen(World w) => Enumerable.Range(0, w.Citizens.Rows.SlotCount).First(w.Citizens.Rows.IsLive);
    private static int Home(World w, int c) => w.Buildings.Rows.Resolve(w.Households.Dwelling[w.Households.Rows.Resolve(w.Citizens.HouseholdOf[c])]);
    private static VisitStage Stage(World w, int r) => (VisitStage)w.Civic.Stage[r];
    private static void Step(Simulation sim, int n) { for (int i = 0; i < n; i++) { sim.Step(default); } }

    [Fact]
    public void School_has_a_stay_a_return_and_no_weekend_departures()
    {
        var (w, sim) = Start(Content.Replace("illness_per_thousand = 30", "illness_per_thousand = 0"));
        int school = Place(w, 2);
        int adult = FirstCitizen(w);
        int child = w.Citizens.Rows.Resolve(w.Bear(w.Citizens.HouseholdOf[adult]));
        bool stayed = false, returned = false;
        for (int i = 0; i < Ticks.PerDay; i++)
        {
            sim.Step(default);
            var activity = (CitizenActivity)w.Citizens.Activity[child];
            stayed |= activity == CitizenActivity.AtSchool;
            returned |= stayed && activity == CitizenActivity.AtHome;
        }
        Assert.True(stayed); Assert.True(returned);
        int row = sim.Civic.RowOf(child);
        Assert.False(sim.Civic.ScheduleSchool(child, Home(w, child), school,
            new Ticks(5UL * Ticks.PerDay), 5));
        sim.CheckEndOfRun();
    }
    [Fact]
    public void Booking_does_not_treat_but_the_completed_visit_does()
    {
        var (w, sim) = Start(Content.Replace("illness_per_thousand = 30", "illness_per_thousand = 0"));
        Place(w, 3);
        int citizen = FirstCitizen(w); w.Citizens.IllnessSeverity[citizen] = 20;
        sim.Civic.Step(Ticks.Zero);
        int row = sim.Civic.RowOf(citizen);
        Assert.Equal(20, w.Citizens.IllnessSeverity[citizen]);
        Assert.True(Stage(w, row) is VisitStage.Waiting or VisitStage.Booked);
        bool treated = false;
        for (int t = 0; t < Ticks.PerDay * 2; t++)
        {
            sim.Step(default);
            treated |= w.Civic.TreatedUntil[row].Raw > 0;
            if (treated && (CitizenActivity)w.Citizens.Activity[citizen] == CitizenActivity.AtHome) { break; }
        }
        Assert.True(treated); Assert.True(w.Citizens.IllnessSeverity[citizen] < 20);
        Assert.True(w.FamilyCare.Rows.LiveCount > 0);
        Assert.Contains(Enumerable.Range(0, w.FamilyCare.Rows.SlotCount), f => w.FamilyCare.Rows.IsLive(f) && !w.FamilyCare.Habitual[f].IsNone);
        sim.CheckEndOfRun();
    }
    [Fact]
    public void Clinic_space_and_overnight_beds_are_separate_and_beds_stay_occupied()
    {
        var (w, sim) = Start(); int clinic = Place(w, 3), hospital = Place(w, 4);
        Assert.Equal(0, sim.Civic.Beds(clinic)); Assert.True(sim.Civic.TreatmentPlaces(clinic) > 0);
        Assert.True(sim.Civic.Beds(hospital) > 0); Assert.True(sim.Civic.TreatmentPlaces(hospital) > 0);
        int c = FirstCitizen(w); sim.Civic.Step(Ticks.Zero); int row = sim.Civic.RowOf(c);
        w.Citizens.IllnessSeverity[c] = 85;
        w.Civic.Provider[row] = w.Buildings.Rows.At(hospital); w.Civic.Place[row] = w.Buildings.Rows.At(hospital);
        w.Civic.Stage[row] = (byte)VisitStage.Consultation; w.Civic.EndsAt[row] = Ticks.Zero;
        w.Citizens.Activity[c] = (byte)CitizenActivity.InTreatment;
        sim.Civic.Step(Ticks.Zero);
        Assert.Equal(VisitStage.Inpatient, Stage(w, row));
        Assert.Equal(1, sim.Civic.Occupied(hospital, VisitStage.Inpatient));
        Assert.Equal((byte)CitizenActivity.InHospital, w.Citizens.Activity[c]);
        w.Civic.ProgressAt[row] = new Ticks(Ticks.PerDay * 2);
        Step(sim, Ticks.PerDay);
        Assert.Equal(VisitStage.Inpatient, Stage(w, row));
        w.Citizens.IllnessSeverity[c] = 10;
        Step(sim, 512);
        Assert.NotEqual(VisitStage.Inpatient, Stage(w, row));
        Assert.Equal(0, sim.Civic.Occupied(hospital, VisitStage.Inpatient));
    }
    [Fact]
    public void Critical_unmet_admission_can_kill_and_leaves_a_trace_after_the_citizen_is_freed()
    {
        string text = Content.Replace("death_per_thousand = 10", "death_per_thousand = 1000")
            .Replace("deterioration_percent = 35", "deterioration_percent = 100");
        var (w, sim) = Start(text); int c = FirstCitizen(w); ulong id = w.Citizens.Rows.IdAt(c);
        sim.Civic.Step(Ticks.Zero); int row = sim.Civic.RowOf(c);
        w.Citizens.IllnessSeverity[c] = w.Rules.Care.DeathSeverity;
        w.Civic.Stage[row] = (byte)VisitStage.AwaitingBed; w.Civic.AdmissionWanted[row] = 1;
        w.Civic.IllSince[row] = new Ticks(1); w.Civic.ProgressAt[row] = new Ticks(1);
        sim.Step(default); sim.Step(default);
        Assert.False(w.Citizens.Rows.IsLive(c));
        Assert.Contains(Enumerable.Range(0, w.CareHistory.Rows.SlotCount), e => w.CareHistory.Rows.IsLive(e)
            && w.CareHistory.CitizenId[e] == id && (CareEventKind)w.CareHistory.Event[e] == CareEventKind.DiedAwaitingBed);
        Assert.Equal(1, sim.Civic.Read().DeathsAwaitingBed);
        sim.CheckEndOfRun();
    }
    [Fact]
    public void Care_and_school_state_round_trip_while_people_are_away()
    {
        var (w, sim) = Start(); Place(w, 2); Place(w, 3); Place(w, 4);
        int c = FirstCitizen(w); w.Bear(w.Citizens.HouseholdOf[c]); w.Citizens.IllnessSeverity[c] = 45;
        Step(sim, 400);
        Assert.True(w.Civic.Rows.LiveCount > 0);
        var file = new MemorySave(); SaveFile.Write(w, 1, file);
        World copy = SaveFile.Read(file, w.Rules, out var header);
        var resumed = new Simulation(copy, header.Key) { VerifyDecideWritesNothing = true };
        sim.VerifyDecideWritesNothing = true;
        Assert.Equal(w.HashState(), copy.HashState());
        for (int t = 0; t < 512; t++)
        { sim.Step(default); resumed.Step(default); Assert.Equal(w.HashState(), copy.HashState()); }
    }
    private static void Quiet(World w, Simulation sim)
    {
        sim.Civic.Step(Ticks.Zero);
        for (int r = 0; r < w.Civic.Rows.SlotCount; r++)
        {
            if (!w.Civic.Rows.IsLive(r)) continue;
            w.Civic.Stage[r] = 0;
            w.Civic.RoutineAt[r] = new Ticks(100UL * Ticks.PerDay);
            w.Civic.ProgressAt[r] = new Ticks(100UL * Ticks.PerDay);
        }
    }
    [Fact]
    public void Urgent_requests_postpone_unstarted_bookings_and_keep_original_wait()
    {
        var (w, sim) = Start(); int clinic = Place(w, 3); Quiet(w, sim);
        int capacity = sim.Civic.TreatmentPlaces(clinic);
        int[] people = Enumerable.Range(0, w.Citizens.Rows.SlotCount).Where(w.Citizens.Rows.IsLive).Take(capacity + 1).ToArray();
        foreach (int c in people.Take(capacity)) sim.Civic.Request(sim.Civic.RowOf(c), Ticks.Zero);
        sim.Civic.Step(Ticks.Zero);
        ulong first = people.Take(capacity).Min(c => w.Civic.NextAt[sim.Civic.RowOf(c)].Raw);
        int urgent = people[^1]; w.Citizens.IllnessSeverity[urgent] = 80;
        sim.Civic.Request(sim.Civic.RowOf(urgent), new Ticks(1));
        sim.Civic.Step(new Ticks(32));
        Assert.Equal(first, w.Civic.NextAt[sim.Civic.RowOf(urgent)].Raw);
        Assert.Contains(people.Take(capacity), c => w.Civic.NextAt[sim.Civic.RowOf(c)].Raw > first);
        foreach (int c in people.Take(capacity)) Assert.Equal(1UL, w.Civic.RequestedAt[sim.Civic.RowOf(c)].Raw);
        Assert.Contains(Enumerable.Range(0, w.CareHistory.Rows.SlotCount), e => w.CareHistory.Rows.IsLive(e)
            && (CareEventKind)w.CareHistory.Event[e] == CareEventKind.Postponed);
    }
    [Theory]
    [InlineData(VisitStage.ClinicOutbound)]
    [InlineData(VisitStage.Consultation)]
    public void Triage_preserves_people_already_travelling_or_in_treatment(VisitStage stage)
    {
        var (w, sim) = Start(); int clinic = Place(w, 3); Quiet(w, sim);
        int[] people = Enumerable.Range(0, w.Citizens.Rows.SlotCount).Where(w.Citizens.Rows.IsLive).Take(2).ToArray();
        int r = sim.Civic.RowOf(people[0]);
        w.Civic.Stage[r] = (byte)stage; w.Civic.Provider[r] = w.Buildings.Rows.At(clinic);
        w.Civic.NextAt[r] = new Ticks(300); w.Civic.EndsAt[r] = new Ticks(400);
        w.Citizens.Activity[people[0]] = (byte)(stage == VisitStage.ClinicOutbound ? CitizenActivity.ServiceTravelling : CitizenActivity.InTreatment);
        w.Citizens.IllnessSeverity[people[1]] = 80; sim.Civic.Request(sim.Civic.RowOf(people[1]), Ticks.Zero);
        sim.Civic.Step(new Ticks(32));
        Assert.Equal(stage, Stage(w, r)); Assert.Equal(300UL, w.Civic.NextAt[r].Raw); Assert.Equal(400UL, w.Civic.EndsAt[r].Raw);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void An_overloaded_family_doctor_allows_fallback_and_repeated_waits_can_change_the_habit(bool repeated)
    {
        var (w, sim) = Start(); int usual = Place(w, 3), alternative = Place(w, 3); Quiet(w, sim);
        int c = FirstCitizen(w), r = sim.Civic.RowOf(c);
        sim.Civic.Request(r, Ticks.Zero); sim.Civic.Step(Ticks.Zero);
        int f = Enumerable.Range(0, w.FamilyCare.Rows.SlotCount).Single(f => w.FamilyCare.Rows.IsLive(f)
            && w.FamilyCare.Household[f] == w.Citizens.HouseholdOf[c]);
        w.FamilyCare.Habitual[f] = w.Buildings.Rows.At(usual);
        int capacity = sim.Civic.TreatmentPlaces(usual);
        foreach (int other in Enumerable.Range(0, w.Citizens.Rows.SlotCount).Where(x => w.Citizens.Rows.IsLive(x) && x != c).Take(capacity))
        {
            int occupied = sim.Civic.RowOf(other); w.Civic.Stage[occupied] = (byte)VisitStage.Consultation;
            w.Civic.Provider[occupied] = w.Buildings.Rows.At(usual);
            w.Civic.EndsAt[occupied] = new Ticks(10UL * Ticks.PerDay);
            w.Citizens.Activity[other] = (byte)CitizenActivity.InTreatment;
        }
        sim.Civic.Step(new Ticks(32));
        Assert.Equal(w.Buildings.Rows.At(alternative), w.Civic.Provider[r]);
        Assert.Equal(w.Buildings.Rows.At(usual), w.FamilyCare.Habitual[f]);
        if (repeated)
        {
            for (int day = 1; day < w.Rules.Care.SwitchAfterWaits; day++)
            {
                w.Civic.Stage[r] = (byte)VisitStage.Waiting;
                w.Citizens.Activity[c] = (byte)CitizenActivity.AtHome;
                sim.Civic.Step(new Ticks((ulong)day * Ticks.PerDay + 32));
            }
        }
        w.Civic.Stage[r] = (byte)VisitStage.Consultation; w.Civic.Place[r] = w.Civic.Provider[r];
        w.Civic.EndsAt[r] = new Ticks(33); w.Citizens.Activity[c] = (byte)CitizenActivity.InTreatment;
        sim.Civic.Step(new Ticks(33));
        Assert.Equal(w.Buildings.Rows.At(repeated ? alternative : usual), w.FamilyCare.Habitual[f]);
        Assert.Equal(repeated ? 0 : 1, w.FamilyCare.ExcessWaits[f]);
    }
    [Fact]
    public void Every_child_in_a_household_can_attend_school()
    {
        var (w, sim) = Start(); Place(w, 2);
        int adult = FirstCitizen(w);
        int[] children = Enumerable.Range(0, 2).Select(_ => w.Citizens.Rows.Resolve(w.Bear(w.Citizens.HouseholdOf[adult]))).ToArray();
        var attended = new bool[children.Length];
        for (int t = 0; t < Ticks.PerDay; t++)
        {
            sim.Step(default);
            for (int i = 0; i < children.Length; i++) attended[i] |= (CitizenActivity)w.Citizens.Activity[children[i]] == CitizenActivity.AtSchool;
        }
        Assert.All(attended, Assert.True); sim.CheckEndOfRun();
    }
    [Fact]
    public void Report_window_survives_trace_eviction_and_expires_old_outcomes()
    {
        string text = Content.Replace("death_per_thousand = 10", "death_per_thousand = 1000")
            .Replace("deterioration_percent = 35", "deterioration_percent = 100");
        var (w, sim) = Start(text); Quiet(w, sim);
        int c = FirstCitizen(w), r = sim.Civic.RowOf(c);
        w.Citizens.IllnessSeverity[c] = w.Rules.Care.DeathSeverity;
        w.Civic.Stage[r] = (byte)VisitStage.AwaitingBed; w.Civic.AdmissionWanted[r] = 1;
        w.Civic.ProgressAt[r] = new Ticks(1); w.Civic.IllSince[r] = new Ticks(1);
        sim.Civic.Step(new Ticks(1));
        for (int e = 0; e < w.CareHistory.Rows.SlotCount; e++)
            if (w.CareHistory.Rows.IsLive(e)) w.CareHistory.Rows.Free(w.CareHistory.Rows.At(e));
        Assert.Equal(1, sim.Civic.Read().DeathsAwaitingBed);
        sim.Civic.Step(new Ticks((ulong)w.Rules.Care.ReportDays * Ticks.PerDay));
        Assert.Equal(0, sim.Civic.Read().DeathsAwaitingBed);
        Assert.True(w.CareDays.Rows.LiveCount <= w.Rules.Care.ReportDays);
    }
    [Fact]
    public void Serious_illness_loses_attendance_and_earnings()
    {
        var (w, sim) = Start(); Step(sim, 128);
        int c = Enumerable.Range(0, w.Citizens.Rows.SlotCount).First(c => w.Citizens.Rows.IsLive(c)
            && w.Businesses.Rows.IsValid(w.Citizens.Workplace[c]));
        int r = sim.Civic.RowOf(c);
        w.Citizens.IllnessSeverity[c] = 70; w.Civic.ProgressAt[r] = new Ticks(100UL * Ticks.PerDay);
        Step(sim, Ticks.PerDay);
        Assert.True(w.Civic.MissedTicks[r] > 0); Assert.True(w.Civic.LostWages[r] > 0);
        Assert.True(sim.Civic.Read().LostWages >= w.Civic.LostWages[r]);
        Assert.NotEqual((byte)CitizenActivity.AtWork, w.Citizens.Activity[c]);
        sim.CheckEndOfRun();
    }
    [Fact]
    public void Beds_cannot_be_double_booked_and_inpatients_survive_save_reload()
    {
        var (w, sim) = Start(); int hospital = Place(w, 4); Quiet(w, sim);
        int beds = sim.Civic.Beds(hospital);
        int[] patients = Enumerable.Range(0, w.Citizens.Rows.SlotCount).Where(w.Citizens.Rows.IsLive).Take(beds + 2).ToArray();
        foreach (int c in patients)
        {
            int r = sim.Civic.RowOf(c); w.Citizens.IllnessSeverity[c] = 85;
            w.Civic.Stage[r] = (byte)VisitStage.Consultation;
            w.Civic.Provider[r] = w.Civic.Place[r] = w.Buildings.Rows.At(hospital);
            w.Civic.EndsAt[r] = Ticks.Zero; w.Citizens.Activity[c] = (byte)CitizenActivity.InTreatment;
        }
        sim.Step(default);
        Assert.Equal(beds, sim.Civic.Occupied(hospital, VisitStage.Inpatient));
        Assert.Contains(patients, c => w.Civic.AdmissionWanted[sim.Civic.RowOf(c)] != 0
            && Stage(w, sim.Civic.RowOf(c)) != VisitStage.Inpatient);
        var save = new MemorySave(); SaveFile.Write(w, 1, save);
        World copy = SaveFile.Read(save, w.Rules, out var header); var resumed = new Simulation(copy, header.Key);
        for (int t = 0; t < 128; t++)
        {
            sim.Step(default); resumed.Step(default); Assert.Equal(w.HashState(), copy.HashState());
            Assert.True(sim.Civic.Occupied(hospital, VisitStage.Inpatient) + sim.Civic.Occupied(hospital, VisitStage.HospitalOutbound) <= beds);
        }
    }
    [Fact]
    public void Mild_illness_can_recover_without_a_clinic()
    {
        var (w, sim) = Start(Content.Replace("deterioration_percent = 35", "deterioration_percent = 0"));
        Quiet(w, sim); int c = FirstCitizen(w), r = sim.Civic.RowOf(c);
        w.Citizens.IllnessSeverity[c] = 4; w.Civic.ProgressAt[r] = new Ticks(1);
        sim.Civic.Step(new Ticks(1));
        Assert.Equal(0, w.Citizens.IllnessSeverity[c]); Assert.Equal(0, sim.Civic.Read().Beds);
        Assert.Contains(Enumerable.Range(0, w.CareHistory.Rows.SlotCount), e => w.CareHistory.Rows.IsLive(e)
            && (CareEventKind)w.CareHistory.Event[e] == CareEventKind.Recovered);
    }
    [Theory]
    [InlineData("serious_severity = 40", "serious_severity = 10")]
    [InlineData("visit_minutes = 30", "visit_minutes = 0")]
    [InlineData("bed_percent = 60", "bed_percent = 101")]
    [InlineData("bell_latest_minute = 540", "bell_latest_minute = 400")]
    public void Invalid_civic_rules_are_refused(string before, string after)
    { Assert.False(RulesetLoader.Parse(Content.Replace(before, after), "bad.toml").Ok); }
}
