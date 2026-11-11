using CookieAuthAndRoles.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - allowed user      -> page shown
//   PositiveNegative - logged in, but the role rule says no -> Access denied
//   NegativeNegative - no login at all   -> sent to the login page
public class AuthorizationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AuthorizationTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    // ---------- Admin area: [Authorize(Roles = "Admin")] ----------

    [Fact]
    public async Task AdminPage_PositivePositive_AdminUser_SeesAdminArea()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        await AccountTestHelper.PostLoginAsync(client, "admin", "Admin123!");

        HttpResponseMessage response = await client.GetAsync("/Admin");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Admin area", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AdminPage_PositiveNegative_NormalUser_IsSentToAccessDenied()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        await AccountTestHelper.PostLoginAsync(client, "alice", "Alice123!");

        HttpResponseMessage response = await client.GetAsync("/Admin");

        Assert.Equal(302, (int)response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", AccountTestHelper.RedirectTarget(response));
    }

    [Fact]
    public async Task AdminPage_NegativeNegative_NotLoggedIn_IsSentToLoginWithReturnUrl()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync("/Admin");

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/Account/Login?ReturnUrl=%2FAdmin", AccountTestHelper.RedirectTarget(response));
    }

    // ---------- Login required for every page (global AuthorizeFilter), except [AllowAnonymous] ones ----------

    [Fact]
    public async Task LoginRequired_PositivePositive_LoggedInUser_SeesOwnProfile()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        await AccountTestHelper.PostLoginAsync(client, "alice", "Alice123!");

        string html = await client.GetStringAsync("/Account/Profile");

        Assert.Contains("alice", html);
        Assert.Contains("User", html);
    }

    [Fact]
    public async Task LoginRequired_PositiveNegative_PublicHomePage_IsOpenWithoutLogin()
    {
        // The global "login required" filter must NOT block pages marked [AllowAnonymous].
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(200, (int)response.StatusCode);
    }

    [Fact]
    public async Task LoginRequired_NegativeNegative_ForgedAuthCookie_IsTreatedAsLoggedOut()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Profile");
        request.Headers.Add("Cookie", AccountTestHelper.AuthCookieName + "=this-is-not-a-real-encrypted-cookie");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(302, (int)response.StatusCode);
        Assert.StartsWith("/Account/Login", AccountTestHelper.RedirectTarget(response));
    }
}
