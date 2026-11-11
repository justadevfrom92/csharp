using EnvironmentAwareEmail.BackgroundServices;
using EnvironmentAwareEmail.Constants;
using EnvironmentAwareEmail.Queues;
using EnvironmentAwareEmail.Senders;
using EnvironmentAwareEmail.Settings;

var builder = WebApplication.CreateBuilder(args);

var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);

// ---------- KEYED registrations ----------
// Two implementations of the SAME interface, told apart by a key.
// Ask for one with [FromKeyedServices("trap")] or serviceProvider.GetRequiredKeyedService<IEmailSender>("trap").
builder.Services.AddKeyedSingleton<IEmailSender, SmtpEmailSender>(EmailSenderKeyConstants.Smtp);
builder.Services.AddKeyedSingleton<IEmailSender, FileTrapEmailSender>(EmailSenderKeyConstants.Trap);

// ---------- The app's normal IEmailSender: a keyed sender wrapped in DECORATORS ----------
// Picked by environment (ASPNETCORE_ENVIRONMENT):
//   Production  : LoggingEmailSender -> SmtpEmailSender
//   UAT         : LoggingEmailSender -> UatRedirectingEmailSender -> SmtpEmailSender
//   anything else (Development, Staging, typos): LoggingEmailSender -> FileTrapEmailSender   (safe by default)
IWebHostEnvironment environment = builder.Environment;
if (environment.IsEnvironment("UAT") && string.IsNullOrWhiteSpace(emailSettings.UatTestInbox))
{
    throw new InvalidOperationException("UAT needs Email:UatTestInbox, or real customers could get test emails.");
}

// FACTORY registration: our code builds the chain, because DI can't guess which sender goes inside which decorator.
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

builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailSendingBackgroundService>();
builder.Services.AddHostedService<DailyDigestBackgroundService>();
builder.Services.AddControllers();

var app = builder.Build();
app.Logger.LogInformation("Environment: {Environment}", environment.EnvironmentName);
app.MapControllers();
app.Run();
