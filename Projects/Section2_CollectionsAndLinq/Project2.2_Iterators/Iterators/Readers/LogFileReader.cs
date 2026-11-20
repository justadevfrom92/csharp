namespace Iterators.Readers;

// Reads a log file ONE LINE AT A TIME and hands out only the ERROR lines.
// A 10 GB file never sits in memory: only the current line does.
public static class LogFileReader
{
    // "finally" in an iterator runs when the loop ENDS - also when the caller stops early with "break",
    // because foreach calls Dispose() on the enumerator. "log" records open/close for the tests.
    public static IEnumerable<string> ReadErrors(string path, List<string>? log = null)
    {
        using StreamReader reader = File.OpenText(path);
        log?.Add("opened");
        try
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains("ERROR"))
                {
                    yield return line;
                }
            }
        }
        finally
        {
            log?.Add("closed");
        }
    }
}
