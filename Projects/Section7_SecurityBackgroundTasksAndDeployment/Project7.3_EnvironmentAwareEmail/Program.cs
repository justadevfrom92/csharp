using EnvironmentAwareEmail.BackgroundServices;
using EnvironmentAwareEmail.Queues;
using EnvironmentAwareEmail.Senders;
using EnvironmentAwareEmail.Settings;

var builder = WebApplication.CreateBuilder(args);

var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);

// Pick the email sender based on the environment (ASPNETCORE_ENVIRONMENT).
IWebHostEnvironment environment = builder.Environment;
if (environment.IsProduction())
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else if (environment.IsEnvironment("UAT"))
{
    if (string.IsNullOrWhiteSpace(emailSettings.UatTestInbox))
    {
        throw new InvalidOperationException("UAT needs Email:UatTestInbox, or real customers could get test emails.");
    }
    builder.Services.AddSingleton<SmtpEmailSender>();
    builder.Services.AddSingleton<IEmailSender, UatRedirectingEmailSender>();
}
else
{
    // Development, Staging, Testing, anything unknown -> trap. Safe by default.
    builder.Services.AddSingleton<IEmailSender, FileTrapEmailSender>();
}

builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailSendingBackgroundService>();
builder.Services.AddHostedService<DailyDigestBackgroundService>();
builder.Services.AddControllers();

var app = builder.Build();
app.Logger.LogInformation("Environment: {Environment}", environment.EnvironmentName);
app.MapControllers();
app.Run();
