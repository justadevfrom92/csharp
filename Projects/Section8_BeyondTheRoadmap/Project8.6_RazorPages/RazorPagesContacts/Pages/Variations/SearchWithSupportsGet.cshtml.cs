using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RazorPagesContacts.Models;
using RazorPagesContacts.Stores;

namespace RazorPagesContacts.Pages.Variations;

// [BindProperty(SupportsGet = true)]: the property is filled on GET too - from the query string (?term=ann).
// The usual way to build a search/filter page.
public class SearchWithSupportsGetPageModel : PageModel
{
    private readonly ContactStore store;

    public SearchWithSupportsGetPageModel(ContactStore store)
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
