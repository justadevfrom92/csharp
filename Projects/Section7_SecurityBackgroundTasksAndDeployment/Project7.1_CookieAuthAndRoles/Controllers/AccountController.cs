using System.Security.Claims;
using CookieAuthAndRoles.Data;
using CookieAuthAndRoles.Data.Entities;
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
    // GET /Account/Login?ReturnUrl=/Admin  - the cookie middleware sends people here with ReturnUrl.
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST /Account/Login - model binding fills LoginViewModel from the form.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        AppUser? user = UserStore.CheckPassword(form.UserName, form.Password);
        if (user == null)
        {
            // Same message for "no such user" and "wrong password", so attackers can't find valid user names.
            // "" = an error for the whole form (shown by asp-validation-summary), not one field.
            ModelState.AddModelError("", "Wrong user name or password.");
            return View(form);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        // Only follow LOCAL return URLs, so a link can't send users to an evil site after login.
        if (Url.IsLocalUrl(form.ReturnUrl))
        {
            return Redirect(form.ReturnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    // POST /Account/Register - length rules are checked by the attributes on RegisterViewModel.
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }
        // New accounts are ALWAYS normal users. Nobody can make themselves an admin from a form.
        if (!UserStore.Register(form.UserName.Trim(), form.Password, "User"))
        {
            ModelState.AddModelError(nameof(RegisterViewModel.UserName), "That user name is taken.");
            return View(form);
        }
        // TempData survives ONE redirect (to Login). Read in Views/Shared/_Layout.cshtml.
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
