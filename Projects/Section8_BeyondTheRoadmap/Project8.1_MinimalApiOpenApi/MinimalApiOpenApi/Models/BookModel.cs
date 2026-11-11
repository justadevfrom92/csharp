namespace MinimalApiOpenApi.Models;

// A book as stored and as returned by the API.
public record BookModel(int Id, string Title, string Author, int Year);
