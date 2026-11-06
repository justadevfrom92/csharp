namespace EnvironmentAwareEmail.Services.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
