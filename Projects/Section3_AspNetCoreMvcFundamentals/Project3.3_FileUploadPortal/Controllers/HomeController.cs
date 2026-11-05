using Microsoft.AspNetCore.Mvc;

namespace MvcFileUploadPortal.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}
