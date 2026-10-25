// A cast tag that duplicates PlacementCandidate (6); BOR0801 checks the enum declaration only.
using Borough.Core.Determinism;
using Borough.Core.Quantities;
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_PurposeTagCast
{
    internal static ulong Run(WorldKey key, ulong id) => Randomness.Draw(key, id, Ticks.Zero, (PurposeTag)6);
}
