using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration.Extensions;
using ServiceRegistration.Jobs;
using ServiceRegistration.Variations;

namespace ServiceRegistration.Tests;

// Other ways to register the jobs (ServiceRegistration/Variations/JobRegistrationVariation.cs), the look-alike
// "get a service" methods (ResolveVariation.cs), and the lifetime mistake the container can catch.
public class VariationsTest
{
    private static List<Type> JobTypes(IServiceCollection services)
    {
        return services.Where(descriptor => descriptor.ServiceType == typeof(IJob)).Select(descriptor => descriptor.ImplementationType!).OrderBy(type => type.Name).ToList();
    }

    [Fact]
    public void Registering_PositivePositive_ByHandPlusAddJob_FindsTheSameThreeAsScanning()
    {
        var scanned = new ServiceCollection();
        scanned.RegisterAllExistingJobs();
        var byHand = new ServiceCollection();
        byHand.RegisterJobsByHand().AddJob<SendDailyReportJob>();

        Assert.Equal(JobTypes(scanned), JobTypes(byHand));
    }

    [Fact]
    public void Registering_PositiveNegative_EachStyleFindsADifferentSet()
    {
        var byHand = new ServiceCollection();
        byHand.RegisterJobsByHand();
        List<Type> byName = JobRegistrationVariation.FindJobsByName(typeof(VariationsTest).Assembly);
        var byAttribute = new ServiceCollection();
        byAttribute.RegisterJobsWithAttribute(typeof(VariationsTest).Assembly);

        Assert.DoesNotContain(typeof(SendDailyReportJob), JobTypes(byHand)); // the forgotten line: this job never runs
        Assert.Contains(typeof(ReportJob), byName);                          // ends in "Job" but ISN'T a job
        Assert.DoesNotContain(typeof(NightlyCleanup), byName);               // IS a job but doesn't end in "Job"
        Assert.Equal([typeof(MarkedJob)], JobTypes(byAttribute));            // only the one carrying the attribute
    }

    [Fact]
    public void Resolving_PositiveNegative_LookAlikeMethods_AnswerDifferentlyForMissingServices()
    {
        var services = new ServiceCollection();
        // BindConfiguration("Shop") needs an IConfiguration in the container (WebApplication adds one; a bare collection doesn't).
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection([new("Shop:ShopName", "Test Shop"), new("Shop:SupportEmail", "help@example.com")]).Build());
        services.AddShopServices();
        using ServiceProvider provider = services.BuildServiceProvider();

        (object? missing, int allNotifiers, string created) = ResolveVariation.Demo(provider);

        Assert.Null(missing);                                                                                       // GetService
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<INeverRegisteredVariation>());  // GetRequiredService
        Assert.Equal(2, allNotifiers);                                                                              // GetServices
        Assert.Equal("Welcome to Test Shop, report reader!", created);                                                                   // ActivatorUtilities
    }

    [Fact]
    public void Lifetimes_NegativeNegative_SingletonHoldingAScopedService_RejectedWhenValidated()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedThing>();
        services.AddSingleton<SingletonThing>(); // a "captive dependency": the scoped object would live forever

        using ServiceProvider notValidated = services.BuildServiceProvider();      // no validation: works, silently wrong
        Assert.NotNull(notValidated.GetRequiredService<SingletonThing>());

        Assert.Throws<AggregateException>(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
    }

    private class ScopedThing
    {
    }

    private class SingletonThing
    {
        public SingletonThing(ScopedThing scoped)
        {
        }
    }

    private class ReportJob
    {
    }

    private class NightlyCleanup : IJob
    {
        public string Name => "NightlyCleanup";

        public TimeSpan Interval => TimeSpan.FromDays(1);

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [AutoRegisterVariation]
    private class MarkedJob : IJob
    {
        public string Name => "Marked";

        public TimeSpan Interval => TimeSpan.FromDays(1);

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
