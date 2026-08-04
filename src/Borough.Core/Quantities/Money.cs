namespace Borough.Core.Quantities;

/// <summary>
/// Currency, as a signed 64-bit integer count of the smallest unit.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why 64-bit.</b> Income flow at the target population is on the order of 10⁹ per period against
/// an <see cref="int"/> maximum of 2.1×10⁹ — i32 is already exceeded before any headroom is asked
/// for, so this is not a margin decision.
/// </para>
/// <para>
/// <b>Why signed, when stocks are non-negative.</b> Every delta, flow and balance-of-payments figure
/// is inherently signed, and they outnumber the stocks. Going unsigned would <em>arm</em>
/// <c>balance - cost</c> — underflow wrapping to ~1.8×10¹⁹ rather than going visibly negative —
/// while protecting the one column that has no bugs in it. The non-negativity of a stock is therefore
/// enforced by <see cref="TryDebit"/> at the one place it is true, not by the type everywhere.
/// </para>
/// </remarks>
public readonly record struct Money(long Raw) : IComparable<Money>
{
    /// <summary>No money. The additive identity, and the floor for any stock.</summary>
    public static Money Zero => new(0);

    /// <summary>True when this is a debt or a negative flow. Always false for a well-formed stock.</summary>
    public bool IsNegative => Raw < 0;

    /// <summary>
    /// Subtracts <paramref name="cost"/> from this balance, refusing rather than going negative.
    /// </summary>
    /// <remarks>
    /// The non-negative-stock invariant lives here, once, instead of at every call site. A caller
    /// that wants a signed difference — a balance of payments, a profit, a shortfall — uses
    /// <c>operator -</c> and gets a signed answer, which is the case the signed representation exists
    /// to serve.
    /// </remarks>
    /// <returns><see langword="true"/> if the balance covered the cost.</returns>
    public bool TryDebit(Money cost, out Money remaining)
    {
        if (cost.Raw > Raw)
        {
            remaining = this;
            return false;
        }

        remaining = new Money(Raw - cost.Raw);
        return true;
    }

    /// <inheritdoc/>
    public int CompareTo(Money other) => Raw.CompareTo(other.Raw);

    public static Money operator +(Money left, Money right) => new(left.Raw + right.Raw);

    public static Money operator -(Money left, Money right) => new(left.Raw - right.Raw);

    public static Money operator -(Money value) => new(-value.Raw);

    /// <summary>Scales by a whole count — a unit price times a quantity. Never Money times Money.</summary>
    public static Money operator *(Money value, int count) => new(value.Raw * count);

    /// <inheritdoc cref="op_Multiply(Money,int)"/>
    public static Money operator *(int count, Money value) => value * count;

    public static bool operator <(Money left, Money right) => left.Raw < right.Raw;

    public static bool operator >(Money left, Money right) => left.Raw > right.Raw;

    public static bool operator <=(Money left, Money right) => left.Raw <= right.Raw;

    public static bool operator >=(Money left, Money right) => left.Raw >= right.Raw;
}
