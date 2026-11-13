# Cheat Sheet — Section 1: Core Fundamentals

Projects: [1.1](../Section1_CoreFundamentals/Project1.1_SimpleLoanAndDateCalculator), [1.2](../Section1_CoreFundamentals/Project1.2_PatternMatching)

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
