namespace EnvironmentAwareEmail.Variations;

// The same kind of background worker as EmailSendingBackgroundService, written against IHostedService directly.
// BackgroundService (original) is a small base class that does this for you: it starts ExecuteAsync without
// blocking StartAsync, and cancels it in StopAsync. Here you write that yourself. (Not registered - for comparison.)
public class HostedServiceVariation : IHostedService, IDisposable
{
    private readonly CancellationTokenSource stopping = new CancellationTokenSource();
    private Task? running;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Must return QUICKLY: the app doesn't start serving requests until every StartAsync has returned.
        running = RunAsync(stopping.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        stopping.Cancel();
        if (running != null)
        {
            await Task.WhenAny(running, Task.Delay(Timeout.Infinite, cancellationToken)); // wait, but not past the host's shutdown timeout
        }
    }

    public void Dispose()
    {
        stopping.Dispose();
    }

    private static async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // the work
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
