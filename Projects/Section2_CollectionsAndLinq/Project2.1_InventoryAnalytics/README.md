# Project 2.1 — Warehouse Inventory & Orders

One console app, one `Program.cs`.

```bash
dotnet run
```

## What it covers

| Collection / feature | Used for |
|---|---|
| `Dictionary<string, Product>` | Stock, looked up by product code |
| `HashSet<string>` | Unique category names (case-insensitive) |
| `Queue<Order>` | Orders handled in the order they arrived |
| `Stack<Order>` | "Undo last order" |
| `List<Sale>` | Completed sales |
| LINQ `Where`, `Select`, `Sum`, `Average`, `Min`, `Max`, `GroupBy`, `OrderBy` | Reports |
| Deferred vs `ToList()` | Part 5 shows the difference live |

## Why these choices

**Why a `Dictionary` and not a `List` for stock.**
Every order asks "do we have product X?". A `List` answers by checking items one by one
(O(n)) — 100,000 products means up to 100,000 checks per order. A `Dictionary` uses the
product code as a key and jumps straight to the item (O(1)), no matter how many products
there are. Part 6 measures it: the List is hundreds of times slower.
The cost: a `Dictionary` uses a bit more memory and has no fixed order. For stock we never
care about order, and we look things up constantly, so the trade is worth it.

**Why a `Queue` for orders.** Orders must be handled fairly: first come, first served.
`Queue` does exactly that and nothing else.

**Why a `Stack` for undo.** Undo always cancels the *latest* thing first. That's what a stack does.

**Why a `HashSet` for categories.** It silently ignores duplicates, so "Computers" and
"computers" can't both end up in the list. Checking "is this in the set?" is also O(1).

**Why `ToList()` sometimes.** A LINQ query doesn't run until you loop over it, and it
re-runs every time. `ToList()` runs it once and keeps the result. Use it when you need a
snapshot or will loop over the result many times.

**Why `Product` is a class but `Order` is a record.** Product stock changes, so it needs a
settable property. Orders and sales never change after they're made, so a one-line `record` is enough.

## Code snippets

The key lines from `Program.cs`.

**`Dictionary`: find by key instantly, `TryGetValue` for "might not exist"**

```csharp
var stockByProductCode = new Dictionary<string, Product>
{
    ["KB-01"] = new Product("KB-01", "Keyboard", "Computers", 25.00m, 40),
    ["MS-01"] = new Product("MS-01", "Mouse", "Computers", 12.50m, 3),
};

if (!stockByProductCode.TryGetValue(order.ProductCode, out Product? product))
{
    Console.WriteLine($"Order {order.OrderNumber}: REJECTED - unknown product {order.ProductCode}");
}
```

**`HashSet`: no duplicates (here ignoring upper/lower case)**

```csharp
var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
categories.Add("Computers");
bool addedAgain = categories.Add("computers"); // false: already there
```

**`Queue` (first in, first out) and `Stack` (last in, first out)**

```csharp
var waitingOrders = new Queue<Order>();
waitingOrders.Enqueue(new Order(1, "KB-01", 5));
Order order = waitingOrders.Dequeue();

var undoHistory = new Stack<Order>();
undoHistory.Push(order);
Order lastOrder = undoHistory.Pop(); // undo the most recent one
```

**LINQ: `Sum`, `Where`, `OrderBy`, `GroupBy` + `Select`**

```csharp
decimal totalStockValue = allProducts.Sum(product => product.Price * product.QuantityInStock);

var valueByCategory = allProducts
    .GroupBy(product => product.Category)
    .Select(group => new { Category = group.Key, Value = group.Sum(product => product.Price * product.QuantityInStock) })
    .OrderByDescending(row => row.Value);

foreach (Product product in allProducts.Where(product => product.QuantityInStock <= 5).OrderBy(product => product.QuantityInStock))
{
    Console.WriteLine($"{product.Name} {product.QuantityInStock} left");
}
```

**Deferred vs immediate: a LINQ query runs when you loop over it, `ToList()` runs it now**

```csharp
IEnumerable<Product> outOfStockLater = allProducts.Where(product => product.QuantityInStock == 0); // nothing runs yet
List<Product> outOfStockNow = allProducts.Where(product => product.QuantityInStock == 0).ToList(); // runs right now

stockByProductCode["CH-01"].QuantityInStock = 0;
outOfStockLater.Count(); // sees the change
outOfStockNow.Count;     // does not
```

**Timing code with `Stopwatch`**

```csharp
var timer = Stopwatch.StartNew();
list.Contains(lastCode);              // O(n): checks every item
long listTime = timer.ElapsedMilliseconds;
timer.Restart();
dictionary.ContainsKey(lastCode);     // O(1): jumps straight to it
```

## Variations

Other ways to write the same code, in `Variations/` (they compile; the results below were checked by running them).

| Job | Variation | Same result? | The difference |
|---|---|---|---|
| find by code | `DictionaryLookupVariation.WithTryGetValue` (original) | — | one lookup, `null` if missing |
| | `WithContainsKey` + `stock[code]` | ✅ | looks the key up **twice** |
| | `WithGetValueOrDefault` | ✅ | shortest |
| | `WithIndexer` (`stock[code]` only) | ❌ | missing key → `KeyNotFoundException` |
| | `WithListSearch` (`List.FirstOrDefault`) | ✅ | checks items one by one: O(n) instead of O(1) |
| low-stock names, sorted | `LinqStyleVariation.WithMethodSyntax` (original) | — | `Where().OrderBy().Select()` |
| | `WithQuerySyntax` (`from ... where ... orderby ... select`) | ✅ | SQL-like; the compiler turns it into the method calls |
| | `WithLoopAndSort` (`foreach` + `List.Sort`) | ✅ without ties, ❌ **with ties** | `OrderBy` is **stable** (equal items keep their order); `List.Sort` is **not**: 20 products with equal stock came out `Delta, Alpha, Tie17, Tie16...` instead of `Delta, Alpha, Beta, Gamma...` |
| value per category | `ValueByCategoryWithGroupBy` / `WithToLookup` | ✅ | `GroupBy` is **deferred** (re-runs every time you loop); `ToLookup` groups **once, now** |
| total value | `TotalWithSum` / `TotalWithAggregate` | ✅ | `Aggregate` is the general "fold" that `Sum`, `Max`... are special cases of |
| one item | `SingleItemVariation.First` / `FirstOrDefault` / `Single` / `SingleOrDefault` | ❌ on 0 or 2+ matches | see below |
| "any out of stock?" | `HasAnyWithAny` / `HasAnyWithCount` (`Count() > 0`) | ✅ | `Any` stops at the first match; `Count` counts everything |
| first in, first out | `CollectionTypeVariation.DrainWithQueue` / `DrainWithListRemoveAt` | ✅ | `List.RemoveAt(0)` shifts every remaining item |
| unique values | `UniqueWithHashSet` / `UniqueWithDistinct` | ✅ here | `Distinct` keeps first-seen order; a `HashSet`'s order is **not guaranteed** |
| "read-only" list | `AsReadOnlyView` (`AsReadOnly()`) | ❌ | a **view**: later changes to the original show through (count went 1 → 3) |
| | `AsReadOnlyInterface` (`IReadOnlyList` cast) | ❌ | only a promise: the caller cast it back to `List` and added an item |
| | `AsImmutableCopy` (`ToImmutableList`) / `AsFrozenSet` (`ToFrozenSet`) | ❌ | real copies that never change (count stayed 1); frozen = built for fast lookups |

**One item — the four look-alikes** (checked with 0, 1 and 2 matches):

| | 0 matches ("Toys") | 1 match ("Lighting") | 2 matches ("Furniture") |
|---|---|---|---|
| `First` | `InvalidOperationException` | Lamp | Desk (the first) |
| `FirstOrDefault` | `null` | Lamp | Desk |
| `Single` | `InvalidOperationException` | Lamp | `InvalidOperationException` |
| `SingleOrDefault` | `null` | Lamp | `InvalidOperationException` |

Use `Single` when 2 matches would be a **bug** (an id); `First` when you just want one of them.

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `Dictionary<string, Product>` for stock | Every order asks "find product by code" — O(1). | `List<Product>` checks items one by one (O(n)); Part 6 measures it. |
| `HashSet<string>(StringComparer.OrdinalIgnoreCase)` | Categories must be unique; "Computers" and "computers" are the same. | `List` allows duplicates; the default comparer is case-sensitive. |
| `Queue<Order>` | Orders are handled in arrival order. | `List` + `RemoveAt(0)` shifts every item each time (slow) and hides the intent. |
| `Stack<Order>` | Undo cancels the *latest* order first. | — |
| `List<Sale>` | Grows as orders complete; we loop over it and remove the last one. | Array can't grow. |
| `IEnumerable<Product> allProducts` | The reports only *loop* over products; they don't need lookup or indexing. Also lets Part 5 show a lazy query. | Keeping the `Dictionary` type would invite lookups the reports don't need. |
| `IEnumerable` vs `List` in Part 5 | The *point* of Part 5: `IEnumerable` from `Where` is lazy and re-runs; `ToList()` is a snapshot. | — |
| `class Product` | `QuantityInStock` changes, so it needs a setter; one product is one object everyone shares. | A `record` is meant to stay unchanged; changing stock would mean replacing it in the dictionary each time. |
| `record Order`, `record Sale` | Created once, never changed. One line each, with `ToString` and equality for free. | A `class` would need ~10 lines of constructor and properties for the same thing. |
| `decimal` prices, `long` timings | Money exact; `Stopwatch.ElapsedMilliseconds` is a `long`. | — |
| `var` for the `GroupBy`/`Select` result | `new { Category, Value }` is an *anonymous type* — it has no name you could write. | A named record would be fine too, but it's used in one loop only. |
| `const int itemCount` | Fixed value known at compile time. | — |
