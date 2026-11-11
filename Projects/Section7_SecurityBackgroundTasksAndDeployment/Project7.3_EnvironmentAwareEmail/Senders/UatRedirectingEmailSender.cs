using EnvironmentAwareEmail.Messages;
using EnvironmentAwareEmail.Settings;

namespace EnvironmentAwareEmail.Senders;

// DECORATOR: it IS an IEmailSender and it WRAPS another IEmailSender.
// It changes the message (recipient -> UAT test inbox), then hands it to whatever sender it wraps.
// It doesn't know or care whether that is SMTP, the trap, or another decorator.
public class UatRedirectingEmailSender : IEmailSender
{
    private readonly EmailSettings settings;

    public UatRedirectingEmailSender(IEmailSender inner, EmailSettings settings)
    {
        Inner = inner;
        this.settings = settings;
    }

    // The sender this decorator wraps (shown by GET / so you can see the chain).
    public IEmailSender Inner { get; }

    public Task SendAsync(EmailMessage message)
    {
        var redirected = new EmailMessage(
            settings.UatTestInbox,
            $"[UAT - meant for {message.To}] {message.Subject}",
            message.Body);
        return Inner.SendAsync(redirected);
    }
}
