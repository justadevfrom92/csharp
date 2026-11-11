using PatternMatching.Models;

namespace PatternMatching.Classifiers;

public static class ShippingCostClassifier
{
    // PROPERTY patterns: { Property: pattern, ... } looks INSIDE the object.
    // Mixed with relational patterns on the property values.
    public static decimal CalculateCost(ParcelModel? parcel)
    {
        return parcel switch
        {
            null => throw new ArgumentNullException(nameof(parcel)),
            { WeightKilograms: <= 0 } => throw new ArgumentException("Weight must be more than 0 kg.", nameof(parcel)),
            { WeightKilograms: > 30 } => throw new InvalidOperationException("Parcels over 30 kg can't be shipped."),
            { CountryCode: "US", IsExpress: true } => 15.00m,
            { CountryCode: "US", WeightKilograms: <= 1 } => 5.00m,
            { CountryCode: "US" } => 9.00m,
            { IsExpress: true } => 40.00m,
            _ => 25.00m,
        };
    }
}
