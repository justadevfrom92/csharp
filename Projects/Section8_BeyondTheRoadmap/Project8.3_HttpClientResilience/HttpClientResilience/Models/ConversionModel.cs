namespace HttpClientResilience.Models;

public record ConversionModel(string From, string To, decimal Amount, decimal Rate, decimal ConvertedAmount);
