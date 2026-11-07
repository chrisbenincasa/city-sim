using Borough.Core.Tables;

namespace Borough.Core.Entities;

/// <summary>A Building's billing record for one delivered Utility.</summary>
public readonly struct UtilityBill;

/// <summary>Today's and yesterday's utility payments, rolled lazily on a billing write.</summary>
public struct UtilityPayments
{
    /// <summary>The Day the current totals belong to.</summary>
    public ushort Day { get; set; }
    /// <summary>What reached the treasury today.</summary>
    public long Paid { get; set; }
    /// <summary>What was waived today.</summary>
    public long Unpaid { get; set; }
    /// <summary>What reached the treasury on the preceding Day.</summary>
    public long PriorPaid { get; set; }
    /// <summary>What was waived on the preceding Day.</summary>
    public long PriorUnpaid { get; set; }
}

/// <summary>Saved per-Building payment evidence in flat rows.</summary>
[Table]
public sealed class UtilityBillTable
{
    private readonly Rows<UtilityBill> _rows;

    /// <summary>Creates the billing table, whose owner handles hash Building identities.</summary>
    public UtilityBillTable(BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        _rows = new Rows<UtilityBill>("utility_bill", 8, Buffering.OneCopy);
        Building = _rows.SavedHandle("building", buildings.Rows, Touch.Cold);
        UtilityKey = _rows.Saved<ulong>("utility_key", Touch.Cold);
        Payments = _rows.Saved<UtilityPayments>("payments", Touch.Cold);
        Next = _rows.Derived<int>("next", Touch.Cold);
        _rows.Seal();
    }

    /// <summary>The slot allocator and declared columns.</summary>
    public Rows<UtilityBill> Rows => _rows;
    /// <summary>The Building this record belongs to.</summary>
    public HandleColumn<Building> Building { get; }
    /// <summary>The stable Resource key, preserved across Ruleset reloads.</summary>
    public Column<ulong> UtilityKey { get; }
    /// <summary>The saved daily totals.</summary>
    public Column<UtilityPayments> Payments { get; }
    /// <summary>The next row for the same Building, encoded plus one.</summary>
    public Column<int> Next { get; }

    /// <summary>Finds a Building's record for a Utility, or no slot.</summary>
    public int Find(BuildingTable buildings, int building, ulong utility)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        for (int row = buildings.UtilityBillHead[building] - 1; row >= 0; row = Next[row] - 1)
        {
            if (UtilityKey[row] == utility) { return row; }
        }

        return Tables.Rows.NoSlot;
    }

    /// <summary>Records a payment and the waived remainder on its firing Day.</summary>
    public void Record(BuildingTable buildings, int building, ulong utility, ushort today, long paid, long unpaid)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        int row = Find(buildings, building, utility);
        if (row == Tables.Rows.NoSlot)
        {
            // There is at most one row per Building per delivered Utility in the Ruleset.
            row = _rows.Resolve(_rows.Allocate());
            Building[row] = buildings.Rows.At(building);
            UtilityKey[row] = utility;
            int previous = Tables.Rows.NoSlot;
            int next = buildings.UtilityBillHead[building] - 1;
            while (next > row)
            {
                previous = next;
                next = Next[next] - 1;
            }

            Next[row] = next + 1;
            if (previous == Tables.Rows.NoSlot) { buildings.UtilityBillHead[building] = row + 1; }
            else { Next[previous] = row + 1; }
        }

        UtilityPayments payments = Payments[row];
        if (payments.Day != today)
        {
            payments.PriorPaid = payments.Day == today - 1 ? payments.Paid : 0;
            payments.PriorUnpaid = payments.Day == today - 1 ? payments.Unpaid : 0;
            payments.Paid = 0;
            payments.Unpaid = 0;
            payments.Day = today;
        }

        payments.Paid = Add(payments.Paid, paid);
        payments.Unpaid = Add(payments.Unpaid, unpaid);
        Payments[row] = payments;
    }

    /// <summary>Reads complete previous-Day totals, including idle Days.</summary>
    public (long Paid, long Unpaid) PriorOn(BuildingTable buildings, int building, ulong utility, ushort today)
    {
        int row = Find(buildings, building, utility);
        if (row == Tables.Rows.NoSlot) { return default; }
        UtilityPayments payments = Payments[row];
        return payments.Day == today
            ? (payments.PriorPaid, payments.PriorUnpaid)
            : payments.Day == today - 1 ? (payments.Paid, payments.Unpaid) : default;
    }

    /// <summary>Rebuilds the Building lists from saved owner handles.</summary>
    public void Rebuild(BuildingTable buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        for (int b = 0; b < buildings.Rows.SlotCount; b++) { buildings.UtilityBillHead[b] = 0; }
        for (int row = 0; row < _rows.SlotCount; row++)
        {
            Next[row] = 0;
            if (_rows.IsLive(row) && buildings.Rows.TryResolve(Building[row], out int building))
            {
                Next[row] = buildings.UtilityBillHead[building];
                buildings.UtilityBillHead[building] = row + 1;
            }
        }
    }

    /// <summary>Frees all evidence when its Building is demolished.</summary>
    public void Retire(BuildingTable buildings, int building)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        int row = buildings.UtilityBillHead[building] - 1;
        while (row >= 0)
        {
            int next = Next[row] - 1;
            _rows.Free(_rows.At(row));
            row = next;
        }

        buildings.UtilityBillHead[building] = 0;
    }

    private static long Add(long standing, long amount) =>
        (long)Int128.Min(long.MaxValue, (Int128)standing + amount);
}
