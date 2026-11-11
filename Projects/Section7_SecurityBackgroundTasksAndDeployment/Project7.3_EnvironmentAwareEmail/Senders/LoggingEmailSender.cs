using System.Diagnostics;
using EnvironmentAwareEmail.Messages;

namespace EnvironmentAwareEmail.Senders;

// DECORATOR: adds logging around ANY IEmailSender without changing it.
// The wrapped sender still does the real work; this class only writes "sending / sent / failed" + the time taken.
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> logger;

    public LoggingEmailSender(IEmailSender inner, ILogger<LoggingEmailSender> logger)
    {
        Inner = inner;
        this.logger = logger;
    }

    public IEmailSender Inner { get; }

    public async Task SendAsync(EmailMessage message)
    {
        logger.LogInformation("Sending email '{Subject}' to {To} via {Sender}", message.Subject, message.To, Inner.GetType().Name);
        var timer = Stopwatch.StartNew();
        try
        {
            await Inner.SendAsync(message);
            logger.LogInformation("Sent '{Subject}' in {Milliseconds} ms", message.Subject, timer.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sending '{Subject}' to {To} failed after {Milliseconds} ms", message.Subject, message.To, timer.ElapsedMilliseconds);
            throw; // log it, but let the caller still see the failure
        }
    }
}
