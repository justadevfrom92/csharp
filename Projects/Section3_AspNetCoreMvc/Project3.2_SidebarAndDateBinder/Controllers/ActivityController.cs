using Microsoft.AspNetCore.Mvc;
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
        if (!ModelState.IsValid)
        {
            ViewBag.Error = ModelState["day"]?.Errors.FirstOrDefault()?.ErrorMessage;
        }

        DateOnly chosenDay = day ?? DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Day = chosenDay;
        return View(ActivityLog.GetForDay(chosenDay));
    }
}
