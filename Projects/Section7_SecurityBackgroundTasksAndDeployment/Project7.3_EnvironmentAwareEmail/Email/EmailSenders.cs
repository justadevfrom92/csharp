using System.Net.Mail;

namespace EnvironmentAwareEmail.Email;

public record EmailMessage(string To, string Subject, string Body);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}

public class EmailSettings
{
    public string FromAddress { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 25;
    public string TrapFolder { get; set; } = "sent-emails";
    public string UatTestInbox { get; set; } = "";
}

// DEVELOPMENT: nothing leaves the machine. Each email is saved as a .eml file you can open.
public class FileTrapEmailSender : IEmailSender
{
    private readonly EmailSettings settings;
    private readonly ILogger<FileTrapEmailSender> logger;

    public FileTrapEmailSender(EmailSettings settings, ILogger<FileTrapEmailSender> logger)
    {
        this.settings = settings;
        this.logger = logger;
    }

    public async Task SendAsync(EmailMessage message)
    {
        Directory.CreateDirectory(settings.TrapFolder);
        string filePath = Path.Combine(settings.TrapFolder, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.eml");
        string fileText = $"From: {settings.FromAddress}\nTo: {message.To}\nSubject: {message.Subject}\n\n{message.Body}\n";
        await File.WriteAllTextAsync(filePath, fileText);
        logger.LogInformation("[TRAP] Email to {To} saved to {File} (not sent)", message.To, filePath);
    }
}

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
