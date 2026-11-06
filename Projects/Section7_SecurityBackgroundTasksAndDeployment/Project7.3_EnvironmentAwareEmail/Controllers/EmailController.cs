using EnvironmentAwareEmail.Services.Email;
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

    [HttpGet("/")]
    public IActionResult Info()
    {
        return Ok(new { environment = environment.EnvironmentName, emailSender = sender.GetType().Name });
    }

    // POST /send-welcome?to=someone@example.com
    [HttpPost("/send-welcome")]
    public async Task<IActionResult> SendWelcome(string to)
    {
        await queue.AddAsync(new EmailMessage(to, "Welcome!", "Thanks for signing up."));
        return Accepted(new { queued = true, sender = sender.GetType().Name });
    }
}
