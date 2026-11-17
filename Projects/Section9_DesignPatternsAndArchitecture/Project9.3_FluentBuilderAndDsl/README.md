# Project 9.3 — Fluent Builder & DSL

```bash
cd FluentBuilderAndDsl
dotnet run
```

```bash
cd FluentBuilderAndDsl.Tests
dotnet test
```

## What it covers

| Topic | Where |
|---|---|
| **Fluent builder pattern**: build an object step by step with chained calls, check everything in `Build()` | `Builders/OrderBuilder.cs` |
| **External DSL**: a tiny text language for pricing rules, parsed from `pricing-rules.txt` | `Parsers/PricingRuleParser.cs` |
| Running the parsed rules against an order | `Evaluators/PricingRuleEvaluator.cs` |

## Fluent builder — why

```csharp
OrderModel order = new OrderBuilder()
    .ForCustomer("Bob")
    .ShipTo("CA")
    .AddLine("KB-01", 2, 25m)
    .AddLine("MN-01", 1, 180m)
    .WithNote("Leave at the door")
    .Build();
```

- **Reads like a sentence.** `new OrderModel("Bob", "CA", lines, "Leave...")` doesn't say what each value is.
- **Optional parts are just left out** (no note → no `WithNote`), instead of passing `null` in the middle of a long constructor.
- **Repeated parts** (`AddLine` many times) are natural.
- **Each step checks its own input** (`AddLine` with quantity 0 throws at once), and **`Build()` checks the
  rules that need several steps** (a customer AND at least one line).
- The result is an **immutable** `record`; `Build()` copies the lines, so changing the builder later doesn't change the order.

The trick is just `return this;` at the end of every step. You already use fluent builders all the time:
`WebApplication.CreateBuilder()`, `services.AddHttpClient(...).AddResilienceHandler(...)`,
`modelBuilder.Entity<T>().Property(...).HasMaxLength(...)`, LINQ.

**When not to:** an object with 2–3 required values just needs a constructor.

## DSL — what and why

A **DSL (Domain-Specific Language)** is a small language made for **one** job.

- **Internal DSL** — written in C# itself, using fluent methods so the code reads like the problem
  (the builder above, LINQ, EF's `modelBuilder` and the minimal API `MapGet(...).RequireAuthorization(...)`).
- **External DSL** — its **own text format** with its own parser. Examples you know: SQL, regular expressions,
  CSS, the `.proto` files in 8.9, the YAML in 7.6. This project writes one:

```text
# pricing-rules.txt
WHEN total > 100 THEN discount 10%
WHEN items >= 5 THEN discount 5%
WHEN total >= 150 THEN shipping 0
WHEN country = CA THEN shipping 4.99
```

**Why:** the people who decide prices (not programmers) can read and change the rules, and a change needs **no
rebuild or redeploy** — just a new text file. **The cost:** you now own a language: its grammar, a parser,
clear error messages (`Line 2: Unknown field 'colour'`), and tests. Only worth it for rules that change often.

Rules of this DSL: discounts **add up** (capped at 50%); the **last** matching shipping rule wins (default 9.99);
`country` can only use `=`; discounts are whole percentages 1–50.

## Code snippets

**The builder: every step returns `this`**, from `Builders/OrderBuilder.cs`

```csharp
public OrderBuilder AddLine(string sku, int quantity, decimal unitPrice)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(sku);
    if (quantity <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be more than 0.");
    }
    lines.Add(new OrderLineModel(sku, quantity, unitPrice));
    return this;
}

public OrderModel Build()
{
    if (customer == null)
    {
        throw new InvalidOperationException("Call ForCustomer(...) before Build().");
    }
    if (lines.Count == 0)
    {
        throw new InvalidOperationException("An order needs at least one line.");
    }
    return new OrderModel(customer, country, lines.ToList(), note);
}
```

**Parsing one DSL line with a regular expression**, from `Parsers/PricingRuleParser.cs`

```csharp
private static readonly Regex linePattern = new Regex(
    @"^WHEN\s+(?<field>\w+)\s*(?<operator>>=|<=|>|<|=)\s*(?<value>\S+)\s+THEN\s+(?<action>\w+)\s+(?<amount>[0-9.]+)(?<percent>%?)$",
    RegexOptions.IgnoreCase);

Match match = linePattern.Match(line);
if (!match.Success)
{
    throw new FormatException($"Line {lineNumber}: Expected 'WHEN <field> <operator> <value> THEN <action> <amount>'.");
}
string field = match.Groups["field"].Value.ToLowerInvariant();
```

**Running a rule**, from `Evaluators/PricingRuleEvaluator.cs`

```csharp
decimal actual = rule.Field == "total" ? order.Subtotal : order.ItemCount;
return rule.Operator switch
{
    ">" => actual > expected,
    ">=" => actual >= expected,
    "<" => actual < expected,
    "<=" => actual <= expected,
    _ => actual == expected,
};
```

**Load rules from a file at runtime**, from `Program.cs`

```csharp
string rulesText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "pricing-rules.txt"));
List<PricingRuleModel> rules = PricingRuleParser.Parse(rulesText);
PriceQuoteModel quote = PricingRuleEvaluator.Quote(order, rules);
```

## Variations

Other ways to build an order and write the rules, in `FluentBuilderAndDsl/Variations/`, checked by
`FluentBuilderAndDsl.Tests/VariationsTest.cs`.

**Creating an order**

| Way | Missing customer | No lines | Reusing a half-built one | The difference |
|---|---|---|---|---|
| mutable `OrderBuilder` (original) | `Build()` throws | `Build()` throws | ❌ **leaks**: two orders started from the same builder share lines (order B got A's keyboard) | steps change the builder and return `this` |
| immutable builder (`ImmutableOrderBuilderVariation`, a `record` + `with`) | `Build()` throws | `Build()` throws | ✅ each branch independent | every step returns a **new** builder |
| `required` + `init` properties (`OrderInitVariation`) | **compile error CS9035** | ✅ accepted (`Lines = []`) | — | the compiler checks presence before the program runs, but rules across fields aren't checked |

**Writing the pricing rules** (same order, same quote from both)

| Way | Example | A typo is found | Change a rule |
|---|---|---|---|
| **external DSL** — text file + parser (original) | `WHEN total > 100 THEN discount 10%` | at **runtime**, when the file is read: `FormatException` with the line number | edit the text file, no rebuild |
| **internal DSL** — fluent C# (`FluentRuleVariation`) | `When(o => o.Subtotal > 100).Discount(10)` | at **compile time** (`o.Subtotl` doesn't compile) | change code, rebuild, redeploy; any C# condition possible |

## Why these types

| Choice | Why | Why not the alternative |
|---|---|---|
| `OrderBuilder` is a `class` | It **changes** as steps are called. | — |
| `OrderModel` is a `record` with `IReadOnlyList<OrderLineModel>` | The finished order never changes; callers can read lines but not add. | A mutable class would make the builder pointless. |
| `Subtotal` / `ItemCount` calculated properties | Always match the lines; nothing to keep in sync. | Stored values could get out of date. |
| `PricingRuleModel` `record` | One parsed rule = plain data; `==` compares all fields (handy in tests). | — |
| `Regex` stored in a `static readonly` field | Built once, reused for every line. | Creating it per line repeats the work. |
| `FormatException` with the line number | The standard "text in the wrong format" exception; the line number makes it fixable. | A generic exception with no position. |
| `static class` parser and evaluator | No state; input in, result out. | — |

## Tests

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Builder | chained steps build the order; left-out steps use defaults | missing customer / no lines refused at `Build()`; changing the builder later doesn't change the built order | quantity 0/negative, price 0, empty/null customer, 3-letter country, blank SKU |
| Parse | comments + blank lines skipped, case ignored | 6 well-formed lines that break a rule → `Line 2: ...` with the reason | gibberish lines, `null` |
| Quote | discounts add up, shipping set | nothing matches → full price + default shipping; 80% capped at 50% | `null` order or rules |
