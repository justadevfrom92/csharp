using EnvironmentAwareEmail.Messages;

namespace EnvironmentAwareEmail.Senders;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
