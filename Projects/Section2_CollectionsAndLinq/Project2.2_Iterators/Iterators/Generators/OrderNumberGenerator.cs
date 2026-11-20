namespace Iterators.Generators;

// ITERATOR METHODS: "yield return" hands out one item and PAUSES the method until the caller asks for the next.
// The compiler turns the method into a hidden IEnumerator class for you.
public static class OrderNumberGenerator
{
    // LAZY: calling Generate(3) runs NOTHING. Each loop step runs the code up to the next yield.
    // "log" (optional) records when each line really runs, so the tests can see it.
    public static IEnumerable<string> Generate(int count, List<string>? log = null)
    {
        for (int number = 1; number <= count; number++)
        {
            log?.Add($"made {number}");
            yield return $"ORD-{number:D4}";
        }
    }

    // Never ends. Fine, because each number is only made when asked for: Forever().Take(3).
    // ToList() or Count() on it would run until the program runs out of memory.
    public static IEnumerable<int> Forever()
    {
        int number = 1;
        while (true)
        {
            yield return number;
            number++;
        }
    }

    // "yield break" ends the sequence early, like "return" in a normal method.
    public static IEnumerable<string> UntilBlankLine(IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                yield break;
            }
            yield return line;
        }
    }

    // Argument checks inside an iterator ALSO run late (see Variations/LateCheckVariation).
    // Fix: check first in a normal method, then return a local iterator function.
    public static IEnumerable<string> GenerateChecked(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Iterate();

        IEnumerable<string> Iterate()
        {
            for (int number = 1; number <= count; number++)
            {
                yield return $"ORD-{number:D4}";
            }
        }
    }
}
