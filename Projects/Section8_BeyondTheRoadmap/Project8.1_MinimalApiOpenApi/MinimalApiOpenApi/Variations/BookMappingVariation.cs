using MinimalApiOpenApi.Models;

namespace MinimalApiOpenApi.Variations;

// MAPPING by hand: entity -> DTO for answers, request DTO -> entity for saves.
// A few plain lines; a mapping library (AutoMapper, Mapster) does the same by matching names, which hides
// mistakes like a renamed field silently becoming empty.
public static class BookMappingVariation
{
    public static BookDtoVariation ToDto(this BookEntityVariation entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new BookDtoVariation(entity.Id, entity.Title, entity.Author, entity.Year);
    }

    // The SERVER decides the secret fields; the client only sends what SaveBookRequestModel allows.
    public static BookEntityVariation ToEntity(this SaveBookRequestModel request, int id, decimal costPrice, string addedBy)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new BookEntityVariation
        {
            Id = id,
            Title = request.Title,
            Author = request.Author,
            Year = request.Year,
            CostPrice = costPrice,
            AddedBy = addedBy,
        };
    }
}
