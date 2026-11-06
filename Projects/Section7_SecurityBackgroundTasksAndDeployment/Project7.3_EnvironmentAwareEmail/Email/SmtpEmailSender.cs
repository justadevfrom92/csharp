using System.Net.Mail;

namespace EnvironmentAwareEmail.Email;

// PRODUCTION: really sends through the SMTP server.
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings settings;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(EmailSettings settings, ILogger<SmtpEmailSender> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message)
    {
        using var smtpClient = new SmtpClient(settings.SmtpHost, settings.SmtpPort);
        using var mailMessage = new MailMessage(settings.FromAddress, message.To, message.Subject, message.Body);
        await smtpClient.SendMailAsync(mailMessage);
        logger.LogInformation("[SMTP] Email sent to {To}", message.To);
    }
}
