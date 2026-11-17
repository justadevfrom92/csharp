# Project 9.1 — Operator Overloading & Lambda Expressions

```bash
cd OperatorsAndLambdas
dotnet run
```

```bash
cd OperatorsAndLambdas.Tests
dotnet test
```

## What it covers

| Topic | Where |
|---|---|
| **Operator overloading**: `+`, `-` (two kinds), `*` (both orders), `<`, `>`, `<=`, `>=`, an `explicit` conversion | `ValueObjects/MoneyValueObject.cs` |
| **Lambda expressions**: expression vs statement lambdas, `Func`/`Action`/`Predicate`, natural type (`var f = (int a) => ...`), default parameters, `static` lambdas, method groups | `Program.cs` |
| **Closures** (a lambda remembering a variable), lambdas returned from and passed to methods, composing them | `Helpers/LambdaHelper.cs` |
| **Expression trees**: `Expression<Func<T, bool>>`, the lambda as data you can read (how EF Core builds SQL) | `Helpers/LambdaHelper.cs` |

## Why

**Operator overloading** makes code read like the problem: `price * 3 + shipping` instead of
`price.Multiply(3).Add(shipping)`. Only do it when the meaning is **obvious** — adding money is; "adding" two
customers isn't. Rules the compiler enforces: operators are `public static`, one parameter must be your type,
and comparison operators come in pairs (`<` needs `>`, `<=` needs `>=`). `==`/`!=` come free with `record`.

**`explicit` vs `implicit` conversion:** `(decimal)money` must be written out because it **throws away** the
currency. An `implicit` conversion happens silently and should only be used when nothing can be lost.

**Lambdas** are small functions you can pass around as values. Every LINQ call (`Where(p => p.Price > 10)`),
every minimal API endpoint, every `Configure(options => ...)` in this repo is a lambda.

**Closures:** a lambda can use variables from where it was written, and keeps them alive.
`MakeMultiplier(1.2m)` returns a lambda that still knows `1.2` after the method has finished.
`MakeCounter()` shows a captured variable that changes between calls.

**`Func` vs `Expression<Func>`:** a `Func` is compiled code — you can only run it. An `Expression` is the lambda
as a **tree of data** — you can read it (`Describe` prints `(product.Price > 20)`) or compile it to run it.
This is how EF Core translates C# lambdas into SQL.

## Code snippets

**Overloading `+` and `*`**, from `ValueObjects/MoneyValueObject.cs`

```csharp
public static MoneyValueObject operator +(MoneyValueObject left, MoneyValueObject right)
{
    CheckSameCurrency(left, right);
    return new MoneyValueObject(left.Amount + right.Amount, left.Currency);
}

public static MoneyValueObject operator *(MoneyValueObject money, decimal factor)
{
    ArgumentNullException.ThrowIfNull(money);
    return new MoneyValueObject(money.Amount * factor, money.Currency);
}

public static MoneyValueObject operator *(decimal factor, MoneyValueObject money) // 3 * price works too
{
    return money * factor;
}
```

**Comparison operators (in pairs) and an explicit conversion**

```csharp
public static bool operator <(MoneyValueObject left, MoneyValueObject right)
{
    return left.CompareTo(right) < 0;
}

public static bool operator >(MoneyValueObject left, MoneyValueObject right)
{
    return left.CompareTo(right) > 0;
}

public static explicit operator decimal(MoneyValueObject money)
{
    return money.Amount;
}
```

**Using them**, from `Program.cs`

```csharp
MoneyValueObject total = price * 3 + shipping;
bool expensive = total > new MoneyValueObject(50m, "USD");
MoneyValueObject refund = -price;
decimal plain = (decimal)total;
```

**Kinds of lambda**, from `Program.cs`

```csharp
Func<int, int, int> add = (a, b) => a + b;              // expression lambda
Func<int, string> describe = number =>                  // statement lambda
{
    if (number % 2 == 0)
    {
        return "even";
    }
    return "odd";
};
Action<string> print = text => Console.WriteLine(text);  // returns nothing
Predicate<int> isPositive = number => number > 0;       // returns bool
var multiply = (int a, int b) => a * b;                  // natural type
var greet = (string name = "friend") => $"Hi {name}";   // default parameter
Func<string, int> length = static text => text.Length;  // can't capture anything
```

**Closures and composition**, from `Helpers/LambdaHelper.cs`

```csharp
public static Func<decimal, decimal> MakeMultiplier(decimal factor)
{
    return price => price * factor;           // "factor" is captured
}

public static Func<T, T> Compose<T>(Func<T, T> first, Func<T, T> second)
{
    return value => second(first(value));
}

public static Func<int> MakeCounter()
{
    int count = 0;
    return () => ++count;                      // the captured variable lives on between calls
}
```

**Expression trees**

```csharp
public static string Describe(Expression<Func<ProductModel, bool>> condition)
{
    return condition.Body.ToString();          // "((product.Price > 20) AndAlso (product.Stock > 0))"
}

Func<ProductModel, bool> compiled = condition.Compile();
```

## Variations

More ways to write the same things, in `OperatorsAndLambdas/Variations/`, checked by `OperatorsAndLambdas.Tests/VariationsTest.cs`.

**"Double a number" as a delegate** (`DelegateSyntaxVariation`; all return 14 for 7)

| Syntax | Era | The difference |
|---|---|---|
| `number => number * 2` (original) | C# 3+ | shortest |
| `delegate (int number) { return number * 2; }` | C# 2 | anonymous method: you'll see it in old code |
| local function `int Double(int number) { ... }` returned as `Double` | C# 7+ | has a name; can call itself (recursion) |
| method group `return DoubleIt;` | always | reuses an existing method |
| custom `delegate int NumberRule(int number)` | always | names the shape; **not** interchangeable with `Func<int, int>` without converting |

**Lambdas created in a loop** (`LoopCaptureVariation`; each lambda returns "its" loop value)

| Loop | Result | Why |
|---|---|---|
| `for (int i ...) actions.Add(() => i);` | **3, 3, 3** | ONE `i` for the whole loop; every lambda sees its final value |
| `for` with `int copy = i;` inside | 0, 1, 2 | a new variable each round (this is why 5.1 copies `thisWorker`) |
| `foreach (int value in ...)` | 0, 1, 2 | since C# 5, a new variable each round |

**Money conversions and operators** (`MoneyImplicitVariation`)

| Way | `price = 19.99m` | The difference |
|---|---|---|
| `explicit operator decimal` only (original) | doesn't compile | the currency can't be lost or guessed |
| `implicit operator MoneyImplicitVariation(decimal)` | compiles, **currency guessed as USD** | silent: `euroPrice.Add(5m)` throws because `5m` became dollars |
| named method `Add(...)` instead of `+` (also 4.2) | — | same rule; longer in formulas, easier to find |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `MoneyValueObject` is a `record` (class) | Equality by value for free (`==`), immutable, can't skip the constructor. | A `struct` has a hidden "all zeros" value with a `null` currency (see [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md)). |
| `IComparable<MoneyValueObject>` | One `CompareTo` that all four comparison operators reuse, and `List.Sort()` / `Max()` work too. | Writing the comparison four times. |
| `explicit operator decimal` | Loses the currency, so the caller must ask for it. | `implicit` would drop the currency silently. |
| `Func<decimal, decimal>` for price rules | A rule is "a price in, a price out" — nothing more is needed. | A `IPriceRule` interface with one method is extra code for the same thing (see 9.4 Strategy for when an interface IS worth it). |
| `Expression<Func<ProductModel, bool>>` | Can be read as data and compiled. | A `Func` can't be inspected. |
| `static` lambda | The compiler stops it capturing variables by accident (no hidden allocations). | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `+` / `-` | same currency adds/subtracts | USD + EUR → exception | `null` side → `ArgumentNullException` |
| `*` / unary `-` | both orders equal, negation | result rounded to cents | `null` → `ArgumentNullException` |
| comparison | all six operators agree | USD > EUR throws, but `==` is just `false` | `null` side |
| constructor + `(decimal)` | lower-case currency stored upper-case, converts | 2- or 7-letter currency | `null`/empty/blank currency |
| `MakeMultiplier` | each closure keeps its own factor | 0 or negative factor | result too big for `decimal` → `OverflowException` |
| `Compose` / `ApplyAll` | runs first then second; rules in order | no rules = unchanged; negative price throws | `null` functions |
| expression trees | read as text and compiled to filter | nothing matches → empty | `null` arguments |
