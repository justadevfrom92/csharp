using Iterators.Readers;

namespace Iterators.Tests;

public class LogFileReaderTest : IDisposable
{
    private readonly string path = Path.GetTempFileName();

    public LogFileReaderTest()
    {
        File.WriteAllLines(path, ["INFO start", "ERROR disk full", "INFO retry", "ERROR timeout"]);
    }

    public void Dispose()
    {
        File.Delete(path);
    }

    [Fact]
    public void ReadErrors_PositivePositive_OnlyErrorLines_FileClosedAtTheEnd()
    {
        var log = new List<string>();

        List<string> errors = LogFileReader.ReadErrors(path, log).ToList();

        Assert.Equal(["ERROR disk full", "ERROR timeout"], errors);
        Assert.Equal(["opened", "closed"], log);
    }

    [Fact]
    public void ReadErrors_PositiveNegative_BreakEarly_StillCloses_ButManualMoveNextWithoutDispose_DoesNot()
    {
        var foreachLog = new List<string>();
        foreach (string _ in LogFileReader.ReadErrors(path, foreachLog))
        {
            break;
        }

        var manualLog = new List<string>();
        IEnumerator<string> cursor = LogFileReader.ReadErrors(path, manualLog).GetEnumerator();
        cursor.MoveNext();

        Assert.Equal(["opened", "closed"], foreachLog);  // foreach calls Dispose()
        Assert.Equal(["opened"], manualLog);             // file still open until Dispose()
        cursor.Dispose();
        Assert.Equal(["opened", "closed"], manualLog);
    }

    [Fact]
    public void ReadErrors_NegativeNegative_MissingFile_ThrowsOnlyWhenLooped()
    {
        IEnumerable<string> errors = LogFileReader.ReadErrors("/no/such/file.log"); // no error yet

        Assert.Throws<DirectoryNotFoundException>(() => errors.ToList());
    }
}
