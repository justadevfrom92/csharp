namespace FormSecurityXssCsrf.Middlewares;

// A CONVENTION-BASED middleware class. ASP.NET Core finds the parts by their shape (no interface needed):
//   - a constructor that takes RequestDelegate next   -> "the rest of the pipeline"
//   - a public InvokeAsync(HttpContext) method          -> runs for every request
// It's created ONCE when the app starts (like a singleton), so it must not keep per-request data in fields.
// Registered in Program.cs with app.UseMiddleware<SecurityHeadersMiddleware>().
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Work BEFORE the rest of the pipeline: add the headers (they must be set before the body starts).
        // XSS safety net: only run scripts/styles from our own site; no inline <script>.
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff"; // don't guess file types
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        // 2. Call the next middleware (and eventually the controller).
        await next(context);

        // 3. Anything here would run AFTER the response was produced (nothing needed for headers).
    }
}
