using Microsoft.AspNetCore.Mvc;
using SidebarAndDateBinder.Stores;

namespace SidebarAndDateBinder.Variations;

// The same "recent activity" sidebar, put on a page four ways. Try /variations/sidebar/....
// Original: the layout uses the TAG HELPER <vc:recent-activity-sidebar count="5" />.
[Route("variations/sidebar")]
public class SidebarVariation : Controller
{
    // Component.InvokeAsync("Name", new { ... }) in the view: same HTML as the tag helper, but the name and
    // arguments are STRINGS/anonymous objects - a typo is only found when the page runs.
    [HttpGet("invoke")]
    public IActionResult WithInvokeAsync()
    {
        return View("~/Views/Variations/SidebarInvoke.cshtml");
    }

    // A PARTIAL VIEW: just HTML. It can't load its own data, so the CONTROLLER must fetch it and pass it in -
    // on every page that shows the sidebar. That's the main reason view components exist.
    [HttpGet("partial")]
    public IActionResult WithPartial()
    {
        return View("~/Views/Variations/SidebarPartial.cshtml", ActivityLogStore.GetNewest(5));
    }

    // Returning a view component STRAIGHT from an action: only the sidebar's HTML, no layout.
    // Handy for refreshing just that piece with JavaScript (fetch("/variations/sidebar/only")).
    [HttpGet("only")]
    public IActionResult OnlyTheComponent(int count = 3)
    {
        return ViewComponent("RecentActivitySidebar", new { count });
    }
}
