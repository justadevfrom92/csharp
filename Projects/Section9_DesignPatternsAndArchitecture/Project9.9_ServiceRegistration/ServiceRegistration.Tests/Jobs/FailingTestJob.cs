using ServiceRegistration.Jobs;

namespace ServiceRegistration.Tests.Jobs;

// A test-only job, registered by hand in the tests (AddTransient<IJob, FailingTestJob>()).
public class FailingTestJob : IJob
{
    public string Name => "Failing";

    public TimeSpan Interval => TimeSpan.FromMinutes(1);

    public Task RunAsync(CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("disk full");
    }
}
