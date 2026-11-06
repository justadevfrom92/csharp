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
| `Development` | `FileTrapEmailSender` | Saved to a `.eml` file. Never leaves the machine. |
| `UAT` | `UatRedirectingEmailSender` | Really sent, but **always** to `Email:UatTestInbox`. Subject shows who it was meant for. |
| `Production` | `SmtpEmailSender` | Sent to the real recipient. |
| Anything else (Staging, typo...) | `FileTrapEmailSender` | Trapped. |

Also in this project: a `BackgroundService` that sends queued emails (so web requests don't wait for
SMTP), and a second one using `PeriodicTimer` that queues a digest every 30 seconds.

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

## Why these types

General rules: [WHY_THESE_TYPES.md](../../WHY_THESE_TYPES.md). Choices made in this project:

| Choice | Why | Why not the alternative |
|---|---|---|
| `interface IEmailSender` | **The** reason interfaces exist: 3 implementations (trap, SMTP, UAT redirect), one picked at startup. The rest of the app only knows the interface. | Using `SmtpEmailSender` directly would need `if (environment...)` everywhere. |
| `UatRedirectingEmailSender` *wraps* `SmtpEmailSender` | It changes the recipient, then hands off to the real sender (composition). | Inheriting from `SmtpEmailSender` would tie it to SMTP details and make it easy to call the base send by mistake. |
| `record EmailMessage` | An email doesn't change once queued. The UAT sender makes a *new* one with the test inbox. | Changing the original's `To` could affect anyone else holding it. |
| `class EmailSettings` | Bound from config with setters. | — |
| `EmailQueue` wraps a `Channel<EmailMessage>` | Thread-safe hand-off between web requests (producers) and the background sender. | Injecting the raw `Channel` everywhere exposes its full API. |
| `IAsyncEnumerable<EmailMessage> ReadAllAsync` | Emails arrive over time; `await foreach` waits for each. | A `List` would need polling. |
| `ValueTask AddAsync` | `Channel.Writer.WriteAsync` returns `ValueTask`; usually completes instantly. | — |
| `: BackgroundService` (an abstract class) | The framework's base class does the start/stop plumbing; we fill in only `ExecuteAsync`. | Implementing `IHostedService` directly means writing that plumbing ourselves. |
| `PeriodicTimer` | `await timer.WaitForNextTickAsync()` — no overlapping runs, stops cleanly with the token. | `System.Threading.Timer` callbacks can overlap and don't support `await`. |
| `readonly` injected fields | Set once in the constructor. | — |
