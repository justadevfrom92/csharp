using PipelinePatterns.Interceptors;
using PipelinePatterns.Services;

namespace PipelinePatterns.Tests;

public class InterceptorTest
{
    private readonly List<string> log = new();

    [Fact]
    public void Intercept_PositivePositive_EveryCallThroughTheProxyIsLogged_AndStillWorks()
    {
        IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);

        stock.Reserve("KB-01", 3);
        int left = stock.GetStock("KB-01");

        Assert.Equal(7, left);
        Assert.Equal(2, log.Count); // Reserve's own internal GetStock call is NOT seen (it doesn't go through the proxy)
        Assert.StartsWith("Reserve(KB-01, 3) ok", log[0]);
        Assert.EndsWith("-> 7", log[1]);
    }

    [Fact]
    public void Intercept_PositiveNegative_BusinessRuleFails_OriginalExceptionTypeReachesCaller_AndIsLogged()
    {
        IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);

        var error = Assert.Throws<InvalidOperationException>(() => stock.Reserve("MS-01", 5)); // not TargetInvocationException

        Assert.Equal("Only 2 MS-01 left.", error.Message);
        Assert.Contains("FAILED", log[0]);
    }

    [Fact]
    public void Intercept_NegativeNegative_UnknownSkuOrNullTarget_Throws()
    {
        IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);

        Assert.Throws<KeyNotFoundException>(() => stock.GetStock("??"));
        Assert.Throws<ArgumentNullException>(() => LoggingInterceptor<IStockService>.Wrap(null!, log));
        Assert.Throws<ArgumentNullException>(() => LoggingInterceptor<IStockService>.Wrap(new StockService(), null!));
    }
}
