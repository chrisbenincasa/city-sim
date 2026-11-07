// Fresh entropy that is not System.Random.
namespace Borough.Core.ReviewProbes;
internal static class ReviewProbe_CryptoRandom
{
    internal static int Run() => System.Security.Cryptography.RandomNumberGenerator.GetInt32(100);
}
