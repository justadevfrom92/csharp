using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Variations;

// The search term in the URL PATH via the @page route template: /Variations/SearchWithRouteValue/ann.
// "{term?}" = optional. Same as the original Edit page's "{id:int}", but for a string.
public class SearchWithRouteValuePageModel : PageModel
{
    private readonly ContactStore store;

    public SearchWithRouteValuePageModel(ContactStore store)
    {
        this.store = store;
    }

    [BindProperty(SupportsGet = true)]
    public string? Term { get; set; }

    public List<ContactModel> Results { get; private set; } = new();

    public void OnGet()
    {
        Results = store.GetAll().Where(contact => string.IsNullOrEmpty(Term) || contact.Name.Contains(Term, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
