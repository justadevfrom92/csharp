using System.Security.Claims;
using IdentityJwtPolicies.Constants;
using IdentityJwtPolicies.Models;
using IdentityJwtPolicies.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace IdentityJwtPolicies.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder account = app.MapGroup("/account").WithTags("Account");
        account.MapPost("/register", RegisterAsync);
        account.MapPost("/login", LoginAsync);
    }

    // POST /account/register - Identity checks the password rules and hashes the password.
    private static async Task<Results<Created, ValidationProblem>> RegisterAsync(RegisterRequestModel request, UserManager<IdentityUser> userManager)
    {
        var user = new IdentityUser { UserName = request.Email, Email = request.Email };
        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            // e.g. "PasswordRequiresDigit", "DuplicateUserName" - returned as a 400 with every reason.
            Dictionary<string, string[]> errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
            return TypedResults.ValidationProblem(errors);
        }

        // New users are ALWAYS normal users; the role is never taken from the request.
        await userManager.AddToRoleAsync(user, "User");
        await userManager.AddClaimsAsync(user,
        [
            new Claim(AppClaimTypeConstants.Department, request.Department.ToLowerInvariant()),
            new Claim(AppClaimTypeConstants.BirthYear, request.BirthYear.ToString()),
        ]);
        return TypedResults.Created();
    }

    // POST /account/login - checks the password (with lockout) and returns a JWT.
    private static async Task<Results<Ok<TokenResponseModel>, ProblemHttpResult>> LoginAsync(
        LoginRequestModel request, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, JwtTokenService tokenService)
    {
        IdentityUser? user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Wrong email or password.");
        }

        // lockoutOnFailure: true -> every wrong password counts; after 3 the account is locked for a while.
        SignInResult result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status423Locked, title: "Too many failed attempts. Try again later.");
        }
        if (!result.Succeeded)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Wrong email or password.");
        }

        // Put who they are into the token: id, email, roles and their stored claims.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Email!),
        };
        claims.AddRange((await userManager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(await userManager.GetClaimsAsync(user));
        return TypedResults.Ok(tokenService.CreateToken(claims));
    }
}
