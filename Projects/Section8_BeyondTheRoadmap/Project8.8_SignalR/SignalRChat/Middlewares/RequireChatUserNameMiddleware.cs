namespace SignalRChat.Middlewares;

// Checks ?user=<name> BEFORE a SignalR connection is made. Without this, a bad name could only be rejected
// inside the hub's OnConnectedAsync - but by then the client is already connected, and simply gets dropped.
// Answering 400 to the connection request means the client's StartAsync() fails with a clear reason instead.
public class RequireChatUserNameMiddleware
{
    public const int MaximumUserNameLength = 30;
    private readonly RequestDelegate next;

    public RequireChatUserNameMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/hubs/chat"))
        {
            string? userName = context.Request.Query["user"];
            if (string.IsNullOrWhiteSpace(userName) || userName.Trim().Length > MaximumUserNameLength)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"Connect with ?user=<your name> (1-{MaximumUserNameLength} characters).");
                return; // don't call next: the hub never sees this request
            }
        }
        await next(context);
    }
}
