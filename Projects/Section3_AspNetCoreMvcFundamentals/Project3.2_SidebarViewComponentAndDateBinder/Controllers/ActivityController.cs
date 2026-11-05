using Microsoft.AspNetCore.Mvc;
using MvcSidebarViewComponentAndDateBinder.Models;
using MvcSidebarViewComponentAndDateBinder.Services;

namespace MvcSidebarViewComponentAndDateBinder.Controllers;

[Route("activity")]
public sealed class ActivityController(IRecentActivityFeedService recentActivityFeedService, TimeProvider timeProvider) : Controller
{
    /// <summary>GET /activity?from=2026-11-01&amp;to=2026-11-05&amp;kind=OrderPlaced</summary>
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ActivityReportDateRangeQuery reportQuery, CancellationToken cancellationToken)
    {
        if (reportQuery.InclusiveStartDate is DateOnly start && reportQuery.InclusiveEndDate is DateOnly end && start > end)
        {
            ModelState.AddModelError("to", "'to' must be on or after 'from'.");
        }

        List<string> queryValidationErrors = ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .ToList();

        IReadOnlyList<RecentActivityEntry> matchingEntries = queryValidationErrors.Count > 0
            ? []
            : await recentActivityFeedService.GetEntriesWithinDateRangeAsync(
                reportQuery.InclusiveStartDate, reportQuery.InclusiveEndDate, reportQuery.KindFilter, cancellationToken);

        if (queryValidationErrors.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        return View(new ActivityReportPageViewModel(reportQuery, matchingEntries, queryValidationErrors));
    }

    /// <summary>
    /// GET /activity/day/2026-11-05 — the route value is bound by the GLOBALLY registered
    /// CustomIsoDateFormatModelBinderProvider (no attribute on this parameter).
    /// </summary>
    [HttpGet("day/{summaryDate}")]
    public async Task<IActionResult> DailySummary(DateOnly summaryDate, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.Values.SelectMany(entry => entry.Errors).Select(error => error.ErrorMessage));
        }

        IReadOnlyList<RecentActivityEntry> entriesOnDay = await recentActivityFeedService
            .GetEntriesWithinDateRangeAsync(summaryDate, summaryDate, null, cancellationToken);
        Dictionary<ActivityKind, int> countsByKind = entriesOnDay
            .GroupBy(entry => entry.Kind)
            .ToDictionary(group => group.Key, group => group.Count());
        return View(new DailyActivitySummaryViewModel(summaryDate, countsByKind, entriesOnDay.Count));
    }

    [HttpGet("record")]
    public IActionResult Record() => View(new RecordActivityFormViewModel());

    [HttpPost("record")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(RecordActivityFormViewModel activityForm, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(activityForm);
        }
        await recentActivityFeedService.RecordActivityAsync(activityForm.Kind!.Value, activityForm.ActorDisplayName.Trim(),
            activityForm.ActivityDescription.Trim(), cancellationToken);
        TempData["StatusAlertText"] = "Activity recorded. Check the sidebar.";
        return RedirectToAction(nameof(DailySummary), new { summaryDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).ToString("yyyy-MM-dd") });
    }
}
