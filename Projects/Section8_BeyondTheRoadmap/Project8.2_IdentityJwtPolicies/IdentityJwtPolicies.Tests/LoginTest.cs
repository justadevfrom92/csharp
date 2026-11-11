using IdentityJwtPolicies.Seeders;
using IdentityJwtPolicies.Tests.Helpers;

namespace IdentityJwtPolicies.Tests;

// PositivePositive - right password -> a JWT that opens /me
// PositiveNegative - wrong password -> 401; 3 wrong passwords -> locked (423) even with the right one
// NegativeNegative - unknown email or empty fields -> refused
public class LoginTest
{
    [Fact]
    public async Task Login_PositivePositive_RightPassword_ReturnsJwtThatOpensMe()
    {
        using var app = new IdentityAppHelper();

        string token = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);

        Assert.Equal(3, token.Split('.').Length); // header.payload.signature
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/me", token));
    }

    [Fact]
    public async Task Login_PositiveNegative_WrongPassword_Returns401()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(401, (int)(await app.LoginAsync(IdentityDataSeeder.AdminEmail, "Wrong123!x")).StatusCode);
    }

    [Fact]
    public async Task Login_PositiveNegative_ThreeWrongPasswords_LocksAccountEvenForRightPassword()
    {
        using var app = new IdentityAppHelper();
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            await app.LoginAsync(IdentityDataSeeder.AdminEmail, "Wrong123!x");
        }

        HttpResponseMessage response = await app.LoginAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);

        Assert.Equal(423, (int)response.StatusCode); // Locked
    }

    [Fact]
    public async Task Login_NegativeNegative_UnknownEmail_Returns401WithSameMessageAsWrongPassword()
    {
        using var app = new IdentityAppHelper();

        HttpResponseMessage unknown = await app.LoginAsync("nobody@example.com", "Whatever1!");
        HttpResponseMessage wrong = await app.LoginAsync(IdentityDataSeeder.AdminEmail, "Wrong123!x");

        Assert.Equal(401, (int)unknown.StatusCode);
        Assert.Contains("Wrong email or password.", await unknown.Content.ReadAsStringAsync());
        Assert.Contains("Wrong email or password.", await wrong.Content.ReadAsStringAsync()); // can't tell which emails exist
    }

    [Fact]
    public async Task Login_NegativeNegative_EmptyFields_Returns400()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(400, (int)(await app.LoginAsync("", "")).StatusCode);
    }
}
