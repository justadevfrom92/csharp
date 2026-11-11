using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiOpenApi.Filters;
using MinimalApiOpenApi.Models;
using MinimalApiOpenApi.Stores;

namespace MinimalApiOpenApi.Endpoints;

// All /books endpoints in one place. Program.cs just calls app.MapBookEndpoints().
public static class BookEndpoints
{
    public static RouteGroupBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        // MapGroup: every endpoint below starts with /books and shares the "Books" tag in the OpenAPI document.
        RouteGroupBuilder books = app.MapGroup("/books").WithTags("Books");

        books.MapGet("/", GetAllBooks)
            .WithSummary("List all books");

        books.MapGet("/{id:int}", GetBookById)
            .WithSummary("Get one book by its id");

        books.MapPost("/", CreateBook)
            .WithSummary("Add a book (needs the X-Api-Key header)")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        books.MapDelete("/{id:int}", DeleteBook)
            .WithSummary("Delete a book (needs the X-Api-Key header)")
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        return books;
    }

    // The return type lists EVERY possible answer. That's how OpenAPI knows this endpoint can return 200 or 404,
    // and the compiler stops us returning anything else.
    private static Ok<List<BookModel>> GetAllBooks(BookStore store)
    {
        return TypedResults.Ok(store.GetAll());
    }

    private static Results<Ok<BookModel>, NotFound> GetBookById(int id, BookStore store)
    {
        BookModel? book = store.FindById(id);
        if (book == null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(book);
    }

    // "request" is read from the JSON body and already VALIDATED (Required, StringLength, Range) before we get here.
    private static Results<Created<BookModel>, Conflict<string>> CreateBook(SaveBookRequestModel request, BookStore store)
    {
        BookModel? book = store.Add(request);
        if (book == null)
        {
            return TypedResults.Conflict($"A book called '{request.Title}' already exists.");
        }
        return TypedResults.Created($"/books/{book.Id}", book);
    }

    private static Results<NoContent, NotFound> DeleteBook(int id, BookStore store)
    {
        if (!store.Delete(id))
        {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
