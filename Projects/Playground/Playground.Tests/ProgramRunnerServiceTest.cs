using Playground.Catalogs;
using Playground.Services;

namespace Playground.Tests;

// The runner calls a project's real Program and captures what it prints - the same thing the web page shows.
public class ProgramRunnerServiceTest
{
    [Fact]
    public async Task Run_PositivePositive_CapturesTheProgramsOutput()
    {
        string output = await new ProgramRunnerService().RunAsync("Iterators", []);

        Assert.Contains("1. After Generate(3): 0 lines ran", output);
        Assert.Contains("5. Shelf starting with C: Coffee, Cocoa", output);
    }

    [Fact]
    public async Task Run_PositiveNegative_AsyncProgramIsAwaited_AndArgsArePassed()
    {
        var runner = new ProgramRunnerService();

        string workflow = await runner.RunAsync("WorkflowEngine", []);    // top-level "await": runs "<Main>$"
        string loan = await runner.RunAsync("SimpleLoanAndDateCalculator", ["--demo"]);

        Assert.Contains("Cid was charged ONCE", workflow);
        Assert.Contains("=== Loan & Date Calculator ===", loan);
        Assert.DoesNotContain("Unhandled", loan);
    }

    [Fact]
    public async Task Run_NegativeNegative_UnknownProject_IsReportedNotThrown()
    {
        string output = await new ProgramRunnerService().RunAsync("NoSuchProject", []);

        Assert.Contains("Unhandled FileNotFoundException", output);
    }

    [Fact]
    public void Catalog_PositivePositive_EveryEntryIsAReferencedProgram()
    {
        foreach (var project in ProjectCatalog.Projects)
        {
            Assert.NotNull(System.Reflection.Assembly.Load(project.AssemblyName).EntryPoint);
        }
    }

    [Fact]
    public void Catalog_PositiveNegative_NumbersAreUnique_AndInRoadmapOrder()
    {
        List<string> numbers = ProjectCatalog.Projects.Select(project => project.Number).ToList();

        Assert.Equal(numbers.Distinct(), numbers);
        Assert.Equal(numbers.OrderBy(number => decimal.Parse(number, System.Globalization.CultureInfo.InvariantCulture)), numbers);
    }

    [Fact]
    public void Catalog_NegativeNegative_NoServerOrThreadedProjectIsListed()
    {
        string[] cannotRunInABrowser = ["ThreadedFileDownloader", "Benchmarks", "TransactionalCheckout", "MultiTenancy", "AsyncStreams"];

        Assert.DoesNotContain(ProjectCatalog.Projects, project => cannotRunInABrowser.Contains(project.AssemblyName));
    }
}
