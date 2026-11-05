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
