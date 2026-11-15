using EnvironmentAwareEmail.Senders;
using EnvironmentAwareEmail.Settings;

namespace EnvironmentAwareEmail.Variations;

// Other ways to choose the email sender per environment. Program.cs (original) uses keyed services + a factory
// that builds a decorator chain. These are alternatives you could put in Program.cs instead (not called).
public static class SenderRegistrationVariation
{
    // Plain if/else on the environment, one implementation each, no decorators: simplest, but the logging and
    // UAT redirection would have to be copied into every sender.
    public static void WithIfElse(IServiceCollection services, IWebHostEnvironment environment)
    {
        if (environment.IsProduction())
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, FileTrapEmailSender>();
        }
    }

    // A factory that news up the chain by hand: the decorators are visible in one expression, but every
    // constructor argument (settings, loggers) must be passed manually - DI doesn't fill them in.
    public static void WithNewInFactory(IServiceCollection services, EmailSettings settings)
    {
        services.AddSingleton<IEmailSender>(serviceProvider => new LoggingEmailSender(
            new UatRedirectingEmailSender(serviceProvider.GetRequiredService<SmtpEmailSender>(), settings),
            serviceProvider.GetRequiredService<ILogger<LoggingEmailSender>>()));
    }
}
