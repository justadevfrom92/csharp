using Microsoft.AspNetCore.Mvc;
using SidebarAndDateBinder.Data;
using SidebarAndDateBinder.Data.Entities;

namespace SidebarAndDateBinder.ViewComponents;

// Used in the layout with: <vc:recent-activity-sidebar count="5" />
// It loads its own data, so no controller has to pass activity data to the view.
public class RecentActivitySidebarViewComponent : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync(int count)
    {
        List<ActivityEntry> newestEntries = ActivityLog.GetNewest(count);
        IViewComponentResult result = View(newestEntries); // Views/Shared/Components/RecentActivitySidebar/Default.cshtml
        return Task.FromResult(result);
    }
}
