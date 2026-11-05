# Project 2.1 — In-Memory Order & Inventory Analytics System

A console warehouse engine: stock is held in a `Dictionary`, orders flow through a `Queue`,
fulfilments can be undone via a `Stack`, categories/tags are de-duplicated with `HashSet`, and
all reporting is LINQ over `IEnumerable<T>`.

## Run

```bash
dotnet run -c Release
```

Release mode matters for section 9 (the lookup benchmark): Debug builds disable JIT optimisations and distort timings.

## Structure

| File | Collection / technique |
|---|---|
| `Domain/WarehouseDomainModels.cs` | `StockKeepingUnitCode` record-struct key; `HashSet<string>` tags (case-insensitive) |
| `Inventory/WarehouseInventoryRegistry.cs` | `Dictionary<StockKeepingUnitCode, InventoryStockItem>` + `HashSet<string>` categories |
| `Orders/FirstInFirstOutOrderProcessingPipeline.cs` | `Queue<CustomerOrder>` (FIFO), `Stack<…>` undo history (LIFO), `List<T>` ledger |
| `Analytics/InventoryAndSalesLinqAnalytics.cs` | `Where`, `Select`, `SelectMany`, `GroupBy`, `Sum`, `Average`, `Max`, `MaxBy`, `OrderBy`/`ThenBy` |
| `Analytics/CollectionLookupPerformanceComparison.cs` | Measured O(n) vs O(1) lookups |
| `Program.cs` | Deferred vs immediate execution, `IEnumerable`/`ICollection`/`IList`/`IQueryable` demo |

## Architectural trade-off: why `Dictionary` instead of `List` for inventory retrieval (the required write-up)

**Access pattern.** The hot path is "given a SKU on an order line, find the stock item". Every
order line does one lookup, so cost scales with *lookups × catalog size* for a list.

| Operation | `List<InventoryStockItem>` | `Dictionary<StockKeepingUnitCode, InventoryStockItem>` |
|---|---|---|
| Find by SKU | O(n) linear scan (`Find`) | O(1) average (hash → bucket) |
| Insert | O(1) amortised append | O(1) amortised |
| Duplicate-SKU detection | O(n) scan before every insert | free: `TryAdd` returns `false` |
| Ordered iteration | yes (insertion order) | no guaranteed order |
| Memory | one array of references | buckets + entries (~2–3× the list's overhead) |

**Measured on this machine (5,000 random lookups, Release build):**

| Catalog size | List scan | Dictionary | Speed-up |
|---|---|---|---|
| 1,000 | ~40 ms | ~0.4 ms | ~90× |
| 10,000 | ~200 ms | ~1.1 ms | ~175× |
| 50,000 | ~1,330 ms | ~1.1 ms | ~1,200× |

The list cost grows linearly with catalog size, while the dictionary cost stays flat. This matches the Big-O analysis.

**Costs accepted:**
* Extra memory per entry (hash buckets). For a warehouse of tens of thousands of SKUs that is a few MB, which is negligible.
* No ordering. Every report that needs ordering states it explicitly with `OrderBy`. That is
  clearer anyway, because the sort key is a business decision, not an accident of insertion order.
* The key must have correct equality and hashing. `StockKeepingUnitCode` is a `readonly record struct`, so
  equality and `GetHashCode` are generated from its value. It normalises the code once (`Trim().ToUpperInvariant()`), so
  `"elec-001"` and `"ELEC-001"` hash to the same bucket. A raw `string` key would need a
  comparer at every call site and could be confused with a product name.

**When a List would be right instead:** small fixed collections (< ~10 items, where a scan
beats hashing), or data that is only ever iterated in order, such as the append-only sales ledger
(`List<FulfilledSaleRecord>`).

## Other collection choices

* **`Queue<CustomerOrder>`.** Orders must be fulfilled in arrival order (fairness). `Enqueue` and
  `Dequeue` are both O(1); doing the same with `List.RemoveAt(0)` is O(n) because every element shifts.
* **`Stack<IReadOnlyList<FulfilledSaleRecord>>`.** Undo must reverse the *most recent* fulfilment
  first, which is LIFO by definition.
* **`HashSet<string>` with `StringComparer.OrdinalIgnoreCase`.** Categories and tags must be
  unique regardless of casing (`"paper"`/`"PAPER"`, `"Office"`/`"office"`). `Add` returns `false` on
  duplicates in O(1), so the code needs no `Contains` check first.
* **Orders are all-or-nothing.** Every line is validated before any stock is decremented, so a
  failing line never leaves a half-fulfilled order behind.

## LINQ and execution semantics

* Analytics accept `IEnumerable<T>`, the narrowest interface that supports querying, so they work
  over `Dictionary.Values`, lists, arrays or generators alike.
* Every analytics method ends in `ToList()` (**immediate execution**). Callers get a stable
  snapshot. Returning the bare query (**deferred execution**) would re-run it on every
  enumeration and could observe inventory mutated after the report was requested. Section 7 of
  the output shows this: defining the query runs the predicate 0 times; `ToList()` runs it 8
  times; re-enumerating after an insert re-runs it and sees the new item while the snapshot does not.
* `IQueryable<T>` is shown via `AsQueryable()`. It builds an *expression tree* (printed in section 8)
  that a provider such as EF Core translates into SQL instead of executing delegates in memory.
* `ICollection<T>` adds `Count`/`Add`/`Remove`, and `IList<T>` adds an indexer. The registry exposes
  `IReadOnlyCollection<string>`, so callers cannot mutate internal state.
