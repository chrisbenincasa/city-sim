using Borough.Core.Quantities;
namespace Borough.Core.Rules;

public readonly record struct SchoolRuleset(int Days, int BellEarliest, int BellLatest,
    int DismissEarliest, int DismissLatest, int RetryTicks, int HistoryKeeps)
{
    public bool Runs => Days != 0;
}

public readonly record struct CareRuleset(int Interval, int RoutineDays, int IllnessPerThousand,
    int HealthRiskPerThousand, int InitialSeverity, int SeriousSeverity, int AdmissionSeverity,
    int RecoveryPerDay, int TreatedRecoveryPerDay, int DeteriorationPerDay, int DeteriorationPercent,
    int DeathSeverity, int DeathPerThousand, int VisitMinutes, int WaitDays, int UrgentWaitDays,
    int PriorityEveryDays, int SwitchAfterWaits, int KnownClinics, int SearchCandidates,
    int HistoryKeeps, int BookingDays, int FloorTilesPerTreatment, int FloorTilesPerBed, int ReportDays)
{
    public bool Runs => Interval > 0;
    public ulong VisitTicks => (ulong)Ticks.AtMinute(VisitMinutes);
}
