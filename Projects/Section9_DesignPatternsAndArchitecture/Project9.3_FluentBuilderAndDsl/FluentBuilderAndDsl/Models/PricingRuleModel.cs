namespace FluentBuilderAndDsl.Models;

// One parsed line of the pricing DSL, e.g. "WHEN total > 100 THEN discount 10%"
//   Field = "total", Operator = ">", Value = "100", Action = "discount", Amount = 10
public record PricingRuleModel(int LineNumber, string Field, string Operator, string Value, string Action, decimal Amount);
