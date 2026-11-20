namespace Iterators.Variations;

// The argument check INSIDE the iterator: calling Generate(-1) does NOT throw.
// The error appears later, at the first loop step - maybe far away from the bad call.
// OrderNumberGenerator.GenerateChecked (original) throws at the call.
public static class LateCheckVariation
{
    public static IEnumerable<string> Generate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        for (int number = 1; number <= count; number++)
        {
            yield return $"ORD-{number:D4}";
        }
    }
}
