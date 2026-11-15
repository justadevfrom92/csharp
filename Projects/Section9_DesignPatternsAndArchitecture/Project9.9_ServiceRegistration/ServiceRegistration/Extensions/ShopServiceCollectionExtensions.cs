using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ServiceRegistration.Decorators;
using ServiceRegistration.Notifiers;
using ServiceRegistration.Repositories;
using ServiceRegistration.Services;
using ServiceRegistration.Settings;

namespace ServiceRegistration.Extensions;

// Grouping registrations in an extension method keeps Program.cs short:  builder.Services.AddShopServices();
// Every way of registering a service is shown once, numbered to match the README.
public static class ShopServiceCollectionExtensions
{
    public const string SingletonKey = "singleton";
    public const string ScopedKey = "scoped";
    public const string TransientKey = "transient";

    public static IServiceCollection AddShopServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // 1. LIFETIMES (and 2. KEYED registrations: same class, three keys).
        services.AddKeyedSingleton<LifetimeProbeService>(SingletonKey);  // one for the whole app
        services.AddKeyedScoped<LifetimeProbeService>(ScopedKey);        // one per request (per scope)
        services.AddKeyedTransient<LifetimeProbeService>(TransientKey);  // a new one every time it's asked for

        // 3. OPTIONS: bind a config section, check it, stop at startup if wrong.
        services.AddOptions<ShopSettings>()
            .BindConfiguration("Shop")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        // PostConfigure runs AFTER binding: fix up values (here: always trim the name).
        services.PostConfigure<ShopSettings>(settings => settings.ShopName = settings.ShopName.Trim());

        // 4. FACTORY: we build the object ourselves, because its constructor needs a plain string.
        services.AddSingleton(serviceProvider =>
            new GreetingService(serviceProvider.GetRequiredService<IOptions<ShopSettings>>().Value.ShopName));

        // 5. OPEN GENERIC: one line for IRepository<ProductModel>, IRepository<CustomerModel>, ...
        services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));

        // 6. SEVERAL IMPLEMENTATIONS of one interface. Ask for IEnumerable<INotifier> to get all of them;
        //    ask for INotifier to get the LAST one registered (sms).
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INotifier, EmailNotifier>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<INotifier, SmsNotifier>());

        // 7. DECORATOR by hand: register the real class as itself, then IPriceService as the wrapper around it.
        services.AddMemoryCache();
        services.AddSingleton<SlowPriceService>();
        services.AddSingleton<IPriceService>(serviceProvider => new CachingPriceServiceDecorator(
            serviceProvider.GetRequiredService<SlowPriceService>(),
            serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));

        // 8. TRY-ADD: "register this only if nobody else has". Library code uses it so the app can override.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(TimeProvider.System); // second call: does nothing

        return services;
    }
}
