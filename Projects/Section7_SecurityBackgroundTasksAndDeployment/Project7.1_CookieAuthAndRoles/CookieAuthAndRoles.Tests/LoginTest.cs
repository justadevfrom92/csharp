using CookieAuthAndRoles.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID input            -> succeeds
//   PositiveNegative - well-formed input that breaks a rule (wrong password, outside URL) -> refused, nothing changes
//   NegativeNegative - MALFORMED or missing input (no anti-forgery token, garbage URL)    -> rejected cleanly
public class LoginTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LoginTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_PositivePositive_CorrectPassword_SetsAuthCookieAndOpensProfile()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await AccountTestHelper.PostLoginAsync(client, "admin", "Admin123!");

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/", AccountTestHelper.RedirectTarget(response));
        Assert.True(AccountTestHelper.SetsAuthCookie(response));
        Assert.Equal(200, (int)(await client.GetAsync("/Account/Profile")).StatusCode);
    }

    [Fact]
    public async Task Login_PositiveNegative_WrongPassword_ShowsErrorAndStaysLoggedOut()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await AccountTestHelper.PostLoginAsync(client, "admin", "WrongPassword1!");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("Wrong user name or password.", await response.Content.ReadAsStringAsync());
        Assert.False(AccountTestHelper.SetsAuthCookie(response));
        Assert.Equal(302, (int)(await client.GetAsync("/Account/Profile")).StatusCode); // sent to the login page
    }

    [Fact]
    public async Task Login_NegativeNegative_MissingAntiForgeryToken_Returns400()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        var fields = new Dictionary<string, string> { ["userName"] = "admin", ["password"] = "Admin123!" };

        HttpResponseMessage response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields));

        Assert.Equal(400, (int)response.StatusCode);
        Assert.False(AccountTestHelper.SetsAuthCookie(response));
    }

    // ---------- ReturnUrl after login (open-redirect protection) ----------

    [Fact]
    public async Task ReturnUrl_PositivePositive_LocalUrl_RedirectsThereAfterLogin()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await AccountTestHelper.PostLoginAsync(client, "admin", "Admin123!", "/Admin");

        Assert.Equal("/Admin", AccountTestHelper.RedirectTarget(response));
    }

    [Fact]
    public async Task ReturnUrl_PositiveNegative_OutsideWebsite_IsIgnoredAndGoesHome()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        // A real, well-formed URL - but to another site. Following it would be an "open redirect".
        HttpResponseMessage response = await AccountTestHelper.PostLoginAsync(client, "admin", "Admin123!", "https://evil.example.com/phish");

        Assert.Equal("/", AccountTestHelper.RedirectTarget(response));
    }

    [Fact]
    public async Task ReturnUrl_NegativeNegative_GarbageUrl_IsIgnoredAndGoesHome()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await AccountTestHelper.PostLoginAsync(client, "admin", "Admin123!", "javascript:alert(1)");

        Assert.Equal("/", AccountTestHelper.RedirectTarget(response));
    }
}
