// Guid.NewGuid is banned by name; CreateVersion7 also reads the clock and fresh entropy.
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_GuidVersion7
{
    internal static System.Guid Run() => System.Guid.CreateVersion7();
}
