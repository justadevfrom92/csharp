using PatternMatching.Models;

namespace PatternMatching.Variations;

// The same shipping rules as ShippingCostClassifier (switch expression with PROPERTY patterns), as nested ifs.
// Same answers; compare how much easier the property-pattern version is to check against the rules.
public static class ShippingCostVariation
{
    public static decimal WithNestedIfs(ParcelModel? parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        if (parcel.WeightKilograms <= 0)
        {
            throw new ArgumentException("Weight must be more than 0 kg.", nameof(parcel));
        }
        if (parcel.WeightKilograms > 30)
        {
            throw new InvalidOperationException("Parcels over 30 kg can't be shipped.");
        }
        if (parcel.CountryCode == "US")
        {
            if (parcel.IsExpress)
            {
                return 15.00m;
            }
            if (parcel.WeightKilograms <= 1)
            {
                return 5.00m;
            }
            return 9.00m;
        }
        if (parcel.IsExpress)
        {
            return 40.00m;
        }
        return 25.00m;
    }
}
