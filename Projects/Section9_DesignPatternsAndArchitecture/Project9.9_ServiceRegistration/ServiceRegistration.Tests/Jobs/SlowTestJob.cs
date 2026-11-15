using ServiceRegistration.Jobs;

namespace ServiceRegistration.Tests.Jobs;

public class SlowTestJob : IJob
{
    public string Name => "Slow";

    public TimeSpan Interval => TimeSpan.FromMinutes(1);

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
    }
}
