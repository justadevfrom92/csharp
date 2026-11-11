namespace EnvironmentAwareEmail.Constants;

// The KEYS used for the keyed DI registrations in Program.cs.
// const (not static readonly) because [FromKeyedServices(...)] is an attribute, and attributes need constants.
public static class EmailSenderKeyConstants
{
    public const string Smtp = "smtp";
    public const string Trap = "trap";
}
