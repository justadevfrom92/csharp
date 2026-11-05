using System.Threading.Channels;

namespace EnvironmentAwareEmail.Email;

// Controllers drop emails here and return at once. A background service sends them.
public class EmailQueue
{
    private readonly Channel<EmailMessage> channel = Channel.CreateBounded<EmailMessage>(1000);

    public ValueTask AddAsync(EmailMessage message)
    {
        return channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken stopToken)
    {
        return channel.Reader.ReadAllAsync(stopToken);
    }
}

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

// PeriodicTimer example: a job that runs every 30 seconds.
public class DailyDigestBackgroundService : BackgroundService
{
    private readonly EmailQueue queue;

    public DailyDigestBackgroundService(EmailQueue queue)
    {
        this.queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // would be once a day in real life
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await queue.AddAsync(new EmailMessage("team@client-company.com", "Digest", $"Status at {DateTime.Now:t}: all good."));
            }
        }
        catch (OperationCanceledException)
        {
            // App is shutting down.
        }
    }
}
