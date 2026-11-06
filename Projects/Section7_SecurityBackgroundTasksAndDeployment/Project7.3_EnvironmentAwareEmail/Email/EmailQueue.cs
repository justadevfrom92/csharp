using System.Threading.Channels;

namespace EnvironmentAwareEmail.Email;

// Controllers drop emails here and return at once. A background service sends them.
public class EmailQueue
{
    private readonly Channel<EmailMessage> channel = Channel.CreateBounded<EmailMessage>(1000);

    public ValueTask AddAsync(EmailMessage message)
    {
        return channel.Writer.WriteAsync(message);
    }

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken stopToken)
    {
        return channel.Reader.ReadAllAsync(stopToken);
    }
}
