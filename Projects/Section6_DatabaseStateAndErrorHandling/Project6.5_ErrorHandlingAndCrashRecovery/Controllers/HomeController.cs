using Microsoft.AspNetCore.Mvc;

namespace ErrorHandlingAndCrashRecovery.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Simulates a bug that leaks secret details in its message.
    public IActionResult Crash()
    {
        throw new InvalidOperationException("Database login failed for user 'sa' with password 'Hunter2' on server 10.0.0.5");
    }

    public IActionResult MissingProduct(int id)
    {
        return NotFound(); // status code only -> UseStatusCodePagesWithReExecute shows the 404 page
    }
}
