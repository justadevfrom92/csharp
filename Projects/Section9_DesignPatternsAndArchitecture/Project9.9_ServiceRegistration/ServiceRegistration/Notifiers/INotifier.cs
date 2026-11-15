namespace ServiceRegistration.Notifiers;

public interface INotifier
{
    string Channel { get; }

    Task NotifyAsync(string to, string message);
}
