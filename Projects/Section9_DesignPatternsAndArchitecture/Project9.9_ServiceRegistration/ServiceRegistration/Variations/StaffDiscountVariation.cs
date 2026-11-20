namespace ServiceRegistration.Variations;

// A plain class for ReflectionVariation to look inside: public properties, an init-only property,
// and a PRIVATE method that normal code can't call.
public class StaffDiscountVariation
{
    public string Code { get; init; } = "";

    public decimal Price { get; set; }

    public decimal PriceForCustomer()
    {
        return Price;
    }

    private decimal ApplyStaffDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "Must be 0-100.");
        }
        return Price - Price * percent / 100;
    }
}
