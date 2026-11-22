using System.Reflection;

namespace Playground.Services;

// Runs a console project's REAL Program in the browser and returns everything it printed.
// How: Console.Out/Error go to a StringWriter, then the program's entry point is called by reflection.
// (Console.In can't be replaced in a browser, so programs that wait for typed input can't run here.)
public class ProgramRunnerService
{
    private bool running;

    public async Task<string> RunAsync(string assemblyName, string[] args)
    {
        if (running)
        {
            return "Another program is still running.";
        }
        running = true;

        var output = new StringWriter();
        TextWriter originalOut = Console.Out;
        TextWriter originalError = Console.Error;
        Console.SetOut(output);
        Console.SetError(output);
        try
        {
            MethodInfo method = FindMain(Assembly.Load(assemblyName));
            object?[] parameters = method.GetParameters().Length == 1 ? [args] : [];
            object? result = method.Invoke(null, parameters);
            if (result is Task task)
            {
                await task;
            }
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            output.WriteLine();
            output.WriteLine($"Unhandled {error.InnerException.GetType().Name}: {error.InnerException.Message}");
        }
        catch (Exception error)
        {
            output.WriteLine();
            output.WriteLine($"Unhandled {error.GetType().Name}: {error.Message}");
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            running = false;
        }
        return output.ToString();
    }

    // Top-level statements with "await" compile to "<Main>$" (returns Task) plus a small entry point that
    // BLOCKS on it with .GetAwaiter().GetResult(). The browser has one thread, so blocking would freeze it:
    // call "<Main>$" and await it instead.
    private static MethodInfo FindMain(Assembly assembly)
    {
        MethodInfo entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException($"{assembly.GetName().Name} has no Main.");
        MethodInfo? asyncMain = entryPoint.DeclaringType?.GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        return asyncMain ?? entryPoint;
    }
}
