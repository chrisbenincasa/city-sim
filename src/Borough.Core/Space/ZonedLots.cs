using Borough.Core.Arithmetic;
using Borough.Core.Entities;
using Borough.Core.Tables;

namespace Borough.Core.Space;

/// <summary>
/// Which live Lots admit a given use. The draw space for anybody looking for somewhere to be.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because <c>adr/0165</c>'s land-use split broke a meaning
/// <see cref="Rules.PlacementEngine"/> had already argued for at length.</b> That engine draws a
/// seeker's candidates over <em>Lots</em> rather than over Buildings, and its own remarks say why:
/// over Buildings, <c>candidates</c> meant something the Ruleset could not state, because roughly
/// 55% of Building slots stand freed at any instant and *"lowering the demolition rate would have
/// silently raised the effective candidate count."* Painting one block in eight commercial does the
/// same thing in the other direction — a look landing on trade land buys nothing, so three looks
/// bought about 2.6, and the land-use share silently became a placement tuning knob. Measured on
/// <c>GoldenFixtures</c> at 1,000 Citizens over 100,000 Ticks: vacancy 18.5% → 26% against an
/// unchanged capacity of ~188, which is fourteen dwellings nobody found.
/// </para>
/// <para>
/// <b>⚠ The dead look it removes is not the dead look the engine defends.</b> That remark keeps *"a
/// look that lands on a vacant one found nothing, which is a thing that happens to somebody looking
/// for somewhere to live"* — and it still does. A vacant Lot that <em>admits</em> a dwelling is a
/// home not yet built, and looking at it is a real disappointment. A Lot that admits only a trade is
/// not a home at all, and nobody flat-hunting ever viewed a shopfront. The first stays a wasted look
/// and the second stops being one.
/// </para>
/// <para>
/// <b>A bucket per permission <em>bit</em>, and a Lot appears once per bit it carries.</b>
/// <c>CONTEXT.md</c> → Zone is a permission set, and it says mixed use *"needs no machinery: it is a
/// permission set with more than one entry"* — so a bucketing that assumed one use per Lot would
/// make mixed use unrepresentable in exactly the structure that decides who may live where. The
/// entry count is therefore the sum of the set bits over live Lots, which is the Lot count today
/// because the generator paints one bit, and grows with mixed use rather than being refused by it.
/// </para>
/// <para>
/// <b>⚠ <c>(derived AND rebuilt)</c>, and rebuilt whole rather than maintained.</b> Unlike
/// <see cref="BuildingResidency"/> this keeps no incremental <c>Add</c>/<c>Remove</c>, because the
/// membership only ever changes where the <em>Lot set</em> does — <see cref="LotTable.Create"/> is
/// the only writer of <see cref="LotTable.Zone"/> in the core, and there are exactly two frees. A
/// demolition <see cref="LotTable.Vacate"/>s and keeps the parcel, so the ordinary churn of a city
/// does not touch this at all: what is indexed is <em>permission</em>, and occupancy is asked
/// separately through <see cref="LotTable.BuildingOn"/>. Subdivision creates Lots in bulk, so an
/// eager rebuild per <c>Create</c> would be quadratic over a road edit; instead every writer calls
/// <see cref="Invalidate"/> and the next query pays one <c>O(n)</c> pass.
/// </para>
/// <para>
/// <b>The rebuild is a counting sort, so its order is recoverable from saved state.</b> That is
/// <c>05 §3</c>'s test for the classification and the one <see cref="BuildingResidency"/> records:
/// entries land in ascending Lot-slot order within each bucket regardless of the order Lots were
/// created, so a load reproduces this <em>exactly</em> rather than plausibly. A draw over it is
/// therefore stable across save and reload, which a creation-ordered structure would not be the
/// moment the free list recycled a slot.
/// </para>
/// </remarks>
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
