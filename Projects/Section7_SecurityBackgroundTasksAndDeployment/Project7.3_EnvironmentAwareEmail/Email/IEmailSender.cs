namespace EnvironmentAwareEmail.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
