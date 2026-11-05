# Project 1.1 — Console Financial & Date Processing Utility

An interactive CLI that computes loan amortization schedules, parses dates typed in several
cultures, converts instants across time zones and prints formatted receipts.

## Run

```bash
dotnet run                 # interactive
dotnet run -- --demo       # scripted, unattended run of every workflow (used as the execution gate)
PREFERRED_DATE_CULTURE=en-GB dotnet run   # change how ambiguous dates like 05/03/2027 are read
```

## Structure

| Folder / file | Responsibility |
|---|---|
| `Validation/BoundedNumericInputValidator.cs` | Parses text → `decimal`/`int`, rejects values outside type limits and business bounds. |
| `Calculation/LoanAmortizationScheduleCalculator.cs` | Pure `decimal` annuity math and the month-by-month schedule. |
| `DateTimeProcessing/MultiCultureDateInputParser.cs` | ISO first, then preferred culture, then fallback cultures → `DateOnly`. |
| `DateTimeProcessing/TimeZoneMeetingTimeConverter.cs` | Converts one `DateTimeOffset` to many IANA zones (`DateOnly`/`TimeOnly`/DST flag). |
| `Formatting/PaymentReceiptTextFormatter.cs` | `StringBuilder` receipt with culture-specific currency and dates. |
| `Diagnostics/NumericTypeLimitsReporter.cs` | `MinValue`/`MaxValue` table, `checked` vs `unchecked`, double vs decimal drift. |
| `ConsoleLineInputSource.cs` | Interactive vs scripted input, so `--demo` runs the real code paths. |

## Design justification

### Why `decimal` and not `double` for money (the required write-up)

* **Base-10 vs base-2 representation.** `double` is an IEEE-754 binary fraction. Values such as
  `0.1` and `0.01` have no exact binary form, so `0.1 + 0.2` is `0.30000000000000004`. `decimal`
  stores a 96-bit integer and a power-of-ten scale, so every amount written in cents is exact.
  The `4` menu option prints this live: adding `0.01` one million times yields
  `10000.000000171856` as `double` and exactly `10000.00` as `decimal`.
* **Errors compound in amortization.** Each month's interest is calculated from the previous
  month's balance. With `double`, a representation error in month 1 feeds into month 2, and so
  on for 360 periods. With `decimal`, each period is rounded to the cent once (`MidpointRounding.AwayFromZero`,
  the rule consumers expect) and the final payment absorbs the leftover pennies so the
  balance ends at exactly `0.00`. Ledgers must reconcile to the cent; that is only achievable deterministically in base 10.
* **Range is sufficient.** `decimal` tops out at ~7.9 × 10²⁸ with 28–29 significant digits —
  far beyond the 10,000,000 principal cap. `double`'s larger range (10³⁰⁸) buys nothing for money
  but costs exactness.
* **Trade-off accepted.** `decimal` arithmetic is roughly 10–20× slower than `double` and has
  no `Math.Pow`. The calculator therefore implements `RaiseDecimalToNonNegativeIntegerPower`
  (exponentiation by squaring, O(log n) multiplications). For a 480-row schedule the cost is microseconds; correctness wins.
* **`checked` arithmetic in the schedule loop.** An overflow in a money calculation must never
  wrap around silently, so the loop body is `checked` — it would throw `OverflowException` instead of producing a garbage balance.

### Input validation

* Validators return a `NumericInputValidationResult<T>` record struct rather than throwing,
  because invalid input is an *expected* event in an interactive tool, not an exceptional one.
* Integers are parsed as `long` first so that `3000000000` is reported as "overflows a 32-bit
  integer" instead of the misleading "not a number".
* Business bounds (`100 – 10,000,000`, `1 – 480` months) are expressed with relational
  patterns (`< min`, `> max`) in `switch` expressions, keeping the rules declarative.

### Nullable handling

* `Console.ReadLine()` returns `string?` — `null` at end-of-stream. Every consumer uses `?.` and
  `??` (`rawInput?.Trim() ?? string.Empty`) so piping a short file into the tool ends the session
  cleanly instead of throwing `NullReferenceException`.
* The prompt helper returns `TNumber?` (`Nullable<T>`); `null` means "user gave up after three
  attempts", which is a distinct state from any valid number, including `0`.
* `??=` is used to apply the default preferred culture only when no override was configured.
* An empty customer name becomes `null` and the receipt shows `customerDisplayName ?? "Valued Customer"`.

### Dates and times

* `DateOnly` for due dates: a payment is due on a calendar day, not an instant; using `DateTime`
  would drag a meaningless midnight time and `Kind` along.
* `DateTimeOffset` for instants: it carries its offset, so it cannot be misread as local time.
* ISO-8601 is tried first because it is unambiguous; the *order* of fallback cultures is the
  disambiguation policy for strings like `05/03/2027` (May 3 in en-US, 5 March in en-GB).
* `InvariantGlobalization` is explicitly `false` so ICU culture data is available on Linux.

### Formatting

* `StringBuilder` with a pre-sized capacity builds the receipt in one buffer instead of
  allocating hundreds of intermediate strings.
* `string.Create(culture, $"...")` formats each interpolated line with the receipt culture
  (e.g. `£` and `03/05/2027` for en-GB) without touching the thread's global `CurrentCulture`.
