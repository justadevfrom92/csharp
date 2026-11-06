namespace ErrorHandlingAndCrashRecovery.Models;

// What the "Sorry, something went wrong" page shows. ONLY the id - never the exception message.
public class ServerErrorViewModel
{
    public string ErrorId { get; set; } = "";
}
