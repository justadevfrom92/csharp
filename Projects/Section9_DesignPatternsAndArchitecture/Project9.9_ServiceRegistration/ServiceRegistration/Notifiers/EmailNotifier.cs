namespace ServiceRegistration.Notifiers;

public class EmailNotifier : INotifier
{
    public List<string> Sent { get; } = new();

    public string Channel => "email";

    public Task NotifyAsync(string to, string message)
    {
        Sent.Add($"{to}: {message}");
        return Task.CompletedTask;
    }
}
