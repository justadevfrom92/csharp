using Microsoft.AspNetCore.Http.HttpResults;
using MinimalApiOpenApi.Models;

namespace MinimalApiOpenApi.Variations;

// Returning/accepting the ENTITY vs a DTO. Mapped under /variations/dto-books/... (see Program.cs).
public static class BookDtoEndpointVariation
{
    public static void MapBookDtoVariations(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/variations/dto-books").WithTags("Variations");

        // ENTITY out: every field is serialized - CostPrice and AddedBy LEAK to the client.
        group.MapGet("/entity/{id:int}", Results<Ok<BookEntityVariation>, NotFound> (int id, BookEntityStoreVariation store) =>
            store.FindById(id) is BookEntityVariation book ? TypedResults.Ok(book) : TypedResults.NotFound());

        // DTO out: mapped first, so only the public fields are sent.
        group.MapGet("/dto/{id:int}", Results<Ok<BookDtoVariation>, NotFound> (int id, BookEntityStoreVariation store) =>
            store.FindById(id) is BookEntityVariation book ? TypedResults.Ok(book.ToDto()) : TypedResults.NotFound());

        // ENTITY in ("over-posting"): the client can set CostPrice and AddedBy itself - and no validation runs on them.
        group.MapPost("/entity", (BookEntityVariation book, BookEntityStoreVariation store) =>
            TypedResults.Ok(store.Add(book)));

        // Request DTO in: extra JSON fields are ignored, [Required]/[Range] are checked, the server fills the rest.
        group.MapPost("/dto", (SaveBookRequestModel request, BookEntityStoreVariation store) =>
            TypedResults.Ok(store.Add(request.ToEntity(0, costPrice: 0m, addedBy: "api")).ToDto()));
    }
}
