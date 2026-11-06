using EnvironmentAwareEmail.Services.Email;
namespace EnvironmentAwareEmail.BackgroundServices;

// BackgroundService: runs for the whole life of the app.
public class EmailSendingBackgroundService : BackgroundService
{
    private readonly EmailQueue queue;
    private readonly IEmailSender sender;
    private readonly ILogger<EmailSendingBackgroundService> logger;

    public EmailSendingBackgroundService(EmailQueue queue, IEmailSender sender, ILogger<EmailSendingBackgroundService> logger)
    {
        this.queue = queue;
        this.sender = sender;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (EmailMessage message in queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await sender.SendAsync(message);
                }
                catch (Exception exception)
                {
                    // One bad email must not stop the whole service.
                    logger.LogError(exception, "Could not send email to {To}", message.To);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
    }
}
