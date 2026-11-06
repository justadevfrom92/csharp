using FormSecurityXssCsrf.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormSecurityXssCsrf.Controllers;

public class GuestbookController : Controller
{
    private static readonly List<GuestbookEntryModel> entries = new();
    private static readonly object listLock = new();

    public IActionResult Index()
    {
        lock (listLock)
        {
            ViewBag.Entries = entries.OrderByDescending(entry => entry.PostedAtUtc).ToList();
        }
        return View(new GuestbookEntryModel());
    }

    // No [ValidateAntiForgeryToken] needed here: it's added to ALL POST actions globally in Program.cs.
    [HttpPost]
    public IActionResult Index(GuestbookEntryModel entry)
    {
        if (!string.IsNullOrEmpty(entry.Website) && !IsSafeWebLink(entry.Website))
        {
            ModelState.AddModelError(nameof(entry.Website), "Only http:// and https:// links are allowed.");
        }
        if (!ModelState.IsValid)
        {
            lock (listLock)
            {
                ViewBag.Entries = entries.OrderByDescending(existing => existing.PostedAtUtc).ToList();
            }
            return View(entry);
        }

        // We store exactly what the user typed. Encoding happens when SHOWING it (see the view).
        entry.PostedAtUtc = DateTime.UtcNow;
        lock (listLock)
        {
            entries.Add(entry);
        }
        return RedirectToAction(nameof(Index));
    }

    // Blocks links like "javascript:alert(1)" that would run code when clicked.
    private static bool IsSafeWebLink(string link)
    {
        return Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
