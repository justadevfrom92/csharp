using System.ComponentModel.DataAnnotations;

namespace ServiceRegistration.Settings;

public class JobRunnerSettings
{
    // How often the runner wakes up to check which jobs are due.
    [Range(10, 60_000)]
    public int TickMilliseconds { get; set; } = 1000;
}
