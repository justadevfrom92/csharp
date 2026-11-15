using ServiceRegistration.Jobs;
using ServiceRegistration.Models;
using ServiceRegistration.Stores;

namespace ServiceRegistration.Services;

// Decides which jobs are DUE and runs them. Separate from the BackgroundService so tests can call it directly.
//
// It's a SINGLETON, but jobs may need SCOPED services (a repository, a DbContext). A singleton must never hold a
// scoped service (it would live forever), so for every run it creates a NEW SCOPE with IServiceScopeFactory,
// resolves the jobs inside it, and disposes the scope afterwards.
public class JobSchedulerService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly JobRunStore store;
    private readonly TimeProvider clock;

    public JobSchedulerService(IServiceScopeFactory scopeFactory, JobRunStore store, TimeProvider clock)
    {
        this.scopeFactory = scopeFactory;
        this.store = store;
        this.clock = clock;
    }

    public async Task<int> RunDueJobsAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IEnumerable<IJob> jobs = scope.ServiceProvider.GetServices<IJob>();
        int ran = 0;
        foreach (IJob job in jobs)
        {
            DateTimeOffset now = clock.GetUtcNow();
            DateTimeOffset? last = store.LastRun(job.Name);
            if (last != null && now - last.Value < job.Interval)
            {
                continue; // not due yet
            }
            try
            {
                await job.RunAsync(cancellationToken);
                store.Add(new JobRunModel(job.Name, now, true, null));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                store.Add(new JobRunModel(job.Name, now, false, exception.Message)); // one bad job doesn't stop the others
            }
            ran++;
        }
        return ran;
    }
}
