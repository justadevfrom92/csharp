using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceRegistration.BackgroundServices;
using ServiceRegistration.Extensions;
using ServiceRegistration.Jobs;

namespace ServiceRegistration.Tests;

// PositivePositive = valid input -> works; PositiveNegative = well-formed input that breaks a rule / finds nothing -> handled;
// NegativeNegative = broken or missing input -> rejected.
public class RegisterAllExistingJobsTest
{
    private static List<Type> JobTypes(IServiceCollection services)
    {
        return services.Where(descriptor => descriptor.ServiceType == typeof(IJob)).Select(descriptor => descriptor.ImplementationType!).ToList();
    }

    [Fact]
    public void Register_PositivePositive_FindsEveryConcreteJob_AndTheRunner()
    {
        var services = new ServiceCollection();

        services.RegisterAllExistingJobs();

        Assert.Equal([typeof(CleanupTempFilesJob), typeof(RefreshPricesJob), typeof(SendDailyReportJob)], JobTypes(services));
        Assert.DoesNotContain(typeof(BaseJob), JobTypes(services)); // abstract: skipped
        Assert.All(services.Where(descriptor => descriptor.ServiceType == typeof(IJob)), descriptor => Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(JobRunnerBackgroundService));
    }

    [Fact]
    public void Register_PositiveNegative_CalledTwice_NoDuplicates_AndAssemblyWithoutJobs_RegistersNone()
    {
        var twice = new ServiceCollection();
        twice.RegisterAllExistingJobs().RegisterAllExistingJobs();

        var noJobs = new ServiceCollection();
        noJobs.RegisterAllExistingJobs(typeof(object).Assembly);

        Assert.Equal(3, JobTypes(twice).Count);
        Assert.Single(twice, descriptor => descriptor.ImplementationType == typeof(JobRunnerBackgroundService));
        Assert.Empty(JobTypes(noJobs));
    }

    [Fact]
    public void Register_NegativeNegative_NullServices_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() => services!.RegisterAllExistingJobs());
    }
}
