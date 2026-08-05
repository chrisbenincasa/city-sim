using System.Globalization;
using Borough.Core.Entities;
using Borough.Core.Quantities;
using Borough.Core.Tables;

namespace Borough.Headless;

/// <summary>
/// Builds a synthetic city, prints what is in it, and prints its State Hash.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is slice 4's "something to look at", and it is kept rather than superseded.</b> Slice 5's
/// hash trace is a better artefact for catching a change, but it cannot show what is <em>in</em> a
/// city — and before slice 7 the only verb a session applies is Zone, so a report printed at the end
/// of a replay would show a handful of Lots and three empty tables. The two modes answer different
/// questions and neither degrades into the other.
/// </para>
/// <para>
/// <b>The core hands this program column names, counts and widths</b>; the layout, the alignment and
/// the units are decided here. <c>adr/0002</c>: the shell owns every string a human reads.
/// </para>
/// </remarks>
internal static class Report
{
    /// <summary>Households per Building in the synthetic city. Provisional; the corpus states none.</summary>
    private const int HouseholdsPerBuilding = 3;

    public static int Print(int population)
    {
        World world = Populate(population);

        Write($"Borough — table report at {population:N0} Citizens");
        Console.WriteLine();

        WriteTables(world);
        Console.WriteLine();
        WriteFootprint(world);
        Console.WriteLine();

        Write($"State Hash  0x{world.HashState():X16}");

        return 0;
    }

    /// <summary>
    /// A city built by construction rather than by rule, because there are no Rules yet.
    /// </summary>
    /// <remarks>
    /// The ratios are S4 task 2's, stated per 1,000 Citizens so they stay correct at any population:
    /// 360 Households, ~150 Buildings, ~225 Lots. Sizing is a derivation, not a constant.
    /// </remarks>
    private static World Populate(int population)
    {
        var world = new World(population);

        int households = (population * 360) / 1_000;
        int buildings = (households / HouseholdsPerBuilding) + 1;

        var dwellings = new Handle<Building>[buildings];

        for (int i = 0; i < buildings; i++)
        {
            Handle<Lot> lot = world.Lots.Create(new Tiles(i % 64), new Tiles(i / 64), zone: 1);
            dwellings[i] = world.Buildings.Create(lot, kind: 1);
        }

        var homes = new Handle<Household>[households];

        for (int i = 0; i < households; i++)
        {
            homes[i] = world.CreateHousehold(dwellings[i % buildings], lifeStage: (byte)(i % 5));
        }

        for (int i = 0; i < population; i++)
        {
            world.CreateCitizen(homes[i % households], new Ticks((ulong)i % 8192));
        }

        return world;
    }

    private static void WriteTables(World world)
    {
        Console.WriteLine("table       buffering     rows  saved  derived   B/row");
        Console.WriteLine("-----------------------------------------------------------");

        foreach (Rows table in world.Tables)
        {
            int saved = 0;
            int derived = 0;

            foreach (Column column in table.Columns)
            {
                if (column.Disposition == Disposition.Saved)
                {
                    saved++;
                }
                else
                {
                    derived++;
                }
            }

            int bytes = table.BytesPerRow(Touch.PerTick)
                      + table.BytesPerRow(Touch.Wake)
                      + table.BytesPerRow(Touch.Cold);

            string counts = F($"{table.LiveCount,7:N0}  {saved,5}  {derived,7}  {bytes,6}");
            Write($"{table.Name,-10}  {Describe(table.Buffering),-9}  {counts}");
        }
    }

    /// <summary>
    /// The footprint, split the way K0 split it, because that is the comparison worth making.
    /// </summary>
    /// <remarks>
    /// S4 task 2 found that <em>hot</em> had never been defined and that the two available readings
    /// are 4× apart. The per-Tick figure sizes the Event Wheel drain and the wake gather; the working
    /// set sizes the world and the save copy. Reporting one number would lose whichever question is
    /// being asked.
    /// </remarks>
    private static void WriteFootprint(World world)
    {
        Console.WriteLine("table           per-tick          wake          cold         total");
        Console.WriteLine("---------------------------------------------------------------------");

        long perTick = 0;
        long wake = 0;
        long cold = 0;

        foreach (Rows table in world.Tables)
        {
            long rows = table.LiveCount;
            long tablePerTick = rows * table.BytesPerRow(Touch.PerTick);
            long tableWake = rows * table.BytesPerRow(Touch.Wake);
            long tableCold = rows * table.BytesPerRow(Touch.Cold);

            perTick += tablePerTick;
            wake += tableWake;
            cold += tableCold;

            long tableTotal = tablePerTick + tableWake + tableCold;
            string tail = F($"{Kibibytes(tableCold),12}  {Kibibytes(tableTotal),12}");
            Write($"{table.Name,-10}  {Kibibytes(tablePerTick),12}  {Kibibytes(tableWake),12}  {tail}");
        }

        string totals = F($"{Kibibytes(cold),12}  {Kibibytes(perTick + wake + cold),12}");
        Write($"{"total",-10}  {Kibibytes(perTick),12}  {Kibibytes(wake),12}  {totals}");
    }

    /// <summary>KiB to one decimal place, in integers. There is no float anywhere in this project.</summary>
    private static string Kibibytes(long bytes)
    {
        long tenths = ((bytes * 10) + 512) / 1024;
        return string.Create(CultureInfo.InvariantCulture, $"{tenths / 10:N0}.{tenths % 10} KiB");
    }

    private static string Describe(Buffering buffering) =>
        buffering == Buffering.OneCopy ? "single" : "double";

    /// <summary>Formats with the invariant culture, which adr/0003 requires of every number here.</summary>
    private static void Write(FormattableString line) => Console.WriteLine(F(line));

    /// <inheritdoc cref="Write"/>
    private static string F(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);
}
