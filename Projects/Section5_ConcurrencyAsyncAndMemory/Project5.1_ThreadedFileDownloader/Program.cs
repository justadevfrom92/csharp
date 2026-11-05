using System.Diagnostics;

// "Downloads" are simulated (sleep + write random bytes to a file) so the demo needs no internet.
string outputFolder = Path.Combine(Path.GetTempPath(), "threaded-downloads");
Directory.CreateDirectory(outputFolder);

// ---------- 1. Race condition: why we need locks at all ----------
int unsafeCounter = 0;
int interlockedCounter = 0;
var threads = new List<Thread>();
for (int i = 0; i < 4; i++)
{
    var thread = new Thread(() =>
    {
        for (int j = 0; j < 100_000; j++)
        {
            unsafeCounter++;                            // read, add, write: two threads can overlap and lose updates
            Interlocked.Increment(ref interlockedCounter); // one single atomic step: never loses updates
        }
    });
    threads.Add(thread);
    thread.Start();
}
foreach (Thread thread in threads)
{
    thread.Join(); // wait for it to finish
}
Console.WriteLine($"Expected 400000 -> unsafe: {unsafeCounter}, Interlocked: {interlockedCounter}");
Console.WriteLine();

// ---------- 2. The downloader ----------
var downloader = new FileDownloader(outputFolder, maximumDownloadsAtOnce: 3);

// Events & delegates: subscribe a method to run whenever a download finishes.
downloader.DownloadFinished += (sender, eventArgs) =>
{
    Console.WriteLine($"  [event] {eventArgs.FileName} done: {eventArgs.SizeInBytes:N0} bytes on thread {eventArgs.ThreadId}");
};

string[] fileNames = Enumerable.Range(1, 10).Select(number => $"file{number:00}.bin").ToArray();
var timer = Stopwatch.StartNew();

// 2a. ThreadPool: reuse existing threads instead of creating new ones.
using var allDone = new CountdownEvent(fileNames.Length);
foreach (string fileName in fileNames)
{
    ThreadPool.QueueUserWorkItem(_ =>
    {
        downloader.Download(fileName);
        allDone.Signal();
    });
}

// 2b. A background thread that prints progress until the downloads are done.
var progressThread = new Thread(() =>
{
    while (!allDone.IsSet)
    {
        Console.WriteLine($"  progress: {downloader.FinishedCount}/{fileNames.Length} files, running now: {downloader.RunningNowCount}");
        Thread.Sleep(300);
    }
});
progressThread.IsBackground = true; // a background thread won't keep the app alive if Main ends
progressThread.Start();

allDone.Wait();
progressThread.Join();
timer.Stop();

Console.WriteLine();
Console.WriteLine($"Downloaded {downloader.FinishedCount} files, {downloader.TotalBytes:N0} bytes in {timer.ElapsedMilliseconds} ms");
Console.WriteLine($"Most downloads running at the same time: {downloader.MostRunningAtOnce} (limit was 3)");
Console.WriteLine($"Log lines written: {downloader.GetLog().Count}");

// ---------- 3. Func, Action, Predicate ----------
Func<long, string> formatSize = bytes => $"{bytes / 1024.0:0.0} KB";
Predicate<string> isEvenFile = name => int.Parse(name.Substring(4, 2)) % 2 == 0;
Action<string> printLine = text => Console.WriteLine("  " + text);

Console.WriteLine();
Console.WriteLine("Even-numbered files:");
foreach (string fileName in Array.FindAll(fileNames, isEvenFile))
{
    printLine($"{fileName} {formatSize(new FileInfo(Path.Combine(outputFolder, fileName)).Length)}");
}

// ---------- 4. Mutex: a lock that also works between different PROGRAMS ----------
using var mutex = new Mutex(false, "Global-Roadmap-ThreadedFileDownloader");
if (mutex.WaitOne(TimeSpan.Zero))
{
    Console.WriteLine();
    Console.WriteLine("Got the mutex: no other copy of this program is running right now.");
    mutex.ReleaseMutex();
}

public class DownloadFinishedEventArgs : EventArgs
{
    public DownloadFinishedEventArgs(string fileName, long sizeInBytes, int threadId)
    {
        FileName = fileName;
        SizeInBytes = sizeInBytes;
        ThreadId = threadId;
    }

    public string FileName { get; }
    public long SizeInBytes { get; }
    public int ThreadId { get; }
}

public class FileDownloader
{
    private readonly string outputFolder;
    private readonly SemaphoreSlim downloadSlots;          // limits how many downloads run at once
    private readonly object logLock = new();                // protects the log list
    private readonly List<string> log = new();
    private int finishedCount;
    private int runningNowCount;
    private int mostRunningAtOnce;
    private long totalBytes;

    public FileDownloader(string outputFolder, int maximumDownloadsAtOnce)
    {
        this.outputFolder = outputFolder;
        downloadSlots = new SemaphoreSlim(maximumDownloadsAtOnce);
    }

    public event EventHandler<DownloadFinishedEventArgs>? DownloadFinished;

    // Volatile.Read makes sure we see the latest value written by any thread.
    public int FinishedCount
    {
        get
        {
            return Volatile.Read(ref finishedCount);
        }
    }

    public int RunningNowCount
    {
        get
        {
            return Volatile.Read(ref runningNowCount);
        }
    }

    public int MostRunningAtOnce
    {
        get
        {
            return Volatile.Read(ref mostRunningAtOnce);
        }
    }

    public long TotalBytes
    {
        get
        {
            return Interlocked.Read(ref totalBytes);
        }
    }

    public void Download(string fileName)
    {
        downloadSlots.Wait(); // wait here if 3 downloads are already running
        try
        {
            int runningNow = Interlocked.Increment(ref runningNowCount);
            RememberHighestRunningCount(runningNow);

            // Simulate the download: wait a bit, then write some bytes.
            Thread.Sleep(Random.Shared.Next(200, 600));
            byte[] data = new byte[Random.Shared.Next(10_000, 50_000)];
            Random.Shared.NextBytes(data);
            File.WriteAllBytes(Path.Combine(outputFolder, fileName), data);

            Interlocked.Add(ref totalBytes, data.Length);
            Interlocked.Increment(ref finishedCount);

            lock (logLock) // List<T> is not thread-safe, so only one thread may add at a time
            {
                log.Add($"{DateTime.Now:HH:mm:ss.fff} {fileName} {data.Length} bytes");
            }

            DownloadFinished?.Invoke(this, new DownloadFinishedEventArgs(fileName, data.Length, Environment.CurrentManagedThreadId));
        }
        finally
        {
            Interlocked.Decrement(ref runningNowCount);
            downloadSlots.Release(); // ALWAYS give the slot back, even if the download failed
        }
    }

    public List<string> GetLog()
    {
        lock (logLock)
        {
            return log.ToList(); // return a copy, so callers can't touch the list without the lock
        }
    }

    private void RememberHighestRunningCount(int runningNow)
    {
        // Compare-and-swap loop: only replace the value if nobody changed it in the meantime.
        int currentHighest = Volatile.Read(ref mostRunningAtOnce);
        while (runningNow > currentHighest)
        {
            int valueBeforeSwap = Interlocked.CompareExchange(ref mostRunningAtOnce, runningNow, currentHighest);
            if (valueBeforeSwap == currentHighest)
            {
                return;
            }
            currentHighest = valueBeforeSwap;
        }
    }
}
