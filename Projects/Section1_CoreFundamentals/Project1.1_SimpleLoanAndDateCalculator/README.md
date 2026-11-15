# Project 1.1 — Loan & Date Calculator

One console app, one `Program.cs`. It asks for a loan, prints a month-by-month payment plan
as a receipt, and shows dates/times in different cultures and time zones.

## Run

```bash
dotnet run              # type your own answers
dotnet run -- --demo    # uses ready-made answers (some wrong on purpose to show validation)
```

## What it covers

| Topic | Where in `Program.cs` |
|---|---|
| `int` / `double` / `decimal` min and max | Part 1 |
| `checked` vs `unchecked` overflow | Part 1 |
| Rejecting input outside min/max | `AskForDecimal` |
| Reading dates typed in different cultures | `AskForDate` |
| `?.`, `??`, `??=`, `int?` | Part 2 (customer name, grace months) |
| `for`, `foreach`, `while` loops | Parts 3–6 and helpers |
| `StringBuilder` + string formatting | Part 4 (receipt) |
| `DateOnly`, `TimeOnly`, `DateTimeOffset`, `TimeSpan` | Parts 4–6 |

## Why these choices

**Why `decimal` for money, not `double`.**
`double` stores numbers in base 2, so simple amounts like `0.1` can't be stored exactly.
The app prints `0.1 + 0.2` both ways: `double` gives `0.30000000000000004`, `decimal` gives `0.3`.
Over many months those tiny errors add up and cents go missing. `decimal` stores base-10
digits exactly, which is how money works. It's slower than `double`, but for a loan
calculator that doesn't matter at all. Correct cents matter more.

**Why the last payment is different.**
Each monthly payment is rounded to cents. That leaves a few cents over or short at the end,
so the last month simply pays whatever is left. The balance always ends at exactly `0.00`.

**Why ISO date first, then cultures.**
`15/01/2027` only makes sense in UK format, but `05/03/2027` means May 3rd in the US and
March 5th in the UK. Trying `yyyy-MM-dd` first gives people a way to type a date that can't
be misread. The app prints which culture it used, so you can see if it guessed wrong.

**Why `DateTimeOffset` for the time zone part.**
`DateTime` can lose track of which time zone it's in. `DateTimeOffset` always carries its
offset (like `-05:00`), so converting to another zone is always correct.

**Why `checked` and `unchecked`.**
An `int` has 32 bits, so its biggest value is `int.MaxValue` = 2,147,483,647. Adding 1 doesn't fit.
C# has two ways to handle that:

| Keyword | What happens on `int.MaxValue + 1` | When to use it |
|---|---|---|
| `unchecked` | Extra bit thrown away, wraps to `-2,147,483,648`. No error. | When wrapping is the *point*, e.g. hash codes or checksums. |
| `checked` | Throws `OverflowException`. | When a wrong number is dangerous: money, quantities, file sizes, array lengths. |
| *(neither)* | Same as `unchecked`, unless the project turns on `<CheckForOverflowUnderflow>`. | Everyday code. |

The demo writes `unchecked` even though it's the default, just so you can see both side by side.
The lesson: by default an overflow gives you a **silently wrong answer**. A loan calculator that
quietly turns a huge total into a negative number is worse than one that crashes, so wrap risky
arithmetic in `checked`. (`decimal` always throws on overflow, with or without `checked` —
another reason it's used for money here.)

**Why one file.**
This is a small program. Splitting it into many classes would just make it harder to read.
The two helpers (`AskForDecimal`, `AskForDate`) exist only because they're used more than once.

## Code snippets

The key lines from `Program.cs`, so you can see the important ideas without reading the whole file.

**`checked` vs `unchecked`: what happens when an `int` goes past its maximum**

```csharp
int biggestInt = int.MaxValue;
Console.WriteLine(unchecked(biggestInt + 1)); // -2147483648: wraps around silently

try
{
    Console.WriteLine(checked(biggestInt + 1));
}
catch (OverflowException)
{
    Console.WriteLine("checked caught the problem");
}
```

**`decimal` for money, `double` for measurements**

```csharp
Console.WriteLine($"{0.1 + 0.2:R}"); // 0.30000000000000004
Console.WriteLine(0.1m + 0.2m);      // 0.3
```

**Null handling: `?.`, `??=` and `??`**

```csharp
string? customerName = ReadAnswer("Your name (optional): ")?.Trim(); // Trim() only if not null
customerName ??= "Customer";                                          // default only if still null

int? extraMonthsOfGrace = null;
Console.WriteLine(extraMonthsOfGrace ?? 0);                           // 0
```

**Reading a number safely with `TryParse` (no exception on bad input)**

```csharp
if (!decimal.TryParse(answer, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
{
    Console.WriteLine("  Not a number, try again.");
}
```

**Dates: `DateOnly`, `TimeOnly`, `DateTimeOffset`, time zones**

```csharp
DateOnly paymentDate = firstPaymentDate.AddMonths(monthNumber - 1);

DateTimeOffset receiptPrintedAt = DateTimeOffset.UtcNow;
DateTimeOffset localTime = TimeZoneInfo.ConvertTime(receiptPrintedAt, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"));
TimeOnly localClock = TimeOnly.FromDateTime(localTime.DateTime);
```

**The same value written for different countries (`CultureInfo`)**

```csharp
CultureInfo culture = CultureInfo.GetCultureInfo("de-DE");
Console.WriteLine(firstPaymentDate.ToString("d", culture)); // 15.01.2027
Console.WriteLine(monthlyPayment.ToString("N2", culture));  // 1.234,56
```

**Building a long text with `StringBuilder` and aligned columns**

```csharp
var receipt = new StringBuilder();
receipt.AppendLine($"{"#",4} {"Date",-12} {"Payment",12}"); // ,4 = right-align in 4 chars, ,-12 = left-align
receipt.AppendLine($"{monthNumber,4} {paymentDate:yyyy-MM-dd}   {paymentThisMonth,12:N2}");
Console.Write(receipt.ToString());
```

## Variations

Other ways to write the same code, in `Variations/` (they compile; the results below were checked by running them).

| Job | Variation (`Variations/...`) | Same result as the original? | The difference |
|---|---|---|---|
| "name or 'Customer'" | `NullHandlingVariation.WithNullCoalescing` (`??`, original style) | — | shortest |
| | `WithIsNull` (`is null`) | ✅ | can't be fooled by an overloaded `==` operator |
| | `WithEqualsNull` (`== null`) | ✅ for `string` | for your own class, an overloaded `==` (9.1) runs and could answer differently |
| | `WithIsNotNullTernary` | ✅ | positive-first reading |
| | `WithIsNullOrWhiteSpace` | ❌ | `"   "` → `Customer` too (the others keep `"   "`) |
| text → number | `NumberParsingVariation.WithTryParseInvariant` (original) | — | bad text → `null`, no exception |
| | `WithParse` | ✅ for good text | `"abc"` → `FormatException` |
| | `WithConvert` (`Convert.ToDecimal`) | ❌ | `null` → **0** (hides a missing value); `"abc"` → `FormatException` |
| | `WithTryParseCulture("1,5", "de-DE")` | ❌ | **1.5** in Germany, but **15** with InvariantCulture (comma = thousands separator) |
| | `WithTryParseCurrentCulture` | depends on the PC | uses the computer's settings: same program, different answers on different machines |
| `int.MaxValue + 1` | `OverflowVariation.Default` / `Unchecked` | ✅ (same as each other) | `-2147483648`, silently |
| | `Checked` / `CheckedBlock` | ❌ | `OverflowException`; a `checked { }` block covers every line inside |
| | `WithLong` / `WithBigInteger` | ❌ | `2147483648` — a bigger type, so no overflow |
| | `<CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>` in the `.csproj` | — | makes **everything** checked by default (no code change) |
| "Ann owes 1,234.50" | `StringBuildingVariation`: `+`, `$"..."`, `string.Create(culture, $"...")`, `string.Format`, `string.Concat`, `StringBuilder`, `string.Join` | ✅ all 7 identical | readability and speed: `$"..."` reads best; `StringBuilder` for many pieces / loops; `string.Format` is the older style |
| monthly payment | `LoanFormulaVariation.WithDecimalLoop` (original) / `WithDoublePow` (`Math.Pow`) | ✅ in a 200,000-loan check | `double` can't store most decimal amounts exactly; once unrounded doubles are added up, cents drift. Use `decimal` for money |
| a date | `DateVariation.AsDateOnly` (original) / `AsDateTime` / `AsDateTimeOffset` | same day | `DateTime` adds a time and a `Kind`; `DateTimeOffset` is a moment, not a day |
| | `WithTryParseExact("01/02/2027")` | ❌ | rejected: only `yyyy-MM-dd` accepted (no guessing) |
| | `WithTryParseCulture("01/02/2027", ...)` | ❌ | **2 January** in `en-US`, **1 February** in `en-GB` — same text, different day |

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `decimal` for amount, rate, payment | Money must be exact to the cent. | `double` can't store `0.1` exactly; errors add up over 480 months. |
| `int` for number of months | Whole number, 1–480. | `decimal` months makes no sense; it's read with the shared `AskForDecimal` and cast with `(int)`. **Shortcut:** `12.5` passes the range check and the cast cuts it to `12`. A stricter app would reject non-whole numbers with its own `int.TryParse` loop. |
| `double` only in Part 1 | Only there to *show* why it's wrong for money. | — |
| `checked` / `unchecked` | Show the two ways C# handles `int` overflow (see table above). | Leaving both out hides that the default is "silently wrong". |
| `DateOnly` for payment dates | A payment is due on a *day*; there is no time. | `DateTime` would carry a fake `00:00` and a time-zone kind that means nothing here. |
| `DateTimeOffset` for "receipt printed at" | An exact moment; converting it to Tokyo or New York is always correct because it carries its offset. | `DateTime` can lose track of whether it's local or UTC. |
| `TimeOnly`, `TimeSpan` | Clock time shown per time zone; "days until first payment" is a length of time. | — |
| `string?` from `ReadAnswer` | `Console.ReadLine()` returns `null` when input ends. The `?` forces us to handle that. | Plain `string` would hide a possible crash. |
| `int? extraMonthsOfGrace` | Shows `Nullable<int>`: "no value" is different from 0. | — |
| `Queue<string>?` for demo answers | Answers are used in order, first one first — exactly a queue. `null` when not in demo mode. | `List` + index counter is more code doing the same thing. |
| `string[]` for cultures / time zones | Fixed list that never changes size. | `List<string>` suggests it can grow. |
| `StringBuilder` for the receipt | Strings can't change; `+=` in a loop creates a new string every time. `StringBuilder` adds to one buffer. | `+=` is fine for 3 lines, wasteful for a 480-row table. |
| Local functions, no classes | Small program; helpers used more than once. | Classes would add files without adding clarity. |
