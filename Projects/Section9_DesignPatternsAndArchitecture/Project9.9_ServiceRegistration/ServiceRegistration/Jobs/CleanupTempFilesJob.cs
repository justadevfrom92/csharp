using ServiceRegistration.Models;
using ServiceRegistration.Repositories;

namespace ServiceRegistration.Jobs;

// Uses a SCOPED service (the repository). That's fine: the runner creates a new scope for every run.
public class CleanupTempFilesJob : BaseJob
{
    private readonly IRepository<ProductModel> products;

    public CleanupTempFilesJob(IRepository<ProductModel> products)
    {
        this.products = products;
    }

    public override TimeSpan Interval => TimeSpan.FromMinutes(1);

    public override Task RunAsync(CancellationToken cancellationToken)
    {
        _ = products.GetAll(); // pretend to check which product images are no longer used
        return Task.CompletedTask;
    }
}
