using MinimalApiOpenApi.Endpoints;
using MinimalApiOpenApi.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<BookStore>();
builder.Services.AddOpenApi();        // builds an OpenAPI (Swagger) description of every endpoint
builder.Services.AddValidation();     // .NET 10: checks [Required], [Range]... on endpoint parameters automatically
builder.Services.AddProblemDetails(); // errors are returned as standard "application/problem+json"

var app = builder.Build();

// A crash becomes a ProblemDetails response, never a stack trace.
// StatusCodeSelector: a BadHttpRequestException (e.g. unreadable JSON) already knows it's a 400 - keep that
// status instead of turning every exception into a 500.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();    // an empty 404/405 gets a ProblemDetails body too

app.MapOpenApi();            // GET /openapi/v1.json
app.MapBookEndpoints();      // GET/POST/DELETE /books...

app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
