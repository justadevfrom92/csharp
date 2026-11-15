using System.Threading.Channels;

// What happens when a bounded channel is FULL? Write 1..10 into a channel that holds 3, with nobody reading,
// then read what's left. Same channel, different BoundedChannelFullMode.
public static class ChannelFullModeVariation
{
    public static List<int> WriteTenThenRead(BoundedChannelFullMode fullMode)
    {
        Channel<int> channel = Channel.CreateBounded<int>(new BoundedChannelOptions(3) { FullMode = fullMode });
        for (int item = 1; item <= 10; item++)
        {
            channel.Writer.TryWrite(item); // with Wait, TryWrite returns false when full (WriteAsync would WAIT instead)
        }
        return ReadAll(channel);
    }

    // No limit: nothing is ever dropped or waited for - and nothing stops memory growing if the reader is slow.
    public static List<int> UnboundedWriteTenThenRead()
    {
        Channel<int> channel = Channel.CreateUnbounded<int>();
        for (int item = 1; item <= 10; item++)
        {
            channel.Writer.TryWrite(item);
        }
        return ReadAll(channel);
    }

    private static List<int> ReadAll(Channel<int> channel)
    {
        channel.Writer.Complete();
        var items = new List<int>();
        while (channel.Reader.TryRead(out int item))
        {
            items.Add(item);
        }
        return items;
    }
}
