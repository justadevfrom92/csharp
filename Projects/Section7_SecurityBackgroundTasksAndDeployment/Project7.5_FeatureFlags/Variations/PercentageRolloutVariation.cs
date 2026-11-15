using System.Security.Cryptography;
using System.Text;

namespace FeatureFlags.Variations;

// Turning a feature on for SOME users ("10% rollout"), two ways.
public static class PercentageRolloutVariation
{
    // STABLE: hash the user id into a number 0-99. The same user ALWAYS gets the same answer, so their screen
    // doesn't change on every click, and raising 10% -> 20% only ADDS users (nobody loses the feature).
    public static bool IsOnForUser(string userId, int percent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        int bucket = BitConverter.ToUInt16(hash, 0) % 100;
        return bucket < percent;
    }

    // RANDOM per request: also "10% of requests", but one user flips between old and new on every page load.
    public static bool IsOnRandomly(int percent)
    {
        return Random.Shared.Next(100) < percent;
    }
}
