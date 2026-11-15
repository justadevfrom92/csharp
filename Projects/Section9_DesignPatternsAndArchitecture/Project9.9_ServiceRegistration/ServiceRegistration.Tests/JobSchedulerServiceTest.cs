using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ServiceRegistration.Jobs;
using ServiceRegistration.Extensions;
using ServiceRegistration.Services;
using ServiceRegistration.Stores;
using ServiceRegistration.Tests.Jobs;

namespace ServiceRegistration.Tests;

public class JobSchedulerServiceTest
{
    private readonly FakeTimeProvider clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 15, 9, 0, 0, TimeSpan.Zero));

    // FakeTimeProvider is registered FIRST, so RegisterAllExistingJobs' TryAddSingleton(TimeProvider.System) keeps it.
    // That's exactly why library code uses TryAdd: the app (or a test) can put its own in first.
    private ServiceProvider Build(Action<IServiceCollection> registerJobs)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddShopServices();
        registerJobs(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    [Fact]
    public async Task RunDue_PositivePositive_AllRunFirstTime_ThenOnlyTheOnesWhoseIntervalPassed()
    {
        using ServiceProvider provider = Build(services => services.RegisterAllExistingJobs());
        var scheduler = provider.GetRequiredService<JobSchedulerService>();

        int first = await scheduler.RunDueJobsAsync(CancellationToken.None);
        clock.Advance(TimeSpan.FromSeconds(10));
        int second = await scheduler.RunDueJobsAsync(CancellationToken.None);

        Assert.Equal(3, first);
        Assert.Equal(1, second); // only RefreshPricesJob (every 10 s); cleanup is every minute, report every day
        Assert.Equal("RefreshPricesJob", provider.GetRequiredService<JobRunStore>().GetAll()[^1].JobName);
    }

    [Fact]
    public async Task RunDue_PositiveNegative_NothingDue_RunsNothing_AndAFailingJobIsRecordedNotThrown()
    {
        using ServiceProvider provider = Build(services => services.RegisterAllExistingJobs().AddTransient<IJob, FailingTestJob>());
        var scheduler = provider.GetRequiredService<JobSchedulerService>();
        var store = provider.GetRequiredService<JobRunStore>();

        await scheduler.RunDueJobsAsync(CancellationToken.None);
        int again = await scheduler.RunDueJobsAsync(CancellationToken.None);

        Assert.Equal(0, again);
        Assert.Contains(store.GetAll(), run => run.JobName == "Failing" && !run.Succeeded && run.Error == "disk full");
        Assert.Contains(store.GetAll(), run => run.JobName == "SendDailyReportJob" && run.Succeeded); // others still ran
    }

    [Fact]
    public async Task RunDue_NegativeNegative_Cancelled_StopsInsteadOfRecordingAFailure()
    {
        // typeof(object).Assembly has no jobs: only the runner's own services are registered, then the slow job by hand.
        using ServiceProvider provider = Build(services => services.RegisterAllExistingJobs(typeof(object).Assembly).AddTransient<IJob, SlowTestJob>());
        var scheduler = provider.GetRequiredService<JobSchedulerService>();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scheduler.RunDueJobsAsync(cancellation.Token));

        Assert.DoesNotContain(provider.GetRequiredService<JobRunStore>().GetAll(), run => run.JobName == "Slow");
    }
}
