using Microsoft.AspNetCore.Mvc;
using SidebarAndDateBinder.Data;
using SidebarAndDateBinder.ModelBinders;
using SidebarAndDateBinder.Models;

namespace SidebarAndDateBinder.Controllers;

public class ActivityController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            ActivityLog.Add(text.Trim());
        }
        return RedirectToAction(nameof(Index));
    }

    // GET /Activity/Day?day=2026-11-05
    public IActionResult Day([ModelBinder(typeof(CustomIsoDateFormatModelBinder))] DateOnly? day)
    {
        DateOnly chosenDay = day ?? DateOnly.FromDateTime(DateTime.Today);

        var page = new DailyActivityViewModel
        {
            Day = chosenDay,
            Entries = ActivityLog.GetForDay(chosenDay),
        };
        if (!ModelState.IsValid)
        {
            // The binder rejected the date; show its message on the page.
            page.DateErrorMessage = ModelState["day"]?.Errors.FirstOrDefault()?.ErrorMessage;
        }
        return View(page);
    }
}
