namespace EnvironmentAwareEmail.Services.Email;

public record EmailMessage(string To, string Subject, string Body);
