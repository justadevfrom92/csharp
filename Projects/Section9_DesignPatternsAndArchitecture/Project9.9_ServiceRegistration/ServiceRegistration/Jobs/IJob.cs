namespace ServiceRegistration.Jobs;

// A piece of background work that runs every "Interval".
// Any non-abstract class implementing this is found and registered by RegisterAllExistingJobs().
public interface IJob
{
    string Name { get; }

    TimeSpan Interval { get; }

    Task RunAsync(CancellationToken cancellationToken);
}
