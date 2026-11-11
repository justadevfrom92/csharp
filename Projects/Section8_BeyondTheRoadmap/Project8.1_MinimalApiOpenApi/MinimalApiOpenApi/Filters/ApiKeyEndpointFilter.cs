namespace MinimalApiOpenApi.Filters;

// An ENDPOINT FILTER: runs before (and after) the endpoint it's attached to - like middleware, but only for
// the endpoints that ask for it (.AddEndpointFilter<ApiKeyEndpointFilter>()).
// Write endpoints need the header "X-Api-Key: <key from appsettings.json>"; reading is open to everyone.
public class ApiKeyEndpointFilter : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";
    private readonly string expectedApiKey;

    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        expectedApiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is missing from configuration.");
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        string? sentKey = context.HttpContext.Request.Headers[HeaderName];
        if (sentKey != expectedApiKey)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: $"Missing or wrong {HeaderName} header.");
        }
        return await next(context); // key is fine: run the endpoint
    }
}
