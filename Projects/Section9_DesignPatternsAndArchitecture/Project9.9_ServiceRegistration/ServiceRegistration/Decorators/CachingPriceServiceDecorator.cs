using Microsoft.Extensions.Caching.Memory;
using ServiceRegistration.Services;

namespace ServiceRegistration.Decorators;

// DECORATOR registered by hand (see Program.cs): the app asks for IPriceService and gets THIS,
// which wraps the real SlowPriceService and remembers answers.
public class CachingPriceServiceDecorator : IPriceService
{
    private readonly IPriceService inner;
    private readonly IMemoryCache cache;

    public CachingPriceServiceDecorator(IPriceService inner, IMemoryCache cache)
    {
        this.inner = inner;
        this.cache = cache;
    }

    public decimal GetPrice(int productId)
    {
        return cache.GetOrCreate($"price:{productId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return inner.GetPrice(productId);
        });
    }
}
