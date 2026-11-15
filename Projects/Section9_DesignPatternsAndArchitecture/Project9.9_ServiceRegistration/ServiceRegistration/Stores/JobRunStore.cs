using ServiceRegistration.Models;

namespace ServiceRegistration.Stores;

// Singleton: remembers every run, for the whole app.
public class JobRunStore
{
    private readonly object listLock = new();
    private readonly List<JobRunModel> runs = new();

    public void Add(JobRunModel run)
    {
        lock (listLock)
        {
            runs.Add(run);
        }
    }

    public List<JobRunModel> GetAll()
    {
        lock (listLock)
        {
            return runs.ToList();
        }
    }

    public DateTimeOffset? LastRun(string jobName)
    {
        lock (listLock)
        {
            return runs.LastOrDefault(run => run.JobName == jobName)?.RanAt;
        }
    }
}
