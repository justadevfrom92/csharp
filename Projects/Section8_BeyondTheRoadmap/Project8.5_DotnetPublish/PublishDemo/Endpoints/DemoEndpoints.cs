using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PublishDemo.Models;

namespace PublishDemo.Endpoints;

public static class DemoEndpoints
{
    public const int MaximumNameLength = 50;

    public static void MapDemoEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /info - shows how this copy of the app was published and is running.
        app.MapGet("/info", () => TypedResults.Ok(new RuntimeInfoModel(
            Framework: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            IsNativeAot: !RuntimeFeature.IsDynamicCodeSupported, // Native AOT can't generate code at runtime
            ExecutablePath: Environment.ProcessPath ?? "unknown")));

        // GET /greet?name=Ada
        app.MapGet("/greet", (string? name) =>
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Results.Problem(statusCode: 400, title: "Send a name, e.g. /greet?name=Ada");
            }
            if (name.Length > MaximumNameLength)
            {
                return Results.Problem(statusCode: 400, title: $"Names can be at most {MaximumNameLength} characters.");
            }
            return Results.Ok(new GreetingModel($"Hello, {name.Trim()}!"));
        });

        app.MapGet("/health", () => "Healthy");
    }
}
