using System.Security.Claims;
using CookieAuthAndRoles.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieAuthAndRoles.Controllers;

// Login/register must work for people who are NOT logged in yet.
[AllowAnonymous]
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl)
    {
        AppUser? user = UserStore.CheckPassword(userName ?? "", password ?? "");
        if (user == null)
        {
            // Same message for "no such user" and "wrong password", so attackers can't find valid user names.
            ViewBag.Error = "Wrong user name or password.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Only follow LOCAL return URLs, so a link can't send users to an evil site after login.
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password) || password.Length < 8)
        {
            ViewBag.Error = "User name is required and password must be at least 8 characters.";
            return View();
        }
        // New accounts are ALWAYS normal users. Nobody can make themselves an admin from a form.
        if (!UserStore.Register(userName.Trim(), password, "User"))
        {
            ViewBag.Error = "That user name is taken.";
            return View();
        }
        TempData["Message"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize] // overrides [AllowAnonymous] above: any logged-in user
    public IActionResult Profile()
    {
        return View();
    }
}
