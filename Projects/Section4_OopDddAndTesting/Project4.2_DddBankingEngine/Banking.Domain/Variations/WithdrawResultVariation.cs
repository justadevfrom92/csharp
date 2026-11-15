namespace Banking.Domain.Variations;

// A RESULT object: success or failure, with the reason, returned instead of thrown.
public record WithdrawResultVariation(bool Succeeded, string? Error);
