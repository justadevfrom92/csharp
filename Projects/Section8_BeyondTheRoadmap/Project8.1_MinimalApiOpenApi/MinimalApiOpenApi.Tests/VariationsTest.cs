using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MinimalApiOpenApi.Tests;

// The five ways to write "get one book" (MinimalApiOpenApi/Variations/BookEndpointVariation.cs) vs the original /books/{id}.
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/books/1")]
    [InlineData("/variations/books/lambda/1")]
    [InlineData("/variations/books/iresult/1")]
    [InlineData("/variations/books/plain/1")]
    [InlineData("/variations/books/throw/1")]
    public async Task GetBook_PositivePositive_ExistingBook_EveryVersionReturns200WithTheSameBook(string path)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        string original = await client.GetStringAsync("/books/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(original, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/books/999", 404)]
    [InlineData("/variations/books/lambda/999", 404)]
    [InlineData("/variations/books/iresult/999", 404)]
    [InlineData("/variations/books/plain/999", 200)]   // "null" with 200: the client can't tell it's missing
    [InlineData("/variations/books/throw/999", 500)]   // an exception for a normal answer
    public async Task GetBook_PositiveNegative_MissingBook_VersionsAnswerDifferently(string path, int expectedStatus)
    {
        Assert.Equal(expectedStatus, (int)(await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task OpenApi_PositiveNegative_TypedResultsDocument404_IResultDoesNot()
    {
        using JsonDocument document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        JsonElement paths = document.RootElement.GetProperty("paths");

        List<string> Codes(string path)
        {
            return paths.GetProperty(path).GetProperty("get").GetProperty("responses").EnumerateObject().Select(code => code.Name).ToList();
        }

        Assert.Equal(["200", "404"], Codes("/variations/books/lambda/{id}"));   // from Results<Ok<BookModel>, NotFound>
        Assert.Equal(["200"], Codes("/variations/books/iresult/{id}"));         // IResult: OpenAPI can't see the 404
    }

    [Fact]
    public async Task AsParameters_PositivePositive_RouteQueryAndServiceInOneObject_AndNegativeNegative_BadId()
    {
        string normal = await client.GetStringAsync("/variations/books/as-parameters/1");
        string upper = await client.GetStringAsync("/variations/books/as-parameters/1?uppercase=true");

        Assert.Equal(upper.Trim('"'), normal.Trim('"').ToUpperInvariant());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/variations/books/as-parameters/abc")).StatusCode); // {id:int} doesn't match
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/variations/books/as-parameters/1?uppercase=maybe")).StatusCode);
    }
}
