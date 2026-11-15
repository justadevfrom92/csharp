using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SidebarAndDateBinder.ModelBinders;

namespace SidebarAndDateBinder.Variations;

// The same "?day=..." read three ways. Try /variations/dates/custom?day=2026-11-05 (and other formats).
[Route("variations/dates")]
public class DateBindingVariation : Controller
{
    // ORIGINAL: the custom model binder - ONLY yyyy-MM-dd; anything else is a ModelState error with a clear message.
    [HttpGet("custom")]
    public IActionResult WithCustomBinder([ModelBinder(typeof(CustomIsoDateFormatModelBinder))] DateOnly? day)
    {
        return Content(Describe(day));
    }

    // Built-in binding: MVC reads query-string values with InvariantCulture, so "11/05/2026" is read as
    // MONTH/day (5 November) - a British user meaning 11 May gets the wrong day, silently.
    [HttpGet("default")]
    public IActionResult WithDefaultBinding(DateOnly? day)
    {
        return Content(Describe(day));
    }

    // Take a string and parse it in the action: same strictness as the custom binder, but repeated in
    // every action that needs a date. A binder writes it once.
    [HttpGet("manual")]
    public IActionResult WithManualParse(string? day)
    {
        if (day != null && !DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            ModelState.AddModelError(nameof(day), $"'{day}' is not a date in yyyy-MM-dd format.");
        }
        DateOnly? parsed = ModelState.IsValid && day != null ? DateOnly.ParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
        return Content(Describe(parsed));
    }

    private string Describe(DateOnly? day)
    {
        string errors = string.Join(" ", ModelState.Values.SelectMany(entry => entry.Errors).Select(error => error.ErrorMessage));
        return $"day={(day.HasValue ? day.Value.ToString("yyyy-MM-dd") : "none")} valid={ModelState.IsValid} {errors}".Trim();
    }
}
