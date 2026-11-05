using Microsoft.AspNetCore.Mvc;

namespace MvcProductCatalog.Controllers;

/// <summary>Reached through CONVENTIONAL routing: {controller=Home}/{action=Index}/{id?}.</summary>
public sealed class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult About() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();

    /// <summary>No [HttpGet]: re-executed requests keep their original verb (GET, POST, ...).</summary>
    [Route("status/{statusCode:int}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View(statusCode);
    }
}
