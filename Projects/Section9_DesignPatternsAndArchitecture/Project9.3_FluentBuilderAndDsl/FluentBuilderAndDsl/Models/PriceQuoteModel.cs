namespace FluentBuilderAndDsl.Models;

public record PriceQuoteModel(decimal Subtotal, int DiscountPercent, decimal Discount, decimal Shipping, decimal Total, IReadOnlyList<string> AppliedRules);
