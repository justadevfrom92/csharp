namespace ServiceRegistration.Jobs;

// ABSTRACT: shared code for jobs. RegisterAllExistingJobs SKIPS abstract classes - you can't create one.
public abstract class BaseJob : IJob
{
    public virtual string Name => GetType().Name;

    public abstract TimeSpan Interval { get; }

    public abstract Task RunAsync(CancellationToken cancellationToken);
}
