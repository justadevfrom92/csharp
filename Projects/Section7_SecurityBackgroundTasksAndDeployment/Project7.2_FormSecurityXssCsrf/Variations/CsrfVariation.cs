using Microsoft.AspNetCore.Mvc;

namespace FormSecurityXssCsrf.Variations;

// The same POST, with and without anti-forgery protection. Program.cs adds AutoValidateAntiforgeryToken to ALL
// controllers (original), so a POST without a token is refused - unless an action opts out.
[Route("variations/csrf")]
public class CsrfVariation : Controller
{
    // Protected by the GLOBAL filter (nothing written here). POST without a token -> 400.
    [HttpPost("protected")]
    public IActionResult Protected(string message)
    {
        return Content($"accepted: {message}");
    }

    // [IgnoreAntiforgeryToken]: opts OUT. Any website could make a logged-in user's browser post here (CSRF).
    // Only right for endpoints that don't use cookies for login (e.g. APIs with a token header, see 8.2).
    [HttpPost("ignored")]
    [IgnoreAntiforgeryToken]
    public IActionResult Ignored(string message)
    {
        return Content($"accepted WITHOUT a token: {message}");
    }

    // GET requests are never checked (they must not change anything). Changing data in a GET is the mistake:
    // <img src="/variations/csrf/delete-via-get"> on ANY site would trigger it.
    [HttpGet("delete-via-get")]
    public IActionResult DeleteViaGet()
    {
        return Content("pretend something was deleted - by a GET, with no protection at all");
    }
}
