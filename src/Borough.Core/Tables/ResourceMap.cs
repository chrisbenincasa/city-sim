namespace Borough.Core.Tables;

/// <summary>
/// Which Resource. A dense small integer, assigned by the Ruleset at world creation.
/// </summary>
/// <remarks>
/// Deliberately not an enum: adr/0031's nine are the Resources <em>this</em> Ruleset declares, and
/// the set is content rather than code. What the core needs is that the identifiers are dense, small
/// and totally ordered, which is what makes <see cref="ResourceMap"/> a sorted array rather than a
/// map.
/// </remarks>
public readonly record struct ResourceId(ushort Raw) : IComparable<ResourceId>
{
    /// <inheritdoc/>
    public int CompareTo(ResourceId other) => Raw.CompareTo(other.Raw);

    public static bool operator <(ResourceId left, ResourceId right) => left.Raw < right.Raw;

    public static bool operator >(ResourceId left, ResourceId right) => left.Raw > right.Raw;

    public static bool operator <=(ResourceId left, ResourceId right) => left.Raw <= right.Raw;

    public static bool operator >=(ResourceId left, ResourceId right) => left.Raw >= right.Raw;
}

/// <summary>
/// A Resource-keyed map held as a sorted array with binary lookup, never as a hash map.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deterministic order is the actual requirement</b>, and at adr/0031's nine Resources the speed
/// question is cache behaviour rather than an algorithmic one — which is what S4's K4 measured
/// directly. .NET randomises string hashing per process, so a <c>Dictionary</c> walked here would
/// order differently between two runs of the same binary; that is <c>BOR0301</c>, and a sorted array
/// is the somewhere-better-to-go that makes the prohibition hold.
/// </para>
/// <para>
/// <b>Static helpers over spans, not a container.</b> The storage belongs to whoever owns the map —
/// an inline array inside a row, or a pair of columns — and this type only imposes the invariant that
/// the keys stay ascending. That is what keeps it usable from a Bin without the Bin becoming a class,
/// and it is why nothing here allocates.
/// </para>
/// <para>
/// <b>The <c>Bin</c> itself is not here.</b> <c>02 §3.2</c> omits the wait list that <c>02 §4.1</c>
/// requires and <c>§3</c> is the least-maintained section in that document, so the struct lands in
/// slice 7 where its semantics are settled. Build the map here; build the Bin where it is decided.
/// </para>
/// </remarks>
public static class ResourceMap
{
    /// <summary>
    /// Binary search over the used prefix.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="key"/> if present; otherwise the bitwise complement of the index
    /// it would be inserted at, which is the same convention <c>Array.BinarySearch</c> uses.
    /// </returns>
    public static int Find(ReadOnlySpan<ResourceId> keys, ResourceId key)
    {
        int low = 0;
        int high = keys.Length - 1;

        while (low <= high)
        {
            // (high - low) >> 1 rather than (low + high) / 2: the shift count is constant, so
            // BOR0204 is satisfied by construction, and the subtraction cannot overflow.
            int middle = low + ((high - low) >> 1);
            ResourceId candidate = keys[middle];

            if (candidate == key)
            {
                return middle;
            }

            if (candidate < key)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return ~low;
    }

    /// <summary>Inserts or overwrites, keeping the keys ascending.</summary>
    /// <returns><see langword="false"/> when the key is absent and there is no room for it.</returns>
    public static bool Set<TValue>(
        Span<ResourceId> keys, Span<TValue> values, ref int count, ResourceId key, TValue value)
        where TValue : unmanaged
    {
        int found = Find(keys[..count], key);

        if (found >= 0)
        {
            values[found] = value;
            return true;
        }

        if (count == keys.Length)
        {
            return false;
        }

        int at = ~found;
        keys[at..count].CopyTo(keys[(at + 1)..]);
        values[at..count].CopyTo(values[(at + 1)..]);

        keys[at] = key;
        values[at] = value;
        count++;
        return true;
    }

    /// <summary>Reads one entry.</summary>
    public static bool TryGet<TValue>(
        ReadOnlySpan<ResourceId> keys, ReadOnlySpan<TValue> values, int count, ResourceId key,
        out TValue value)
        where TValue : unmanaged
    {
        int found = Find(keys[..count], key);

        if (found < 0)
        {
            value = default;
            return false;
        }

        value = values[found];
        return true;
    }

    /// <summary>Removes one entry, closing the gap so the keys stay ascending and dense.</summary>
    public static bool Remove<TValue>(
        Span<ResourceId> keys, Span<TValue> values, ref int count, ResourceId key)
        where TValue : unmanaged
    {
        int found = Find(keys[..count], key);

        if (found < 0)
        {
            return false;
        }

        keys[(found + 1)..count].CopyTo(keys[found..]);
        values[(found + 1)..count].CopyTo(values[found..]);

        count--;
        keys[count] = default;
        values[count] = default;
        return true;
    }
}
