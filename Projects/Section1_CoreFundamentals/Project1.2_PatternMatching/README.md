# Project 1.2 — Pattern Matching: switch expressions & relational patterns

| Folder | What |
|---|---|
| `PatternMatching/` | Console demo |
| `PatternMatching.Tests/` | xUnit tests, every behaviour with all three kinds |

```bash
dotnet run --project PatternMatching
```

```bash
dotnet test PatternMatching.Tests
```

## The patterns, where each one is used

| Pattern | Looks like | Where |
|---|---|---|
| **Switch expression** | `value switch { pattern => result, ... }` | Every classifier |
| **Relational** | `< 580`, `>= 670`, `> 30` | `CreditScoreClassifier`, weights in `ShippingCostClassifier` |
| **Logical** | `and`, `or`, `not` | `>= 580 and < 670`, `< 300 or > 850`, `not "0000"` |
| **Constant** | `null`, `"US"`, `"help"` | Everywhere |
| **Discard** | `_` | "anything else" — the last arm |
| **Property** | `{ CountryCode: "US", IsExpress: true }` | `ShippingCostClassifier` |
| **Type** | `CardPaymentModel card` | `PaymentClassifier` — checks the type AND gives you a typed variable |
| **`when` guard** | `CardPaymentModel card when card.Amount > 1000` | `PaymentClassifier` |
| **List** | `[]`, `["help"]`, `["deposit", var amount]`, `[var first, ..]` | `BankCommandParser` |
| **`is` + pattern** | `payment is CardPaymentModel { Amount: > 1000 }` | `NeedsApproval`, `HasUsableCardDigits` |

## How a switch expression works

```csharp
return creditScore switch
{
    < 300 or > 850 => throw new ArgumentOutOfRangeException(...),
    < 580 => "Poor",
    < 670 => "Fair",
    _ => "Excellent",
};
```

- Arms are checked **top to bottom**; the **first** match wins. That's why `< 670` doesn't need `>= 580`:
  anything below 580 already matched the arm above. (The demo writes `>= 580 and < 670` once only to show `and`.)
- An arm can `throw` instead of returning a value.
- `_` catches everything else. If you leave out a case and nothing catches it, the compiler warns you
  ("the switch expression does not handle all possible values") — something an `if` chain never does.
- The result is a **value**, so it can go straight into `return`, a variable or a method argument.

`CreditScoreClassifier.ClassifyWithIfStatements` has the same rules written with `if`s, so you can compare.
The tests check both versions give the same answer for every edge.

## Why these choices

- **Switch expression instead of `if`/`else` chains:** the rules read like a table — condition on the left,
  result on the right — and the compiler warns about cases you forgot.
- **Property patterns instead of `parcel.CountryCode == "US" && parcel.IsExpress`:** the pattern also handles
  `null` safely (`{ ... }` never matches `null`), and several conditions stay on one readable line.
- **Type patterns instead of `if (payment is CardPaymentModel) { var card = (CardPaymentModel)payment; ... }`:**
  one step does the check and the cast, so you can't cast to the wrong type by mistake.
- **List patterns for commands:** the *shape* of the input (`["deposit", amount]`) is the rule. Wrong number
  of words falls through to the error arms automatically.
- **Order matters:** error arms (`null`, out of range, too heavy) come **first**, so no later arm ever sees bad data.

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| Credit score | every category incl. both sides of each edge (579/580, 669/670 ...) | 299, 851 (just outside 300–850) → refused | -1, `int.MinValue`, `int.MaxValue` → rejected |
| Shipping cost | US/DE, express/normal, 1 kg and 30 kg edges | 30.01 kg (over the limit) → refused | `null`, 0 kg, negative kg → rejected |
| Describe payment | card, large card, cash | cash over 10,000 → refused | `null`, amount 0, a `string` instead of a payment → rejected |
| Needs approval | card > 1000, cash > 5000 → true | exactly 1000 / 5000 → false | `null`, an `int` → false |
| Usable card digits (`not`) | "4242" → true | "0000" → false | "42", "", `null` → false |
| Bank command | help, balance, deposit 25.50, withdraw 10 | `deposit -5`, `withdraw 0`, `deposit abc` → refused | `null`, empty, unknown command, `deposit` with no amount → rejected |
