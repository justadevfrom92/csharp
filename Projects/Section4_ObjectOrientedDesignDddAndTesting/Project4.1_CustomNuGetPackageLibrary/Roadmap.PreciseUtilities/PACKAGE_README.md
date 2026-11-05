# Roadmap.PreciseUtilities

```csharp
using Roadmap.PreciseUtilities.Text;
using Roadmap.PreciseUtilities.Validation;
using Roadmap.PreciseUtilities.Guards;

UrlSlugGenerator.GenerateUrlSafeSlug("Crème Brûlée & Café!");        // "creme-brulee-cafe"
"A long sentence".TruncateWithEllipsis(maximumLength: 10);          // "A long se…"
PaymentCardNumberLuhnChecksumValidator.IsValidLuhnChecksum("4539 1488 0343 6467"); // true
ArgumentRangeGuard.ThrowIfOutsideInclusiveRange(quantity, 1, 100);
```
