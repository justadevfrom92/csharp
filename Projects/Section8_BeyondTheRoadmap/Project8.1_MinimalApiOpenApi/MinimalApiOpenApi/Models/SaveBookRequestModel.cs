using System.ComponentModel.DataAnnotations;

namespace MinimalApiOpenApi.Models;

// What a client sends to create or change a book. The attributes are checked automatically
// (builder.Services.AddValidation() in Program.cs) before the endpoint code runs.
public record SaveBookRequestModel(
    [property: Required, StringLength(200, MinimumLength = 1)] string Title,
    [property: Required, StringLength(100, MinimumLength = 1)] string Author,
    [property: Range(1450, 2100)] int Year);
