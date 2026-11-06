using EnvironmentAwareEmail.Messages;
using EnvironmentAwareEmail.Queues;

namespace EnvironmentAwareEmail.BackgroundServices;

// PeriodicTimer example: a job that runs every 30 seconds.
public class DailyDigestBackgroundService : BackgroundService
{
    private readonly EmailQueue queue;

    public DailyDigestBackgroundService(EmailQueue queue)
    {
        this.queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // would be once a day in real life
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await queue.AddAsync(new EmailMessage("team@client-company.com", "Digest", $"Status at {DateTime.Now:t}: all good."));
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
    }
}
