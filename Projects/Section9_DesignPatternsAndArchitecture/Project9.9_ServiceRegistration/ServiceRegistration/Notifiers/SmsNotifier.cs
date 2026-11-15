namespace ServiceRegistration.Notifiers;

public class SmsNotifier : INotifier
{
    public List<string> Sent { get; } = new();

    public string Channel => "sms";

    public Task NotifyAsync(string to, string message)
    {
        Sent.Add($"{to}: {message}");
        return Task.CompletedTask;
    }
}
