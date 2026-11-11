using CookieAuthAndRoles.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - valid logout (POST + token) -> logged out
//   PositiveNegative - a GET to the logout URL (e.g. an <img> on another site) -> must NOT log out
//   NegativeNegative - POST without the anti-forgery token -> 400, still logged in
public class LogoutTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LogoutTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private async Task<HttpClient> LoggedInClientAsync()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        await AccountTestHelper.PostLoginAsync(client, "alice", "Alice123!");
        return client;
    }

    private static async Task<bool> IsLoggedInAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/Account/Profile");
        return (int)response.StatusCode == 200;
    }

    [Fact]
    public async Task Logout_PositivePositive_PostWithToken_LogsOut()
    {
        HttpClient client = await LoggedInClientAsync();
        // The token must come from a page seen WHILE logged in: tokens are tied to the user.
        string token = await AccountTestHelper.GetAntiForgeryTokenAsync(client, "/");

        HttpResponseMessage response = await client.PostAsync("/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/", AccountTestHelper.RedirectTarget(response));
        Assert.False(await IsLoggedInAsync(client));
    }

    [Fact]
    public async Task Logout_PositiveNegative_GetRequest_DoesNotLogOut()
    {
        HttpClient client = await LoggedInClientAsync();

        HttpResponseMessage response = await client.GetAsync("/Account/Logout");

        Assert.Equal(405, (int)response.StatusCode); // Method Not Allowed: logout only accepts POST
        Assert.True(await IsLoggedInAsync(client));
    }

    [Fact]
    public async Task Logout_NegativeNegative_PostWithoutToken_Returns400AndStaysLoggedIn()
    {
        HttpClient client = await LoggedInClientAsync();

        HttpResponseMessage response = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(400, (int)response.StatusCode);
        Assert.True(await IsLoggedInAsync(client));
    }
}
