using System.Collections.Concurrent;
using System.Threading.Channels;

// One producer, one consumer, 1,000 items - with three different queues. All deliver every item in order;
// they differ in HOW the consumer waits for the next one.
public static class ProducerConsumerVariation
{
    // ORIGINAL: Channel - the consumer AWAITS (no thread blocked while waiting).
    public static async Task<long> WithChannel()
    {
        Channel<int> channel = Channel.CreateBounded<int>(100);
        Task producer = Task.Run(async () =>
        {
            for (int item = 1; item <= 1000; item++)
            {
                await channel.Writer.WriteAsync(item);
            }
            channel.Writer.Complete();
        });
        long sum = 0;
        await foreach (int item in channel.Reader.ReadAllAsync())
        {
            sum += item;
        }
        await producer;
        return sum;
    }

    // BlockingCollection (older, .NET 4): the consumer BLOCKS a thread while waiting. Fine in a dedicated thread,
    // wasteful in async code. Channel replaced it for new code.
    public static long WithBlockingCollection()
    {
        using var queue = new BlockingCollection<int>(boundedCapacity: 100);
        Task producer = Task.Run(() =>
        {
            for (int item = 1; item <= 1000; item++)
            {
                queue.Add(item);
            }
            queue.CompleteAdding();
        });
        long sum = 0;
        foreach (int item in queue.GetConsumingEnumerable())
        {
            sum += item;
        }
        producer.Wait();
        return sum;
    }

    // ConcurrentQueue: thread-safe, but has NO "wait for the next item" and NO size limit - the consumer must
    // poll (check again and again), and the producer is never slowed down (no backpressure).
    public static long WithConcurrentQueue()
    {
        var queue = new ConcurrentQueue<int>();
        bool producerDone = false;
        Task producer = Task.Run(() =>
        {
            for (int item = 1; item <= 1000; item++)
            {
                queue.Enqueue(item);
            }
            Volatile.Write(ref producerDone, true);
        });
        long sum = 0;
        while (!Volatile.Read(ref producerDone) || !queue.IsEmpty)
        {
            if (queue.TryDequeue(out int item))
            {
                sum += item;
            }
            else
            {
                Thread.Yield(); // nothing yet: try again (busy-waiting)
            }
        }
        producer.Wait();
        return sum;
    }
}
