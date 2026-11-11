using IdentityJwtPolicies.Seeders;
using IdentityJwtPolicies.Tests.Helpers;

namespace IdentityJwtPolicies.Tests;

// For each policy:
// PositivePositive - a user who meets it -> 200
// PositiveNegative - logged in, but doesn't meet it -> 403 Forbidden
// NegativeNegative - no token / no way to check -> 401 (or 403 when the claim is missing)
public class PolicyTest
{
    // ---------- AdminOnly (role) ----------

    [Fact]
    public async Task AdminOnly_PositivePositive_AdminRole_Returns200()
    {
        using var app = new IdentityAppHelper();
        string token = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);

        Assert.Equal(200, await app.GetStatusWithTokenAsync("/admin/dashboard", token));
    }

    [Fact]
    public async Task AdminOnly_PositiveNegative_NormalUser_Returns403()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(403, await app.GetStatusWithTokenAsync("/admin/dashboard", await app.RegisterAndGetTokenAsync()));
    }

    [Fact]
    public async Task AdminOnly_NegativeNegative_NoToken_Returns401()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/admin/dashboard", null));
    }

    // ---------- FinanceDepartment (claim) ----------

    [Fact]
    public async Task FinanceDepartment_PositivePositive_FinanceClaim_Returns200()
    {
        using var app = new IdentityAppHelper();

        // "Finance" is stored lower-case, so the claim matches the policy's "finance".
        Assert.Equal(200, await app.GetStatusWithTokenAsync("/reports/finance", await app.RegisterAndGetTokenAsync(department: "Finance")));
    }

    [Fact]
    public async Task FinanceDepartment_PositiveNegative_OtherDepartment_Returns403()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(403, await app.GetStatusWithTokenAsync("/reports/finance", await app.RegisterAndGetTokenAsync(department: "sales")));
    }

    [Fact]
    public async Task FinanceDepartment_NegativeNegative_NoToken_Returns401()
    {
        using var app = new IdentityAppHelper();

        Assert.Equal(401, await app.GetStatusWithTokenAsync("/reports/finance", null));
    }

    // ---------- AtLeast18 (custom requirement + handler) ----------

    [Fact]
    public async Task AtLeast18_PositivePositive_Exactly18_Returns200()
    {
        using var app = new IdentityAppHelper();
        int birthYear = DateTime.UtcNow.Year - 18;

        Assert.Equal(200, await app.GetStatusWithTokenAsync("/content/adults-only", await app.RegisterAndGetTokenAsync(birthYear: birthYear)));
    }

    [Fact]
    public async Task AtLeast18_PositiveNegative_Age17_Returns403()
    {
        using var app = new IdentityAppHelper();
        int birthYear = DateTime.UtcNow.Year - 17;

        Assert.Equal(403, await app.GetStatusWithTokenAsync("/content/adults-only", await app.RegisterAndGetTokenAsync(birthYear: birthYear)));
    }

    [Fact]
    public async Task AtLeast18_NegativeNegative_NoBirthYearClaim_Returns403()
    {
        using var app = new IdentityAppHelper();
        // The seeded admin has no birth_year claim: the handler can't check the age, so it must say no.
        string token = await app.GetTokenAsync(IdentityDataSeeder.AdminEmail, IdentityDataSeeder.AdminPassword);

        Assert.Equal(403, await app.GetStatusWithTokenAsync("/content/adults-only", token));
    }
}
