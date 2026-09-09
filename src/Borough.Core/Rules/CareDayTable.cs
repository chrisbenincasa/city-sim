using Borough.Core.Tables;
namespace Borough.Core.Rules;

public readonly struct CareDay;
[Table]
public sealed class CareDayTable
{
    public CareDayTable()
    {
        Rows = new Rows<CareDay>("care_day", 32, Buffering.OneCopy);
        Day = Rows.Saved<long>("day");
        DeathsAwaitingBed = Rows.Saved<int>("deaths_awaiting_bed");
        Deaths = Rows.Saved<int>("deaths");
        MissedTicks = Rows.Saved<long>("missed_ticks");
        LostWages = Rows.Saved<long>("lost_wages");
        Rows.Seal();
    }
    public Rows<CareDay> Rows { get; }
    public Column<long> Day { get; }
    public Column<int> DeathsAwaitingBed { get; }
    public Column<int> Deaths { get; }
    public Column<long> MissedTicks { get; }
    public Column<long> LostWages { get; }
}
