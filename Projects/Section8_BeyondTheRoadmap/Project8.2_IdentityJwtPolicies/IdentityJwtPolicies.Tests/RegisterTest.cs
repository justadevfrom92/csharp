using IdentityJwtPolicies.Tests.Helpers;

namespace IdentityJwtPolicies.Tests;

// PositivePositive - valid new user -> created, can log in
// PositiveNegative - well-formed but breaks a rule (weak password, email taken) -> 400 with the reason
// NegativeNegative - malformed (not an email, missing fields) -> 400
public class RegisterTest
{
    [Fact]
    public async Task Register_PositivePositive_ValidUser_IsCreatedAndCanLogIn()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(201, (int)(await app.RegisterAsync("new@example.com", "Passw0rd!")).StatusCode);
        Assert.Equal(200, (int)(await app.LoginAsync("new@example.com", "Passw0rd!")).StatusCode);
    }

    [Theory]
    [InlineData("short1!", "PasswordTooShort")]
    [InlineData("password1!", "PasswordRequiresUpper")]
    [InlineData("Password!!", "PasswordRequiresDigit")]
    [InlineData("Password11", "PasswordRequiresNonAlphanumeric")]
    public async Task Register_PositiveNegative_WeakPassword_Returns400WithReason(string password, string expectedErrorCode)
    {
        using var app = new IdentityAppHelper();

        HttpResponseMessage response = await app.RegisterAsync("weak@example.com", password);

        Assert.Equal(400, (int)response.StatusCode);
        Assert.Contains(expectedErrorCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_PositiveNegative_EmailAlreadyUsed_Returns400()
    {
        using var app = new IdentityAppHelper();
        await app.RegisterAsync("twice@example.com", "Passw0rd!");

        HttpResponseMessage response = await app.RegisterAsync("twice@example.com", "Passw0rd!");

        Assert.Equal(400, (int)response.StatusCode);
        Assert.Contains("DuplicateUserName", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("not-an-email", "Passw0rd!")]
    [InlineData("", "Passw0rd!")]
    [InlineData("ok@example.com", "")]
    public async Task Register_NegativeNegative_MalformedInput_Returns400(string email, string password)
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(400, (int)(await app.RegisterAsync(email, password)).StatusCode);
    }
}
