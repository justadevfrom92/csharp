using ServiceRegistration.Services;

namespace ServiceRegistration.Jobs;

public class RefreshPricesJob : BaseJob
{
    private readonly IPriceService prices;

    public RefreshPricesJob(IPriceService prices)
    {
        this.prices = prices;
    }

    public override TimeSpan Interval => TimeSpan.FromSeconds(10);

    public override Task RunAsync(CancellationToken cancellationToken)
    {
        _ = prices.GetPrice(1);
        return Task.CompletedTask;
    }
}
