using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CookieAuthAndRoles.Tests.Helpers;

// Small steps every auth test needs: a fresh client, reading the anti-forgery token, logging in.
public static class AccountTestHelper
{
    public const string AuthCookieName = ".Roadmap.Auth";

    // A NEW client = its own, empty cookie jar, so no test can see another test's login.
    // https: the cookies are marked Secure, so they're only sent over HTTPS.
    // No auto-redirect: the tests check WHERE the app redirects to.
    public static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    // Opens a page and reads the hidden __RequestVerificationToken the form tag helper put on it.
    public static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string pagePath)
    {
        string html = await client.GetStringAsync(pagePath);
        Match match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success)
        {
            throw new InvalidOperationException($"No anti-forgery token on {pagePath}.");
        }
        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string userName, string password, string? returnUrl = null)
    {
        string token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["userName"] = userName,
            ["password"] = password,
            ["returnUrl"] = returnUrl ?? "",
        };
        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(fields));
    }

    public static bool SetsAuthCookie(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
            && cookies.Any(cookie => cookie.StartsWith(AuthCookieName + "=") && !cookie.StartsWith(AuthCookieName + "=;"));
    }

    // Where a 302 redirect points to, e.g. "/Admin" or "/Account/Login?ReturnUrl=%2FAdmin".
    public static string RedirectTarget(HttpResponseMessage response)
    {
        Uri location = response.Headers.Location ?? throw new InvalidOperationException("Not a redirect.");
        return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
    }
}
