using PatternMatching.Models;

namespace PatternMatching.Variations;

// A CHILD record of CardPaymentModel. Used to show how type checks behave with inheritance:
//   "is CardPaymentModel" -> true (a gift card IS a card payment)
//   GetType() == typeof(CardPaymentModel) -> false (its exact type is GiftCardPaymentVariation)
public record GiftCardPaymentVariation(decimal Amount, string LastFourDigits, string GiftCode) : CardPaymentModel(Amount, LastFourDigits);
