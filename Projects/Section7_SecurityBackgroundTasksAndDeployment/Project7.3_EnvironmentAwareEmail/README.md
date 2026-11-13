# Project 7.3 — Environment-Aware Email Dispatcher

```bash
dotnet run --launch-profile Development
```

```bash
curl -X POST "localhost:5073/send-welcome?to=real.customer@gmail.com"
```

In Development the email is written to `sent-emails/*.eml` (open it in any mail app). Nothing is sent.
`GET /` shows which environment and sender are active. Try `--launch-profile UAT` and `Production` too
(they need a real SMTP server to actually deliver).

## Routing rules

| Environment (`ASPNETCORE_ENVIRONMENT`) | Sender | What happens |
|---|---|---|
| `Development` | `LoggingEmailSender` → `FileTrapEmailSender` | Saved to a `.eml` file. Never leaves the machine. |
| `UAT` | `LoggingEmailSender` → `UatRedirectingEmailSender` → `SmtpEmailSender` | Really sent, but **always** to `Email:UatTestInbox`. Subject shows who it was meant for. |
| `Production` | `LoggingEmailSender` → `SmtpEmailSender` | Sent to the real recipient. |
| Anything else (Staging, typo...) | `LoggingEmailSender` → `FileTrapEmailSender` | Trapped. |

Also in this project: a `BackgroundService` that sends queued emails (so web requests don't wait for
SMTP), and a second one using `PeriodicTimer` that queues a digest every 30 seconds.

`GET /` shows the chain for the current environment, e.g.
`{"environment":"UAT","emailSenderChain":"LoggingEmailSender -> UatRedirectingEmailSender -> SmtpEmailSender"}`.

## Keyed DI registrations

```csharp
builder.Services.AddKeyedSingleton<IEmailSender, SmtpEmailSender>(EmailSenderKeyConstants.Smtp);   // key "smtp"
builder.Services.AddKeyedSingleton<IEmailSender, FileTrapEmailSender>(EmailSenderKeyConstants.Trap); // key "trap"
```

Two implementations of the **same** interface, told apart by a **key**. Without keys, registering
`IEmailSender` twice means "give me one" always returns the *last* one, and you can't pick.

Two ways to ask for a keyed one:
- In code: `serviceProvider.GetRequiredKeyedService<IEmailSender>("smtp")` (used in `Program.cs` to build the chain).
- As a parameter: `[FromKeyedServices(EmailSenderKeyConstants.Trap)] IEmailSender trapSender` — used by
  `POST /send-preview`, which **always** saves to the trap, even in Production, so someone can check what an
  email looks like without anybody receiving it.

```bash
curl -X POST "localhost:5073/send-preview?to=real.customer@gmail.com"
```

The keys are `const` strings in `Constants/EmailSenderKeyConstants.cs`, because attributes like
`[FromKeyedServices(...)]` only accept constants, and a typo in a constant is a compile error.

## Decorators

A **decorator** is a class that implements an interface **and wraps another object of the same interface**.
It adds something before/after, then passes the call on. The caller can't tell the difference.

```
IEmailSender sender = new LoggingEmailSender(          <- logs "sending", time taken, failures
                          new UatRedirectingEmailSender(   <- swaps the recipient for the UAT inbox
                              smtpSender,                  <- really sends
                              settings),
                          logger);
```

| Decorator | Adds | Wraps |
|---|---|---|
| `LoggingEmailSender` | "Sending … / Sent in N ms / failed" log lines; rethrows errors so callers still see them | **any** `IEmailSender` |
| `UatRedirectingEmailSender` | Changes the recipient to the UAT test inbox | **any** `IEmailSender` |

**Why decorators instead of putting logging inside each sender:** every sender would need the same logging
code (copied 3 times), and the UAT rule would have to be written into the SMTP sender. With decorators each
class does one job, and you **stack** the jobs you want per environment, in `Program.cs`, in one place.

**Why a factory registration (`AddSingleton<IEmailSender>(serviceProvider => ...)`) builds the chain:** DI
can't guess which sender goes inside which decorator, so our code decides and returns the finished chain.

## Why these isolation rules

The nightmare scenario: someone copies the production customer database into a test system, runs a
test, and 50,000 real customers get "TEST TEST ignore this" emails — or worse, fake invoices.

1. **Only `Production` can email real people.** The check is `IsProduction()` — an exact match. Every
   other value, including unknown names and typos, falls through to the trap. *Safe by default:*
   a mistake means "no email sent", never "email sent to a customer".
2. **The choice is made once, at startup, in one place** (`Program.cs`). The rest of the app only
   knows `IEmailSender`. No controller has `if (isProduction)` checks that someone could get wrong.
3. **UAT really sends, but only to a test inbox.** Testers need to see real emails in a real mail
   client (formatting, links, spam filters). Rewriting the *recipient* makes that safe even if UAT
   holds real customer addresses. The original address is kept in the subject so testers can check it.
4. **UAT refuses to start without a test inbox.** A missing setting stops the app with a clear error
   instead of silently sending to real addresses.
5. **Different SMTP hosts per environment** (`appsettings.UAT.json` overrides `SmtpHost`), so even a
   wrong sender class in UAT would hit the UAT mail server, not production's.
6. **Secrets (SMTP passwords) are not in appsettings.** Put them in environment variables or a secret
   store per environment, so a dev machine can't have production credentials at all.

## Why a background queue

SMTP can take seconds or time out. If a web request waited for it, the user would wait too, and a
slow mail server would make the whole site slow. The request just adds the email to an in-memory
`Channel` and returns `202 Accepted`. (In-memory means queued emails are lost on restart — a real
system would use a database table or a message queue.)

## Code snippets

**Keyed services: two implementations of one interface, told apart by a key**, from `Program.cs`

```csharp
builder.Services.AddKeyedSingleton<IEmailSender, SmtpEmailSender>(EmailSenderKeyConstants.Smtp);
builder.Services.AddKeyedSingleton<IEmailSender, FileTrapEmailSender>(EmailSenderKeyConstants.Trap);

// Ask for one by key in a controller action:
public async Task<IActionResult> SendPreview(string to, [FromKeyedServices(EmailSenderKeyConstants.Trap)] IEmailSender trapSender)
```

**Factory registration: pick the sender by environment and wrap it in decorators**

```csharp
builder.Services.AddSingleton<IEmailSender>(serviceProvider =>
{
    IEmailSender realSender;
    if (environment.IsProduction())
    {
        realSender = serviceProvider.GetRequiredKeyedService<IEmailSender>(EmailSenderKeyConstants.Smtp);
    }
    else if (environment.IsEnvironment("UAT"))
    {
        IEmailSender smtpSender = serviceProvider.GetRequiredKeyedService<IEmailSender>(EmailSenderKeyConstants.Smtp);
        realSender = new UatRedirectingEmailSender(smtpSender, emailSettings);
    }
    else
    {
        realSender = serviceProvider.GetRequiredKeyedService<IEmailSender>(EmailSenderKeyConstants.Trap);
    }
    return new LoggingEmailSender(realSender, serviceProvider.GetRequiredService<ILogger<LoggingEmailSender>>());
});
```

**A decorator: IS an `IEmailSender` and WRAPS another one**, from `Senders/UatRedirectingEmailSender.cs`

```csharp
public class UatRedirectingEmailSender : IEmailSender
{
    public UatRedirectingEmailSender(IEmailSender inner, EmailSettings settings)
    {
        Inner = inner;
        this.settings = settings;
    }

    public IEmailSender Inner { get; }

    public Task SendAsync(EmailMessage message)
    {
        var redirected = new EmailMessage(settings.UatTestInbox, $"[UAT - meant for {message.To}] {message.Subject}", message.Body);
        return Inner.SendAsync(redirected);
    }
}
```

**A settings file per environment**: `appsettings.UAT.json` is loaded on top of `appsettings.json` when `ASPNETCORE_ENVIRONMENT=UAT`

```json
{
  "Email": {
    "SmtpHost": "smtp.uat.roadmap-shop.example",
    "UatTestInbox": "uat-inbox@roadmap-shop.example"
  }
}
```

**Background queue: the controller returns at once, a `BackgroundService` sends**, from `Queues/EmailQueue.cs` and `BackgroundServices/EmailSendingBackgroundService.cs`

```csharp
private readonly Channel<EmailMessage> channel = Channel.CreateBounded<EmailMessage>(1000);

public ValueTask AddAsync(EmailMessage message)
{
    return channel.Writer.WriteAsync(message);
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (EmailMessage message in queue.ReadAllAsync(stoppingToken))
    {
        try
        {
            await sender.SendAsync(message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send email to {To}", message.To); // one bad email must not stop the service
        }
    }
}

builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailSendingBackgroundService>();
```

**A job that runs on a timer**, from `BackgroundServices/DailyDigestBackgroundService.cs`

```csharp
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await queue.AddAsync(new EmailMessage("team@client-company.com", "Digest", $"Status at {DateTime.Now:t}: all good."));
}
```

**Run in a chosen environment**

```bash
ASPNETCORE_ENVIRONMENT=UAT dotnet run
```

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `interface IEmailSender` | **The** reason interfaces exist: 3 implementations (trap, SMTP, UAT redirect), one picked at startup. The rest of the app only knows the interface. | Using `SmtpEmailSender` directly would need `if (environment...)` everywhere. |
| `UatRedirectingEmailSender` and `LoggingEmailSender` wrap **any** `IEmailSender` (decorators) | Each adds one job and passes the call on, so they can be stacked in any order per environment. | Inheriting from `SmtpEmailSender` would tie them to SMTP and couldn't be combined. |
| `record EmailMessage` | An email doesn't change once queued. The UAT sender makes a *new* one with the test inbox. | Changing the original's `To` could affect anyone else holding it. |
| `class EmailSettings` | Bound from config with setters. | — |
| `EmailQueue` wraps a `Channel<EmailMessage>` | Thread-safe hand-off between web requests (producers) and the background sender. | Injecting the raw `Channel` everywhere exposes its full API. |
| `IAsyncEnumerable<EmailMessage> ReadAllAsync` | Emails arrive over time; `await foreach` waits for each. | A `List` would need polling. |
| `ValueTask AddAsync` | `Channel.Writer.WriteAsync` returns `ValueTask`; usually completes instantly. | — |
| `: BackgroundService` (an abstract class) | The framework's base class does the start/stop plumbing; we fill in only `ExecuteAsync`. | Implementing `IHostedService` directly means writing that plumbing ourselves. |
| `PeriodicTimer` | `await timer.WaitForNextTickAsync()` — no overlapping runs, stops cleanly with the token. | `System.Threading.Timer` callbacks can overlap and don't support `await`. |
| `readonly` injected fields | Set once in the constructor. | — |
| `AddKeyedSingleton` + `const` keys | Two `IEmailSender`s that must be picked by name (`"smtp"`, `"trap"`). | Two plain registrations: only the last one could be asked for. |
| `[FromKeyedServices]` on one parameter | Only `/send-preview` needs the trap; the rest of the controller uses the normal sender. | Injecting the trap in the constructor would give every action access to it. |
