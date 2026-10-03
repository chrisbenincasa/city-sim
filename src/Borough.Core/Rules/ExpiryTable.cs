using System.Runtime.CompilerServices;
using Borough.Core.Tables;

namespace Borough.Core.Rules;

/// <summary>The age record of one Bin whose Resource spoils.</summary>
public readonly struct Expiry;

/// <summary>A Bin's stock split by age. Bucket 0 is the newest.</summary>
[InlineArray(ShelfLife.MaxCycles)]
public struct AgeBuckets
{
    private long _element;
}

/// <summary>
/// One row per Bin whose Resource declares a shelf life, holding that Bin's stock by age.
/// </summary>
/// <remarks>
/// <para>
/// The buckets always sum to the Bin's level. <see cref="World"/> keeps them in step on every level
/// write, so every reader of <see cref="BinTable.LevelAt"/> sees stock that is still good.
/// </para>
/// <para>
/// A Bin gains its row on its first deposit, so non-expiring Bins and never-filled ones cost nothing.
/// The row is freed with its Bin, or by the sweep once its Resource stops expiring.
/// </para>
/// </remarks>
[Table]
public sealed class ExpiryTable
{
    public ExpiryTable(BinTable bins)
    {
        ArgumentNullException.ThrowIfNull(bins);

        Rows = new Rows<Expiry>("expiry", 8);
        Bin = Rows.SavedHandle("bin", bins.Rows);
        Buckets = Rows.Saved<AgeBuckets>("buckets", Touch.PerTick);
        SpoilingDay = Rows.Saved<ushort>("spoiling_day", Touch.Cold);
        DaySpoiled = Rows.Saved<long>("day_spoiled", Touch.Cold);
        PriorSpoiled = Rows.Saved<long>("prior_spoiled", Touch.Cold);
        Rows.Seal();
    }

    public Rows<Expiry> Rows { get; }

    /// <summary>The Bin this row ages.</summary>
    public HandleColumn<Bin> Bin { get; }

    /// <summary>The Bin's stock by age, newest first.</summary>
    public Column<AgeBuckets> Buckets { get; }

    /// <summary>The Day <see cref="DaySpoiled"/> is counting for.</summary>
    public Column<ushort> SpoilingDay { get; }

    /// <summary>What spoiled from this Bin on <see cref="SpoilingDay"/>.</summary>
    public Column<long> DaySpoiled { get; }

    /// <summary>
    /// What spoiled over the whole Day before <see cref="SpoilingDay"/>, or zero when nothing
    /// spoiled that Day.
    /// </summary>
    public Column<long> PriorSpoiled { get; }

    /// <summary>What has spoiled from <paramref name="row"/> so far on <paramref name="today"/>.</summary>
    public long SpoiledOn(int row, ushort today) => SpoilingDay[row] == today ? DaySpoiled[row] : 0;

    /// <summary>What spoiled from <paramref name="row"/> over the whole Day before <paramref name="today"/>.</summary>
    public long PriorSpoiledOn(int row, ushort today)
    {
        int spoiling = SpoilingDay[row];

        if (spoiling == today)
        {
            return PriorSpoiled[row];
        }

        return spoiling == today - 1 ? DaySpoiled[row] : 0;
    }

    /// <summary>The row ageing <paramref name="binSlot"/>, or <see cref="Tables.Rows.NoSlot"/>.</summary>
    public int RowOf(BinTable bins, int binSlot)
    {
        int row = bins.ExpiryRow[binSlot] - 1;

        return row >= 0 && Rows.IsLive(row) ? row : Tables.Rows.NoSlot;
    }

    /// <summary>
    /// Allocates the row for <paramref name="binSlot"/>. Stock already standing counts as newest.
    /// </summary>
    internal int Open(BinTable bins, int binSlot)
    {
        int row = Rows.Resolve(Rows.Allocate());

        Bin[row] = bins.Rows.At(binSlot);
        Buckets[row] = default;
        Buckets[row][0] = bins.LevelAt(binSlot);
        SpoilingDay[row] = 0;
        DaySpoiled[row] = 0;
        PriorSpoiled[row] = 0;
        bins.ExpiryRow[binSlot] = row + 1;

        return row;
    }

    /// <summary>Frees the row ageing <paramref name="binSlot"/>, when it has one.</summary>
    internal void Close(BinTable bins, int binSlot)
    {
        int row = RowOf(bins, binSlot);

        bins.ExpiryRow[binSlot] = 0;

        if (row != Tables.Rows.NoSlot)
        {
            Rows.Free(Rows.At(row));
        }
    }

    /// <summary>Adds fresh stock to the newest bucket.</summary>
    internal void Add(int row, long amount) => Buckets[row][0] += amount;

    /// <summary>Takes stock from the oldest bucket first.</summary>
    internal void Take(int row, long amount)
    {
        ref AgeBuckets buckets = ref Buckets[row];

        for (int age = ShelfLife.MaxCycles - 1; age >= 0 && amount > 0; age--)
        {
            long taken = buckets[age] < amount ? buckets[age] : amount;

            buckets[age] -= taken;
            amount -= taken;
        }
    }

    /// <summary>Moves every bucket of <paramref name="from"/> into the same age of <paramref name="into"/>.</summary>
    internal void Merge(int from, int into)
    {
        ref AgeBuckets source = ref Buckets[from];
        ref AgeBuckets target = ref Buckets[into];

        for (int age = 0; age < ShelfLife.MaxCycles; age++)
        {
            target[age] += source[age];
            source[age] = 0;
        }
    }

    /// <summary>
    /// Ages <paramref name="row"/> by one cycle and returns what spoiled. Stock older than
    /// <paramref name="cycles"/> cycles is discarded and counted against <paramref name="today"/>.
    /// </summary>
    internal long Shift(int row, int cycles, ushort today)
    {
        ref AgeBuckets buckets = ref Buckets[row];
        long spoiled = 0;

        for (int age = cycles - 1; age < ShelfLife.MaxCycles; age++)
        {
            spoiled += buckets[age];
        }

        for (int age = ShelfLife.MaxCycles - 1; age > 0; age--)
        {
            buckets[age] = age < cycles ? buckets[age - 1] : 0;
        }

        buckets[0] = 0;

        if (spoiled > 0)
        {
            if (SpoilingDay[row] != today)
            {
                PriorSpoiled[row] = SpoilingDay[row] == today - 1 ? DaySpoiled[row] : 0;
                DaySpoiled[row] = 0;
                SpoilingDay[row] = today;
            }

            DaySpoiled[row] += spoiled;
        }

        return spoiled;
    }

    /// <summary>The sum of the row's buckets, which must equal its Bin's level.</summary>
    public long Total(int row)
    {
        long total = 0;

        for (int age = 0; age < ShelfLife.MaxCycles; age++)
        {
            total += Buckets[row][age];
        }

        return total;
    }

    /// <summary>Rebuilds <see cref="BinTable.ExpiryRow"/> from each row's saved Bin handle.</summary>
    internal void Rebuild(BinTable bins)
    {
        for (int slot = 0; slot < bins.Rows.SlotCount; slot++)
        {
            bins.ExpiryRow[slot] = 0;
        }

        for (int row = 0; row < Rows.SlotCount; row++)
        {
            if (Rows.IsLive(row) && bins.Rows.TryResolve(Bin[row], out int binSlot))
            {
                bins.ExpiryRow[binSlot] = row + 1;
            }
        }
    }
}
