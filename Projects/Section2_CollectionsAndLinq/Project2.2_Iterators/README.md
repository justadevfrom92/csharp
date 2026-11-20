# Project 2.2 — Iterators (`IEnumerable`, `yield`, your own `foreach`-able collection)

How `foreach` really works, and how to **write** sequences instead of only using `List<T>`: iterator methods with
`yield return` / `yield break`, a lazy file reader, an endless sequence, and a collection class with a hand-written
enumerator. 2.1 *uses* LINQ; this shows what is underneath it.

```bash
cd Iterators
dotnet run
```

```bash
cd Iterators.Tests
dotnet test
```

Output:

```
1. After Generate(3): 0 lines ran
   After the loop: made 1 | used ORD-0001 | made 2 | used ORD-0002 | made 3 | used ORD-0003
2. Forever().Take(5): 1, 2, 3, 4, 5
3. Until the blank line: a, b
4. First error: ERROR disk full
   File: opened, closed
5. Shelf starting with C: Coffee, Cocoa
```

## How `foreach` works

```csharp
foreach (string item in shelf) { ... }
```

is turned by the compiler into:

```csharp
IEnumerator<string> cursor = shelf.GetEnumerator();
try
{
    while (cursor.MoveNext())
    {
        string item = cursor.Current;
        ...
    }
}
finally
{
    cursor.Dispose(); // always - even after "break" or an exception
}
```

| Interface | One job | Methods |
|---|---|---|
| `IEnumerable<T>` | "can be looped over" | `GetEnumerator()` |
| `IEnumerator<T>` | the **cursor** for one loop | `MoveNext()`, `Current`, `Reset()`, `Dispose()` |

`yield return` makes the compiler write the `IEnumerator<T>` class for you.

## What's in it

| What | File |
|---|---|
| lazy `yield return`, endless `Forever()`, `yield break`, argument check done right | `Iterators/Generators/OrderNumberGenerator.cs` |
| a log file read one line at a time; `finally` runs even after `break` | `Iterators/Readers/LogFileReader.cs` |
| a collection usable with `foreach`, LINQ and `{ "a", "b" }` | `Iterators/Collections/ShelfCollection.cs` |
| the cursor written by hand | `Iterators/Enumerators/ShelfEnumerator.cs` |

## Things the tests showed

- `Generate(3)` runs **zero** lines until the loop starts; then "made" and "used" **take turns** — one item exists at a time.
- `orders.Take(2)` made only 2 of 100 items. Calling `Count()` afterwards started again **from 1** (102 lines ran in total).
- `UntilBlankLine(null)` does **not** throw at the call — only when looped (`NullReferenceException`).
- A missing file in `ReadErrors` also throws only when looped.
- `foreach` + `break` still closed the file. Calling `GetEnumerator()`/`MoveNext()` by hand **without** `Dispose()` left it open.

## Code snippets

**An iterator method**

```csharp
public static IEnumerable<string> Generate(int count)
{
    for (int number = 1; number <= count; number++)
    {
        yield return $"ORD-{number:D4}"; // hand out one, pause here
    }
}
```

**Stop early**

```csharp
if (string.IsNullOrWhiteSpace(line))
{
    yield break;
}
```

**Check arguments at the call, not at the first loop**

```csharp
public static IEnumerable<string> GenerateChecked(int count)
{
    ArgumentOutOfRangeException.ThrowIfNegative(count); // runs now
    return Iterate();

    IEnumerable<string> Iterate()                         // runs when looped
    {
        for (int number = 1; number <= count; number++)
        {
            yield return $"ORD-{number:D4}";
        }
    }
}
```

**A lazy file reader with cleanup**

```csharp
using StreamReader reader = File.OpenText(path);
string? line;
while ((line = reader.ReadLine()) != null)
{
    if (line.Contains("ERROR"))
    {
        yield return line;
    }
}
// the "using" closes the file when the loop ends OR the caller breaks out
```

**Your own collection**

```csharp
public class ShelfCollection : IEnumerable<string>
{
    private readonly List<string> items = new();

    public void Add(string item) { items.Add(item); }        // + IEnumerable = { "Tea", "Coffee" } works

    public IEnumerator<string> GetEnumerator()
    {
        return new ShelfEnumerator(items.ToArray());
    }

    IEnumerator IEnumerable.GetEnumerator()                 // the old non-generic version
    {
        return GetEnumerator();
    }
}
```

## Variations

Other ways to write the same things, in `Iterators/Variations/`, checked by `Iterators.Tests/VariationsTest.cs`.

**`GetEnumerator()` by hand vs with `yield`** (`ShelfYieldVariation`)

| Way | Code | Add an item during the loop |
|---|---|---|
| `ShelfEnumerator` class (original) | a whole class: position, `MoveNext`, `Current`, `Reset` | allowed — the loop sees a **copy** |
| `foreach (...) yield return item;` | 4 lines | **`InvalidOperationException`** ("Collection was modified") from `List<T>` |
| `return items.GetEnumerator();` | 1 line | same as `yield` (`List<T>`'s own cursor) |

**Where the argument check goes** (`LateCheckVariation`)

| Way | `Generate(-1)` |
|---|---|
| check, then a local iterator function (original `GenerateChecked`) | throws **at the call** |
| check inside the iterator | **no error** until the first loop step — maybe far away |

**Using a query twice** (`MultipleEnumerationVariation`)

| Way | Source read | Why |
|---|---|---|
| `Count()` then `First()` on the query | **2 times** | a query is a recipe; each use cooks it again (2 database trips, 2 file reads) |
| `.ToList()` once, then `Count` and `[0]` | 1 time | the results are kept |
| only building the query | 0 times | nothing runs until someone loops |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| return `IEnumerable<T>` from iterators | the only return type `yield` allows (with `IEnumerator<T>`); callers can use LINQ | `List<T>` would build everything first, losing the laziness |
| `string[]` copy inside `ShelfEnumerator` | the loop can't be broken by changes | looping the live list needs a "was it changed?" check like `List<T>` |
| `int position = -1` | "before the first item": the first `MoveNext()` moves to 0 | starting at 0 would skip nothing but make `Current` valid before `MoveNext` |
| `StreamReader.ReadLine()` | one line in memory | `File.ReadAllLines` loads the whole file |
| `List<string>? log` parameter | lets the tests SEE when code runs | `Console.WriteLine` can't be checked |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `Generate` | runs only when looped, one item at a time | `Take(2)` makes 2 of 100; `Count()` starts again | 0 or negative count → empty |
| `yield break` | stops at the blank line | no blank → all; blank first → nothing | `null` input throws only when looped |
| `GenerateChecked` | same items as `Generate` | 0 allowed | negative → throws at the call |
| `ReadErrors` | only ERROR lines; opened then closed | `break` still closes; manual `MoveNext` without `Dispose` doesn't | missing file throws only when looped |
| `ShelfCollection` | `foreach`, LINQ, initializer | adding during a loop is fine (copy) | blank item; `Current` before/after the loop |
| Variations | same items / same answers | `yield` version throws on change; late check; query read twice | blank item rejected; checked version throws early; building a query reads nothing |
