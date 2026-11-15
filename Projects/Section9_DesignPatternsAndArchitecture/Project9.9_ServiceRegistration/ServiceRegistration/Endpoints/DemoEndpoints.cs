using ServiceRegistration.Extensions;
using ServiceRegistration.Jobs;
using ServiceRegistration.Models;
using ServiceRegistration.Notifiers;
using ServiceRegistration.Repositories;
using ServiceRegistration.Services;
using ServiceRegistration.Stores;

namespace ServiceRegistration.Endpoints;

public static class DemoEndpoints
{
    public static void MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        // Every registered job and its runs.
        app.MapGet("/jobs", (IEnumerable<IJob> jobs, JobRunStore store) => TypedResults.Ok(new
        {
            registeredJobs = jobs.Select(job => new { job.Name, intervalSeconds = job.Interval.TotalSeconds }),
            runs = store.GetAll(),
        }));

        // Asks for each lifetime TWICE in the same request and shows whether it got the same object.
        app.MapGet("/lifetimes", (IServiceProvider services) =>
        {
            Guid Id(string key)
            {
                return services.GetRequiredKeyedService<LifetimeProbeService>(key).Id;
            }

            return TypedResults.Ok(new
            {
                singletonSameTwice = Id(ShopServiceCollectionExtensions.SingletonKey) == Id(ShopServiceCollectionExtensions.SingletonKey),
                scopedSameTwice = Id(ShopServiceCollectionExtensions.ScopedKey) == Id(ShopServiceCollectionExtensions.ScopedKey),
                transientSameTwice = Id(ShopServiceCollectionExtensions.TransientKey) == Id(ShopServiceCollectionExtensions.TransientKey),
                singletonId = Id(ShopServiceCollectionExtensions.SingletonKey),
                scopedId = Id(ShopServiceCollectionExtensions.ScopedKey),
            });
        });

        app.MapGet("/greeting/{customer}", (string customer, GreetingService greeting) => TypedResults.Ok(greeting.Greet(customer)));

        // Open generic: two different closed types from ONE registration.
        app.MapGet("/repositories", (IRepository<ProductModel> products, IRepository<CustomerModel> customers) =>
        {
            products.Add(new ProductModel(1, "Keyboard"));
            customers.Add(new CustomerModel(1, "Ann"));
            return TypedResults.Ok(new { products = products.GetAll().Count, customers = customers.GetAll().Count, productRepository = products.GetType().Name });
        });

        app.MapGet("/notifiers", (IEnumerable<INotifier> all, INotifier single) => TypedResults.Ok(new
        {
            all = all.Select(notifier => notifier.Channel),
            whenAskingForOne = single.Channel,
        }));

        app.MapGet("/prices/{productId:int}", (int productId, IPriceService prices, SlowPriceService slow) =>
        {
            if (productId <= 0)
            {
                return Results.BadRequest("Product id must be more than 0.");
            }
            decimal price = prices.GetPrice(productId);
            return Results.Ok(new { price, decoratedBy = prices.GetType().Name, slowLookupsSoFar = slow.Lookups });
        });
    }
}
