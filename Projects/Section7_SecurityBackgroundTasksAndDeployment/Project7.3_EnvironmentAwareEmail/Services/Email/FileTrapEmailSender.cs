using EnvironmentAwareEmail.Settings;
namespace EnvironmentAwareEmail.Services.Email;

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
