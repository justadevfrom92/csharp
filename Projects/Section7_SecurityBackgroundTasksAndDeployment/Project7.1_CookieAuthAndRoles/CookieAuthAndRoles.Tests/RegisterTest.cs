using CookieAuthAndRoles.Tests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests;

// Every behaviour has three kinds of test:
//   PositivePositive - VALID new account   -> created (always as a normal User)
//   PositiveNegative - well-formed but the name is taken -> refused
//   NegativeNegative - missing / too-short input         -> rejected
public class RegisterTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public RegisterTest(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    private static async Task<HttpResponseMessage> PostRegisterAsync(HttpClient client, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await AccountTestHelper.GetAntiForgeryTokenAsync(client, "/Account/Register");
        return await client.PostAsync("/Account/Register", new FormUrlEncodedContent(fields));
    }

    [Fact]
    public async Task Register_PositivePositive_NewUser_IsCreatedAsNormalUserEvenIfFormSaysAdmin()
    {
        // Unique name: the user store lives for the whole test run.
        string userName = "user" + Guid.NewGuid().ToString("N").Substring(0, 8);
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await PostRegisterAsync(client, new Dictionary<string, string>
        {
            ["userName"] = userName,
            ["password"] = "Secret123!",
            ["role"] = "Admin", // an attacker adding a field the form never had
        });

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/Account/Login", AccountTestHelper.RedirectTarget(response));

        HttpResponseMessage login = await AccountTestHelper.PostLoginAsync(client, userName, "Secret123!");
        Assert.True(AccountTestHelper.SetsAuthCookie(login));
        HttpResponseMessage adminPage = await client.GetAsync("/Admin");
        Assert.StartsWith("/Account/AccessDenied", AccountTestHelper.RedirectTarget(adminPage)); // NOT an admin
    }

    [Fact]
    public async Task Register_PositiveNegative_TakenUserName_IsRefused()
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await PostRegisterAsync(client, new Dictionary<string, string>
        {
            ["userName"] = "alice",
            ["password"] = "Another123!",
        });

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("That user name is taken.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("", "Secret123!")]   // missing user name
    [InlineData("bob", "")]          // missing password
    [InlineData("bob", "short")]     // password under 8 characters
    public async Task Register_NegativeNegative_MissingOrTooShortInput_IsRejected(string userName, string password)
    {
        HttpClient client = AccountTestHelper.CreateClient(factory);

        HttpResponseMessage response = await PostRegisterAsync(client, new Dictionary<string, string>
        {
            ["userName"] = userName,
            ["password"] = password,
        });

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("password must be at least 8 characters", await response.Content.ReadAsStringAsync());
    }
}
