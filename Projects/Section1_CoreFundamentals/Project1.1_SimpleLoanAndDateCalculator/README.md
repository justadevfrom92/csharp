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

**Why `checked`.**
By default `int.MaxValue + 1` silently wraps to a big negative number. `checked` turns that
into an exception, so the bug is caught instead of hidden.

**Why one file.**
This is a small program. Splitting it into many classes would just make it harder to read.
The two helpers (`AskForDecimal`, `AskForDate`) exist only because they're used more than once.
