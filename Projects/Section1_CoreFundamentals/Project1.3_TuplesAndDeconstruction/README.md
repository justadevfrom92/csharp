# Project 1.3 — Tuples & Deconstruction

Several values without writing a class: **named tuples** to return more than one result, **deconstruction**
(`var (a, b) = ...`) of tuples, classes, records and dictionary entries, **switching on two values at once**,
swapping, discards (`_`), tuples in LINQ, and how tuples compare with `out`, `Tuple<>`, anonymous types and records.

```bash
cd Tuples
dotnet run
```

```bash
cd Tuples.Tests
dotnet test
```

Output:

```
1. Total 50.00, count 3, average 16.67
   Deconstructed: 50.00 over 3 items
2. Range 5.01 to 25.00
   Empty list: no range
3. Shipping US standard 5, US express 15, FR standard 25, FR express 40
4. Swapped: left 2, right 1
5. Parcel: 2.5 kg to Paris
   Tea: 12
   Coffee: 0
6. Item #0 costs 19.99
6. Item #1 costs 5.01
```

## What's in it

| What | File |
|---|---|
| return `(decimal Total, int Count, decimal Average)`; nullable tuple `(...)?` for "no answer" | `Tuples/Calculators/PriceStatsCalculator.cs` |
| `(country, express) switch { ("US", false) => ..., (_, true) => ... }` | `Tuples/Calculators/ShippingCalculator.cs` |
| a class with a hand-written `Deconstruct` | `Tuples/Models/ParcelModel.cs` |
| swap, discards, `foreach (var (key, value) in dictionary)`, `Index()` in LINQ | `Tuples/Program.cs` |

## Things the tests showed

- **JSON loses tuple names**: `JsonSerializer.Serialize(("Tea", 12))` as a `(string Name, int Quantity)` gives **`{}`**:
  tuple elements are *fields*, and System.Text.Json skips fields. With `IncludeFields = true` you get
  `{"Item1":"Tea","Item2":12}`: the names exist only in your source code. A record gives `{"Total":50.00,"Count":3}`.
- **`==` ignores names**: `(Width: 3, Height: 4) == (Count: 3, Total: 4)` is `true`. Only positions and values count.
- **Value tuples are mutable structs**: changing `copy.X` left the original alone (1 vs 99). `Tuple<>` properties have no setter.
- **A positional pattern uses `Deconstruct`**: `parcel switch { (> 20m, _) => "heavy" }` works on the class because it has one.
- Deconstructing a `null` object throws `NullReferenceException` (the `Deconstruct` call needs an object).

## Code snippets

**Return several values**

```csharp
public static (decimal Total, int Count, decimal Average) Summarize(IReadOnlyList<decimal> prices)
{
    decimal total = prices.Sum();
    return (total, prices.Count, Math.Round(total / prices.Count, 2));
}

var stats = Summarize(prices);          // stats.Total, stats.Count
var (total, count, _) = Summarize(prices); // deconstruct; "_" skips the average
```

**Switch on two values**

```csharp
return (country, express) switch
{
    ("US", false) => 5m,
    ("US", true) => 15m,
    ("CA" or "MX", false) => 12m,
    (_, true) => 40m,
    (_, false) => 25m,
};
```

**Swap**

```csharp
(left, right) = (right, left);
```

**Make your own class deconstructable**

```csharp
public void Deconstruct(out decimal weightKg, out string destination)
{
    weightKg = WeightKg;
    destination = Destination;
}

var (weight, destination) = parcel;
```

**Dictionary entries and LINQ**

```csharp
foreach (var (product, quantity) in stock)
{
}

var cheap = prices.Index().Where(entry => entry.Item < 20m).Select(entry => (entry.Index, Price: entry.Item));
```

## Variations

In `Tuples/Variations/`, checked by `Tuples.Tests/VariationsTest.cs`.

**"Give back a total and a count"** (`MultipleResultsVariation`)

| Way | Caller writes | The difference |
|---|---|---|
| named tuple `(decimal Total, int Count)` (original) | `result.Total`, or `var (total, count) = ...` | no extra type; names only in source code |
| `out decimal total, out int count` | `WithOut(prices, out var total, out var count)` | old style (`int.TryParse`); can't be used in `async` methods |
| `Tuple<decimal, int>` (class, .NET 4) | `result.Item1`, `result.Item2` | no names, a heap object, read-only |
| unnamed `(decimal, int)` | `result.Item1` | works, but unreadable at the call site |
| `record PriceTotalsVariation(decimal Total, int Count)` | `result.Total`, or deconstruct | a real type: survives JSON, APIs, many callers |
| anonymous `new { Total, Count }` | — | has no type name to return; fine **inside** one method (2.1's `Select`) |

**Deconstructable types**

| Type | `Deconstruct` | |
|---|---|---|
| class `ParcelModel` (original) | written by hand | — |
| `record ParcelRecordVariation(decimal WeightKg, string Destination)` | generated | same `var (weight, destination) = ...` |
| `KeyValuePair<K, V>` | built in | `foreach (var (key, value) in dictionary)` |
| `(int X, int Y)` | built in | — |

**When to use which**

| Situation | Use |
|---|---|
| a private helper returns 2–3 values | named tuple |
| the result goes into JSON, an API, or is used in many places | record |
| a `Try...` method: success + value | `bool Try...(out T value)` (the .NET convention) |
| shaping data inside one LINQ query | anonymous type or tuple |
| two inputs decide one answer | `switch` on a tuple |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| named tuple return in `Summarize` | three related numbers, one caller, no new type to maintain | a class for every small result clutters the code |
| `(decimal, decimal)?` for `Range` | "no answer" can't be confused with a real 0–0 range | returning `(0, 0)` lies about an empty list |
| `IReadOnlyList<decimal>` parameter | needs `Count` and promises not to change the list | `IEnumerable` would be read twice (2.2) |
| `decimal` | money | `double` rounding (see 1.1) |
| `ParcelModel` as a class with `Deconstruct` | shows what records generate | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| `Summarize` / `Range` | named elements and deconstruction | empty list → zeros; range `null` | `null` list throws |
| shipping switch | each rule | lower case; fall-through to `(_, true)` / `(_, false)` | blank / `null` country |
| deconstruction | class, dictionary, swap | discards; positional pattern | `null` object throws |
| multiple results | five ways, same numbers | struct vs class; record equality; anonymous only via reflection | `null` list throws every way |
| gotchas | record keeps names in JSON | tuple JSON is `{}` / `Item1`; mutable copy; `==` ignores names | `Tuple<>` can't be changed; `ValueTuple` has public fields |
