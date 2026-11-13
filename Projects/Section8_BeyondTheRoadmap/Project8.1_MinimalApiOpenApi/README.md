# Project 8.1 — Minimal APIs + OpenAPI

| Folder | What |
|---|---|
| `MinimalApiOpenApi/` | A books API written with Minimal APIs |
| `MinimalApiOpenApi.Tests/` | xUnit tests for every endpoint and for the OpenAPI document |

```bash
dotnet run --project MinimalApiOpenApi
```

```bash
curl localhost:5000/books/1
```

```bash
curl -H "X-Api-Key: demo-key-change-me" -H "Content-Type: application/json" -d '{"title":"Emma","author":"Jane Austen","year":1815}' localhost:5000/books
```

```bash
curl localhost:5000/openapi/v1.json
```

```bash
dotnet test MinimalApiOpenApi.Tests
```

## Minimal APIs vs controllers

Controllers (Projects 3.x, 6.x) put endpoints in classes with attributes. **Minimal APIs** map a URL straight to
a method: `app.MapGet("/books/{id:int}", GetBookById)`. Less ceremony, slightly faster, and the default for new
APIs in .NET. Controllers are still the better fit for MVC apps with views.

## What's in it

| Feature | Where | Why |
|---|---|---|
| `MapGroup("/books")` | `Endpoints/BookEndpoints.cs` | Every endpoint shares the `/books` prefix and the `Books` tag, written once |
| Endpoints in their own static class | `MapBookEndpoints()` extension | `Program.cs` stays a short list of steps |
| `TypedResults` + `Results<Ok<BookModel>, NotFound>` | each endpoint | The return type lists **every** possible answer: the compiler stops you returning anything else, and OpenAPI reads it to document 200 **and** 404 |
| `AddValidation()` (.NET 10) | `Program.cs` + attributes on `SaveBookRequestModel` | `[Required]`, `[StringLength]`, `[Range]` are checked **before** the endpoint runs; failures return 400 with every field's error |
| Endpoint filter | `Filters/ApiKeyEndpointFilter.cs` | Like middleware, but only on the endpoints that add it (`.AddEndpointFilter<...>()`): POST/DELETE need `X-Api-Key`, GETs don't |
| ProblemDetails | `AddProblemDetails()`, `UseExceptionHandler`, `UseStatusCodePages` | Every error is standard `application/problem+json` with `status`, `title`, `traceId` |
| OpenAPI | `AddOpenApi()` + `MapOpenApi()` | A machine-readable description at `/openapi/v1.json` that tools (Postman, client generators, Swagger UI) can read |

## A bug the tests found

The NegativeNegative test "malformed JSON → 400" **failed with 500**. In Development, Minimal APIs *throw* a
`BadHttpRequestException` for a body they can't read (so you see the details while debugging), and
`UseExceptionHandler()` turned every exception into a 500. The fix in `Program.cs`:

```csharp
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
```

Now bad JSON is a 400 in every environment (checked in Development by the test and in Production by hand).

## Tests (every behaviour has all three kinds)

| Behaviour | PositivePositive | PositiveNegative | NegativeNegative |
|---|---|---|---|
| GET `/books/{id}` | id 1 → the book | id 9999 → 404 `application/problem+json` | `/books/abc` → 404 |
| POST `/books` | valid → 201, `Location: /books/3`, readable afterwards | empty title + year 3000 → 400 with `Title` and `Year` errors; duplicate title → 409 | malformed JSON → 400; no API key → 401, nothing added |
| DELETE `/books/{id}` | → 204, then GET → 404 | unknown id → 404 | wrong API key → 401, book still there |
| OpenAPI document | lists `/books/{id}` with summary, 200 **and** 404 | doesn't list `/authors`, a `PUT`, or a 409 on GET | `/openapi/v2.json` → 404 |

Each test creates a **new** app, so the in-memory book list starts fresh and tests can't affect each other.

## Code snippets

**OpenAPI, automatic validation, ProblemDetails errors**, from `MinimalApiOpenApi/Program.cs`

```csharp
builder.Services.AddOpenApi();        // OpenAPI description of every endpoint
builder.Services.AddValidation();     // .NET 10: checks [Required], [Range]... automatically
builder.Services.AddProblemDetails(); // errors as standard "application/problem+json"

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.MapOpenApi();       // GET /openapi/v1.json
app.MapBookEndpoints();
```

**Group endpoints under one prefix, in their own file**, from `Endpoints/BookEndpoints.cs`

```csharp
public static RouteGroupBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
{
    RouteGroupBuilder books = app.MapGroup("/books").WithTags("Books");
    books.MapGet("/", GetAllBooks).WithSummary("List all books");
    books.MapGet("/{id:int}", GetBookById).WithSummary("Get one book by its id");
    books.MapPost("/", CreateBook).AddEndpointFilter<ApiKeyEndpointFilter>();
    return books;
}
```

**Typed results: the return type lists every possible answer**

```csharp
private static Results<Ok<BookModel>, NotFound> GetBookById(int id, BookStore store)
{
    BookModel? book = store.FindById(id);
    if (book == null)
    {
        return TypedResults.NotFound();
    }
    return TypedResults.Ok(book);
}

return TypedResults.Created($"/books/{book.Id}", book);
```

**Validation attributes on a record**, from `Models/SaveBookRequestModel.cs`

```csharp
public record SaveBookRequestModel(
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(100, MinimumLength = 1)] string Author,
    [property: Range(1450, 2100)] int Year);
```

**An endpoint filter: check an API key before the endpoint runs**, from `Filters/ApiKeyEndpointFilter.cs`

```csharp
public class ApiKeyEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        string? sentKey = context.HttpContext.Request.Headers[HeaderName];
        if (sentKey != expectedApiKey)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: $"Missing or wrong {HeaderName} header.");
        }
        return await next(context);
    }
}
```

**Call it**

```bash
curl -X POST http://localhost:5000/books -H "X-Api-Key: <key from appsettings.json>" -H "Content-Type: application/json" -d '{"title":"Dune","author":"Frank Herbert","year":1965}'
```
