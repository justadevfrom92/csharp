namespace OperatorsAndLambdas.Variations;

// Creating one lambda per loop round. Which value does each lambda remember?
public static class LoopCaptureVariation
{
    // "for": there is ONE variable i for the whole loop. Every lambda captures that SAME variable, which is 3
    // when they finally run -> 3, 3, 3. A classic surprise.
    public static List<int> WithForLoop()
    {
        var actions = new List<Func<int>>();
        for (int i = 0; i < 3; i++)
        {
            actions.Add(() => i);
        }
        return actions.Select(action => action()).ToList();
    }

    // "for" + a COPY inside the loop body: a new variable each round -> 0, 1, 2.
    public static List<int> WithForLoopAndCopy()
    {
        var actions = new List<Func<int>>();
        for (int i = 0; i < 3; i++)
        {
            int copy = i;
            actions.Add(() => copy);
        }
        return actions.Select(action => action()).ToList();
    }

    // "foreach": since C# 5 each round gets its OWN variable -> 0, 1, 2 (5.1's "thisWorker" copy is for a for-loop).
    public static List<int> WithForeachLoop()
    {
        var actions = new List<Func<int>>();
        foreach (int value in new[] { 0, 1, 2 })
        {
            actions.Add(() => value);
        }
        return actions.Select(action => action()).ToList();
    }
}
