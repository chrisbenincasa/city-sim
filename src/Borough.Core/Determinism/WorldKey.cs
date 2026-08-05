namespace Borough.Core.Determinism;

using Borough.Core.Quantities;

/// <summary>
/// A world seed, already folded through its own hashing round. The first coordinate of
/// <see cref="Randomness.Draw"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists as a type rather than as a <c>ulong</c>.</b> adr/0003's <c>draw</c> originally
/// began <c>mix(seed + GOLDEN + entity)</c>, which added two externally chosen coordinates together so
/// that only their sum reached the hash — making world <c>S</c> entity <c>k+1</c> bit-identical to
/// world <c>S+1</c> entity <c>k</c>, at every Tick and for every purpose. Rerolling the seed produced
/// the same world shifted by one entity. The fix gives the seed its own round, and that round is
/// loop-invariant, so it belongs somewhere it is paid once. This type is that somewhere.
/// </para>
/// <para>
/// <b>It is not shaped like the types in <c>Quantities</c>, and the difference is the point.</b> Any
/// <c>ulong</c> is a valid <see cref="Ticks"/>; only a derived value is a valid key. So there is no
/// public constructor — a key can be obtained from a seed and no other way. Passing a raw seed where a
/// key belongs would not throw or corrupt anything; it would quietly simulate a <em>different world</em>
/// than the save says, which is the kind of defect that survives every test that does not already know
/// the answer.
/// </para>
/// <para>
/// <b>Save the seed, not the key.</b> The key is derivable, and storing the derived form invites the two
/// to disagree after any change to the derivation.
/// </para>
/// </remarks>
public readonly struct WorldKey : IEquatable<WorldKey>
{
    internal readonly ulong Raw;

    private WorldKey(ulong raw) => Raw = raw;

    /// <summary>Derives the key for a world seed. The one way to obtain a <see cref="WorldKey"/>.</summary>
    public static WorldKey FromSeed(ulong worldSeed) =>
        new(Randomness.Mix(unchecked(worldSeed + Randomness.Golden)));

    /// <inheritdoc/>
    public bool Equals(WorldKey other) => Raw == other.Raw;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is WorldKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Raw.GetHashCode();

    public static bool operator ==(WorldKey left, WorldKey right) => left.Equals(right);

    public static bool operator !=(WorldKey left, WorldKey right) => !left.Equals(right);
}
