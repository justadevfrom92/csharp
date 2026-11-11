namespace OutputCachingCatalog.Middlewares;

// CONVENTION-BASED middleware (recognised by its shape: constructor with RequestDelegate + InvokeAsync).
// Adds "X-Full-Pipeline: true" to every request that runs the whole pipeline.
// A short-circuited request (like /robots.txt) never gets here, so it has no such header - that's how the
// tests prove .ShortCircuit() works.
public class FullPipelineHeaderMiddleware
{
    private readonly RequestDelegate next;

    public FullPipelineHeaderMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Full-Pipeline"] = "true";
        await next(context);
    }
}
