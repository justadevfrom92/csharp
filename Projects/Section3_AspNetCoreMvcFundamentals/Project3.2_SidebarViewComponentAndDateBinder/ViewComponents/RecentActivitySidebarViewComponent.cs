using Microsoft.AspNetCore.Mvc;
using MvcSidebarViewComponentAndDateBinder.Services;

namespace MvcSidebarViewComponentAndDateBinder.ViewComponents;

/// <summary>
/// Self-contained "Recent Activity" widget. It resolves its own dependency and loads its own data,
/// so no controller action has to fetch sidebar data or stuff it into ViewBag.
/// Rendered from the layout with &lt;vc:recent-activity-sidebar maximum-entry-count="5" /&gt;.
/// </summary>
public sealed class RecentActivitySidebarViewComponent(IRecentActivityFeedService recentActivityFeedService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int maximumEntryCount = 5)
    {
        var recentEntries = await recentActivityFeedService.GetMostRecentEntriesAsync(maximumEntryCount, HttpContext.RequestAborted);

        // Different views for different states keeps the Razor free of branching.
        return recentEntries.Count == 0
            ? View("Empty")
            : View(recentEntries);
    }
}
