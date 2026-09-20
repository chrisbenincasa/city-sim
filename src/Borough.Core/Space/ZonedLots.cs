using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Derived land-discovery buckets over Lots' common geographic use bits, in ascending slot order.
/// This is not a housing search index and never authorises construction; complete site checks do.
/// Rebuild after permission or Lot changes. Membership is bounded by live Lots times use bits.
/// </summary>
public sealed class ZonedLots
{
    /// <summary>Start of each bit's run in <see cref="_entries"/>, plus a terminator.</summary>
    private readonly int[] _starts = new int[LotTable.ZoneBits + 1];

    private int[] _entries = [];

    private bool _stale = true;

    /// <summary>
    /// Marks the index out of date. Called by every writer of the Lot set or of a Zone.
    /// </summary>
    public void Invalidate() => _stale = true;

    /// <summary>
    /// How many live Lots admit <paramref name="permission"/>, which is the size of the draw space.
    /// </summary>
    /// <param name="permission">A single permission bit, such as <see cref="LotTable.Housing"/>.</param>
    public int Count(LotTable lots, ushort permission)
    {
        int bit = BitIndex(permission);

        Ensure(lots);

        return _starts[bit + 1] - _starts[bit];
    }

    /// <summary>
    /// The <paramref name="ordinal"/>th live Lot admitting <paramref name="permission"/>, in
    /// ascending slot order. Returns a Lot slot, never a handle.
    /// </summary>
    /// <remarks>
    /// <b>Every slot it returns is live</b>, which is what lets a caller drop the
    /// <c>Rows.IsLive</c> test it would otherwise need — a freed Lot invalidates the index, so a
    /// stale entry cannot be read.
    /// </remarks>
    public int Nth(LotTable lots, ushort permission, int ordinal)
    {
        int bit = BitIndex(permission);

        Ensure(lots);

        int start = _starts[bit];
        int length = _starts[bit + 1] - start;

        if (ordinal < 0 || ordinal >= length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal),
                ordinal,
                $"{length} live Lots admit bit {bit}.");
        }

        return _entries[start + ordinal];
    }

    /// <summary>
    /// Rebuilds the whole index from the Lots' Zones. Called from <c>World.RebuildDerived</c>.
    /// </summary>
    public void Rebuild(LotTable lots)
    {
        Array.Clear(_starts);

        int slots = lots.Rows.SlotCount;
        int total = 0;

        // Counting sort, pass one: how many entries each bit takes. Counted into starts[bit + 1] so
        // that the prefix sum below turns the same array into the runs without a second buffer.
        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            ushort zone = lots.Zone[slot];

            for (int bit = 0; bit < LotTable.ZoneBits; bit++)
            {
                if ((zone & (ushort)IntegerMath.ShiftLeft(1, bit)) != 0)
                {
                    _starts[bit + 1]++;
                    total++;
                }
            }
        }

        for (int bit = 0; bit < LotTable.ZoneBits; bit++)
        {
            _starts[bit + 1] += _starts[bit];
        }

        if (_entries.Length < total)
        {
            _entries = new int[total];
        }

        // Pass two, walking slots ascending again, so each run comes out in slot order. The cursor
        // is a copy of the run starts rather than the array itself, which stays the runs.
        Span<int> cursor = stackalloc int[LotTable.ZoneBits];

        for (int bit = 0; bit < LotTable.ZoneBits; bit++)
        {
            cursor[bit] = _starts[bit];
        }

        for (int slot = 0; slot < slots; slot++)
        {
            if (!lots.Rows.IsLive(slot))
            {
                continue;
            }

            ushort zone = lots.Zone[slot];

            for (int bit = 0; bit < LotTable.ZoneBits; bit++)
            {
                if ((zone & (ushort)IntegerMath.ShiftLeft(1, bit)) != 0)
                {
                    _entries[cursor[bit]++] = slot;
                }
            }
        }

        _stale = false;
    }

    private void Ensure(LotTable lots)
    {
        if (_stale)
        {
            Rebuild(lots);
        }
    }

    /// <summary>
    /// The bit index of a single-bit permission mask.
    /// </summary>
    /// <remarks>
    /// <b>A mask rather than an index is what callers hold</b> — <see cref="LotTable.Housing"/> and
    /// <see cref="LotTable.Trade"/> are the names the rest of the core reads, and a parallel set of
    /// index constants beside them is the *"bit index repeated in two files"* that
    /// <see cref="LotTable.Housing"/>'s own remarks warn about.
    /// </remarks>
    private static int BitIndex(ushort permission)
    {
        for (int bit = 0; bit < LotTable.ZoneBits; bit++)
        {
            if (permission == (ushort)IntegerMath.ShiftLeft(1, bit))
            {
                return bit;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(permission),
            permission,
            "A draw space is one permission, so the mask carries exactly one bit.");
    }
}
