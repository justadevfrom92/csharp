using EnvironmentAwareEmail.Constants;
using EnvironmentAwareEmail.Messages;
using EnvironmentAwareEmail.Queues;
using EnvironmentAwareEmail.Senders;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentAwareEmail.Controllers;

[ApiController]
public class EmailController : ControllerBase
{
    private readonly EmailQueue queue;
    private readonly IWebHostEnvironment environment;
    private readonly IEmailSender sender;

    public EmailController(EmailQueue queue, IWebHostEnvironment environment, IEmailSender sender)
    {
        this.queue = queue;
        this.environment = environment;
        this.sender = sender;
    }

    // GET /  - shows the environment and the decorator chain, e.g. "LoggingEmailSender -> FileTrapEmailSender".
    [HttpGet("/")]
    public IActionResult Info()
    {
        return Ok(new { environment = environment.EnvironmentName, emailSenderChain = DescribeChain(sender) });
    }

    // POST /send-welcome?to=someone@example.com  - goes through the normal (environment-picked) sender, via the queue.
    [HttpPost("/send-welcome")]
    public async Task<IActionResult> SendWelcome(string to)
    {
        await queue.AddAsync(new EmailMessage(to, "Welcome!", "Thanks for signing up."));
        return Accepted(new { queued = true, emailSenderChain = DescribeChain(sender) });
    }

    // POST /send-preview?to=someone@example.com  - ALWAYS goes to the file trap, even in Production,
    // so a designer can check how an email looks without anyone receiving it.
    // [FromKeyedServices] picks the KEYED registration "trap" for just this parameter.
    [HttpPost("/send-preview")]
    public async Task<IActionResult> SendPreview(string to, [FromKeyedServices(EmailSenderKeyConstants.Trap)] IEmailSender trapSender)
    {
        await trapSender.SendAsync(new EmailMessage(to, "Preview: Welcome!", "Thanks for signing up."));
        return Ok(new { previewSavedBy = trapSender.GetType().Name });
    }

    // Walks through the decorators to show what wraps what.
    private static string DescribeChain(IEmailSender emailSender)
    {
        if (emailSender is LoggingEmailSender loggingSender)
        {
            return nameof(LoggingEmailSender) + " -> " + DescribeChain(loggingSender.Inner);
        }
        if (emailSender is UatRedirectingEmailSender uatSender)
        {
            return nameof(UatRedirectingEmailSender) + " -> " + DescribeChain(uatSender.Inner);
        }
        return emailSender.GetType().Name;
    }
}
