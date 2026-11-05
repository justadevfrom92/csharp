using Microsoft.AspNetCore.Mvc;

namespace MvcSidebarViewComponentAndDateBinder.Controllers;

/// <summary>Notice: no action here knows anything about the activity sidebar.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Error() => View();
}
