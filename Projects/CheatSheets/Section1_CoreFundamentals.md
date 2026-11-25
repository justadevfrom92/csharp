# Cheat Sheet — Section 1: Core Fundamentals

Projects: [1.1](../Section1_CoreFundamentals/Project1.1_SimpleLoanAndDateCalculator), [1.2](../Section1_CoreFundamentals/Project1.2_PatternMatching), [1.3](../Section1_CoreFundamentals/Project1.3_TuplesAndDeconstruction), [1.4](../Section1_CoreFundamentals/Project1.4_RecordsSealedAndExtensions)

## Variables and types

```csharp
int count = 5;                 // whole numbers, ±2.1 billion
long bytes = 5_000_000_000L;   // bigger whole numbers
decimal price = 19.99m;        // money (exact)
double ratio = 0.75;           // measurements (tiny rounding errors)
bool isDone = false;
char letter = 'A';
string name = "Alice";
var list = new List<string>(); // var: type is obvious from the right side
const int MaxItems = 100;      // fixed at compile time
```

## Converting and parsing

```csharp
int number = int.Parse("42");                     // throws on bad input
bool ok = int.TryParse("abc", out int parsed);    // false, no exception
decimal amount = decimal.Parse("19.99", CultureInfo.InvariantCulture);
int rounded = (int)Math.Round(7.6);               // 8
string text = 42.ToString();
```

## Overflow: `checked` / `unchecked`

```csharp
int big = int.MaxValue;
int wrapped = unchecked(big + 1); // -2147483648, silently
int safe = checked(big + 1);      // throws OverflowException
```

## Null handling

```csharp
string? maybe = null;          // ? = "can be null"
int length = maybe?.Length ?? 0; // ?. stops at null, ?? gives a default
maybe ??= "default";           // assign only if null
int? optionalNumber = null;    // nullable value type
ArgumentNullException.ThrowIfNull(maybe);
ArgumentException.ThrowIfNullOrWhiteSpace(maybe);
```

## Strings

```csharp
string greeting = $"Hello {name}, you have {count} items"; // interpolation
string money = $"{price:C}";          // $19.99 (current culture)
string twoDecimals = $"{ratio:0.00}"; // 0.75
string padded = $"{name,-10}|";       // left-align in 10 characters
string path = @"C:\files\new";        // @ = backslashes are literal
string raw = """
    He said "hi"
    """;                              // raw string literal
bool hasA = name.Contains("A", StringComparison.OrdinalIgnoreCase);
string[] parts = "a,b,c".Split(',');
string joined = string.Join(", ", parts);
string trimmed = "  x  ".Trim();
var builder = new StringBuilder();    // many appends in a loop
builder.Append("a").AppendLine("b");
```

## Dates and times

```csharp
DateOnly day = new DateOnly(2027, 1, 15);
TimeOnly time = new TimeOnly(10, 30);
DateTimeOffset now = DateTimeOffset.UtcNow;      // an exact moment
DateTime utcNow = DateTime.UtcNow;               // for database columns
TimeSpan twentyMinutes = TimeSpan.FromMinutes(20);
DateOnly nextMonth = day.AddMonths(1);
string iso = day.ToString("yyyy-MM-dd");
bool okDate = DateOnly.TryParseExact("2027-01-15", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedDay);
DateTimeOffset tokyo = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"));
```

## Culture (how numbers and dates are written)

```csharp
CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
string germanPrice = 1234.5m.ToString("N2", german); // 1.234,50
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US"); // for the whole app
```

## Control flow

```csharp
if (count > 0)
{
}
else if (count == 0)
{
}
else
{
}

for (int i = 0; i < 10; i++)
{
}

foreach (string item in list)
{
}

while (count > 0)
{
    count--;
}

string size = count > 10 ? "big" : "small"; // ternary
```

## Switch expressions and patterns

```csharp
string grade = score switch
{
    < 0 or > 100 => throw new ArgumentOutOfRangeException(nameof(score)),
    >= 90 => "A",
    >= 80 and < 90 => "B",
    _ => "C",
};

string label = shape switch
{
    Circle { Radius: > 10 } => "big circle",       // type + property pattern
    Circle circle => $"circle {circle.Radius}",
    Square square when square.Side == 0 => "dot",   // when guard
    null => "nothing",
    _ => "unknown",
};

bool isLargeCard = payment is CardPayment { Amount: > 1000 };
bool isValid = code is { Length: 4 } and not "0000";

string command = words switch
{
    [] => "empty",
    ["help"] => "help",
    ["add", var amount] => $"add {amount}",
    [var first, ..] => $"unknown {first}",          // list pattern, .. = any number
};
```

## Methods

```csharp
public static decimal AddTax(decimal amount, decimal ratePercent = 20m)
{
    return amount * (1 + ratePercent / 100);
}

decimal withTax = AddTax(100m);                  // optional parameter
decimal custom = AddTax(100m, ratePercent: 5m);  // named argument

public static bool TryDivide(int a, int b, out int result)
{
    if (b == 0)
    {
        result = 0;
        return false;
    }
    result = a / b;
    return true;
}
```

## Exceptions

```csharp
try
{
    int value = int.Parse(input);
}
catch (FormatException exception)
{
    Console.WriteLine(exception.Message);
}
catch (Exception exception) when (exception is OverflowException or ArgumentNullException)
{
}
finally
{
    // always runs
}

throw new InvalidOperationException("Can't do that now.");
```

## Tuples and deconstruction — 1.3

```csharp
(decimal Total, int Count) Summarize(List<decimal> prices)   // return several values
{
    return (prices.Sum(), prices.Count);
}

var result = Summarize(prices);        // result.Total, result.Count
var (total, count) = Summarize(prices); // deconstruct
var (_, onlyCount) = Summarize(prices); // "_" discards a value
(left, right) = (right, left);          // swap

foreach (var (key, value) in dictionary) { }

decimal cost = (country, express) switch // switch on two values
{
    ("US", false) => 5m,
    (_, true) => 40m,
    _ => 25m,
};

public void Deconstruct(out int x, out int y) { x = X; y = Y; } // make a class deconstructable
```

| Need | Use |
|---|---|
| a private method returns 2–3 values | named tuple |
| the result goes into JSON / an API | a `record` (tuple names become `{}` or `Item1` in JSON) |
| success + value | `bool TryX(out T value)` |

## `with`, sealed families, extension blocks — 1.4

```csharp
var p2 = p1 with { Name = "Filip Ekberg" };        // copy, change one property; p1 unchanged
var p3 = p1 with { Name = "Sam", Age = 41 };       // several
// shallow: a List inside is SHARED by both copies -> with { Orders = [.. p1.Orders] }

public abstract record TransportationModel;        // can't be created itself
public sealed record PlaneModel(string FlightNumber, TimeOnly ArrivalTime) : TransportationModel; // no sub-kinds

public static class TransportationExtensions
{
    extension(TransportationModel transport)       // .NET 10: methods AND properties
    {
        public string ToLabel()
        {
            ArgumentNullException.ThrowIfNull(transport); // extensions can be called on null
            return transport switch { PlaneModel plane => $"flight {plane.FlightNumber}", _ => "other" };
        }
    }

    extension(TransportationModel)                 // static members: TransportationModel.ByCar(...)
    {
        public static TransportationModel ByCar(TimeOnly at) { return new CarModel(at); }
    }
}
```

| Trap | Fix |
|---|---|
| a constructor-only check is skipped by `with` | check in `init => field = ...` |
| `{ init => check } = Name;` skips the check in the constructor | assign `Name = name;` in a normal constructor |
| `_ => throw` needed although every kind is handled (CS8509) | expected with `abstract`; future `closed` would remove it |

## Records, classes, enums

```csharp
public record Point(int X, int Y);               // immutable data, equal by value
Point moved = new Point(1, 2) with { X = 5 };    // copy with one change

public enum OrderStatus
{
    Pending,
    Shipped,
    Delivered,
}
```
