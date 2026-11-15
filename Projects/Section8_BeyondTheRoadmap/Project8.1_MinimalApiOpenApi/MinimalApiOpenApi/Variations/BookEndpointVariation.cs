using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiOpenApi.Models;
using MinimalApiOpenApi.Stores;

namespace MinimalApiOpenApi.Variations;

// "Get one book" written five ways. BookEndpoints.GetBookById (original) uses a static method returning
// Results<Ok<BookModel>, NotFound>. Mapped under /variations/books/... (see Program.cs).
public static class BookEndpointVariation
{
    public static void MapBookVariations(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/variations/books").WithTags("Variations");

        // INLINE LAMBDA with TypedResults: same behaviour and same OpenAPI document as the original - the logic
        // just lives in Program-style code instead of a named method (harder to unit test, fine for 2 lines).
        group.MapGet("/lambda/{id:int}", Results<Ok<BookModel>, NotFound> (int id, BookStore store) =>
            store.FindById(id) is BookModel book ? TypedResults.Ok(book) : TypedResults.NotFound());

        // Results.Ok / Results.NotFound (no "Typed") returning IResult: same HTTP answers, but the return type
        // is just "IResult", so OpenAPI can't see which status codes are possible - the document loses the 404.
        group.MapGet("/iresult/{id:int}", IResult (int id, BookStore store) =>
        {
            BookModel? book = store.FindById(id);
            return book == null ? Results.NotFound() : Results.Ok(book);
        });

        // Returning the object itself: always 200. A missing book is sent as 200 with the body "null"
        // instead of a 404 - clients can't tell "not found" from "found".
        group.MapGet("/plain/{id:int}", (int id, BookStore store) => store.FindById(id));

        // [AsParameters]: the route value, query value and service arrive as ONE object.
        group.MapGet("/as-parameters/{id:int}", Results<Ok<string>, NotFound> ([AsParameters] BookLookupRequestVariation request) =>
        {
            BookModel? book = request.Store.FindById(request.Id);
            if (book == null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(request.Uppercase ? book.Title.ToUpperInvariant() : book.Title);
        });

        // Throwing for "not found": the exception handler turns it into a 500 - wrong status, and exceptions
        // for a normal answer are slow. Shown so you recognise it.
        group.MapGet("/throw/{id:int}", (int id, BookStore store) =>
            store.FindById(id) ?? throw new KeyNotFoundException($"Book {id} not found."));
    }
}
