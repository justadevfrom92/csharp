using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace PipelinePatterns.Interceptors;

// INTERCEPTOR / AOP (Aspect-Oriented Programming): add a "cross-cutting concern" (logging, timing,
// security, caching...) to EVERY method of an interface WITHOUT writing it in each method and without
// writing a decorator class per interface.
//
// DispatchProxy (built into .NET) creates a class at runtime that implements T. Every call to any
// method of that object comes to Invoke() below, which logs it and then calls the real object.
//
//   IStockService stock = LoggingInterceptor<IStockService>.Wrap(new StockService(), log);
//   stock.Reserve("KB-01", 2);   // -> log: "Reserve(KB-01, 2) ok in 0 ms"
//
// Compare a decorator (above): one class per interface, written by hand, can change behaviour per method.
// An interceptor: ONE class for ANY interface, but it only sees "a method was called with these arguments".
// Note: only calls made THROUGH the proxy are seen. StockService.Reserve calling its own GetStock is not logged.
public class LoggingInterceptor<T> : DispatchProxy where T : class
{
    private T target = null!;
    private List<string> log = null!;

    // DispatchProxy needs a public parameterless constructor, so setup happens here instead.
    public static T Wrap(T target, List<string> log)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(log);
        T proxy = Create<T, LoggingInterceptor<T>>();
        var interceptor = (LoggingInterceptor<T>)(object)proxy;
        interceptor.target = target;
        interceptor.log = log;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        string call = $"{targetMethod.Name}({string.Join(", ", args ?? [])})";
        var timer = Stopwatch.StartNew();
        try
        {
            object? result = targetMethod.Invoke(target, args);  // the real method
            log.Add($"{call} ok in {timer.ElapsedMilliseconds} ms" + (result == null ? "" : $" -> {result}"));
            return result;
        }
        catch (TargetInvocationException wrapped) when (wrapped.InnerException != null)
        {
            // Reflection wraps the real exception; unwrap it so callers see the ORIGINAL exception type.
            // ExceptionDispatchInfo re-throws it WITH its original stack trace ("throw inner;" would lose it).
            log.Add($"{call} FAILED: {wrapped.InnerException.Message}");
            ExceptionDispatchInfo.Capture(wrapped.InnerException).Throw();
            throw; // never reached; the compiler needs it
        }
    }
}
