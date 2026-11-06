using EnvironmentAwareEmail.Settings;
namespace EnvironmentAwareEmail.Services.Email;

// UAT: uses the real SMTP sender, but EVERY email is sent to the test inbox instead of the real person.
public class UatRedirectingEmailSender : IEmailSender
{
    private readonly SmtpEmailSender realSender;
    private readonly EmailSettings settings;

    public UatRedirectingEmailSender(SmtpEmailSender realSender, EmailSettings settings)
    {
        this.realSender = realSender;
        this.settings = settings;
    }

    public Task SendAsync(EmailMessage message)
    {
        var redirected = new EmailMessage(
            settings.UatTestInbox,
            $"[UAT - meant for {message.To}] {message.Subject}",
            message.Body);
        return realSender.SendAsync(redirected);
    }
}
