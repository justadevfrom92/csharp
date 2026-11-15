using CookieAuthAndRoles.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests;

// The role checks and sign-in options in CookieAuthAndRoles/Variations/AuthorizationVariation.cs.
public class VariationsTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public VariationsTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private async Task<HttpClient> LoggedInAs(string userName, string password)
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);
        await AccountTestHelper.PostLoginAsync(client, userName, password);
        return client;
    }

    [Fact]
    public async Task Roles_PositivePositive_CommaMeansOr_BothUsersAllowed()
    {
        HttpClient admin = await LoggedInAs("admin", "Admin123!");
        HttpClient alice = await LoggedInAs("alice", "Alice123!");

        Assert.Equal(200, (int)(await admin.GetAsync("/variations/auth/admin-or-user")).StatusCode);
        Assert.Equal(200, (int)(await alice.GetAsync("/variations/auth/admin-or-user")).StatusCode);
        Assert.Equal(200, (int)(await admin.GetAsync("/variations/auth/manual-check")).StatusCode);
    }

    [Fact]
    public async Task Roles_PositiveNegative_TwoAttributesMeanAnd_AndManualChecksBehaveDifferently()
    {
        HttpClient admin = await LoggedInAs("admin", "Admin123!");
        HttpClient alice = await LoggedInAs("alice", "Alice123!");

        HttpResponseMessage adminAnd = await admin.GetAsync("/variations/auth/admin-and-user"); // needs BOTH roles
        HttpResponseMessage aliceManual = await alice.GetAsync("/variations/auth/manual-check");
        HttpResponseMessage alicePlain = await alice.GetAsync("/variations/auth/plain-status");

        Assert.Equal(302, (int)adminAnd.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", AccountTestHelper.RedirectTarget(adminAnd));
        Assert.Equal(302, (int)aliceManual.StatusCode);                                    // Forbid() -> same redirect as [Authorize]
        Assert.StartsWith("/Account/AccessDenied", AccountTestHelper.RedirectTarget(aliceManual));
        Assert.Equal(403, (int)alicePlain.StatusCode);                                     // StatusCode(403) -> no redirect
    }

    [Fact]
    public async Task SignIn_PositivePositive_RememberMe_CookieGetsAnExpiryDate_OtherwiseNot()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        string remembered = string.Join(";", (await client.GetAsync("/variations/auth/sign-in-demo?remember=true")).Headers.GetValues("Set-Cookie"));
        string session = string.Join(";", (await client.GetAsync("/variations/auth/sign-in-demo?remember=false")).Headers.GetValues("Set-Cookie"));

        Assert.Contains("expires=", remembered);    // survives closing the browser
        Assert.DoesNotContain("expires=", session); // a session cookie: gone when the browser closes
    }

    [Fact]
    public async Task Roles_NegativeNegative_NotLoggedIn_EveryVariationSendsToLogin()
    {
        HttpClient anonymous = AccountTestHelper.CreateClient(factory);

        foreach (string path in new[] { "/variations/auth/admin-or-user", "/variations/auth/manual-check", "/variations/auth/plain-status" })
        {
            HttpResponseMessage response = await anonymous.GetAsync(path);
            Assert.Equal(302, (int)response.StatusCode);
            Assert.StartsWith("/Account/Login", AccountTestHelper.RedirectTarget(response)); // the global filter runs first
        }
    }
}
