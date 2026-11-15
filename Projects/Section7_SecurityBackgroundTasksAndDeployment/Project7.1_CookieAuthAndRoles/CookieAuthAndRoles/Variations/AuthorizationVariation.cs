using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieAuthAndRoles.Variations;

// Role checks and sign-in options written different ways. The users: "admin" has role Admin, "alice" has role User.
// Remember: the global filter in Program.cs already requires a login for every action here.
[Route("variations/auth")]
public class AuthorizationVariation : Controller
{
    // ONE attribute, roles separated by commas = OR: Admin OR User may enter.
    [HttpGet("admin-or-user")]
    [Authorize(Roles = "Admin,User")]
    public IActionResult AdminOrUser()
    {
        return Content("allowed: Admin OR User");
    }

    // TWO attributes = AND: you need BOTH roles. Nobody here has both, so everyone is refused.
    [HttpGet("admin-and-user")]
    [Authorize(Roles = "Admin")]
    [Authorize(Roles = "User")]
    public IActionResult AdminAndUser()
    {
        return Content("allowed: Admin AND User");
    }

    // The check written INSIDE the action instead of an attribute. Same result as [Authorize(Roles = "Admin")]
    // (Forbid() -> the cookie handler sends the user to /Account/AccessDenied), but invisible from outside the method.
    [HttpGet("manual-check")]
    public IActionResult ManualCheck()
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }
        return Content("allowed: manual IsInRole check");
    }

    // Returning a status code yourself: plain 401/403 - NO redirect to the login or access-denied page.
    // Right for APIs; wrong for pages a person is looking at.
    [HttpGet("plain-status")]
    public IActionResult PlainStatus()
    {
        if (!User.IsInRole("Admin"))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }
        return Content("allowed");
    }

    // "Remember me": IsPersistent = true gives the cookie an EXPIRY date, so it survives closing the browser.
    // false (the original Login) = a "session cookie" that's deleted when the browser closes.
    [HttpGet("sign-in-demo")]
    [AllowAnonymous]
    public async Task<IActionResult> SignInDemo(bool remember)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "alice"), new Claim(ClaimTypes.Role, "User")], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = remember });
        return Content($"signed in, remember = {remember}");
    }
}
