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
