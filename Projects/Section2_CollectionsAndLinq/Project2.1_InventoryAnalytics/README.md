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
