using ServiceRegistration.Notifiers;

namespace ServiceRegistration.Jobs;

// Gets ALL notifiers (email + SMS) because several implementations of INotifier are registered.
public class SendDailyReportJob : BaseJob
{
    private readonly IEnumerable<INotifier> notifiers;

    public SendDailyReportJob(IEnumerable<INotifier> notifiers)
    {
        this.notifiers = notifiers;
    }

    public override TimeSpan Interval => TimeSpan.FromDays(1);

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (INotifier notifier in notifiers)
        {
            await notifier.NotifyAsync("manager", "Daily report is ready.");
        }
    }
}
