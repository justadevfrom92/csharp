using Microsoft.Extensions.Options;
using ServiceRegistration.Services;
using ServiceRegistration.Settings;

namespace ServiceRegistration.BackgroundServices;

// Runs for the life of the app: every tick, asks the scheduler to run whatever is due.
public class JobRunnerBackgroundService : BackgroundService
{
    private readonly JobSchedulerService scheduler;
    private readonly JobRunnerSettings settings;

    public JobRunnerBackgroundService(JobSchedulerService scheduler, IOptions<JobRunnerSettings> settings)
    {
        this.scheduler = scheduler;
        this.settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.TickMilliseconds));
        try
        {
            do
            {
                await scheduler.RunDueJobsAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // app is stopping
        }
    }
}
