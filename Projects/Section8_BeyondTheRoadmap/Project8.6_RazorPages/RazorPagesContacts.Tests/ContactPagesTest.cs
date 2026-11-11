using RazorPagesContacts.Tests.Helpers;

namespace RazorPagesContacts.Tests;

// PositivePositive - valid input -> succeeds
// PositiveNegative - well-formed input that breaks a rule, or a well-formed id that doesn't exist
// NegativeNegative - malformed / missing input (no token, "abc" id, wrong method)
public class ContactPagesTest
{
    // ---------- Create ----------

    [Fact]
    public async Task Create_PositivePositive_ValidContact_RedirectsAndIsListed()
    {
        var app = new RazorPageFormHelper();

        HttpResponseMessage response = await app.PostFormAsync("/Contacts/Create", new Dictionary<string, string>
        {
            ["Contact.Name"] = "Grace Hopper",
            ["Contact.Email"] = "grace@example.com",
            ["Contact.Phone"] = "555-0100",
        });

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Equal("/Contacts", response.Headers.Location?.OriginalString);
        Assert.Contains("Grace Hopper", await app.GetListHtmlAsync());
    }

    [Fact]
    public async Task Create_PositiveNegative_RuleBreakingValues_ShowsErrorsAndAddsNothing()
    {
        var app = new RazorPageFormHelper();

        HttpResponseMessage response = await app.PostFormAsync("/Contacts/Create", new Dictionary<string, string>
        {
            ["Contact.Name"] = "X",                 // too short
            ["Contact.Email"] = "not-an-email",
            ["Contact.Phone"] = "call me maybe",
        });
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(200, (int)response.StatusCode); // same page again
        Assert.Contains("Name must be 2 to 60 characters.", html);
        Assert.Contains("That isn&#x27;t a valid email address.", html);
        Assert.Contains("That isn&#x27;t a valid phone number.", html);
        Assert.DoesNotContain("not-an-email", await app.GetListHtmlAsync());
    }

    [Fact]
    public async Task Create_NegativeNegative_MissingAntiForgeryToken_Returns400()
    {
        var app = new RazorPageFormHelper();

        HttpResponseMessage response = await app.Client.PostAsync("/Contacts/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Contact.Name"] = "Grace Hopper",
            ["Contact.Email"] = "grace@example.com",
        }));

        Assert.Equal(400, (int)response.StatusCode);
        Assert.DoesNotContain("Grace Hopper", await app.GetListHtmlAsync());
    }

    // ---------- Edit ----------

    [Fact]
    public async Task Edit_PositivePositive_ValidChange_IsSaved()
    {
        var app = new RazorPageFormHelper();

        HttpResponseMessage response = await app.PostFormAsync("/Contacts/Edit/2", new Dictionary<string, string>
        {
            ["Contact.Id"] = "2",
            ["Contact.Name"] = "Alan M. Turing",
            ["Contact.Email"] = "alan@example.com",
        });

        Assert.Equal(302, (int)response.StatusCode);
        Assert.Contains("Alan M. Turing", await app.GetListHtmlAsync());
    }

    [Fact]
    public async Task Edit_PositiveNegative_WellFormedIdThatDoesNotExist_Returns404()
    {
        var app = new RazorPageFormHelper();

        Assert.Equal(404, (int)(await app.Client.GetAsync("/Contacts/Edit/9999")).StatusCode);
    }

    [Fact]
    public async Task Edit_PositiveNegative_ChangingTheHiddenIdField_CannotEditAnotherContact()
    {
        var app = new RazorPageFormHelper();

        // The form is for contact 2, but someone edits the hidden field to say 1.
        await app.PostFormAsync("/Contacts/Edit/2", new Dictionary<string, string>
        {
            ["Contact.Id"] = "1",
            ["Contact.Name"] = "Hacked Name",
            ["Contact.Email"] = "hacked@example.com",
        });
        string list = await app.GetListHtmlAsync();

        Assert.Contains("Ada Lovelace", list);  // contact 1 untouched
        Assert.Contains("Hacked Name", list);   // the change went to contact 2 (the id in the URL)
    }

    [Fact]
    public async Task Edit_NegativeNegative_NonNumericId_Returns404()
    {
        var app = new RazorPageFormHelper();

        Assert.Equal(404, (int)(await app.Client.GetAsync("/Contacts/Edit/abc")).StatusCode);
    }

    // ---------- Delete (named handler) ----------

    [Fact]
    public async Task Delete_PositivePositive_NamedHandler_RemovesContact()
    {
        var app = new RazorPageFormHelper();

        HttpResponseMessage response = await app.PostFormAsync("/Contacts/Edit/1", new Dictionary<string, string>(), postPath: "/Contacts/Edit/1?handler=Delete");

        Assert.Equal(302, (int)response.StatusCode);
        Assert.DoesNotContain("Ada Lovelace", await app.GetListHtmlAsync());
    }

    [Fact]
    public async Task Delete_PositiveNegative_AlreadyDeletedContact_Returns404()
    {
        var app = new RazorPageFormHelper();
        await app.PostFormAsync("/Contacts/Edit/1", new Dictionary<string, string>(), postPath: "/Contacts/Edit/1?handler=Delete");

        // The page for 1 is gone, so take the token from another page and try deleting 1 again.
        HttpResponseMessage response = await app.PostFormAsync("/Contacts/Create", new Dictionary<string, string>(), postPath: "/Contacts/Edit/1?handler=Delete");

        Assert.Equal(404, (int)response.StatusCode);
    }

    [Fact]
    public async Task Delete_NegativeNegative_GetRequestToDeleteHandler_DoesNotDelete()
    {
        var app = new RazorPageFormHelper();

        await app.Client.GetAsync("/Contacts/Edit/1?handler=Delete"); // there is no OnGetDelete

        Assert.Contains("Ada Lovelace", await app.GetListHtmlAsync());
    }
}
