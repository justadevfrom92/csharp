using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RazorPagesContacts.Tests;

// Four ways for a Razor Page to receive a search term (RazorPagesContacts/Pages/Variations/).
// Seed data: "Ada Lovelace" and "Alan Turing" - searching "ada" should find 1 of the 2.
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    private async Task<(string Term, int Count)> SearchAsync(string url)
    {
        string html = await client.GetStringAsync(url);
        string term = Regex.Match(html, "id=\"term\">term: \"(.*?)\"").Groups[1].Value;
        int count = int.Parse(Regex.Match(html, "id=\"count\">(\\d+) result").Groups[1].Value);
        return (term, count);
    }

    [Theory]
    [InlineData("/Variations/SearchWithSupportsGet?term=ada")]
    [InlineData("/Variations/SearchWithRouteValue/ada")]
    public async Task Search_PositivePositive_SupportsGetAndRouteValue_FilterAndShowTheTerm(string url)
    {
        (string term, int count) = await SearchAsync(url);

        Assert.Equal("ada", term);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Search_PositiveNegative_OtherStyles_DropTheTermSomewhere()
    {
        (string termNoSupportsGet, int countNoSupportsGet) = await SearchAsync("/Variations/SearchWithoutSupportsGet?term=ada");
        (string termParameter, int countParameter) = await SearchAsync("/Variations/SearchWithHandlerParameter?term=ada");

        Assert.Equal("", termNoSupportsGet);  // [BindProperty] alone ignores GET values...
        Assert.Equal(2, countNoSupportsGet);  // ...so nothing was filtered
        Assert.Equal("", termParameter);      // the handler parameter filtered...
        Assert.Equal(1, countParameter);      // ...but the page can't show what was searched
    }

    [Fact]
    public async Task Search_NegativeNegative_NoTermOrNoMatch_ListEverythingOrNothing()
    {
        Assert.Equal(2, (await SearchAsync("/Variations/SearchWithSupportsGet")).Count);
        Assert.Equal(0, (await SearchAsync("/Variations/SearchWithSupportsGet?term=zzz")).Count);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/Variations/SearchWithRouteValue/a/b")).StatusCode); // one segment only
    }
}
