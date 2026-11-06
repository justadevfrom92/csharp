namespace EnvironmentAwareEmail.Settings;

public class EmailSettings
{
    public string FromAddress { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 25;
    public string TrapFolder { get; set; } = "sent-emails";
    public string UatTestInbox { get; set; } = "";
}
