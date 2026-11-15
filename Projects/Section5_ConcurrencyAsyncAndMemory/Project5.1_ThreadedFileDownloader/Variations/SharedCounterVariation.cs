// Four threads each add 1 to a shared counter 100,000 times. Expected total: 400,000.
// Which ways of adding are SAFE when threads run at the same time?
public static class SharedCounterVariation
{
    private const int ThreadCount = 4;
    private const int AddsPerThread = 100_000;

    // UNSAFE: ++ is read, add, write. Two threads can read the same value and one update is LOST.
    public static int WithPlusPlus()
    {
        int counter = 0;
        RunOnThreads(() => counter++);
        return counter;
    }

    // ORIGINAL: Interlocked.Increment - one atomic CPU instruction. Fastest safe way for a single number.
    public static int WithInterlocked()
    {
        int counter = 0;
        RunOnThreads(() => Interlocked.Increment(ref counter));
        return counter;
    }

    // lock: also safe; needed when SEVERAL values must change together. Slower than Interlocked for one number.
    public static int WithLock()
    {
        int counter = 0;
        object counterLock = new object();
        RunOnThreads(() =>
        {
            lock (counterLock)
            {
                counter++;
            }
        });
        return counter;
    }

    // System.Threading.Lock (.NET 9+): a dedicated lock TYPE. Same "lock (x) { }" syntax, a bit faster,
    // and you can't accidentally lock on something else (like a string or "this").
    public static int WithLockType()
    {
        int counter = 0;
        Lock counterLock = new Lock();
        RunOnThreads(() =>
        {
            lock (counterLock)
            {
                counter++;
            }
        });
        return counter;
    }

    // Volatile.Write(Volatile.Read + 1): STILL UNSAFE. Volatile makes each read/write see the latest value,
    // but read-then-write is still two steps another thread can squeeze between. A common misunderstanding.
    public static int WithVolatile()
    {
        int counter = 0;
        RunOnThreads(() => Volatile.Write(ref counter, Volatile.Read(ref counter) + 1));
        return counter;
    }

    private static void RunOnThreads(Action add)
    {
        var threads = new List<Thread>();
        for (int i = 0; i < ThreadCount; i++)
        {
            var thread = new Thread(() =>
            {
                for (int j = 0; j < AddsPerThread; j++)
                {
                    add();
                }
            });
            threads.Add(thread);
            thread.Start();
        }
        foreach (Thread thread in threads)
        {
            thread.Join();
        }
    }
}
