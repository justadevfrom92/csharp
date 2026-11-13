# Cheat Sheet — Section 2: Collections & LINQ

Project: [2.1](../Section2_CollectionsAndLinq/Project2.1_InventoryAnalytics)

## Which collection

| Need | Use |
|---|---|
| Fixed number of items | `T[]` |
| A growing list, in order | `List<T>` |
| Find by key, fast | `Dictionary<TKey, TValue>` |
| No duplicates / "is it in there?" | `HashSet<T>` |
| First in, first out | `Queue<T>` |
| Last in, first out (undo) | `Stack<T>` |
| Shared between threads | `ConcurrentDictionary<TKey, TValue>`, `Channel<T>` |

## Arrays and lists

```csharp
string[] colors = ["red", "green", "blue"];  // collection expression
int[] numbers = new int[10];
List<string> names = ["Ann", "Bob"];
names.Add("Cid");
names.AddRange(["Dee", "Eve"]);
names.Insert(0, "Zed");
names.Remove("Bob");
names.RemoveAt(0);
names.RemoveAll(name => name.StartsWith("D"));
bool hasAnn = names.Contains("Ann");
int index = names.IndexOf("Cid");
names.Sort();
string first = names[0];
string last = names[^1];        // index from the end
string[] middle = colors[1..3];   // range: items 1 and 2
```

## Dictionary

```csharp
var stock = new Dictionary<string, int>
{
    ["KB-01"] = 40,
    ["MS-01"] = 3,
};
stock["MN-01"] = 8;                        // add or replace
bool added = stock.TryAdd("MN-01", 1);     // false: already there
if (stock.TryGetValue("KB-01", out int quantity))
{
}
int orZero = stock.GetValueOrDefault("XX", 0);
stock.Remove("MS-01");
foreach (KeyValuePair<string, int> pair in stock)
{
    Console.WriteLine($"{pair.Key}: {pair.Value}");
}
```

## HashSet, Queue, Stack

```csharp
var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
bool isNew = tags.Add("Sale");
tags.UnionWith(["New", "Hot"]);
tags.IntersectWith(["Sale", "Hot"]);

var queue = new Queue<string>();
queue.Enqueue("first");
string next = queue.Dequeue();
bool any = queue.TryPeek(out string? peeked);

var stack = new Stack<string>();
stack.Push("step 1");
string undo = stack.Pop();
```

## LINQ: filter, sort, shape

```csharp
List<Product> cheap = products.Where(product => product.Price < 10).ToList();
List<Product> sorted = products.OrderBy(product => product.Category).ThenByDescending(product => product.Price).ToList();
List<string> names = products.Select(product => product.Name).ToList();
var small = products.Select(product => new { product.Name, product.Price }); // anonymous type
List<string> allTags = products.SelectMany(product => product.Tags).Distinct().ToList();
List<Product> page2 = products.Skip(10).Take(10).ToList();
Dictionary<int, Product> byId = products.ToDictionary(product => product.Id);
```

## LINQ: single items and yes/no

```csharp
Product firstOne = products.First();                         // throws if empty
Product? maybe = products.FirstOrDefault(product => product.Id == 5); // null if none
Product exactlyOne = products.Single(product => product.Id == 5);     // throws if 0 or 2+
bool anyCheap = products.Any(product => product.Price < 10);
bool allInStock = products.All(product => product.Stock > 0);
bool hasKeyboard = products.Select(product => product.Name).Contains("Keyboard");
```

## LINQ: totals and groups

```csharp
int count = products.Count(product => product.Stock == 0);
decimal total = products.Sum(product => product.Price * product.Stock);
decimal average = products.Average(product => product.Price);
decimal highest = products.Max(product => product.Price);
Product cheapest = products.MinBy(product => product.Price)!;

var perCategory = products
    .GroupBy(product => product.Category)
    .Select(group => new { Category = group.Key, Count = group.Count(), Value = group.Sum(product => product.Price) })
    .OrderByDescending(row => row.Value);

Dictionary<string, int> countByCategory = products.CountBy(product => product.Category).ToDictionary();
```

## LINQ: joining two lists

```csharp
var lines = orders.Join(products,
    order => order.ProductId,
    product => product.Id,
    (order, product) => new { order.Id, product.Name, order.Quantity });
```

## Deferred vs immediate

```csharp
IEnumerable<Product> query = products.Where(product => product.Stock == 0); // nothing runs yet
List<Product> snapshot = query.ToList();                                    // runs now
```

A query runs **each time** you loop over it. Call `ToList()` once if you'll use the result more than once,
or before the data source changes / goes away (a `lock`, a `DbContext`).

## Return types

```csharp
public IReadOnlyList<Product> Items { get; } // callers can read, not change
public List<Product> GetCopy()               // a fresh copy the caller owns
{
    return items.ToList();
}
public void PrintAll(IEnumerable<Product> anything) // accept arrays, lists, queries
{
}
```
