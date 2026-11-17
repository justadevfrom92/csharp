namespace OperatorsAndLambdas.Variations;

// "Double a number" as a Func<int, int>, written five ways. All give the same answer; they're different eras of C#.
public static class DelegateSyntaxVariation
{
    // Lambda (C# 3+, the original style everywhere in this repo).
    public static Func<int, int> AsLambda()
    {
        return number => number * 2;
    }

    // Anonymous method with the "delegate" keyword (C# 2). Still compiles; you'll see it in old code.
    public static Func<int, int> AsAnonymousMethod()
    {
        return delegate (int number)
        {
            return number * 2;
        };
    }

    // A LOCAL FUNCTION (C# 7): a named method inside a method. Can be recursive, can have its own
    // attributes; turned into a delegate only when needed.
    public static Func<int, int> AsLocalFunction()
    {
        int Double(int number)
        {
            return number * 2;
        }
        return Double;
    }

    // A METHOD GROUP: an existing method used as the delegate.
    public static Func<int, int> AsMethodGroup()
    {
        return DoubleIt;
    }

    // A custom DELEGATE TYPE instead of Func: gives the shape a name. Not interchangeable with Func<int, int>
    // even though the signature is the same - you'd need to convert.
    public delegate int NumberRule(int number);

    public static NumberRule AsCustomDelegate()
    {
        return number => number * 2;
    }

    private static int DoubleIt(int number)
    {
        return number * 2;
    }
}
