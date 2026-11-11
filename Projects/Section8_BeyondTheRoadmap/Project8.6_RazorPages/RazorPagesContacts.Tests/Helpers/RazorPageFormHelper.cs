using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RazorPagesContacts.Tests.Helpers;

// A fresh app (fresh contact list) + a client that does NOT follow redirects, so tests can check where they go.
public class RazorPageFormHelper
{
    public RazorPageFormHelper()
    {
        Client = new WebApplicationFactory<Program>().CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public HttpClient Client { get; }

    // Opens the page, reads its anti-forgery token, and posts the form fields with it.
    public async Task<HttpResponseMessage> PostFormAsync(string pagePath, Dictionary<string, string> fields, string? postPath = null)
    {
        string html = await Client.GetStringAsync(pagePath);
        Match token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        fields["__RequestVerificationToken"] = token.Groups[1].Value;
        return await Client.PostAsync(postPath ?? pagePath, new FormUrlEncodedContent(fields));
    }

    public Task<string> GetListHtmlAsync()
    {
        return Client.GetStringAsync("/Contacts");
    }
}
