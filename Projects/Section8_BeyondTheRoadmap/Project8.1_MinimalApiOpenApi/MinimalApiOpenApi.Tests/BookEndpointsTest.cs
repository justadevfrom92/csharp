using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MinimalApiOpenApi.Filters;
using MinimalApiOpenApi.Models;

namespace MinimalApiOpenApi.Tests;

// PositivePositive - valid request -> succeeds
// PositiveNegative - well-formed request a rule refuses (unknown id, duplicate, out-of-range year) -> 404/409/400
// NegativeNegative - malformed or missing (bad JSON, "abc" id, no API key) -> rejected
public class BookEndpointsTest
{
    // A NEW app per test: the book list starts fresh, so tests can't affect each other.
    private static HttpClient CreateClient(bool withApiKey)
    {
        HttpClient client = new WebApplicationFactory<Program>().CreateClient();
        if (withApiKey)
        {
            client.DefaultRequestHeaders.Add(ApiKeyEndpointFilter.HeaderName, "demo-key-change-me");
        }
        return client;
    }

    // ---------- GET /books/{id} ----------

    [Fact]
    public async Task GetBook_PositivePositive_ExistingId_ReturnsBook()
    {
        HttpClient client = CreateClient(withApiKey: false);

        BookModel? book = await client.GetFromJsonAsync<BookModel>("/books/1");

        Assert.Equal(new BookModel(1, "Frankenstein", "Mary Shelley", 1818), book);
    }

    [Fact]
    public async Task GetBook_PositiveNegative_WellFormedIdThatDoesNotExist_Returns404ProblemDetails()
    {
        HttpClient client = CreateClient(withApiKey: false);

        HttpResponseMessage response = await client.GetAsync("/books/9999");

        Assert.Equal(404, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetBook_NegativeNegative_NonNumericId_Returns404()
    {
        HttpClient client = CreateClient(withApiKey: false);

        HttpResponseMessage response = await client.GetAsync("/books/abc"); // {id:int} doesn't match "abc"

        Assert.Equal(404, (int)response.StatusCode);
    }

    // ---------- POST /books ----------

    [Fact]
    public async Task CreateBook_PositivePositive_ValidBook_Returns201WithLocation()
    {
        HttpClient client = CreateClient(withApiKey: true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new SaveBookRequestModel("Emma", "Jane Austen", 1815));
        BookModel? created = await response.Content.ReadFromJsonAsync<BookModel>();

        Assert.Equal(201, (int)response.StatusCode);
        Assert.Equal($"/books/{created!.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Emma", (await client.GetFromJsonAsync<BookModel>($"/books/{created.Id}"))!.Title);
    }

    [Fact]
    public async Task CreateBook_PositiveNegative_RuleBreakingValues_Returns400WithFieldErrors()
    {
        HttpClient client = CreateClient(withApiKey: true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new SaveBookRequestModel("", "X", 3000));
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(400, (int)response.StatusCode);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("Year", out _));
    }

    [Fact]
    public async Task CreateBook_PositiveNegative_DuplicateTitle_Returns409()
    {
        HttpClient client = CreateClient(withApiKey: true);

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new SaveBookRequestModel("dracula", "Someone", 1900));

        Assert.Equal(409, (int)response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_NegativeNegative_MalformedJson_Returns400()
    {
        HttpClient client = CreateClient(withApiKey: true);

        HttpResponseMessage response = await client.PostAsync("/books", new StringContent("{ this is not json", Encoding.UTF8, "application/json"));

        Assert.Equal(400, (int)response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_NegativeNegative_MissingApiKey_Returns401AndAddsNothing()
    {
        HttpClient client = CreateClient(withApiKey: false);

        HttpResponseMessage response = await client.PostAsJsonAsync("/books", new SaveBookRequestModel("Emma", "Jane Austen", 1815));

        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal(2, (await client.GetFromJsonAsync<List<BookModel>>("/books"))!.Count);
    }

    // ---------- DELETE /books/{id} ----------

    [Fact]
    public async Task DeleteBook_PositivePositive_ExistingId_Returns204AndBookIsGone()
    {
        HttpClient client = CreateClient(withApiKey: true);

        HttpResponseMessage response = await client.DeleteAsync("/books/2");

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Equal(404, (int)(await client.GetAsync("/books/2")).StatusCode);
    }

    [Fact]
    public async Task DeleteBook_PositiveNegative_UnknownId_Returns404()
    {
        HttpClient client = CreateClient(withApiKey: true);

        Assert.Equal(404, (int)(await client.DeleteAsync("/books/9999")).StatusCode);
    }

    [Fact]
    public async Task DeleteBook_NegativeNegative_WrongApiKey_Returns401AndKeepsBook()
    {
        HttpClient client = CreateClient(withApiKey: false);
        client.DefaultRequestHeaders.Add(ApiKeyEndpointFilter.HeaderName, "wrong-key");

        Assert.Equal(401, (int)(await client.DeleteAsync("/books/1")).StatusCode);
        Assert.Equal(200, (int)(await client.GetAsync("/books/1")).StatusCode);
    }
}
