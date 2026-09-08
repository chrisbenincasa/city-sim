using Borough.Core.Tables;

namespace Borough.Headless;

// Read after Step: reporting allocations must not be charged to the simulation.
internal sealed class TableGrowthProbe
{
    private readonly Rows[] _tables;
    private readonly int[] _capacities;

    internal TableGrowthProbe(ReadOnlySpan<Rows> tables)
    {
        _tables = tables.ToArray();
        _capacities = new int[_tables.Length];
        for (int i = 0; i < _tables.Length; i++) { _capacities[i] = _tables[i].Capacity; }
    }

    internal Growth[] Read()
    {
        List<Growth>? changes = null;
        for (int i = 0; i < _tables.Length; i++)
        {
            Rows table = _tables[i];
            int before = _capacities[i], after = table.Capacity;
            if (before == after) { continue; }
            // Rows.Grow doubles all columns, including intrinsic columns and both buffers.
            // Count every replacement array's payload, not just the net capacity increase.
            long allocatedSlots = 0;
            int capacity = before, growths = 0;
            while (capacity < after)
            {
                capacity = checked(capacity == 0 ? 1 : capacity * 2);
                allocatedSlots += capacity;
                growths++;
            }
            if (capacity != after || after < before)
            { throw new InvalidOperationException("Capacity changed outside Rows.Grow during Step."); }
            int copies = table.Buffering == Buffering.TwoCopies ? 2 : 1;
            var columns = new ColumnGrowth[table.Columns.Length];
            long payload = 0;
            for (int c = 0; c < columns.Length; c++)
            {
                Column column = table.Columns[c];
                long bytes = allocatedSlots * column.BytesPerRow * copies;
                columns[c] = new ColumnGrowth(column.Name, column.BytesPerRow, copies, bytes);
                payload += bytes;
            }
            (changes ??= []).Add(new Growth(table.Name, before, after, table.SlotCount,
                table.LiveCount, growths, payload, columns));
            _capacities[i] = after;
        }
        return changes?.ToArray() ?? [];
    }

    internal sealed record ColumnGrowth(string Name, int BytesPerRow, int Copies, long PayloadBytes);
    internal sealed record Growth(string Table, int Before, int After, int Slots, int Live,
        int Growths, long PayloadBytes, ColumnGrowth[] Columns);
}
