using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MinimalApiOpenApi.Models;
using MinimalApiOpenApi.Variations;

namespace MinimalApiOpenApi.Tests;

// Entity vs DTO (MinimalApiOpenApi/Variations/BookDtoEndpointVariation.cs): what leaks out, what gets in.
public class DtoVariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public DtoVariationsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    private static List<string> FieldNames(string json)
    {
        return JsonDocument.Parse(json).RootElement.EnumerateObject().Select(field => field.Name).ToList();
    }

    [Fact]
    public async Task Get_PositivePositive_DtoSendsOnlyThePublicFields()
    {
        string json = await client.GetStringAsync("/variations/dto-books/dto/1");

        Assert.Equal(["id", "title", "author", "year"], FieldNames(json));
    }

    [Fact]
    public async Task Get_PositiveNegative_EntityLeaksCostPriceAndAddedBy()
    {
        string json = await client.GetStringAsync("/variations/dto-books/entity/1");

        Assert.Contains("costPrice", FieldNames(json));
        Assert.Contains("addedBy", FieldNames(json));
    }

    [Fact]
    public async Task Post_PositiveNegative_EntityLetsTheClientSetSecretFields_DtoIgnoresThem()
    {
        var sneaky = new { title = "Emma", author = "Jane Austen", year = 1815, costPrice = -100m, addedBy = "hacker" };

        BookEntityVariation? viaEntity = await (await client.PostAsJsonAsync("/variations/dto-books/entity", sneaky)).Content.ReadFromJsonAsync<BookEntityVariation>();
        HttpResponseMessage viaDto = await client.PostAsJsonAsync("/variations/dto-books/dto", sneaky);
        string dtoJson = await viaDto.Content.ReadAsStringAsync();

        Assert.Equal(-100m, viaEntity!.CostPrice); // over-posting worked
        Assert.Equal("hacker", viaEntity.AddedBy);
        Assert.Equal(HttpStatusCode.OK, viaDto.StatusCode);
        Assert.Equal(["id", "title", "author", "year"], FieldNames(dtoJson)); // extra fields simply ignored
    }

    [Fact]
    public async Task Post_NegativeNegative_InvalidBook_DtoRejects_EntityAcceptsAnything()
    {
        var invalid = new { title = "", author = "", year = 3000 };

        HttpResponseMessage viaDto = await client.PostAsJsonAsync("/variations/dto-books/dto", invalid);
        HttpResponseMessage viaEntity = await client.PostAsJsonAsync("/variations/dto-books/entity", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, viaDto.StatusCode);   // [Required]/[Range] on SaveBookRequestModel
        Assert.Equal(HttpStatusCode.OK, viaEntity.StatusCode);        // no rules on the entity: empty title saved
    }

    [Fact]
    public async Task Get_NegativeNegative_MissingBook_BothReturn404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/variations/dto-books/entity/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/variations/dto-books/dto/999")).StatusCode);
    }

    [Fact]
    public void Mapping_PositivePositive_ServerFillsTheSecretFields_DtoDropsThem()
    {
        BookEntityVariation entity = new SaveBookRequestModel("Emma", "Jane Austen", 1815).ToEntity(7, 3.50m, "api");

        Assert.Equal(3.50m, entity.CostPrice);
        Assert.Equal("api", entity.AddedBy);
        Assert.Equal(new BookDtoVariation(7, "Emma", "Jane Austen", 1815), entity.ToDto());
    }

    [Fact]
    public async Task Post_PositivePositive_ValidBookViaDto_GetsANewIdAndOnlyPublicFields()
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/variations/dto-books/dto", new SaveBookRequestModel("Emma", "Jane Austen", 1815));
        BookDtoVariation? saved = await response.Content.ReadFromJsonAsync<BookDtoVariation>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(saved!.Id > 1);
        Assert.Equal("Emma", saved.Title);
    }

    [Fact]
    public void Mapping_PositiveNegative_EmptyEntity_MapsToEmptyDto_SecretsStillDropped()
    {
        Assert.Equal(new BookDtoVariation(0, "", "", 0), new BookEntityVariation { CostPrice = 9m }.ToDto());
    }

    [Fact]
    public void Mapping_NegativeNegative_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((BookEntityVariation)null!).ToDto());
        Assert.Throws<ArgumentNullException>(() => ((SaveBookRequestModel)null!).ToEntity(1, 0m, "api"));
    }
}
