using FormSecurityXssCsrf.Data;
using FormSecurityXssCsrf.Data.Entities;
using FormSecurityXssCsrf.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormSecurityXssCsrf.Controllers;

public class GuestbookController : Controller
{
    public IActionResult Index()
    {
        var page = new GuestbookPageViewModel { Entries = GuestbookStore.GetNewestFirst() };
        return View(page);
    }

    // No [ValidateAntiForgeryToken] needed here: it's added to ALL POST actions globally in Program.cs.
    // Model binding fills Name, Message and Website from the form.
    [HttpPost]
    public IActionResult Index(GuestbookPageViewModel form)
    {
        if (!string.IsNullOrEmpty(form.Website) && !IsSafeWebLink(form.Website))
        {
            ModelState.AddModelError(nameof(form.Website), "Only http:// and https:// links are allowed.");
        }
        if (!ModelState.IsValid)
        {
            form.Entries = GuestbookStore.GetNewestFirst(); // not posted, so load again
            return View(form);
        }

        // We store exactly what the user typed. Encoding happens when SHOWING it (see the view).
        GuestbookStore.Add(new GuestbookEntry
        {
            Name = form.Name,
            Message = form.Message,
            Website = form.Website,
            PostedAtUtc = DateTime.UtcNow,
        });
        return RedirectToAction(nameof(Index));
    }

    // Blocks links like "javascript:alert(1)" that would run code when clicked.
    private static bool IsSafeWebLink(string link)
    {
        return Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
