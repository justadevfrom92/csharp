using CookieAuthAndRoles.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookieAuthAndRoles.Controllers;

// Put on the CLASS, so every action here (including ones added later) is admin-only.
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
