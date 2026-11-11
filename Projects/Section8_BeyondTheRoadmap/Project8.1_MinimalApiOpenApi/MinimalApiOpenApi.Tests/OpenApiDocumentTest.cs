using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MinimalApiOpenApi.Tests;

// PositivePositive - the document exists and describes the endpoints and their possible answers
// PositiveNegative - things that must NOT be in it (endpoints that don't exist, answers an endpoint can't give)
// NegativeNegative - a document version that doesn't exist -> 404
public class OpenApiDocumentTest
{
    private static async Task<JsonDocument> GetDocumentAsync()
    {
        HttpClient client = new WebApplicationFactory<Program>().CreateClient();
        return JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
    }

    [Fact]
    public async Task OpenApi_PositivePositive_DescribesBookEndpointsAndTheirResponses()
    {
        using JsonDocument document = await GetDocumentAsync();
        JsonElement paths = document.RootElement.GetProperty("paths");

        JsonElement getById = paths.GetProperty("/books/{id}").GetProperty("get");
        Assert.Equal("Get one book by its id", getById.GetProperty("summary").GetString());
        Assert.True(getById.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(getById.GetProperty("responses").TryGetProperty("404", out _));
        Assert.True(paths.GetProperty("/books").TryGetProperty("post", out _));
    }

    [Fact]
    public async Task OpenApi_PositiveNegative_DoesNotListThingsThatDoNotExist()
    {
        using JsonDocument document = await GetDocumentAsync();
        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.False(paths.TryGetProperty("/authors", out _));                                         // no such endpoint
        Assert.False(paths.GetProperty("/books").TryGetProperty("put", out _));                        // no PUT /books
        Assert.False(paths.GetProperty("/books/{id}").GetProperty("get").GetProperty("responses").TryGetProperty("409", out _));
    }

    [Fact]
    public async Task OpenApi_NegativeNegative_UnknownDocumentVersion_Returns404()
    {
        HttpClient client = new WebApplicationFactory<Program>().CreateClient();

        Assert.Equal(404, (int)(await client.GetAsync("/openapi/v2.json")).StatusCode);
    }
}
