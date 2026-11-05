using System.Security.Claims;
using CookieAuthAndRoles.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieAuthAndRoles.Controllers;

// Login/register/access-denied are marked [AllowAnonymous]: they must work for people who are NOT logged in yet.
// Everything else here (Logout, Profile) needs a login because of the global filter in Program.cs.
public class AccountController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // No attribute needed: the global filter already requires a login.
    public IActionResult Profile()
    {
        return View();
    }
}
