namespace MinimalApiOpenApi.Variations;

// A DTO (data transfer object): only what the CLIENT gets - no CostPrice, no AddedBy.
// A record, because it's built once, sent, and never changed.
// The original BookModel plays BOTH roles (stored and returned); fine while every field is public anyway.
public record BookDtoVariation(int Id, string Title, string Author, int Year);
