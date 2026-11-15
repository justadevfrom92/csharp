using IdentityJwtPolicies.Variations;
using System.Text;
using IdentityJwtPolicies.Constants;
using IdentityJwtPolicies.DbContexts;
using IdentityJwtPolicies.Endpoints;
using IdentityJwtPolicies.Handlers;
using IdentityJwtPolicies.Requirements;
using IdentityJwtPolicies.Seeders;
using IdentityJwtPolicies.Services;
using IdentityJwtPolicies.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database for Identity's tables ----------
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Identity")));

// ---------- ASP.NET Core Identity: users, roles, password hashing, lockout ----------
builder.Services.AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 3;                     // 3 wrong passwords...
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // ...locks the account for 5 minutes
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

// ---------- JWT bearer authentication ----------
JwtSettings jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Every incoming token is checked against ALL of these. Any failure -> 401.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30), // tolerate small clock differences between servers
        };
        options.MapInboundClaims = false; // keep claim names exactly as we wrote them
        options.TokenValidationParameters.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
        options.TokenValidationParameters.NameClaimType = System.Security.Claims.ClaimTypes.Name;
    });

// ---------- Authorization POLICIES ----------
builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNameConstants.AdminOnly, policy => policy.RequireRole("Admin"))
    .AddPolicy(PolicyNameConstants.FinanceDepartment, policy => policy.RequireClaim(AppClaimTypeConstants.Department, "finance"))
    .AddPolicy(PolicyNameConstants.AtLeast18, policy => policy.AddRequirements(new MinimumAgeRequirement(18)));

builder.Services.AddValidation();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Create the database and the starting roles/admin user.
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.EnsureCreated();
    await IdentityDataSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest ? badRequest.StatusCode : 500,
});
app.UseStatusCodePages();
app.UseAuthentication(); // reads "Authorization: Bearer <token>" and checks it
app.UseAuthorization();  // applies RequireAuthorization(...) policies

app.MapAccountEndpoints();
app.MapProtectedEndpoints();
app.MapPolicyVariations();   // /variations/policies/... - other ways to write rules (Variations/)

app.Run();

// Lets the test project start this app in memory with WebApplicationFactory<Program>.
public partial class Program
{
}
