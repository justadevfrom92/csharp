namespace EfCoreModelling.Models;

// Work sold by the hour.
public sealed class ServiceModel : ProductModel
{
    public ServiceModel(string name, decimal hourlyRate) : base(name)
    {
        HourlyRate = hourlyRate;
    }

    public decimal HourlyRate { get; private set; }
}
